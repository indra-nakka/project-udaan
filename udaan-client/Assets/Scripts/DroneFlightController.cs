using UnityEngine;
using Unity.Netcode;
using System;

public class DroneFlightController : NetworkBehaviour
{
    private Rigidbody rb;
    private PlayerEconomy playerEconomy;
    private FlightInputRouter input;           // device-agnostic input (gamepad + touch), see Scripts/Input
    private float activeSpeedModifier = 1.0f;   // Default state: 100% speed

    [Header("Class Data Configurations")]
    public DroneClassData defaultClassData; // Declared at class level so it displays in Unity!

    [Header("Base Flight Values")]
    public float forwardThrust = 15f;
    public float tiltAmount = 25f;
    public float flyUpForce = 15f;
    private float initialFlyUpForce;
    public float hoverAltitude = 1.5f;
    public float hoverForce = 12f;
    public float hoverDamp = 6f;

    [Header("Steering Sensitivity")]
    public float turnSpeed = 100f;
    public float pitchSpeed = 100f;

    [Header("Speed Tuning (M1)")]
    [Tooltip("Multiplies thrust/strafe forces on top of class data — raises top speed. ~3x per M1 feedback.")]
    public float thrustMultiplier = 3f;
    [Tooltip("Reverse force as a fraction of forward (slider below center). Player fine-tunes via the slider.")]
    [Range(0f, 1f)] public float reverseScale = 0.6f;
    [Tooltip("Scales turn/pitch rate so gates stay makeable at the higher speed.")]
    public float steeringMultiplier = 1.5f;
    [Tooltip("Vertical aim direction. Default = push up climbs. Untick if it feels backwards for you.")]
    public bool invertPitch = true;

    [Header("Juke / Dodge")]
    [Tooltip("Instant velocity burst (m/s) in the left-stick direction when dash is pressed.")]
    public float dashImpulse = 10f;
    public float dashCooldown = 0.6f;
    private float _nextDash;

    [Tooltip("AI drones set this so the player's race countdown doesn't freeze them (they'd otherwise fall).")]
    public bool ignoreCountdownLock = false;

    [Header("Collision")]
    [Tooltip("Softly push off walls instead of hard-stalling on them.")]
    public bool collisionPushOff = true;
    public float pushOffForce = 22f;

    [Header("Assists & Brake (touch feel)")]
    [Tooltip("When no pitch input, spring the nose back to level. OFF by default — it fights holding an attitude to aim. Enable only for a beginner/easy mode.")]
    public bool autoLevelAssist = false;
    public float autoLevelSpeed = 3f;
    [Tooltip("Cosmetic bank-into-turn strength (0 = none). Auto-fades near vertical to avoid gimbal spin.")]
    public float bankStrength = 1f;
    [Tooltip("How hard the air-brake bleeds off velocity.")]
    public float brakeStrength = 5f;

    [Header("Laser Mounts")]
    public Transform leftLaserMount;
    public Transform rightLaserMount;

    // Track input state for hover suppression
    private bool isManuallyOverridingHover = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerEconomy = GetComponent<PlayerEconomy>();
        initialFlyUpForce = flyUpForce;

        // Ensure an input stack exists even with zero manual wiring (auto-provisions gamepad + touch).
        input = GetComponent<FlightInputRouter>();
        if (input == null) input = gameObject.AddComponent<FlightInputRouter>();

        // Defensive Guard: Only load values if the ScriptableObject has been dragged into the slot
        if (defaultClassData != null)
        {
            forwardThrust = defaultClassData.baseThrustForce;
            rb.linearDamping = defaultClassData.baseDragValue;
            Debug.Log($"Class [{defaultClassData.className}] profiles successfully initialized for local entity physical assets.");
        }
        else
        {
            Debug.LogWarning("DroneFlightController Alert: No Default Class Data asset assigned! Falling back to base hardcoded inspector presets.");
        }
    }

    public void InitializeClassData(DroneClassData classData)
    {
        defaultClassData = classData;
        if (defaultClassData != null)
        {
            forwardThrust = defaultClassData.baseThrustForce;
            if (rb == null) rb = GetComponent<Rigidbody>();
            rb.linearDamping = defaultClassData.baseDragValue;
            Debug.Log($"[DRONE-PLAYER-{(IsServer ? "HOST" : "CLIENT")}] NetID: {NetworkObjectId} - Successfully applied Class: {classData.className}");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;

        // Subscribing cleanly to our wallet's upgrade modifications hook safely on spawn
        if (playerEconomy != null)
        {
            playerEconomy.OnSpeedModifierUpgraded += HandleSpeedUpgraded;
        }

        SpawnSafetyReroute();
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribing cleanly to prevent performance memory fragmentation leaks
        if (playerEconomy != null)
        {
            playerEconomy.OnSpeedModifierUpgraded -= HandleSpeedUpgraded;
        }
        base.OnNetworkDespawn();
    }

    private void HandleSpeedUpgraded(float newMultiplier)
    {
        activeSpeedModifier *= newMultiplier;
        Debug.Log($"Flight Controller received upgrade! New speed modifier calculation parameter is: {activeSpeedModifier}");
    }

    private void SpawnSafetyReroute()
    {
        float randomX = UnityEngine.Random.Range(-8f, 8f);
        float randomZ = UnityEngine.Random.Range(-8f, 8f);
        transform.position = new Vector3(randomX, 3f, randomZ);
    }

    /// <summary>
    /// True when this instance is allowed to drive the drone. In a network session only the owner
    /// flies (invariants: ownership-gated local input). When NOT spawned in a session — i.e. the
    /// offline single-player flight toy — control is always granted so the drone flies without a host.
    /// </summary>
    private bool HasControl()
    {
        return !IsSpawned || IsOwner;
    }

    void FixedUpdate()
    {
        if (!HasControl()) return;
        if (RaceManager.FlightLocked && !ignoreCountdownLock) return; // frozen during the 3-2-1-GO countdown

        HandleFlightMovement();
        HandleHoverPhysics();
        ClampToArena();
    }

    private void HandleFlightMovement()
    {
        // Single device-agnostic read: gamepad + on-screen touch merged into one 5-axis state.
        FlightInputState f = input.Read();

        float thrustInput   = f.thrust;    // 0..1 throttle (forward only)
        float altitudeInput = f.altitude;  // -1..1
        float strafeInput   = f.strafe;    // -1..1
        float yawInput      = f.yaw;       // -1..1
        float pitchInput    = f.pitch;     // -1..1

        // Check if we are explicitly providing manual override inputs (thrust or altitude)
        isManuallyOverridingHover = Mathf.Abs(thrustInput) > 0.1f || Mathf.Abs(altitudeInput) > 0.1f;

        // Execute Thrust (forward when >0, reverse when <0; reverse is scaled down).
        if (Mathf.Abs(thrustInput) > 0.01f)
        {
            float dirScale = thrustInput >= 0f ? 1f : reverseScale;
            rb.AddForce(transform.forward * thrustInput * forwardThrust * activeSpeedModifier * thrustMultiplier * dirScale, ForceMode.Acceleration);
        }

        // Execute Altitude Thrust
        if (Mathf.Abs(altitudeInput) > 0.01f)
        {
            rb.AddForce(Vector3.up * altitudeInput * flyUpForce, ForceMode.Acceleration);
        }

        // Execute Strafe Drift
        if (Mathf.Abs(strafeInput) > 0.01f)
        {
            rb.AddForce(transform.right * strafeInput * (forwardThrust * 0.5f) * activeSpeedModifier * thrustMultiplier, ForceMode.Acceleration);
        }

        // Execute Yaw Rotation
        if (Mathf.Abs(yawInput) > 0.01f)
        {
            transform.Rotate(Vector3.up * yawInput * turnSpeed * steeringMultiplier * Time.fixedDeltaTime, Space.World);
        }

        // Execute Pitch Rotation (invertPitch flips vertical aim to taste)
        if (Mathf.Abs(pitchInput) > 0.01f)
        {
            float pitchDir = invertPitch ? 1f : -1f;
            transform.Rotate(Vector3.right * pitchDir * pitchInput * pitchSpeed * steeringMultiplier * Time.fixedDeltaTime, Space.Self);
        }

        // Air-brake: bleed horizontal velocity for tight gate turns.
        if (f.brake)
        {
            Vector3 horizVel = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f, rb.linearVelocity.z);
            rb.AddForce(-horizVel * brakeStrength, ForceMode.Acceleration);
        }

        // Juke/dodge: instant burst in the left-stick direction (local right/up), else a forward hop.
        if (f.dash && Time.time >= _nextDash)
        {
            _nextDash = Time.time + dashCooldown;
            Vector3 d = transform.right * yawInput + transform.up * pitchInput;
            if (d.sqrMagnitude < 0.04f) d = transform.forward;
            rb.AddForce(d.normalized * dashImpulse, ForceMode.VelocityChange);
        }

        ApplyOrientationAssists(yawInput, pitchInput);
    }

    /// <summary>
    /// Cosmetic bank on turn + optional auto-level of pitch when the pilot isn't actively aiming.
    /// Auto-level makes the touch schemes far more forgiving (no slow drift into a nose-dive).
    /// </summary>
    private void ApplyOrientationAssists(float yawInput, float pitchInput)
    {
        // Near-vertical (nose straight up/down) the euler rebuild below gimbal-locks and snaps the drone
        // upright/spins it. Fade the whole assist out as we point steeply up or down.
        float vertical = Mathf.Abs(Vector3.Dot(transform.forward, Vector3.up)); // 0 = level, 1 = vertical
        float authority = 1f - Mathf.SmoothStep(0.75f, 0.97f, vertical);
        if (authority <= 0.001f) return;

        Vector3 e = transform.eulerAngles;

        // Bank (Z) proportional to turn.
        float targetBank = -yawInput * tiltAmount * bankStrength;
        float newZ = Mathf.LerpAngle(e.z, targetBank, Time.fixedDeltaTime * 5f * authority);

        // Pitch (X) auto-level toward 0 only when no active pitch input.
        float newX = e.x;
        if (autoLevelAssist && Mathf.Abs(pitchInput) < 0.05f)
            newX = Mathf.LerpAngle(e.x, 0f, Time.fixedDeltaTime * autoLevelSpeed * authority);

        transform.rotation = Quaternion.Euler(newX, e.y, newZ);
    }

    // Smoothly slide off walls: kill the velocity going into the surface and nudge back out.
    // Ignores near-horizontal surfaces (floors/platforms) so the drone can still rest/hover on them.
    void OnCollisionStay(Collision c)
    {
        if (!collisionPushOff || !HasControl()) return;
        if (RaceManager.FlightLocked && !ignoreCountdownLock) return;

        Vector3 n = c.GetContact(0).normal;
        Vector3 nH = new Vector3(n.x, 0f, n.z);
        if (nH.sqrMagnitude < 0.04f) return; // floor/ceiling — let hover physics handle it
        nH.Normalize();

        float into = Vector3.Dot(rb.linearVelocity, nH);
        if (into < 0f) rb.linearVelocity -= nH * into;   // cancel velocity pushing into the wall
        rb.AddForce(nH * pushOffForce, ForceMode.Acceleration);
    }

    private void ClampToArena()
    {
        if (!ArenaBounds.Enabled) return;
        Vector3 off = transform.position - ArenaBounds.Center;
        float d = off.magnitude;
        if (d > ArenaBounds.Radius)
        {
            Vector3 n = off / d;
            transform.position = ArenaBounds.Center + n * ArenaBounds.Radius;
            float outward = Vector3.Dot(rb.linearVelocity, n);
            if (outward > 0f) rb.linearVelocity -= n * outward; // hard wall: kill outward velocity
        }
    }

    private void HandleHoverPhysics()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, hoverAltitude + 2f))
        {
            float distance = hit.distance;
            if (distance <= hoverAltitude + 0.5f)
            {
                // Smoothly dampen the hover influence if manual inputs are active
                float activeHoverForce = isManuallyOverridingHover ? hoverForce * 0.1f : hoverForce;
                float activeHoverDamp = isManuallyOverridingHover ? hoverDamp * 0.1f : hoverDamp;

                float error = hoverAltitude - distance;
                float upwardVelocity = rb.linearVelocity.y;
                float lift = (error * activeHoverForce) - (upwardVelocity * activeHoverDamp);

                rb.AddForce(Vector3.up * lift, ForceMode.Acceleration);
            }
        }
    }
}
