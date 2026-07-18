using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private EnemyAI.EnemyRole role;

    [Header("Hit Reaction")]
    [SerializeField] private float hitReactionDuration = 0.18f;
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float knockbackForce = 4f;

    [Header("References")]
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    [SerializeField] private EnemyAI enemyAI;

    private Rigidbody rb;
    private Renderer enemyRenderer;

    private Color originalColor;
    private int currentHealth;
    private bool isDead;

    private Coroutine hitFlashCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyRenderer = GetComponentInChildren<Renderer>();

        if (enemyAI == null)
        {
            enemyAI = GetComponent<EnemyAI>();
        }

        if (enemyRenderer != null)
        {
            originalColor =
                enemyRenderer.material.color;
        }

        if (performanceTracker == null)
        {
            performanceTracker =
                FindFirstObjectByType<PlayerPerformanceTracker>();
        }

        ConfigureHealth();
        currentHealth = maxHealth;
    }

    public void TakeDamage(
        int damage,
        Vector3 hitDirection)
    {
        TakeDamage(
            damage,
            hitDirection,
            1f,
            hitReactionDuration
        );
    }

    public void TakeDamage(
        int damage,
        Vector3 hitDirection,
        float knockbackMultiplier,
        float reactionDuration)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        ApplyKnockback(
            hitDirection,
            knockbackMultiplier
        );

        if (currentHealth > 0 &&
            enemyAI != null)
        {
            enemyAI.HitReact(
                reactionDuration
            );
        }

        StartHitFlash();

        Debug.Log(
            $"{gameObject.name} took {damage} damage. " +
            $"HP: {currentHealth}/{maxHealth}"
        );

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

    private void StartHitFlash()
    {
        if (enemyRenderer == null)
            return;

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }

        hitFlashCoroutine =
            StartCoroutine(HitFlash());
    }

    private IEnumerator HitFlash()
    {
        enemyRenderer.material.color =
            hitFlashColor;

        yield return new WaitForSeconds(
            hitFlashDuration
        );

        /*
         * Αν ο enemy βρίσκεται ακόμη σε parry stagger,
         * δεν θέλουμε το flash να αφαιρέσει το cyan.
         *
         * Στην απλή πρώτη έκδοση, το EnemyAI θα ξαναδώσει
         * το σωστό stagger visual όταν γίνει parry.
         */
        enemyRenderer.material.color =
            originalColor;

        hitFlashCoroutine = null;
    }

    private void ApplyKnockback(
    Vector3 hitDirection,
    float forceMultiplier)
    {
        if (rb == null)
            return;

        hitDirection.y = 0f;

        if (hitDirection.sqrMagnitude < 0.01f)
            return;

        hitDirection.Normalize();

        float finalKnockbackForce =
            knockbackForce *
            Mathf.Max(0f, forceMultiplier);

        rb.AddForce(
            hitDirection * finalKnockbackForce,
            ForceMode.Impulse
        );
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log($"{gameObject.name} died.");

        if (performanceTracker != null)
        {
            performanceTracker.RegisterEnemyKilled();
        }

        Destroy(gameObject);
    }
}