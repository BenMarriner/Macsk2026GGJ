using System;
using UnityEngine;
using UnityEngine.UI;

public class MaskManager : MonoBehaviour
{
    [SerializeField] private MaskMode _currentMaskMode = MaskMode.NoMask;
    private int MaskIntMax = Enum.GetNames(typeof(MaskMode)).Length - 1;
    
    [SerializeField] private Material screenTint;
    [SerializeField] private float redTintValue = .6f;
    [SerializeField] private float blueTintValue = .5f;
    [SerializeField] private float greenTintValue = .4f;

    protected virtual void OnEnable()
    {
        EventManager.RegisterEvent(EventKey.MASK_INPUT, SwitchMaskScroll);
        ResetScreenTint();
    }

    protected virtual void OnDisable()
    {
        EventManager.DeregisterEvent(EventKey.MASK_INPUT, SwitchMaskScroll);
    }

    protected void ResetScreenTint()
    {
        screenTint.SetFloat("_Red", 0f);
        screenTint.SetFloat("_Blue", 0f);
        screenTint.SetFloat("_Green", 0f);
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

        switch (_currentMaskMode)
        {
            case MaskMode.NoMask:
                EventManager.TriggerEvent(EventKey.MUSIC, SoundType.NoMask);
                ResetScreenTint();
                break;
            case MaskMode.RedMask:
                EventManager.TriggerEvent(EventKey.MUSIC, SoundType.RedMask);
                SetRedScreenTint();
                break;
            case MaskMode.GreenMask:
                EventManager.TriggerEvent(EventKey.MUSIC, SoundType.GreenMask);
                SetGreenScreenTint();
                break;
            case MaskMode.BlueMask:
                EventManager.TriggerEvent(EventKey.MUSIC, SoundType.BlueMask);
                SetBlueScreenTint();
            break;
        }
    }
    
    private void SetRedScreenTint()
    {
        screenTint.SetFloat("_Red", redTintValue);
        screenTint.SetFloat("_Blue", 0f);
        screenTint.SetFloat("_Green", 0f);
    }
    
    private void SetBlueScreenTint()
    {
        screenTint.SetFloat("_Red", 0f);
        screenTint.SetFloat("_Blue", blueTintValue);
        screenTint.SetFloat("_Green", 0f);
    }
    
    private void SetGreenScreenTint()
    {
        screenTint.SetFloat("_Red", 0f);
        screenTint.SetFloat("_Blue", 0f);
        screenTint.SetFloat("_Green", greenTintValue);
    }
    
}