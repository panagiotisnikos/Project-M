using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private PlayerPerformanceTracker performanceTracker;

    private int currentHealth;
    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        Debug.Log($"[PlayerHealth] Player Health: {currentHealth}/{maxHealth}");
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"[PlayerHealth] Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

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
}