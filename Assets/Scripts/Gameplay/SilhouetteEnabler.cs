using System.Collections.Generic;
using UnityEngine;

public class SilhouetteEnabler : MonoBehaviour
{
    [SerializeField] private List<GreenObject> _silhouetteObjects;
    [SerializeField] private string _playerTag = "Player";

    void OnTriggerEnter(Collider collider)
    {
        if (!collider.gameObject.CompareTag(_playerTag))
        {
            return;
        }

        foreach (GreenObject item in _silhouetteObjects)
        {
            item.SetSilhouetteEnabled(true);
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (!collider.gameObject.CompareTag(_playerTag))
        {
            return;
        }

        foreach (GreenObject item in _silhouetteObjects)
        {
            item.SetSilhouetteEnabled(false);
        }
    }
}
