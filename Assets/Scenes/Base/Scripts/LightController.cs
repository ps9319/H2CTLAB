using UnityEngine;

public class LightController : MonoBehaviour
{
    [SerializeField] private float targetIntensity = 20f;
    
    void OnTriggerEnter(Collider other)
    {
        LightState lightState = other.GetComponent<LightState>();
        
        if (lightState != null)
        {
            lightState.Toggle(targetIntensity);
        }
    }
}