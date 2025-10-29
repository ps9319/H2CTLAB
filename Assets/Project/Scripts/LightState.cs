
using System.Collections;
using UnityEngine;

public class LightState : MonoBehaviour
{
    public bool isOn = false;
    
    [SerializeField] private float targetIntensity = 0f;  // 0으로 변경
    [SerializeField] private float transitionDuration = 1f;
    
    private Light _light;
    private Coroutine _currentFadeCoroutine;

    private void Awake()
    {
        _light = GetComponent<Light>();
        if (_light == null)
        {
            Debug.LogError($"Light component not found on {gameObject.name}");
        }
    }

    public void Toggle(float defaultIntensity)
    {
        isOn = !isOn;
        FadeToState(defaultIntensity);
    }

    public void TurnOn(float defaultIntensity)
    {
        isOn = true;
        FadeToState(defaultIntensity);
    }

    public void TurnOff(float defaultIntensity)
    {
        isOn = false;
        FadeToState(defaultIntensity);
    }

    private void FadeToState(float defaultIntensity)
    {
        if (_light == null) return;

        // 이미 실행중인 코루틴이 있으면 중단
        if (_currentFadeCoroutine != null)
        {
            StopCoroutine(_currentFadeCoroutine);
        }

        // targetIntensity가 0이면 defaultIntensity 사용, 아니면 자체 값 사용
        float actualTargetIntensity = targetIntensity > 0f ? targetIntensity : defaultIntensity;
        float endIntensity = isOn ? actualTargetIntensity : 0f;
        
        _currentFadeCoroutine = StartCoroutine(FadeLight(_light.intensity, endIntensity));
    }

    private IEnumerator FadeLight(float startIntensity, float endIntensity)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            _light.intensity = Mathf.Lerp(startIntensity, endIntensity, t);
            yield return null;
        }
        
        _light.intensity = endIntensity;
        _currentFadeCoroutine = null;
    }
}