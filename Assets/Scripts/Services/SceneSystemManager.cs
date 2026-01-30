using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneSystemManager : MonoBehaviour
{
	#region Variables
	[SerializeField] private TransitionState _state = TransitionState.IDLE;
	private int _nextScene;
	[SerializeField] private Scene _currentScene;
	[SerializeField] private SceneChangeMode _sceneChangeMode = SceneChangeMode.SERVICES;
	[SerializeField] private List<int> _levelSceneIndexes = new List<int>(){2,3,4,5,6,7,8,9};
	[SerializeField] private List<int> _endScreenIndexes = new List<int>(){10,11};
	[SerializeField] private int _servicesSceneIndex = 0;
	[SerializeField] private int _gameplaySceneIndex = 2;
	#endregion

	#region Event Functions
	private void OnEnable()
	{
		EventManager.RegisterEvent(EventKey.QUIT_GAME, QuitEvent);
		EventManager.RegisterEvent(EventKey.OPEN_SCENE, ChangeSceneEvent);

		_currentScene = SceneManager.GetActiveScene();
	}

	private void OnDisable()
	{
		EventManager.DeregisterEvent(EventKey.QUIT_GAME, QuitEvent);
		EventManager.DeregisterEvent(EventKey.OPEN_SCENE, ChangeSceneEvent);
	}
	#endregion

	#region Start Transition
	private void ChangeSceneEvent(object eventData) => ChangeScene((int)eventData);

	// Starts a transition to a new scene
	public void ChangeScene(int sceneNum)
	{
		if (!IsTransitioning()) return;

		_state = TransitionState.SETSCENE;
		_nextScene = sceneNum;

		// If scene is services or main menu, set services mode
		if (sceneNum == 0 || sceneNum == 1)
		{
			_sceneChangeMode = SceneChangeMode.SERVICES;
		}
		// If scene is gameplay, open the first level and set gameplay mode
		else if (sceneNum == 2)
		{
			_sceneChangeMode = SceneChangeMode.GAMEPLAY;
			_nextScene = _gameplaySceneIndex + 1;
		}
		// If scene is level, set gameplay mode
		else if (_levelSceneIndexes.Contains(sceneNum))
		{
			_sceneChangeMode = SceneChangeMode.GAMEPLAY;
		}
		// If scene is end screen, services mode
		else if (_endScreenIndexes.Contains(sceneNum))
		{
			_sceneChangeMode = SceneChangeMode.SERVICES;
		}
		else
		// if something else, just load main menu
		{
			_sceneChangeMode = SceneChangeMode.SERVICES;
			_nextScene = 1;
		}

		StartCoroutine(ChangeSceneCoroutine(_nextScene));
	}

	public bool IsTransitioning()
	{
		return _state == TransitionState.IDLE; // CHANGED: return true when ready
	}
	#endregion

	#region Scene Loading
	// Unloads the old scene, loads the new one and makes sure 
	// services + gameplay scenes are loaded if necessary
	private IEnumerator ChangeSceneCoroutine(int sceneIndex)
	{
		yield return StartCoroutine(CheckScenes());
		yield return StartCoroutine(UnloadScene(_currentScene.buildIndex));
		yield return StartCoroutine(LoadScene(sceneIndex));

		// Enable new scene
		_currentScene = SceneManager.GetSceneByBuildIndex(sceneIndex);
		SceneManager.SetActiveScene(_currentScene);

		// Wait for everything to initialise
		yield return new WaitForEndOfFrame();
		_state = TransitionState.IDLE;
		EventManager.TriggerEvent(EventKey.LOADING_COMPLETE, true);
	}

	private IEnumerator CheckScenes()
	{
		Scene services = SceneManager.GetSceneByBuildIndex(_servicesSceneIndex);
		Scene gameplay = SceneManager.GetSceneByBuildIndex(_gameplaySceneIndex);

		if (!services.isLoaded) yield return StartCoroutine(LoadScene(_servicesSceneIndex));

		if (!gameplay.isLoaded)
		{
			if (_sceneChangeMode == SceneChangeMode.GAMEPLAY)
				yield return StartCoroutine(LoadScene(_gameplaySceneIndex));
		}
		else
		{
			if (_sceneChangeMode == SceneChangeMode.SERVICES)
				yield return StartCoroutine(UnloadScene(_gameplaySceneIndex));
		}
	}

	private IEnumerator LoadScene(int sceneIndex)
	{
		AsyncOperation loadAsync = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);
		while (!loadAsync.isDone) yield return null;
	}

	private IEnumerator UnloadScene(int sceneIndex)
	{
		AsyncOperation loadAsync = SceneManager.UnloadSceneAsync(sceneIndex);
		while (!loadAsync.isDone) yield return null;
	}
	#endregion

	#region Quit Transition
	private void QuitEvent(object eventData)
	{
		Debug.Log("SceneSystemManager.OnTransitionClosed(): Quitting game!");
#if UNITY_EDITOR
		EditorApplication.isPlaying = false;
#else
				Application.Quit();
#endif
	}
	#endregion

	public enum TransitionState
	{
		IDLE, // Ready to initiate a transition
		SETSCENE, // Transitioning to a new scene
		QUIT // Transitioning out of the game
	}
	
	public enum SceneChangeMode
	{
		SERVICES, // Needs only services scene
		GAMEPLAY, // Needs services AND gameplay scenes
	}
}

