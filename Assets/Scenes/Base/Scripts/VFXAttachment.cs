using UnityEngine;
using UnityEngine.VFX;
using System;

public class VFXAttachment : MonoBehaviour
{
    [Header("VFX & Animator")]
    public VisualEffect vfx;
    public Animator animator;

    [Header("Trigger Settings")]
    public string vfxPlayTrigger;
    public string vfxStopTrigger;
    public string animatorPlayTrigger;
    public string animatorStopTrigger;

    public event Action<VFXAttachment> OnFinished;

    private void Awake()
    {
        if (vfx == null)
            vfx = GetComponent<VisualEffect>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void Play()
    {
        if (vfx != null && !string.IsNullOrEmpty(vfxPlayTrigger))
            vfx.SendEvent(vfxPlayTrigger);

        if (animator != null && !string.IsNullOrEmpty(animatorPlayTrigger))
            animator.SetTrigger(animatorPlayTrigger);
    }

    public void Finish()
    {
        OnFinished?.Invoke(this);
    }

    public void Stop()
    {
        if (vfx != null && !string.IsNullOrEmpty(vfxStopTrigger))
            vfx.SendEvent(vfxStopTrigger);

        if (animator != null && !string.IsNullOrEmpty(animatorStopTrigger))
            animator.SetTrigger(animatorStopTrigger);
    }
}