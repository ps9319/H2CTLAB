using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TextureSwitcher : MonoBehaviour
{
    public Renderer targetRenderer;
    public Texture2D[] textures;
    public bool loop = true;
    public bool randomOrder = true;

    [Header("Fade Effect")]
    public float fadeDuration = 0.3f;
    public bool fadeInOnEnable = true;
    public float fadeInDuration = 0.5f;

    [Header("Delay")]
    public float startDelay = 0f;

    [Header("Interval Range")]
    public float intervalMin = 1f;
    public float intervalMax = 2f;

    [Header("Add Color")]
    public Color addColor = Color.clear;

    int idx = 0;
    Coroutine runner;

    Material matA;
    Material matB;
    Material[] matsArr;

    WaitForSeconds startDelayWait;
    Color colorA;
    Color colorB;

    void Reset() => targetRenderer = GetComponent<Renderer>();

    void Awake()
    {
        if (startDelay > 0)
            startDelayWait = new WaitForSeconds(startDelay);
    }

    void OnEnable()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null || textures == null || textures.Length == 0) return;

        idx = Random.Range(0, textures.Length);
        InitializeMaterials();

        if (runner != null) StopCoroutine(runner);
        runner = StartCoroutine(Run());
    }

    void OnDisable()
    {
        if (runner != null) StopCoroutine(runner);
        runner = null;
    }

    void OnDestroy()
    {
        CleanupMaterials();
    }

    void InitializeMaterials()
    {
        CleanupMaterials();

        matA = new Material(targetRenderer.sharedMaterial);
        matB = new Material(targetRenderer.sharedMaterial);
        matsArr = new Material[2] { matA, matB };
        targetRenderer.materials = matsArr;

        colorA = matA.color;
        colorB = matB.color;

        if (fadeInOnEnable)
        {
            colorA.a = 0f;
            colorB.a = 0f;
        }
        else
        {
            colorA.a = 1f;
            colorB.a = 0f;
        }

        matA.color = colorA + addColor;
        matB.color = colorB + addColor;
    }

    void CleanupMaterials()
    {
        if (matA != null)
        {
            Destroy(matA);
            matA = null;
        }
        if (matB != null)
        {
            Destroy(matB);
            matB = null;
        }
        matsArr = null;
    }

    IEnumerator Run()
    {
        if (startDelay > 0)
            yield return startDelayWait;

        matA.mainTexture = textures[idx];

        if (fadeInOnEnable)
            yield return StartCoroutine(FadeIn());
        else
        {
            colorA.a = 1f;
            matA.color = colorA;
        }

        while (true)
        {
            float waitTime = Random.Range(intervalMin, intervalMax);
            yield return new WaitForSeconds(waitTime);

            if (randomOrder)
            {
                int prevIdx = idx;
                do
                {
                    idx = Random.Range(0, textures.Length);
                } while (textures.Length > 1 && idx == prevIdx);
            }
            else
            {
                idx++;
                if (idx >= textures.Length)
                {
                    if (loop) idx = 0;
                    else yield break;
                }
            }

            yield return StartCoroutine(FadeToTexture(textures[idx]));
        }
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeInDuration);

            colorA.a = alpha;
            matA.color = colorA + addColor;

            yield return null;
        }

        colorA.a = 1f;
        matA.color = colorA + addColor;
    }

    IEnumerator FadeToTexture(Texture2D nextTex)
    {
        if (matA == null || matB == null)
            InitializeMaterials();

        matB.mainTexture = nextTex;

        colorA.a = 1f;
        colorB.a = 0f;
        matA.color = colorA + addColor;
        matB.color = colorB + addColor;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);

            colorA.a = Mathf.Lerp(1f, 0f, alpha);
            colorB.a = Mathf.Lerp(0f, 1f, alpha);
            matA.color = colorA + addColor;
            matB.color = colorB + addColor;

            yield return null;
        }

        colorA.a = 0f;
        colorB.a = 1f;
        matA.color = colorA + addColor;
        matB.color = colorB + addColor;

        matA.mainTexture = nextTex;
        colorA.a = 1f;
        matA.color = colorA + addColor;
        colorB.a = 0f;
        matB.color = colorB + addColor;
        matB.mainTexture = null;
    }
}