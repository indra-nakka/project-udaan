using UnityEngine;
using Unity.Netcode;

public class TargetHealth : NetworkBehaviour
{
    /// <summary>
    /// Live registry of all enabled TargetHealth in the scene. Self-maintained via OnEnable/OnDisable so
    /// hot paths (radar, targeting, AI, mission checks) iterate this small list instead of doing a full
    /// allocating FindObjectsByType scan many times per second. Callers still filter (team/alive/range).
    /// </summary>
    public static readonly System.Collections.Generic.List<TargetHealth> All = new System.Collections.Generic.List<TargetHealth>();

    [Header("Health Settings")]
    public DroneClassData defaultClassData;
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Targeting")]
    [Tooltip("Higher = locked first. Plan: destroyable projectile > enemy drone > minion > turret ...")]
    public int targetPriority = 0;
    [Tooltip("Faction: 0 = neutral, 1 = player, 2 = enemy. Targeting/damage only apply across different teams.")]
    public int team = 0;
    [Tooltip("Can this be locked/hit right now? False during spawn stealth/protection.")]
    public bool targetable = true;

    private float _invulnUntil, _protectedUntil;

    /// <summary>Who dealt the most recent damage — used to attribute kills (player vs ally) in run stats.</summary>
    [System.NonSerialized] public int lastAttackerTeam = -1;
    [System.NonSerialized] public bool lastHitFromPlayer = false;

    /// <summary>Current HP as a 0..1 fraction (used by AI for evade decisions).</summary>
    public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 1f;

    /// <summary>Spawn/launch protection: untargetable + immune to damage for a few seconds.</summary>
    public void SetProtected(float seconds)
    {
        targetable = false;
        _invulnUntil = Time.time + seconds;
        _protectedUntil = Time.time + seconds;
    }

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        if (!targetable && Time.time >= _protectedUntil) targetable = true;
    }

    [Header("Death")]
    [Tooltip("On death: refill + relocate (true) or destroy the object (false, e.g. enemy drones).")]
    public bool respawnOnDeath = true;
    [Tooltip("Player: stay in place at 0 HP on death (mission Defeat handles it) — no respawn, no destroy.")]
    public bool freezeOnDeath = false;

    [Header("Drop Settings")]
    public GameObject scrapPrefab;
    public int dropCount = 3;

    /// <summary>Fired whenever any TargetHealth takes damage: (victim, attackerTeam, amount, fromPlayer).</summary>
    public static event System.Action<TargetHealth, int, float, bool> OnAnyDamaged;
    /// <summary>Fired when a TargetHealth dies (before it is destroyed/frozen/relocated).</summary>
    public static event System.Action<TargetHealth> OnDeath;

    private Vector3 _initialScale;

    void Start()
    {
        _initialScale = transform.localScale;
        InitializeClassData(defaultClassData);
        if (currentHealth <= 0f) currentHealth = maxHealth; // offline / no class data: ensure full HP
    }

    // Damage may be applied offline (single-player toy) or server-side in a network session.
    private bool HasDamageAuthority => !IsSpawned || IsServer;

    public void InitializeClassData(DroneClassData classData)
    {
        defaultClassData = classData;
        if (defaultClassData != null)
        {
            maxHealth = defaultClassData.maxHealth;
            currentHealth = maxHealth;
            Debug.Log($"Target Health dynamically loaded class max HP: {maxHealth}");
        }
    }

    public void Heal(float amount)
    {
        if (!HasDamageAuthority) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        transform.localScale = _initialScale * Mathf.Lerp(0.5f, 1f, HealthFraction);
    }

    /// <summary>Set max HP and fill to full immediately (before Start runs) — avoids a 0-HP false read.</summary>
    public void Configure(float max)
    {
        maxHealth = max;
        currentHealth = max;
        if (_initialScale == Vector3.zero) _initialScale = transform.localScale;
    }

    // Called by projectiles on hit. attackerTeam/fromPlayer drive HUD feedback + run stats.
    public void TakeDamage(float amount, int attackerTeam = -1, bool fromPlayer = false)
    {
        if (!HasDamageAuthority) return;
        if (Time.time < _invulnUntil) return; // spawn protection
        lastAttackerTeam = attackerTeam;
        lastHitFromPlayer = fromPlayer;
        currentHealth -= amount;

        Debug.Log($"[HIT] {name} (team {team}) took {amount:0} → HP {Mathf.Max(currentHealth,0):0}/{maxHealth:0}");
        OnAnyDamaged?.Invoke(this, attackerTeam, amount, fromPlayer);

        // Shrink with health: full HP = 100% size, 0 HP = 50% size, then pop.
        float frac = Mathf.Clamp01(currentHealth / maxHealth);
        transform.localScale = _initialScale * Mathf.Lerp(0.5f, 1f, frac);

        if (currentHealth <= 0)
        {
            Pop();
        }
    }

    void Pop()
    {
        Debug.Log($"[DEATH] {name} (team {team}) destroyed");
        OnDeath?.Invoke(this);
        Vfx.Poof(transform.position, Mathf.Max(transform.localScale.x, 1f) * 1.3f);   // kid-tone cartoon defeat (smoke + spiral)

        // Player defeat: stay put at 0 HP; the MissionDirector shows Defeat + restart.
        if (freezeOnDeath) { currentHealth = 0f; return; }

        // Enemies (and anything non-respawning) are removed entirely.
        if (!respawnOnDeath)
        {
            if (!IsSpawned) Destroy(gameObject);
            else if (IsServer) NetworkObject.Despawn();
            return;
        }

        // --- ECON-02: The Piñata Drop ---
        // Ensure only the server handles spawning to prevent double-drops
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (scrapPrefab != null)
            {
                for (int i = 0; i < dropCount; i++)
                {
                    // Add slight random offset so they don't spawn inside each other
                    Vector3 spawnPos = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f));
                    GameObject newScrap = Instantiate(scrapPrefab, spawnPos, Random.rotation);

                    // Add explosive scatter force
                    Rigidbody rb = newScrap.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 scatterDir = (Vector3.up + Random.insideUnitSphere).normalized;
                        float force = Random.Range(4f, 8f);
                        rb.AddForce(scatterDir * force, ForceMode.Impulse);
                    }

                    // Crucial: Sync across the network
                    NetworkObject netObj = newScrap.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn();
                    }
                }
            }
        }

        // Respawn the dummy: refill, reset size, and relocate (server in a session, or locally when offline).
        if (IsServer || !IsSpawned)
        {
            currentHealth = maxHealth;
            transform.localScale = _initialScale;
            float randomX = UnityEngine.Random.Range(-20f, 20f);
            float randomZ = UnityEngine.Random.Range(-20f, 20f);
            transform.position = new Vector3(randomX, transform.position.y, randomZ);
            Debug.Log($"Target popped! Relocating to {transform.position}");
        }
    }
}