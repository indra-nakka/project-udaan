using UnityEngine;

/// <summary>
/// Runtime holder for the per-faction visual models (enemy bot / ally / boss octacopter). Set by
/// <see cref="DemoFlow"/> (auto-assigned by the "Udaan ▸ Create or Update Demo Scene" editor tool).
/// <see cref="CombatSpawner"/> reads this to give each faction a DISTINCT body instead of the reused
/// tinted player prefab. If a model is null, the spawner falls back to the greybox + tint.
/// </summary>
public static class FactionVisuals
{
    public static GameObject Enemy, Ally, Boss;
    public static float EnemyScale = 0.35f, AllyScale = 0.35f, BossScale = 0.4f;
    public static Vector3 EnemyEuler, AllyEuler, BossEuler;   // per-faction rotation nudge (e.g. nose yaw)

    public static GameObject ModelFor(int team, bool boss) => boss ? Boss : (team == 1 ? Ally : Enemy);
    public static float ScaleFor(int team, bool boss) => boss ? BossScale : (team == 1 ? AllyScale : EnemyScale);
    public static Vector3 EulerFor(int team, bool boss) => boss ? BossEuler : (team == 1 ? AllyEuler : EnemyEuler);
}
