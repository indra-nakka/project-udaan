using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A CONTESTABLE outpost (the Precinct-Assault seed). A small beacon inside a translucent capture
/// bubble. Whichever side has drones alone inside the bubble fills the capture bar; hold it long
/// enough and the outpost flips to that team — the orb turns blue (player) or red (enemy) and its
/// glow switches on. While owned it periodically spawns that team's drones (allies for the player,
/// hostiles for the enemy), so both sides fight over it. Neutral = white orb, glow off.
/// </summary>
public class Outpost : MonoBehaviour
{
    [Header("Capture")]
    public float captureRadius = 11f;
    public float captureTime = 3.5f;

    [Header("Reinforcements")]
    public float spawnInterval = 6f;
    public int maxUnits = 2;

    // Injected by the MissionDirector.
    [HideInInspector] public GameObject dronePrefab;
    [HideInInspector] public int playerTeam = 1;
    [HideInInspector] public int enemyTeam = 2;
    [HideInInspector] public float allyHealth = 80f;
    [HideInInspector] public float enemyHealth = 100f;
    [HideInInspector] public Color enemyColor = new Color(1f, 0.35f, 0.3f);

    /// <summary>Live registry of active outposts (avoids per-frame FindObjectsByType in the radar).</summary>
    public static readonly List<Outpost> All = new List<Outpost>();

    /// <summary>Fired when an outpost flips owner: (outpost, newOwnerTeam).</summary>
    public static event System.Action<Outpost, int> OnCaptured;

    /// <summary>0 = neutral, 1 = player-owned, 2 = enemy-owned.</summary>
    public int OwnerTeam { get; private set; }
    public bool Captured => OwnerTeam == playerTeam;   // kept for the director's player-captured count
    public float Progress01 => captureTime > 0f ? Mathf.Clamp01(_progress / captureTime) : 0f;

    private int _contender;          // team currently filling the bar (0 = none)
    private float _progress, _nextSpawn;
    private readonly List<TargetHealth> _units = new List<TargetHealth>();

    private Transform _orb;
    private Material _orbMat, _zoneMat;
    private const float OrbHeight = 6f;

    private static readonly Color Neutral = new Color(0.92f, 0.92f, 0.95f);
    private static readonly Color PlayerCol = new Color(0.3f, 0.62f, 1f);
    private static readonly Color EnemyCol = new Color(1f, 0.3f, 0.28f);
    private Color ColorFor(int team) => team == playerTeam ? PlayerCol : team == enemyTeam ? EnemyCol : Neutral;

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start() { BuildVisual(); }

    void Update()
    {
        int solo = SoloTeamInside();

        if (solo != 0 && solo != OwnerTeam)               // a single side is capturing (from neutral or a flip)
        {
            if (_contender != solo) { _contender = solo; _progress = 0f; }
            _progress += Time.deltaTime;
            if (_progress >= captureTime) SetOwner(solo);
        }
        else                                              // empty, contested, or the owner is home → bar decays
        {
            _progress = Mathf.Max(0f, _progress - Time.deltaTime * 0.75f);
            if (_progress <= 0f) _contender = 0;
        }

        UpdateVisual();
        if (OwnerTeam != 0) TrySpawn();
    }

    private static readonly Collider[] _zoneBuf = new Collider[24]; // shared scratch for the capture check

    // Which team (if any) is alone inside the bubble. 0 = nobody, contested, or mixed.
    private int SoloTeamInside()
    {
        bool p = false, e = false;
        int n = Physics.OverlapSphereNonAlloc(transform.position, captureRadius, _zoneBuf);
        for (int i = 0; i < n; i++)
        {
            var th = _zoneBuf[i].GetComponentInParent<TargetHealth>();
            if (th == null || th.HealthFraction <= 0f) continue;
            if (th.team == playerTeam) p = true;
            else if (th.team == enemyTeam) e = true;
        }
        if (p && !e) return playerTeam;
        if (e && !p) return enemyTeam;
        return 0;
    }

    private void SetOwner(int team)
    {
        OwnerTeam = team;
        _contender = 0;
        _progress = 0f;
        _nextSpawn = Time.time + 1f;
        Vfx.Explode(transform.position + Vector3.up * OrbHeight, 5f, ColorFor(team)); // claimed pop
        Debug.Log($"[OUTPOST] {name} captured by {(team == playerTeam ? "PLAYER" : "ENEMY")}");
        OnCaptured?.Invoke(this, team);
    }

    private void TrySpawn()
    {
        _units.RemoveAll(u => u == null);
        if (_units.Count >= maxUnits || Time.time < _nextSpawn) return;
        _nextSpawn = Time.time + spawnInterval;

        Vector3 p = transform.position + Vector3.up * (OrbHeight + 4f) + Random.insideUnitSphere * 3f;
        GameObject u = OwnerTeam == playerTeam
            ? CombatSpawner.Ally(dronePrefab, p, playerTeam, allyHealth)
            : CombatSpawner.Enemy(dronePrefab, p, Quaternion.identity, enemyHealth, enemyColor, enemyTeam, false);
        if (u != null) _units.Add(u.GetComponent<TargetHealth>());
    }

    private void UpdateVisual()
    {
        Color owned = ColorFor(OwnerTeam);
        float k = Progress01;
        // While a side is capturing, the orb bleeds toward that side's colour.
        Color show = _contender != 0 ? Color.Lerp(owned, ColorFor(_contender), k) : owned;

        if (_orbMat != null)
        {
            if (_orbMat.HasProperty("_BaseColor")) _orbMat.SetColor("_BaseColor", show);
            if (_orbMat.HasProperty("_Color")) _orbMat.SetColor("_Color", show);
            if (_orbMat.HasProperty("_EmissionColor"))
            {
                float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 4f);
                float glow = OwnerTeam != 0 ? (1.6f + pulse) : (0.05f + k * 1.6f); // glow ON when owned, OFF when neutral
                _orbMat.SetColor("_EmissionColor", show * glow);
            }
        }
        if (_orb != null) _orb.localScale = Vector3.one * (2f + (OwnerTeam != 0 ? Mathf.Sin(Time.time * 5f) * 0.2f : k * 0.3f));

        if (_zoneMat != null)
        {
            Color z = owned; z.a = 0.14f + k * 0.12f;
            if (_zoneMat.HasProperty("_BaseColor")) _zoneMat.SetColor("_BaseColor", z);
            if (_zoneMat.HasProperty("_Color")) _zoneMat.SetColor("_Color", z);
        }
    }

    private void BuildVisual()
    {
        // Small beacon post.
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.SetParent(transform, false);
        pole.transform.localScale = new Vector3(0.6f, 2.6f, 0.6f);
        pole.transform.localPosition = Vector3.up * 2.6f;
        PaintOpaque(pole, new Color(0.5f, 0.5f, 0.55f), false);

        // Orb on top — white when neutral, blue/red when owned.
        var orbGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orbGo.transform.SetParent(transform, false);
        orbGo.transform.localScale = Vector3.one * 2f;
        orbGo.transform.localPosition = Vector3.up * OrbHeight;
        _orb = orbGo.transform;
        PaintOpaque(orbGo, Neutral, true);
        _orbMat = orbGo.GetComponent<Renderer>().material;

        // Translucent capture bubble (no collider — purely a visual zone).
        var zone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var zc = zone.GetComponent<Collider>();
        if (zc != null) Destroy(zc);
        zone.transform.SetParent(transform, false);
        zone.transform.localScale = Vector3.one * (captureRadius * 2f);
        zone.transform.localPosition = Vector3.up * 0.5f;
        _zoneMat = MakeTransparent(new Color(Neutral.r, Neutral.g, Neutral.b, 0.14f));
        zone.GetComponent<Renderer>().sharedMaterial = _zoneMat;
    }

    private void PaintOpaque(GameObject go, Color c, bool emissive)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (emissive) { m.EnableKeyword("_EMISSION"); if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 1.2f); }
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    // URP transparent unlit material for the capture bubble.
    private static Material MakeTransparent(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        var m = new Material(sh);
        m.SetFloat("_Surface", 1f);   // 0 opaque, 1 transparent
        m.SetFloat("_Blend", 0f);     // alpha blend
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        return m;
    }
}
