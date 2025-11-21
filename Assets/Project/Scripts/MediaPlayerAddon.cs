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
    public float VideoDuration => mediaPlayer != null && mediaPlayer.Info != null 
        ? (float)mediaPlayer.Info.GetDuration() 
        : 0f;

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

    private void OnMediaPlayerEvent(MediaPlayer mp, MediaPlayerEvent.EventType eventType, ErrorCode errorCode)
    {
        if (eventType == MediaPlayerEvent.EventType.FinishedPlaying)
        {
            currentLoopCount++;
            // Debug.Log($"Loop Count : {currentLoopCount}");
        
            if (currentLoopCount >= repeatCount)
            {
                SceneManager.LoadScene(0);
            }
            else
            {
                mediaPlayer.Rewind(false);
                mediaPlayer.Play();
            }
        }
    }
}