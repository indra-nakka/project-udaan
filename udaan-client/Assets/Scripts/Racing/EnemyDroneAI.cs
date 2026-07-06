using UnityEngine;

/// <summary>
/// Tier-1 enemy pilot. Thanks to the device-agnostic input layer, an AI is just another
/// <see cref="IFlightInput"/> source: it outputs a <see cref="FlightInputState"/> each tick and the
/// SAME <see cref="DroneFlightController"/> + <see cref="DroneWeapon"/> the player uses fly and shoot.
///
/// Behaviour: acquire the nearest enemy-of-my-team via <see cref="TargetingSystem"/> (world-based,
/// not camera), turn to face it, hold an engagement distance while orbiting, fire when roughly on
/// target, and back off when low on health. Add this at runtime to a Drone_Player prefab instance
/// (plus a TargetHealth with team = enemy); it strips the player-only camera/HUD.
/// </summary>
[RequireComponent(typeof(FlightInputRouter))]
public class EnemyDroneAI : MonoBehaviour, IFlightInput
{
    [Header("Engagement (metres)")]
    public float engageMin = 18f;
    public float engageMax = 45f;
    public float weaponRange = 70f;
    [Tooltip("Fire when the target is within this angle of the nose (deg).")]
    public float firingAngle = 14f;

    [Header("Feel")]
    [Tooltip("How hard the AI turns toward its target (higher = snappier).")]
    public float turnGain = 3f;
    public float evadeHealthFraction = 0.3f;
    [Tooltip("Seconds between enemy shots (higher = slower than the player).")]
    public float fireInterval = 0.5f;

    [Header("Self-defense")]
    [Tooltip("When shot, break off the current target and hunt the attacker for this long (refreshed by each hit).")]
    public float retaliateDuration = 4.5f;

    private TargetingSystem _targeting;
    private TargetHealth _health;
    private Rigidbody _rb;
    private float _orbitDir = 1f, _nextOrbitFlip;
    private float _retaliateUntil;
    private Transform _retaliateTarget;
    private bool _wasRetaliating;
    private float _unstuckUntil, _unstuckYaw;

    // Shared scratch buffers so per-frame physics queries don't allocate (GC pressure on mobile).
    private static readonly RaycastHit[] _rayBuf = new RaycastHit[24];
    private static readonly Collider[] _colBuf = new Collider[24];

    void Awake()
    {
        // Strip player-only components so this drone is fully AI-driven.
        StripPlayerControl();

        // Route input through this AI, don't auto-provision touch/gamepad.
        var router = GetComponent<FlightInputRouter>();
        router.autoProvisionSources = false;
        router.Register(this);

        // Flight tuned for predictable AI aiming; ignore the player's race countdown freeze.
        var flight = GetComponent<DroneFlightController>();
        if (flight != null) { flight.invertPitch = false; flight.autoLevelAssist = false; flight.ignoreCountdownLock = true; }

        // Targeting locks by world position (no player camera); weapon fires down the nose + assist.
        _targeting = GetComponent<TargetingSystem>();
        if (_targeting == null) _targeting = gameObject.AddComponent<TargetingSystem>();
        _targeting.useCameraForView = false;

        var weapon = GetComponent<DroneWeapon>();
        if (weapon != null) { weapon.useCameraAim = false; weapon.bulletRate = fireInterval; weapon.infiniteAmmo = true; }

        _health = GetComponent<TargetHealth>();
        _rb = GetComponent<Rigidbody>();

        // Floating health bar above the enemy.
        if (GetComponent<HealthBar>() == null) gameObject.AddComponent<HealthBar>();
    }

    void OnEnable() { TargetHealth.OnAnyDamaged += OnAnyDamaged; }
    void OnDisable() { TargetHealth.OnAnyDamaged -= OnAnyDamaged; }

    // Getting shot makes this drone selfish: it drops what it was doing (e.g. hammering the Core) and
    // hunts the nearest enemy combatant for a few seconds. Sustained fire keeps it peeled off.
    private void OnAnyDamaged(TargetHealth victim, int attackerTeam, float amount, bool fromPlayer)
    {
        if (victim != _health) return;
        _retaliateUntil = Time.time + retaliateDuration;
        _retaliateTarget = NearestCombatant();
    }

    // Nearest live drone on the other team (has a flight controller → a real fighter, NOT the Core).
    private Transform NearestCombatant()
    {
        Transform best = null;
        float bestSqr = float.MaxValue;
        int myTeam = _health != null ? _health.team : 2;
        var all = TargetHealth.All;
        for (int i = 0; i < all.Count; i++)
        {
            var t = all[i];
            if (t == null || t.team == myTeam || !t.targetable || t.HealthFraction <= 0f) continue;
            if (t.GetComponent<DroneFlightController>() == null) continue; // skip the Core / non-fighters
            float d = (t.transform.position - transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = t.transform; }
        }
        return best;
    }

    private void StripPlayerControl()
    {
        var cam = GetComponent<CameraController>();
        if (cam != null) { cam.enabled = false; Destroy(cam); }

        foreach (var hud in GetComponents<TouchFlightHUD>())
        {
            hud.enabled = false;
            Destroy(hud);
        }
        foreach (var gp in GetComponents<GamepadFlightInput>()) Destroy(gp);

        // In case a HUD canvas was already built, remove it.
        var canvas = transform.Find("TouchFlightHUD_Canvas");
        if (canvas != null) Destroy(canvas.gameObject);
    }

    public FlightInputState Read()
    {
        var s = FlightInputState.None;

        // Self-defense overrides the default target (usually the Core) while retaliating — and forces the
        // weapon's aim-assist onto the attacker too, so both the nose and the shots follow it.
        bool retaliating = Time.time < _retaliateUntil;
        if (retaliating)
        {
            if (_retaliateTarget == null || !_retaliateTarget.gameObject.activeInHierarchy)
                _retaliateTarget = NearestCombatant();
            if (_retaliateTarget != null && _targeting != null) _targeting.ForceTarget(_retaliateTarget);
        }
        else if (_wasRetaliating && _targeting != null)
        {
            _targeting.ReleaseForcedTarget(); // resume auto-acquire (back to the Core, etc.)
        }
        _wasRetaliating = retaliating;

        Transform target = retaliating && _retaliateTarget != null
            ? _retaliateTarget
            : (_targeting != null ? _targeting.CurrentTarget : null);

        if (target == null)
        {
            // No target: gentle forward search-turn.
            s.thrust = 0.25f;
            s.yaw = 0.3f;
            return s;
        }

        Vector3 to = target.position - transform.position;
        float dist = to.magnitude;
        Vector3 local = transform.InverseTransformDirection(dist > 0.001f ? to / dist : transform.forward);
        float ang = Vector3.Angle(transform.forward, to);
        bool lowHp = _health != null && _health.HealthFraction < evadeHealthFraction;

        // Turn toward the target (invertPitch is false on AI, so these signs point the nose at it).
        s.yaw = Mathf.Clamp(local.x * turnGain, -1f, 1f);
        s.pitch = Mathf.Clamp(local.y * turnGain, -1f, 1f);

        // Throttle: hold the engagement band. When low, KITE — back off but keep shooting (no full retreat).
        if (lowHp) s.thrust = -0.35f;
        else if (dist > engageMax) s.thrust = 1f;
        else if (dist < engageMin) s.thrust = -0.4f;
        else s.thrust = 0.1f;

        // Orbit strafe (occasionally flip direction) to avoid a head-on stalemate.
        if (Time.time >= _nextOrbitFlip)
        {
            _orbitDir = Random.value < 0.5f ? -1f : 1f;
            _nextOrbitFlip = Time.time + Random.Range(2f, 4f);
        }
        if (dist < engageMax * 1.2f) s.strafe = _orbitDir * (lowHp ? 0.85f : 0.5f);

        // Steer around walls/props directly ahead so the AI stops burying itself in geometry.
        AvoidObstacles(ref s);

        // Fire only with a clear line of sight (no shooting through walls) and in range.
        bool los = HasLineOfSight(target, dist);
        if (ang < firingAngle && dist < weaponRange && los) s.firePrimary = true;

        return s;
    }

    // True if nothing solid (non-drone geometry) sits between us and the target.
    private bool HasLineOfSight(Transform target, float dist)
    {
        Vector3 origin = transform.position + transform.forward * 1.5f; // start ahead of our own collider
        Vector3 dir = target.position - origin;
        float d = dir.magnitude;
        if (d < 0.5f) return true;
        int n = Physics.RaycastNonAlloc(origin, dir / d, _rayBuf, d - 1f);
        for (int i = 0; i < n; i++)
        {
            var h = _rayBuf[i];
            if (h.collider.transform.root == transform.root) continue;        // ourselves
            if (h.collider.transform.root == target.root) continue;           // the target itself
            if (h.collider.GetComponentInParent<TargetHealth>() != null) continue; // another drone — not a wall
            return false;                                                     // solid terrain in the way
        }
        return true;
    }

    // Whisker-based obstacle avoidance with a stuck-recovery fallback.
    private void AvoidObstacles(ref FlightInputState s)
    {
        const float look = 20f;

        // 1) Stuck recovery. SphereCast can't see a wall it's already overlapping, so when we're commanded
        //    to move yet barely moving next to geometry, we're wedged — back out, spin and climb for a beat.
        if (Time.time >= _unstuckUntil && _rb != null)
        {
            bool wantsMove = Mathf.Abs(s.thrust) > 0.05f || Mathf.Abs(s.strafe) > 0.05f;
            if (wantsMove && _rb.linearVelocity.sqrMagnitude < 1.5f && TouchingObstacle())
            {
                _unstuckUntil = Time.time + 1.2f;
                _unstuckYaw = Random.value < 0.5f ? -1f : 1f;
            }
        }
        if (Time.time < _unstuckUntil)
        {
            s.thrust = -0.7f;             // reverse out
            s.strafe = _unstuckYaw * 0.7f;
            s.yaw = _unstuckYaw;          // spin away
            s.pitch = 0.5f;               // and climb
            return;
        }

        // 2) Forward whiskers: measure clearance ahead, and to each front-quarter, then steer to the open side.
        float fwd = Clearance(transform.forward, look);
        float leftF = Clearance((transform.forward - transform.right * 0.7f).normalized, look);
        float rightF = Clearance((transform.forward + transform.right * 0.7f).normalized, look);

        float closest = Mathf.Min(fwd, Mathf.Min(leftF, rightF));
        if (closest >= look) return; // all clear

        float urgency = 1f - Mathf.Clamp01(closest / look);
        float steer = rightF - leftF;                       // + => more room right => bank right
        float dirSign = Mathf.Abs(steer) < 0.01f ? _orbitDir : Mathf.Sign(steer);
        s.yaw = Mathf.Clamp(s.yaw + dirSign * (0.7f + urgency), -1f, 1f);
        s.pitch = Mathf.Clamp(s.pitch + 0.5f * urgency, -1f, 1f); // climb over

        // Don't drive into it: cut throttle when close, reverse when very close.
        if (fwd < look * 0.35f) s.thrust = -0.25f;
        else s.thrust = Mathf.Min(s.thrust, 0.15f);
    }

    // Distance to the nearest solid terrain along dir (max if clear / only drones in the way).
    private float Clearance(Vector3 dir, float max)
    {
        Vector3 origin = transform.position + dir * 1.2f; // start just ahead to clear our own hull
        int n = Physics.RaycastNonAlloc(origin, dir, _rayBuf, max);
        float best = max;
        for (int i = 0; i < n; i++)
        {
            var h = _rayBuf[i];
            if (h.collider.transform.root == transform.root) continue;
            if (h.collider.GetComponentInParent<TargetHealth>() != null) continue; // a drone, not a wall
            if (h.distance < best) best = h.distance;
        }
        return best;
    }

    // Any solid (non-drone) geometry overlapping us right now? Catches the wedged case a cast misses.
    private bool TouchingObstacle()
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position, 2.6f, _colBuf);
        for (int i = 0; i < n; i++)
        {
            var c = _colBuf[i];
            if (c.transform.root == transform.root) continue;
            if (c.GetComponentInParent<TargetHealth>() != null) continue;
            return true;
        }
        return false;
    }
}
