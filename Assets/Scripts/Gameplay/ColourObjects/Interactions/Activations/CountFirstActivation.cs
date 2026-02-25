using UnityEngine;

public class CountFirstActivation : AbstractActivation
{
    [SerializeField] private GameObject[] _pairedInteractables;
    [SerializeField] private int _activationsBeforeTrigger = 1;
    [SerializeField] private int _currentActivations = 0;
    
    public override void Activate()
    {
        if (_activated) return;
        _currentActivations++;
        if (_currentActivations < _activationsBeforeTrigger) return;

        _activated = true;
        
        foreach (GameObject item in _pairedInteractables)
        {
            if (!item || !item.TryGetComponent(out IActivate activateable)) return;

            activateable.Activate();
        }
    }
}
