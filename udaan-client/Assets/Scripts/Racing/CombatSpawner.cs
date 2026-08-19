using UnityEngine;

/// <summary>
/// Shared factory that turns the Drone_Player prefab into an AI combatant (or boss). Used by both the
/// sandbox bootstrap and the MissionDirector so wave enemies and the boss are assembled identically.
/// </summary>
public static class CombatSpawner
{
    public static GameObject Enemy(GameObject prefab, Vector3 pos, Quaternion rot, float hp, Color color, int team, bool boss)
    {
        if (prefab == null) return null;

        var e = Object.Instantiate(prefab, pos, rot);
        e.name = boss ? "Boss" : "Enemy";

        var rb = e.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        if (boss) e.transform.localScale *= 2.2f;

        var th = e.GetComponent<TargetHealth>();
        if (th == null) th = e.AddComponent<TargetHealth>();
        th.team = team;
        th.maxHealth = hp;
        th.targetPriority = boss ? 2 : 1;
        th.respawnOnDeath = false;

        // Give each faction its own body (enemy bot / ally / boss octacopter) if models are assigned.
        var model = FactionVisuals.ModelFor(team, boss);
        if (model != null)
        {
            AttachSkin(e, model, FactionVisuals.ScaleFor(team, boss), FactionVisuals.EulerFor(team, boss));
            if (!boss) Tint(e, color);   // tint enemy/ally for team readability; boss keeps its native purple
        }
        else Tint(e, color);             // greybox fallback (no faction models wired)

        e.AddComponent<EnemyDroneAI>(); // strips player camera/HUD, configures targeting/weapon for AI

        if (boss)
        {
            var w = e.GetComponent<DroneWeapon>();
            if (w != null) { w.bulletRate = 0.2f; w.bulletDamage = 36f; }   // buffed: faster + harder hitting
            var ai = e.GetComponent<EnemyDroneAI>();
            if (ai != null)
            {
                ai.weaponRange = 90f;      // reaches out further
                ai.firingAngle = 20f;      // fires from a wider cone
                ai.turnGain = 3.6f;         // tracks harder
                ai.evadeHealthFraction = 0.15f; // presses the attack longer
            }
            e.AddComponent<BossController>();   // entrance laugh + slime-thrower + shockwave specials
        }
        return e;
    }

    /// <summary>An allied drone: same AI, on the player's team so it targets enemies and ignores the player.</summary>
    public static GameObject Ally(GameObject prefab, Vector3 pos, int team, float hp)
    {
        var a = Enemy(prefab, pos, Quaternion.identity, hp, new Color(0.35f, 0.6f, 1f), team, false);
        if (a != null)
        {
            a.name = "Ally";
            // Allies are support, not carry: slower fire + lighter hits than a wave enemy so the
            // player stays the main threat.
            var w = a.GetComponent<DroneWeapon>();
            if (w != null) { w.bulletRate = 0.85f; w.bulletDamage = 9f; }
            if (MissionStats.Active != null) MissionStats.Active.alliesSpawned++;
        }
        return a;
    }

    // Replace the greybox/player visual with a faction model (visual only — colliders/scripts stay on the prefab).
    private static void AttachSkin(GameObject e, GameObject model, float scale, Vector3 euler)
    {
        foreach (var mr in e.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = false;   // hide existing visual
        var skin = Object.Instantiate(model);
        skin.name = "Skin";
        skin.transform.SetParent(e.transform, false);
        skin.transform.localPosition = Vector3.zero;
        skin.transform.localRotation = Quaternion.Euler(euler);
        skin.transform.localScale = Vector3.one * scale;
        foreach (var c in skin.GetComponentsInChildren<Collider>()) Object.Destroy(c);           // visual only, no physics
    }

    private static void Tint(GameObject go, Color c)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var m = r.material; // per-instance
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }
}
