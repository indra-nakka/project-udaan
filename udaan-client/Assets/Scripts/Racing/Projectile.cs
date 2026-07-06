using UnityEngine;

/// <summary>
/// A pooled projectile (bullet or rocket). Flies straight, damages the first TargetHealth it hits
/// (or everything in a splash radius for rockets), then returns itself to its pool. Object pooling
/// is mandatory for projectiles on mobile (see architecture/invariants.md).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private Rigidbody _rb;
    private TrailRenderer _trail;
    private float _dieAt, _damage, _splash;
    private int _ownerTeam;
    private bool _fromPlayer;
    private bool _spent;
    private System.Action<Projectile> _onDone;

    void Awake() { _rb = GetComponent<Rigidbody>(); _trail = GetComponent<TrailRenderer>(); }

    public void Launch(Vector3 pos, Vector3 dir, Vector3 inheritVel, float speed, float damage,
                       float life, float splash, int ownerTeam, bool fromPlayer, Collider[] ignore, System.Action<Projectile> onDone)
    {
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
        _damage = damage; _splash = splash; _ownerTeam = ownerTeam; _fromPlayer = fromPlayer; _onDone = onDone; _spent = false;
        _dieAt = Time.time + life;

        gameObject.SetActive(true);
        if (_trail != null) _trail.Clear(); // no streak from the pooled previous position
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = inheritVel + dir * speed;
            _rb.angularVelocity = Vector3.zero;
        }

        // Don't collide with the drone that fired us.
        var mine = GetComponent<Collider>();
        if (ignore != null && mine != null)
            foreach (var c in ignore) if (c != null) Physics.IgnoreCollision(mine, c, true);
    }

    void Update()
    {
        if (!_spent && Time.time >= _dieAt) Done();
    }

    void OnCollisionEnter(Collision col)
    {
        if (_spent) return;

        if (_splash > 0f)
        {
            foreach (var c in Physics.OverlapSphere(transform.position, _splash))
            {
                var h = c.GetComponentInParent<TargetHealth>();
                if (h != null && h.team != _ownerTeam) h.TakeDamage(_damage, _ownerTeam, _fromPlayer);
            }
            Vfx.Explode(transform.position, _splash, new Color(1f, 0.5f, 0.2f)); // rocket burst
        }
        else
        {
            var h = col.collider.GetComponentInParent<TargetHealth>();
            if (h != null && h.team != _ownerTeam) h.TakeDamage(_damage, _ownerTeam, _fromPlayer);
            Vfx.Spark(transform.position, new Color(1f, 0.85f, 0.3f)); // bullet spark
        }
        Done();
    }

    private void Done()
    {
        _spent = true;
        if (_rb != null) { _rb.linearVelocity = Vector3.zero; _rb.isKinematic = true; }
        gameObject.SetActive(false);
        _onDone?.Invoke(this);
    }
}
