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
                TriggerRedEffect();
            break;
            case MaskMode.GreenMask:
                TriggerGreenEffect();
            break;
            case MaskMode.BlueMask:
                TriggerBlueEffect();
            break;
        }
    }

    protected virtual void TriggerRedEffect()
    {
        SetRedEffect(true);
        SetGreenEffect(false);
        SetBlueEffect(false);
    }

    protected virtual void TriggerGreenEffect()
    {
        SetRedEffect(false);
        SetGreenEffect(true);
        SetBlueEffect(false);
    }

    protected virtual void TriggerBlueEffect()
    {
        SetRedEffect(false);
        SetGreenEffect(false);
        SetBlueEffect(true);
    }

    protected virtual void SetRedEffect(bool redEnabled)
    {
    }

    protected virtual void SetGreenEffect(bool greenEnabled)
    {
    }

    protected virtual void SetBlueEffect(bool blueEnabled)
    {
    }
}
