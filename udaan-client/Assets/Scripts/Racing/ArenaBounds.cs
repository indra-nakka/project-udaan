using UnityEngine;

/// <summary>
/// Global arena boundary. For now a hard sphere: drones are clamped inside it (DroneFlightController).
/// Later this becomes a PUBG-style shrinking zone (leave = take damage) rather than a hard wall.
/// </summary>
public static class ArenaBounds
{
    public static bool Enabled;
    public static Vector3 Center = Vector3.zero;
    public static float Radius = 120f;
}
