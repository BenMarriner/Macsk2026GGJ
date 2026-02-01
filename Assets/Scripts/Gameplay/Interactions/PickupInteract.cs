using UnityEngine;

public class PickupInteract : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        // give the player the mask
        
        Destroy(gameObject);
    }

    public void SetCanBeInteracted(bool val)
    {
        throw new System.NotImplementedException();
    }

    public void Highlight()
    {
        throw new System.NotImplementedException();
    }

    public void Unhighlight()
    {
        throw new System.NotImplementedException();
    }
}
