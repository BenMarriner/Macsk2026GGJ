using UnityEngine;

public class ColourObjectActivator : MonoBehaviour, IActivate
{
    private bool activated = false;

    [SerializeField] private ColouredObject _colouredObject;
    [SerializeField] private bool _disablesObjects = false;
    
    public void Activate()
    {
        if (activated) return;
        activated = !activated;
        
        _colouredObject.SetEnabled(!_disablesObjects);
    }
}
