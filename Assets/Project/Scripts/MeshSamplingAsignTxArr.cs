using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class MeshSamplingAsignTxArr : MonoBehaviour
{
    public Texture2DArray mainTextureArray;
    public Transform targetQuad;

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

        hasRangeProperty = vfx.HasFloat(RangePropertyName);
        hasThresholdProperty = vfx.HasFloat(ThresholdPropertyName);

        // TextureArray 할당
        AssignTextureArrayToVFX();
        UpdateTargetQuadSettings();

        // 흰색 텍스처 인덱스는 마지막
        if (mainTextureArray != null)
            whiteTextureIndex = mainTextureArray.depth - 1;
    }

    private void ResetCache()
    {
        prevRangeValue = float.MinValue;
        prevThreshold = float.MinValue;
        prevUseWhite = false;
    }

    private void AssignTextureArrayToVFX()
    {
        if (vfx != null && mainTextureArray != null)
        {
            vfx.SetTexture(TextureArrayPropertyName, mainTextureArray);
        }
    }

    private void UpdateTargetQuadSettings()
    {
        if (targetQuad == null) return;

        transform.position = targetQuad.position;

        if (vfx != null)
        {
            vfx.SetVector3(TargetQuadScalePropertyName, targetQuad.lossyScale);
        }
    }

    void Update()
    {
        if (vfx == null || mainTextureArray == null) return;

        float rangeValue = hasRangeProperty ? vfx.GetFloat(RangePropertyName) : 0f;
        float threshold = hasThresholdProperty ? vfx.GetFloat(ThresholdPropertyName) : 1f;

        // 흰색 사용 여부 결정
        bool useWhite = rangeValue < threshold || mainTextureArray.depth < 1;

        // 상태가 변경되었을 때만 처리
        bool stateChanged = useWhite != prevUseWhite ||
                           rangeValue != prevRangeValue ||
                           threshold != prevThreshold;

        if (!stateChanged && prevUseWhite) return;

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
            int texIndex = Random.Range(0, mainTextureArray.depth - 1); // 마지막은 흰색
            vfx.SetInt(TexIndexPropertyName, texIndex);
            vfx.SetBool(UseSetColorPropertyName, false);
        }

        prevUseWhite = useWhite;
        prevRangeValue = rangeValue;
        prevThreshold = threshold;
    }
}