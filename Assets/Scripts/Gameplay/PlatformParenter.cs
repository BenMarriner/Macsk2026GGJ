using UnityEngine;

public class PlatformParenter : MonoBehaviour
{
    [SerializeField] string PlayerTag = "Player";

    void OnTriggerEnter(Collider collider)
    {
        if (!collider.gameObject.CompareTag(PlayerTag))
        {
            return;
        }

        collider.transform.parent.SetParent(transform);
    }

    void OnTriggerExit(Collider collider)
    {
        if (!collider.gameObject.CompareTag(PlayerTag))
        {
            return;
        }

        collider.transform.parent.SetParent(null);
    }
}
