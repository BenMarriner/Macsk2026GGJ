using UnityEngine;

public class SpawnPlayerOnStart : MonoBehaviour
{
    [SerializeField] private GameObject cameraHolder;
    [SerializeField] private GameObject playerCharacter;
    [SerializeField] private Transform spawnPosition;
    private bool _spawnedPlayer = false;

    private void OnEnable()
	{
		EventManager.RegisterEvent(EventKey.LOADING_COMPLETE, SpawnPlayerHandler);
	}

	private void OnDisable()
	{
		EventManager.DeregisterEvent(EventKey.LOADING_COMPLETE, SpawnPlayerHandler);
	}

    private void SpawnPlayerHandler(object eventData)
    {
        if (_spawnedPlayer) return;
        if (!cameraHolder || !playerCharacter) return;

        _spawnedPlayer = true;
        GameObject ch = Instantiate(cameraHolder, spawnPosition.position, Quaternion.identity);
        GameObject pc = Instantiate(playerCharacter, spawnPosition.position, Quaternion.identity);

        if (ch && pc && ch.TryGetComponent(out InputHandler ih))
        {
            ih.AssignAndSetupPlayerCharacter(pc);

            if (PlayerPrefs.HasKey("cameraSensitivity") && ch.TryGetComponent(out CameraController cc))
            {
                cc.SetCameraSensitivity(PlayerPrefs.GetFloat("cameraSensitivity"));
            }
        }
    }
}
