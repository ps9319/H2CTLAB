using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Newtonsoft.Json.Linq;
using System;
// using Google.MiniJSON;

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
            UpdateTransformFromQuad();

        var drawingData = GetDrawingData();
        if (drawingData == null)
        {
            Debug.LogError("[JsonPointsToTexture] drawingData가 JSON에 없습니다.");
            return;
        }

        float canvasWidth = drawingData["canvasWidth"]?.ToObject<float>() ?? 512f;
        float canvasHeight = drawingData["canvasHeight"]?.ToObject<float>() ?? 512f;

        var pointsStr = drawingData["shapeData"]?["points"]?.ToString();
        var points = !string.IsNullOrEmpty(pointsStr) ? JArray.Parse(pointsStr) : new JArray();
        if (points.Count < 2) return;

        var allPoints = new List<Vector2>(points.Count / 2);
        for (int i = 0; i < points.Count - 1; i += 2)
        {
            float x = points[i]?.ToObject<float>() ?? 0f;
            float y = points[i + 1]?.ToObject<float>() ?? 0f;
            allPoints.Add(new Vector2(x / canvasWidth, y / canvasHeight));
        }

        if (allPoints.Count > 0)
            ProcessJsonData(allPoints);
    }

    private JObject GetDrawingData()
    {
        string jsonText = jsonSource switch
        {
            JsonSourceType.LocalJsonFile => jsonFile?.text,
            JsonSourceType.FirebaseRealtime => EventListener.Instance?.GetCurrentSketchJson(),
            _ => null
        };
        if (string.IsNullOrEmpty(jsonText)) return null;

        var json = JObject.Parse(jsonText);
        var sketchJson = jsonSource == JsonSourceType.LocalJsonFile ? json["sketch_json"] as JObject : json;
        return sketchJson?["drawingData"] as JObject;
    }

    private void ProcessJsonData(List<Vector2> allPoints)
    {
        try
        {
            if (vfx == null)
                return;

            Color[] pixels = new Color[allPoints.Count];
            for (int i = 0; i < allPoints.Count; i++)
            {
                pixels[i] = new Color(allPoints[i].x, allPoints[i].y, 0, 0);
            }

            Texture2D tex = new Texture2D(allPoints.Count, 1, TextureFormat.RGFloat, false, true);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels(pixels);
            tex.Apply();

            vfx.SetTexture(propertyName, tex);
            vfx.SetInt(sizeProperty, allPoints.Count);
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonPointsToTexture] JSON 처리 실패: {e.Message}");
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

// // JSON 파싱용 클래스 (LocalJsonFile 모드용)
// [System.Serializable]
// public class DrawingDataRoot
// {
//     public DrawingData drawingData;
// }

// [System.Serializable]
// public class DrawingData
// {
//     public float canvasWidth;
//     public float canvasHeight;
//     public List<ShapeData> shapeData;
// }

// [System.Serializable]
// public class ShapeData
// {
//     public float x;
//     public float y;
//     public List<PointData> points;
// }

// [System.Serializable]
// public class PointData
// {
//     public float x;
//     public float y;
// }