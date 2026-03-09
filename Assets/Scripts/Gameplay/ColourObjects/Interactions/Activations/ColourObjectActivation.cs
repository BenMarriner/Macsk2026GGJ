using UnityEngine;

public class ColourObjectActivator : AbstractActivation
{
    [SerializeField] private ColouredObject _colouredObject;
    [SerializeField] private ColouredObject[] _colouredObjects;
    [SerializeField] private bool _disablesObjects = false;
    
    public override void Activate()
    {
        if (_activated) return;
        _activated = !_activated;

        if (_colouredObject)
        {
            _colouredObject.SetEnabled(!_disablesObjects);
        }
        
        foreach (ColouredObject item in _colouredObjects)
        {
            if (!item) continue;

            item.SetEnabled(!_disablesObjects);
        }
    }
}
