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

    public bool enableSprint { get; private set; } = false;
    public bool enableCrouch { get; private set; } = false;

    #region MovementStateVariables
    [SerializeField] private MovementState state;
    public enum MovementState
    {
        Freeze,
        Unlimited,
        Walking,
        Sprinting,
        WallRunning,
        Grappling,
        Swinging,
        Crouching,
        Dashing,
        Climbing,
        Sliding,
        Air
    }

    public bool isDashing { get; set; } = false;
    public bool isSliding { get; set; } = false;
    public bool isClimbing { get; set; } = false;
    public bool isWallRunning { get; set; } = false;
    public bool isExitingWall { get; set; } = false;
    public bool isFrozen { get; set; } = false;
    public bool isUnlimited { get; set; } = false;
    public bool restricted { get; set; } = false;
    public bool activeSwinging { get; set; } = false;
    #endregion
    
    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
   
    [SerializeField] private float airMinSpeed;
    private float _moveSpeed;
    private float _desiredMoveSpeed;
    private float _lastDesiredMoveSpeed;
    public float maxYSpeed { get; set; }

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
    public float defaultHeight { get; private set; }
    public float currentHeight { get; set; }
    private Vector3 _groundCheckBoxSize;
    public bool grounded { get; private set; } = false;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle;
    private RaycastHit _slopeHit;
    private bool _exitingSlope;

    public void SetCapabilities(
        bool sprint, 
        bool crouch)
    {
        enableSprint = sprint;
        enableCrouch = crouch;
    }

    [Header("Camera Position")]
    public Transform cameraPoint;
    public Transform playerCameraTransform { get; private set; }
    
    public Transform GetCameraPoint()
    {
        if (cameraPoint)
        { return cameraPoint; }

        return null;
    }

    private void OnDrawGizmos()
    {
        float maxDistance = currentHeight * 0.5f + lengthOfCheck;
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
        defaultHeight = 1.8f * 2f; // TODO: maybe not hard coded
        currentHeight = defaultHeight;
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
            currentHeight * 0.5f + lengthOfCheck,
            groundLayer);
    }

    // Controls how the player moves up and down slopes
    public bool OnSlope()
    {
        if (!Physics.SphereCast(
                transform.position,
                _groundCheckBoxSize.x / 2,
                Vector3.down, out _slopeHit,
                currentHeight * 0.5f + lengthOfCheck)) return false;
        float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
        return angle < maxSlopeAngle && angle != 0f;

    }

    private bool _enableMovementOnNextTouch;

    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;

        _velocityToSet = CalculateJumpVelocity(
            transform.position, targetPosition, trajectoryHeight);
        Invoke(nameof(SetVelocity), 0.1f);

        Invoke(nameof(ResetRestrictions), 3f);
    }

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
        ResetRestrictions();
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

    private Vector2 _movementInput;
    private bool _jumpInput;
    private bool _sprintInput;
    private bool _crouchInput;

    // called every frame
    public void HandlePlayerInputs(
        Vector2 moving, bool jumping, bool sprinting, bool crouching)
    {
        if (!isUnlimited)
        {
            _movementInput = moving;
            _jumpInput = jumping;
            _sprintInput = sprinting;
            _crouchInput = crouching;
            
            return;
        }
        _movementInput = Vector2.zero;
        _jumpInput = false;
        _sprintInput = false;
        _crouchInput = false;
    }

    private void Update()
    {
        grounded = SphereCastCheck();

        HandleMovement();
        HandleJump();
        HandleCrouch();

        SpeedControl();

        StateHandler();

        GroundDrag();
    }

    private bool _keepMomentum;
    private MovementState _lastState;

    private void StateHandler()
    { 
        // Mode - Freeze
        if (isFrozen)
        { 
            state = MovementState.Freeze; 
            _rigidbody.linearVelocity = Vector3.zero;
            _desiredMoveSpeed = 0f;
        }
        // Mode - Unlimited
        else if (isUnlimited)
        { 
            state = MovementState.Unlimited;
            _desiredMoveSpeed = 999f;
        }
        // Mode - Grappling
        else if (activeGrapple)
        {
            state = MovementState.Grappling;
            _desiredMoveSpeed = grappleSpeed;
        }
        // Mode - Swinging
        else if (activeSwinging)
        {
            state = MovementState.Swinging;
            _desiredMoveSpeed = swingSpeed;
        }
        // Mode - Dashing
        else if (isDashing)
        {
            state = MovementState.Dashing;
            _desiredMoveSpeed = dashSpeed;
            _speedChangeFactor = dashSpeedChangeFactor;
        }
        // Mode - Climbing
        else if (isClimbing)
        {
            state = MovementState.Climbing;
            _desiredMoveSpeed = climbSpeed;
        }
        // Mode - Wall Running
        else if (isWallRunning)
        {
            state = MovementState.WallRunning;
            _desiredMoveSpeed = wallRunSpeed;
        }
        // Mode - Sliding
        else if (isSliding)
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
        else if (enableCrouch && _crouchInput)
        {
            state = MovementState.Crouching;
            _desiredMoveSpeed = crouchSpeed;
        }
        // Mode - Sprinting
        else if (enableSprint && grounded && _sprintInput)
        {
            state = MovementState.Sprinting;
            _desiredMoveSpeed = sprintSpeed;
        }
        // Mode - Walk
        else if (grounded)
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
    }

    private bool _stillCrouching = false;

    private void HandleCrouch()
    {
        if (!enableCrouch) { return; }
        
        if (_crouchInput && !_stillCrouching)
        {
            // Forces the player character to the ground

            // Modify character model (NOTE: may get swapped with an animation)
            playerModel.transform.localScale =
                new Vector3(
                    playerModel.transform.localScale.x,
                    crouchYScale,
                    playerModel.transform.localScale.z);

            currentHeight *= crouchYScale + 0.25f;

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

            currentHeight = defaultHeight;

            _stillCrouching = false;
        }
    }

    private void HandleJump()
    {
        if (_jumpInput && _canJump)
        {
            if (grounded || (!grounded && _jumpCount < jumpLimit))
            {
                if (!grounded && _jumpCount == 0)
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



    private void UpdateMovementDirection()
    { 
        _movementDirection = 
            (orientation.forward * _verticalMovement) + 
            (orientation.right * _horizontalMovement); 
    }

    private void MoveCharacter()
    {
        if (state == MovementState.Dashing || 
            activeGrapple || activeSwinging)
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
        else if (grounded)
        { _rigidbody.AddForce(_movementDirection.normalized * (_moveSpeed * 10f * _rigidbody.mass), ForceMode.Force); }
        // in air
        else if (!grounded)
        { _rigidbody.AddForce(_movementDirection.normalized * (_moveSpeed * 10f * _rigidbody.mass * airMultiplier), ForceMode.Force); }

        // Turn off gravity when on slopes
        if (!isWallRunning) { _rigidbody.useGravity = !OnSlope(); }
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
        if (activeGrapple) { return; }

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

        if (maxYSpeed != 0 && _rigidbody.linearVelocity.y > maxYSpeed)
        { _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, maxYSpeed, _rigidbody.linearVelocity.z); }
    }

    private void Jump()
    {
        _exitingSlope = true;

        _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(transform.up * (jumpForce * _rigidbody.mass), ForceMode.Impulse);
    }
    
    private void TryResetJumpLimit()
    {
        if (grounded && _canJump)
        { _jumpCount = 0; }
    }

    private void ResetJump()
    {
        _canJump = true;
        _exitingSlope = false;
    }

    private void GroundDrag()
    {
        if ((state == MovementState.Walking || 
            state == MovementState.Sprinting || 
            state == MovementState.Crouching ||
            state == MovementState.Sliding) && 
            !activeGrapple && !Mathf.Approximately(_rigidbody.linearDamping, groundDrag))
        { _rigidbody.linearDamping = groundDrag; }
        else
        { _rigidbody.linearDamping = 0; }
    }

    private void FixedUpdate()
    {
        if (isExitingWall || restricted) return;

        MoveCharacter();
    }
    
    /// <summary>
    /// Extra movement features section
    /// </summary>
    
    [Header("Extra Movement Feature Variables")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashSpeedChangeFactor;
    [SerializeField] private float slideSpeed;
    [SerializeField] private float climbSpeed;
    [SerializeField] private float wallRunSpeed;
    [SerializeField] private float swingSpeed;
    
    [Header("Grapple Movement Feature Variables")]
    [SerializeField] private Transform grappleOrientation;
    [SerializeField] private float grappleSpeed;
    public bool activeGrapple { get; set; } = false;
    
    public void UpdateGrappleOrientationRotation(float x, float y)
    { grappleOrientation.rotation = Quaternion.Euler(x, y, 0); }
    
    public void ResetRestrictions()
    { activeGrapple = false; }

}
