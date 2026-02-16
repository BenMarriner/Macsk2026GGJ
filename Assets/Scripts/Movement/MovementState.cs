/// <summary>
/// Defines the different movement states for the character controller.
/// Used by movement controllers and camera system to coordinate behavior.
/// </summary>
public enum MovementState
{
    Freeze,
    Unlimited,
    Walking,
    Sprinting,
    WallRunning,
    Crouching,
    Dashing,
    Climbing,
    Sliding,
    Air
}
