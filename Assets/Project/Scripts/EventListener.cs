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
    // 클래스 상단에 필드 추가
    private FirebaseStorage storage;
    private int currentIslandId = -1;
    private string currentSketchJson = "";
    
    // --- 싱글톤 인스턴스 ---
    private static EventListener instance;
    public static EventListener Instance => instance;

    // --- 큐 관리 변수 (Island ID + JSON + 이미지 경로) ---
    private Queue<(int islandId, string sketchJson, string imagePath)> taskQueue = new Queue<(int, string, string)>();
    private bool isScenePlaying = false;
    private string currentImagePath = ""; // 현재 재생 중인 씬의 이미지 경로

    // --- Firebase 설정 ---
    private FirebaseFirestore db;
    private ListenerRegistration configListenerRegistration;
    private const string CONFIG_COLLECTION = "config";
    private const string CONFIG_DOCUMENT = "current_task";
    private DocumentReference queueCountRef;
    
    // --- 중복 방지를 위한 마지막 처리 timestamp ---
    private Timestamp lastProcessedTimestamp = new Timestamp();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[EventListener] 싱글톤 생성 및 DontDestroyOnLoad 적용");
    }

    void Update()
    {
        // 🔥 테스트용: Q 키로 0번 씬(_StartScene)으로 복귀
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("[Test] Q 키 입력 감지 - 0번 씬으로 복귀");
        
            // 현재 재생 중인 이미지 삭제
            if (!string.IsNullOrEmpty(currentImagePath) && File.Exists(currentImagePath))
            {
                try
                {
                    File.Delete(currentImagePath);
                    Debug.Log($"[Storage] 강제 복귀 시 이미지 삭제: {currentImagePath}");
                    currentImagePath = "";
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Storage] 이미지 삭제 실패: {e.Message}");
                }
            }
        
            isScenePlaying = false;
            SceneManager.LoadScene(0);
        }
        
        // T 누르면 다음 씬으로 (빌드 설정에 등록된 씬 기준)
        // TODO 삭제 필요
        if (Input.GetKeyDown(KeyCode.T))
        {
            int current = SceneManager.GetActiveScene().buildIndex;
            int count = SceneManager.sceneCountInBuildSettings;
            int next = (current + 1) % Mathf.Max(1, count); // 안전 처리
            Debug.Log($"[Scene] T pressed: loading scene index {next}");
            SceneManager.LoadScene(next);
        }
    }

    void Start()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                storage = FirebaseStorage.DefaultInstance;
                queueCountRef = db.Collection(CONFIG_COLLECTION).Document("tablet_config");
                Debug.Log("[EventListener] Firebase Firestore 초기화 성공");

                ListenForConfigChanges();
                StartCoroutine(ProcessQueue());
            }
            else
            {
                Debug.LogError($"[EventListener] Firebase 종속성 문제: {dependencyStatus}");
            }
        });
    }

    private void OnDestroy()
    {
        if (configListenerRegistration != null)
        {
            configListenerRegistration.Dispose();
            Debug.Log("[EventListener] Firestore Listener 해제");
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    private async void ListenForConfigChanges()
{
    DocumentReference configRef = db.Collection(CONFIG_COLLECTION).Document(CONFIG_DOCUMENT);

    // 🔥 리스너 등록 전에 현재 timestamp를 먼저 읽어서 초기화
    try
    {
        DocumentSnapshot initialSnapshot = await configRef.GetSnapshotAsync();
        
        if (initialSnapshot.Exists && initialSnapshot.TryGetValue("updated_at", out object timestampObj))
        {
            lastProcessedTimestamp = (Timestamp)timestampObj;
            Debug.Log($"[Listen] 초기 timestamp 설정: {lastProcessedTimestamp.ToDateTime():yyyy-MM-dd HH:mm:ss}");
        }
        else
        {
            Debug.LogWarning("[Listen] 초기 문서를 찾을 수 없습니다.");
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[Listen] 초기 timestamp 읽기 실패: {e.Message}");
    }

    // 이제 리스너 등록
    configListenerRegistration = configRef.Listen(async snapshot =>
    {
        if (!snapshot.Exists)
        {
            Debug.LogWarning("[Listen] config/current_task 문서가 존재하지 않습니다.");
            return;
        }

        if (snapshot.TryGetValue("updated_at", out object timestampObj) &&
            snapshot.TryGetValue("matched_island_id", out object islandIdObj) &&
            snapshot.TryGetValue("sketch_json", out object sketchJsonObj))
        {
            Timestamp currentTimestamp = (Timestamp)timestampObj;
            int matchedIslandId = (int)(long)islandIdObj;

            string sketchJson;
            if (sketchJsonObj is string str)
            {
                sketchJson = str;
            }
            else if (sketchJsonObj is Dictionary<string, object> dict)
            {
                sketchJson = Newtonsoft.Json.JsonConvert.SerializeObject(dict);
            }
            else
            {
                Debug.LogError($"[Listen] sketch_json 타입 오류: {sketchJsonObj.GetType()}");
                return;
            }

            Debug.Log($"[Listen] 변환된 JSON: {sketchJson}");

            // 🔥 timestamp 비교로 중복 방지
            if (currentTimestamp.CompareTo(lastProcessedTimestamp) <= 0)
            {
                Debug.Log($"[Listen] 이미 처리된 요청 (current: {currentTimestamp.ToDateTime():HH:mm:ss}, last: {lastProcessedTimestamp.ToDateTime():HH:mm:ss}). 건너뜀.");
                return;
            }

            // 🔥 이미지를 큐에 넣을 때 다운로드
            string imagePath = await DownloadIslandImageWithUniqueFileName(matchedIslandId, currentTimestamp);

            if (string.IsNullOrEmpty(imagePath))
            {
                Debug.LogError("[Listen] 이미지 다운로드 실패. 큐에 추가하지 않음.");
                return;
            }

            taskQueue.Enqueue((matchedIslandId, sketchJson, imagePath));
            lastProcessedTimestamp = currentTimestamp;
            Debug.Log($"[Queue] Island ID {matchedIslandId} + JSON + 이미지({imagePath}) 추가. 큐 크기: {taskQueue.Count}");
            
            UpdateQueueCount(); // 🔥 Enqueue 시 Firestore 동기화
        }
        else
        {
            Debug.LogWarning("[Listen] config 문서에 필수 필드가 없습니다.");
        }
    });

    Debug.Log("[EventListener] Firestore Listen 시작");
}

    IEnumerator ProcessQueue()
    {
        while (true)
        {
            if (SceneManager.GetActiveScene().name == "_StartScene" &&
                taskQueue.Count > 0 &&
                !isScenePlaying)
            {
                isScenePlaying = true;
                var (islandId, sketchJson, imagePath) = taskQueue.Dequeue();
                
                UpdateQueueCount(); // 🔥 Dequeue 시 Firestore 동기화
                
                
                currentIslandId = islandId;
                currentSketchJson = sketchJson;
                currentImagePath = imagePath; // 현재 이미지 경로 저장

                // 별도 코루틴으로 처리
                yield return StartCoroutine(ProcessSingleTask(islandId, sketchJson, imagePath));
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
    
    private IEnumerator ProcessSingleTask(int islandId, string sketchJson, string imagePath)
    {
        string category = "wave"; // 기본값

        try
        {
            if (!string.IsNullOrEmpty(sketchJson))
            {
                JObject json = JObject.Parse(sketchJson);

                if (json?["drawingData"]?["category"] != null)
                {
                    category = json["drawingData"]["category"].ToString();
                    Debug.Log($"[Queue] 파싱된 Category: {category}");
                }
                else
                {
                    Debug.LogWarning("[Queue] category 경로를 찾을 수 없음. 기본값(wave) 사용.");
                }
            }

            Debug.Log($"[Queue] Island ID {islandId}, Category: {category}, 이미지: {imagePath} 처리 시작.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Queue] JSON 파싱 실패: {e.Message}");
            Debug.LogError($"[Queue] 받은 JSON: {sketchJson}");
        }

        // 이미지는 이미 다운로드되어 있으므로 바로 씬 로드
        LoadSceneByCategory(category);
        yield break;
    }
    
    // 🔥 고유 파일명으로 이미지 다운로드 (updated_at timestamp 사용)
    private async Task<string> DownloadIslandImageWithUniqueFileName(int islandId, Timestamp timestamp)
    {
        try
        {
            // Firebase Storage 경로
            string storagePath = "generated/latest_island.png";
        
            // 고유한 로컬 파일명 생성 (timestamp를 밀리초로 변환)
            long uniqueId = timestamp.ToDateTime().Ticks;
            string fileName = $"island_{islandId}_{uniqueId}.png";
            string localPath = Path.Combine(Application.persistentDataPath, fileName);

            StorageReference storageRef = storage.GetReference(storagePath);
        
            Debug.Log($"[Storage] 다운로드 시작: {storagePath} → {localPath}");

            await storageRef.GetFileAsync(localPath);

            Debug.Log($"[Storage] 다운로드 완료: {localPath}");
            return localPath;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Storage] 이미지 다운로드 실패: {e.Message}");
            return null;
        }
    }
    
    // 큐 크기를 Firestore에 업데이트하는 메서드
    private async void UpdateQueueCount()
    {
        if (queueCountRef == null) return;

        try
        {
            await queueCountRef.UpdateAsync("QUEUE_COUNT", taskQueue.Count);
            Debug.Log($"[Firestore] QUEUE_COUNT 업데이트: {taskQueue.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firestore] QUEUE_COUNT 업데이트 실패: {e.Message}");
        }
    }
    
    // 기존 DownloadIslandImage 메서드는 제거 가능
    
    private void LoadSceneByCategory(string category)
    {
        string normalizedCategory = category.ToLower().Trim();
    
        if (categorySceneMap.TryGetValue(normalizedCategory, out string sceneToLoad))
        {
            Debug.Log($"[Scene] Category '{category}' → {sceneToLoad} 로드");
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning($"[Scene] Category '{category}'는 정의되지 않음. DefaultScene 로드.");
            SceneManager.LoadScene("DefaultScene");
        }
    }
    
    public string GetCurrentSketchJson()
    {
        return currentSketchJson;
    }
    
    // 🔥 현재 이미지 경로를 반환하는 메서드 추가
    public string GetCurrentImagePath()
    {
        return currentImagePath;
    }
    
    // 🔥 이미지 삭제 메서드 추가
    public void DeleteCurrentImage()
    {
        if (!string.IsNullOrEmpty(currentImagePath) && File.Exists(currentImagePath))
        {
            try
            {
                File.Delete(currentImagePath);
                Debug.Log($"[Storage] 이미지 삭제 완료: {currentImagePath}");
                currentImagePath = "";
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Storage] 이미지 삭제 실패: {e.Message}");
            }
        }
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "_StartScene")
        {
            // 🔥 씬 재생 완료 후 이미지 삭제
            if (!string.IsNullOrEmpty(currentImagePath) && File.Exists(currentImagePath))
            {
                try
                {
                    File.Delete(currentImagePath);
                    Debug.Log($"[Storage] 사용 완료된 이미지 삭제: {currentImagePath}");
                    currentImagePath = ""; // 경로 초기화
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Storage] 이미지 삭제 실패: {e.Message}");
                }
            }

            isScenePlaying = false;
            Debug.Log("[Scene] TestScene 복귀 - 다음 작업 대기 중");
        }
    }

}