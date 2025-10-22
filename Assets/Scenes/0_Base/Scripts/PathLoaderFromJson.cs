using System;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class PathLoaderFromJson : MonoBehaviour
{
    [Header("JSON File (TextAsset)")]
    public TextAsset jsonFileAsset;

    [Header("Path Settings")]
    public bool closedPath = false;
    [Range(0f, 2f)] public float autoControlLength = 0.3f;
    public float scale = 1f;
    public Vector2 offset = Vector2.zero;

    // [Header("Transform Settings")]
    // public Vector3 childPathPosition = Vector3.zero;
    // public Vector3 childPathRotation = Vector3.zero;
    // public Vector3 childPathScale = Vector3.one;

    [Header("Target Quad")]
    public Transform quadObject; // 드래그해서 넣을 Quad 오브젝트

    // [Header("Scale Constraint")]
    // public bool constrainChildScaleProportion = true;

    [System.Serializable]
    public class Point
    {
        public float x;
        public float y;
    }

    [System.Serializable]
    public class Shape
    {
        public string name;
        public List<Point> points;
    }

    [System.Serializable]
    public class DrawingData
    {
        public List<Shape> shapeData;
    } 

    [System.Serializable]
    public class Root
    {
        public DrawingData drawingData;
    }

    /// <summary>
    /// target quad의 중심(quadCenter)과 크기(quadSize)를 받아,
    /// 현재 오브젝트의 위치를 해당 quad의 좌하단으로 이동시킵니다.
    /// </summary>
    public void SetPositionToQuadBottomLeft(Transform quadObj)
    {
        if (quadObj == null) return;

        Vector3 center = quadObj.position;
        Vector3 worldSize = quadObj.lossyScale;
        Quaternion rotation = quadObj.rotation;
        Vector3 bottomLeft = center - rotation * new Vector3(worldSize.x / 2f, worldSize.y / 2f, 0f);

        transform.position = bottomLeft;
        transform.rotation = rotation;

        // 부모의 scale을 고려해서 localScale 계산
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

    /// <summary>
    /// shape 좌표를 quad 기준으로 정규화하여 path를 생성합니다.
    /// useUndo: 에디터에서 Undo 지원 여부
    /// </summary>
    private void CreatePathsFromJson(bool useUndo)
    {
        if (jsonFileAsset == null)
        {
            Debug.LogError("Please assign a JSON file (TextAsset).");
            return;
        }

        string json = jsonFileAsset.text;
        Root root = JsonUtility.FromJson<Root>(json);
        if (root == null || root.drawingData == null || root.drawingData.shapeData == null || root.drawingData.shapeData.Count == 0)
        {
            Debug.LogError("Invalid JSON structure.");
            return;
        }

        // Remove existing child objects
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (useUndo)
                UnityEditor.Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // 오브젝트 정보 가져오기
        Vector3 objSize = transform.localScale;
        Vector3 objPos = transform.position;

        foreach (var shape in root.drawingData.shapeData)
        {
            if (shape.points == null || shape.points.Count < 2)
                continue;

            List<Vector3> positions = new List<Vector3>();
            foreach (var p in shape.points)
            {
                float normX = p.x / 500f;
                float normY = p.y / 500f;
                float objZ = objPos.z;
                Vector3 xy = objPos + new Vector3(normX * objSize.x, normY * objSize.y, 0) + (Vector3)offset;
                Vector3 worldPos = new Vector3(xy.x, xy.y, objZ);
                positions.Add(worldPos);
            }

            GameObject go = new GameObject(string.IsNullOrEmpty(shape.name) ? "Path" : shape.name);
            if (useUndo)
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Path");
            go.transform.parent = this.transform;

            var pathCreator = go.AddComponent<PathCreator>();
            var bezierPath = new BezierPath(positions, closedPath, PathSpace.xyz)
            {
                AutoControlLength = autoControlLength
            };
            pathCreator.bezierPath = bezierPath;
        }

        // CustomPathFollower 새로고침 (런타임만)
        if (!useUndo)
        {
            var followers = FindObjectsOfType<CustomPathFollower>();
            foreach (var follower in followers)
            {
                if (follower.pathParent == this.transform)
                {
                    follower.RefreshPaths();
                }
            }
        }
    }

    void Start()
    {
        SetPositionToQuadBottomLeft(quadObject);
        CreatePathsFromJson(false);
    }

    void Update()
    {
        SetPositionToQuadBottomLeft(quadObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        SetPositionToQuadBottomLeft(quadObject);
    }

    void OnValidate()
    {
        SetPositionToQuadBottomLeft(quadObject);
    }

    public void CreateEditablePathsFromJson()
    {
        SetPositionToQuadBottomLeft(quadObject);
        CreatePathsFromJson(true);
    }

    public void ClearAllPathsInEditor()
    {
        // Remove all child objects (with Undo support)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            UnityEditor.Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
        }
    }

    public void PrintAllShapePointsInEditor()
    {
        if (jsonFileAsset == null)
        {
            Debug.LogError("Please assign a JSON file (TextAsset).");
            return;
        }

        string json = jsonFileAsset.text;

        Root root = JsonUtility.FromJson<Root>(json);
        if (root == null || root.drawingData == null || root.drawingData.shapeData == null || root.drawingData.shapeData.Count == 0)
        {
            Debug.LogError("Invalid JSON structure.");
            return;
        }

        foreach (var shape in root.drawingData.shapeData)
        {
            if (shape.points == null || shape.points.Count < 2)
                continue;

            for (int i = 0; i < shape.points.Count; i++)
            {
                Debug.Log($"Shape: {shape.name}, Point {i}: ({shape.points[i].x}, {shape.points[i].y})");
            }
        }
    }
#endif
}
