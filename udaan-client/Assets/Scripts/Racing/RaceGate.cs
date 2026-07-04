using UnityEngine;

/// <summary>
/// One hoop in the circuit. Carries its order index and reports to the RaceManager when the drone
/// flies through its trigger disc. Provides a highlight so the player can see the next gate to hit.
/// The dedicated start/finish line sets <see cref="isStartFinish"/> and uses a distinct palette.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RaceGate : MonoBehaviour
{
    [HideInInspector] public int gateIndex;
    [HideInInspector] public RaceManager manager;
    [HideInInspector] public bool isStartFinish;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    // Regular gate palette.
    private static readonly Color GateDim  = new Color(0.15f, 0.35f, 0.55f);
    private static readonly Color GateNext = new Color(0.1f, 1f, 0.4f);   // green = fly through me next
    // Start/finish palette.
    private static readonly Color StartDim  = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color StartNext = new Color(1f, 0.85f, 0.1f);  // gold = start / lap line

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
        SetHighlighted(false);
    }

    void OnTriggerEnter(Collider other)
    {
        // Accept the drone whether the collider is on the root or a child of it.
        var drone = other.GetComponentInParent<DroneFlightController>();
        if (drone == null) return;
        if (manager != null) manager.NotifyGatePassed(this);
    }

    public void SetHighlighted(bool isNext)
    {
        if (_renderers == null) return;
        Color c = isStartFinish ? (isNext ? StartNext : StartDim)
                                : (isNext ? GateNext  : GateDim);
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", c);       // URP/Lit
            _mpb.SetColor("_Color", c);            // Standard fallback
            _mpb.SetColor("_EmissionColor", c * (isNext ? 2.2f : 0.4f));
            r.SetPropertyBlock(_mpb);
        }
    }
}
