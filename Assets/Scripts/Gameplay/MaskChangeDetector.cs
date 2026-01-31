using System;
using UnityEngine;

public class MaskChangeDetector : MonoBehaviour
{
    [SerializeField] protected MaskMode _currentMaskMode = MaskMode.NoMask;
    [SerializeField] protected bool _effectReversed = false;

    protected virtual void OnEnable()
    {
        EventManager.RegisterEvent(EventKey.MASK_MODE_CHANGED, MaskChangeHandler);
    }

    protected virtual void OnDisable()
    {
        EventManager.DeregisterEvent(EventKey.MASK_MODE_CHANGED, MaskChangeHandler);
    }

    void Start()
    {
        
    }

    protected virtual void MaskChangeHandler(object eventData)
    {
        if (eventData is not MaskMode) this.LogError("Event listener recieved incorrect data type!");
        _currentMaskMode = (MaskMode)eventData;
        
        switch (_currentMaskMode)
        {
            case MaskMode.RedMask:
                ToggleRedEffect();
            break;
            case MaskMode.BlueMask:
                ToggleBlueEffect();
            break;
            case MaskMode.GreenMask:
                ToggleGreenEffect();
            break;
        }
    }

    protected virtual void ToggleRedEffect()
    {
        EnableRedEffect();
        DisableGreenEffect();
        DisableBlueEffect();
    }

    protected virtual void ToggleGreenEffect()
    {
        DisableRedEffect();
        EnableGreenEffect();
        DisableBlueEffect();
    }

    protected virtual void ToggleBlueEffect()
    {
        DisableRedEffect();
        DisableGreenEffect();
        EnableBlueEffect();
    }

    protected virtual void EnableRedEffect()
    {
        
    }

    protected virtual void DisableRedEffect()
    {
        
    }

    protected virtual void EnableGreenEffect()
    {
        
    }

    protected virtual void DisableGreenEffect()
    {
        
    }

    protected virtual void EnableBlueEffect()
    {
        
    }

    protected virtual void DisableBlueEffect()
    {
        
    }
}
