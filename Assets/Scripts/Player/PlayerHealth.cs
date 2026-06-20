using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private PlayerPerformanceTracker performanceTracker;

    private int currentHealth;
    private bool isDead;
    [SerializeField] private float knockbackForce = 4f;

    private Rigidbody rb;

    public bool IsDead => isDead;

   private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage, Vector3 hitDirection)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        ApplyKnockback(hitDirection);

        //Debug.Log($"[PlayerHealth] Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (performanceTracker != null)
            performanceTracker.RegisterDamageTaken(damage);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("[PlayerHealth] Player died.");

        if (performanceTracker != null)
            performanceTracker.StopTracking();
    }
    private void ApplyKnockback(Vector3 hitDirection)
    {
        if (rb == null)
            return;

        hitDirection.y = 0f;
        hitDirection.Normalize();

        rb.AddForce(hitDirection * knockbackForce, ForceMode.Impulse);
    }
}