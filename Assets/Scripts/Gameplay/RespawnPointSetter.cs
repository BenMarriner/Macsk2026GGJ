using UnityEngine;

public class RespawnPointSetter : MonoBehaviour
{
    [SerializeField] private Transform _respawnPoint;
    [SerializeField] private string _playerTag = "Player";
    private void OnTriggerEnter(Collider collider)
    {
        if (!collider.gameObject.CompareTag(_playerTag)) return;
        if (!_respawnPoint) return;
        
        EventManager.TriggerEvent(EventKey.RESPAWN_SET, _respawnPoint.position);
    }
}
