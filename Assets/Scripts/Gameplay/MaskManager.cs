using System;
using UnityEngine;

public class MaskManager : MonoBehaviour
{
    [SerializeField] private MaskMode CurrentMaskMode = MaskMode.NoMask;
    private int MaskIntMax = Enum.GetNames(typeof(MaskMode)).Length - 1;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    void SwitchMaskScroll(int maskChange)
    {
        CurrentMaskMode = CurrentMaskMode + maskChange;

        if ((int)CurrentMaskMode > MaskIntMax)
        {
            CurrentMaskMode = 0;
        }
        else if ((int)CurrentMaskMode < 0)
        {
            CurrentMaskMode = (MaskMode)MaskIntMax;
        }

        if (CurrentMaskMode == MaskMode.NoMask)
        {
            SwitchMaskScroll(maskChange);
            return;
        }

        EventManager.TriggerEvent(EventKey.MASK_MODE_CHANGED, CurrentMaskMode);
    }
}