using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Source for class: https://discussions.unity.com/t/checking-if-a-layer-is-in-a-layer-mask/860331/7
public class LayerMaskCheck
{
    public static bool IsInLayerMask(GameObject obj, LayerMask mask) => (mask.value & (1 << obj.layer)) != 0;
    public static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
}

// TODO: fix the wall running limit for the same wall and 
// the wall run timer doesn't account for resetting on the same wall
public class WallRunningController : MonoBehaviour
{
    [Header("Wall Running")]
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallJumpUpForce;
    [SerializeField] private float wallJumpSideForce;
    [SerializeField] private float wallClimbSpeed;
    [SerializeField] private float maxWallRunTime;
    [SerializeField] private bool allowVerticalClimbing;
    private float _wallRunTimer;

    private bool _upwardsRunning;
    private bool _downwardsRunning;
    private Vector2 _movementInputs;
    private InputDetector _jumpInput;

    [Header("Detection")]
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private float minJumpHeight;
    private RaycastHit _leftWallHit;
    private RaycastHit _rightWallHit;
    private bool _wallLeft;
    private bool _wallRight;

    [Header("Exiting")]
    [SerializeField] private float exitWallTime;
    private bool _exitingWall;
    private float _exitWallTimer;

    [Header("Gravity")]
    [SerializeField] private bool useGravity;
    [SerializeField, Range(.01f, 9.81f)] private float gravityCounterForce;

    [Header("References")]
    [SerializeField] private Transform orientation;
    private CharacterMovementController _characterMovementController;
    private Rigidbody _rigidbody;

    private void Start()
    {
        _characterMovementController = GetComponent<CharacterMovementController>();
        _rigidbody = GetComponent<Rigidbody>();
        
    }

    private void CheckForWall()
    { 
        _wallRight = Physics.Raycast(transform.position, orientation.right, out _rightWallHit, wallCheckDistance, whatIsWall);
        _wallLeft = Physics.Raycast(transform.position, -orientation.right, out _leftWallHit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround()
    { return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround); }

    // WARNING: Does not account for which face/side of the wall you are running on, just the object
    // (bad if two different walls are the same object) consider this: https://discussions.unity.com/t/checking-what-face-an-object-collided-with/516678/4
    private void OnCollisionEnter(Collision collision)
    {
        if (LayerMaskCheck.IsInLayerMask(collision.collider.gameObject, whatIsGround))
        { _lastWall = null; }
    }

    private GameObject _lastWall;

    private GameObject GetCurrentWall()
    {
        GameObject wall = null;

        if (_wallRight)
        { wall = _rightWallHit.transform.gameObject; }
        else if (_wallLeft)
        { wall = _leftWallHit.transform.gameObject; }

        return wall;
    }

    private void SetLastWall()
    {
        if (_wallRight)
        { _lastWall = _rightWallHit.transform.gameObject; }
        else if (_wallLeft)
        { _lastWall = _leftWallHit.transform.gameObject; }
        else
        { _lastWall = null; }
    }

    private bool IsNewWall(GameObject wall)
    { return wall != _lastWall; }


    private void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void FixedUpdate()
    {
        if (_characterMovementController.isWallRunning)
        { WallRunningMovement(); }
    }

    public void HandlePlayerInputs(Vector2 movement, bool upward, bool downward, bool jump)
    {
        _upwardsRunning = upward;
        _downwardsRunning = downward;
        _jumpInput.InputState = jump;
        _movementInputs = movement;
    }

    private void StateMachine()
    {
        int inputStateChange = _jumpInput.HasStateChanged();
        
        // State 1 - Wall running
        if ((_wallLeft || _wallRight) && 
            _movementInputs.y > 0 && 
            AboveGround() && 
            !_exitingWall && 
            (IsNewWall(GetCurrentWall()) || _characterMovementController.isWallRunning)) 
        {
            if (!_characterMovementController.isWallRunning)
            { StartWallRun(); }
            
            if (_wallRunTimer > 0)
            { _wallRunTimer -= Time.deltaTime; }

            if (_wallRunTimer <= 0 && _characterMovementController.isWallRunning)
            { 
                _exitingWall = true;
                _exitWallTimer = exitWallTime;
            }

            if (inputStateChange == 0)
            { 
                WallJump(true);
                inputStateChange = -1;
            }
        }
        // State 2 - Exiting
        else if (_exitingWall)
        {
            if (_characterMovementController.isWallRunning)
            { StopWallRun(); }

            if (_exitWallTimer > 0)
            { _exitWallTimer -= Time.deltaTime; }

            if (_exitWallTimer <= 0)
            { _exitingWall = false; }

            if ((_wallLeft || _wallRight) && AboveGround())
            {
                if (inputStateChange == 0)
                {
                    WallJump();
                    inputStateChange = -1; // prevents repeatedly triggering jumps
                }
            }
        }
        // State 3 - None
        else
        {
            if (_characterMovementController.isWallRunning)
            { StopWallRun(); }
        }

        if ((_wallLeft || _wallRight) && AboveGround())
        {
            if (inputStateChange == 0)
            {
                WallJump();
                inputStateChange = -1; // prevents repeatedly triggering jumps
            }
        }
    }

    private void StartWallRun()
    {
        _characterMovementController.isWallRunning = true;
        SetLastWall();
        _wallRunTimer = maxWallRunTime;
        _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
    }

    private void StopWallRun()
    {
        _characterMovementController.isWallRunning = false;
    }

    private void WallRunningMovement()
    {
        _rigidbody.useGravity = useGravity;
        
        Vector3 wallNormal = _wallRight ? _rightWallHit.normal : _leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
        { wallForward = -wallForward; }

        // Forward force
        _rigidbody.AddForce(wallForward * (wallRunForce * _rigidbody.mass), ForceMode.Force);

        if (allowVerticalClimbing)
        {
            if (_upwardsRunning)
            { _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, wallClimbSpeed, _rigidbody.linearVelocity.z); }
            if (_downwardsRunning)
            { _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, -wallClimbSpeed, _rigidbody.linearVelocity.z); }
        }
        
        if (!(_wallLeft && _movementInputs.x > 0) && !(_wallRight && _movementInputs.x > 0))
        { _rigidbody.AddForce(-wallNormal * (100f * _rigidbody.mass), ForceMode.Force); }

        // weaken gravity
        if (useGravity)
        { _rigidbody.AddForce(transform.up * (gravityCounterForce * _rigidbody.mass), ForceMode.Force); }
    }
    

    private void WallJump(bool leavingWall = false)
    {
        if (!_wallRight && !_wallLeft) { return; }

        if (leavingWall)
        {
            _exitingWall = true;
            _exitWallTimer = exitWallTime;
        }
        
        Vector3 wallNormal = _wallRight ? _rightWallHit.normal : _leftWallHit.normal;
        Vector3 forceToApply = (transform.up * wallJumpUpForce) + (wallNormal * wallJumpSideForce);
        
        _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(forceToApply * _rigidbody.mass, ForceMode.Impulse);
    }
}
