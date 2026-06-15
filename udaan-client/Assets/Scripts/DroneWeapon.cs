using UnityEngine;
using Unity.Netcode;

public class DroneWeapon : NetworkBehaviour
{
    [Header("Weapon Settings")]
    public GameObject dartPrefab;
    public Transform muzzlePoint;
    public float shootForce = 80f;
    public float fireRate = 0.15f; // Time between shots

    private float nextFireTime = 0f;

    void Update()
    {
        // If this is not YOUR drone, do not let it shoot!
        if (!IsOwner) return;

        // Fire with Left Mouse Click (Mouse0) OR Right Bumper on Xbox (JoystickButton5)
        if ((Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.JoystickButton5)) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        // Add a 90-degree tilt on the X-axis to fix the upright cylinder issue
        Quaternion dartRotation = muzzlePoint.rotation * Quaternion.Euler(90f, 0f, 0f);
        
        // Spawn it with the new corrected rotation
        GameObject newDart = Instantiate(dartPrefab, muzzlePoint.position, dartRotation);

        // 2. Grab its physical body and launch it forward
        Rigidbody dartRb = newDart.GetComponent<Rigidbody>();
        
        // We add the drone's current velocity so flying forward makes the dart fly faster!
        Rigidbody droneRb = GetComponent<Rigidbody>();
        Vector3 inheritedVelocity = droneRb != null ? droneRb.linearVelocity : Vector3.zero;

        dartRb.linearVelocity = inheritedVelocity + (muzzlePoint.forward * shootForce);

        // 3. Destroy the dart after 3 seconds so they don't crash the game's memory
        Destroy(newDart, 3f);
    }
}