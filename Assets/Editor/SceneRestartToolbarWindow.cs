using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRestartToolbarWindow
{
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
                // Debug.Log($"Reloaded scene 0: {SceneManager.GetSceneAt(0).name}");
            }
            else
            {
                Debug.LogWarning("No scenes in Build Settings.");
            }
        }
    }
}