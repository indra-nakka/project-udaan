using System.Collections;
using UnityEngine;

/// <summary>
/// The octacopter boss brain (sits on top of EnemyDroneAI). On spawn it makes an entrance (evil laugh +
/// shake), then every few seconds fires one of two TELEGRAPHED specials at the player: a SLIME-THROWER
/// fan (green globs) or a SHOCKWAVE ring (readable pulse → expanding blast + knockback). Kid-tone: the
/// slime just gunks you, the shockwave shoves you — non-lethal, fair, dramatic.
/// </summary>
public class BossController : MonoBehaviour
{
    public float specialInterval = 5f;
    public float slimeDamage = 16f;
    public float shockwaveDamage = 22f, shockwaveRange = 16f, shockwavePush = 14f;

    private TargetHealth _health;
    private TargetHealth _player;

    void Start()
    {
        _health = GetComponent<TargetHealth>();
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        Sfx.BossLaugh(transform.position);          // entrance
        CameraController.Shake(1.3f);
        yield return new WaitForSeconds(1.6f);

        int i = 0;
        while (_health != null && _health.HealthFraction > 0f)
        {
            yield return new WaitForSeconds(specialInterval);
            _player = FindPlayer();
            if (_player == null || _player.HealthFraction <= 0f) continue;
            if (i % 2 == 0) yield return SlimeThrower();
            else            yield return Shockwave();
            i++;
        }
    }

    // Pulse the boss's emission so the attack is fair + readable.
    private IEnumerator Telegraph(float seconds, Color c)
    {
        var rends = GetComponentsInChildren<Renderer>();
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = 0.5f + 0.5f * Mathf.Sin(t * 22f);
            foreach (var r in rends)
                if (r != null && r.material.HasProperty("_EmissionColor"))
                { r.material.EnableKeyword("_EMISSION"); r.material.SetColor("_EmissionColor", c * k * 2.2f); }
            yield return null;
        }
        foreach (var r in rends)
            if (r != null && r.material.HasProperty("_EmissionColor")) r.material.SetColor("_EmissionColor", Color.black);
    }

    private IEnumerator SlimeThrower()
    {
        yield return Telegraph(0.8f, new Color(0.3f, 1f, 0.3f));
        Sfx.Rocket(transform.position);
        Vector3 origin = transform.position + Vector3.down * 0.5f;
        Vector3 baseDir = (_player.transform.position - origin).normalized;
        for (int s = 0; s < 10; s++)
        {
            Vector3 d = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(-30f, 30f), 0f) * baseDir;
            SpawnSlime(origin + d * 2.5f, d * 20f);
            yield return new WaitForSeconds(0.06f);
        }
    }

    private void SpawnSlime(Vector3 pos, Vector3 vel)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "BossSlime";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.7f;
        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        Color c = new Color(0.3f, 0.85f, 0.35f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = vel;
        go.AddComponent<SlimeGlob>().damage = slimeDamage;
    }

    private IEnumerator Shockwave()
    {
        yield return Telegraph(1.0f, new Color(1f, 0.3f, 0.3f));
        Sfx.Explosion(transform.position);
        CameraController.Shake(1f);
        Vfx.Explode(transform.position, shockwaveRange, new Color(1f, 0.5f, 0.2f));   // expanding ring blast
        if (_player != null)
        {
            float d = Vector3.Distance(_player.transform.position, transform.position);
            if (d <= shockwaveRange)
            {
                _player.TakeDamage(shockwaveDamage, 2, false);
                var rb = _player.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddForce((_player.transform.position - transform.position).normalized * shockwavePush, ForceMode.VelocityChange);
            }
        }
        yield return null;
    }

    // The human player = a team-1 flying drone that is NOT AI-driven (allies have EnemyDroneAI).
    private TargetHealth FindPlayer()
    {
        foreach (var t in TargetHealth.All)
            if (t != null && t.team == 1 && t.GetComponent<DroneFlightController>() != null && t.GetComponent<EnemyDroneAI>() == null)
                return t;
        return null;
    }
}

/// <summary>A boss slime glob: gunks any team-1 target it touches, then poofs.</summary>
public class SlimeGlob : MonoBehaviour
{
    public float damage = 16f;
    private float _life;

    void Update() { _life += Time.deltaTime; if (_life > 4f) Destroy(gameObject); }

    void OnTriggerEnter(Collider other)
    {
        var th = other.GetComponentInParent<TargetHealth>();
        if (th != null && th.team == 1)
        {
            th.TakeDamage(damage, 2, false);
            Vfx.Poof(transform.position, 1f);
            Destroy(gameObject);
        }
    }
}
