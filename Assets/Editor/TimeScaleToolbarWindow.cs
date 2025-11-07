using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class TimeScaleToolbarWindow
{
    // 단축키: Ctrl+Q → 2x <-> 1x 토글
    [MenuItem("Tools/TimeScale/2x Toggle %q")]
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
    [MenuItem("Tools/TimeScale/0.5x Toggle %w")]
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

    // 단축키: Ctrl+e → 0번째 씬으로 재시작
    [MenuItem("Tools/Scene/Reload Scene 0 %e")]
    private static void ReloadScene0()
    {
        if (EditorApplication.isPlaying)
        {
            // 플레이 모드일 때: 런타임 씬 로드
            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                SceneManager.LoadScene(0);
                Debug.Log($"Reloaded scene 0: {SceneManager.GetSceneAt(0).name}");
            }
            else
            {
                Debug.LogWarning("No scenes in Build Settings.");
            }
        }
    }

}