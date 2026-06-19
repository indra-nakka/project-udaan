using UnityEngine;
using Unity.Netcode;

public class ScrapItem : NetworkBehaviour
{
    private Rigidbody rb;
    private bool isFloating = false;
    
    [Header("Hover Settings")]
    public float floatHeight = 1.5f;   // How high above its landing spot it stays
    public float bounceSpeed = 3f;     // How fast it bobs up and down
    public float bounceAmplitude = 0.2f; // How dramatic the bobbing is
    public float rotationSpeed = 45f;  // How fast it spins to grab attention

    private Vector3 targetFloatPos;
    private float startY;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Once the server registers that the scrap has bounced off the ground mesh
        if (!isFloating)
        {
            // Transition to hovering state
            isFloating = true;
            rb.useGravity = false;
            rb.isKinematic = true; // Stop active physics calculations
            
            // Set the resting float position 1.5m above the ground it hit
            startY = transform.position.y + floatHeight;
        }
    }

    void Update()
    {
        // Keep it spinning on both client and host windows
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        if (isFloating)
        {
            // Calculate a smooth up-and-down bobbing motion using a sine wave
            float newY = startY + (Mathf.Sin(Time.time * bounceSpeed) * bounceAmplitude);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
}