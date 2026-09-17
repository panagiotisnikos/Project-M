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
    [Tooltip("Plays on every successful block, including a full (0-damage) block - " +
             "previously a full block gave no feedback at all beyond a console log.")]
    [SerializeField] private ParticleSystem blockVfx;
    [SerializeField] private float parryTrauma = 0.4f;
    [SerializeField] private float hurtTrauma = 0.3f;
    [SerializeField] private float blockTrauma = 0.18f;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtGruntSfx;
    [Range(0f, 1f)] [SerializeField] private float hurtGruntVolume = 0.5f;
    [SerializeField] private AudioClip blockSfx;
    [Range(0f, 1f)] [SerializeField] private float blockSfxVolume = 0.5f;
    [Tooltip("Plays when a block is attempted but fails for lack of stamina, so a hit " +
             "that passes straight through a held guard reads differently from a clean hit.")]
    [SerializeField] private AudioClip guardBreakSfx;
    [Range(0f, 1f)] [SerializeField] private float guardBreakVolume = 0.55f;

    [Header("References")]
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private HearthEmber hearthEmber;

    private int currentHealth;
    private bool isDead;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    /// <summary>Restore health, clamped to max. Used by consumables.</summary>
    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    /// <summary>Directly set health from a save file - no death check, no VFX/knockback,
    /// this is a silent state restore, not combat damage.</summary>
    public void RestoreHealth(int amount)
    {
        currentHealth = Mathf.Clamp(amount, 1, maxHealth);
    }

    private void Awake()
    {
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

        if (hearthEmber == null)
        {
            hearthEmber = GetComponent<HearthEmber>();
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

        if (performanceTracker != null)
        {
            performanceTracker.RegisterCleanDodge();
        }

        return;
    }

    damage = ApplyArmorReduction(damage);

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

            if (performanceTracker != null)
            {
                performanceTracker.RegisterBlock();
            }

            /*
             * Fires on every successful block, even a full (0-damage) one -
             * previously the only feedback below this point was gated on
             * finalDamage > 0, so a shield that fully absorbs a hit (e.g. the
             * Round Shield's 0 BlockedDamageMultiplier) gave no VFX/audio/shake
             * at all, just a console log.
             */
            CombatVfx.Play(
                blockVfx,
                transform.position + Vector3.up + transform.forward * 0.5f,
                -transform.forward
            );

            CombatAudio.Play(blockSfx, transform.position, blockSfxVolume);

            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.AddTrauma(blockTrauma);
            }

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
             * No guard-break state yet - the attack simply passes through the
             * guard for full damage (handled by the normal hurt-feedback path
             * below). This SFX is the only thing that distinguishes "you tried
             * to block but didn't have the stamina" from a plain unblocked hit.
             */
            CombatAudio.Play(guardBreakSfx, transform.position, guardBreakVolume);

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

        CombatAudio.Play(hurtGruntSfx, transform.position, hurtGruntVolume);

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
        if (hearthEmber != null && hearthEmber.TryConsume())
        {
            SurviveOnEmber();
        }
        else
        {
            Die();
        }
    }
}

    private void HandleParry(
        IStaggerable attacker,
        float staggerDuration)
    {
        Debug.Log(
            "[PlayerHealth] PARRY!"
        );

        if (performanceTracker != null)
        {
            performanceTracker.RegisterParry();
        }

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

    /// <summary>Flat passive reduction from equipped armor, applied before block/parry.</summary>
    private int ApplyArmorReduction(int damage)
    {
        if (playerEquipment == null || playerEquipment.EquippedArmor == null)
            return damage;

        float reduction = playerEquipment.EquippedArmor.DamageReduction;
        return Mathf.Max(1, Mathf.RoundToInt(damage * (1f - reduction)));
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
    /// <summary>
    /// The Refuge's one lightweight return-home benefit paying off: a banked
    /// HearthEmber charge (see RefugeZone/HearthEmber) spends itself to stop a
    /// killing blow from ending the run, leaving a small sliver of health
    /// instead of zero. A second chance, not a free heal - the player still
    /// has to fight or flee from there.
    /// </summary>
    private void SurviveOnEmber()
    {
        currentHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * 0.3f));

        Debug.Log(
            "[PlayerHealth] The hearth ember spared you from death!"
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

    /// <summary>
    /// The inverse of Die() - foundation for an in-place respawn (see
    /// GameUIController.RespawnPlayer()). Restores health (full, unless a
    /// specific amount is given) and resumes performance tracking.
    /// </summary>
    public void Respawn(int health = -1)
    {
        isDead = false;
        currentHealth = health > 0 ? Mathf.Min(health, maxHealth) : maxHealth;

        if (performanceTracker != null)
        {
            performanceTracker.ResumeTracking();
        }

        Debug.Log("[PlayerHealth] Player respawned.");
    }

    private void ApplyKnockback(
        Vector3 hitDirection,
        float force)
    {
        if (force <= 0f)
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

        /*
         * A raw Rigidbody impulse gets silently overridden every FixedUpdate by
         * PlayerMovement.Move()'s own MovePosition call, so knockback rides the
         * same kinematic displacement queue attack-steps use instead of physics.
         */
        if (playerMovement != null)
        {
            playerMovement.ApplyKnockback(hitDirection, force);
        }
    }
}