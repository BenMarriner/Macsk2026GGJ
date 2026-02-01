using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private GameObject _mainMenuContainer;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] protected int travelSceneIndex = 3;
    
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        InitialiseMenu();
        SetupButtonListeners();
        EventManager.TriggerEvent(EventKey.SFX, SoundType.ExampleSound);
    }

    private void InitialiseMenu()
    {

    }

    private void SetupButtonListeners()
    {
        // Main menu buttons
        _playButton?.onClick.AddListener(OnPlayClicked);
        _quitButton?.onClick.AddListener(OnQuitClicked);
    }

    #region Button Handlers
    protected virtual void OnPlayClicked()
    {
        EventManager.TriggerEvent(EventKey.SFX, SoundType.ButtonClick);
        DebugLogger.Log("Starting game...");
        // Scene index 2 should be your Gameplay scene
        EventManager.TriggerEvent(EventKey.OPEN_SCENE, travelSceneIndex);
    }

    protected virtual void OnQuitClicked()
    {
        EventManager.TriggerEvent(EventKey.SFX, SoundType.ButtonClick);
        DebugLogger.Log("Quitting game...");
        EventManager.TriggerEvent(EventKey.QUIT_GAME, null);
    }
    #endregion
}