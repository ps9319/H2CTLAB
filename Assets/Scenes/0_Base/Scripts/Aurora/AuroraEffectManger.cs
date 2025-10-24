using System;
using System.Collections;
using UnityEngine;

public class AuroraEffectManger : MonoBehaviour
{
    [Header("Auto Play")]
    public bool autoPlay = true;

    [Header("Latitude Effect")]
    public bool latitudeEnabled = true;
    public float latitudeDelay = 0f;
    public float latitudeDuration = 2f;
    [Range(-90f, 90f)]
    public float latitudeStartValue = 0f;
    [Range(-90f, 90f)]
    public float latitudeEndValue = 45f;

    [Header("Rain Effect")]
    public bool rainEnabled = true;
    public float rainDelay = 1f;
    public float rainDuration = 3f;
    [Range(0f, 1f)]
    public float rainStartValue = 0f;
    [Range(0f, 1f)]
    public float rainEndValue = 0.8f;

    [Header("Lightning Effect")]
    public bool lightningEnabled = true;
    public float lightningDelay = 2f;
    public float lightningDuration = 1f;
    [Range(0f, 1f)]
    public float lightningStartValue = 0f;
    [Range(0f, 1f)]
    public float lightningEndValue = 1f;

    [Header("Time Compression Effect")]
    public bool timeCompressionEnabled = true;
    public float timeCompressionDelay = 0.5f;
    public float timeCompressionDuration = 4f;
    [Range(0.1f, 2000f)]
    public float timeCompressionStartValue = 1f;
    [Range(0.1f, 2000f)]
    public float timeCompressionEndValue = 10f;

    private TenkokuParameterController paramController;

    void Awake()
    {
        paramController = FindObjectOfType<TenkokuParameterController>();
        if (paramController == null)
        {
            Debug.LogWarning("[EffectManger] TenkokuParameterController not found in scene.");
        }
    }

    void Start()
    {
        if (autoPlay)
        {
            StartAll();
        }
        StartCoroutine(LogElapsedTime());

    }

    private const float LogInterval = 0.5f;
    
    // 경과된 시간을 로그로 출력하는 코루틴 함수
    IEnumerator LogElapsedTime()
    {
        // 무한 반복하여 0.5초마다 실행
        while (true)
        {
            // 씬 시작 후 경과된 시간 (float)을 가져옴
            float elapsedTime = Time.time; 
            
            // 경과된 시간을 소수점 둘째 자리까지 표시하여 로그 출력
            Debug.Log($"씬 시작 후 경과 시간: {elapsedTime:F2}초");

            // LogInterval(0.5초) 만큼 기다림
            yield return new WaitForSeconds(LogInterval);
        }
    }
    
    public void StartAll()
    {
        if (latitudeEnabled)
            StartCoroutine(RunLatitudeEffect());
        if (rainEnabled)
            StartCoroutine(RunRainEffect());
        if (lightningEnabled)
            StartCoroutine(RunLightningEffect());
        if (timeCompressionEnabled)
            StartCoroutine(RunTimeCompressionEffect());
    }

    public void StartLatitudeEffect()
    {
        if (latitudeEnabled)
            StartCoroutine(RunLatitudeEffect());
    }

    public void StartRainEffect()
    {
        if (rainEnabled)
            StartCoroutine(RunRainEffect());
    }

    public void StartLightningEffect()
    {
        if (lightningEnabled)
            StartCoroutine(RunLightningEffect());
    }

    public void StartTimeCompressionEffect()
    {
        if (timeCompressionEnabled)
            StartCoroutine(RunTimeCompressionEffect());
    }

    private IEnumerator RunLatitudeEffect()
    {
        if (paramController == null) yield break;

        if (latitudeDelay > 0f)
            yield return new WaitForSeconds(latitudeDelay);

        float t = 0f;
        while (t < latitudeDuration)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / Mathf.Max(0.00001f, latitudeDuration));
            float val = Mathf.Lerp(latitudeStartValue, latitudeEndValue, norm);
            paramController.latitude = val;
            yield return null;
        }

        paramController.latitude = latitudeEndValue;
    }

    private IEnumerator RunRainEffect()
    {
        if (paramController == null) yield break;

        if (rainDelay > 0f)
            yield return new WaitForSeconds(rainDelay);

        float t = 0f;
        while (t < rainDuration)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / Mathf.Max(0.00001f, rainDuration));
            float val = Mathf.Lerp(rainStartValue, rainEndValue, norm);
            paramController.rain = val;
            yield return null;
        }

        paramController.rain = rainEndValue;
    }

    private IEnumerator RunLightningEffect()
    {
        if (paramController == null) yield break;

        if (lightningDelay > 0f)
            yield return new WaitForSeconds(lightningDelay);

        float t = 0f;
        while (t < lightningDuration)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / Mathf.Max(0.00001f, lightningDuration));
            float val = Mathf.Lerp(lightningStartValue, lightningEndValue, norm);
            paramController.lightning = val;
            yield return null;
        }

        paramController.lightning = lightningEndValue;
    }

    private IEnumerator RunTimeCompressionEffect()
    {
        if (paramController == null) yield break;

        if (timeCompressionDelay > 0f)
            yield return new WaitForSeconds(timeCompressionDelay);

        float t = 0f;
        while (t < timeCompressionDuration)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / Mathf.Max(0.00001f, timeCompressionDuration));
            float val = Mathf.Lerp(timeCompressionStartValue, timeCompressionEndValue, norm);
            paramController.timeCompression = val;
            yield return null;
        }

        paramController.timeCompression = timeCompressionEndValue;
    }
}
