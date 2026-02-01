using System;
using UnityEngine;
using UnityEngine.UI;

public class MaskManager : MonoBehaviour
{
    [SerializeField] private MaskMode _currentMaskMode = MaskMode.NoMask;
    private int MaskIntMax = Enum.GetNames(typeof(MaskMode)).Length - 1;

    protected virtual void OnEnable()
    {
        EventManager.RegisterEvent(EventKey.MASK_INPUT, SwitchMaskScroll);
    }

    protected virtual void OnDisable()
    {
        EventManager.DeregisterEvent(EventKey.MASK_INPUT, SwitchMaskScroll);
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    void SwitchMaskScroll(object eventData)
    {
        if (eventData is not int) this.LogError("Event listener recieved incorrect data type!");
        int maskChange = (int)eventData;

        _currentMaskMode = _currentMaskMode + maskChange;

        if ((int)_currentMaskMode > MaskIntMax)
        {
            _currentMaskMode = 0;
        }
        else if ((int)_currentMaskMode < 0)
        {
            _currentMaskMode = (MaskMode)MaskIntMax;
        }

        if (_currentMaskMode == MaskMode.NoMask)
        {
            SwitchMaskScroll(maskChange);
            return;
        }

        EventManager.TriggerEvent(EventKey.MASK_MODE_CHANGED, _currentMaskMode);
        EventManager.TriggerEvent(EventKey.SFX, SoundType.MaskChanage02);

        switch (_currentMaskMode)
        {
            case MaskMode.NoMask:
                EventManager.TriggerEvent(EventKey.MUSIC, SoundType.NoMask);
                break;
            case MaskMode.RedMask:
                EventManager.TriggerEvent(EventKey.MUSIC, SoundType.RedMask);
                break;
            case MaskMode.GreenMask:
                EventManager.TriggerEvent(EventKey.MUSIC, SoundType.GreenMask);
                break;
            case MaskMode.BlueMask:
                EventManager.TriggerEvent(EventKey.MUSIC, SoundType.BlueMask);
            break;
        }
    }
}