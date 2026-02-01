using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GamePlayStatics;

class TeleportInteract: GreenObject, IInteractable
{
    [SerializeField]
    private Transform teleportPoint;

    public void SetCanBeInteracted(bool val)
    {
        // placeholder
    }
    
    public void Interact()
    {
        if (GetPlayer().TryGetComponent(out PlayerTeleport pt))
        {
            pt.TeleportToPosition(teleportPoint.position);
        }
    }
}
