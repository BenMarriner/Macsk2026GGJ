using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private LayerMask whatIsWall;
    private Rigidbody _rigidbody;
    private CharacterMovementController _characterMovementController;


    [Header("Climbing")]
    [SerializeField] private float climbSpeed;
    [SerializeField] private float maxClimbTime;
    
    private float _climbTimer;
    private bool _climbing;

    [Header("ClimbJumping")]
    [SerializeField] private float climbJumpUpForce;
    [SerializeField] private float climbJumpBackForce;
    [SerializeField] private int climbJumps;
    private int _climbJumpsLeft;

    [Header("Detection")]
    [SerializeField] private float detectionLength;
    [SerializeField] private float sphereCastRadius;
    [SerializeField] private float maxWallLookAngle;
    private float _wallLookAngle;

    private RaycastHit _frontWallHit;
    private bool _wallFront;

    private Transform _lastWall;
    private Vector3 _lastWallNormal;
    [SerializeField] private float minWallNormalAngleChange;

    [Header("Exiting")]
    [SerializeField] private float exitWallTime;
    private float _exitWallTimer;
    private bool _exitingWall;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _characterMovementController = GetComponent<CharacterMovementController>();
    }

    private bool _climbingInput;
    private InputDetector _jumpInput;

    public void HandlePlayerInputs(Vector2 moveInput, bool jump)
    {
        _climbingInput = moveInput.y > 0f;
        _jumpInput.InputState = jump;
    }

    private void Update()
    {
        WallCheck();
        StateMachine();

        if (_climbing && !_exitingWall)
        { ClimbingMovement(); }
    }

    private void StateMachine()
    {
        // State 0 - Ledge Grabbing EDIT: it has been removed

        // State 1 - Climbing
        if (_wallFront && _climbingInput && _wallLookAngle < maxWallLookAngle && !_exitingWall)
        {
            if (!_climbing & _climbTimer > 0)
            { StartClimbing(); }

            // timer
            if (_climbTimer > 0)
            { _climbTimer -= Time.deltaTime; }

            if (_climbTimer < 0)
            { StopClimbing(); }
        }
        // State 2 - Exiting
        else if (_exitingWall)
        {
            if (_climbing) { StopClimbing(); }
            if (_exitWallTimer > 0) { _exitWallTimer -= Time.deltaTime; }
            if (_exitWallTimer < 0) 
            { 
                _exitingWall = false;
                _characterMovementController.isExitingWall = false;
            }
        }
        // State 3 - None
        else
        {
            if (_climbing) { StopClimbing(); }
        }

        if (_wallFront && _jumpInput.HasStateChanged() == 0 && _climbJumpsLeft > 0) { ClimbJump(); }
    }

    private void WallCheck()
    {
        _wallFront = Physics.SphereCast(
            transform.position, 
            sphereCastRadius, 
            orientation.forward, 
            out _frontWallHit, 
            detectionLength, 
            whatIsWall);
        _wallLookAngle = Vector3.Angle(orientation.forward, -_frontWallHit.normal);

        bool newWall = _frontWallHit.transform != _lastWall || 
            Mathf.Abs(Vector3.Angle(_lastWallNormal, _frontWallHit.normal)) > minWallNormalAngleChange;

        if ((_wallFront && newWall) || _characterMovementController.grounded) 
        { 
            _climbTimer = maxClimbTime;
            _climbJumpsLeft = climbJumps;
        }
    }

    private void ClimbingMovement()
    {
        _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, climbSpeed, _rigidbody.linearVelocity.z);

        // sound effect
    }

    private void StartClimbing()
    {
        _climbing = true;
        _characterMovementController.isClimbing = true;

        _lastWall = _frontWallHit.transform;
        _lastWallNormal = _frontWallHit.normal;

        // change camera view
    }

    private void StopClimbing()
    {
        _climbing = false;
        _characterMovementController.isClimbing = false;
        // revert camera view
    }
    
    private void ClimbJump()
    {
        if (_characterMovementController.grounded) { return; }

        _exitingWall = true;
        _characterMovementController.isExitingWall = true;
        _exitWallTimer = exitWallTime;

        Vector3 forceToApply = (transform.up * climbJumpUpForce) + (_frontWallHit.normal * climbJumpBackForce);

        _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(forceToApply * _rigidbody.mass, ForceMode.Impulse);

        _climbJumpsLeft--;
    }
}
