using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SpawnGroup
{
    [Tooltip("생성할 오브젝트를 붙일 타겟 리스트")]
    public List<GameObject> targetObjects = new List<GameObject>();
    [Tooltip("이 그룹의 시작 시간")]
    public float startTime;
    [Tooltip("이 그룹의 종료 시간")]
    public float endTime;
}

public class SpawnObjectsOnTargets : MonoBehaviour
{
    [Tooltip("복제할 오브젝트 프리팹")]
    public GameObject prefab;

    [Tooltip("스폰 그룹 리스트")]
    public List<SpawnGroup> spawnGroups = new List<SpawnGroup>();

    private List<GameObject> spawnedObjects = new List<GameObject>();

    void OnEnable()
    {
        if (prefab == null || spawnGroups == null) return;
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        float maxEndTime = 0f;
        foreach (var group in spawnGroups)
        {
            if (group != null && group.endTime > maxEndTime)
                maxEndTime = group.endTime;
        }

        float timer = 0f;
        var spawned = new HashSet<SpawnGroup>();
        var despawned = new HashSet<SpawnGroup>();

        while (timer < maxEndTime)
        {
            foreach (var group in spawnGroups)
            {
                if (group == null) continue;

                // Spawn
                if (!spawned.Contains(group) && timer >= group.startTime)
                {
                    foreach (GameObject obj in group.targetObjects)
                    {
                        if (obj != null)
                        {
                            var instance = Instantiate(prefab, obj.transform.position, Quaternion.identity, obj.transform);
                            spawnedObjects.Add(instance);
                        }
                    }
                    spawned.Add(group);
                }
                // Despawn
                if (!despawned.Contains(group) && timer >= group.endTime)
                {
                    foreach (GameObject obj in group.targetObjects)
                    {
                        foreach (Transform child in obj.transform)
                        {
                            if (child != null && spawnedObjects.Contains(child.gameObject))
                            {
                                Destroy(child.gameObject);
                                spawnedObjects.Remove(child.gameObject);
                            }
                        }
                    }
                    despawned.Add(group);
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 혹시 끝까지 Despawn 안된 것 처리
        foreach (var group in spawnGroups)
        {
            if (group != null && !despawned.Contains(group))
            {
                foreach (GameObject obj in group.targetObjects)
                {
                    foreach (Transform child in obj.transform)
                    {
                        if (child != null && spawnedObjects.Contains(child.gameObject))
                        {
                            Destroy(child.gameObject);
                            spawnedObjects.Remove(child.gameObject);
                        }
                    }
                }
            }
        }
    }
}