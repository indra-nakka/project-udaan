using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Children's-park map. Scatters park models (or greybox primitives as fallback) so the combat sandbox
/// has cover, verticality and landmarks. Props are placed WITHOUT overlapping — each spawned model's
/// footprint is measured and the generator rejects positions that collide with already-placed props.
/// </summary>
public class ParkMapGenerator : MonoBehaviour
{
    [Header("Area")]
    public Vector3 center = Vector3.zero;
    public float radius = 100f;
    [Tooltip("Keep props out of this radius around the spawn/center.")]
    public float clearCenter = 14f;
    [Tooltip("Global size multiplier for all park models (2 = double).")]
    public float propScale = 2f;

    [Header("Counts (sparse — props are placed without overlapping)")]
    public int trees = 26;
    public int slides = 6;
    public int swings = 6;
    public int gyms = 5;
    public int sandboxes = 4;
    public int seesaws = 6;
    [Tooltip("Big combined multi-slide structures (the hero piece).")]
    public int playsets = 3;
    public int merries = 4;
    public int tyreSwings = 4;
    [Header("New stations (from the reference photos)")]
    public int rockWalls = 3;
    public int tyreWalls = 3;
    public int trampolines = 3;
    public int benches = 5;
    public int animalMerries = 2;
    [Tooltip("Giant shade-trees — tall cover / landmarks. The large props ARE the maze.")]
    public int towers = 14;
    public bool perimeterFence = true;
    [Tooltip("Extra gap (m) kept between any two placed props.")]
    public float propSpacing = 5f;

    // Playful palette.
    private static readonly Color Bark = new Color(0.42f, 0.27f, 0.14f);
    private static readonly Color Leaf = new Color(0.22f, 0.68f, 0.27f);
    private static readonly Color Red = new Color(0.9f, 0.25f, 0.25f);
    private static readonly Color Blue = new Color(0.25f, 0.5f, 0.95f);
    private static readonly Color Yellow = new Color(1f, 0.82f, 0.2f);
    private static readonly Color Orange = new Color(1f, 0.55f, 0.15f);
    private static readonly Color Green = new Color(0.3f, 0.75f, 0.35f);
    private static readonly Color Metal = new Color(0.7f, 0.72f, 0.75f);

    void Awake()
    {
        // Place BIG props first so they claim open ground; small props then fill the gaps around them.
        for (int i = 0; i < playsets; i++) BuildPlayset(RandomGroundPos());
        for (int i = 0; i < towers; i++) BuildTower(RandomGroundPos());
        for (int i = 0; i < slides; i++) BuildSlide(RandomGroundPos());
        for (int i = 0; i < gyms; i++) BuildGym(RandomGroundPos());
        for (int i = 0; i < sandboxes; i++) BuildSandbox(RandomGroundPos());
        for (int i = 0; i < merries; i++) BuildMerry(RandomGroundPos());
        for (int i = 0; i < animalMerries; i++) BuildProp(ParkProps.AnimalMerry, RandomGroundPos());
        for (int i = 0; i < tyreWalls; i++) BuildProp(ParkProps.TyreWall, RandomGroundPos());
        for (int i = 0; i < swings; i++) BuildSwing(RandomGroundPos());
        for (int i = 0; i < seesaws; i++) BuildSeesaw(RandomGroundPos());
        for (int i = 0; i < rockWalls; i++) BuildProp(ParkProps.RockWall, RandomGroundPos());
        for (int i = 0; i < tyreSwings; i++) BuildTyreSwing(RandomGroundPos());
        for (int i = 0; i < trampolines; i++) BuildProp(ParkProps.Trampoline, RandomGroundPos());
        for (int i = 0; i < trees; i++) BuildTree(RandomGroundPos());
        for (int i = 0; i < benches; i++) BuildProp(ParkProps.Bench, RandomGroundPos());
        if (perimeterFence) BuildFence();
    }

    // Tall cover far above the ground. With a tree model → a giant park shade-tree; else a greybox fort.
    private void BuildTower(Vector3 p)
    {
        if (ParkProps.Tree != null)
        {
            // Giant landmark shade-tree. (SpawnProp also multiplies by propScale, so this stays sane at 2×.)
            SpawnProp(ParkProps.Tree, p, RandomYaw(), Random.Range(2.4f, 3.8f));
            return;
        }
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        float h = Random.Range(30f, 52f);
        float w = Random.Range(4f, 6.5f);
        Vector3[] c = { new Vector3(-w, 0, -w), new Vector3(w, 0, -w), new Vector3(-w, 0, w), new Vector3(w, 0, w) };
        foreach (var v in c)
            Prim(PrimitiveType.Cube, p + rot * (v + Vector3.up * h * 0.5f), new Vector3(0.6f, h * 0.5f, 0.6f), rot, Orange);

        int levels = Mathf.Max(2, Mathf.RoundToInt(h / 6f));
        for (int l = 1; l <= levels; l++)
        {
            float y = l * (h / (levels + 1));
            Prim(PrimitiveType.Cube, p + rot * new Vector3(0f, y, 0f), new Vector3(w * 2f, 0.35f, w * 2f), rot, (l % 2 == 0) ? Blue : Yellow);
        }
        Prim(PrimitiveType.Cube, p + rot * new Vector3(0f, h, 0f), new Vector3(w * 2.4f, 0.4f, w * 2.4f), rot, Red); // roof
    }

    private Vector3 RandomGroundPos()
    {
        Vector2 c = Random.insideUnitCircle * radius;
        if (c.magnitude < clearCenter) c = c.normalized * clearCenter;
        return center + new Vector3(c.x, 0f, c.y);
    }

    private Quaternion RandomYaw() => Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

    // Placed props as (ground XZ position, footprint radius) so new props can avoid overlapping them.
    private readonly List<(Vector3 pos, float r)> _placed = new List<(Vector3, float)>();

    // Instantiate a park model, find a non-overlapping spot, sit its base on the ground, add collision.
    private void SpawnProp(GameObject model, Vector3 p, Quaternion rot, float scale = 1f)
    {
        var go = Instantiate(model);
        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(p, rot);
        go.transform.localScale = Vector3.one * (scale * propScale);

        float radius = FootprintRadius(go);          // measure this prop's XZ half-size
        Vector3 pos = FindOpenSpot(p, radius);        // reroll until it doesn't collide with placed props
        go.transform.position = pos;

        var rends = go.GetComponentsInChildren<Renderer>();   // models pivot at bounds-centre → drop base to ground
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            go.transform.position += Vector3.up * (pos.y - b.min.y);
        }
        AddColliders(go);
        _placed.Add((new Vector3(pos.x, 0f, pos.z), radius));
    }

    private float FootprintRadius(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return 2f;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return Mathf.Max(b.extents.x, b.extents.z) * 1.12f;   // small inflation so canopies/edges don't graze
    }

    // Try the first candidate, then re-roll random ground positions until one clears every placed prop.
    private Vector3 FindOpenSpot(Vector3 first, float radius)
    {
        Vector3 cand = first;
        for (int t = 0; t < 60; t++)
        {
            bool ok = true;
            for (int i = 0; i < _placed.Count; i++)
            {
                float minD = radius + _placed[i].r + propSpacing;
                float dx = cand.x - _placed[i].pos.x, dz = cand.z - _placed[i].pos.z;
                if (dx * dx + dz * dz < minD * minD) { ok = false; break; }
            }
            if (ok) return cand;
            cand = RandomGroundPos();
        }
        return cand;   // gave up after 60 tries — place anyway (rare, densest fill)
    }

    private void AddColliders(GameObject go)
    {
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null && mf.GetComponent<MeshCollider>() == null)
                mf.gameObject.AddComponent<MeshCollider>();
    }

    // ---- props ----
    private void BuildTree(Vector3 p)
    {
        if (ParkProps.Tree != null) { SpawnProp(ParkProps.Tree, p, RandomYaw()); return; }
        float h = Random.Range(4f, 8f);
        Prim(PrimitiveType.Cylinder, p + Vector3.up * (h * 0.5f), new Vector3(0.8f, h * 0.5f, 0.8f), Quaternion.identity, Bark);
        Prim(PrimitiveType.Sphere, p + Vector3.up * (h + 0.5f), Vector3.one * Random.Range(3.2f, 5f), Quaternion.identity, Leaf);
    }

    private void BuildSlide(Vector3 p)
    {
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        if (ParkProps.Slide != null) { SpawnProp(ParkProps.Slide, p, rot, 2f); return; }   // extra 2× (art direction)
        Prim(PrimitiveType.Cube, p + rot * new Vector3(0f, 3f, -2f), new Vector3(3.5f, 0.4f, 3.5f), rot, Blue);           // top platform
        Prim(PrimitiveType.Cube, p + rot * new Vector3(-1.3f, 1.5f, -2f), new Vector3(0.3f, 3f, 0.3f), rot, Metal);       // post
        Prim(PrimitiveType.Cube, p + rot * new Vector3(1.3f, 1.5f, -2f), new Vector3(0.3f, 3f, 0.3f), rot, Metal);        // post
        Prim(PrimitiveType.Cube, p + rot * new Vector3(0f, 1.5f, 1.5f), new Vector3(1.8f, 0.25f, 6f), rot * Quaternion.Euler(32f, 0f, 0f), Yellow); // slide surface
    }

    private void BuildSwing(Vector3 p)
    {
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        if (ParkProps.Swing != null) { SpawnProp(ParkProps.Swing, p, rot); return; }
        // A-frame posts + top bar
        Prim(PrimitiveType.Cube, p + rot * new Vector3(-2.5f, 2.5f, 0f), new Vector3(0.35f, 5f, 0.35f), rot * Quaternion.Euler(0f, 0f, 12f), Red);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(2.5f, 2.5f, 0f), new Vector3(0.35f, 5f, 0.35f), rot * Quaternion.Euler(0f, 0f, -12f), Red);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(0f, 4.8f, 0f), new Vector3(6f, 0.35f, 0.35f), rot, Metal);
        // two seats
        Prim(PrimitiveType.Cube, p + rot * new Vector3(-1.2f, 1.2f, 0f), new Vector3(1f, 0.2f, 0.6f), rot, Yellow);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(1.2f, 1.2f, 0f), new Vector3(1f, 0.2f, 0.6f), rot, Yellow);
    }

    // New Indian-park stations (model-only; additive — no primitive fallback needed).
    private void BuildProp(GameObject model, Vector3 p) { if (model != null) SpawnProp(model, p, RandomYaw()); }
    private void BuildMerry(Vector3 p)      { if (ParkProps.Merry != null)     SpawnProp(ParkProps.Merry, p, RandomYaw()); }
    private void BuildTyreSwing(Vector3 p)  { if (ParkProps.TyreSwing != null) SpawnProp(ParkProps.TyreSwing, p, RandomYaw()); }
    // Playset is huge — give it another 2× on top of propScale (per the art direction).
    private void BuildPlayset(Vector3 p)    { if (ParkProps.Playset != null)   SpawnProp(ParkProps.Playset, p, RandomYaw(), 2f); }

    private void BuildGym(Vector3 p)
    {
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        if (ParkProps.Dome != null) { SpawnProp(ParkProps.Dome, p, rot); return; }   // dome climber
        if (ParkProps.Gym != null)  { SpawnProp(ParkProps.Gym, p, rot); return; }
        float s = 4f;
        // four corner posts + top frame (a climbable cube)
        Vector3[] corners = { new Vector3(-s, 0f, -s), new Vector3(s, 0f, -s), new Vector3(-s, 0f, s), new Vector3(s, 0f, s) };
        foreach (var c in corners)
            Prim(PrimitiveType.Cube, p + rot * (c + Vector3.up * 2.5f), new Vector3(0.3f, 5f, 0.3f), rot, Orange);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(0f, 5f, -s), new Vector3(s * 2f, 0.3f, 0.3f), rot, Orange);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(0f, 5f, s), new Vector3(s * 2f, 0.3f, 0.3f), rot, Orange);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(-s, 5f, 0f), new Vector3(0.3f, 0.3f, s * 2f), rot, Orange);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(s, 5f, 0f), new Vector3(0.3f, 0.3f, s * 2f), rot, Orange);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(0f, 2.6f, 0f), new Vector3(s * 2f, 0.25f, 2f), rot, Blue); // mid platform (cover)
    }

    private void BuildSandbox(Vector3 p)
    {
        if (ParkProps.Sandbox != null) { SpawnProp(ParkProps.Sandbox, p, RandomYaw()); return; }
        float s = 5f, wall = 0.9f;
        Prim(PrimitiveType.Cube, p + new Vector3(0f, wall * 0.5f, s), new Vector3(s * 2f, wall, 0.5f), Quaternion.identity, Yellow);
        Prim(PrimitiveType.Cube, p + new Vector3(0f, wall * 0.5f, -s), new Vector3(s * 2f, wall, 0.5f), Quaternion.identity, Yellow);
        Prim(PrimitiveType.Cube, p + new Vector3(s, wall * 0.5f, 0f), new Vector3(0.5f, wall, s * 2f), Quaternion.identity, Yellow);
        Prim(PrimitiveType.Cube, p + new Vector3(-s, wall * 0.5f, 0f), new Vector3(0.5f, wall, s * 2f), Quaternion.identity, Yellow);
    }

    private void BuildSeesaw(Vector3 p)
    {
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        if (ParkProps.Seesaw != null) { SpawnProp(ParkProps.Seesaw, p, rot); return; }
        Prim(PrimitiveType.Cube, p + Vector3.up * 0.6f, new Vector3(0.8f, 1.2f, 0.8f), rot, Red);                         // fulcrum
        Prim(PrimitiveType.Cube, p + Vector3.up * 1.2f, new Vector3(0.6f, 0.2f, 6f), rot * Quaternion.Euler(9f, 0f, 0f), Green); // plank
    }

    private void BuildFence()
    {
        int posts = Mathf.Max(24, Mathf.RoundToInt(radius * 0.6f));
        for (int i = 0; i < posts; i++)
        {
            float a = (i / (float)posts) * Mathf.PI * 2f;
            Vector3 pos = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Prim(PrimitiveType.Cube, pos + Vector3.up * 1.5f, new Vector3(0.4f, 3f, 0.4f), Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a))), Metal);
        }
    }

    private GameObject Prim(PrimitiveType t, Vector3 pos, Vector3 scale, Quaternion rot, Color c)
    {
        var g = GameObject.CreatePrimitive(t);
        g.transform.SetParent(transform, false);
        g.transform.SetPositionAndRotation(pos, rot);
        g.transform.localScale = scale;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }
}
