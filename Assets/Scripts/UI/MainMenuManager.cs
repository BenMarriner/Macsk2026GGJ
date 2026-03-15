using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IUIController
{
    public void EnablingUI();
}


public class MainMenuManager : MonoBehaviour, IUIController
{
    [Header("Main Menu")]
    [SerializeField] private GameObject _mainMenuContainer;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] protected int travelSceneIndex = 3;
    
    [Header("Options")]
    [SerializeField] private Button _optionsButton;
    [SerializeField] private OptionsMenu optionsMenu;
    
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetupButtonListeners();
        EventManager.TriggerEvent(EventKey.MUSIC, MusicKey.NoMask);
    }

    public void EnablingUI()
    {
        _playButton.Select();
    }

    private void SetupButtonListeners()
    {
        // Main menu buttons
        _playButton?.onClick.AddListener(OnPlayClicked);
        _quitButton?.onClick.AddListener(OnQuitClicked);
        
        _optionsButton?.onClick.AddListener(OnOptionsClicked);
    }

    #region Button Handlers
    protected virtual void OnPlayClicked()
    {
        EventManager.TriggerEvent(EventKey.SFX, SoundType.ButtonClick);
        DebugLogger.Log("Starting game...");
        
        EventManager.TriggerEvent(EventKey.OPEN_SCENE, travelSceneIndex);
    }

    protected virtual void OnQuitClicked()
    {
        EventManager.TriggerEvent(EventKey.SFX, SoundType.ButtonClick);
        DebugLogger.Log("Quitting game...");
        EventManager.TriggerEvent(EventKey.QUIT_GAME, null);
    }

    protected virtual void OnOptionsClicked()
    {
        EventManager.TriggerEvent(EventKey.SFX, SoundType.ButtonClick);
        DebugLogger.Log("Opening options menu...");
        
        // Enable Options menu
        optionsMenu?.AccessOptionsMenu(_mainMenuContainer, this);
    }
    
    
    #endregion
    

}