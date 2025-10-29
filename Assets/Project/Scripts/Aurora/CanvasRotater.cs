using System.Collections;
            using System.Collections.Generic;
            using UnityEngine;
            
            public class CanvasRotater : MonoBehaviour
            {
                [Header("Transform Settings")]
                [SerializeField] private Vector3 movePosition = Vector3.zero;
                [SerializeField] private Vector3 moveRotation = Vector3.zero;
            
                void Start()
                {
                    transform.localPosition += movePosition;
                    transform.localRotation *= Quaternion.Euler(moveRotation);
                } 
            }