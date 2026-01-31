using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Manages the controls for the dash ability for the player
public class DashingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    private Transform _playerCameraObject;
    private Rigidbody _rigidbody;
    private CharacterMovementController _characterMovementController;

    [Header("Dashing")]
    [SerializeField] private float dashForce;
    [SerializeField] private float dashUpwardForce;
    [SerializeField] private float maxDashYSpeed;
    [SerializeField] private float dashDuration;

    [Header("Settings")]
    [SerializeField] private bool useCameraForward = true;
    [SerializeField] private bool allowAllDirections = true;
    [SerializeField] private bool disableGravity = false;
    [SerializeField] private bool resetVelocity = true;
    [SerializeField] private bool canOnlyDashAgainOnceGrounded = false;

    private bool _canDashAgain = true;

    [Header("Cooldown")]
    [SerializeField] private float dashCooldown;
    private float _dashCooldownTimer;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _characterMovementController = GetComponent<CharacterMovementController>();
    }

    public void SetPlayerCamera(Transform cam)
    { _playerCameraObject = cam; }

    private Vector2 _movementInput;
    private InputDetector _dashInput = new();

    public void HandlePlayerInputs(Vector2 movement, bool dash)
    {
        _dashInput.InputState = dash;
        _movementInput = movement;
    }
    // Only able to dash when it's off cooldown
    private void Update()
    {
        int inputChange = _dashInput.HasStateChanged();

        if (canOnlyDashAgainOnceGrounded && !_canDashAgain)
        { _canDashAgain = _characterMovementController.grounded; }

        if (inputChange == 0 && !_characterMovementController.isWallRunning)
        { Dash(); }

        // Controls dash cooldown
        if (_dashCooldownTimer > 0)
        { _dashCooldownTimer -= Time.deltaTime; }
    }
    // Holds force to apply to player character for next dash
    private void Dash()
    {
        // Checks dash cooldown
        if ((_dashCooldownTimer > 0 || !_canDashAgain)) { return; }
        
        _dashCooldownTimer = dashCooldown; 

        _characterMovementController.isDashing = true;
        _characterMovementController.maxYSpeed = maxDashYSpeed;

        Transform forwardT = orientation;

        if (useCameraForward)
        { forwardT = _playerCameraObject; }

        // Get the direction of the dash, then apply the force
        Vector3 direction = GetDirection(forwardT);
        Vector3 forceToApply = (direction * dashForce) + (orientation.up * dashUpwardForce);
        
        if (disableGravity)
        { _rigidbody.useGravity = false; }

        if (canOnlyDashAgainOnceGrounded)
        { _canDashAgain = false; }

        // Applies the force and invokes the required functions under a delay
        _delayedForceToApply = forceToApply;
        Invoke(nameof(DelayedDashForce), 0.025f);
        Invoke(nameof(ResetDash), dashDuration);
    }

    // Holds force to apply to player character for next dash
    private Vector3 _delayedForceToApply;
    // Applies an impulse dash force to player character 
    private void DelayedDashForce()
    {
        if (resetVelocity)
        { _rigidbody.linearVelocity = Vector3.zero; }

        _rigidbody.AddForce(_delayedForceToApply * _rigidbody.mass, ForceMode.Impulse);
    }
    // Resets the Dash ability (not cooldown)
    private void ResetDash()
    {
        _characterMovementController.isDashing = false;
        _characterMovementController.maxYSpeed = 0;

        if (disableGravity)
        { _rigidbody.useGravity = true; }
    }
    // Controls and gets the direction for the dash ability
    private Vector3 GetDirection(Transform forwardT)
    {
        if (allowAllDirections && _movementInput is not { y: 0, x: 0 })
        {
            return (forwardT.forward * _movementInput.y + forwardT.right * _movementInput.x).normalized;
        }

        return forwardT.forward.normalized;
    }
}

