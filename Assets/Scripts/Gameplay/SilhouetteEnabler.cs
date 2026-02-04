using System.Collections.Generic;
using UnityEngine;

public class SilhouetteEnabler : MonoBehaviour
{
    [SerializeField] private List<GreenObject> _silhouetteObjects;
    [SerializeField] private string _playerTag = "Player";

    void OnTriggerEnter(Collider collider)
    {
        DebugLogger.Log(collider.gameObject.tag);
        if (!collider.gameObject.CompareTag(_playerTag))
        {
            return;
        }
        DebugLogger.Log("enter");

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
        DebugLogger.Log("exit");

        foreach (GreenObject item in _silhouetteObjects)
        {
            item.SetSilhouetteEnabled(false);
        }
    }
}
