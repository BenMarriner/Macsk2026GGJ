using UnityEngine;
using UnityEngine.SceneManagement;

public class ServicesLoader : MonoBehaviour
{
    [SerializeField] private int _servicesSceneIndex = 0;
	[SerializeField] private int _gameplaySceneIndex = 2;
	[SerializeField] private bool _loadGameplay = false;
    private void Awake()
    {
        Scene services = SceneManager.GetSceneByBuildIndex(_servicesSceneIndex);

        if (!services.isLoaded)
        {
            SceneManager.LoadScene(_servicesSceneIndex, LoadSceneMode.Additive);
        }

        if (_loadGameplay)
        {
            Scene gameplay = SceneManager.GetSceneByBuildIndex(_gameplaySceneIndex);
            if (!gameplay.isLoaded)
            {
                SceneManager.LoadScene(_gameplaySceneIndex, LoadSceneMode.Additive);
            }
        }
    }
}
