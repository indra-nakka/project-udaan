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
    public RaceTrackGenerator trackGenerator;

    [Header("Targets (to test weapons/aim)")]
    public bool spawnTargets = true;
    public int targetCount = 6;
    public float targetAreaRadius = 25f;
    public float targetHeight = 5f;

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
                var go = new GameObject("RaceTrack");
                trackGenerator = go.AddComponent<RaceTrackGenerator>(); // Awake builds gates immediately
            }
            start = trackGenerator.StartPose;
        }

        var drone = SpawnDrone(start);

        // Hand the drone to the race manager so it owns placement + the 3-2-1-GO countdown + restart.
        if (drone != null && buildTrack && trackGenerator != null && trackGenerator.raceManager != null)
            trackGenerator.raceManager.SetupRace(drone, start);

        if (spawnTargets) SpawnTargets();
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
            go.AddComponent<TargetHealth>();
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
