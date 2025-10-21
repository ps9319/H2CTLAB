using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotalManager : MonoBehaviour
{
    [Serializable]
    public class SequenceGroup
    {
        public List<GameObject> targets = new List<GameObject>();
        public float startTime;
        public float endTime;
    }

    public List<SequenceGroup> sequenceGroups = new List<SequenceGroup>();

    private void Awake()
    {
        foreach (var group in sequenceGroups)
        {
            foreach (var obj in group.targets)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
    }

    private void Start()
    {
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        float elapsed = 0f;
        float maxTime = 0f;
        foreach (var group in sequenceGroups)
            if (group.endTime > maxTime) maxTime = group.endTime;

        while (elapsed < maxTime)
        {
            foreach (var group in sequenceGroups)
            {
                foreach (var obj in group.targets)
                {
                    if (obj == null) continue;
                    // 활성화
                    if (!obj.activeSelf && elapsed >= group.startTime && elapsed < group.endTime)
                        obj.SetActive(true);
                    // 비활성화
                    if (obj.activeSelf && elapsed >= group.endTime)
                        obj.SetActive(false);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 마지막으로 모두 비활성화
        foreach (var group in sequenceGroups)
            foreach (var obj in group.targets)
                if (obj != null)
                    obj.SetActive(false);
    }
}