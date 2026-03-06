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

    void OnTriggerEnter(Collider collider)
    {
        if (!collider.gameObject.CompareTag("Player"))
        {
            return;
        }

        EventManager.TriggerEvent(EventKey.MASK_PICKUP, null);

        EventManager.TriggerEvent(EventKey.SFX, SoundType.MaskPickup);

        Destroy(gameObject);
    }
}
