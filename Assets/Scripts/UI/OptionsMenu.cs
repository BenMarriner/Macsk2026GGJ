using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button backOptionsButton;
    
    [SerializeField] private TextMeshProUGUI cameraSensitivityText;
    [SerializeField] private Slider cameraSensitivitySlider;

    [SerializeField] private float _defaultCameraSensitivity = 50;
    
    private CameraController cameraControllerRef;
    private GameObject previousUIElementsHolder;
    
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
    public void AccessOptionsMenu(GameObject previousUIElements)
    {

        if (previousUIElements != null && 
            previousUIElementsHolder != previousUIElements)
        {
            previousUIElementsHolder = previousUIElements;
        }

        previousUIElementsHolder?.SetActive(false);
        
        optionsPanel.SetActive(true);

        IsInOptionsMenu = true;
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
        if (!PlayerPrefs.HasKey("cameraSensitivity"))
        {
            PlayerPrefs.SetFloat("cameraSensitivity", _defaultCameraSensitivity);
            LoadCameraSensitivity();
        }
        else
        {
            LoadCameraSensitivity();
        }
    }

    private void SetupButtonListeners()
    {
        backOptionsButton?.onClick.AddListener(OnBackOptionsClicked);
        cameraSensitivitySlider?.onValueChanged.AddListener(OnChangeCameraSensitivity);
    }
    
    public virtual void OnBackOptionsClicked()
    {
        EventManager.TriggerEvent(EventKey.SFX, SoundType.ButtonClick);
        DebugLogger.Log("Opening options menu...");
        
        // Disable Options menu
        optionsPanel.SetActive(false);
        previousUIElementsHolder?.SetActive(true);
        
        IsInOptionsMenu = false;
    }
    
    protected virtual void OnChangeCameraSensitivity(float value)
    {
        //gameSettingsManger.cameraSensitivity = cameraSensitivitySlider.value;
        
        if (!cameraSensitivitySlider || !cameraSensitivityText) return;
        SaveCameraSensitivity(value);
        
        cameraSensitivityText.text = cameraSensitivitySlider.value.ToString("F1");

        if (!cameraControllerRef)
        {
            GetCameraControllerRef();
        }
        
        cameraControllerRef?.SetCameraSensitivity(value);
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
