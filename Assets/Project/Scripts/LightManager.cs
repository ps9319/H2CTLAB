using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LightGroup
{
    [Header("조명 그룹")]
    public List<Light> lights = new List<Light>();
    
    [Header("조명 설정")]
    public float targetIntensity = 20f;
    public float turnOnTime = 0f;
    public float transitionDuration = 1f;

    [Header("끄기 설정")]
    public bool turnOff = false;
    public float turnOffTime = 5f;
}

public class LightManager : MonoBehaviour
{
    [Header("전체 시작 딜레이(초)")]
    [SerializeField] private float globalStartDelay = 0f;

    [SerializeField] private List<LightGroup> lightGroups = new List<LightGroup>();
    
    private Dictionary<Light, Coroutine> _activeCoroutines = new Dictionary<Light, Coroutine>();

    private void OnEnable()
    {
        StartCoroutine(ActivateLightsWithDelay());
    }

    private IEnumerator ActivateLightsWithDelay()
    {
        if (globalStartDelay > 0f)
            yield return new WaitForSeconds(globalStartDelay);

        ActivateLights();
    }

    public void ActivateLights()
    {
        foreach (var group in lightGroups)
        {
            StartCoroutine(ProcessLightGroup(group));
        }
    }

    public void ActivateLightGroup(int groupIndex)
    {
        if (groupIndex >= 0 && groupIndex < lightGroups.Count)
        {
            StartCoroutine(ProcessLightGroup(lightGroups[groupIndex]));
        }
        else
        {
            Debug.LogWarning($"Light group index {groupIndex} out of range!");
        }
    }

    public void TurnOffAllLights(float duration = 1f)
    {
        foreach (var group in lightGroups)
        {
            foreach (var light in group.lights)
            {
                if (light != null)
                {
                    StartLightTransition(light, light.intensity, 0f, duration);
                }
            }
        }
    }

    public void TurnOffLightGroup(int groupIndex, float duration = 1f)
    {
        if (groupIndex >= 0 && groupIndex < lightGroups.Count)
        {
            var group = lightGroups[groupIndex];
            foreach (var light in group.lights)
            {
                if (light != null)
                {
                    StartLightTransition(light, light.intensity, 0f, duration);
                }
            }
        }
    }

    private IEnumerator ProcessLightGroup(LightGroup group)
    {
        if (group.turnOnTime > 0f)
        {
            yield return new WaitForSeconds(group.turnOnTime);
        }

        // 무조건 켜기
        foreach (var light in group.lights)
        {
            if (light == null) continue;

            float startIntensity = light.intensity;
            float endIntensity = group.targetIntensity;
            StartLightTransition(light, startIntensity, endIntensity, group.transitionDuration);
        }

        // 자동 끄기 처리
        if (group.turnOff)
        {
            yield return new WaitForSeconds(group.turnOffTime);
            
            foreach (var light in group.lights)
            {
                if (light != null)
                {
                    StartLightTransition(light, light.intensity, 0f, group.transitionDuration);
                }
            }
        }
    }

    private void StartLightTransition(Light light, float startIntensity, float endIntensity, float duration)
    {
        if (_activeCoroutines.ContainsKey(light) && _activeCoroutines[light] != null)
        {
            StopCoroutine(_activeCoroutines[light]);
        }

        Coroutine coroutine = StartCoroutine(FadeLight(light, startIntensity, endIntensity, duration));
        _activeCoroutines[light] = coroutine;
    }

    private IEnumerator FadeLight(Light light, float startIntensity, float endIntensity, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            if (light == null) yield break;
            
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            light.intensity = Mathf.Lerp(startIntensity, endIntensity, t);
            yield return null;
        }
        
        if (light != null)
        {
            light.intensity = endIntensity;
        }
        
        _activeCoroutines.Remove(light);
    }
}