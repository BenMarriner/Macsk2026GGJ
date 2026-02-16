using UnityEngine;

// false -> true is down button press change (0)
// true -> false is up button release change (1)
public struct InputDetector
{
    public bool InputState;
    private bool _previousInputState;

    public int HasStateChanged()
    {
        int result = -1;
        if (!_previousInputState && InputState)
        { result = 0; }
        else if (_previousInputState && !InputState)
        { result = 1; }
        _previousInputState = InputState;
        return result;
    }
}


public class InputHandler : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    private GameObject _playerComponentHolder; // object with all the movement components

    private CharacterMovementController _characterMovementController;
    private Interactor _playerInteract;

    [Header("Modify Movement Inputs")]
    [SerializeField] private bool toggleCrouch = false;
    [SerializeField] private bool toggleSprint = false;

    [Header("Bonus Movement Capabilities")]
    [SerializeField] private bool enableCrouch = false;
    [SerializeField] private bool enableSprint = false;

    #region InputHandleFunctions
    // 1 or -1
    private float _toggleMaskInput = 0;
    private void HandleToggleMask(float val)
    {
        _toggleMaskInput = val;

        EventManager.TriggerEvent(EventKey.MASK_INPUT, (int)val);
    }
    private bool _interactInput = false;

    private void HandleInteract(bool val)
    {
        _interactInput = val;
        if (val) _playerInteract?.InteractWithObject();
    }

    private void HandleEscape(bool val)
    {
        Debug.Log("Escape");
        if (val)
        {
            EventManager.TriggerEvent(EventKey.OPEN_SCENE, 1);
        }
    }
    
    #endregion
    private void OnDisable()
    { UnassignInputs(); }
    private void OnEnable()
    { AssignInputs(); }

    private void GetMovementComponents()
    {
        if (_playerComponentHolder)
        {
            if (!_playerComponentHolder.TryGetComponent(out CharacterMovementController cmc)) return;
            _characterMovementController = cmc;
            
            //TODO: Get reference to MASK TOGGLE script here
            
            //TODO: Get reference to INTERACTION script here

            if (!_playerComponentHolder.TryGetComponent(out Interactor pi)) return;
            _playerInteract = pi;
            //GetExtraInputFeatureComponents();
        }
        else
        { Debug.LogError("ERROR: Missing component holder, no player object assigned."); }
    }

    public void AssignAndSetupPlayerCharacter(GameObject player = null)
    {
        if (player) { _playerComponentHolder = player; }

        if (!_playerComponentHolder) 
        {
            Debug.LogError("ERROR: Missing player character object"); 
            return; 
        }

        GetMovementComponents();

        if (!_characterMovementController)
        {
            Debug.LogError("ERROR: Missing primary movement component (Character Movement Controller).");
            return;
        }

        SetUpCapabilities();
    }

    private void SetUpCapabilities()
    {
        _characterMovementController.SetCapabilities(
            inputReader,
            enableSprint, 
            enableCrouch,
            toggleSprint,
            toggleCrouch);
        
        if (TryGetComponent(out CameraController cameraController))
        {
            cameraController.Initialize(
                inputReader, 
                _characterMovementController, 
                _characterMovementController.GetCameraPoint());
            
            _playerInteract.SetCapabilities(cameraController.GetCameraObject());
        }
        else
        {
            Debug.LogError("ERROR: Cannot get CameraController component.");
        }
        
        // For disabling unused scripts
        //SetupExtraInputFeatureCapabilities();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void ResetInputValues()
    {
        _characterMovementController.ResetInputValues();
    }
    
    private void AssignInputs()
    {
        inputReader.ToggleMaskEvent += HandleToggleMask;
        inputReader.InteractEvent += HandleInteract;
        inputReader.EscapeEvent += HandleEscape;
        
        AssignExtraInputFeatures();
    }
    private void UnassignInputs()
    {
        inputReader.ToggleMaskEvent -= HandleToggleMask;
        inputReader.InteractEvent -= HandleInteract;
        inputReader.EscapeEvent -= HandleEscape;
        
        UnassignExtraInputFeatures();

        ResetInputValues();
    }
    
    #region ExtraInputFeatures
    // TODO: Need to figure out alternate means for input reading these
    
    [Header("Extra Input Features")]
    private SlidingController _slidingController;
    private ClimbingController _climbingController;
    private WallRunningController _wallRunningController;   // works but needs improvement
    private DashingController _dashingController;
    
    [SerializeField] private bool enableSlide = false;
    [SerializeField] private bool enableClimbing = false;
    [SerializeField] private bool enableWallRunning = false;
    [SerializeField] private bool enableDashing = false;

    private bool _upwardWallRun = false;
    private void HandleUpwardsWallRun(bool val)
    { _upwardWallRun = val; }
    private bool _downwardWallRun = false;
    private void HandleDownwardsWallRun(bool val)
    { _downwardWallRun = val; }
    private bool _slideInput = false;
    private void HandleSlide(bool val)
    { _slideInput = val; }
    private bool _dashInput = false;
    private void HandleDash(bool val)
    { _dashInput = val; }
    private bool _grappleInput = false;
    private void HandleGrapple(bool val)
    { _grappleInput = val; }
    private bool _swingInput = false;
    private void HandleSwing(bool val)
    { _swingInput = val; }
    private bool _alternateInput = false;
    private void HandleAlternate(bool val)
    { _alternateInput = val; }
    
    private void GetExtraInputFeatureComponents()
    {
        if (_playerComponentHolder.TryGetComponent(out SlidingController sc))
        { _slidingController = sc; }
        if (_playerComponentHolder.TryGetComponent(out ClimbingController cc))
        { _climbingController = cc; }
        if (_playerComponentHolder.TryGetComponent(out WallRunningController wrc))
        { _wallRunningController = wrc; }
        if (_playerComponentHolder.TryGetComponent(out DashingController dc))
        { _dashingController = dc; }
    }
    private void AssignExtraInputFeatures()
    {
        /*
        inputReader.DashEvent += HandleUpwardsWallRun;
        inputReader.SlideEvent += HandleDownwardsWallRun;
        inputReader.SlideEvent += HandleSlide;
        inputReader.DashEvent += HandleDash;
        */
    }
    private void UnassignExtraInputFeatures()
    {
        /*
        inputReader.DashEvent -= HandleUpwardsWallRun;
        inputReader.SlideEvent -= HandleDownwardsWallRun;
        inputReader.SlideEvent -= HandleSlide;
        inputReader.DashEvent -= HandleDash;
        */
    }
    private void SetupExtraInputFeatureCapabilities()
    {
        if (_slidingController) { _slidingController.enabled = enableSlide; }
        if (_climbingController) { _climbingController.enabled = enableClimbing; }
        if (_wallRunningController) { _wallRunningController.enabled = enableWallRunning; }
        if (_dashingController) 
        { 
            _dashingController.enabled = enableDashing; 
            // assign camera transform if enabled
            if (TryGetComponent(out CameraController cameraController))
            { _dashingController.SetPlayerCamera(cameraController.GetCameraObject().transform); }
        }
    }
    private void HandleExtraInputFeatures()
    {
        /*
        if (_slidingController && enableSlide)
        { _slidingController.HandlePlayerInputs(_slideInput, _movementInput); }

        if (_climbingController && enableClimbing)
        { _climbingController.HandlePlayerInputs(_movementInput, _jumpInput); }

        if (_wallRunningController && enableWallRunning)
        {
            _wallRunningController.HandlePlayerInputs(
                _movementInput, _upwardWallRun, _downwardWallRun, _jumpInput);
        }

        if (_dashingController && enableDashing)
        { _dashingController.HandlePlayerInputs(_movementInput, _dashInput); }
        */
    }
    #endregion
}
