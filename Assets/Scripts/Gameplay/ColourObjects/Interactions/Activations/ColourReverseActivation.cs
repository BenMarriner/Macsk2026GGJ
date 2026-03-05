using UnityEngine;

public class ColourReverseActivator : AbstractActivation
{
    [SerializeField] private ColouredObject _colouredObject;
    
    public override void Activate()
    {
        if (_activated) return;
        _activated = !_activated;
        
        _colouredObject.ToggleEffectReversed();
    }
}
