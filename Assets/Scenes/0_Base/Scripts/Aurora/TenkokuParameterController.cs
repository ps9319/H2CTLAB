using UnityEngine;

[ExecuteInEditMode]
public class TenkokuParameterController : MonoBehaviour
{
    public Tenkoku.Core.TenkokuModule tenkokuObject;

    [Header("Time Settings")]
    [Range(0.0f, 2000.0f)]
    public float timeCompression = 0f;
    [Range(-90f, 90f)]
    public float latitude = 0f;

    [Header("Atmosphere Settings")]
    [Range(0f, 5f)]
    public float skyBrightness = 1f;
    [Range(0f, 1f)]
    public float nightBrightness = 0.5f;
    [Range(0f, 4f)]
    public float atmosphereDensity = 1f;

    [Header("Weather Settings")]
    [Range(0f, 1f)]
    public float altoStratus = 0f;
    [Range(0f, 1f)]
    public float cirrus = 0f;
    [Range(0f, 1f)]
    public float cumulus = 0f;
    [Range(0f, 1f)]
    public float overcast = 0f;
    [Range(0f, 1f)]
    public float humidity = 0f;
    [Range(0f, 1f)]
    public float rain = 0f;
    [Range(0f, 1f)]
    public float snow = 0f;
    [Range(0f, 1f)]
    public float windAmount = 0f;
    [Range(0f, 365f)]
    public float windDirection = 0f;
    [Range(0f, 1f)]
    public float lightning = 0f;
    [Range(0f, 1f)]
    public float rainbow = 0f;

    void Start()
    {
        tenkokuObject = FindObjectOfType<Tenkoku.Core.TenkokuModule>();
        UpdateAllParameters();
    }

    void Update()
    {
        if (tenkokuObject == null) return;
        UpdateAllParameters();
    }

    void UpdateAllParameters()
    {
        // Time Settings
        tenkokuObject.timeCompression = timeCompression;
        tenkokuObject.setLatitude = latitude;

        // Atmosphere Settings
        tenkokuObject.skyBrightness = skyBrightness;
        tenkokuObject.nightBrightness = nightBrightness;
        tenkokuObject.atmosphereDensity = atmosphereDensity;

        // Weather Settings
        tenkokuObject.weather_cloudAltoStratusAmt = altoStratus;
        tenkokuObject.weather_cloudCirrusAmt = cirrus;
        tenkokuObject.weather_cloudCumulusAmt = cumulus;
        tenkokuObject.weather_OvercastAmt = overcast;
        tenkokuObject.weather_humidity = humidity;
        tenkokuObject.weather_RainAmt = rain;
        tenkokuObject.weather_SnowAmt = snow;
        tenkokuObject.weather_WindAmt = windAmount;
        tenkokuObject.weather_cloudSpeed = windAmount;
        tenkokuObject.weather_WindDir = windDirection;
        tenkokuObject.weather_lightning = lightning;
        tenkokuObject.weather_rainbow = rainbow;

        // Additional Settings
        tenkokuObject.weather_lightningRange = 120.0f;
        tenkokuObject.volumeAmbDay = Mathf.Lerp(0.6f, -2.0f, overcast);
        tenkokuObject.autoTime = timeCompression > 0;
    }
}