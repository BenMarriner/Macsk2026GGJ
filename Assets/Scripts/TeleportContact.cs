using UnityEngine;

public class TeleportContact : MonoBehaviour
{
    [SerializeField] private string _playerTag = "Player";
    private void OnTriggerEnter(Collider collider)
    {
        if (!collider.gameObject.CompareTag(_playerTag))
        {
            return;
        }
        
        EventManager.TriggerEvent(EventKey.PLAYER_DIED, null);
    }
}
