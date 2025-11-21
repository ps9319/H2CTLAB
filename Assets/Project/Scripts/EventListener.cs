using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Storage;
using UnityEngine.SceneManagement;

public class EventListener : MonoBehaviour
{
    #region Singleton
    private static EventListener instance;
    public static EventListener Instance => instance;
    #endregion

    #region Fields

    // 카테고리별 씬 매핑
    private Dictionary<string, string> categorySceneMap = new Dictionary<string, string>()
    {
        // 예술 Art
        { "dansaekhwa", "Art_dansaekhwa" },
        { "klimt", "Art_klimt" },
        { "monet", "Art_monet" },
        { "origami", "Art_origami" },
        { "pollock", "Art_pollok" },
        { "vangogh", "Art_vangogh" },
        // 문화유산 Culture
        { "baekja", "Culture_baekja" },
        { "jage", "Culture_jage" },
        { "jasu", "Culture_jasu" },
        { "kimhongdo", "Culture_kimhongdo" },
        { "sinyunbok", "Culture_sinyunbok"},
        { "traditional", "Culture_traditional" },
        // 자연 Natural
        { "autumn", "Nature_autumn" },
        { "camellia", "Nature_camelia" },
        { "canola", "Nature_canola" },
        { "cherry", "Nature_cherry" },
        { "hydragea", "Nature_hydragea" },
        { "winter", "Nature_winter" },
        // 풍류 Pungryu
        { "constellation", "Pungryu_constellation" },
        { "entertainment", "Pungryu_entertainment" },
        { "landscape", "Pungryu_landscape" },
        { "wave", "Pungryu_wave" },
        { "sumuk", "Pungryu_sumuk" },
        { "fantasy", "Pungryu_fantasy" }
    };

    private FirebaseStorage storage;
    private FirebaseFirestore db;
    private ListenerRegistration configListenerRegistration;
    private DocumentReference queueCountRef;

    private Queue<(int islandId, string sketchJson, string imagePath)> taskQueue = new Queue<(int, string, string)>();
    private int currentIslandId = -1;
    private string currentSketchJson = "";
    private string currentImagePath = "";
    private bool isScenePlaying = false;
    private Timestamp lastProcessedTimestamp = new Timestamp();

    private const string CONFIG_COLLECTION = "config";
    private const string CONFIG_DOCUMENT = "current_task";

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // 싱글톤 패턴 적용
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Firebase 초기화 및 리스너 등록
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                storage = FirebaseStorage.DefaultInstance;
                queueCountRef = db.Collection(CONFIG_COLLECTION).Document("tablet_config");
                ListenForConfigChanges();
                DeleteAllImagesInPersistentDataPath();
                UpdateQueueCount();
                StartCoroutine(ProcessQueue());
            }
        });
    }

    void Update()
    {
        // Q 키 입력 시 현재 이미지 삭제 및 씬 초기화
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!string.IsNullOrEmpty(currentImagePath) && File.Exists(currentImagePath))
            {
                try
                {
                    File.Delete(currentImagePath);
                    currentImagePath = "";
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Storage] 이미지 삭제 실패: {e.Message}");
                }
            }
            isScenePlaying = false;
            SceneManager.LoadScene(0);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        // SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        // SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        configListenerRegistration?.Dispose();
        if (instance == this)
            instance = null;
    }

    #endregion

    #region Firestore Listener

    /// <summary>
    /// Firestore에서 config 변경 감지 및 큐에 작업 추가
    /// </summary>
    private async void ListenForConfigChanges()
    {
        DocumentReference configRef = db.Collection(CONFIG_COLLECTION).Document(CONFIG_DOCUMENT);

        try
        {
            DocumentSnapshot initialSnapshot = await configRef.GetSnapshotAsync();
            if (initialSnapshot.Exists && initialSnapshot.TryGetValue("updated_at", out object timestampObj))
                lastProcessedTimestamp = (Timestamp)timestampObj;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Listen] 초기 timestamp 읽기 실패 : {e.Message}");
        }

        configListenerRegistration = configRef.Listen(async snapshot =>
        {
            if (!snapshot.Exists) return;

            if (snapshot.TryGetValue("updated_at", out object timestampObj) &&
                snapshot.TryGetValue("matched_island_id", out object islandIdObj) &&
                snapshot.TryGetValue("sketch_json", out object sketchJsonObj))
            {
                Timestamp currentTimestamp = (Timestamp)timestampObj;
                int matchedIslandId = (int)(long)islandIdObj;

                string sketchJson = sketchJsonObj is string str
                    ? str
                    : sketchJsonObj is Dictionary<string, object> dict
                        ? Newtonsoft.Json.JsonConvert.SerializeObject(dict)
                        : null;

                if (sketchJson == null) return;

                if (currentTimestamp.CompareTo(lastProcessedTimestamp) <= 0)
                    return;

                string imagePath = await DownloadIslandImageWithUniqueFileName(matchedIslandId, currentTimestamp);
                if (string.IsNullOrEmpty(imagePath)) return;

                taskQueue.Enqueue((matchedIslandId, sketchJson, imagePath));
                lastProcessedTimestamp = currentTimestamp;
                UpdateQueueCount();
            }
        });
    }

    #endregion

    #region Queue Processing

    /// <summary>
    /// 큐에 쌓인 작업을 순차적으로 처리
    /// </summary>
    IEnumerator ProcessQueue()
    {
        while (true)
        {
            if (SceneManager.GetActiveScene().name == "_StartScene" &&
                taskQueue.Count > 0 &&
                !isScenePlaying)
            {
                isScenePlaying = true;
                var (islandId, sketchJson, imagePath) = taskQueue.Peek();
                currentIslandId = islandId;
                currentSketchJson = sketchJson;
                currentImagePath = imagePath;
                yield return StartCoroutine(ProcessSingleTask(islandId, sketchJson, imagePath));
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// 단일 작업 처리 및 카테고리별 씬 로드
    /// </summary>
    private IEnumerator ProcessSingleTask(int islandId, string sketchJson, string imagePath)
    {
        string category = "wave";
        try
        {
            if (!string.IsNullOrEmpty(sketchJson))
            {
                JObject json = JObject.Parse(sketchJson);
                if (json?["drawingData"]?["category"] != null)
                    category = json["drawingData"]["category"].ToString();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Queue] JSON 파싱 실패: {e.Message}");
            Debug.LogError($"[Queue] 받은 JSON: {sketchJson}");
        }
        LoadSceneByCategory(category);
        yield break;
    }

    #endregion

    #region Storage & Firestore Update

    /// <summary>
    /// Firebase Storage에서 이미지 다운로드 (고유 파일명)
    /// </summary>
    private async Task<string> DownloadIslandImageWithUniqueFileName(int islandId, Timestamp timestamp)
    {
        try
        {
            string storagePath = "generated/latest_island.png";
            long uniqueId = timestamp.ToDateTime().Ticks;
            string fileName = $"island_{islandId}_{uniqueId}.png";
            string localPath = Path.Combine(Application.persistentDataPath, fileName);
            StorageReference storageRef = storage.GetReference(storagePath);
            await storageRef.GetFileAsync(localPath);
            return localPath;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Storage] 이미지 다운로드 실패: {e.Message}");
            return null;
        }
    }

    public void DeleteAllImagesInPersistentDataPath()
    {
        string path = Application.persistentDataPath;
        string[] imageExtensions = new[] { "*.png", "*.jpg", "*.jpeg" };

        foreach (var ext in imageExtensions)
        {
            foreach (var file in Directory.GetFiles(path, ext))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Storage] 파일 삭제 실패: {file} - {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Firestore에 큐 개수 업데이트
    /// </summary>
    private async void UpdateQueueCount()
    {
        if (queueCountRef == null) return;
        try
        {
            await queueCountRef.UpdateAsync("QUEUE_COUNT", taskQueue.Count);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firestore] QUEUE_COUNT 업데이트 실패: {e.Message}");
        }
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// 카테고리에 따라 씬 로드
    /// </summary>
    private void LoadSceneByCategory(string category)
    {
        string normalizedCategory = category.ToLower().Trim();
        if (categorySceneMap.TryGetValue(normalizedCategory, out string sceneToLoad))
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
        else
            SceneManager.LoadScene("DefaultScene", LoadSceneMode.Additive);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 _StartScene이 아닐 때 이미지 삭제
        // if (scene.name != "_StartScene")
        // {
        //     DeleteCurrentImage();
        // }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        // 씬이 _StartScene이 아닐 때 큐에서 작업 제거 및 상태 초기화
        if (scene.name != "_StartScene")
        {
            DeleteCurrentImage();
            isScenePlaying = false;
            if (taskQueue.Count > 0)
            {
                taskQueue.Dequeue();
                UpdateQueueCount();
            }
        }
    }

    #endregion

    #region Public Methods

    public string GetCurrentSketchJson() => currentSketchJson;
    public string GetCurrentImagePath() => currentImagePath;

    /// <summary>
    /// 현재 이미지 파일 삭제
    /// </summary>
    public void DeleteCurrentImage()
    {
        if (!string.IsNullOrEmpty(currentImagePath) && File.Exists(currentImagePath))
        {
            try
            {
                File.Delete(currentImagePath);
                currentImagePath = "";
            }
            catch (Exception e)
            {
                Debug.LogError($"[Storage] 이미지 삭제 실패: {e.Message}");
            }
        }
    }

    #endregion
}