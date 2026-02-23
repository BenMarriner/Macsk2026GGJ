using UnityEngine;

public class MultipleResponseActivation : MonoBehaviour, IActivate
{
    private bool _activated = false;

    [SerializeField] private GameObject[] _pairedInteractables;
    [SerializeField] private int _activationsBeforeTrigger = 1;
    [SerializeField] private int _currentActivations = 0;
    
    public void Activate()
    {
        if (_activated) return;
        if (_currentActivations < _activationsBeforeTrigger) return;

        _activated = true;
        
        foreach (GameObject item in _pairedInteractables)
        {
            if (!item || !item.TryGetComponent(out IActivate activateable)) return;

            activateable.Activate();
        }
    }
}
