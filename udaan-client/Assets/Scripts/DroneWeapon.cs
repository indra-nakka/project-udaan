using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Two-weapon system driven by the device-agnostic input (FlightInputRouter):
///   Primary   = rapid bullets (held), low damage.
///   Secondary = dumb rockets (held), slow, splash damage.
/// Shots fly along the nose, rotated within a cone by the free-aim reticle (aimX/aimY). Projectiles
/// are procedurally created + pooled, so no prefab wiring is needed. Works offline (single-player)
/// and, later, as the owner in a network session.
/// </summary>
public class DroneWeapon : NetworkBehaviour
{
    [Header("Muzzle")]
    public Transform muzzlePoint;
    [Tooltip("Optional: multiple muzzles for weapon upgrades (double turrets, etc.). Empty = use muzzlePoint.")]
    public Transform[] muzzlePoints;

    [Header("Aim")]
    [Tooltip("Free-aim cone half-angle (deg) the reticle can pull a shot off the nose.")]
    public float maxAimAngle = 22f;
    [Tooltip("Player: true (fire down the camera view). AI enemies: false (fire down the nose + assist onto lock).")]
    public bool useCameraAim = true;

    [Header("Primary — Bullets")]
    public float bulletRate = 0.1f;
    public float bulletSpeed = 220f;
    public float bulletDamage = 18f;   // ~6 hits to pop a 100 HP dummy
    public float bulletLife = 2.5f;
    public int bulletMagSize = 30;
    public float reloadTime = 1.6f;

    [Header("Secondary — Rockets")]
    public float rocketRate = 0.7f;
    public float rocketSpeed = 140f;
    public float rocketDamage = 60f;   // 2 rockets pop a 100 HP dummy (1 leaves it alive)
    public float rocketLife = 4f;
    public float rocketSplash = 4f;
    public int rocketMax = 6;
    public float rocketRegen = 4f;     // seconds to regenerate one rocket

    [Header("Ammo")]
    [Tooltip("Enemies set this true so they don't run dry / reload.")]
    public bool infiniteAmmo = false;

    // ---- ammo state (for the HUD) ----
    private int _bulletMag, _rockets;
    private float _reloadDoneAt, _nextRocketRegen;
    private bool _reloading;
    public int BulletsInMag => _bulletMag;
    public int BulletMagSize => bulletMagSize;
    public int Rockets => _rockets;
    public bool IsReloading => _reloading;
    public bool HasAmmoSystem => !infiniteAmmo;

    /// <summary>Refill bullet magazine + rockets (ammo pickup).</summary>
    public void ResupplyAmmo()
    {
        _bulletMag = bulletMagSize;
        _reloading = false;
        _rockets = rocketMax;
    }

    private FlightInputRouter _input;
    private TargetingSystem _targeting;
    private Rigidbody _rb;
    private Collider[] _ownColliders;
    private Transform[] _muzzles;
    private Camera _cam;
    private TargetHealth _ownHealth;
    private int OwnTeam { get { if (_ownHealth == null) _ownHealth = GetComponent<TargetHealth>(); return _ownHealth != null ? _ownHealth.team : 0; } }
    private float _nextBullet, _nextRocket;

    // Must match TouchFlightHUD's crosshair offset so shots go exactly where the reticle is drawn.
    private const float CrosshairOffsetFraction = 0.18f;

    private Camera Cam { get { if (_cam == null) _cam = Camera.main; return _cam; } }

    private readonly Queue<Projectile> _bulletPool = new Queue<Projectile>();
    private readonly Queue<Projectile> _rocketPool = new Queue<Projectile>();

    void Awake()
    {
        _input = GetComponent<FlightInputRouter>();
        _rb = GetComponent<Rigidbody>();
        _ownColliders = GetComponentsInChildren<Collider>();
        if (muzzlePoint == null) muzzlePoint = transform;
        _muzzles = (muzzlePoints != null && muzzlePoints.Length > 0) ? muzzlePoints : new Transform[] { muzzlePoint };

        // Soft lock-on: auto-provision so aim assist works with zero wiring.
        _targeting = GetComponent<TargetingSystem>();
        if (_targeting == null) _targeting = gameObject.AddComponent<TargetingSystem>();

        _bulletMag = bulletMagSize;
        _rockets = rocketMax;
    }

    private bool HasControl() => !IsSpawned || IsOwner;

    void Update()
    {
        if (!HasControl()) return;
        if (RaceManager.FlightLocked) return; // no firing during the countdown

        FlightInputState f = _input != null ? _input.Last : FlightInputState.None;

        // Ammo upkeep: finish reloads, regenerate rockets.
        if (!infiniteAmmo)
        {
            if (_reloading && Time.time >= _reloadDoneAt) { _bulletMag = bulletMagSize; _reloading = false; }
            if (_rockets < rocketMax && Time.time >= _nextRocketRegen) { _rockets++; _nextRocketRegen = Time.time + rocketRegen; }
        }

        if (f.firePrimary && Time.time >= _nextBullet && (infiniteAmmo || (!_reloading && _bulletMag > 0)))
        {
            _nextBullet = Time.time + bulletRate;
            Fire(_bulletPool, 0.12f, new Color(1f, 0.9f, 0.4f), bulletSpeed, bulletDamage, bulletLife, 0f, f);
            if (useCameraAim) Sfx.Bullet(muzzlePoint.position); // player only (avoid enemy spam)
            if (!infiniteAmmo && --_bulletMag <= 0) { _reloading = true; _reloadDoneAt = Time.time + reloadTime; }
        }
        if (f.fireSecondary && Time.time >= _nextRocket && (infiniteAmmo || _rockets > 0))
        {
            _nextRocket = Time.time + rocketRate;
            Fire(_rocketPool, 0.3f, new Color(1f, 0.4f, 0.2f), rocketSpeed, rocketDamage, rocketLife, rocketSplash, f);
            if (useCameraAim) Sfx.Rocket(muzzlePoint.position);
            if (!infiniteAmmo) _rockets--;
        }
    }

    private void Fire(Queue<Projectile> pool, float size, Color color, float speed, float dmg, float life, float splash, FlightInputState f)
    {
        Vector3 inherit = _rb != null ? _rb.linearVelocity : Vector3.zero;

        // Fire one projectile per muzzle (single by default; upgrades add muzzles for double turrets, etc.).
        for (int i = 0; i < _muzzles.Length; i++)
        {
            Transform m = _muzzles[i] != null ? _muzzles[i] : transform;
            Vector3 muzzle = m.position;
            Vector3 dir = AimDirection(f, muzzle);
            if (_targeting != null) dir = _targeting.AimDir(dir, muzzle); // snap onto the locked target
            Vector3 pos = muzzle + dir * 1.0f; // spawn ahead so it clears the drone

            Projectile p = pool.Count > 0 ? pool.Dequeue() : CreateProjectile(size, color);
            p.Launch(pos, dir, inherit, speed, dmg, life, splash, OwnTeam, useCameraAim, _ownColliders, ret => pool.Enqueue(ret));
        }
    }

    /// <summary>
    /// Base fire direction = straight down the camera view (where you're looking, since the right stick
    /// now orbits the camera). TargetingSystem.AimDir then bends this onto the locked target.
    /// Falls back to the drone nose if there's no camera.
    /// </summary>
    private Vector3 AimDirection(FlightInputState f, Vector3 fromPos)
    {
        Camera cam = Cam;
        if (!useCameraAim || cam == null) return transform.forward;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        Vector3 target = ray.GetPoint(500f); // far point straight ahead of the view
        return (target - fromPos).normalized;
    }

    private Projectile CreateProjectile(float size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Projectile";
        go.transform.localScale = Vector3.one * size;

        go.GetComponent<Collider>().isTrigger = false;

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 0.05f; // near-massless so a hit barely nudges the drone (knockback fix)
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 2f);
        go.GetComponent<Renderer>().sharedMaterial = mat;

        // Tracer trail.
        var tr = go.AddComponent<TrailRenderer>();
        tr.time = 0.22f;
        tr.startWidth = size * 0.9f;
        tr.endWidth = 0f;
        tr.numCapVertices = 2;
        tr.minVertexDistance = 0.1f;
        var tmat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
        tmat.color = color;
        tr.material = tmat;
        tr.startColor = color;
        tr.endColor = new Color(color.r, color.g, color.b, 0f);

        var p = go.AddComponent<Projectile>();
        go.SetActive(false);
        return p;
    }
}
