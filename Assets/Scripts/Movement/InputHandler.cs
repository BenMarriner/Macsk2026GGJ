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
    [SerializeField] private GameObject playerCamera;
    private Transform _cameraFollowPoint;
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

    private float _xRotation;
    private float _yRotation;

    private bool _inputEnabled = false;

    private Vector2 _movementInput = Vector2.zero;
    private void HandleMove(Vector2 val)
    { _movementInput = val; }
    private Vector2 _lookInput = Vector2.zero;
    private void HandleLook(Vector2 val)
    { _lookInput = val; }
    private bool _jumpInput = false;
    private void HandleJump(bool val)
    { _jumpInput = val;}
    private bool _sprintInput = false;
    private void HandleSprint(bool val)
    {
        switch (toggleSprint)
        {
            case false:
                _sprintInput = val;
                return;
            case true when !val:
                return;
            default:
                _sprintInput = !_sprintInput;
                break;
        }
    }
    private bool _crouchInput = false;
    private void HandleCrouch(bool val)
    {
        switch (toggleCrouch)
        {
            case false:
                _crouchInput = val;
                return;
            case true when !val:
                return;
            default:
                _crouchInput = !_crouchInput;
                break;
        }
    }
    
    // 1 or -1
    private float _toggleMaskInput = 0;
    private void HandleToggleMask(float val)
    {
        _toggleMaskInput = val;

        EventManager.TriggerEvent(EventKey.MASK_INPUT, (int)val);
        
        // if (val != 0) Debug.Log(_toggleMaskInput);
    }
    private bool _interactInput = false;

    private void HandleInteract(bool val)
    {
        _interactInput = val;
        
        if (val) _playerInteract?.InteractWithObject();
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

        _cameraFollowPoint = _characterMovementController.GetCameraPoint();

        if (!_cameraFollowPoint)
        {
            Debug.LogError("ERROR: Missing camera follow point, camera can't follow the player.");
            return;
        }

        SetUpCapabilities();
    }

    private void SetUpCapabilities()
    {
        _characterMovementController.SetCapabilities(
            enableSprint, enableCrouch);
        
        //TODO: Setup capabilities for MASK TOGGLE script here
        
        //TODO: Setup capabilities for INTERACTION script here
        
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
        _movementInput = Vector2.zero;
        _sprintInput = false;
    }
    private void AssignInputs()
    {
        //Debug.Log("Assigning inputs");
        _inputEnabled = true;
        inputReader.MoveEvent += HandleMove;
        inputReader.LookEvent += HandleLook;
        inputReader.JumpEvent += HandleJump;
        inputReader.CrouchEvent += HandleCrouch;
        inputReader.SprintEvent += HandleSprint;
        inputReader.ToggleMaskEvent += HandleToggleMask;
        inputReader.InteractEvent += HandleInteract;

        AssignExtraInputFeatures();
    }
    private void UnassignInputs()
    {
        //Debug.Log("Unassigning inputs");
        _inputEnabled = false;
        inputReader.MoveEvent -= HandleMove;
        inputReader.LookEvent -= HandleLook;
        inputReader.JumpEvent -= HandleJump;
        inputReader.CrouchEvent -= HandleCrouch;
        inputReader.SprintEvent -= HandleSprint;
        inputReader.ToggleMaskEvent -= HandleToggleMask;
        inputReader.InteractEvent -= HandleInteract;
        
        UnassignExtraInputFeatures();

        ResetInputValues();
    }

    private void CameraControl()
    {
        _yRotation += _lookInput.x * inputReader.mouseSensitivityX * Time.deltaTime;
        _xRotation -= _lookInput.y * inputReader.mouseSensitivityY * Time.deltaTime;

        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
    }
    private void CameraMove()
    {
        transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0);
        // Controls character body face rotation
        if (!_characterMovementController) return;
        _characterMovementController.UpdateOrientationRotation(_yRotation);
        
        //TODO: Feed Input data for MASK TOGGLE script through here
        
        //TODO: Feed Input data for INTERACTION script through here

        
       
        
        //_characterMovementController.UpdateGrappleOrientationRotation(_xRotation, _yRotation);
    }

    private void UpdateCameraFollowPoint()
    {
        if (!_cameraFollowPoint) { return; }
        transform.position = _cameraFollowPoint.position;
    }
    private void MovementControl()
    {
        if (!_characterMovementController) return;
        _characterMovementController.HandlePlayerInputs(
            _movementInput, _jumpInput, _sprintInput, _crouchInput);
            
        
        

        //HandleExtraInputFeatures();
    }

    private void Update()
    {
        if (_inputEnabled)
        { MovementControl(); }
    }

    private void LateUpdate()
    {
        if (_inputEnabled)
        {
            CameraControl();
            CameraMove();
        }
        UpdateCameraFollowPoint();
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
            _dashingController.SetPlayerCamera(playerCamera.transform);
        }
    }
    private void HandleExtraInputFeatures()
    {
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
    }
    #endregion
}
