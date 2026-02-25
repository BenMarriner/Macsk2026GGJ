using UnityEngine;

public class CountFirstActivation : AbstractActivation
{
    [SerializeField] private AbstractActivation[] _pairedInteractables;
    [SerializeField] private int _activationsBeforeTrigger = 1;
    [SerializeField] private int _currentActivations = 0;
    
    public override void Activate()
    {
        if (_activated) return;
        _currentActivations++;
        if (_currentActivations < _activationsBeforeTrigger) return;

        _activated = true;
        
        foreach (AbstractActivation item in _pairedInteractables)
        {
            if (!item) return;

            item.Activate();
        }
    }
}
