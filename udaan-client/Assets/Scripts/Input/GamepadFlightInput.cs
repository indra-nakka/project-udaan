using UnityEngine;

/// <summary>
/// Legacy Input Manager reader. Dev-convenience path (touch is the shipping target, see DEC-002).
///
/// LEFT stick = flight aim (yaw/pitch). RIGHT stick role depends on the router's AimMode:
///   NoseAim  -> strafe/altitude (translation)
///   FreeAim  -> free-aim reticle offset (aimX/aimY), recenters when released
/// Fire: LB = primary bullets, RB = secondary rockets (LMB/RMB mirror on PC).
/// </summary>
public class GamepadFlightInput : MonoBehaviour, IFlightInput
{
    [Tooltip("Flip right-stick vertical for free-aim so up = aim up.")]
    public bool invertAimY = true;

    private FlightInputRouter _router;

    void Awake() => _router = GetComponent<FlightInputRouter>();

    public FlightInputState Read()
    {
        var s = FlightInputState.None;

        // Flight aim (primary stick).
        s.yaw   = SafeAxis("Horizontal"); // L-stick X -> turn
        s.pitch = SafeAxis("Vertical");   // L-stick Y -> nose up/down

        // Right stick: translation OR free-aim depending on mode.
        float rx = SafeAxis("TargetYaw");   // R-stick X
        float ry = SafeAxis("TargetPitch"); // R-stick Y
        bool freeAim = _router != null && _router.Aim == FlightInputRouter.AimMode.FreeAim;
        if (freeAim) { s.aimX = rx; s.aimY = invertAimY ? -ry : ry; } // up = aim up
        else         { s.strafe = rx; s.altitude = ry; }

        // Throttle: RT forward, LT reverse -> -1..1.
        s.thrust = Mathf.Clamp(SafeAxis("Xbox_RT") - SafeAxis("Xbox_LT"), -1f, 1f);

        // Fire (held). LB = bullets, RB = rockets; mouse mirrors for PC testing.
        s.firePrimary   = Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.Mouse0);
        s.fireSecondary = Input.GetKey(KeyCode.JoystickButton5) || Input.GetKey(KeyCode.Mouse1);

        // Juke/dodge burst — gamepad X or Left Ctrl (edge + cooldown handled in the controller).
        s.dash = Input.GetKey(KeyCode.JoystickButton2) || Input.GetKey(KeyCode.LeftControl);

        return s;
    }

    private static float SafeAxis(string axis)
    {
        try { return Input.GetAxis(axis); }
        catch { return 0f; }
    }
}
