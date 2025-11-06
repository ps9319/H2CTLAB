using UnityEngine;
using UnityEngine.VFX;

public class ApplyParentVFXTexture : MonoBehaviour
{
    public string texturePropertyName = "Texture"; // 노출 프로퍼티 이름

    void Start()
    {
        var parent = transform.parent;
        if (parent == null) return;

        var parentVFX = parent.GetComponent<VisualEffect>();
        var myVFX = GetComponent<VisualEffect>();

        if (parentVFX != null && myVFX != null)
        {
            if (parentVFX.HasTexture(texturePropertyName))
            {
                Texture parentTexture = parentVFX.GetTexture(texturePropertyName);
                myVFX.SetTexture(texturePropertyName, parentTexture);
            }
        }
    }
}