using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Newtonsoft.Json.Linq;
using System;

[ExecuteAlways]
public class JsonPointsToTexture : MonoBehaviour
{
    // JSON 소스 타입 선택
    public enum JsonSourceType
    {
        LocalJsonFile,      // 저장된 JSON 파일 사용
        FirebaseRealtime    // Firebase 실시간 데이터 사용
    }
    
    [Header("JSON Source Settings")]
    [Tooltip("JSON 데이터 소스를 선택하세요")]
    public JsonSourceType jsonSource = JsonSourceType.LocalJsonFile;
    
    [Header("VFX Settings")]
    public VisualEffect vfx;
    public string propertyName = "positionMap";
    public string sizeProperty = "positionMapSize";
    
    [Header("Local JSON File (LocalJsonFile 모드에서만 사용)")]
    [Tooltip("LocalJsonFile 모드일 때만 사용됩니다")]
    public TextAsset jsonFile; // 인스펙터에서 할당

    [Header("Quad Transform Settings")]
    // 타겟 쿼드 오브젝트를 인스펙터에서 할당
    public Transform targetQuad;

    // 쿼드 로컬 공간에서의 오프셋 (기본값: 좌하단, z도 입력 가능)
    public Vector3 quadLocalOffset = new Vector3(-0.5f, -0.5f, 0f);
    // 쿼드 스케일에 곱해지는 오프셋 (x, y에 동시에 적용)
    [Range(0.01f, 10f)]
    public float quadScaleOffset = 1f;

    void OnEnable()
    {
        // 타겟 쿼드의 오프셋 위치에 현재 오브젝트 위치시키기
        if (targetQuad != null)
        {
            UpdateTransformFromQuad();
        }

        // JSON 소스에 따라 처리 분기
        string jsonText = null;
        
        switch (jsonSource)
        {
            case JsonSourceType.LocalJsonFile:
                if (jsonFile == null)
                {
                    Debug.LogError("[JsonPointsToTexture] LocalJsonFile 모드: JSON 파일이 할당되지 않았습니다!");
                    return;
                }
                jsonText = jsonFile.text;
                Debug.Log("[JsonPointsToTexture] LocalJsonFile 모드로 JSON 처리");
                break;
                
            case JsonSourceType.FirebaseRealtime:
                if (EventListener.Instance == null)
                {
                    Debug.LogWarning("[JsonPointsToTexture] FirebaseRealtime 모드: EventListener 인스턴스를 찾을 수 없습니다.");
                    return;
                }
                jsonText = EventListener.Instance.GetCurrentSketchJson();
                if (string.IsNullOrEmpty(jsonText))
                {
                    Debug.LogWarning("[JsonPointsToTexture] FirebaseRealtime 모드: Firebase에서 JSON 데이터를 가져올 수 없습니다.");
                    return;
                }
                Debug.Log("[JsonPointsToTexture] FirebaseRealtime 모드로 JSON 처리");
                break;
        }

        if (!string.IsNullOrEmpty(jsonText))
        {
            ProcessJsonData(jsonText);
        }
    }

    private void UpdateTransformFromQuad()
    {
        // 쿼드의 월드 크기 (x, y에만 오프셋 적용)
        Vector3 worldSize = targetQuad.lossyScale;
        worldSize.x *= quadScaleOffset;
        worldSize.y *= quadScaleOffset;

        // 쿼드의 중앙에서 오프셋까지의 벡터 (쿼드의 로컬 공간)
        Vector3 localOffset = quadLocalOffset;
        // 쿼드의 월드 공간에서의 오프셋 위치
        Vector3 offsetWorld = targetQuad.rotation * Vector3.Scale(localOffset, worldSize);
        Vector3 targetWorldPos = targetQuad.position + offsetWorld;
        // 위치와 회전 적용
        transform.SetPositionAndRotation(targetWorldPos, targetQuad.rotation);

        // 부모의 스케일을 고려한 localScale 적용
        if (transform.parent != null)
        {
            Vector3 parentScale = transform.parent.lossyScale;
            transform.localScale = new Vector3(
                worldSize.x / parentScale.x,
                worldSize.y / parentScale.y,
                worldSize.z / parentScale.z
            );
        }
        else
        {
            transform.localScale = worldSize;
        }
    }

    private void ProcessJsonData(string jsonText)
    {
        try
        {
            if (vfx == null)
            {
                Debug.LogError("[JsonPointsToTexture] VFX가 할당되지 않았습니다.");
                return;
            }

            List<Vector2> allPoints = new List<Vector2>();

            // JSON 소스에 따라 파싱 방법 선택
            if (jsonSource == JsonSourceType.LocalJsonFile)
            {
                // Unity JsonUtility 사용 (기존 방식)
                DrawingDataRoot root = JsonUtility.FromJson<DrawingDataRoot>(jsonText);
                
                if (root?.drawingData?.shapeData == null)
                {
                    Debug.LogError("[JsonPointsToTexture] LocalJsonFile: JSON 구조가 올바르지 않습니다.");
                    return;
                }

                foreach (var shape in root.drawingData.shapeData)
                {
                    if (shape.points != null)
                    {
                        foreach (var pt in shape.points)
                        {
                            allPoints.Add(new Vector2(pt.x, pt.y));
                        }
                    }
                }
            }
            else // FirebaseRealtime
            {
                // Newtonsoft.Json 사용 (Firebase 방식)
                JObject json = JObject.Parse(jsonText);
                JArray shapeData = json["drawingData"]?["shapeData"] as JArray;

                if (shapeData == null || shapeData.Count == 0)
                {
                    Debug.LogError("[JsonPointsToTexture] FirebaseRealtime: shapeData를 찾을 수 없습니다.");
                    return;
                }

                foreach (JObject shape in shapeData)
                {
                    JArray points = shape["points"] as JArray;
                    if (points == null) continue;

                    foreach (JObject point in points)
                    {
                        float x = point["x"]?.ToObject<float>() ?? 0f;
                        float y = point["y"]?.ToObject<float>() ?? 0f;
                        allPoints.Add(new Vector2(x, y));
                    }
                }
            }

            if (allPoints.Count == 0)
            {
                Debug.LogWarning("[JsonPointsToTexture] 포인트가 없습니다.");
                return;
            }

            // 정규화 (0~1) : 전체 points의 min/max로 정규화
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            
            foreach (var pt in allPoints)
            {
                if (pt.x < minX) minX = pt.x;
                if (pt.x > maxX) maxX = pt.x;
                if (pt.y < minY) minY = pt.y;
                if (pt.y > maxY) maxY = pt.y;
            }

            // Color 배열 생성 (R=x, G=y)
            Color[] pixels = new Color[allPoints.Count];
            for (int i = 0; i < allPoints.Count; i++)
            {
                float fx = (maxX - minX) > 0 ? (allPoints[i].x - minX) / (maxX - minX) : 0f;
                float fy = (maxY - minY) > 0 ? (allPoints[i].y - minY) / (maxY - minY) : 0f;
                pixels[i] = new Color(fx, fy, 0, 0);
            }

            // 1D 텍스처 생성
            Texture2D tex = new Texture2D(allPoints.Count, 1, TextureFormat.RGFloat, false, true);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels(pixels);
            tex.Apply();

            // VFX에 텍스처와 사이즈 전달
            vfx.SetTexture(propertyName, tex);
            vfx.SetInt(sizeProperty, allPoints.Count);

            Debug.Log($"[JsonPointsToTexture] {allPoints.Count}개의 포인트로 텍스처 생성 완료 (모드: {jsonSource})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonPointsToTexture] JSON 처리 실패: {e.Message}");
        }
    }

    void OnValidate()
    {
        // 에디터에서 Inspector 값 변경 시
        if (targetQuad != null)
        {
            UpdateTransformFromQuad();
        }

        // LocalJsonFile 모드이고 필요한 값이 모두 있을 때만 재처리
        if (jsonSource == JsonSourceType.LocalJsonFile && jsonFile != null && vfx != null)
        {
            OnEnable();
        }
    }
}

// JSON 파싱용 클래스 (LocalJsonFile 모드용)
[System.Serializable]
public class DrawingDataRoot
{
    public DrawingData drawingData;
}

[System.Serializable]
public class DrawingData
{
    public List<ShapeData> shapeData;
}

[System.Serializable]
public class ShapeData
{
    public float x;
    public float y;
    public List<PointData> points;
}

[System.Serializable]
public class PointData
{
    public float x;
    public float y;
}