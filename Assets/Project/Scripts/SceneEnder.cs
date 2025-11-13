using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEnder : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.LoadScene(0);
    }
}
