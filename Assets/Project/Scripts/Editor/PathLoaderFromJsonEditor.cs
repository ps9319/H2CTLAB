using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathLoaderFromJson))]
public class PathLoaderFromJsonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PathLoaderFromJson loader = (PathLoaderFromJson)target;
        GUILayout.Space(10);

        if (GUILayout.Button("Create Paths (Editable in Editor)"))
        {
            loader.CreateEditablePathsFromJson();
        }

        if (GUILayout.Button("Clear Paths (Delete All Path Objects)"))
        {
            Undo.RegisterCompleteObjectUndo(loader.gameObject, "Clear Paths");
            loader.ClearAllPathsInEditor();
        }

        if (GUILayout.Button("Print Coordinates (Show in Console)"))
        {
            loader.PrintAllShapePointsInEditor();
        }
    }
}