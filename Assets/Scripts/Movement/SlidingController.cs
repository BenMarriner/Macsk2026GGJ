using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlidingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform playerModel;
    private Rigidbody _rigidbody;
    private CharacterMovementController _characterMovementController;

    private float _horizontalInput;
    private float _verticalInput;
    private InputDetector _slideInput;

    [Header("Sliding")]
    [SerializeField] private float maxSlideTime;
    [SerializeField] private float slideForce;
    private float _slideTimer;

    [SerializeField] private float slideYScale;
    private float _startYScale;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _characterMovementController = GetComponent<CharacterMovementController>();
        _startYScale = playerModel.transform.localScale.y;
    }

    public void HandlePlayerInputs(bool sliding, Vector2 movementInput)
    {
        _horizontalInput = movementInput.x;
        _verticalInput = movementInput.y;
        _slideInput.InputState = sliding;
    }

    private void Update()
    {
        int inputChange = _slideInput.HasStateChanged();

        if (!_characterMovementController.isSliding && inputChange == 0 &&
            (_horizontalInput != 0 || _verticalInput != 0))
        { StartSlide(); }

        if (inputChange == 1 && _characterMovementController.isSliding)
        { StopSlide(); }
    }

    private void FixedUpdate()
    {
        if (_characterMovementController.isSliding)
        { SlidingMovement(); }
    }

    private void SlidingMovement()
    {
        if (_slideTimer <= 0 || !_characterMovementController.isSliding) { return; }

        Vector3 inputDirection = orientation.forward * _verticalInput + orientation.right * _horizontalInput;

        // Normal
        if (!_characterMovementController.OnSlope() || _rigidbody.linearVelocity.y > -0.1f)
        {
            _rigidbody.AddForce(
                inputDirection.normalized * (slideForce * 10f * _rigidbody.mass), ForceMode.Force);

            _slideTimer -= Time.deltaTime;
        }
        // Slope
        else
        { 
            _rigidbody.AddForce(
                _characterMovementController.GetSlopeMoveDirection(
                    inputDirection) * (slideForce * 10f * _rigidbody.mass), ForceMode.Force); 
        }

        if (_slideTimer <= 0)
        { StopSlide(); }
    }
    private void StartSlide()
    {
        if (_characterMovementController.isWallRunning) { return; }

        _characterMovementController.isSliding = true;

        playerModel.transform.localScale =
                new Vector3(
                    playerModel.transform.localScale.x,
                    slideYScale,
                    playerModel.transform.localScale.z);

        _characterMovementController.currentHeight *= slideYScale + 0.25f;

        _rigidbody.AddForce(Vector3.down * (5f * _rigidbody.mass), ForceMode.Impulse);

        _slideTimer = maxSlideTime;
    }
    private void StopSlide()
    {
        _characterMovementController.isSliding = false;

        playerModel.transform.localScale =
                new Vector3(
                    playerModel.transform.localScale.x,
                    _startYScale,
                    playerModel.transform.localScale.z);

        _characterMovementController.currentHeight = _characterMovementController.defaultHeight;
    }
}
