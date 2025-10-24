using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainbowRainManger : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float startFallSpeed = 5f;
    [SerializeField] private float acceleration = 2f;
    [SerializeField] private float delayBeforeFall = 0f;

    private bool isStarted = false;
    private float currentSpeed = 0f;

    void Start()
    {
        StartCoroutine(StartFalling());
    }

    void Update()
    {
        if (isStarted)
        {
            currentSpeed += acceleration * Time.deltaTime;
            transform.position += Vector3.down * currentSpeed * Time.deltaTime;
        }
    }

    private IEnumerator StartFalling()
    {
        yield return new WaitForSeconds(delayBeforeFall);
        currentSpeed = startFallSpeed;
        isStarted = true;
    }
}