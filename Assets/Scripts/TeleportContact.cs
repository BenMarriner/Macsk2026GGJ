using System;
using UnityEngine;

public class TeleportContact : MonoBehaviour
{
    [SerializeField]
    private Transform teleportPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (!teleportPoint || other.transform.parent.gameObject.TryGetComponent(out PlayerTeleport pt)) return;
        
        pt.TeleportToPosition(teleportPoint.position);
    }
}
