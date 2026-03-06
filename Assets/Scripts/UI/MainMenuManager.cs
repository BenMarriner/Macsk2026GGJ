using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private GameObject _mainMenuContainer;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] protected int travelSceneIndex = 3;
    
    [Header("Options")]
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _backOptionsButton;
    [SerializeField] private GameObject _optionsMenu;
    
    [SerializeField] private TextMeshProUGUI cameraSensitivityText;
    [SerializeField] private Slider cameraSensitivitySlider;
    
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        InitialiseMenu();
        SetupButtonListeners();
        EventManager.TriggerEvent(EventKey.MUSIC, MusicKey.NoMask);
    }

    private void InitialiseMenu()
    {
        if (!PlayerPrefs.HasKey("cameraSensitivity"))
        {
            PlayerPrefs.SetFloat("cameraSensitivity", 100);
            LoadCameraSensitivity();
        }
        else
        {
            LoadCameraSensitivity();
        }
    }

    private void SetupButtonListeners()
    {
        // Main menu buttons
        _playButton?.onClick.AddListener(OnPlayClicked);
        _quitButton?.onClick.AddListener(OnQuitClicked);
        
        _optionsButton?.onClick.AddListener(OnOptionsClicked);
        _backOptionsButton?.onClick.AddListener(OnBackOptionsClicked);
        cameraSensitivitySlider?.onValueChanged.AddListener(OnChangeCameraSensitivity);
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
        _optionsMenu.SetActive(true);
    }
    
    protected virtual void OnBackOptionsClicked()
    {
        EventManager.TriggerEvent(EventKey.SFX, SoundType.ButtonClick);
        DebugLogger.Log("Opening options menu...");
        
        // Disable Options menu
        _optionsMenu.SetActive(false);
    }
    
    #endregion
    
    protected virtual void OnChangeCameraSensitivity(float value)
    {
        //gameSettingsManger.cameraSensitivity = cameraSensitivitySlider.value;
        
        if (!cameraSensitivitySlider || !cameraSensitivityText) return;
        SaveCameraSensitivity(value);

        cameraSensitivityText.text = cameraSensitivitySlider.value.ToString("F1");
    }
    
    private void LoadCameraSensitivity()
    {
        if (!cameraSensitivitySlider || !cameraSensitivityText) return;
        cameraSensitivitySlider.value = PlayerPrefs.GetFloat("cameraSensitivity");
        cameraSensitivityText.text = cameraSensitivitySlider.value.ToString("F1");
    }
    
    private void SaveCameraSensitivity(float value)
    {
        PlayerPrefs.SetFloat("cameraSensitivity", value);
    }
}