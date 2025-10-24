using UnityEngine;
using UnityEngine.VFX;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode] // 에디터에서도 실행
public class TextureArrayManager : MonoBehaviour
{
    public VisualEffect[] vfxArray;
    public Texture2D[] sourceTextures;

    private const string TextureArrayPropertyName = "particleTextureArr";
    private const string TexIndexPropertyName = "texIndex";
    private const string RangePropertyName = "range";
    private const string UseSetColorPropertyName = "useSetColor";
    private const string ThresholdPropertyName = "threshold";

    private Texture2DArray textureArray;

    void Awake()
    {
        CreateTextureArray();
    }

    void Start()
    {
        AssignTextureArrayToVFX();
    }

#if UNITY_EDITOR
    // 에디터에서 값이 바뀔 때마다 자동으로 할당
    void OnValidate()
    {
        CreateTextureArray();
        AssignTextureArrayToVFX();
    }
#endif

    private void CreateTextureArray()
    {
        if (sourceTextures == null || sourceTextures.Length == 0)
            return;

        int srcCount = sourceTextures.Length;
        int width = sourceTextures[0].width;
        int height = sourceTextures[0].height;
        TextureFormat format = TextureFormat.RGBA32;

        textureArray = new Texture2DArray(
            width,
            height,
            srcCount + 1,
            format,
            false,
            false
        );

        textureArray.filterMode = FilterMode.Bilinear;
        textureArray.wrapMode = TextureWrapMode.Clamp;

        for (int arrIdx = 0; arrIdx < srcCount + 1; arrIdx++)
        {
            Texture2D tex;
            if (arrIdx == 0)
            {
                tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color[] whitePixels = new Color[width * height];
                for (int j = 0; j < whitePixels.Length; j++)
                    whitePixels[j] = Color.white;
                tex.SetPixels(whitePixels);
                tex.Apply();
            }
            else
            {
                var src = sourceTextures[arrIdx - 1];
                if (src == null)
                    continue;

                RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
                Graphics.Blit(src, rt);

                RenderTexture prev = RenderTexture.active;
                try
                {
                    RenderTexture.active = rt;
                    tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();
                }
                finally
                {
                    // 이전 active 먼저 복원
                    RenderTexture.active = prev;
                    // 임시 RT가 아직 active라면 해제 전에 비움
                    if (RenderTexture.active == rt)
                    {
                        RenderTexture.active = null;
                    }
                    RenderTexture.ReleaseTemporary(rt);
                }
            }
            textureArray.SetPixels(tex.GetPixels(), arrIdx);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(tex);
            else
                Object.Destroy(tex);
#else
            Object.Destroy(tex);
#endif
        }
        textureArray.Apply(false, true);
    }

    private void AssignTextureArrayToVFX()
    {
        if (vfxArray == null || vfxArray.Length == 0 || textureArray == null)
            return;

        foreach (var vfx in vfxArray)
        {
            if (vfx != null)
                vfx.SetTexture(TextureArrayPropertyName, textureArray);
        }
    }

    void Update()
    {
        int depth = textureArray.depth;

        for (int i = 0; i < vfxArray.Length; i++)
        {
            var vfx = vfxArray[i];
            if (vfx == null) continue;

            float rangeValue = vfx.HasFloat(RangePropertyName) ? vfx.GetFloat(RangePropertyName) : 0f;
            float threshold = vfx.HasFloat(ThresholdPropertyName) ? vfx.GetFloat(ThresholdPropertyName) : 1f;
            int texIndex = 0;
            bool useSetColor = true;

            bool curveCondition = rangeValue >= threshold; // 추가

            if (curveCondition && depth > 1)
            {
                // 임계값 넘었을 때는 흰색(0번) 제외하고 랜덤
                texIndex = Random.Range(1, depth);
                useSetColor = false;
            }
            else
            {
                // 임계값 이하일 때만 흰색(0번)
                texIndex = 0;
                useSetColor = true;
            }
            vfx.SetInt(TexIndexPropertyName, texIndex);
            vfx.SetBool(UseSetColorPropertyName, useSetColor);

            vfx.SetBool("curveCondition", curveCondition); // 추가
        }
    }
}