using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerObjectPingPong : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveDistance = 5f;  // 이동 거리
    public float moveDuration = 2f;  // 이동하는데 걸리는 시간(초)
    
    [Header("대기 시간 설정")]
    public float waitTimeAtEnd = 1f;  // 끝에 도달했을 때 대기 시간(초)
    
    [Header("자동 시작")]
    public bool autoStart = true;  // 시작 시 자동으로 ping-pong 시작
    
    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool isMoving = false;
    
    void Start()
    {
        startPosition = transform.position;
        endPosition = startPosition + Vector3.right * moveDistance;
        
        if (autoStart)
        {
            StartPingPong();
        }
    }
    
    public void StartPingPong()
    {
        if (!isMoving)
        {
            StartCoroutine(PingPongMovement());
        }
    }
    
    public void StopPingPong()
    {
        StopAllCoroutines();
        isMoving = false;
    }
    
    private IEnumerator PingPongMovement()
    {
        isMoving = true;
        
        while (true)
        {
            // 시작 위치에서 끝 위치로 이동
            yield return StartCoroutine(MoveToPosition(endPosition, moveDuration));
            
            // 끝 위치에서 대기
            yield return new WaitForSeconds(waitTimeAtEnd);
            
            // 끝 위치에서 시작 위치로 이동
            yield return StartCoroutine(MoveToPosition(startPosition, moveDuration));
            
            // 시작 위치에서 대기
            yield return new WaitForSeconds(waitTimeAtEnd);
        }
    }
    
    private IEnumerator MoveToPosition(Vector3 targetPosition, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 부드러운 이동을 위한 Lerp 사용
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            
            yield return null;
        }
        
        // 정확한 위치로 설정
        transform.position = targetPosition;
    }
}
