using UnityEngine;

public class Madmapper : MonoBehaviour
{
    private static Madmapper instance;
    public static Madmapper Instance => instance;
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
