using UnityEngine;

/// <summary>
/// Greybox children's-park map. Procedurally scatters primitive props (trees, slides, swings, jungle
/// gyms, sandboxes, see-saws) + a perimeter fence to give the combat sandbox cover, verticality and
/// landmarks. Everything is a colored primitive with a collider — art comes later on the same layout.
/// </summary>
public class ParkMapGenerator : MonoBehaviour
{
    [Header("Area")]
    public Vector3 center = Vector3.zero;
    public float radius = 100f;
    [Tooltip("Keep props out of this radius around the spawn/center.")]
    public float clearCenter = 14f;

    [Header("Counts")]
    public int trees = 26;
    public int slides = 6;
    public int swings = 6;
    public int gyms = 5;
    public int sandboxes = 4;
    public int seesaws = 6;
    [Tooltip("Tall climbing-fort towers (cover well above ground).")]
    public int towers = 11;
    [Tooltip("Connected wall runs (each several segments) that form maze corridors at altitude.")]
    public int walls = 14;
    public bool perimeterFence = true;

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
        for (int i = 0; i < trees; i++) BuildTree(RandomGroundPos());
        for (int i = 0; i < slides; i++) BuildSlide(RandomGroundPos());
        for (int i = 0; i < swings; i++) BuildSwing(RandomGroundPos());
        for (int i = 0; i < gyms; i++) BuildGym(RandomGroundPos());
        for (int i = 0; i < sandboxes; i++) BuildSandbox(RandomGroundPos());
        for (int i = 0; i < seesaws; i++) BuildSeesaw(RandomGroundPos());
        for (int i = 0; i < towers; i++) BuildTower(RandomGroundPos());
        for (int i = 0; i < walls; i++) BuildWallCluster(RandomGroundPos());
        if (perimeterFence) BuildFence();
    }

    // Tall climbing fort: gives cover and platforms far above the ground.
    private void BuildTower(Vector3 p)
    {
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

    // A connected run of tall wall segments that turns ~90° between segments → maze corridors.
    private void BuildWallCluster(Vector3 start)
    {
        int segs = Random.Range(3, 6);
        Vector3 cursor = start;
        float heading = Random.Range(0f, 360f);
        float h = Random.Range(26f, 46f); // consistent height per run

        for (int i = 0; i < segs; i++)
        {
            Quaternion rot = Quaternion.Euler(0f, heading, 0f);
            Vector3 fwd = rot * Vector3.forward;
            float len = Random.Range(14f, 26f);
            Vector3 center = cursor + fwd * (len * 0.5f) + Vector3.up * (h * 0.5f);
            Prim(PrimitiveType.Cube, center, new Vector3(1.4f, h, len), rot, Green); // thin X, tall Y, long Z (along run)
            cursor += fwd * len;
            heading += (Random.value < 0.5f ? 90f : -90f) + Random.Range(-15f, 15f); // corner
        }
    }

    private Vector3 RandomGroundPos()
    {
        Vector2 c = Random.insideUnitCircle * radius;
        if (c.magnitude < clearCenter) c = c.normalized * clearCenter;
        return center + new Vector3(c.x, 0f, c.y);
    }

    // ---- props ----
    private void BuildTree(Vector3 p)
    {
        float h = Random.Range(4f, 8f);
        Prim(PrimitiveType.Cylinder, p + Vector3.up * (h * 0.5f), new Vector3(0.8f, h * 0.5f, 0.8f), Quaternion.identity, Bark);
        Prim(PrimitiveType.Sphere, p + Vector3.up * (h + 0.5f), Vector3.one * Random.Range(3.2f, 5f), Quaternion.identity, Leaf);
    }

    private void BuildSlide(Vector3 p)
    {
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(0f, 3f, -2f), new Vector3(3.5f, 0.4f, 3.5f), rot, Blue);           // top platform
        Prim(PrimitiveType.Cube, p + rot * new Vector3(-1.3f, 1.5f, -2f), new Vector3(0.3f, 3f, 0.3f), rot, Metal);       // post
        Prim(PrimitiveType.Cube, p + rot * new Vector3(1.3f, 1.5f, -2f), new Vector3(0.3f, 3f, 0.3f), rot, Metal);        // post
        Prim(PrimitiveType.Cube, p + rot * new Vector3(0f, 1.5f, 1.5f), new Vector3(1.8f, 0.25f, 6f), rot * Quaternion.Euler(32f, 0f, 0f), Yellow); // slide surface
    }

    private void BuildSwing(Vector3 p)
    {
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        // A-frame posts + top bar
        Prim(PrimitiveType.Cube, p + rot * new Vector3(-2.5f, 2.5f, 0f), new Vector3(0.35f, 5f, 0.35f), rot * Quaternion.Euler(0f, 0f, 12f), Red);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(2.5f, 2.5f, 0f), new Vector3(0.35f, 5f, 0.35f), rot * Quaternion.Euler(0f, 0f, -12f), Red);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(0f, 4.8f, 0f), new Vector3(6f, 0.35f, 0.35f), rot, Metal);
        // two seats
        Prim(PrimitiveType.Cube, p + rot * new Vector3(-1.2f, 1.2f, 0f), new Vector3(1f, 0.2f, 0.6f), rot, Yellow);
        Prim(PrimitiveType.Cube, p + rot * new Vector3(1.2f, 1.2f, 0f), new Vector3(1f, 0.2f, 0.6f), rot, Yellow);
    }

    private void BuildGym(Vector3 p)
    {
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
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
        float s = 5f, wall = 0.9f;
        Prim(PrimitiveType.Cube, p + new Vector3(0f, wall * 0.5f, s), new Vector3(s * 2f, wall, 0.5f), Quaternion.identity, Yellow);
        Prim(PrimitiveType.Cube, p + new Vector3(0f, wall * 0.5f, -s), new Vector3(s * 2f, wall, 0.5f), Quaternion.identity, Yellow);
        Prim(PrimitiveType.Cube, p + new Vector3(s, wall * 0.5f, 0f), new Vector3(0.5f, wall, s * 2f), Quaternion.identity, Yellow);
        Prim(PrimitiveType.Cube, p + new Vector3(-s, wall * 0.5f, 0f), new Vector3(0.5f, wall, s * 2f), Quaternion.identity, Yellow);
    }

    private void BuildSeesaw(Vector3 p)
    {
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
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
