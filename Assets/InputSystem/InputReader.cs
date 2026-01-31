using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerInputScheme;

// Reads the players inputs
[CreateAssetMenu(fileName = "New Input Reader", menuName = "Input/Input Reader")]
public class InputReader : ScriptableObject, IFirstPersonInputsActions
{
    public event Action<Vector2> MoveEvent;
    public event Action<Vector2> LookEvent;
    public event Action<bool> JumpEvent;
    public event Action<bool> CrouchEvent;
    public event Action<bool> SprintEvent;

    [Range(1f, 500f)] public float mouseSensitivityX = 200f;
    [Range(1f, 500f)] public float mouseSensitivityY = 200f;

    private PlayerInputScheme _controls;

    private void OnEnable()
    {
        if (_controls == null)
        {
            _controls = new PlayerInputScheme();
            _controls.FirstPersonInputs.SetCallbacks(this);
        }
        _controls.FirstPersonInputs.Enable();
    }

    private void OnDisable()
    {
        if (_controls != null)
        {
            _controls.FirstPersonInputs.RemoveCallbacks(this);
            _controls.FirstPersonInputs.Disable();
        }
    }

    public void OnMovement(InputAction.CallbackContext context)
    { MoveEvent?.Invoke(context.ReadValue<Vector2>()); }
    public void OnLook(InputAction.CallbackContext context)
    { LookEvent?.Invoke(context.ReadValue<Vector2>()); }
    public void OnJump(InputAction.CallbackContext context)
    { JumpEvent?.Invoke(context.action.IsPressed()); }
    public void OnCrouch(InputAction.CallbackContext context)
    { CrouchEvent?.Invoke(context.action.IsPressed()); }
    public void OnSprint(InputAction.CallbackContext context)
    { SprintEvent?.Invoke(context.action.IsPressed()); }
}
