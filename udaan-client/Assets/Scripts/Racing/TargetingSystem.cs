using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Radar + full-AI lock-on. Tracks TargetHealth within radar range; auto-locks the nearest one that's
/// on-screen, and the player can cycle with a button. Once locked, the target is KEPT even if it
/// leaves the FOV (so the HUD can show an off-screen arrow) until it dies or leaves radar range.
/// The HUD moves the crosshair onto the lock; DroneWeapon fires straight down the view and AimDir()
/// bends the shot fully onto the lock so bullets hit the crosshair at any screen position.
/// Reusable by enemy AI (aimed at the player) later.
/// </summary>
public class TargetingSystem : MonoBehaviour
{
    [Header("Radar / Lock-on")]
    [Tooltip("Radar sphere radius — targets inside this are lockable/kept.")]
    public float lockRange = 150f;

    [Header("Assist")]
    [Tooltip("How hard fire bends onto the lock. 1 = shots hit exactly where the crosshair sits; lower = skill/hard mode.")]
    [Range(0f, 1f)] public float assistStrength = 1f;
    [Tooltip("Assist only applies when the target is within this angle of the view — beyond it you just get the off-screen arrow.")]
    public float assistMaxAngle = 55f;
    [Tooltip("Player: true (locks on-screen targets via the camera). AI enemies: false (locks by world position around the drone).")]
    public bool useCameraForView = true;

    public Transform CurrentTarget { get; private set; }

    private Camera _cam;
    private Camera Cam { get { if (_cam == null) _cam = Camera.main; return _cam; } }
    private TargetHealth _ownHealth;
    private int MyTeam { get { if (_ownHealth == null) _ownHealth = GetComponent<TargetHealth>(); return _ownHealth != null ? _ownHealth.team : 0; } }
    private float _nextScan;
    private readonly List<Transform> _onScreen = new List<Transform>(); // in-range AND visible, distance-sorted
    private bool _manual;
    private readonly Dictionary<Transform, float> _sortKeys = new Dictionary<Transform, float>(); // reused each scan
    private System.Comparison<Transform> _cmp;                                                     // single cached delegate

    void Update()
    {
        if (Time.time >= _nextScan)
        {
            _nextScan = Time.time + 0.1f;
            RefreshOnScreen();

            if (!_manual)
            {
                // Prefer the nearest on-screen target; if none visible, keep the current lock (for the arrow).
                Transform nearest = _onScreen.Count > 0 ? _onScreen[0] : null;
                if (nearest != null) CurrentTarget = nearest;
                else if (!IsValid(CurrentTarget)) CurrentTarget = null;
            }
        }

        if (CurrentTarget != null && !IsValid(CurrentTarget)) { CurrentTarget = null; _manual = false; }
    }

    private bool IsValid(Transform t)
    {
        return t != null && t.gameObject.activeInHierarchy
            && Vector3.Distance(transform.position, t.position) <= lockRange;
    }

    private void RefreshOnScreen()
    {
        _onScreen.Clear();
        Camera cam = useCameraForView ? Cam : null;
        Vector3 origin = transform.position;
        Vector3 camPos = cam != null ? cam.transform.position : origin;
        Vector3 camFwd = cam != null ? cam.transform.forward : transform.forward;

        int myTeam = MyTeam;
        var all = TargetHealth.All; // self-maintained registry (no per-scan allocation)
        foreach (var t in all)
        {
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            if (!t.targetable) continue;                      // stealth / spawn protection
            if (t.transform.root == transform.root) continue; // never lock ourselves
            if (t.team == myTeam) continue;                   // only lock other factions
            if (Vector3.Distance(origin, t.transform.position) > lockRange) continue;
            if (cam != null)
            {
                Vector3 v = cam.WorldToViewportPoint(t.transform.position);
                if (v.z <= 0f || v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f) continue; // on-screen only
            }
            _onScreen.Add(t.transform);
        }

        // Priority: (1) targetPriority (enemy class), (2) closest to camera center, (3) distance.
        // Precompute each key ONCE (no GetComponent inside the O(n log n) comparator) and reuse one delegate.
        _sortKeys.Clear();
        for (int i = 0; i < _onScreen.Count; i++)
        {
            Transform t = _onScreen[i];
            var th = t.GetComponent<TargetHealth>();
            int pr = th != null ? th.targetPriority : 0;
            float angle = Vector3.Angle(camFwd, t.position - camPos); // centeredness
            float dist = Vector3.Distance(origin, t.position);
            _sortKeys[t] = -pr * 1_000_000f + angle * 1000f + dist;
        }
        if (_cmp == null) _cmp = (a, b) => _sortKeys[a].CompareTo(_sortKeys[b]);
        _onScreen.Sort(_cmp);
    }

    /// <summary>Cycle among on-screen targets (dir +1/-1). Manual lock holds until it leaves radar range.</summary>
    public void CycleTarget(int dir)
    {
        if (_onScreen.Count == 0) return;
        int idx = CurrentTarget != null ? _onScreen.IndexOf(CurrentTarget) : -1;
        idx = ((idx + dir) % _onScreen.Count + _onScreen.Count) % _onScreen.Count;
        CurrentTarget = _onScreen[idx];
        _manual = true;
    }

    /// <summary>Force the lock onto a specific target (AI retaliation). Holds until released or invalid.</summary>
    public void ForceTarget(Transform t) { if (t != null) { CurrentTarget = t; _manual = true; } }

    /// <summary>Release a forced lock so auto-acquisition resumes.</summary>
    public void ReleaseForcedTarget() { _manual = false; }

    /// <summary>Bend a base fire direction fully onto the locked target (if it's within the assist angle).</summary>
    public Vector3 AimDir(Vector3 baseDir, Vector3 fromPos)
    {
        if (CurrentTarget == null) return baseDir;
        Vector3 toTarget = (CurrentTarget.position - fromPos).normalized;
        if (Vector3.Angle(baseDir, toTarget) > assistMaxAngle) return baseDir; // off-view -> no warp
        return Vector3.Slerp(baseDir, toTarget, Mathf.Clamp01(assistStrength)).normalized;
    }
}
