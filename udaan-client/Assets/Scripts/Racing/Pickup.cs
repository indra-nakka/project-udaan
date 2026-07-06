using UnityEngine;

/// <summary>
/// Floating ammo cache / health pack. A trigger the player flies through to resupply; it disappears
/// on pickup and respawns after a delay. Builds its own bobbing, spinning colored visual.
/// </summary>
public class Pickup : MonoBehaviour
{
    public enum Kind { Ammo, Health }

    public Kind kind = Kind.Ammo;
    public float healAmount = 120f;
    public float respawnDelay = 8f;
    public float pickupRadius = 3f;
    public int forTeam = 1; // only this faction can collect

    private Transform _visual;
    private Collider _col;
    private float _reactivateAt = -1f;
    private float _baseY;

    void Start()
    {
        var sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = pickupRadius;
        _col = sc;

        Color c = kind == Kind.Health ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.7f, 0.15f);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cc = cube.GetComponent<Collider>();
        if (cc != null) Destroy(cc);
        _visual = cube.transform;
        _visual.SetParent(transform, false);
        _visual.localScale = Vector3.one * 1.3f;
        _visual.localPosition = Vector3.zero;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c * 1.6f);
        cube.GetComponent<Renderer>().sharedMaterial = mat;

        _baseY = transform.position.y;
    }

    void Update()
    {
        if (_reactivateAt > 0f && Time.time >= _reactivateAt) SetAvailable(true);

        if (_visual != null && _visual.gameObject.activeSelf)
        {
            _visual.Rotate(Vector3.up, 80f * Time.deltaTime, Space.World);
            _visual.localPosition = Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.3f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_reactivateAt > 0f) return; // already consumed, waiting to respawn
        if (other.GetComponentInParent<EnemyDroneAI>() != null) return; // AI (ally/enemy) can't collect
        var th = other.GetComponentInParent<TargetHealth>();
        if (th == null || th.team != forTeam) return;

        if (kind == Kind.Health) th.Heal(healAmount);
        else { var w = th.GetComponent<DroneWeapon>(); if (w != null) w.ResupplyAmmo(); }
        Debug.Log($"[PICKUP] {kind} collected by {th.name}");

        SetAvailable(false);
        _reactivateAt = Time.time + respawnDelay;
    }

    private void SetAvailable(bool on)
    {
        if (_visual != null) _visual.gameObject.SetActive(on);
        if (_col != null) _col.enabled = on;
        if (on) _reactivateAt = -1f;
    }
}
