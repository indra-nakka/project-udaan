using UnityEngine;

/// <summary>
/// Procedurally builds a simple circular drone-racing circuit of hoop gates at runtime, so feel
/// testing needs no hand-placed level. Each hoop is a torus mesh with a thin trigger disc across
/// the hole; gates are registered with a RaceManager in clockwise order. Also drops a large ground
/// plane so the hover assist has something to reference.
/// </summary>
public class RaceTrackGenerator : MonoBehaviour
{
    [Header("Circuit Layout")]
    [Tooltip("Build the hoop circuit. Off = combat sandbox (ground only, no rings).")]
    public bool buildGates = true;
    public int gateCount = 8;
    public float circuitRadius = 35f;
    public float gateHeight = 4f;
    [Tooltip("Random up/down offset per gate for a bit of vertical interest.")]
    public float heightVariation = 2.5f;

    [Header("Hoop Dimensions")]
    public float holeRadius = 3f;     // the gap you fly through
    public float tubeRadius = 0.35f;  // ring thickness
    public int ringSegments = 32;
    public int tubeSegments = 10;
    [Tooltip("Start/finish hoop is this much wider than a normal gate.")]
    public float startHoleScale = 1.5f;

    [Header("Scene")]
    public bool createGroundPlane = true;
    public RaceManager raceManager;   // auto-created if left empty

    private Mesh _hoopMesh;
    private Mesh _startMesh;
    private Material _hoopMat;

    /// <summary>Where the drone should start: just behind the first gate, facing into the circuit.</summary>
    public Pose StartPose { get; private set; }

    void Awake()
    {
        if (raceManager == null) raceManager = GetOrCreateManager();
        _hoopMat = CreateHoopMaterial();

        if (createGroundPlane) BuildGround();

        if (buildGates)
        {
            _hoopMesh = BuildTorus(holeRadius, tubeRadius, ringSegments, tubeSegments);
            _startMesh = BuildTorus(holeRadius * startHoleScale, tubeRadius * 1.2f, ringSegments, tubeSegments);
            BuildGates();
        }
        else
        {
            // Combat sandbox: no rings. Default a spawn pose just behind the arena centre.
            StartPose = new Pose(transform.position + Vector3.up * gateHeight - Vector3.forward * 8f, Quaternion.identity);
        }
    }

    private RaceManager GetOrCreateManager()
    {
        var existing = FindFirstObjectByType<RaceManager>();
        if (existing != null) return existing;
        return new GameObject("RaceManager").AddComponent<RaceManager>();
    }

    private void BuildGates()
    {
        Vector3 center = transform.position;
        float step = Mathf.PI * 2f / gateCount;

        // Regular gates, evenly spaced clockwise.
        for (int i = 0; i < gateCount; i++)
        {
            float angle = i * step;
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)); // clockwise travel dir
            float h = gateHeight + Mathf.Sin(angle * 2f) * heightVariation;
            Vector3 pos = center + radial * circuitRadius + Vector3.up * h;
            Quaternion rot = Quaternion.LookRotation(tangent, Vector3.up);        // hole axis (local Z) = travel dir

            var gate = CreateHoop($"Gate_{i}", pos, rot, _hoopMesh, holeRadius);
            raceManager.RegisterGate(gate);
        }

        // Dedicated start/finish line: half a step before gate 0, larger + gold.
        float sAngle = -step * 0.5f;
        Vector3 sRadial = new Vector3(Mathf.Cos(sAngle), 0f, Mathf.Sin(sAngle));
        Vector3 sTangent = new Vector3(-Mathf.Sin(sAngle), 0f, Mathf.Cos(sAngle));
        Vector3 sPos = center + sRadial * circuitRadius + Vector3.up * gateHeight;
        Quaternion sRot = Quaternion.LookRotation(sTangent, Vector3.up);

        var startGate = CreateHoop("Gate_StartFinish", sPos, sRot, _startMesh, holeRadius * startHoleScale);
        raceManager.RegisterStartGate(startGate);

        // Drone begins ~8 units behind the start line, nose pointed through it.
        StartPose = new Pose(sPos - sTangent * 8f, sRot);
    }

    private RaceGate CreateHoop(string name, Vector3 pos, Quaternion rot, Mesh mesh, float triggerHalfExtent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(pos, rot);

        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = _hoopMat;

        // Thin trigger disc filling the hole; thin along local Z (the through-axis).
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(triggerHalfExtent * 2f, triggerHalfExtent * 2f, 0.6f);

        return go.AddComponent<RaceGate>();
    }

    private void BuildGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "RaceGround";
        ground.transform.position = transform.position;
        ground.transform.localScale = Vector3.one * (circuitRadius * 0.8f); // Plane is 10 units per scale
        var r = ground.GetComponent<Renderer>();
        var m = CreateLitMaterial(new Color(0.12f, 0.13f, 0.16f));
        r.sharedMaterial = m;
    }

    // ---------------------------------------------------------------------------------------------
    // Torus mesh with its hole axis along local +Z, so LookRotation(travelDir) aligns fly-through.
    // ---------------------------------------------------------------------------------------------
    private Mesh BuildTorus(float ringRadius, float tube, int ringSeg, int tubeSeg)
    {
        var mesh = new Mesh { name = "HoopTorus" };
        int vCount = (ringSeg + 1) * (tubeSeg + 1);
        var verts = new Vector3[vCount];
        var normals = new Vector3[vCount];
        var uvs = new Vector2[vCount];

        int vi = 0;
        for (int i = 0; i <= ringSeg; i++)
        {
            float u = (i / (float)ringSeg) * Mathf.PI * 2f;
            Vector3 radial = new Vector3(Mathf.Cos(u), Mathf.Sin(u), 0f); // ring lies in local XY plane
            Vector3 ringCenter = radial * ringRadius;

            for (int j = 0; j <= tubeSeg; j++)
            {
                float v = (j / (float)tubeSeg) * Mathf.PI * 2f;
                Vector3 dir = radial * Mathf.Cos(v) + Vector3.forward * Mathf.Sin(v);
                verts[vi] = ringCenter + dir * tube;
                normals[vi] = dir;
                uvs[vi] = new Vector2(i / (float)ringSeg, j / (float)tubeSeg);
                vi++;
            }
        }

        var tris = new int[ringSeg * tubeSeg * 6];
        int ti = 0, stride = tubeSeg + 1;
        for (int i = 0; i < ringSeg; i++)
        {
            for (int j = 0; j < tubeSeg; j++)
            {
                int a = i * stride + j;
                int b = (i + 1) * stride + j;
                int c = (i + 1) * stride + (j + 1);
                int d = i * stride + (j + 1);
                tris[ti++] = a; tris[ti++] = b; tris[ti++] = c;
                tris[ti++] = a; tris[ti++] = c; tris[ti++] = d;
            }
        }

        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    private Material CreateHoopMaterial()
    {
        var m = CreateLitMaterial(new Color(0.15f, 0.35f, 0.55f));
        m.EnableKeyword("_EMISSION");
        return m;
    }

    private Material CreateLitMaterial(Color color)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        var m = new Material(s);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        return m;
    }
}
