using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTeleport : MonoBehaviour
{
    private Vector3 _currentStoredPosition;
    
    void Start()
    {
        SetNewStoredPosition(transform.position);
    }

    public void SetNewStoredPosition(Vector3 position)
    {
        _currentStoredPosition = position;
    }

    public void TeleportToCurrentStoredPosition()
    {
        transform.position = _currentStoredPosition;
    }
    
    public void TeleportToPosition(Vector3 position)
    {
        transform.position = position;
    }
}
