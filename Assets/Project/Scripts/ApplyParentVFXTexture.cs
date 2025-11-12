using UnityEngine;
using UnityEngine.VFX;

public class ApplyParentVFXTexture : MonoBehaviour
{
    public enum SourceMode
    {
        Parent,
        AssignedObject
    }

    [Header("Source Settings")]
    public SourceMode sourceMode = SourceMode.Parent;
    public VisualEffect sourceVFX; // AssignedObject 모드일 때 사용

    public string texturePropertyName = "Texture"; // 노출 프로퍼티 이름

    void OnEnable()
    { 
        VisualEffect src = null;

        if (sourceMode == SourceMode.Parent)
        {
            var parent = transform.parent;
            if (parent != null)
                src = parent.GetComponent<VisualEffect>();
        }
        else if (sourceMode == SourceMode.AssignedObject)
        {
            src = sourceVFX;
        }

        var myVFX = GetComponent<VisualEffect>();

        if (src != null && myVFX != null)
        {
            if (src.HasTexture(texturePropertyName))
            {
                Texture parentTexture = src.GetTexture(texturePropertyName);
                myVFX.SetTexture(texturePropertyName, parentTexture);
            }
        }
    }
}