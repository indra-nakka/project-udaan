using UnityEngine;

public class TargetHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // This function will be called by the Nerf Dart when it hits
    public void TakeDamage(float amount)
    {
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
        // We will replace this with a cool confetti particle effect later
        Destroy(gameObject); 
    }
}