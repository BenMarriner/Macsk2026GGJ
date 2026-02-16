using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.EventSystems;


public class CharacterMovementController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private Transform orientation;
    private float _horizontalMovement;
    private float _verticalMovement;

    private Vector3 _movementDirection;
    private Rigidbody _rigidbody;
    private InputReader _inputReader;

    public bool EnableSprint { get; private set; } = false;
    public bool EnableCrouch { get; private set; } = false;

    private bool _toggleSprint = false;
    private bool _toggleCrouch = false;

    #region MovementStateVariables
    [SerializeField] private MovementState state;

    public bool IsDashing { get; set; } = false;
    public bool IsSliding { get; set; } = false;
    public bool IsClimbing { get; set; } = false;
    public bool IsWallRunning { get; set; } = false;
    public bool IsExitingWall { get; set; } = false;
    public bool IsFrozen { get; set; } = false;
    public bool IsUnlimited { get; set; } = false;
    public bool Restricted { get; set; } = false;
    #endregion
    
    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
   
    [SerializeField] private float airMinSpeed;
    private float _moveSpeed;
    private float _desiredMoveSpeed;
    private float _lastDesiredMoveSpeed;
    public float MaxYSpeed { get; set; }

    [SerializeField] private float speedIncreaseMultiplier;
    [SerializeField] private float slopeIncreaseMultiplier;
    [SerializeField] private float groundDrag;

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown = 0.45f;
    [SerializeField] private float airMultiplier;
    [SerializeField] private int jumpLimit = 2; // number of times the player can jump
    private int _jumpCount;
    private bool _canJump = true;

    [Header("Crouching")]
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float crouchYScale;
    private float _startYScale;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject playerModel;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private float lengthOfCheck = -1.1f;
    [SerializeField, Range(0.001f, 3f)] private float groundCheckBoxSizeMultiplier = 0.8f;
    public float DefaultHeight { get; private set; }
    public float CurrentHeight { get; set; }
    private Vector3 _groundCheckBoxSize;
    public bool Grounded { get; private set; } = false;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle;
    private RaycastHit _slopeHit;
    private bool _exitingSlope;

    public void SetCapabilities(
        InputReader inputReader,
        bool sprint, 
        bool crouch,
        bool toggleSprint,
        bool toggleCrouch)
    {
        _inputReader = inputReader;
        EnableSprint = sprint;
        EnableCrouch = crouch;
        _toggleSprint = toggleSprint;
        _toggleCrouch = toggleCrouch;

        if (_inputReader)
        { AssignInputs(); }
    }

    [Header("Camera Position")]
    public Transform cameraPoint;
    
    [Header("Camera Integration")]
    private CameraController _cameraController;
    private bool _wasGrounded = false;
    private float _fallVelocity = 0f;

    [Header("SFX")]
    [SerializeField] private AudioSource _footstepAudioSource;
    
    public Transform GetCameraPoint()
    {
        if (cameraPoint)
        { return cameraPoint; }

        return null;
    }

    private void OnDrawGizmos()
    {
        float maxDistance = CurrentHeight * 0.5f + lengthOfCheck;
        /*
        RaycastHit hit;

        bool isHit = Physics.BoxCast(
            playerCollider.bounds.center,
            new Vector3(0.2f, 0.2f, 0.2f),
            Vector3.down,
            out hit,
            transform.rotation,
            maxDistance,
            groundLayer);

        if (isHit)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Vector3.down * hit.distance);
            Gizmos.DrawWireSphere(transform.position + (Vector3.down * maxDistance), groundCheckBoxSize.x / 2);
        }
        */
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * maxDistance);
        Gizmos.DrawWireSphere(transform.position + (Vector3.down * maxDistance), _groundCheckBoxSize.x / 2);

        // slope detection ray
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, Vector3.down * maxDistance);
        Gizmos.DrawWireSphere(transform.position + (Vector3.down * maxDistance), _groundCheckBoxSize.x / 2);
    }
    private void SetGroundCheckBoxSize()
    {
        DefaultHeight = 1.8f * 2f; // TODO: maybe not hard coded
        CurrentHeight = DefaultHeight;
        // Note: also used for Sphere Cast
        _groundCheckBoxSize = new Vector3(
            playerModel.transform.localScale.x * groundCheckBoxSizeMultiplier
            , 0.05f,
            playerModel.transform.localScale.z * groundCheckBoxSizeMultiplier);
    }
    private bool SphereCastCheck()
    {
        return Physics.SphereCast(
            transform.position,
            _groundCheckBoxSize.x / 2,
            Vector3.down, out RaycastHit hit,
            CurrentHeight * 0.5f + lengthOfCheck,
            groundLayer);
    }

    // Controls how the player moves up and down slopes
    public bool OnSlope()
    {
        if (!Physics.SphereCast(
                transform.position,
                _groundCheckBoxSize.x / 2,
                Vector3.down, out _slopeHit,
                CurrentHeight * 0.5f + lengthOfCheck)) return false;
        float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
        return angle < maxSlopeAngle && angle != 0f;

    }

    private bool _enableMovementOnNextTouch;

    private Vector3 _velocityToSet;

    private void SetVelocity()
    {
        _enableMovementOnNextTouch = true;
        _rigidbody.linearVelocity = _velocityToSet; 
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (!_enableMovementOnNextTouch) return;
        _enableMovementOnNextTouch = false;
    }

    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) + 
            Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }

    // Get the direct the player is moving on a slope
    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    { return Vector3.ProjectOnPlane(direction, _slopeHit.normal).normalized; }
    

    private void Start()
    {
        SetGroundCheckBoxSize();
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;
        _startYScale = playerModel.transform.localScale.y;

        _canJump = true;
        
    }

    #region InputHandling

    private Vector2 _movementInput;
    private bool _jumpInput;
    private bool _sprintInput;
    private bool _crouchInput;

    private void OnMove(Vector2 val)
    {
        if (IsUnlimited)
        {
            _movementInput = Vector2.zero;
            return;
        }

        _movementInput = val;
    }

    private void OnJump(bool val)
    {
        if (IsUnlimited)
        {
            _jumpInput = false;
            return;
        }

        _jumpInput = val;
    }

    private void OnSprint(bool val)
    {
        if (IsUnlimited)
        {
            _sprintInput = false;
            return;
        }
        
        switch (_toggleSprint)
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
    
    private void OnCrouch(bool val)
    {
        if (IsUnlimited)
        {
            _crouchInput = false;
            return;
        }
            
        switch (_toggleCrouch)
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

    private void AssignInputs()
    {
        _inputReader.MoveEvent += OnMove;
        _inputReader.JumpEvent += OnJump;
        _inputReader.CrouchEvent += OnCrouch;
        _inputReader.SprintEvent += OnSprint;
    }
    
    private void UnassignInputs()
    {
        _inputReader.MoveEvent -= OnMove;
        _inputReader.JumpEvent -= OnJump;
        _inputReader.CrouchEvent -= OnCrouch;
        _inputReader.SprintEvent -= OnSprint;
    }

    public void ResetInputValues()
    {
        _movementInput = Vector2.zero;
        _sprintInput = false;
    }

    #endregion

    private void OnEnable()
    {
        if (_inputReader)
        { AssignInputs(); }
    }
    
    private void OnDisable()
    {
        if (_inputReader)
        { UnassignInputs(); }
    }
    
    private void Update()
    {
        Grounded = SphereCastCheck();
        
        // Landing detection for camera effects
        DetectLanding();

        HandleMovement();
        HandleJump();
        HandleCrouch();

        SpeedControl();

        StateHandler();

        GroundDrag();
    }
    
    /// <summary>
    /// Detects landing events and notifies the camera controller.
    /// Tracks fall velocity and triggers camera landing effect when player lands.
    /// </summary>
    private void DetectLanding()
    {
        // Track fall velocity when in the air
        if (!Grounded && _rigidbody)
        {
            // Store the downward velocity (negative Y velocity)
            float currentFallVelocity = -_rigidbody.linearVelocity.y;
            if (currentFallVelocity > _fallVelocity)
            {
                _fallVelocity = currentFallVelocity;
            }
        }
        
        // Detect landing: transition from not grounded to grounded
        if (Grounded && !_wasGrounded)
        {
            // Player just landed
            if (_cameraController && _fallVelocity > 0f)
            {
                // Call camera controller with impact velocity
                _cameraController.OnLanding(_fallVelocity);
            }
            
            // Reset fall velocity
            _fallVelocity = 0f;
        }
        
        // Update previous grounded state for next frame
        _wasGrounded = Grounded;
    }

    private bool _keepMomentum;
    private MovementState _lastState;

    private void StateHandler()
    { 
        // Mode - Freeze
        if (IsFrozen)
        { 
            state = MovementState.Freeze; 
            _rigidbody.linearVelocity = Vector3.zero;
            _desiredMoveSpeed = 0f;
        }
        // Mode - Unlimited
        else if (IsUnlimited)
        { 
            state = MovementState.Unlimited;
            _desiredMoveSpeed = 999f;
        }
        // Mode - Dashing
        else if (IsDashing)
        {
            state = MovementState.Dashing;
            _desiredMoveSpeed = dashSpeed;
            _speedChangeFactor = dashSpeedChangeFactor;
        }
        // Mode - Climbing
        else if (IsClimbing)
        {
            state = MovementState.Climbing;
            _desiredMoveSpeed = climbSpeed;
        }
        // Mode - Wall Running
        else if (IsWallRunning)
        {
            state = MovementState.WallRunning;
            _desiredMoveSpeed = wallRunSpeed;
        }
        // Mode - Sliding
        else if (IsSliding)
        {
            state = MovementState.Sliding;
            if (OnSlope() && _rigidbody.linearVelocity.y < 0.1f)
            { 
                _desiredMoveSpeed = slideSpeed; 
                _keepMomentum = true;
            }
            else
            { _desiredMoveSpeed = sprintSpeed; }
        }
        // Mode - Crouch
        else if (EnableCrouch && _crouchInput)
        {
            state = MovementState.Crouching;
            _desiredMoveSpeed = crouchSpeed;
        }
        // Mode - Sprinting
        else if (EnableSprint && Grounded && _sprintInput)
        {
            state = MovementState.Sprinting;
            _desiredMoveSpeed = sprintSpeed;
        }
        // Mode - Walk
        else if (Grounded)
        {
            state = MovementState.Walking;
            _desiredMoveSpeed = walkSpeed;
        }
        // Mode - Air
        else
        {
            state = MovementState.Air;

            if (_moveSpeed < airMinSpeed)
                _desiredMoveSpeed = airMinSpeed;
        }

        bool desiredMoveSpeedHasChanged = 
            !Mathf.Approximately(_desiredMoveSpeed, _lastDesiredMoveSpeed);

        if (_lastState == MovementState.Dashing)
        { _keepMomentum = true; }

        if (desiredMoveSpeedHasChanged)
        {
            if (_keepMomentum)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                StopAllCoroutines();
                _moveSpeed = _desiredMoveSpeed;
            }
        }

        _lastDesiredMoveSpeed = _desiredMoveSpeed;
        _lastState = state;

        if (Mathf.Abs(_desiredMoveSpeed - _moveSpeed) < 0.1f)
        { _keepMomentum = false; }
    }

    private float _speedChangeFactor;
    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float difference = Mathf.Abs(_desiredMoveSpeed - _moveSpeed);
        float startValue = _moveSpeed;

        float boostFactor = _speedChangeFactor;

        while (time < difference)
        {
            _moveSpeed = Mathf.Lerp(startValue, _desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, _slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * boostFactor * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
            { time += Time.deltaTime * boostFactor; }
            
            yield return null;
        }

        _speedChangeFactor = speedIncreaseMultiplier;
        _moveSpeed = _desiredMoveSpeed;
        _keepMomentum = false;
    }

    private void HandleMovement()
    {
        _horizontalMovement = _movementInput.x;
        _verticalMovement = _movementInput.y;

        if ( state == MovementState.Walking && 
            (_verticalMovement != 0 || _horizontalMovement != 0) && 
            _rigidbody.linearVelocity.sqrMagnitude > 0.1f)
        {
            if (!_footstepAudioSource.isPlaying)
            {
                _footstepAudioSource.Play();
            }
        }
        else if (_footstepAudioSource.isPlaying)
        {
            _footstepAudioSource.Stop();
        }
    }

    private bool _stillCrouching = false;

    private void HandleCrouch()
    {
        if (!EnableCrouch) { return; }
        
        if (_crouchInput && !_stillCrouching)
        {
            // Forces the player character to the ground

            // Modify character model (NOTE: may get swapped with an animation)
            playerModel.transform.localScale =
                new Vector3(
                    playerModel.transform.localScale.x,
                    crouchYScale,
                    playerModel.transform.localScale.z);

            CurrentHeight *= crouchYScale + 0.25f;

            _rigidbody.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            _stillCrouching = true;
        }
        else if (!_crouchInput && _stillCrouching)
        {
            playerModel.transform.localScale =
               new Vector3(
                   playerModel.transform.localScale.x,
                   _startYScale,
                   playerModel.transform.localScale.z);

            CurrentHeight = DefaultHeight;

            _stillCrouching = false;
        }
    }

    private void HandleJump()
    {
        if (_jumpInput && _canJump)
        {
            if (Grounded || (!Grounded && _jumpCount < jumpLimit))
            {
                if (!Grounded && _jumpCount == 0)
                { 
                    if (jumpLimit <= 1) { return; }
                    _jumpCount++; 
                }

                _canJump = false;
                Jump();
                _jumpCount++;
                Invoke(nameof(ResetJump), jumpCooldown);
            }
        }

        TryResetJumpLimit();
    }

    public void UpdateOrientationRotation(float yRotation)
    { orientation.rotation = Quaternion.Euler(0, yRotation, 0); }

    // State exposure methods for CameraController integration
    public MovementState GetCurrentState()
    { return state; }
    
    public Vector3 GetVelocity()
    { return _rigidbody ? _rigidbody.linearVelocity : Vector3.zero; }
    
    /// <summary>
    /// Sets the camera controller reference for landing detection.
    /// </summary>
    public void SetCameraController(CameraController cameraController)
    {
        _cameraController = cameraController;
    }

    private void UpdateMovementDirection()
    { 
        _movementDirection = 
            (orientation.forward * _verticalMovement) + 
            (orientation.right * _horizontalMovement); 
    }

    private void MoveCharacter()
    {
        if (state == MovementState.Dashing)
        { return; }

        UpdateMovementDirection();
        
        // on slope
        if (OnSlope() && !_exitingSlope)
        {
            _rigidbody.AddForce(GetSlopeMoveDirection(_movementDirection) * (_moveSpeed * 20f * _rigidbody.mass), ForceMode.Force);

            if (_rigidbody.linearVelocity.y > 0)
            { _rigidbody.AddForce(Vector3.down * (80f * _rigidbody.mass), ForceMode.Force); }
        }
        // on ground
        else if (Grounded)
        { _rigidbody.AddForce(_movementDirection.normalized * (_moveSpeed * 10f * _rigidbody.mass), ForceMode.Force); }
        // in air
        else if (!Grounded)
        { _rigidbody.AddForce(_movementDirection.normalized * (_moveSpeed * 10f * _rigidbody.mass * airMultiplier), ForceMode.Force); }

        // Turn off gravity when on slopes
        if (!IsWallRunning) { _rigidbody.useGravity = !OnSlope(); }
    }
    /*
    // Rate in which the agent slows down to a stop when there is no movement input
    [SerializeField, Range(0.01f, 1f)] private float decelerationRate = 0.5f;    // Must be between 0 and 1

    // Reduces the velocity of the game object to zero over time
    private void DecelerateVelocity()
    {
        rb.linearVelocity = rb.linearVelocity * decelerationRate * Time.deltaTime;
        rb.angularVelocity = rb.angularVelocity * decelerationRate * Time.deltaTime;
    }
    */
    private void SpeedControl()
    {
        if (OnSlope() && !_exitingSlope)
        {
            if (_rigidbody.linearVelocity.magnitude > _moveSpeed)
            { _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _moveSpeed; }
        }
        else
        {
            Vector3 flatVel = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);

            if (flatVel.magnitude > _moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * _moveSpeed;
                _rigidbody.linearVelocity = new Vector3(limitedVel.x, _rigidbody.linearVelocity.y, limitedVel.z);
            }
        }

        if (MaxYSpeed != 0 && _rigidbody.linearVelocity.y > MaxYSpeed)
        { _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, MaxYSpeed, _rigidbody.linearVelocity.z); }
    }

    private void Jump()
    {
        _exitingSlope = true;

        _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(transform.up * (jumpForce * _rigidbody.mass), ForceMode.Impulse);
    }
    
    private void TryResetJumpLimit()
    {
        if (Grounded && _canJump)
        { _jumpCount = 0; }
    }

    private void ResetJump()
    {
        _canJump = true;
        _exitingSlope = false;
    }

    private void GroundDrag()
    {
        if (state is 
                MovementState.Walking or 
                MovementState.Sprinting or 
                MovementState.Crouching or 
                MovementState.Sliding && 
             !Mathf.Approximately(_rigidbody.linearDamping, groundDrag))
        { _rigidbody.linearDamping = groundDrag; }
        else
        { _rigidbody.linearDamping = 0; }
    }

    private void FixedUpdate()
    {
        if (IsExitingWall || Restricted) return;

        MoveCharacter();
    }
    
    [Header("Extra Movement Feature Variables")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashSpeedChangeFactor;
    [SerializeField] private float slideSpeed;
    [SerializeField] private float climbSpeed;
    [SerializeField] private float wallRunSpeed;
}
