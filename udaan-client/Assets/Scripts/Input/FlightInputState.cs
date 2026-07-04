using UnityEngine;

/// <summary>
/// Device-agnostic snapshot of the 5-axis flight scheme (see architecture/controller-map.md)
/// plus the two touch-era action buttons. Any input source (gamepad, touch, AI) produces one
/// of these each frame, so DroneFlightController never needs to know where the numbers came from.
///
/// Axis conventions (all already clamped to the ranges below by the producing source):
///   thrust   : 0..1   forward accelerate (throttle). No reverse; use brake instead.
///   altitude : -1..1  L-stick Y  -> +up / -down
///   strafe   : -1..1  L-stick X  -> +right / -left
///   yaw      : -1..1  R-stick X  -> +turn right / -turn left
///   pitch    : -1..1  R-stick Y  -> +nose up / -nose down (inversion handled in the controller)
///   brake    : bool   air-brake / hard slow for tight gate turns
///   boost    : bool   reserved (currently unused; throttle-slider scheme chosen for M1)
/// </summary>
public struct FlightInputState
{
    public float thrust;
    public float altitude;
    public float strafe;
    public float yaw;
    public float pitch;
    public bool brake;
    public bool boost;

    // ---- weapons ----
    public float aimX;          // -1..1 free-aim reticle offset (yaw), 0 = nose
    public float aimY;          // -1..1 free-aim reticle offset (pitch), 0 = nose
    public bool firePrimary;    // rapid bullets (held)
    public bool fireSecondary;  // rockets (held)
    public bool dash;           // juke/dodge burst (held; edge-detected + cooldown in the controller)

    public static FlightInputState None => new FlightInputState();

    /// <summary>True if any analog axis is meaningfully deflected.</summary>
    public bool HasAnalog(float deadzone = 0.05f)
    {
        return Mathf.Abs(thrust) > deadzone
            || Mathf.Abs(altitude) > deadzone
            || Mathf.Abs(strafe) > deadzone
            || Mathf.Abs(yaw) > deadzone
            || Mathf.Abs(pitch) > deadzone;
    }

    /// <summary>
    /// Combine two sources, keeping the larger-magnitude value per axis. Lets a gamepad and the
    /// on-screen touch HUD both feed the same drone without one zeroing out the other.
    /// </summary>
    public static FlightInputState Merge(FlightInputState a, FlightInputState b)
    {
        return new FlightInputState
        {
            thrust   = Mathf.Abs(a.thrust)   >= Mathf.Abs(b.thrust)   ? a.thrust   : b.thrust,
            altitude = Mathf.Abs(a.altitude) >= Mathf.Abs(b.altitude) ? a.altitude : b.altitude,
            strafe   = Mathf.Abs(a.strafe)   >= Mathf.Abs(b.strafe)   ? a.strafe   : b.strafe,
            yaw      = Mathf.Abs(a.yaw)      >= Mathf.Abs(b.yaw)      ? a.yaw      : b.yaw,
            pitch    = Mathf.Abs(a.pitch)    >= Mathf.Abs(b.pitch)    ? a.pitch    : b.pitch,
            aimX     = Mathf.Abs(a.aimX)     >= Mathf.Abs(b.aimX)     ? a.aimX     : b.aimX,
            aimY     = Mathf.Abs(a.aimY)     >= Mathf.Abs(b.aimY)     ? a.aimY     : b.aimY,
            brake         = a.brake || b.brake,
            boost         = a.boost || b.boost,
            firePrimary   = a.firePrimary || b.firePrimary,
            fireSecondary = a.fireSecondary || b.fireSecondary,
            dash          = a.dash || b.dash,
        };
    }
}

/// <summary>
/// Implemented by every input source. Read() is polled once per FixedUpdate by the flight
/// controller (via FlightInputRouter).
/// </summary>
public interface IFlightInput
{
    FlightInputState Read();
}
