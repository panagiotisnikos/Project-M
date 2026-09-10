using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private EnemyAI.EnemyRole role;

    [Header("Hit Reaction")]
    [SerializeField] private float hitReactionDuration = 0.18f;
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float knockbackForce = 4f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem hitVfx;
    [SerializeField] private ParticleSystem deathVfx;
    [SerializeField] private float deathLinger = 0.4f;

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
            // spin to face the attacker if we were unaware, then react
            enemyAI.NotifyDamaged(-hitDirection);
            enemyAI.HitReact(
                reactionDuration
            );
        }

        CombatVfx.Play(
            hitVfx,
            transform.position + Vector3.up * 0.9f,
            -hitDirection
        );

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
        hitDirection.y = 0f;

        if (hitDirection.sqrMagnitude < 0.01f)
            return;

        hitDirection.Normalize();

        float knockback =
            knockbackForce *
            Mathf.Max(0f, forceMultiplier);

        // Movement is NavMesh-driven now, so a knockback is a short shove
        // along the navmesh rather than a physics impulse.
        if (enemyAI != null)
        {
            enemyAI.Nudge(hitDirection, knockback * 0.06f);
        }
        else if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(hitDirection * knockback, ForceMode.Impulse);
        }
    }

    /// <summary>Fired once when the enemy dies, so the animator can play its death clip.</summary>
    public event System.Action Died;

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

        CombatVfx.Play(
            deathVfx,
            transform.position + Vector3.up * 0.9f
        );

        Died?.Invoke();

        /*
         * Stop acting and stop blocking the player immediately, but keep the
         * mesh visible so the death animation can play out before Destroy.
         */
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        Destroy(gameObject, Mathf.Max(0f, deathLinger));
    }
}