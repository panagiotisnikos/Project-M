using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Tooltip("Overwritten at Awake by enemyAI's EnemyData, if assigned.")]
    [SerializeField] private int maxHealth = 30;

    [Header("Hit Reaction")]
    [SerializeField] private float hitReactionDuration = 0.18f;
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float knockbackForce = 4f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem hitVfx;
    [SerializeField] private ParticleSystem deathVfx;
    [SerializeField] private float deathLinger = 0.4f;

    [Header("Audio")]
    [Tooltip("Role-specific hit reaction (Brute/StalkerGetsHit).")]
    [SerializeField] private AudioClip hitSfx;
    [Range(0f, 1f)] [SerializeField] private float hitVolume = 0.45f;
    [SerializeField] private AudioClip deathSfx;
    [Range(0f, 1f)] [SerializeField] private float deathVolume = 0.5f;

    [Header("References")]
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    [SerializeField] private EnemyAI enemyAI;
    [Tooltip("Optional - if present, its RewardTable/AdaptiveRewardTable is granted on death. " +
             "The archetype's 'loot profile' lives entirely in whichever table this points at; " +
             "not every enemy needs one (see LootDropper for the older per-entry-chance table).")]
    [SerializeField] private RewardSource rewardSource;

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

        if (rewardSource == null)
        {
            rewardSource = GetComponent<RewardSource>();
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

        CombatAudio.Play(hitSfx, transform.position, hitVolume);

        StartHitFlash();

        DevLog.Log(
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
        if (enemyAI != null && enemyAI.Data != null)
        {
            maxHealth = enemyAI.Data.maxHealth;
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

        DevLog.Log($"{gameObject.name} died.");

        if (performanceTracker != null)
        {
            performanceTracker.RegisterEnemyKilled();
        }

        CombatVfx.Play(
            deathVfx,
            transform.position + Vector3.up * 0.9f
        );

        CombatAudio.Play(deathSfx, transform.position, deathVolume);

        if (rewardSource != null)
        {
            rewardSource.Grant();
        }

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