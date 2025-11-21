using System.Collections;
using RenderHeads.Media.AVProVideo;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MediaPlayerAddon : MonoBehaviour
{
    private MediaPlayer mediaPlayer;
    [SerializeField] private float playDelay = 0f; // Inspector에서 설정 가능
    [SerializeField] private int repeatCount = 3;
    
    private int currentLoopCount = 0;

    public int CurrentLoopCount => currentLoopCount;
    public int RepeatCount => repeatCount == 1 ? 1 : repeatCount;
    public float Delay => playDelay;
    public bool IsLastLoop => currentLoopCount >= repeatCount - 1;

    // 동영상 길이를 반환하는 속성 추가
    public float VideoDuration => (float)mediaPlayer.Info.GetDuration();

    void Awake()
    {
        mediaPlayer = GetComponent<MediaPlayer>();
    }

    void Start()
    {
        StartCoroutine(PlayAfterDelay());
    }

    private IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(playDelay);
        mediaPlayer.Control.Play();
    }

    private void OnEnable()
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Events.AddListener(OnMediaPlayerEvent);
        }
    }

    private void OnDisable()
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Events.RemoveListener(OnMediaPlayerEvent);
        }
    }

    public bool getIsPlaying()
    {
       return mediaPlayer.Control.IsPlaying();
    }

    private void OnMediaPlayerEvent(MediaPlayer mp, MediaPlayerEvent.EventType eventType, ErrorCode errorCode)
    {
        if (eventType == MediaPlayerEvent.EventType.FinishedPlaying)
        {
            currentLoopCount++;
            // Debug.Log($"Loop Count : {currentLoopCount}");
        
            if (currentLoopCount >= repeatCount)
            {
                // 현재 활성 씬 중 _StartScene이 아닌 씬을 언로드
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.name != "_StartScene" && scene.isLoaded)
                    {
                        SceneManager.UnloadSceneAsync(scene);
                    }
                }
            }
            else
            {
                mediaPlayer.Rewind(false);
                mediaPlayer.Play();
            }
        }
    }
}