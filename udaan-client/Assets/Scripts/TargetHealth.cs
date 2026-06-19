using UnityEngine;
using Unity.Netcode;

public class TargetHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Drop Settings")]
    public GameObject scrapPrefab;
    public int dropCount = 3;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // This function will be called by the Nerf Dart when it hits
    public void TakeDamage(float amount)
    {
        if (!IsServer) return;
        currentHealth -= amount;
        
        Debug.Log("Hit! Dummy health is now: " + currentHealth);

        // Make the dummy pop a little bit when hit (visual feedback)
        transform.localScale = transform.localScale * 0.9f; 

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

        // We will replace this with a cool confetti particle effect later
        Destroy(gameObject); 
    }
}