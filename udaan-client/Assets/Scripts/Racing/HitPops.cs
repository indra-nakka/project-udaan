using UnityEngine;

/// <summary>
/// Floating "pop" feedback on the PLAYER's hits — juice + readability for kids. Colorful bouncy numbers,
/// with an occasional cartoon word ("POW!", "SPLAT!") on big hits. Spawned once by the gameplay bootstrap.
/// Toggle with <see cref="Enabled"/> (wire to a settings option later).
/// </summary>
public class HitPops : MonoBehaviour
{
    public static bool Enabled = true;
    private static readonly string[] BigWords = { "POW!", "SPLAT!", "BONK!", "BOP!" };
    private Font _font;

    void Awake()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
    }

    void OnEnable()  { TargetHealth.OnAnyDamaged += OnDamaged; }
    void OnDisable() { TargetHealth.OnAnyDamaged -= OnDamaged; }

    private void OnDamaged(TargetHealth victim, int attackerTeam, float amount, bool fromPlayer)
    {
        if (!Enabled || !fromPlayer || victim == null) return;

        Vector3 pos = victim.transform.position + Vector3.up * 1.3f + Random.insideUnitSphere * 0.3f;
        bool big = amount >= 40f;
        string text = (big && Random.value < 0.5f) ? BigWords[Random.Range(0, BigWords.Length)] : Mathf.RoundToInt(amount).ToString();
        Color col = big ? new Color(1f, 0.6f, 0.2f) : new Color(1f, 0.92f, 0.35f);

        var go = new GameObject("HitPop") { transform = { position = pos } };
        var tm = go.AddComponent<TextMesh>();
        tm.text = text; tm.font = _font;
        tm.fontSize = 64; tm.characterSize = big ? 0.14f : 0.1f;
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold; tm.color = col;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && _font != null) mr.sharedMaterial = _font.material;
        go.AddComponent<HitPopAnim>();
    }
}

/// <summary>Bounces the pop up, gives it a little scale-punch, billboards it to the camera, fades, self-destructs.</summary>
public class HitPopAnim : MonoBehaviour
{
    private float _t;
    private Camera _cam;
    private Vector3 _vel;

    void Start() { _cam = Camera.main; _vel = Vector3.up * 2.2f + new Vector3(Random.Range(-0.4f, 0.4f), 0f, 0f); }

    void Update()
    {
        _t += Time.deltaTime;
        _vel.y -= 3f * Time.deltaTime;
        transform.position += _vel * Time.deltaTime;
        if (_cam != null) transform.rotation = _cam.transform.rotation;   // billboard
        transform.localScale = Vector3.one * (1f + Mathf.Clamp01(_t * 8f) * 0.35f);

        var tm = GetComponent<TextMesh>();
        if (tm != null) { var c = tm.color; c.a = 1f - _t / 0.9f; tm.color = c; }
        if (_t > 0.9f) Destroy(gameObject);
    }
}
