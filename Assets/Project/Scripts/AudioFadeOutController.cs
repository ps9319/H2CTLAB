using System.Collections;
using UnityEngine;

public class AudioFadeOutController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private MediaPlayerAddon mediaPlayerAddon;
    [SerializeField] private float fadeOutDuration = 2f;
    [SerializeField] private float fadeOutBeforeVideoEnd = 5f; // 동영상 끝나기 몇 초 전에 페이드 아웃 시작

    private float initialVolume;
    private bool isFadingOut = false;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        initialVolume = audioSource.volume;
    }

    void Start()
    {
        audioSource.Play();
    }

    void Update()
    {
        if (!isFadingOut && mediaPlayerAddon != null && mediaPlayerAddon.IsLastLoop)
        {
            // 동영상 길이를 가져와서 페이드 아웃 시작 시간 계산
            float videoDuration = mediaPlayerAddon.VideoDuration;
            float fadeOutStartTime = videoDuration - fadeOutBeforeVideoEnd;

            // 현재 재생 시간이 페이드 아웃 시작 시간을 넘었는지 확인
            if (fadeOutStartTime > 0 && Time.time >= fadeOutStartTime)
            {
                StartCoroutine(FadeOutAudio());
            }
        }
    }

    private IEnumerator FadeOutAudio()
    {
        isFadingOut = true;

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
    }
}