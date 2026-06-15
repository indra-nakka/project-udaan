using UnityEngine;

public class DartCollision : MonoBehaviour
{
    [Header("Dart Stats")]
    public float damage = 20f;
    
    // This stops the dart from doing damage twice if it bounces rapidly
    private bool hasDealtDamage = false; 

    void OnCollisionEnter(Collision collision)
    {
        if (hasDealtDamage) return;

        // Check if the object we just hit has the TargetHealth script attached
        TargetHealth target = collision.gameObject.GetComponent<TargetHealth>();
        
        if (target != null)
        {
            // Tell the target to take damage!
            target.TakeDamage(damage);
            hasDealtDamage = true;
            
            // Destroy the dart immediately upon a successful hit
            // (If we missed, it will just bounce and be destroyed after 3 seconds by our weapon script)
            Destroy(gameObject); 
        }
    }
}