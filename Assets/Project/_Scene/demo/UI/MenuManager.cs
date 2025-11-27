using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

public class MenuManager : MonoBehaviour
{
    [Header("UI Control Target")]
    public GameObject entireUICanvas; // UI 전체를 포함하는 최상위 부모 (Hierarchy의 'UI')

    [Header("Main Category Panel")]
    public GameObject categoryPanel; 

    [Header("Sub Menu Panels")]
    public GameObject[] subMenus; 
    
    void Start()
    {
        if (entireUICanvas != null) 
        {
            entireUICanvas.SetActive(true); 
        }
        ShowCategoryPanel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Additive로 추가된 씬만 언로드, _StartScene은 유지
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != "_StartScene" && scene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 1. 카테고리(메인) 화면 보여주기 (테마 창에서 뒤로가기 누를 때)
    public void ShowCategoryPanel()
    {
        categoryPanel.SetActive(true);
        
        for (int i = 0; i < subMenus.Length; i++)
        {
            subMenus[i].SetActive(false);
        }
    }

    // 2. 서브 메뉴 열기 (카테고리 버튼 누를 때)
    public void OpenSubMenu(int index)
    {
        categoryPanel.SetActive(false); 

        for (int i = 0; i < subMenus.Length; i++)
        {
            subMenus[i].SetActive(false);
        }

        if (index >= 0 && index < subMenus.Length)
        {
            subMenus[index].SetActive(true);
        }
    }

    // 3. 씬 Additive 로드 + UI 숨기기 (각 테마 버튼에 연결)
    public void LoadThemeScene(string sceneName)
    {
        // 해당 이름의 씬을 기존 씬 위에 겹쳐서 로드
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }
    
    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        // 씬이 재생되는 동안 메뉴 UI 전체를 숨김
        if (entireUICanvas != null)
        {
            entireUICanvas.SetActive(false);
        }
    }

    private void OnSceneUnloaded(Scene arg0)
    {
        if (entireUICanvas != null)
        {
            entireUICanvas.SetActive(true);
        }

        ShowCategoryPanel();
    }
}