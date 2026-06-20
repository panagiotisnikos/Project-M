using UnityEngine;
using System.Collections;
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private EnemyAI.EnemyRole role;
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    [SerializeField] private float hitFlashDuration = 0.2f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float knockbackForce = 4f;
    private Rigidbody rb;

    private Renderer enemyRenderer;
    private Color originalColor;
    private int currentHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyRenderer = GetComponentInChildren<Renderer>();

        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
        
        if (performanceTracker == null)
        {
            performanceTracker = FindFirstObjectByType<PlayerPerformanceTracker>();
        }
        ConfigureHealth();
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage, Vector3 hitDirection)
    {
        currentHealth -= damage;
        if (enemyRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(HitFlash());
            ApplyKnockback(hitDirection);
        }
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} took {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void ConfigureHealth()
    {
        switch (role)
        {
            case EnemyAI.EnemyRole.Stalker:
                maxHealth = 20;
                break;

            case EnemyAI.EnemyRole.Brute:
                maxHealth = 60;
                break;
        }
    }
    private void Die()
    {
        Debug.Log($"{gameObject.name} died.");

        if (performanceTracker != null)
        {
            performanceTracker.RegisterEnemyKilled();
        }

        Destroy(gameObject);
    }
    private IEnumerator HitFlash()
    {
        enemyRenderer.material.color = hitFlashColor;

        yield return new WaitForSeconds(hitFlashDuration);

        enemyRenderer.material.color = originalColor;
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