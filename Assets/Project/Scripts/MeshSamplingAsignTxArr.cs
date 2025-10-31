using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class MeshSamplingAsignTxArr : MonoBehaviour
{
    public Texture2DArray mainTextureArray;
    public Transform targetQuad;

    private Texture2DArray combinedTextureArray;
    private VisualEffect vfx;

    // Property name constants
    private const string TextureArrayPropertyName = "particleTextureArr";
    private const string TexIndexPropertyName = "texIndex";
    private const string RangePropertyName = "range";
    private const string UseSetColorPropertyName = "useSetColor";
    private const string ThresholdPropertyName = "threshold";
    private const string TargetQuadScalePropertyName = "targetQuadScale";

    // Cache variables
    private float prevRangeValue = float.MinValue;
    private float prevThreshold = float.MinValue;
    private bool prevUseWhite = false;
    private int whiteTextureIndex;
    private bool hasRangeProperty;
    private bool hasThresholdProperty;

    void Awake()
    {
        Initialize();
    }

    void OnEnable()
    {
        Initialize();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Initialize();
        ResetCache();
    }
#endif

    private void Initialize()
    {
        if (vfx == null)
            vfx = GetComponent<VisualEffect>();

        if (vfx == null) return;

        // VFX 프로퍼티 존재 여부 캐싱
        hasRangeProperty = vfx.HasFloat(RangePropertyName);
        hasThresholdProperty = vfx.HasFloat(ThresholdPropertyName);

        CreateCombinedTextureArray();
        AssignTextureArrayToVFX();
        UpdateTargetQuadSettings();
    }

    private void ResetCache()
    {
        prevRangeValue = float.MinValue;
        prevThreshold = float.MinValue;
        prevUseWhite = false;
    }

    private void CreateCombinedTextureArray()
    {
        if (mainTextureArray == null) return;

        int width = mainTextureArray.width;
        int height = mainTextureArray.height;
        int depth = mainTextureArray.depth;
        bool hasMipmaps = mainTextureArray.mipmapCount > 1;

        // 기존 어레이 + 흰색 1장
        combinedTextureArray = new Texture2DArray(
            width,
            height,
            depth + 1,
            mainTextureArray.format,
            hasMipmaps
        );

        whiteTextureIndex = depth; // 마지막 인덱스 캐싱

        // 기존 텍스처 복사
        int mipmapCount = mainTextureArray.mipmapCount;
        for (int i = 0; i < depth; i++)
        {
            for (int mip = 0; mip < mipmapCount; mip++)
            {
                Graphics.CopyTexture(mainTextureArray, i, mip, combinedTextureArray, i, mip);
            }
        }

        // 흰색 텍스처 생성 및 추가
        CreateAndAddWhiteTexture(width, height, hasMipmaps, mipmapCount);

        combinedTextureArray.Apply();
    }

    private void CreateAndAddWhiteTexture(int width, int height, bool hasMipmaps, int mipmapCount)
    {
        Texture2D whiteTex = new Texture2D(width, height, mainTextureArray.format, hasMipmaps);
        
        // 픽셀 배열 생성 및 설정
        int pixelCount = width * height;
        Color[] whitePixels = new Color[pixelCount];
        for (int i = 0; i < pixelCount; i++)
            whitePixels[i] = Color.white;
        
        whiteTex.SetPixels(whitePixels);
        whiteTex.Apply();

        // 텍스처 복사
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            Graphics.CopyTexture(whiteTex, 0, mip, combinedTextureArray, whiteTextureIndex, mip);
        }

#if UNITY_EDITOR
        DestroyImmediate(whiteTex);
#else
        Destroy(whiteTex);
#endif
    }

    private void AssignTextureArrayToVFX()
    {
        if (vfx != null && combinedTextureArray != null)
        {
            vfx.SetTexture(TextureArrayPropertyName, combinedTextureArray);
        }
    }

    private void UpdateTargetQuadSettings()
    {
        if (targetQuad == null) return;

        // 위치 동기화
        transform.position = targetQuad.position;

        // 스케일 전달
        if (vfx != null)
        {
            vfx.SetVector3(TargetQuadScalePropertyName, targetQuad.lossyScale);
        }
    }

    void Update()
    {
        if (vfx == null || combinedTextureArray == null) return;

        float rangeValue = hasRangeProperty ? vfx.GetFloat(RangePropertyName) : 0f;
        float threshold = hasThresholdProperty ? vfx.GetFloat(ThresholdPropertyName) : 1f;

        // 흰색 사용 여부 결정
        bool useWhite = rangeValue < threshold || mainTextureArray == null || mainTextureArray.depth < 1;

        // 상태가 변경되었을 때만 처리
        bool stateChanged = useWhite != prevUseWhite || 
                           rangeValue != prevRangeValue || 
                           threshold != prevThreshold;

        if (!stateChanged && prevUseWhite) return; // 흰색 상태가 계속되면 스킵

        if (useWhite)
        {
            if (stateChanged)
            {
                vfx.SetInt(TexIndexPropertyName, whiteTextureIndex);
                vfx.SetBool(UseSetColorPropertyName, true);
            }
        }
        else
        {
            // 임계값을 넘으면 매 프레임 랜덤 인덱스
            int texIndex = Random.Range(0, mainTextureArray.depth);
            vfx.SetInt(TexIndexPropertyName, texIndex);
            vfx.SetBool(UseSetColorPropertyName, false);
        }

        // 캐시 업데이트
        prevUseWhite = useWhite;
        prevRangeValue = rangeValue;
        prevThreshold = threshold;
    }
}