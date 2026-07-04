using UnityEngine;
using Unity.Netcode;

public class TargetHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    public DroneClassData defaultClassData;
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Targeting")]
    [Tooltip("Higher = locked first. Plan: destroyable projectile > enemy drone > minion > turret ...")]
    public int targetPriority = 0;

    [Header("Drop Settings")]
    public GameObject scrapPrefab;
    public int dropCount = 3;

    private Vector3 _initialScale;

    void Start()
    {
        _initialScale = transform.localScale;
        InitializeClassData(defaultClassData);
        if (currentHealth <= 0f) currentHealth = maxHealth; // offline / no class data: ensure full HP
    }

    // Damage may be applied offline (single-player toy) or server-side in a network session.
    private bool HasDamageAuthority => !IsSpawned || IsServer;

    public void InitializeClassData(DroneClassData classData)
    {
        defaultClassData = classData;
        if (defaultClassData != null)
        {
            maxHealth = defaultClassData.maxHealth;
            currentHealth = maxHealth;
            Debug.Log($"Target Health dynamically loaded class max HP: {maxHealth}");
        }
    }

    // This function will be called by the Nerf Dart when it hits
    public void TakeDamage(float amount)
    {
        if (!HasDamageAuthority) return;
        currentHealth -= amount;

        Debug.Log("Hit! Dummy health is now: " + currentHealth);

        // Shrink with health: full HP = 100% size, 0 HP = 50% size, then pop.
        float frac = Mathf.Clamp01(currentHealth / maxHealth);
        transform.localScale = _initialScale * Mathf.Lerp(0.5f, 1f, frac);

        if (currentHealth <= 0)
        {
            Pop();
        }
    }

    void Pop()
    {
        Debug.Log("Target Destroyed!");
        
        // --- ECON-02: The Piñata Drop ---
        // Ensure only the server handles spawning to prevent double-drops
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (scrapPrefab != null)
            {
                for (int i = 0; i < dropCount; i++)
                {
                    // Add slight random offset so they don't spawn inside each other
                    Vector3 spawnPos = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f));
                    GameObject newScrap = Instantiate(scrapPrefab, spawnPos, Random.rotation);

                    // Add explosive scatter force
                    Rigidbody rb = newScrap.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 scatterDir = (Vector3.up + Random.insideUnitSphere).normalized;
                        float force = Random.Range(4f, 8f);
                        rb.AddForce(scatterDir * force, ForceMode.Impulse);
                    }

                    // Crucial: Sync across the network
                    NetworkObject netObj = newScrap.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn();
                    }
                }
            }
        }

        // Respawn the dummy: refill, reset size, and relocate (server in a session, or locally when offline).
        if (IsServer || !IsSpawned)
        {
            currentHealth = maxHealth;
            transform.localScale = _initialScale;
            float randomX = UnityEngine.Random.Range(-20f, 20f);
            float randomZ = UnityEngine.Random.Range(-20f, 20f);
            transform.position = new Vector3(randomX, transform.position.y, randomZ);
            Debug.Log($"Target popped! Relocating to {transform.position}");
        }
    }
}