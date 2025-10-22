using System.Collections;
using UnityEngine;

public class ImageEffect : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 1f;
    public float stayDuration = 2f;

    private Renderer[] allRenderers;
    private Material[] allMaterials;

    private void Awake()
    {
        allRenderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        if (allRenderers == null || allRenderers.Length == 0) return;

        allMaterials = new Material[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
        {
            allMaterials[i] = allRenderers[i].material;  // 인스턴스
            EnsureTransparent(allMaterials[i]);
        }
        
        StartCoroutine(FadeInOut());
    }

    private IEnumerator FadeInOut()
    {
        SetAlpha(0f);

        float t = 0f;
        while (t < fadeInDuration) { t += Time.deltaTime; SetAlpha(Mathf.Lerp(0f, 1f, t / fadeInDuration)); yield return null; }
        SetAlpha(1f);

        yield return new WaitForSeconds(stayDuration);

        t = 0f;
        while (t < fadeOutDuration) { t += Time.deltaTime; SetAlpha(Mathf.Lerp(1f, 0f, t / fadeOutDuration)); yield return null; }
        SetAlpha(0f);
    }

    private void SetAlpha(float a)
    {
        if (allMaterials == null) return;

        for (int i = 0; i < allMaterials.Length; i++)
        {
            if (allMaterials[i] == null) continue;

            if (allMaterials[i].HasProperty("_Color"))
            {
                var c = allMaterials[i].color; c.a = a; allMaterials[i].color = c;
            }
            if (allMaterials[i].HasProperty("_BaseColor"))
            {
                var bc = allMaterials[i].GetColor("_BaseColor"); bc.a = a; allMaterials[i].SetColor("_BaseColor", bc);
            }
        }
    }

    private static void EnsureTransparent(Material mat)
    {
        if (mat == null) return;

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);   // Alpha
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0);
            mat.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0);
            mat.renderQueue = 3000;
            return;
        }

        if (mat.shader != null && mat.shader.name.Contains("Standard"))
        {
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }
}