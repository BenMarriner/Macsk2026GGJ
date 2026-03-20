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
    [SerializeField] private float _defaultSoundVolume = 80;

    [SerializeField] private AudioMixer _audioMixer;

    private string _cameraSenseString = "cameraSensitivity";
    private string _SFXVolumeString = "SFXVolume";
    private string _musicVolumeString = "musicVolume";
    private string _mixerMusicString = "MusicVolume";
    private string _musicSFXString = "SFXVolume";
    
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
        SaveCameraSensitivity(sensitivity);
        SetCameraSensitivity(sensitivity, false);
    }

    protected virtual void OnChangeSFXVolume(float volume)
    {
        SaveSFXVolume(volume);
        SetSFXVolume(volume, false);
    }

    protected virtual void OnChangeMusicVolume(float volume)
    {
        SaveMusicVolume(volume);
        SetMusicVolume(volume, false);
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
        _SFXVolumeText.text = volume.ToString("F1");
        _audioMixer.SetFloat(_musicSFXString, volume);
    }

    private void SetMusicVolume(float volume, bool setSlider = true)
    {
        if (!_musicVolumeSlider || !_musicVolumeText || !_audioMixer) return;
        if (setSlider) _musicVolumeSlider.value = volume;
        _musicVolumeText.text = volume.ToString("F1");
        _audioMixer.SetFloat(_mixerMusicString, volume);
    }

    private void SaveAndApplySettings(float sensitivity)
    {
        PlayerPrefs.SetFloat(_cameraSenseString, sensitivity);
    }
    
    private void SaveCameraSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat(_cameraSenseString, sensitivity);
    }

    private void SaveSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat(_SFXVolumeString, volume);
    }

    private void SaveMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(_musicVolumeString, volume);
    }
}
