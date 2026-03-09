using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject eventSystem;
    
    [Header("Canvas Panels")]
    [SerializeField] private GameObject pausePanel;
    
    [Header("UI Elements")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    //Todo: add options menu button UI elements
    
    public bool IsPaused { get; private set; } = false;
    
    private void Start()
    {
        DisableCursor();
        SetupButtonListeners();
        pausePanel.SetActive(false);
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

    private void SetupButtonListeners()
    {
        resumeButton?.onClick.AddListener(OnResumeClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);

        if (eventSystem && eventSystem.TryGetComponent(out InputSystemUIInputModule ISUIInputModule))
        {
            ISUIInputModule.cancel.action.performed += EnablePause;
        }
        else
        {
            Debug.LogWarning("No UI InputModule found");
        }
        
        //Todo: add options menu button listeners
    }

    private void OnResumeClicked()
    {
        DisablePause();
    }

    private void OnQuitClicked()
    {
        EventManager.TriggerEvent(EventKey.OPEN_SCENE, 1);
    }

    private void EnablePause(InputAction.CallbackContext context)
    {
        if (IsPaused)
        {
            DisablePause();
            return;
        }
        
        Debug.Log("OnPause");
        IsPaused = true;

        EnableCursor();
        
        // Disable player controls
        TogglePlayerControls(false);
        
        // Enable Pause Menu UI
        pausePanel.SetActive(true);
        
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
        pausePanel.SetActive(false);
        
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
