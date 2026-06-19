using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class DroneFlightController : NetworkBehaviour
{
    [Header("Flight Settings")]
    public float forwardThrust = 20f;
    public float pitchSpeed = 5f;
    public float yawSpeed = 8f;

    [Header("Hover Mechanics (Arcade Feel)")]
    public float hoverHeight = 2f; 
    public float hoverForce = 65f;
    public float hoverDampening = 15f; 

    private Rigidbody rb;
    private PlayerEconomy playerEconomy;
    private float activeSpeedModifier = 1.0f; // Default state: 100% speed
    private float pitchInput;
    private float yawInput;
    private float thrustInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerEconomy = GetComponent<PlayerEconomy>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // This runs the exact millisecond the drone connects to the server
        if (IsOwner)
        {
            // Teleport to a random X and Z coordinate so we don't stack like pancakes
            float randomX = Random.Range(-8f, 8f);
            float randomZ = Random.Range(-8f, 8f);
            
            // Drop them from 3 meters in the air so they gracefully hover down
            transform.position = new Vector3(randomX, 3f, randomZ);

            // Subscribe to our wallet's speed upgrade hook safely on spawn
            if (playerEconomy != null)
            {
                playerEconomy.OnSpeedModifierUpgraded += HandleSpeedUpgraded;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        // Always unsubscribe on network teardown to prevent memory leaks!
        if (playerEconomy != null)
        {
            playerEconomy.OnSpeedModifierUpgraded -= HandleSpeedUpgraded;
        }
        base.OnNetworkDespawn();
    }

    private void HandleSpeedUpgraded(float newMultiplier)
    {
        // Scale our active modifier (e.g. 1.0f -> 1.15f)
        activeSpeedModifier *= newMultiplier; 
        Debug.Log($"Flight Controller received upgrade! New speed modifier: {activeSpeedModifier}");
    }

    void Update()
    {
        // If this is not OUR drone, do not read our controller inputs!
        if (!IsOwner) return;

        // 1. GATHER INPUTS
        
        // Left Analog L/R (Yaw) - Works for Keyboard A/D and Xbox Left Stick X
        yawInput = Input.GetAxis("Horizontal"); 
        
        // Left Analog U/D (Pitch) - Works for Keyboard W/S and Xbox Left Stick Y
        pitchInput = Input.GetAxis("Vertical");   

        // Thrust Inputs
        thrustInput = 0f;

        // Forward Thrust: Spacebar OR Xbox 'A' Button
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.JoystickButton0)) 
        {
            thrustInput = 1f;
        }
            
        // Reverse/Brake: Left Shift OR Xbox 'B' Button
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.JoystickButton1)) 
        {
            thrustInput = -1f;
        }
    }

    void FixedUpdate()
    {
        // If this is not OUR drone, do not run the physics!
        if (!IsOwner) return;

        // Physics calculations must happen in FixedUpdate
        ApplyHover();
        ApplyMovement();
    }

    void ApplyHover()
    {
        // Shoot an invisible laser down to find the ground
        Ray ray = new Ray(transform.position, -Vector3.up);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, hoverHeight * 2f))
        {
            // Calculate how far we are from our target hover height
            float proportionalHeight = (hoverHeight - hit.distance) / hoverHeight;
            
            // Apply upward force like a spring. The closer to the ground, the harder it pushes up.
            Vector3 appliedHoverForce = Vector3.up * proportionalHeight * hoverForce;
            
            // Apply dampening so it doesn't bounce endlessly like a pogo stick
            rb.AddForce(appliedHoverForce - (rb.linearVelocity * hoverDampening), ForceMode.Acceleration);
        }
        else
        {
            // If we fly off a ledge and the laser misses the ground, apply a gentle fake gravity
            rb.AddForce(-Vector3.up * (hoverForce / 2f), ForceMode.Acceleration);
        }
    }

    void ApplyMovement()
    {
        // YAW (Turning Left/Right)
        rb.AddRelativeTorque(Vector3.up * yawInput * yawSpeed, ForceMode.Acceleration);

        // PITCH (Tilting the nose Up/Down)
        rb.AddRelativeTorque(Vector3.right * pitchInput * pitchSpeed, ForceMode.Acceleration);

        // THRUST (Moving Forward/Backwards relative to where the nose is pointing)
        if (Mathf.Abs(thrustInput) > 0.1f)
        {
            rb.AddRelativeForce(Vector3.forward * thrustInput * forwardThrust * activeSpeedModifier, ForceMode.Acceleration);
        }
    }
}