using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManger : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Camera targetCamera;
    
    [Header("Camera Rotation Settings")]
    [SerializeField] private float delayTime = 2f;
    [SerializeField] private float startAngle = 0f;
    [SerializeField] private float endAngle = 45f;
    [SerializeField] private float effectDuration = 3f;

    private bool effectStarted = false;

    void Start()
    {
        if (targetCamera == null)
        {
            Debug.LogError("Target Camera가 할당되지 않았습니다!");
            return;
        }

        // 시작 각도로 초기화
        targetCamera.transform.rotation = Quaternion.Euler(startAngle, targetCamera.transform.rotation.eulerAngles.y, targetCamera.transform.rotation.eulerAngles.z);

        // 지연 후 효과 시작
        StartCoroutine(StartRotationEffect());
    }

    void Update()
    {

    }

    private IEnumerator StartRotationEffect()
    {
        if (targetCamera == null) yield break;

        // 지연 시간 대기
        yield return new WaitForSeconds(delayTime);

        effectStarted = true;
        float elapsedTime = 0f;

        while (elapsedTime < effectDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / effectDuration;

            // 부드러운 보간을 위해 Lerp 사용
            float currentAngle = Mathf.Lerp(startAngle, endAngle, progress);
            targetCamera.transform.rotation = Quaternion.Euler(currentAngle, targetCamera.transform.rotation.eulerAngles.y, targetCamera.transform.rotation.eulerAngles.z);

            yield return null;
        }

        // 최종 각도로 설정
        targetCamera.transform.rotation = Quaternion.Euler(endAngle, targetCamera.transform.rotation.eulerAngles.y, targetCamera.transform.rotation.eulerAngles.z);
    }
}