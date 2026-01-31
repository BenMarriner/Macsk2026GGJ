using UnityEngine;

public class SpawnPlayerOnStart : MonoBehaviour
{
    [SerializeField] private GameObject cameraHolder;
    [SerializeField] private GameObject playerCharacter;

    [SerializeField] private Transform spawnPosition;

    private void Start()
    {
        if (!cameraHolder || !playerCharacter) { return; }

        GameObject ch = Instantiate(cameraHolder, spawnPosition.position, Quaternion.identity);
        GameObject pc = Instantiate(playerCharacter, spawnPosition.position, Quaternion.identity);

        if (ch && pc && ch.TryGetComponent(out InputHandler ih))
        {
            ih.AssignAndSetupPlayerCharacter(pc);
        }
    }
}
