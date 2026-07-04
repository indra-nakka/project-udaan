using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single point the DroneFlightController reads from. Collects every IFlightInput on this
/// drone (gamepad + touch HUD + anything future) and merges them so multiple devices can drive
/// the same drone. Auto-provisions the default sources if none are wired in the inspector, so the
/// controls "just work" when the scripts are dropped into a scene with zero manual setup.
/// </summary>
[DefaultExecutionOrder(-50)] // ensure sources exist before the controller's FixedUpdate reads
public class FlightInputRouter : MonoBehaviour, IFlightInput
{
    public enum AimMode { NoseAim, FreeAim } // FreeAim: right stick aims a reticle off the nose

    [Tooltip("If on, adds a GamepadFlightInput + TouchFlightHUD automatically when none are found.")]
    public bool autoProvisionSources = true;

    /// <summary>Current aim mode. Sources read this to decide what the right stick does.</summary>
    public AimMode Aim { get; private set; } = AimMode.FreeAim;

    /// <summary>Cycle aim mode (called by the on-screen AIM button / gamepad / key).</summary>
    public void CycleAim() => Aim = Aim == AimMode.NoseAim ? AimMode.FreeAim : AimMode.NoseAim;

    private readonly List<IFlightInput> _sources = new List<IFlightInput>();

    void Awake()
    {
        // Grab any input sources already sitting on this drone (excluding this router itself).
        foreach (var comp in GetComponents<MonoBehaviour>())
        {
            if (comp is IFlightInput src && !(comp is FlightInputRouter))
                _sources.Add(src);
        }

        if (_sources.Count == 0 && autoProvisionSources)
        {
            _sources.Add(gameObject.AddComponent<GamepadFlightInput>());
            _sources.Add(gameObject.AddComponent<TouchFlightHUD>());
        }
    }

    /// <summary>Register a source created at runtime (e.g. a networked remote-input feed).</summary>
    public void Register(IFlightInput source)
    {
        if (source != null && !_sources.Contains(source)) _sources.Add(source);
    }

    /// <summary>The most recent merged input, cached so the HUD can mirror it for display.</summary>
    public FlightInputState Last { get; private set; }

    public FlightInputState Read()
    {
        var merged = FlightInputState.None;
        for (int i = 0; i < _sources.Count; i++)
            merged = FlightInputState.Merge(merged, _sources[i].Read());
        Last = merged;
        return merged;
    }
}
