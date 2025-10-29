using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

[DisallowMultipleComponent]
public class VFXTextureSwitcher : MonoBehaviour
{
    public VisualEffect vfx;                       // 자동 할당하려면 비워두세요
    public Texture2D[] textures;                   // 바꿀 텍스처 목록 (list)
    public string textureProperty = "Texture";     // VFX에서 Exposed name (기본: "Texture")
    public float interval = 1f;                    // 교체 간격(초)
    public bool loop = true;                       // 목록 끝에서 반복할지
    public bool randomOrder = false;               // 랜덤 순서로 바꿀지

    int idx = 0;
    Coroutine runner;

    void Reset() => vfx = GetComponent<VisualEffect>();

    void OnEnable()
    {
        if (vfx == null) vfx = GetComponent<VisualEffect>();
        if (vfx == null || textures == null || textures.Length == 0) return;
        if (runner != null) StopCoroutine(runner);
        runner = StartCoroutine(Run());
    }

    void OnDisable()
    {
        if (runner != null) StopCoroutine(runner);
        runner = null;
    }

    IEnumerator Run()
    {
        if (randomOrder) idx = Random.Range(0, textures.Length);
        vfx.SetTexture(textureProperty, textures[idx]);

        while (true)
        {
            yield return new WaitForSeconds(interval);

            if (randomOrder)
            {
                idx = Random.Range(0, textures.Length);
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

            vfx.SetTexture(textureProperty, textures[idx]);
        }
    }
}