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

    void OnEnable()
    {
        if (targetQuad != null)
        {
            UpdateTransformFromQuad();
        }

        List<Vector2> allPoints = new List<Vector2>();
        float canvasWidth = 512f;
        float canvasHeight = 512f;

        switch (jsonSource)
        {
            case JsonSourceType.LocalJsonFile:
                if (jsonFile == null)
                    return;
                DrawingDataRoot root = JsonUtility.FromJson<DrawingDataRoot>(jsonFile.text);
                if (root?.drawingData?.shapeData == null)
                    return;

                // drawingData에 canvasWidth, canvasHeight가 없으면 기본값 사용
                canvasWidth = root.drawingData.canvasWidth != 0f ? root.drawingData.canvasWidth : 512f;
                canvasHeight = root.drawingData.canvasHeight != 0f ? root.drawingData.canvasHeight : 512f;

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
                break;

            case JsonSourceType.FirebaseRealtime:
                if (EventListener.Instance == null)
                    return;
                string jsonText = EventListener.Instance.GetCurrentSketchJson();
                if (string.IsNullOrEmpty(jsonText))
                    return;

                JObject json = JObject.Parse(jsonText);
                JObject drawingData = json["drawingData"] as JObject;
                if (drawingData == null)
                    return;

                canvasWidth = drawingData["canvasWidth"]?.ToObject<float>() ?? 512f;
                canvasHeight = drawingData["canvasHeight"]?.ToObject<float>() ?? 512f;

                JArray shapeData = drawingData["shapeData"] as JArray;
                if (shapeData == null)
                    return;

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
                break;
        }

        if (allPoints.Count > 0)
        {
            ProcessJsonData(allPoints, canvasWidth, canvasHeight);
        }
    }

    private void UpdateTransformFromQuad()
    {
        // 쿼드의 월드 크기 (x, y에만 오프셋 적용)
        Vector3 worldSize = targetQuad.lossyScale;

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

    private void ProcessJsonData(List<Vector2> allPoints, float canvasWidth, float canvasHeight)
    {
        try
        {
            if (vfx == null)
                return;

            // canvasWidth, canvasHeight로 정규화
            List<Vector2> normalizedPoints = new List<Vector2>();
            for (int i = 0; i < allPoints.Count; i++)
            {
                normalizedPoints.Add(new Vector2(
                    allPoints[i].x / canvasWidth,
                    allPoints[i].y / canvasHeight
                ));
            }

            Color[] pixels = new Color[normalizedPoints.Count];
            for (int i = 0; i < normalizedPoints.Count; i++)
            {
                pixels[i] = new Color(normalizedPoints[i].x, normalizedPoints[i].y, 0, 0);
            }

            Texture2D tex = new Texture2D(normalizedPoints.Count, 1, TextureFormat.RGFloat, false, true);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels(pixels);
            tex.Apply();

            vfx.SetTexture(propertyName, tex);
            vfx.SetInt(sizeProperty, normalizedPoints.Count);
        }
        catch (Exception e)
        {
            // Debug.LogError($"[JsonPointsToTexture] JSON 처리 실패: {e.Message}");
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
    public float canvasWidth;
    public float canvasHeight;
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