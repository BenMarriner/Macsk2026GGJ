using UnityEngine;

public class PlayerTeleport : MonoBehaviour
{
    private Vector3 _currentStoredPosition;

    protected void OnEnable()
	{
		EventManager.RegisterEvent(EventKey.RESPAWN_SET, RespawnSetHandler);
		EventManager.RegisterEvent(EventKey.PLAYER_DIED, PlayerDiedhandler);
	}

	protected void OnDisable()
	{
		EventManager.DeregisterEvent(EventKey.RESPAWN_SET, RespawnSetHandler);
		EventManager.DeregisterEvent(EventKey.PLAYER_DIED, PlayerDiedhandler);
	}
    
    void Start()
    {
        SetNewStoredPosition(transform.position);
    }

    protected virtual void RespawnSetHandler(object eventData)
    {
        if (eventData is not Vector3) this.LogError("Event listener recieved incorrect data type!");
        SetNewStoredPosition((Vector3)eventData);
    }

    protected virtual void PlayerDiedhandler(object eventData)
    {
        TeleportToCurrentStoredPosition();
    }

    public void SetNewStoredPosition(Vector3 position)
    {
        _currentStoredPosition = position;
    }

    public void TeleportToCurrentStoredPosition()
    {
        transform.SetPositionAndRotation(_currentStoredPosition, Quaternion.identity);
    }
    
    public void TeleportToPosition(Vector3 position)
    {
        //transform.position = position;
        transform.SetPositionAndRotation(position, Quaternion.identity);
    }
}
