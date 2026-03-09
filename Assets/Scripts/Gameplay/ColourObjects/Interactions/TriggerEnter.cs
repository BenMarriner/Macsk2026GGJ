using UnityEngine;

public class TriggerEnter : MonoBehaviour
{
    [SerializeField] private AbstractActivation[] _pairedInteractables;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private bool _onlyTriggeredOnce = true;
    private bool _activated = false;

    void OnTriggerEnter(Collider collider)
    {
        if (_activated) return;

        if (!collider.gameObject.CompareTag(_playerTag))
        {
            return;
        }

        if (_onlyTriggeredOnce) _activated = true;
        
        foreach (AbstractActivation item in _pairedInteractables)
        {
            if (!item) continue;

            item.Activate();
        }
    }
}
