using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
public class JsonPointsToTexture : MonoBehaviour
{
    public VisualEffect vfx;
    public string propertyName = "positionMap";
    public string sizeProperty = "positionMapSize";
    public TextAsset jsonFile; // 인스펙터에서 할당

    // 타겟 쿼드 오브젝트를 인스펙터에서 할당
    public Transform targetQuad;

    // 쿼드 로컬 공간에서의 오프셋 (기본값: 좌하단, z도 입력 가능)
    public Vector3 quadLocalOffset = new Vector3(-0.5f, -0.5f, 0f);

    void OnEnable()
    {
        // 타겟 쿼드의 오프셋 위치에 현재 오브젝트 위치시키기
        if (targetQuad != null)
        {
            // 쿼드의 로컬 오프셋 (사용자 입력)
            Vector3 worldOffset = targetQuad.TransformVector(Vector3.Scale(quadLocalOffset, targetQuad.localScale));
            transform.position = targetQuad.position + worldOffset;
            transform.rotation = targetQuad.rotation;
            transform.localScale = targetQuad.lossyScale;
        }

        if (jsonFile == null)
        {
            Debug.LogError("Json file not assigned!");
            return;
        }

        // 1. JSON 파일 읽기
        string jsonText = jsonFile.text;

        // 2. JSON 파싱 (Unity 내장 JsonUtility 사용)
        DrawingDataRoot root = JsonUtility.FromJson<DrawingDataRoot>(jsonText);

        // 3. 모든 points를 하나의 리스트로 합치기
        List<Vector2> allPoints = new List<Vector2>();
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

        // 4. 정규화 (0~1) : 전체 points의 min/max로 정규화
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        foreach (var pt in allPoints)
        {
            if (pt.x < minX) minX = pt.x;
            if (pt.x > maxX) maxX = pt.x;
            if (pt.y < minY) minY = pt.y;
            if (pt.y > maxY) maxY = pt.y;
        }

        // 5. Color 배열 생성 (R=x, G=y)
        Color[] pixels = new Color[allPoints.Count];
        for (int i = 0; i < allPoints.Count; i++)
        {
            float fx = (maxX - minX) > 0 ? (allPoints[i].x - minX) / (maxX - minX) : 0f;
            float fy = (maxY - minY) > 0 ? (allPoints[i].y - minY) / (maxY - minY) : 0f;
            pixels[i] = new Color(fx, fy, 0, 0);
        }

        // 6. 1D 텍스처 생성
        Texture2D tex = new Texture2D(allPoints.Count, 1, TextureFormat.RGFloat, false, true);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels(pixels);
        tex.Apply();

        // 7. VFX에 텍스처와 사이즈 전달
        vfx.SetTexture(propertyName, tex);
        vfx.SetInt(sizeProperty, allPoints.Count);
    }

    void OnValidate()
    {
        if (jsonFile == null || vfx == null) return;
        OnEnable(); // 기존 로직 재사용
    }
}

// JSON 파싱용 클래스
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