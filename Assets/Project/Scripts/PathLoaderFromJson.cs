using System;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using Newtonsoft.Json.Linq;

public class PathLoaderFromJson : MonoBehaviour
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
    
    [Header("JSON File (LocalJsonFile 모드에서만 사용)")]
    [Tooltip("LocalJsonFile 모드일 때만 사용됩니다")]
    public TextAsset jsonFileAsset;

    [Header("Path Settings")]
    public bool closedPath = false;
    [Range(0f, 2f)] public float autoControlLength = 0.3f;
    public float scale = 1f;
    public Vector2 offset = Vector2.zero;

    [Header("Target Quad")]
    public Transform quadObject; // 드래그해서 넣을 Quad 오브젝트

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
    /// JSON 소스에서 데이터를 가져옵니다.
    /// </summary>
    private string GetJsonData()
    {
        switch (jsonSource)
        {
            case JsonSourceType.LocalJsonFile:
                if (jsonFileAsset == null)
                {
                    Debug.LogError("[PathLoaderFromJson] LocalJsonFile 모드: JSON 파일이 할당되지 않았습니다!");
                    return null;
                }
                Debug.Log("[PathLoaderFromJson] LocalJsonFile 모드로 JSON 처리");
                return jsonFileAsset.text;
                
            case JsonSourceType.FirebaseRealtime:
                if (EventListener.Instance == null)
                {
                    Debug.LogWarning("[PathLoaderFromJson] FirebaseRealtime 모드: EventListener 인스턴스를 찾을 수 없습니다.");
                    return null;
                }
                string firebaseJson = EventListener.Instance.GetCurrentSketchJson();
                if (string.IsNullOrEmpty(firebaseJson))
                {
                    Debug.LogWarning("[PathLoaderFromJson] FirebaseRealtime 모드: Firebase에서 JSON 데이터를 가져올 수 없습니다.");
                    return null;
                }
                Debug.Log("[PathLoaderFromJson] FirebaseRealtime 모드로 JSON 처리");
                return firebaseJson;
                
            default:
                return null;
        }
    }

    /// <summary>
    /// JSON 데이터를 파싱하여 Shape 리스트를 반환합니다.
    /// </summary>
    private List<Shape> ParseJsonData(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            if (jsonSource == JsonSourceType.LocalJsonFile)
            {
                // Unity JsonUtility 사용 (기존 방식)
                Root root = JsonUtility.FromJson<Root>(json);
                if (root == null || root.drawingData == null || root.drawingData.shapeData == null || root.drawingData.shapeData.Count == 0)
                {
                    Debug.LogError("[PathLoaderFromJson] LocalJsonFile: Invalid JSON structure.");
                    return null;
                }
                return root.drawingData.shapeData;
            }
            else // FirebaseRealtime
            {
                // Newtonsoft.Json 사용 (Firebase 방식)
                JObject jsonObj = JObject.Parse(json);
                JArray shapeDataArray = jsonObj["drawingData"]?["shapeData"] as JArray;

                if (shapeDataArray == null || shapeDataArray.Count == 0)
                {
                    Debug.LogError("[PathLoaderFromJson] FirebaseRealtime: shapeData를 찾을 수 없습니다.");
                    return null;
                }

                List<Shape> shapes = new List<Shape>();
                foreach (JObject shapeObj in shapeDataArray)
                {
                    Shape shape = new Shape
                    {
                        name = shapeObj["name"]?.ToString() ?? "Path",
                        points = new List<Point>()
                    };

                    JArray pointsArray = shapeObj["points"] as JArray;
                    if (pointsArray != null)
                    {
                        foreach (JObject pointObj in pointsArray)
                        {
                            Point point = new Point
                            {
                                x = pointObj["x"]?.ToObject<float>() ?? 0f,
                                y = pointObj["y"]?.ToObject<float>() ?? 0f
                            };
                            shape.points.Add(point);
                        }
                    }

                    shapes.Add(shape);
                }

                return shapes;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PathLoaderFromJson] JSON 파싱 실패: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// shape 좌표를 quad 기준으로 정규화하여 path를 생성합니다.
    /// useUndo: 에디터에서 Undo 지원 여부
    /// </summary>
    private void CreatePathsFromJson(bool useUndo)
    {
        // JSON 데이터 가져오기
        string json = GetJsonData();
        if (string.IsNullOrEmpty(json))
            return;

        // JSON 파싱
        List<Shape> shapes = ParseJsonData(json);
        if (shapes == null || shapes.Count == 0)
            return;

        // Remove existing child objects
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (useUndo)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
#endif
            }
            else
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        // 오브젝트 정보 가져오기
        Vector3 objSize = transform.localScale;
        Vector3 objPos = transform.position;

        foreach (var shape in shapes)
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
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Path");
#endif
            }
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

        Debug.Log($"[PathLoaderFromJson] {shapes.Count}개의 Shape로부터 Path 생성 완료 (모드: {jsonSource})");
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
        string json = GetJsonData();
        if (string.IsNullOrEmpty(json))
            return;

        List<Shape> shapes = ParseJsonData(json);
        if (shapes == null || shapes.Count == 0)
            return;

        foreach (var shape in shapes)
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
