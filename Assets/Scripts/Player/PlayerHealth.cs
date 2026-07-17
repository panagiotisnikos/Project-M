using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("Blocking")]
    [SerializeField] private float blockAngle = 120f;
    [SerializeField] private float blockedDamageMultiplier = 0f;

    [Header("Parry")]
    [SerializeField] private float parryStaggerDuration = 1.25f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float blockedKnockbackMultiplier = 0.2f;

    [Header("References")]
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    [SerializeField] private PlayerMovement playerMovement;

    private int currentHealth;
    private bool isDead;

    private Rigidbody rb;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        currentHealth = maxHealth;
    }

    public void TakeDamage(
        int damage,
        Vector3 hitDirection)
    {
        TakeDamage(
            damage,
            hitDirection,
            null
        );
    }

    public void TakeDamage(
        int damage,
        Vector3 hitDirection,
        EnemyAI attacker)
    {
        if (isDead)
            return;

        bool attackIsFrontal =
            IsAttackInsideBlockAngle(hitDirection);

        if (attackIsFrontal &&
            playerMovement != null &&
            playerMovement.TryConsumeParry())
        {
            HandleParry(attacker);
            return;
        }

        bool blocked =
            attackIsFrontal &&
            playerMovement != null &&
            playerMovement.IsBlocking;

        int finalDamage = damage;
        float finalKnockbackForce = knockbackForce;

        if (blocked)
        {
            finalDamage = Mathf.RoundToInt(
                damage * blockedDamageMultiplier
            );

            finalKnockbackForce *=
                blockedKnockbackMultiplier;

            Debug.Log(
                $"[PlayerHealth] BLOCK! " +
                $"Damage reduced from {damage} " +
                $"to {finalDamage}."
            );
        }

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(currentHealth, 0);

        ApplyKnockback(
            hitDirection,
            finalKnockbackForce
        );

        if (performanceTracker != null &&
            finalDamage > 0)
        {
            performanceTracker.RegisterDamageTaken(
                finalDamage
            );
        }

        Debug.Log(
            $"[PlayerHealth] Health: " +
            $"{currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void HandleParry(EnemyAI attacker)
    {
        Debug.Log("[PlayerHealth] PARRY!");

        if (attacker != null)
        {
            attacker.Stagger(parryStaggerDuration);
        }
    }

    private bool IsAttackInsideBlockAngle(
        Vector3 hitDirection)
    {
        if (playerMovement == null ||
            !playerMovement.IsBlocking)
        {
            return false;
        }

        /*
         * hitDirection points from the attacker
         * toward the player.
         *
         * Reverse it to get the direction
         * from the player toward the attacker.
         */
        Vector3 directionToAttacker =
            -hitDirection;

        directionToAttacker.y = 0f;

        if (directionToAttacker.sqrMagnitude < 0.01f)
            return false;

        directionToAttacker.Normalize();

        float angleToAttacker =
            Vector3.Angle(
                transform.forward,
                directionToAttacker
            );

        float halfBlockAngle =
            blockAngle * 0.5f;

        return angleToAttacker <= halfBlockAngle;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("[PlayerHealth] Player died.");

        if (performanceTracker != null)
        {
            performanceTracker.StopTracking();
        }
    }

    private void ApplyKnockback(
        Vector3 hitDirection,
        float force)
    {
        if (rb == null || force <= 0f)
            return;

        hitDirection.y = 0f;
        hitDirection.Normalize();

        rb.AddForce(
            hitDirection * force,
            ForceMode.Impulse
        );
    }
}