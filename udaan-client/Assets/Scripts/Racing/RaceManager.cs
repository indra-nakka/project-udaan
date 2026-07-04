using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs the hoop circuit: a 3-2-1-GO countdown (drone frozen), a dedicated start/finish line, the
/// ordered gates, lap counting and current/best/total timing, plus restart.
///
/// Lap flow:  cross START (gold) to start the clock  ->  hit every gate 0..N-1 in order (green)
///            ->  cross the START/FINISH line again to complete the lap. Repeat for totalLaps.
///
/// While <see cref="FlightLocked"/> is true, DroneFlightController ignores input (used during the
/// countdown). Pushes a live readout / countdown into the drone's TouchFlightHUD if present.
/// </summary>
public class RaceManager : MonoBehaviour
{
    [Header("Race Config")]
    public int totalLaps = 3;
    public float countdownSeconds = 3f;

    /// <summary>Global flight lock honored by DroneFlightController (set during the countdown).</summary>
    public static bool FlightLocked = false;

    private readonly List<RaceGate> _gates = new List<RaceGate>();
    private RaceGate _startGate;

    private int _nextGate = 0;
    private int _lap = 0;
    private bool _started = false;       // clock running (first start-line cross happened)
    private bool _finished = false;
    private bool _running = false;       // countdown done, race live
    private bool _awaitingStartCross = false; // all gates hit; next start-line cross closes the lap

    private float _raceTime = 0f;
    private float _lapStartTime = 0f;
    private float _bestLap = Mathf.Infinity;

    private Transform _drone;
    private Rigidbody _rb;
    private Pose _startPose;

    private TouchFlightHUD _hud;
    private bool _hudSearched;
    private string _countdownText;

    void Awake() => FlightLocked = false;

    // ---- registration (called by RaceTrackGenerator) ----
    public void RegisterStartGate(RaceGate gate)
    {
        _startGate = gate;
        gate.manager = this;
        gate.isStartFinish = true;
        gate.gateIndex = -1;
    }

    public void RegisterGate(RaceGate gate)
    {
        gate.gateIndex = _gates.Count;
        gate.manager = this;
        _gates.Add(gate);
    }

    /// <summary>Called by the bootstrap once the drone exists. Positions it and starts the countdown.</summary>
    public void SetupRace(GameObject drone, Pose startPose)
    {
        _drone = drone.transform;
        _rb = drone.GetComponent<Rigidbody>();
        _startPose = startPose;
        RestartRace();
    }

    public void RestartRace()
    {
        StopAllCoroutines();
        _nextGate = 0; _lap = 0;
        _started = false; _finished = false; _running = false; _awaitingStartCross = false;
        _raceTime = 0f; _lapStartTime = 0f; _bestLap = Mathf.Infinity;

        PlaceDroneAtStart();
        RefreshHighlights(startIsTarget: true);
        StartCoroutine(CountdownThenGo());
    }

    private void PlaceDroneAtStart()
    {
        if (_drone == null) return;
        _drone.SetPositionAndRotation(_startPose.position, _startPose.rotation);
        // Only zero velocity on a non-kinematic body (avoids Unity warnings when frozen mid-countdown).
        if (_rb != null && !_rb.isKinematic) { _rb.linearVelocity = Vector3.zero; _rb.angularVelocity = Vector3.zero; }
    }

    private IEnumerator CountdownThenGo()
    {
        FlightLocked = true;
        if (_rb != null) _rb.isKinematic = true; // freeze in place, no fall, during countdown

        for (float t = Mathf.Ceil(countdownSeconds); t > 0; t -= 1f)
        {
            _countdownText = t.ToString("0");
            yield return new WaitForSeconds(1f);
        }

        _countdownText = "GO!";
        if (_rb != null) _rb.isKinematic = false;
        FlightLocked = false;
        _running = true;

        yield return new WaitForSeconds(0.8f);
        _countdownText = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) { RestartRace(); return; }
        if (_running && _started && !_finished) _raceTime += Time.deltaTime;
        PushHud();
    }

    public void NotifyGatePassed(RaceGate gate)
    {
        if (!_running || _finished) return;

        if (gate.isStartFinish) { HandleStartCross(); return; }

        if (gate.gateIndex != _nextGate) return; // must hit gates in order
        _gates[_nextGate].SetHighlighted(false);
        _nextGate++;

        if (_nextGate >= _gates.Count)
        {
            // All gates cleared — the start/finish line is now the target to close the lap.
            _awaitingStartCross = true;
            if (_startGate != null) _startGate.SetHighlighted(true);
        }
        else
        {
            _gates[_nextGate].SetHighlighted(true);
        }
    }

    private void HandleStartCross()
    {
        // First crossing arms the race and starts the clock.
        if (!_started)
        {
            _started = true;
            _raceTime = 0f; _lapStartTime = 0f;
            RefreshHighlights(startIsTarget: false);
            _nextGate = 0;
            if (_gates.Count > 0) _gates[0].SetHighlighted(true);
            return;
        }

        // Otherwise it only counts once every gate this lap has been cleared.
        if (!_awaitingStartCross) return;
        _awaitingStartCross = false;

        float lapTime = _raceTime - _lapStartTime;
        _lapStartTime = _raceTime;
        if (lapTime < _bestLap) _bestLap = lapTime;
        _lap++;

        if (_startGate != null) _startGate.SetHighlighted(false);

        if (_lap >= totalLaps)
        {
            _finished = true;
            Debug.Log($"[RACE] Finished {totalLaps} laps in {Fmt(_raceTime)} | best lap {Fmt(_bestLap)}");
            return;
        }

        _nextGate = 0;
        if (_gates.Count > 0) _gates[0].SetHighlighted(true);
    }

    private void RefreshHighlights(bool startIsTarget)
    {
        foreach (var g in _gates) if (g != null) g.SetHighlighted(false);
        if (_startGate != null) _startGate.SetHighlighted(startIsTarget);
    }

    private void PushHud()
    {
        if (!_hudSearched) { _hud = FindFirstObjectByType<TouchFlightHUD>(); _hudSearched = true; }
        if (_hud == null) return;

        string status;
        if (!string.IsNullOrEmpty(_countdownText))
            status = _countdownText;
        else if (_finished)
            status = $"FINISH!  Total {Fmt(_raceTime)}   Best lap {Fmt(_bestLap)}   (R = restart)";
        else if (!_started)
            status = $"Cross the GOLD start line to begin  ({totalLaps} laps)";
        else
        {
            string best = float.IsInfinity(_bestLap) ? "--:--" : Fmt(_bestLap);
            string target = _awaitingStartCross ? "-> FINISH line" : $"Gate {_nextGate + 1}/{_gates.Count}";
            status = $"Lap {Mathf.Min(_lap + 1, totalLaps)}/{totalLaps}   {target}   {Fmt(_raceTime)}   Best {best}";
        }
        _hud.SetInfoText(status);
    }

    private static string Fmt(float t)
    {
        if (float.IsInfinity(t)) return "--:--";
        int m = (int)(t / 60f);
        float s = t - m * 60f;
        return $"{m:00}:{s:00.00}";
    }
}
