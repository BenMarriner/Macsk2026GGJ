using UnityEngine;

public class PlatformParenter : MonoBehaviour
{
    [SerializeField] string PlayerTag = "Player";

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(PlayerTag))
        {
            return;
        }

        collision.transform.SetParent(transform);
    }

    void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.CompareTag(PlayerTag))
        {
            return;
        }

        collision.transform.SetParent(null);
    }
}
