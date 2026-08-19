using UnityEngine;
using Unity.Netcode;

/// <summary>
/// One-component setup for the offline single-player flight toy. Drop this on an empty GameObject
/// in a scene, assign the Drone_Player prefab, press Play: it builds the hoop circuit and spawns a
/// controllable drone at the start line — no NetworkManager host required.
///
/// If a real network session is already running, this stays out of the way (the normal
/// DroneClassSpawner path owns spawning in multiplayer).
/// </summary>
public class SinglePlayerBootstrap : MonoBehaviour
{
    [Header("Drone")]
    [Tooltip("Assign Assets/Prefabs/Drone_Player.prefab")]
    public GameObject dronePrefab;
    [Tooltip("Optional: flight/stat profile to apply. Falls back to prefab defaults if empty.")]
    public DroneClassData classData;

    [Header("Track")]
    public bool buildTrack = true;
    [Tooltip("Build the hoop race circuit + countdown. Off = combat sandbox (ground only).")]
    public bool buildGates = false;
    public RaceTrackGenerator trackGenerator;

    [Header("Targets (to test weapons/aim)")]
    public bool spawnTargets = false;
    public int targetCount = 6;
    public float targetAreaRadius = 25f;
    public float targetHeight = 5f;

    [Header("Enemies (Tier-1 AI)")]
    public bool spawnEnemies = true;
    public int enemyCount = 3;
    public float enemyHealth = 100f;
    public float playerHealth = 300f;
    public Color enemyColor = new Color(1f, 0.35f, 0.3f);
    [Tooltip("How far from the arena centre enemies spawn (player starts near the centre).")]
    public float enemySpawnDistance = 75f;

    [Header("Arena")]
    public bool hardBoundary = true;
    public float arenaRadius = 120f;
    public bool buildMap = true;

    [Header("Pickups")]
    public bool spawnPickups = true;
    public int ammoPickups = 6;
    public int healthPickups = 4;
    public float pickupHeight = 6f;

    [Header("Mission")]
    [Tooltip("Run the 'Sky Sentinel' waves→boss mission. Off = plain enemy sandbox.")]
    public bool missionMode = true;

    private const int TeamPlayer = 1;
    private const int TeamEnemy = 2;

    void Start()
    {
        // Defer to the networked path if a session is live.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) return;

        EnsureCamera();

        Pose start = new Pose(new Vector3(0f, 3f, -8f), Quaternion.identity);
        if (buildTrack)
        {
            if (trackGenerator == null)
            {
                // Create inactive so we can set buildGates BEFORE the generator's Awake runs.
                var go = new GameObject("RaceTrack");
                go.SetActive(false);
                trackGenerator = go.AddComponent<RaceTrackGenerator>();
                trackGenerator.buildGates = buildGates;
                go.SetActive(true);
            }
            start = trackGenerator.StartPose;
        }

        // Hard arena boundary (sphere).
        ArenaBounds.Enabled = hardBoundary;
        ArenaBounds.Center = (buildTrack && trackGenerator != null) ? trackGenerator.transform.position : transform.position;
        ArenaBounds.Radius = arenaRadius;

        // Greybox children's-park cover. Inactive-then-activate so center/radius are set before its Awake.
        if (buildMap)
        {
            var mgo = new GameObject("ParkMap");
            mgo.SetActive(false);
            var pm = mgo.AddComponent<ParkMapGenerator>();
            pm.center = ArenaBounds.Center;
            pm.radius = arenaRadius * 0.92f;
            mgo.SetActive(true);
        }

        var drone = SpawnDrone(start);

        // Race countdown/laps only when the hoop circuit exists; combat sandbox flies immediately.
        if (drone != null && buildTrack && buildGates && trackGenerator != null && trackGenerator.raceManager != null)
            trackGenerator.raceManager.SetupRace(drone, start);

        // Magenta practice dummies removed from the combat sandbox (set spawnTargets + call back to re-enable).
        if (missionMode) StartMission(drone);
        else if (spawnEnemies) SpawnEnemies();
        if (spawnPickups) SpawnPickups();
    }

    private void StartMission(GameObject drone)
    {
        if (drone == null || dronePrefab == null) return;
        var md = new GameObject("MissionDirector").AddComponent<MissionDirector>();
        md.difficulty = GameConfig.Difficulty;   // set by the start menu
        md.enemyPrefab = dronePrefab;
        md.arenaCenter = ArenaBounds.Center;
        md.spawnRadius = Mathf.Min(enemySpawnDistance, arenaRadius * 0.85f);
        md.spawnHeight = 12f;
        md.player = drone.GetComponent<TargetHealth>();
        md.enemyColor = enemyColor;

        new GameObject("HitPops").AddComponent<HitPops>();   // floating hit feedback on the player's shots
    }

    private void SpawnPickups()
    {
        Vector3 center = ArenaBounds.Center;
        float r = arenaRadius * 0.8f;
        for (int i = 0; i < ammoPickups; i++) MakePickup(Pickup.Kind.Ammo, center, r);
        for (int i = 0; i < healthPickups; i++) MakePickup(Pickup.Kind.Health, center, r);
    }

    private void MakePickup(Pickup.Kind kind, Vector3 center, float r)
    {
        Vector2 c = Random.insideUnitCircle * r;
        var go = new GameObject("Pickup_" + kind);
        go.transform.position = center + new Vector3(c.x, pickupHeight, c.y);
        var p = go.AddComponent<Pickup>();
        p.kind = kind;
        p.forTeam = TeamPlayer;
    }

    /// <summary>Spawn Tier-1 AI enemy drones (reuse the player prefab; EnemyDroneAI strips player control).</summary>
    private void SpawnEnemies()
    {
        if (dronePrefab == null) return;
        Vector3 center = (buildTrack && trackGenerator != null) ? trackGenerator.transform.position : transform.position;
        float dist = Mathf.Min(enemySpawnDistance, arenaRadius * 0.85f); // stay inside the boundary
        float baseH = (buildTrack && trackGenerator != null) ? trackGenerator.gateHeight : 6f;

        for (int i = 0; i < enemyCount; i++)
        {
            float ang = (i / (float)enemyCount) * Mathf.PI * 2f + 0.3f;
            Vector3 pos = center
                        + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist
                        + Vector3.up * (baseH + 6f + Random.Range(0f, 6f));

            var e = Instantiate(dronePrefab, pos, Quaternion.LookRotation((center - pos).normalized, Vector3.up));
            e.name = "Enemy_" + i;

            var rb = e.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

            var th = e.GetComponent<TargetHealth>();
            if (th == null) th = e.AddComponent<TargetHealth>();
            th.team = TeamEnemy;
            th.maxHealth = enemyHealth;
            th.targetPriority = 1;      // enemy drones lock before other things
            th.respawnOnDeath = false;  // enemies die (no respawn)

            TintRenderers(e, enemyColor); // stand out from the player

            e.AddComponent<EnemyDroneAI>();
        }
    }

    private void TintRenderers(GameObject go, Color c)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var m = r.material; // per-enemy instance
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }

    /// <summary>Procedural shooting-range dummies (sphere + TargetHealth) so weapons have something to hit.</summary>
    private void SpawnTargets()
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        Color c = new Color(1f, 0.15f, 0.9f); // bright magenta — pops against the ground/sky
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c * 1.6f); // glow so they're easy to spot

        // Spawn along the gate ring so targets sit among the checkpoints, not off in the distance.
        bool haveTrack = buildTrack && trackGenerator != null;
        Vector3 center = haveTrack ? trackGenerator.transform.position : transform.position;
        float ringR = haveTrack ? trackGenerator.circuitRadius : targetAreaRadius;
        float baseH = haveTrack ? trackGenerator.gateHeight : targetHeight;

        for (int i = 0; i < targetCount; i++)
        {
            // Offset half a gate-step so targets sit between gates, near the racing line.
            float ang = ((i + 0.5f) / targetCount) * Mathf.PI * 2f;
            Vector3 pos = center
                        + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (ringR + Random.Range(-5f, 5f))
                        + Vector3.up * (baseH + Random.Range(-1.5f, 2.5f));

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "TargetDummy";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 2f;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            go.AddComponent<NetworkObject>();  // keeps TargetHealth (a NetworkBehaviour) happy; inert offline
            var th = go.AddComponent<TargetHealth>();
            th.team = TeamEnemy; // shootable by the player; ignored by enemy drones
        }
    }

    private GameObject SpawnDrone(Pose pose)
    {
        if (dronePrefab == null)
        {
            Debug.LogError("SinglePlayerBootstrap: no dronePrefab assigned — assign Drone_Player.prefab in the inspector.");
            return null;
        }

        var drone = Instantiate(dronePrefab, pose.position, pose.rotation);
        drone.name = "Drone_Player (SinglePlayer)";

        var rb = drone.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // prefab Rigidbody may start kinematic (NGO); enable offline flight
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        var flight = drone.GetComponent<DroneFlightController>();
        if (flight != null)
        {
            flight.autoLevelAssist = false; // hold attitude for aiming (override any old prefab value)
            if (classData != null) flight.InitializeClassData(classData);
        }

        // Make the player a faction-1 target so enemies can lock/damage it.
        var ph = drone.GetComponent<TargetHealth>();
        if (ph == null) ph = drone.AddComponent<TargetHealth>();
        ph.team = TeamPlayer;
        ph.maxHealth = playerHealth;
        ph.freezeOnDeath = missionMode; // death = mission Defeat, not a silent respawn

        return drone;
    }

    private void EnsureCamera()
    {
        if (Camera.main != null) return;
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
    }
}
