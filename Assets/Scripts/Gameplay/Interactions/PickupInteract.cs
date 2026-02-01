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
    }

    public void Highlight()
    {
    }

    public void Unhighlight()
    {
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        EventManager.TriggerEvent(EventKey.MASK_PICKUP, null);

        Destroy(gameObject);
    }
}
