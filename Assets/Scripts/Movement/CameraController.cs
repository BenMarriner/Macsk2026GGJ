using UnityEngine;

/// <summary>
/// Dedicated camera controller that handles all camera transformations and effects.
/// Provides smooth, jitter-free camera movement with configurable rotation, position following, and optional effects.
/// </summary>
public class CameraController : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private new Camera camera;
    public GameObject GetCameraObject() { return camera.gameObject; }
    
    [Header("Rotation Settings")]
    [SerializeField] [Range(1f, 500f)] private float mouseSensitivityX = 170f;
    [SerializeField] [Range(1f, 500f)] private float mouseSensitivityY = 170f;
    [SerializeField] private bool enableRotationSmoothing = false;
    [SerializeField] [Range(0.01f, 100f)] private float rotationSmoothSpeed = 30f;

    [Header("Position Settings")]
    [SerializeField] private bool enablePositionSmoothing = true;
    [SerializeField] [Range(0.01f, 100f)] private float positionSmoothSpeed = 60f;

    [Header("Head Bob Settings")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] [Range(0f, 1f)] private float headBobAmplitude = 0.05f;
    [SerializeField] [Range(0.1f, 10f)] private float headBobFrequency = 1.5f;
    [SerializeField] [Range(0.01f, 100f)] private float headBobSmoothSpeed = 5f;

    [Header("Landing Impact Settings")]
    [SerializeField] private bool enableLandingImpact = true;
    [SerializeField] [Range(0f, 45f)] private float landingImpactIntensity = 5f;
    [SerializeField] [Range(0.01f, 100f)] private float landingRecoverySpeed = 8f;
    [SerializeField] [Range(0f, 50f)] private float minImpactVelocity = 5f;

    [Header("FOV Effect Settings")]
    [SerializeField] private bool enableFOVEffects = true;
    [SerializeField] [Range(1f, 179f)] private float defaultFOV = 60f;
    [SerializeField] [Range(1f, 179f)] private float sprintFOV = 70f;
    [SerializeField] [Range(1f, 179f)] private float slideFOV = 75f;
    [SerializeField] [Range(0.01f, 100f)] private float fovTransitionSpeed = 8f;

    #endregion

    #region Private State
    
    // Private references
    private InputReader _inputReader;
    private Transform _followPoint;
    private CharacterMovementController _movementController;
    
    // Rotation state
    private float _xRotation; // Pitch (vertical)
    private float _yRotation; // Yaw (horizontal)
    private float _targetXRotation;
    private float _targetYRotation;

    // Input storage
    private Vector2 _lookInput;

    // Position state
    private Vector3 _targetPosition;

    // Head bob state
    private float _headBobTimer;
    private float _currentHeadBobIntensity;

    // Landing impact state
    private float _currentLandingTilt;
    private bool _isRecoveringFromLanding;

    // FOV state
    private float _targetFOV;
    private float _currentFOV;

    // Movement state tracking
    private bool _isGrounded;
    private bool _isMoving;
    private float _currentSpeed;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Validate and clamp configuration values to valid ranges
        ValidateConfiguration();
        
        // Initialize FOV
        _currentFOV = defaultFOV;
        _targetFOV = defaultFOV;
        
        // Initialize target position to current position
        _targetPosition = transform.position;
        
        // Apply initial FOV if camera is available
        if (camera) 
        {
            camera.fieldOfView = _currentFOV;
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to input events if InputReader is already assigned
        if (_inputReader)
        {
            _inputReader.LookEvent += OnLookInput;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from input events to prevent memory leaks
        if (_inputReader)
        {
            _inputReader.LookEvent -= OnLookInput;
        }
    }

    private void LateUpdate()
    {
        // Respect Time.timeScale for pause support
        // When timeScale is 0 or negative, skip all camera updates
        if (Time.timeScale <= 0f)
        {
            return;
        }
        
        // All camera transformations happen in LateUpdate to ensure they occur after character movement
        UpdateRotation();
        UpdateHeadBob();
        UpdateLandingImpact();
        UpdateFOV();
        UpdatePosition();
    }

    private void AssignInputs()
    {
        if (_inputReader)
        {
            _inputReader.LookEvent += OnLookInput;
        }
        else
        {
            Debug.LogWarning("CameraController: InputReader reference is null! Camera will not respond to input.");
        }
    }
    
    /// <summary>
    /// Validates configuration values and clamps them to valid ranges.
    /// Called during Awake to ensure all parameters are within acceptable bounds.
    /// </summary>
    private void ValidateConfiguration()
    {
        // Clamp sensitivity values to reasonable ranges (0.1 to 1000)
        mouseSensitivityX = Mathf.Clamp(mouseSensitivityX, 0.1f, 1000f);
        mouseSensitivityY = Mathf.Clamp(mouseSensitivityY, 0.1f, 1000f);
        
        // Clamp smoothing speeds to positive values (0.01 to 100)
        rotationSmoothSpeed = Mathf.Clamp(rotationSmoothSpeed, 0.01f, 100f);
        positionSmoothSpeed = Mathf.Clamp(positionSmoothSpeed, 0.01f, 100f);
        headBobSmoothSpeed = Mathf.Clamp(headBobSmoothSpeed, 0.01f, 100f);
        landingRecoverySpeed = Mathf.Clamp(landingRecoverySpeed, 0.01f, 100f);
        fovTransitionSpeed = Mathf.Clamp(fovTransitionSpeed, 0.01f, 100f);
        
        // Clamp FOV values to Unity's valid range (1 to 179 degrees)
        defaultFOV = Mathf.Clamp(defaultFOV, 1f, 179f);
        sprintFOV = Mathf.Clamp(sprintFOV, 1f, 179f);
        slideFOV = Mathf.Clamp(slideFOV, 1f, 179f);
        
        // Clamp head bob amplitude to prevent excessive movement (0 to 1)
        headBobAmplitude = Mathf.Clamp(headBobAmplitude, 0f, 1f);
        headBobFrequency = Mathf.Clamp(headBobFrequency, 0.1f, 10f);
        
        // Clamp landing impact values
        landingImpactIntensity = Mathf.Clamp(landingImpactIntensity, 0f, 45f);
        minImpactVelocity = Mathf.Clamp(minImpactVelocity, 0f, 50f);
    }

    #endregion

    #region Input Handling

    private void OnLookInput(Vector2 lookDelta)
    {
        // Store input for processing in LateUpdate
        _lookInput = lookDelta;
    }

    #endregion

    #region Rotation

    private void UpdateRotation()
    {
        // Calculate target rotation from input
        float mouseX = _lookInput.x * mouseSensitivityX * Time.deltaTime;
        float mouseY = _lookInput.y * mouseSensitivityY * Time.deltaTime;

        // Update target rotations
        _targetYRotation += mouseX;
        _targetXRotation -= mouseY; // Inverted for natural mouse look

        // Clamp vertical rotation to prevent camera flipping
        _targetXRotation = Mathf.Clamp(_targetXRotation, -90f, 90f);

        // Apply rotation (with or without smoothing)
        if (enableRotationSmoothing)
        {
            // Smooth interpolation towards target using Quaternion.Slerp
            Quaternion currentRotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
            Quaternion targetRotation = Quaternion.Euler(_targetXRotation, _targetYRotation, 0f);
            Quaternion smoothedRotation = Quaternion.Slerp(currentRotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
            
            // Extract euler angles from smoothed rotation
            Vector3 smoothedEuler = smoothedRotation.eulerAngles;
            _xRotation = NormalizeAngle(smoothedEuler.x);
            _yRotation = NormalizeAngle(smoothedEuler.y);
        }
        else
        {
            // Direct assignment (no smoothing)
            _xRotation = _targetXRotation;
            _yRotation = _targetYRotation;
        }

        // Apply rotation to camera transform with landing tilt offset
        float finalXRotation = _xRotation + _currentLandingTilt;
        transform.localRotation = Quaternion.Euler(finalXRotation, _yRotation, 0f);

        // Propagate yaw rotation to player body
        if (_movementController)
        { _movementController.UpdateOrientationRotation(_yRotation); }
    }

    /// <summary>
    /// Normalize angle to -180 to 180 range for proper interpolation.
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;
        while (angle < -180f)
            angle += 360f;
        return angle;
    }

    #endregion

    #region Position

    private void UpdatePosition()
    {
        // Validate follow point reference
        if (!_followPoint)
        { return; }

        // Update target position from follow point
        _targetPosition = _followPoint.position;

        // Calculate head bob offset
        Vector3 headBobOffset = CalculateHeadBobOffset();

        // Apply position (with or without smoothing)
        if (enablePositionSmoothing)
        {
            // Smooth interpolation towards target position
            transform.position = Vector3.Lerp(
                transform.position, 
                _targetPosition + headBobOffset, 
                positionSmoothSpeed * Time.deltaTime);
        }
        else
        {
            // Direct assignment (no smoothing) - exactly match follow point
            transform.position = _targetPosition + headBobOffset;
        }
    }

    #endregion

    #region Head Bob

    private void UpdateHeadBob()
    {
        if (!enableHeadBob)
        {
            // When disabled, smoothly fade out head bob
            HeadBobFadeOut();
            return;
        }

        // Query movement state and velocity from CharacterMovementController
        if (_movementController)
        {
            // Query grounded status for head bob
            _isGrounded = _movementController.Grounded;

            // Query velocity for head bob intensity
            Vector3 velocity = _movementController.GetVelocity();
            // Calculate horizontal speed only (ignore vertical velocity)
            _currentSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            
            _isMoving = _currentSpeed > 0.1f;
        }

        // Disable head bob when not grounded (in air)
        if (!_isGrounded)
        {
            // Smoothly fade out head bob when in air
            HeadBobFadeOut();
            return;
        }

        // Scale head bob intensity based on movement speed
        if (_isMoving)
        {
            // Calculate target intensity based on speed
            // Normalize speed to a reasonable range (0-10 m/s typical walking/running speeds)
            float normalizedSpeed = Mathf.Clamp01(_currentSpeed / 10f);
            float targetIntensity = normalizedSpeed;

            // Smoothly fade in head bob when starting to move
            _currentHeadBobIntensity = Mathf.Lerp(_currentHeadBobIntensity, targetIntensity, headBobSmoothSpeed * Time.deltaTime);
            
            // Increment timer based on speed and frequency
            _headBobTimer += Time.deltaTime * headBobFrequency * _currentSpeed;
        }
        else
        {
            // Smoothly fade out head bob when stopping movement
            HeadBobFadeOut();
        }
    }

    private void HeadBobFadeOut()
    {
        _currentHeadBobIntensity = Mathf.Lerp(_currentHeadBobIntensity, 0f, headBobSmoothSpeed * Time.deltaTime);
    }

    private Vector3 CalculateHeadBobOffset()
    {
        if (!enableHeadBob || _currentHeadBobIntensity <= 0.001f)
        {
            return Vector3.zero;
        }

        // Calculate sinusoidal head bob
        float bobAmount = Mathf.Sin(_headBobTimer) * headBobAmplitude * _currentHeadBobIntensity;
        
        // Apply as vertical offset
        return new Vector3(0f, bobAmount, 0f);
    }

    #endregion

    #region Landing Impact

    private void UpdateLandingImpact()
    {
        // Skip if landing impact is disabled
        if (!enableLandingImpact)
        {
            // When disabled, ensure tilt is zero
            _currentLandingTilt = 0f;
            _isRecoveringFromLanding = false;
            return;
        }

        // Smoothly recover from landing impact
        if (_isRecoveringFromLanding)
        {
            // Interpolate landing tilt back to zero
            _currentLandingTilt = Mathf.Lerp(_currentLandingTilt, 0f, landingRecoverySpeed * Time.deltaTime);

            // Stop recovery when tilt is negligible
            if (Mathf.Abs(_currentLandingTilt) < 0.01f)
            {
                _currentLandingTilt = 0f;
                _isRecoveringFromLanding = false;
            }
        }
    }

    #endregion

    #region FOV Effects

    private void UpdateFOV()
    {
        // Skip if FOV effects are disabled
        if (!enableFOVEffects)
        {
            // When disabled, ensure FOV is at default
            _currentFOV = defaultFOV;
            _targetFOV = defaultFOV;
            
            if (camera)
            {
                camera.fieldOfView = _currentFOV;
            }
            return;
        }

        // Query movement state in LateUpdate for FOV
        if (_movementController)
        {
            MovementState currentState = _movementController.GetCurrentState();
            
            // Set target FOV based on movement state
            switch (currentState)
            {
                case MovementState.Sprinting:
                    _targetFOV = sprintFOV;
                    break;
                case MovementState.Sliding:
                    _targetFOV = slideFOV;
                    break;
                default:
                    _targetFOV = defaultFOV;
                    break;
            }
        }

        // Smoothly interpolate current FOV towards target FOV
        _currentFOV = Mathf.Lerp(_currentFOV, _targetFOV, fovTransitionSpeed * Time.deltaTime);

        // Apply FOV to camera
        if (camera)
        {
            camera.fieldOfView = _currentFOV;
        }
    }

    #endregion

    #region Public Interface

    /// <summary>
    /// Initialize the camera controller with required references.
    /// Unsubscribes from previous InputReader if any, then subscribes to the new one.
    /// </summary>
    public void Initialize(
        InputReader reader, 
        CharacterMovementController characterMovementController, 
        Transform cameraFollowPoint)
    {
        // Unsubscribe from previous InputReader if it exists
        if (_inputReader)
        {
            _inputReader.LookEvent -= OnLookInput;
        }
        
        // Assign new references
        _inputReader = reader;
        _followPoint = cameraFollowPoint;
        _movementController = characterMovementController;
        
        // Subscribe to new InputReader events
        AssignInputs();
        
        // Register this camera controller with the movement controller for landing detection
        if (!_movementController)
        {
            Debug.LogWarning("CameraController: Movement controller reference is null. Effects will not respond to movement state.");
            return;
        }
        
        _movementController.SetCameraController(this);
        
        // Validate references and log warnings for missing optional components
        if (!_followPoint)
        {
            Debug.LogWarning("CameraController: Follow point reference is null. Position tracking will not work.");
        }
    }

    /// <summary>
    /// Reset all camera effects and state to default values.
    /// Used when player dies, respawns, or when effects need to be cleared.
    /// </summary>
    public void ResetCamera()
    {
        // Clear head bob state
        _headBobTimer = 0f;
        _currentHeadBobIntensity = 0f;
        
        // Clear landing impact state
        _currentLandingTilt = 0f;
        _isRecoveringFromLanding = false;
        
        // Reset FOV to default
        _targetFOV = defaultFOV;
        _currentFOV = defaultFOV;
        
        // Clear input accumulation
        _lookInput = Vector2.zero;
        
        // Apply default FOV immediately if camera is available
        if (camera)
        {
            camera.fieldOfView = _currentFOV;
        }
    }

    /// <summary>
    /// Instantly snap camera to follow point position without interpolation.
    /// Used for teleports and respawns.
    /// </summary>
    public void SnapToFollowPoint()
    {
        if (_followPoint != null)
        {
            transform.position = _followPoint.position;
            _targetPosition = _followPoint.position; // Update target to prevent interpolation
        }
    }

    /// <summary>
    /// Called by movement system when player lands on ground.
    /// </summary>
    /// <param name="impactVelocity">The velocity of the landing impact</param>
    public void OnLanding(float impactVelocity)
    {
        if (!enableLandingImpact || impactVelocity < minImpactVelocity)
        { return; }
        // Calculate landing tilt based on impact velocity
        float normalizedImpact = Mathf.Clamp01(impactVelocity / 20f); // Normalize to 0-1 range
        _currentLandingTilt = normalizedImpact * landingImpactIntensity;
        _isRecoveringFromLanding = true;
    }

    /// <summary>
    /// Called by movement system when movement state changes.
    /// </summary>
    /// <param name="newState">The new movement state</param>
    public void OnMovementStateChanged(MovementState newState)
    {
        if (!enableFOVEffects)
        {
            return;
        }

        // Set target FOV based on movement state
        switch (newState)
        {
            case MovementState.Sprinting:
                _targetFOV = sprintFOV;
                break;
            case MovementState.Sliding:
                _targetFOV = slideFOV;
                break;
            default:
                _targetFOV = defaultFOV;
                break;
        }
    }

    #endregion
}
