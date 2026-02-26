using UnityEngine;

public class ColourObjectActivator : AbstractActivation
{
    [SerializeField] private ColouredObject _colouredObject;
    [SerializeField] private bool _disablesObjects = false;
    
    public override void Activate()
    {
        if (_activated) return;
        _activated = !_activated;
        
        _colouredObject.SetEnabled(!_disablesObjects);
    }
}
