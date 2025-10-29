using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class VFXGroup
{
    public List<VFXAttachment> vfxList = new List<VFXAttachment>();
    public float startTime;
    public float endTime;
    public Transform targetTransform; // 추가: 타겟 오브젝트
    public bool enabled = true; // 추가: 그룹 활성/비활성화 (기본값 true)
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
            if (group == null || !group.enabled) continue; // 비활성화 그룹은 무시
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
        var finished = new HashSet<VFXGroup>();
        var disabled = new HashSet<VFXGroup>();

        while (timer < maxEndTime)
        {
            foreach (var group in vfxGroups)
            {
                if (group == null || !group.enabled) continue; // 비활성화 그룹은 무시

                // Play
                if (!played.Contains(group) && timer >= group.startTime)
                {
                    foreach (var vfx in group.vfxList)
                    {
                        if (vfx != null)
                        {
                            // 실행 직전에만 활성화
                            if (!vfx.gameObject.activeSelf)
                                vfx.gameObject.SetActive(true);

                            // targetTransform이 있으면 위치와 스케일을 맞춤
                            if (group.targetTransform != null)
                            {
                                vfx.transform.position = group.targetTransform.position;
                                vfx.transform.localScale = group.targetTransform.localScale;
                            }
                            vfx.Play();
                        }
                    }
                    played.Add(group);
                }
                // Finish
                if (!finished.Contains(group) && timer >= group.endTime)
                {
                    foreach (var vfx in group.vfxList)
                    {
                        if (vfx != null)
                            vfx.Finish();
                    }
                    finished.Add(group);
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

        // 혹시 끝까지 Finish/Disable 안된 것 처리
        foreach (var group in vfxGroups)
        {
            if (group != null && group.enabled)
            {
                if (!finished.Contains(group))
                {
                    foreach (var vfx in group.vfxList)
                    {
                        if (vfx != null)
                            vfx.Finish();
                    }
                }
                if (!disabled.Contains(group))
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
}