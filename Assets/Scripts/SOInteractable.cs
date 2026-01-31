using UnityEngine;

//public class

interface IInteractionBehaviour<in T>
{
    public void Interact(T data);
}

public class SOInteractable<T> : ScriptableObject, IInteractionBehaviour<T> 
{
    public virtual void Interact(T data)
    {
        Debug.Log($"Interacting Test");
    }
}


[CreateAssetMenu(fileName = "New Interactable SO", menuName = "Interactable/Basic")]
public class SOBasicInteractable: SOInteractable<int>
{
    public override void Interact(int data)
    {
        Debug.Log($"Interacting Basic");
    }
}
