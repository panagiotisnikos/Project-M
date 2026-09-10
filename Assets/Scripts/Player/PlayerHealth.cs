using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("Base Knockback")]
    [SerializeField] private float knockbackForce = 4f;

    [Header("Feel / VFX")]
    [SerializeField] private ParticleSystem parryVfx;
    [SerializeField] private ParticleSystem hurtVfx;
    [SerializeField] private float parryTrauma = 0.4f;
    [SerializeField] private float hurtTrauma = 0.3f;

    [Header("References")]
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private PlayerStamina playerStamina;

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
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        if (playerEquipment == null)
        {
            playerEquipment =
                GetComponent<PlayerEquipment>();
        }
        if (playerStamina == null)
        {
            playerStamina =
                GetComponent<PlayerStamina>();
        }

        if (playerStamina == null)
        {
            Debug.LogWarning(
                "[PlayerHealth] PlayerStamina component is missing."
            );
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
    IStaggerable attacker)
{
    if (isDead)
        return;

    if (playerMovement != null &&
        playerMovement.IsInvulnerable)
    {
        Debug.Log(
            "[PlayerHealth] Attack avoided " +
            "with dodge i-frames."
        );

        return;
    }

    ShieldData shield =
        GetEquippedShield();

    bool isBlocking =
        playerMovement != null &&
        playerMovement.IsBlocking;

    bool attackIsFrontal =
        isBlocking &&
        shield != null &&
        IsAttackInsideBlockAngle(
            hitDirection,
            shield.BlockAngle
        );

    /*
     * Parry is checked before regular block.
     *
     * A correctly timed parry still requires
     * its small stamina cost.
     */
    if (attackIsFrontal &&
        playerMovement.TryConsumeParry())
    {
        if (TrySpendStamina(
                shield.ParryStaminaCost))
        {
            HandleParry(
                attacker,
                shield.ParryStaggerDuration
            );

            return;
        }

        Debug.Log(
            "[PlayerHealth] Parry timing succeeded, " +
            "but there was not enough stamina."
        );
    }

    int finalDamage = damage;

    float finalKnockbackForce =
        knockbackForce;

    if (attackIsFrontal)
    {
        float blockStaminaCost =
            damage *
            shield.BlockStaminaMultiplier;

        if (TrySpendStamina(
                blockStaminaCost))
        {
            finalDamage =
                Mathf.RoundToInt(
                    damage *
                    shield.BlockedDamageMultiplier
                );

            finalKnockbackForce *=
                shield.BlockedKnockbackMultiplier;

            Debug.Log(
                $"[PlayerHealth] BLOCK with " +
                $"{shield.ShieldName}! " +
                $"Damage reduced from {damage} " +
                $"to {finalDamage}. " +
                $"Stamina cost: " +
                $"{blockStaminaCost:0.0}."
            );
        }
        else
        {
            /*
             * No guard-break state yet.
             * The attack simply passes through the guard.
             */
            Debug.Log(
                $"[PlayerHealth] BLOCK FAILED! " +
                $"Not enough stamina. Required: " +
                $"{blockStaminaCost:0.0}."
            );
        }
    }

    currentHealth -= finalDamage;

    currentHealth =
        Mathf.Max(
            currentHealth,
            0
        );

    ApplyKnockback(
        hitDirection,
        finalKnockbackForce
    );

    if (finalDamage > 0)
    {
        if (performanceTracker != null)
        {
            performanceTracker
                .RegisterDamageTaken(
                    finalDamage
                );
        }

        CombatVfx.Play(
            hurtVfx,
            transform.position + Vector3.up
        );

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.AddTrauma(hurtTrauma);
        }
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

    private void HandleParry(
        IStaggerable attacker,
        float staggerDuration)
    {
        Debug.Log(
            "[PlayerHealth] PARRY!"
        );

        attacker?.Stagger(
            staggerDuration
        );

        CombatVfx.Play(
            parryVfx,
            transform.position + Vector3.up + transform.forward * 0.6f,
            -transform.forward
        );

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.AddTrauma(parryTrauma);
        }
    }

    private ShieldData GetEquippedShield()
    {
        if (playerEquipment == null)
            return null;

        return playerEquipment.EquippedShield;
    }

    private bool IsAttackInsideBlockAngle(
        Vector3 hitDirection,
        float blockAngle)
    {
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

        if (directionToAttacker.sqrMagnitude <
            0.01f)
        {
            return false;
        }

        directionToAttacker.Normalize();

        float angleToAttacker =
            Vector3.Angle(
                transform.forward,
                directionToAttacker
            );

        float halfBlockAngle =
            blockAngle * 0.5f;

        return angleToAttacker <=
               halfBlockAngle;
    }
    private bool TrySpendStamina(
        float amount)
    {
        if (amount <= 0f)
            return true;

        if (playerStamina == null)
        {
            /*
            * Temporary fail-safe if the stamina
            * component has not been assigned.
            */
            return true;
        }

        return playerStamina.TrySpend(
            amount
        );
    }
    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log(
            "[PlayerHealth] Player died."
        );

        if (performanceTracker != null)
        {
            performanceTracker.StopTracking();
        }
    }

    private void ApplyKnockback(
        Vector3 hitDirection,
        float force)
    {
        if (rb == null ||
            force <= 0f)
        {
            return;
        }

        hitDirection.y = 0f;

        if (hitDirection.sqrMagnitude <
            0.01f)
        {
            return;
        }

        hitDirection.Normalize();

        rb.AddForce(
            hitDirection * force,
            ForceMode.Impulse
        );
    }
}