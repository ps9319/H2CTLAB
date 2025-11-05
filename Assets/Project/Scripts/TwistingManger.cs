using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TwistingManger : MonoBehaviour
{
    public enum GroupingMode { Random, ByX, ByY, ByAngle }
    public enum SortDirection { Ascending, Descending }
    public enum GroupOrderMode { Normal, OutsideIn, InsideOut }

    [Tooltip("그룹화 기준")]
    public GroupingMode groupingMode = GroupingMode.Random;

    [Tooltip("정렬 방향 (ByX, ByY에서만 사용)")]
    public SortDirection sortDirection = SortDirection.Ascending;

    [Tooltip("그룹 순서 모드 (Normal: 정렬순, OutsideIn: 양 끝→중앙, InsideOut: 중앙→양 끝)")]
    public GroupOrderMode groupOrderMode = GroupOrderMode.Normal;

    [Tooltip("복제할 오브젝트 프리팹")]
    public GameObject prefab;

    [Tooltip("트위스팅 타겟 오브젝트 리스트")]
    public List<GameObject> targetObjects = new List<GameObject>();

    [Tooltip("복제 타이밍 간격(초)")]
    public float intervalTime = 0.5f;

    [Tooltip("전체 그룹 트위스팅에 걸리는 총 시간(초, 0이면 intervalTime 사용)")]
    public float totalDuration = 0f;

    [Header("그룹화 설정")]
    [Tooltip("랜덤 그룹: 한 그룹에 포함될 오브젝트 개수 (1 이상)")]
    [Min(1)]
    public int groupSize = 1;

    [Tooltip("X/Y 그룹: 그룹화 범위")]
    public float groupRange = 0.0001f;

    [Header("오프셋 설정")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
    public Vector3 scaleOffset = Vector3.one;

    [Tooltip("ByAngle: 시작 각도(도, 0=오른쪽, 90=위, 180=왼쪽, -90=아래)")]
    [Range(-180f, 180f)]
    public float angleStartDegree = 90f; // 기본값: 세로(위쪽)부터 시작

    private List<List<GameObject>> groupedObjects;
    private Quaternion cachedRotation;
    private Vector3 cachedScale;
    private WaitForSeconds cachedWait;

    private void OnEnable()
    {
        if (!ValidateInputs())
            return;

        CacheValues();
        GroupByAxis();
        StartCoroutine(TwistCoroutine());
    }

    private bool ValidateInputs()
    {
        return targetObjects != null && targetObjects.Count > 0 && prefab != null;
    }

    private void CacheValues()
    {
        cachedRotation = Quaternion.Euler(rotationOffset);
        cachedScale = Vector3.Scale(prefab.transform.localScale, scaleOffset);
        cachedWait = new WaitForSeconds(intervalTime);
    }

    private void GroupByAxis()
    {
        groupedObjects = new List<List<GameObject>>();
        var filteredObjects = targetObjects.Where(obj => obj != null).ToList();

        if (groupingMode == GroupingMode.Random)
        {
            // 랜덤 섞기
            for (int i = filteredObjects.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = filteredObjects[i];
                filteredObjects[i] = filteredObjects[j];
                filteredObjects[j] = temp;
            }

            // groupSize 단위로 그룹화
            for (int i = 0; i < filteredObjects.Count; i += groupSize)
            {
                var group = filteredObjects.Skip(i).Take(groupSize).ToList();
                groupedObjects.Add(group);
            }
        }
        else if (groupingMode == GroupingMode.ByAngle)
        {
            // 중심점 계산
            Vector3 center = Vector3.zero;
            foreach (var obj in filteredObjects)
                center += obj.transform.position;
            center /= filteredObjects.Count;

            float angleStartRad = angleStartDegree * Mathf.Deg2Rad;

            // 각도 계산 및 정렬 (시작 각도 보정)
            if (sortDirection == SortDirection.Ascending)
            {
                filteredObjects = filteredObjects
                    .OrderBy(obj =>
                    {
                        float angle = Mathf.Atan2(obj.transform.position.y - center.y, obj.transform.position.x - center.x);
                        float delta = Mathf.DeltaAngle(Mathf.Rad2Deg * angleStartRad, Mathf.Rad2Deg * angle);
                        return (delta + 360f) % 360f;
                    })
                    .ToList();
            }
            else
            {
                filteredObjects = filteredObjects
                    .OrderByDescending(obj =>
                    {
                        float angle = Mathf.Atan2(obj.transform.position.y - center.y, obj.transform.position.x - center.x);
                        float delta = Mathf.DeltaAngle(Mathf.Rad2Deg * angleStartRad, Mathf.Rad2Deg * angle);
                        return (delta + 360f) % 360f;
                    })
                    .ToList();
            }

            // groupRange로 그룹화 (각도 기준, 시작 각도 보정)
            foreach (var obj in filteredObjects)
            {
                float angle = Mathf.Atan2(obj.transform.position.y - center.y, obj.transform.position.x - center.x);
                float delta = Mathf.DeltaAngle(Mathf.Rad2Deg * angleStartRad, Mathf.Rad2Deg * angle);

                var matchingGroup = groupedObjects.FirstOrDefault(
                    group =>
                    {
                        float groupAngle = Mathf.Atan2(group[0].transform.position.y - center.y, group[0].transform.position.x - center.x);
                        float groupDelta = Mathf.DeltaAngle(Mathf.Rad2Deg * angleStartRad, Mathf.Rad2Deg * groupAngle);
                        return Mathf.Abs(delta - groupDelta) <= groupRange;
                    });

                if (matchingGroup != null)
                    matchingGroup.Add(obj);
                else
                    groupedObjects.Add(new List<GameObject> { obj });
            }
        }
        else
        {
            // ByY 또는 ByX 정렬 + 방향
            if (groupingMode == GroupingMode.ByY)
            {
                filteredObjects = sortDirection == SortDirection.Ascending
                    ? filteredObjects.OrderBy(obj => obj.transform.position.y).ToList()
                    : filteredObjects.OrderByDescending(obj => obj.transform.position.y).ToList();
            }
            else // ByX
            {
                filteredObjects = sortDirection == SortDirection.Ascending
                    ? filteredObjects.OrderBy(obj => obj.transform.position.x).ToList()
                    : filteredObjects.OrderByDescending(obj => obj.transform.position.x).ToList();
            }

            // 그룹핑 순서 결정
            List<GameObject> orderedObjects = new List<GameObject>();
            if (groupOrderMode == GroupOrderMode.Normal || groupingMode == GroupingMode.Random)
            {
                orderedObjects = filteredObjects;
            }
            else if (groupOrderMode == GroupOrderMode.OutsideIn)
            {
                // 양 끝에서 중앙으로
                int left = 0, right = filteredObjects.Count - 1;
                while (left <= right)
                {
                    if (left != right) {
                        orderedObjects.Add(filteredObjects[left]);
                        orderedObjects.Add(filteredObjects[right]);
                    } else {
                        orderedObjects.Add(filteredObjects[left]);
                    }
                    left++;
                    right--;
                }
            }
            else if (groupOrderMode == GroupOrderMode.InsideOut)
            {
                // 중앙에서 양 끝으로
                int mid = filteredObjects.Count / 2;
                int left = mid - 1, right = mid + (filteredObjects.Count % 2 == 0 ? 0 : 1);
                orderedObjects.Add(filteredObjects[mid]);
                while (left >= 0 || right < filteredObjects.Count)
                {
                    if (right < filteredObjects.Count) orderedObjects.Add(filteredObjects[right++]);
                    if (left >= 0) orderedObjects.Add(filteredObjects[left--]);
                }
            }

            // 그룹화 (Random은 groupSize, 나머지는 groupRange)
            if (groupingMode == GroupingMode.Random)
            {
                for (int i = 0; i < orderedObjects.Count; i += groupSize)
                    groupedObjects.Add(orderedObjects.Skip(i).Take(groupSize).ToList());
            }
            else if (groupingMode == GroupingMode.ByAngle)
            {
                // 중심점 계산
                Vector3 center = Vector3.zero;
                foreach (var obj in orderedObjects)
                    center += obj.transform.position;
                center /= orderedObjects.Count;

                float angleStartRad = angleStartDegree * Mathf.Deg2Rad;

                foreach (var obj in orderedObjects)
                {
                    float angle = Mathf.Atan2(obj.transform.position.y - center.y, obj.transform.position.x - center.x);
                    float delta = Mathf.DeltaAngle(Mathf.Rad2Deg * angleStartRad, Mathf.Rad2Deg * angle);

                    var matchingGroup = groupedObjects.FirstOrDefault(
                        group =>
                        {
                            float groupAngle = Mathf.Atan2(group[0].transform.position.y - center.y, group[0].transform.position.x - center.x);
                            float groupDelta = Mathf.DeltaAngle(Mathf.Rad2Deg * angleStartRad, Mathf.Rad2Deg * groupAngle);
                            return Mathf.Abs(delta - groupDelta) <= groupRange;
                        });

                    if (matchingGroup != null)
                        matchingGroup.Add(obj);
                    else
                        groupedObjects.Add(new List<GameObject> { obj });
                }
            }
            else
            {
                foreach (var obj in orderedObjects)
                {
                    float value = groupingMode == GroupingMode.ByY ? obj.transform.position.y : obj.transform.position.x;

                    var matchingGroup = groupedObjects.FirstOrDefault(
                        group => Mathf.Abs(
                            (groupingMode == GroupingMode.ByY ? group[0].transform.position.y : group[0].transform.position.x) - value
                        ) <= groupRange
                    );

                    if (matchingGroup != null)
                        matchingGroup.Add(obj);
                    else
                        groupedObjects.Add(new List<GameObject> { obj });
                }
            }
        }
    }

    private IEnumerator TwistCoroutine()
    {
        float interval = intervalTime;

        // 전체 duration이 0보다 크면 그룹 개수로 나눠서 interval 계산
        if (totalDuration > 0f && groupedObjects.Count > 0)
            interval = totalDuration / groupedObjects.Count;
        else if (groupOrderMode == GroupOrderMode.OutsideIn || groupOrderMode == GroupOrderMode.InsideOut)
            interval *= 0.5f;

        var wait = new WaitForSeconds(interval);

        foreach (var group in groupedObjects)
        {
            InstantiateGroup(group);
            yield return wait;
        }
    }

    private void InstantiateGroup(List<GameObject> group)
    {
        foreach (var obj in group)
        {
            if (obj == null) continue;

            var clone = Instantiate(
                prefab,
                obj.transform.position + positionOffset,
                cachedRotation,
                obj.transform
            );
            clone.transform.localScale = cachedScale;
        }
    }
}