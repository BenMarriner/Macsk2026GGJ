using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button backOptionsButton;
    
    [SerializeField] private TextMeshProUGUI cameraSensitivityText;
    [SerializeField] private Slider cameraSensitivitySlider;
    [SerializeField] private TextMeshProUGUI _SFXVolumeText;
    [SerializeField] private Slider _SFXVolumeSlider;
    [SerializeField] private TextMeshProUGUI _musicVolumeText;
    [SerializeField] private Slider _musicVolumeSlider;

    [SerializeField] private float _defaultCameraSensitivity = 50;
    [SerializeField] private float _defaultSoundVolume = 0;
    [SerializeField] private float _volumeDisplayOffset = 80;

    [SerializeField] private AudioMixer _audioMixer;

    private string _cameraSenseString = "cameraSensitivity"; //variable name for accessing sensitivity setting in PlayerPrefs
    private string _SFXVolumeString = "SFXVolume"; //variable name for accessing sfx setting in PlayerPrefs
    private string _musicVolumeString = "musicVolume"; //variable name for accessing music setting in PlayerPrefs
    private string _musicSFXString = "SFXVolume"; //variable name for accessing SFX setting in audio mixer
    private string _mixerMusicString = "MusicVolume"; //variable name for accessing music setting in audio mixer
    
    private CameraController cameraControllerRef;
    private GameObject previousUIElementsHolder;
    private IUIController previousUIController;
    
    public bool IsInOptionsMenu { get; private set; } = false;
    
    private void Start()
    {
        if (optionsPanel && optionsPanel.activeSelf)
        {
            optionsPanel.SetActive(false);
        }
        
        SetupButtonListeners();
        InitialiseMenu();

        GetCameraControllerRef();
    }
    
    // Used by other scripts to enable the options menu
    public void AccessOptionsMenu(GameObject previousUIElements, IUIController controller)
    {
        if (previousUIElements != null && 
            previousUIElementsHolder != previousUIElements)
        {
            previousUIElementsHolder = previousUIElements;
        }

        previousUIElementsHolder?.SetActive(false);
        
        optionsPanel.SetActive(true);

        IsInOptionsMenu = true;
        
        cameraSensitivitySlider.Select();
        previousUIController = controller;
    }
    
    private void GetCameraControllerRef()
    {
        GameObject cameraHolder = GamePlayStatics.GetCameraHolder();
        if (cameraHolder && cameraHolder.TryGetComponent(out CameraController cc))
        {
            cameraControllerRef = cc;
        }
        else
        {
            Debug.Log("No camera controller found in this scene");
        }
    }
    
    private void InitialiseMenu()
    {
        if (!PlayerPrefs.HasKey(_cameraSenseString))
        {
            PlayerPrefs.SetFloat(_cameraSenseString, _defaultCameraSensitivity);
            LoadSettings();
        }
        else
        {
            LoadSettings();
        }
    }

    private void SetupButtonListeners()
    {
        backOptionsButton?.onClick.AddListener(OnBackOptionsClicked);
        cameraSensitivitySlider?.onValueChanged.AddListener(OnChangeCameraSensitivity);
        _SFXVolumeSlider?.onValueChanged.AddListener(OnChangeSFXVolume);
        _musicVolumeSlider?.onValueChanged.AddListener(OnChangeMusicVolume);
    }
    
    public virtual void OnBackOptionsClicked()
    {
        EventManager.TriggerEvent(EventKey.SFX, SoundType.ButtonClick);
        DebugLogger.Log("Opening options menu...");
        
        // Disable Options menu
        optionsPanel.SetActive(false);
        previousUIElementsHolder?.SetActive(true);
        
        IsInOptionsMenu = false;
        
        previousUIController.EnablingUI();
    }
    
    protected virtual void OnChangeCameraSensitivity(float sensitivity)
    {
        SaveFloatSetting(_cameraSenseString, sensitivity);
        SetCameraSensitivity(sensitivity, false);
    }

    protected virtual void OnChangeSFXVolume(float volume)
    {
        SaveFloatSetting(_SFXVolumeString, volume);
        SetSFXVolume(volume, false);
    }

    protected virtual void OnChangeMusicVolume(float volume)
    {
        SaveFloatSetting(_musicVolumeString, volume);
        SetMusicVolume(volume, false);
    }

    private void LoadSettings()
    {
        SetCameraSensitivity(PlayerPrefs.GetFloat(_cameraSenseString));
        SetSFXVolume(PlayerPrefs.GetFloat(_SFXVolumeString));
        SetMusicVolume(PlayerPrefs.GetFloat(_musicVolumeString));
    }
    
    private void SetCameraSensitivity(float sensitivity, bool setSlider = true)
    {
        if (!cameraSensitivitySlider || !cameraSensitivityText) return;
        if (setSlider) cameraSensitivitySlider.value = sensitivity;
        cameraSensitivityText.text = sensitivity.ToString("F1");

        if (!cameraControllerRef)
        {
            GetCameraControllerRef();
        }
        
        cameraControllerRef?.SetCameraSensitivity(sensitivity);
    }

    private void SetSFXVolume(float volume, bool setSlider = true)
    {
        if (!_SFXVolumeSlider || !_SFXVolumeText || !_audioMixer) return;
        if (setSlider) _SFXVolumeSlider.value = volume;
        _SFXVolumeText.text = (volume + _volumeDisplayOffset).ToString("F1");
        _audioMixer.SetFloat(_musicSFXString, volume);
    }

    private void SetMusicVolume(float volume, bool setSlider = true)
    {
        if (!_musicVolumeSlider || !_musicVolumeText || !_audioMixer) return;
        if (setSlider) _musicVolumeSlider.value = volume;
        _musicVolumeText.text = (volume + _volumeDisplayOffset).ToString("F1");
        _audioMixer.SetFloat(_mixerMusicString, volume);
    }
    
    private void SaveFloatSetting(string settingStringKey,float value)
    {
        PlayerPrefs.SetFloat(settingStringKey, value);
    }
}
