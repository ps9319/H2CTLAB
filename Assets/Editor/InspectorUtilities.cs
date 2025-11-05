// 파일명: InspectorUtilities.cs
using UnityEditor;
using UnityEngine;
using System.Reflection;

public static class InspectorUtilities
{
    // ===============================
    //  인스펙터 잠금 토글 함수
    // ===============================
    public static void ToggleInspectorLockCore()
    {
        var asm = typeof(Editor).Assembly;
        var inspectorType = asm.GetType("UnityEditor.InspectorWindow");
        var inspectors = Resources.FindObjectsOfTypeAll(inspectorType);

        if (inspectors.Length == 0)
        {
            return;
        }

        foreach (var inspector in inspectors)
        {
            var isLockedProp = inspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (isLockedProp == null)
            {
                return;
            }

            bool current = (bool)isLockedProp.GetValue(inspector, null);
            isLockedProp.SetValue(inspector, !current, null);

            var repaintMethod = inspectorType.GetMethod("Repaint", BindingFlags.Instance | BindingFlags.Public);
            repaintMethod?.Invoke(inspector, null);
        }
    }

    // ===============================
    //  Ctrl + L : 인스펙터 잠금 토글
    // ===============================
    [MenuItem("Tools/Inspector Utilities/Toggle Inspector Lock _%l")]
    private static void ToggleInspectorLock()
    {
        ToggleInspectorLockCore();
    }

    // ===============================
    //  Ctrl + Numpad + : 인스펙터 잠금 토글
    // ===============================
    [MenuItem("Tools/Inspector Utilities/Toggle Inspector Lock 2 _%`")]
    private static void ToggleInspectorLockBacktick()
    {
        ToggleInspectorLockCore();
    }

    // ==========================================
    //  인스펙터 팝업 열기 함수
    // ==========================================
    public static void OpenInspectorPopupCore()
    {
        if (Selection.activeObject == null)
        {
            return;
        }

        var asm = typeof(Editor).Assembly;
        var inspectorType = asm.GetType("UnityEditor.InspectorWindow");
        if (inspectorType == null)
        {
            return;
        }

        var createInspectorMethod = inspectorType.GetMethod("CreateInspectorWindow", BindingFlags.Static | BindingFlags.NonPublic);
        EditorWindow newInspector = null;

        if (createInspectorMethod != null)
        {
            newInspector = (EditorWindow)createInspectorMethod.Invoke(null, null);
        }
        else
        {
            newInspector = ScriptableObject.CreateInstance(inspectorType) as EditorWindow;
            newInspector.Show(true);
        }

        if (newInspector != null)
        {
            newInspector.titleContent = new GUIContent($"Inspector - {Selection.activeObject.name}");
            newInspector.Show(true);
        }
    }

    // ==========================================
    //  Shift + P : 현재 선택 오브젝트 팝업으로 열기
    // ==========================================
    [MenuItem("Tools/Inspector Utilities/Open Inspector Popup _#p")]
    private static void OpenInspectorPopup()
    {
        OpenInspectorPopupCore();
    }
}
