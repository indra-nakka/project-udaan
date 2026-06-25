using UnityEngine;
using Unity.Netcode;
using System;

public class DroneFlightController : NetworkBehaviour
{
    private Rigidbody rb;
    private PlayerEconomy playerEconomy;
    private float activeSpeedModifier = 1.0f; // Default state: 100% speed

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

    void FixedUpdate()
    {
        if (!IsOwner) return;

        HandleFlightMovement();
        HandleHoverPhysics();
    }

    private void HandleFlightMovement()
    {
        // 1. Forward Thrust Processing (Right Trigger)
        float thrustInput = 0f;
        try { thrustInput = Input.GetAxis("Xbox_RT"); } catch { }

        // 2. Altitude (Ascend/Descend from Left Stick Y)
        float altitudeInput = 0f;
        try { altitudeInput = Input.GetAxis("Vertical"); } catch { }

        // 3. Strafe/Lateral Translation (Left Stick X)
        float strafeInput = 0f;
        try { strafeInput = Input.GetAxis("Horizontal"); } catch { }

        // 4. Aim Yaw (Turn Left/Right from Right Stick X)
        float yawInput = 0f;
        try { yawInput = Input.GetAxis("TargetYaw"); } catch { }

        // 5. Aim Pitch (Nose Up/Down from Right Stick Y)
        float pitchInput = 0f;
        try { pitchInput = Input.GetAxis("TargetPitch"); } catch { }

        // Check if we are explicitly providing manual override inputs (thrust or altitude)
        isManuallyOverridingHover = Mathf.Abs(thrustInput) > 0.1f || Mathf.Abs(altitudeInput) > 0.1f;

        // Execute Forward Thrust
        if (Mathf.Abs(thrustInput) > 0.01f)
        {
            rb.AddForce(transform.forward * thrustInput * forwardThrust * activeSpeedModifier, ForceMode.Acceleration);
        }

        // Execute Altitude Thrust
        if (Mathf.Abs(altitudeInput) > 0.01f)
        {
            rb.AddForce(Vector3.up * altitudeInput * flyUpForce, ForceMode.Acceleration);
        }

        // Execute Strafe Drift
        if (Mathf.Abs(strafeInput) > 0.01f)
        {
            rb.AddForce(transform.right * strafeInput * (forwardThrust * 0.5f) * activeSpeedModifier, ForceMode.Acceleration);
        }

        // Execute Yaw Rotation
        if (Mathf.Abs(yawInput) > 0.01f)
        {
            transform.Rotate(Vector3.up * yawInput * turnSpeed * Time.fixedDeltaTime, Space.World);
        }

        // Execute Pitch Rotation
        if (Mathf.Abs(pitchInput) > 0.01f)
        {
            // Invert pitch so pulling back looks up (standard flight mechanics)
            transform.Rotate(Vector3.right * -pitchInput * pitchSpeed * Time.fixedDeltaTime, Space.Self);
        }

        // Cosmetic Roll (Bank on turn)
        float targetBank = -yawInput * tiltAmount;
        Vector3 currentEuler = transform.eulerAngles;
        // Keep the current pitch (X) and yaw (Y), but smoothly interpolate the bank (Z)
        float newZ = Mathf.LerpAngle(currentEuler.z, targetBank, Time.fixedDeltaTime * 5f);
        transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, newZ);
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