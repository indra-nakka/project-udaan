using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "Sky Sentinel" demo mission spine: intro → escalating enemy waves (clear each to advance) → boss →
/// victory/defeat, with an in-place restart on R. Drives the objective readout in the HUD. Spawns via
/// CombatSpawner so wave enemies and the boss reuse the exact player flight/weapon systems.
///
/// Next layers (post-spine): capturable outposts + allied AI, an escort/defend objective beat, and a
/// mid-mission scrap upgrade — see 🎮 Combat & Game Vision.
/// </summary>
public class MissionDirector : MonoBehaviour
{
    public enum Phase { Intro, Wave, Capture, Defend, Upgrade, Boss, Victory, Defeat }
    public enum Difficulty { Recruit, Pilot, Ace }

    [Header("Difficulty")]
    public Difficulty difficulty = Difficulty.Pilot;   // data-driven; a menu can set this later (#81)

    // Injected by the bootstrap.
    [HideInInspector] public GameObject enemyPrefab;
    [HideInInspector] public Vector3 arenaCenter;
    [HideInInspector] public float spawnRadius = 75f;
    [HideInInspector] public float spawnHeight = 12f;
    [HideInInspector] public TargetHealth player;

    [Header("Mission config")]
    public int[] waveCounts = { 2, 3 };   // pre-capture skirmish
    public float waveEnemyHealth = 100f;
    public float bossHealth = 1200f;
    public Color enemyColor = new Color(1f, 0.35f, 0.3f);
    public Color bossColor = new Color(0.7f, 0.2f, 0.9f);

    [Header("Enemy scaling (per wave, leading to boss)")]
    public float enemyBaseDamage = 14f;
    public float enemyBaseFire = 0.5f;   // seconds between shots
    [Tooltip("Extra HP / damage fraction added per wave index.")]
    public float perWaveHealth = 0.35f;
    public float perWaveDamage = 0.25f;
    public float perWaveFireSpeedup = 0.08f; // shots get this fraction faster per wave

    [Header("Defend beat")]
    public float coreHealth = 800f;
    public float defendDuration = 35f;
    public float defendPushInterval = 7f;
    public int defendPushCount = 2;
    public int defendMaxEnemies = 5;
    [Tooltip("Core self-repairs when no enemy is within this radius of it...")]
    public float coreSafeRadius = 45f;
    [Tooltip("...for this long (seconds) with no attacker nearby, then heals at coreRegenRate HP/s.")]
    public float coreRegenDelay = 2.5f;   // (legacy — regen is now continuous; kept for tuning reference)
    public float coreRegenRate = 55f;     // slightly above ONE attacker's DPS: a straggler is survivable, a push is not

    [Header("Player")]
    public int startingLives = 3;
    public float launchStealth = 3f;   // untargetable+invuln at start to orient
    public float respawnProtection = 2.5f;

    [Header("Outposts / allies")]
    public int outpostCount = 3;
    public float outpostMinRadius = 30f;   // random placement band from arena centre
    public float outpostMaxRadius = 70f;
    public float outpostSeparation = 34f;  // keep bubbles from overlapping
    public float allyHealth = 80f;
    [Tooltip("On capturing an outpost: full heal + this fraction of max HP as a temporary overshield.")]
    public float captureOvershieldFraction = 0.4f;
    public float captureBuffSeconds = 30f;

    public Phase phase { get; private set; }

    private readonly List<TargetHealth> _alive = new List<TargetHealth>();
    private readonly List<Outpost> _outposts = new List<Outpost>();
    private TouchFlightHUD _hud;
    private TargetHealth _core;
    private int _lives;
    private bool _defeated;
    private float _overshieldBonus, _overshieldUntil;
    private MissionStats _stats;
    private int _runStartLives;
    private const int PlayerTeam = 1, EnemyTeam = 2;

    // Difficulty multipliers (applied at SPAWN time only — player-damage scaling deferred to avoid restart stacking).
    private float EnemyHpMul  => difficulty == Difficulty.Recruit ? 0.8f : difficulty == Difficulty.Ace ? 1.35f : 1f;
    private float EnemyDmgMul => difficulty == Difficulty.Recruit ? 0.7f : difficulty == Difficulty.Ace ? 1.4f  : 1f;
    private float CoreHpMul   => difficulty == Difficulty.Recruit ? 1.3f : difficulty == Difficulty.Ace ? 0.8f  : 1f;
    private int   LivesFor    => difficulty == Difficulty.Recruit ? startingLives + 1 : difficulty == Difficulty.Ace ? Mathf.Max(1, startingLives - 1) : startingLives;

    void Start() { StartCoroutine(RunMission()); }

    void OnEnable()
    {
        TargetHealth.OnAnyDamaged += OnDamaged;
        TargetHealth.OnDeath += OnDeath;
        Outpost.OnCaptured += OnOutpostCaptured;
    }

    void OnDisable()
    {
        TargetHealth.OnAnyDamaged -= OnDamaged;
        TargetHealth.OnDeath -= OnDeath;
        Outpost.OnCaptured -= OnOutpostCaptured;
    }

    // Capturing an outpost = resupply: full heal + a temporary overshield. Re-capturing refreshes the
    // timer and reconciles the bonus off the TRUE max (so it plays nicely with the Armor upgrade too).
    private void OnOutpostCaptured(Outpost o, int team)
    {
        if (team != PlayerTeam || player == null) return;
        player.maxHealth -= _overshieldBonus;                       // strip any existing overshield first
        _overshieldBonus = player.maxHealth * captureOvershieldFraction;
        player.maxHealth += _overshieldBonus;
        _overshieldUntil = Time.time + captureBuffSeconds;
        player.Heal(player.maxHealth);                              // top off to base + overshield
        Objective($"OUTPOST SECURED — resupply! full heal +{Mathf.RoundToInt(captureOvershieldFraction * 100f)}% shield ({captureBuffSeconds:0}s)");
        Debug.Log($"[BUFF] Capture overshield +{_overshieldBonus:0} for {captureBuffSeconds:0}s");
    }

    void Update()
    {
        if (_overshieldBonus > 0f && Time.time >= _overshieldUntil)
        {
            if (player != null) { player.maxHealth -= _overshieldBonus; player.Heal(0f); } // clamp HP to new max
            _overshieldBonus = 0f;
        }
    }

    private void OnDamaged(TargetHealth victim, int attackerTeam, float amount, bool fromPlayer)
    {
        if (_stats == null || victim == null) return;
        if (victim == player) { _stats.damageTaken += amount; return; }
        if (victim.team != EnemyTeam) return;                 // only count damage onto hostiles
        if (fromPlayer) _stats.damageDealt += amount;
        else if (attackerTeam == PlayerTeam) _stats.allyDamage += amount; // an ally landed the hit
    }

    private void OnDeath(TargetHealth victim)
    {
        if (_stats == null || victim == null) return;
        if (victim.team == EnemyTeam)
        {
            _stats.kills++;
            if (victim.lastHitFromPlayer) _stats.playerKills++;
            else if (victim.lastAttackerTeam == PlayerTeam) _stats.allyKills++;
        }
        else if (victim.team == PlayerTeam && victim != player && victim != _core)
        {
            _stats.alliesLost++;                              // an allied drone was destroyed
        }
    }

    private IEnumerator RunMission()
    {
        while (true)
        {
            _runStartLives = LivesFor;
            _lives = _runStartLives;
            _defeated = false;
            _stats = new MissionStats { startTime = Time.time };
            MissionStats.Active = _stats;
            ClearAll();
            HealAndProtect(launchStealth);            // launch stealth to orient
            phase = Phase.Intro;
            Objective($"SKY SENTINEL — get your bearings ({launchStealth:0}s stealth). Hostiles inbound.    LIVES {_lives}");
            yield return new WaitForSeconds(launchStealth);
            SpawnOutposts();

            yield return WaveLoop(0, waveCounts.Length);             // skirmish
            if (!_defeated) yield return CaptureLoop();               // capture outposts → allies
            if (!_defeated) yield return DefendLoop();                // hold the core against a push
            if (!_defeated) yield return UpgradeLoop();               // spend on a buff
            if (!_defeated) yield return BossLoop();                  // boss

            if (_stats != null)
            {
                _stats.endPhase = phase.ToString();   // phase the run actually ended in (before we flip to Defeat/Victory)
                _stats.result = _defeated ? "DEFEAT" : "VICTORY";
                _stats.livesUsed = Mathf.Max(0, _runStartLives - _lives);
                _stats.livesRemaining = Mathf.Max(0, _lives);
                _stats.outpostsCaptured = CapturedCount();
                _stats.Print();          // scorecard to Console
                MissionStats.Active = null;
            }
            phase = _defeated ? Phase.Defeat : Phase.Victory;
            ClearAll();
            Objective(_defeated ? "DEFEATED — press R to retry" : "VICTORY!  Sky Sentinel down — press R to replay");
            while (!Input.GetKeyDown(KeyCode.R)) yield return null;
        }
    }

    private IEnumerator WaveLoop(int from, int to)
    {
        for (int w = from; w < to && !_defeated; w++)
        {
            phase = Phase.Wave;
            SpawnWaveTuned(waveCounts[w], w);
            while (AliveCount() > 0)
            {
                if (PlayerDead() && !HandleDeath()) { _defeated = true; yield break; }
                Objective($"OBJECTIVE: Destroy all enemy drones  ·  Wave {w + 1}/{waveCounts.Length}, {AliveCount()} left    LIVES {_lives}");
                yield return null;
            }
            Objective($"Wave {w + 1} cleared!");
            if (_stats != null) _stats.waveClearTimes.Add(Time.time - _stats.startTime);
            yield return new WaitForSeconds(1.3f);
        }
    }

    private IEnumerator CaptureLoop()
    {
        phase = Phase.Capture;
        while (CapturedCount() < _outposts.Count)
        {
            if (PlayerDead() && !HandleDeath()) { _defeated = true; yield break; }
            Objective($"OBJECTIVE: Capture outposts  ·  fly INTO a bubble & hold until the orb turns BLUE  ·  {CapturedCount()}/{_outposts.Count}    LIVES {_lives}");
            yield return null;
        }
        Objective("Outposts secured — allies inbound!");
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator BossLoop()
    {
        phase = Phase.Boss;
        SpawnBoss();
        while (AliveCount() > 0)
        {
            if (PlayerDead() && !HandleDeath()) { _defeated = true; yield break; }
            Objective($"OBJECTIVE: Destroy the Boss  ·  {BossPct()}%    LIVES {_lives}");
            yield return null;
        }
    }

    private IEnumerator DefendLoop()
    {
        phase = Phase.Defend;
        TargetHealth core = SpawnCore();
        SpawnCoreGuardians(2);                    // two allied guardians help hold the line
        MarkObjective(core.transform, "CORE");    // on-screen marker + edge arrow so it's always findable
        int pushTier = waveCounts.Length;         // defend enemies are as tough as one wave past the skirmish
        float end = Time.time + defendDuration;
        float nextPush = Time.time;

        while (Time.time < end)
        {
            if (PlayerDead() && !HandleDeath()) { _defeated = true; ClearObjective(); yield break; }
            if (core == null || core.HealthFraction <= 0f)
            {
                _defeated = true;
                if (_stats != null) _stats.defeatReason = "The Core was destroyed by the enemy push";
                Objective("CORE DESTROYED — defense failed");
                ClearObjective();
                yield return new WaitForSeconds(1.5f);
                yield break;
            }
            // Cap on TOTAL live hostiles (push + any outpost-spawned) so it can't runaway-swarm the Core.
            if (Time.time >= nextPush && CountLiveEnemies() < defendMaxEnemies)
            {
                nextPush = Time.time + defendPushInterval;
                SpawnWaveTuned(defendPushCount, pushTier);
            }

            // Continuous self-repair: the Core always recovers a little, but any real push out-damages it.
            // Peeling/killing attackers (they retaliate when shot) is what saves it — a lone straggler is
            // survivable, a swarm is not. Regen rate is tuned just above one attacker's DPS (see Balance-Model).
            int nearCore = EnemiesNear(core.transform.position, coreSafeRadius);
            if (core.HealthFraction < 1f) core.Heal(coreRegenRate * Time.deltaTime);

            string status = nearCore >= 2 ? $"CORE UNDER HEAVY ATTACK — clear the {nearCore} on it!"
                          : nearCore == 1 ? "CORE UNDER ATTACK — peel that drone off!"
                          : "Core clear — repairing.";
            Objective($"DEFEND THE CORE (blue beam, centre)  ·  {status}  ·  Core {Mathf.CeilToInt(core.HealthFraction * 100f)}%  ·  {Mathf.CeilToInt(end - Time.time)}s    LIVES {_lives}");
            yield return null;
        }

        Objective("Core held! Enemy falling back…");
        ClearObjective();
        ClearEnemies();
        if (core != null) Destroy(core.gameObject);
        _core = null;
        yield return new WaitForSeconds(1.5f);
    }

    // Count live hostiles within radius of a point (used for the Core self-repair gate).
    private int EnemiesNear(Vector3 p, float radius)
    {
        int n = 0;
        var all = TargetHealth.All;
        for (int i = 0; i < all.Count; i++)
        {
            var t = all[i];
            if (t != null && t.team == EnemyTeam && t.HealthFraction > 0f && t.gameObject.activeInHierarchy
                && Vector3.Distance(t.transform.position, p) <= radius) n++;
        }
        return n;
    }

    private void MarkObjective(Transform t, string label)
    {
        if (_hud == null) _hud = FindFirstObjectByType<TouchFlightHUD>();
        if (_hud != null) _hud.SetObjectiveMarker(t, label);
    }

    private void ClearObjective()
    {
        if (_hud != null) _hud.SetObjectiveMarker(null, "");
    }

    private TargetHealth SpawnCore()
    {
        var root = new GameObject("Core");
        root.transform.position = arenaCenter + Vector3.up * 4f;

        var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vis.transform.SetParent(root.transform, false);
        vis.transform.localScale = Vector3.one * 8f;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        Color c = new Color(0.3f, 0.7f, 1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c * 1.5f);
        vis.GetComponent<Renderer>().sharedMaterial = mat;

        // Tall light-beam beacon so the Core is visible from anywhere on the map.
        var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var bc = beam.GetComponent<Collider>();
        if (bc != null) Destroy(bc);
        beam.transform.SetParent(root.transform, false);
        beam.transform.localScale = new Vector3(1.4f, 120f, 1.4f); // very tall thin pillar
        beam.transform.localPosition = Vector3.up * 120f;
        var bmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (bmat.HasProperty("_BaseColor")) bmat.SetColor("_BaseColor", c);
        bmat.EnableKeyword("_EMISSION");
        if (bmat.HasProperty("_EmissionColor")) bmat.SetColor("_EmissionColor", c * 3f);
        beam.GetComponent<Renderer>().sharedMaterial = bmat;

        var th = root.AddComponent<TargetHealth>();
        th.team = 1;
        th.Configure(coreHealth * CoreHpMul);  // fill HP now (avoids a 0-HP false "core destroyed" before Start)
        th.targetPriority = 3;     // enemies favour the core but still engage the player/allies nearby
        th.respawnOnDeath = false;
        th.freezeOnDeath = true;   // stay put at 0 HP so we can detect the loss

        var hb = root.AddComponent<HealthBar>();
        hb.offset = new Vector3(0f, 7f, 0f);
        hb.width = 8f;
        _core = th;
        return th;
    }

    private IEnumerator UpgradeLoop()
    {
        phase = Phase.Upgrade;
        int pick = 0;
        while (pick == 0)
        {
            if (PlayerDead() && !HandleDeath()) { _defeated = true; yield break; }
            Objective("UPGRADE:  [1/A] Fire Rate    [2/B] Damage    [3/X] Armor");
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.JoystickButton0)) pick = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.JoystickButton1)) pick = 2;
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.JoystickButton2)) pick = 3;
            yield return null;
        }
        ApplyUpgrade(pick);
        Objective("Upgrade installed!");
        yield return new WaitForSeconds(1.2f);
    }

    private void ApplyUpgrade(int pick)
    {
        if (player == null) return;
        var w = player.GetComponent<DroneWeapon>();
        switch (pick)
        {
            case 1: if (w != null) w.bulletRate *= 0.6f; break;              // faster fire
            case 2: if (w != null) w.bulletDamage *= 1.5f; break;            // more damage
            case 3: player.maxHealth += 100f; player.Heal(player.maxHealth); break; // armor
        }
        Debug.Log($"[UPGRADE] picked {pick}");
    }

    /// <summary>Player died: spend a life and respawn, or return false if out of lives (→ Defeat).</summary>
    private bool HandleDeath()
    {
        if (_lives <= 0)
        {
            if (_stats != null && string.IsNullOrEmpty(_stats.defeatReason))
                _stats.defeatReason = "Ran out of lives — your drone was destroyed too many times";
            return false;
        }
        _lives--;
        if (player != null)
        {
            player.transform.position = arenaCenter + Vector3.up * (spawnHeight + 12f); // safe, high, centre
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        }
        HealAndProtect(respawnProtection);
        Debug.Log($"[MISSION] respawn — {_lives} lives left");
        return true;
    }

    private void HealAndProtect(float seconds)
    {
        if (player == null) return;
        player.Heal(player.maxHealth);
        player.SetProtected(seconds);
    }

    // Enemies get tougher each wave: more HP, more damage, slightly faster fire — building to the boss.
    private void SpawnWaveTuned(int n, int waveIndex)
    {
        float hp = waveEnemyHealth * (1f + perWaveHealth * waveIndex) * EnemyHpMul;
        float dmg = enemyBaseDamage * (1f + perWaveDamage * waveIndex) * EnemyDmgMul;
        float fire = enemyBaseFire * Mathf.Max(0.35f, 1f - perWaveFireSpeedup * waveIndex);
        for (int i = 0; i < n; i++)
        {
            float a = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            Vector3 pos = arenaCenter
                        + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * spawnRadius
                        + Vector3.up * (spawnHeight + Random.Range(0f, 6f));
            Quaternion rot = Quaternion.LookRotation((arenaCenter - pos).normalized, Vector3.up);
            var e = CombatSpawner.Enemy(enemyPrefab, pos, rot, hp, enemyColor, EnemyTeam, false);
            if (e != null)
            {
                TuneEnemy(e, dmg, fire);
                _alive.Add(e.GetComponent<TargetHealth>());
            }
        }
    }

    // Apply per-wave weapon tuning after spawn (EnemyDroneAI sets the base rate in Awake).
    private void TuneEnemy(GameObject e, float dmg, float fire)
    {
        var w = e.GetComponent<DroneWeapon>();
        if (w != null) { w.bulletDamage = dmg; w.bulletRate = fire; }
        var ai = e.GetComponent<EnemyDroneAI>();
        if (ai != null) ai.fireInterval = fire;
    }

    private void SpawnBoss()
    {
        Vector3 pos = arenaCenter + Vector3.forward * (spawnRadius * 0.5f) + Vector3.up * (spawnHeight + 8f);
        var e = CombatSpawner.Enemy(enemyPrefab, pos, Quaternion.identity, bossHealth * EnemyHpMul, bossColor, 2, true);
        if (e != null)
        {
            var w = e.GetComponent<DroneWeapon>();
            if (w != null) w.bulletDamage *= EnemyDmgMul;   // scale boss damage with difficulty
            _alive.Add(e.GetComponent<TargetHealth>());
        }
    }

    private int AliveCount() { _alive.RemoveAll(t => t == null); return _alive.Count; }

    // Every live hostile in the scene (scripted push + any outpost-spawned), for the Defend cap.
    private int CountLiveEnemies()
    {
        int n = 0;
        var all = TargetHealth.All;
        for (int i = 0; i < all.Count; i++)
        {
            var t = all[i];
            if (t != null && t.team == EnemyTeam && t.gameObject.activeInHierarchy && t.HealthFraction > 0f) n++;
        }
        return n;
    }

    // Allied guardians that spawn near the Core to help defend it.
    private void SpawnCoreGuardians(int n)
    {
        for (int i = 0; i < n; i++)
        {
            float a = (i / (float)Mathf.Max(1, n)) * Mathf.PI * 2f;
            Vector3 p = arenaCenter + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 12f + Vector3.up * (spawnHeight + 4f);
            CombatSpawner.Ally(enemyPrefab, p, PlayerTeam, allyHealth);
        }
    }

    private void ClearEnemies()
    {
        foreach (var t in _alive) if (t != null) Destroy(t.gameObject);
        _alive.Clear();
    }

    private void SpawnOutposts()
    {
        var placed = new List<Vector3>();
        for (int i = 0; i < outpostCount; i++)
        {
            Vector3 pos = arenaCenter;
            for (int attempt = 0; attempt < 24; attempt++)   // random spot, spaced out from the others
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float r = Random.Range(outpostMinRadius, outpostMaxRadius);
                pos = arenaCenter + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                bool ok = true;
                foreach (var q in placed) if (Vector3.Distance(q, pos) < outpostSeparation) { ok = false; break; }
                if (ok) break;
            }
            placed.Add(pos);

            var go = new GameObject("Outpost_" + i);
            go.transform.position = pos;
            var o = go.AddComponent<Outpost>();
            o.dronePrefab = enemyPrefab;
            o.playerTeam = PlayerTeam;
            o.enemyTeam = EnemyTeam;
            o.allyHealth = allyHealth;
            o.enemyHealth = waveEnemyHealth;
            o.enemyColor = enemyColor;
            _outposts.Add(o);
        }
    }

    private int CapturedCount()
    {
        _outposts.RemoveAll(o => o == null);
        int c = 0;
        foreach (var o in _outposts) if (o != null && o.Captured) c++;
        return c;
    }

    // Full wipe for restart: enemies + allies (any EnemyDroneAI) + outposts.
    private void ClearAll()
    {
        ClearEnemies();
        foreach (var ai in Object.FindObjectsByType<EnemyDroneAI>(FindObjectsSortMode.None))
            if (ai != null) Destroy(ai.gameObject);
        foreach (var o in _outposts) if (o != null) Destroy(o.gameObject);
        _outposts.Clear();
        if (_core != null) { Destroy(_core.gameObject); _core = null; }
    }

    private bool PlayerDead() => player == null || player.HealthFraction <= 0f;

    private int BossPct()
    {
        foreach (var t in _alive) if (t != null) return Mathf.CeilToInt(t.HealthFraction * 100f);
        return 0;
    }

    private string _lastObjective;
    private void Objective(string s)
    {
        if (s == _lastObjective) return;   // objective loops run every frame — skip unchanged text
        _lastObjective = s;
        if (_hud == null) _hud = FindFirstObjectByType<TouchFlightHUD>();
        if (_hud != null) _hud.SetInfoText(s);
    }
}
