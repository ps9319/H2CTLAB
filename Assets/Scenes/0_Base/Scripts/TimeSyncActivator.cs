using System.Collections.Generic;
using UnityEngine;

public class TimeSyncActivator : MonoBehaviour
{
    [Tooltip("활성화할 오브젝트 리스트")]
    public List<GameObject> objectsToEnable;

    private void OnEnable()
    {
        if (objectsToEnable == null) return;
        foreach (var obj in objectsToEnable)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (objectsToEnable == null) return;
        foreach (var obj in objectsToEnable)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}