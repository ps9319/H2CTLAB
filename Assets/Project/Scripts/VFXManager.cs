using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class VFXGroup
{
    public List<VisualEffect> vfxList = new List<VisualEffect>();
    public float startTime;
    public float endTime;
    public bool enabled = true;
}

public class VFXManager : MonoBehaviour
{
    [Header("VFX Groups")]
    public List<VFXGroup> vfxGroups = new List<VFXGroup>();

    [Header("Delay Settings")]
    public float startDelay = 0f;

    private void OnEnable()
    {
        PlayAll();
    }

    private void OnDisable()
    {
        foreach (var group in vfxGroups)
        {
            if (group == null || !group.enabled) continue;
            foreach (var vfx in group.vfxList)
            {
                if (vfx != null && vfx.gameObject.activeSelf)
                    vfx.gameObject.SetActive(false);
            }
        }
    }

    public void PlayAll()
    {
        StopAllCoroutines();
        StartCoroutine(PlayAllRoutine());
    }

    private IEnumerator PlayAllRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        float maxEndTime = 0f;
        foreach (var group in vfxGroups)
        {
            if (group != null && group.enabled && group.endTime > maxEndTime)
                maxEndTime = group.endTime;
        }

        float timer = 0f;
        var played = new HashSet<VFXGroup>();
        var disabled = new HashSet<VFXGroup>();

        while (timer < maxEndTime)
        {
            foreach (var group in vfxGroups)
            {
                if (group == null || !group.enabled) continue;

                // Enable
                if (!played.Contains(group) && timer >= group.startTime)
                {
                    foreach (var vfx in group.vfxList)
                    {
                        if (vfx != null && !vfx.gameObject.activeSelf)
                            vfx.gameObject.SetActive(true);
                    }
                    played.Add(group);
                }
                // Disable after endTime
                if (!disabled.Contains(group) && timer >= group.endTime)
                {
                    foreach (var vfx in group.vfxList)
                    {
                        if (vfx != null && vfx.gameObject.activeSelf)
                            vfx.gameObject.SetActive(false);
                    }
                    disabled.Add(group);
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 혹시 끝까지 Disable 안된 것 처리
        foreach (var group in vfxGroups)
        {
            if (group != null && group.enabled && !disabled.Contains(group))
            {
                foreach (var vfx in group.vfxList)
                {
                    if (vfx != null && vfx.gameObject.activeSelf)
                        vfx.gameObject.SetActive(false);
                }
            }
        }
    }
}