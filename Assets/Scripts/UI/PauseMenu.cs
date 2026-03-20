using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

public class PauseMenu : MonoBehaviour, IUIController
{
    [SerializeField] private GameObject eventSystem;
    
    [Header("Canvas Panels")]
    [SerializeField] private GameObject pauseCanvas;

    [SerializeField] private GameObject pausePanel;
    
    [Header("UI Elements")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button _backgroundButton;
    [SerializeField] private OptionsMenu optionsMenu;
    
    public bool IsPaused { get; private set; } = false;
    
    private void Start()
    {
        DisableCursor();
        SetupButtonListeners();
        pauseCanvas.SetActive(false);
    }

    private void EnableCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    private void DisableCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void EnablingUI()
    {
        resumeButton.Select();
    }

    private void SetupButtonListeners()
    {
        resumeButton?.onClick.AddListener(OnResumeClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);
        _backgroundButton?.onClick.AddListener(OnBackgroundClicked);

        if (eventSystem && eventSystem.TryGetComponent(out InputSystemUIInputModule ISUIInputModule))
        {
            ISUIInputModule.cancel.action.performed += OnCancelClicked;
        }
        else
        {
            Debug.LogWarning("No UI InputModule found");
        }
        
        //Todo: add options menu button listeners
        optionsButton?.onClick.AddListener(OnOptionsClicked);
    }

    private void OnResumeClicked()
    {
        DisablePause();
    }

    private void OnQuitClicked()
    {
        DisablePause();
        
        EventManager.TriggerEvent(EventKey.OPEN_SCENE, 1);
    }

    public virtual void OnBackgroundClicked()
    {   
        if (!resumeButton) return;

        resumeButton.Select();
    }

    private void OnOptionsClicked()
    {
        optionsMenu.AccessOptionsMenu(pausePanel, this);
    }

    private void OnCancelClicked(InputAction.CallbackContext context)
    {
        if (!IsPaused)
        {
            EnablePause();
        }
        else if (optionsMenu.IsInOptionsMenu)
        {
            optionsMenu.OnBackOptionsClicked();
        }
        else
        {
            DisablePause();
        }
    }
    
    private void EnablePause()
    {
        if (IsPaused)
        {
            return;
        }
        
        Debug.Log("OnPause");
        IsPaused = true;

        EnableCursor();
        
        // Disable player controls
        TogglePlayerControls(false);
        
        // Enable Pause Menu UI
        pauseCanvas?.SetActive(true);
        
        // Set Time scale to 0
        Time.timeScale = 0;
    }

    private void DisablePause()
    {
        if (!IsPaused) return;
        
        Debug.Log("OnUnpause");
        IsPaused = false;

        DisableCursor();
        
        // Enable player controls
        TogglePlayerControls(true);
        
        // Disable Pause Menu UI
        pauseCanvas?.SetActive(false);
        
        // Set Time scale to 1
        Time.timeScale = 1;
    }

    private void TogglePlayerControls(bool enable)
    {
        GameObject player = GamePlayStatics.GetPlayer();
        GameObject cameraHolder = GamePlayStatics.GetCameraHolder();
        if (player && 
            player.TryGetComponent(out CharacterMovementController playerMovementController) && 
            player.TryGetComponent(out Interactor playerInteractor))
        {
            playerMovementController.enabled = enable;
            playerInteractor.enabled = enable;
        }
        else
        {
            Debug.LogWarning("No player found");
        }

        if (cameraHolder && 
            cameraHolder.TryGetComponent(out CameraController cameraController) && 
            cameraController.TryGetComponent(out InputHandler inputHandler))
        {
            cameraController.enabled = enable;
            inputHandler.enabled = enable;
        }
        else
        {
            Debug.LogWarning("No Camera Holder found");
        }
    }
}