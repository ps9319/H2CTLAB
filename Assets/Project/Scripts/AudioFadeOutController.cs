using System.Collections;
using System.Linq.Expressions;
using UnityEngine;

public class AudioFadeOutController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private MediaPlayerAddon mediaPlayerAddon;
    [SerializeField] private float FadeoutTime = 5f; 

    [SerializeField] private bool enableFadeIn = false;
    [SerializeField] private float FadeinTime = 5f; 


    private float initialVolume;
    private bool isFadingOut = false;
    private bool isFadingIn = false;

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
        if (audioSource == null) return;

        // enableFadeIn이 true이면 0에서 initialVolume까지 페이드인
        if (enableFadeIn && FadeinTime > 0f)
        {
            initialVolume = audioSource.volume;
            audioSource.volume = 0f;
            audioSource.Play();
            StartCoroutine(FadeInAudio());
        }
        else
        {
            audioSource.volume = initialVolume;
            audioSource.Play();
        }
    }

    void Update()
    {
        if(!mediaPlayerAddon.getIsPlaying()) return;

        if (!isFadingOut && mediaPlayerAddon != null && mediaPlayerAddon.IsLastLoop)
        {
            float videoDuration = mediaPlayerAddon.VideoDuration;
            // Debug.Log($"videoDuration: {videoDuration}");
            // Debug.Log($"FadeoutTime: {FadeoutTime}");
            float fadeOutStartTime = videoDuration - FadeoutTime;
            isFadingOut = true; // 코루틴 시작 전에 플래그 세움 
            // Debug.Log($"Delay: {mediaPlayerAddon.Delay}");
            StartCoroutine(FadeOutAudio(fadeOutStartTime));
        }
    }

    private IEnumerator FadeOutAudio(float delayTime)
    {
        isFadingOut = true;
        // Debug.Log($"delayTime:{delayTime}");

        if (delayTime > 0f)
        {
            yield return new WaitForSeconds(delayTime);
        }

        // Debug.Log($" 페이드아웃 시작");

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < FadeoutTime)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / FadeoutTime);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
    }

        private IEnumerator FadeInAudio()
    {
        if (audioSource == null) yield break;
        isFadingIn = true;

        if (FadeinTime <= 0f)
        {
            audioSource.volume = initialVolume;
            isFadingIn = false;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < FadeinTime)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, initialVolume, elapsed / FadeinTime);
            yield return null;
        }

        audioSource.volume = initialVolume;
        isFadingIn = false;
    }
}