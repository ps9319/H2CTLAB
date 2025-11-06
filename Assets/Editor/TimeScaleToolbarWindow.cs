using UnityEditor;
using UnityEngine;

public class TimeScaleToolbarWindow
{
    // 단축키: Ctrl+Q → 2x <-> 1x 토글
    [MenuItem("Tools/TimeScale/2x Toggle %]")]
    private static void ToggleDoubleTimeScale()
    {
        if (Time.timeScale >= 2.0f)
        {
            Time.timeScale = 1.0f;
        }
        else
        {
            Time.timeScale = 2.0f;
        }
        Debug.Log($"Current TimeScale: {Time.timeScale:0.00}");
    }

    // 단축키: Ctrl+W → 1x <-> 0.5x 토글
    [MenuItem("Tools/TimeScale/0.5x Toggle %[")]
    private static void ToggleHalfTimeScale()
    {
        if (Time.timeScale <= 0.5f)
        {
            Time.timeScale = 1.0f;
        }
        else
        {
            Time.timeScale = 0.5f;
        }
        Debug.Log($"Current TimeScale: {Time.timeScale:0.00}");
    }
}