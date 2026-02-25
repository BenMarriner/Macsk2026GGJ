using UnityEngine;

public abstract class AbstractActivation : MonoBehaviour, IActivate
{
    protected bool _activated = false;
    
    public abstract void Activate();
}
