using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using UnityEngine.VFX;

public class CustomPathFollower : MonoBehaviour
{
    public Transform pathParent; // 여러 PathCreator를 가진 부모 오브젝트
    public float speed = 5f;
    public float startDelay = 0f; // 시작 딜레이(초)
    public float delayBetweenPaths = 0f; // 경로 사이 딜레이(초)`

    [HideInInspector]
    public EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Stop;

    private List<PathCreator> paths = new List<PathCreator>();
    private int currentPathIndex = 0;
    private float distanceTravelled = 0f;
    private bool isWaiting = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void OnEnable()
    {
        // 원래 위치와 회전 저장
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        RefreshPaths();

        if (startDelay > 0f)
            StartCoroutine(StartWithDelay());
    }

    IEnumerator StartWithDelay()
    {
        isWaiting = true;
        yield return new WaitForSeconds(startDelay);
        isWaiting = false;
    }

    void Update()
    {
        if (isWaiting)
            return;

        if (paths.Count == 0)
        {
            Debug.LogWarning("No paths to follow!");
            return;
        }

        var path = paths[currentPathIndex].path;
        if (path == null)
        {
            Debug.LogWarning($"Path at index {currentPathIndex} is null!");
            return;
        }
        if (path.length == 0)
        {
            Debug.LogWarning($"Path at index {currentPathIndex} has zero length!");
            return;
        }

        distanceTravelled += speed * Time.deltaTime;
        transform.position = path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
        transform.rotation = path.GetRotationAtDistance(distanceTravelled, endOfPathInstruction);

        if (distanceTravelled >= path.length)
        {
            UnsubscribeFromPathUpdated();
            currentPathIndex++;
            if (currentPathIndex >= paths.Count)
            {
                // 모든 경로를 다 돌면 원래 위치와 회전으로 이동
                transform.position = originalPosition;
                transform.rotation = originalRotation;
                Debug.Log("All paths finished. Moved to original position.");

                enabled = false;
                return;
            }
            StartCoroutine(DelayAndMoveToNextPath());
        }
    }

    IEnumerator DelayAndMoveToNextPath()
    {
        isWaiting = true;
        yield return new WaitForSeconds(delayBetweenPaths);
        distanceTravelled = 0f;
        SubscribeToPathUpdated();
        isWaiting = false;
    }

    void SubscribeToPathUpdated()
    {
        if (paths.Count > currentPathIndex)
            paths[currentPathIndex].pathUpdated += OnPathChanged;
    }

    void UnsubscribeFromPathUpdated()
    {
        if (paths.Count > currentPathIndex)
            paths[currentPathIndex].pathUpdated -= OnPathChanged;
    }

    void OnPathChanged()
    {
        var path = paths[currentPathIndex].path;
        distanceTravelled = path.GetClosestDistanceAlongPath(transform.position);
    }

    void OnDisable()
    {
        UnsubscribeFromPathUpdated();
    }

    public void RefreshPaths()
    {
        paths.Clear();
        currentPathIndex = 0;
        distanceTravelled = 0f;

        if (pathParent == null)
            return;

        foreach (Transform child in pathParent)
        {
            var pc = child.GetComponent<PathCreator>();
            if (pc != null)
                paths.Add(pc);
        }
    }
}