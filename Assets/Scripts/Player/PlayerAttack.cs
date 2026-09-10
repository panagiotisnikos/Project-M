using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    private enum AttackState
    {
        Idle,
        Windup,
        Recovery
    }

    private enum AttackType
    {
        Light,
        Heavy
    }

    private const int MaxComboSteps = 3;

    [Header("Hit Stop Runtime")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float hitStopTimeScale = 0.05f;

    [Header("Combo Feel")]
    [Tooltip("Extra time after a light hit's recovery window during which a late light-attack " +
             "press still continues the combo instead of starting a fresh one. Higher = more " +
             "forgiving. Scaled by the weapon's attack speed.")]
    [SerializeField] private float comboBufferGrace = 0.4f;

    [Header("Safety")]
    [Tooltip("If a windup never receives its AnimationAttackHit event within this long (scaled " +
             "by attack speed), the hit is resolved in code so the player can never lock up. " +
             "Should never fire in normal play.")]
    [SerializeField] private float windupFallbackCap = 2.5f;

    [Header("Light Hit")]
    [Tooltip("Hit-reaction (flinch) duration applied to a target struck by a light attack.")]
    [SerializeField] private float lightHitReactionDuration = 0.18f;

    [Header("Feel / VFX")]
    [SerializeField] private ParticleSystem lightHitVfx;
    [SerializeField] private ParticleSystem heavyHitVfx;
    [SerializeField] private float lightHitTrauma = 0.14f;
    [SerializeField] private float heavyHitTrauma = 0.35f;

    [Header("References")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private PlayerStamina playerStamina;

    private AttackState currentState =
        AttackState.Idle;

    private AttackType currentAttackType =
        AttackType.Light;

    /*
     * The weapon used when the attack begins.
     *
     * This prevents an equipment swap during an attack
     * from changing the weapon halfway through the sequence.
     */
    private WeaponData activeWeapon;

    private int currentComboStep;
    private bool nextAttackQueued;

    private float stateEnteredTime;

    /*
     * While in Recovery, a light-attack press received any time before this
     * moment continues the combo. After it, the swing ends and a fresh press
     * starts a new combo from step 1.
     */
    private float comboWindowEnd;

    private Coroutine hitStopCoroutine;
    private float normalFixedDeltaTime;

    public bool IsAttacking =>
        currentState != AttackState.Idle;

    public bool IsUsingHeavyAttack =>
        IsAttacking &&
        currentAttackType == AttackType.Heavy;

    public int CurrentComboStep =>
        currentAttackType == AttackType.Light &&
        currentState != AttackState.Idle
            ? currentComboStep
            : 0;

    /*
     * The active weapon's swing-speed multiplier (Animator playback + all code
     * timers scale by this). Falls back to the equipped weapon between swings so
     * PlayerAnimationController can prime the Animator before the first hit.
     */
    private float AttackSpeedMultiplier
    {
        get
        {
            WeaponData weapon = activeWeapon != null
                ? activeWeapon
                : GetEquippedWeapon();

            return weapon != null
                ? Mathf.Max(0.1f, weapon.AttackSpeedMultiplier)
                : 1f;
        }
    }

    public float CurrentAttackSpeed => AttackSpeedMultiplier;

    public float CurrentMovementMultiplier
    {
        get
        {
            if (!IsAttacking ||
                activeWeapon == null)
            {
                return 1f;
            }

            return currentAttackType == AttackType.Heavy
                ? activeWeapon.HeavyMovementMultiplier
                : activeWeapon.LightMovementMultiplier;
        }
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
                "[PlayerAttack] PlayerStamina component is missing."
            );
        }
        normalFixedDeltaTime =
            Time.fixedDeltaTime;
    }

    private void Update()
    {
        if (GameUIController.IsPaused)
            return;

        TickAttackState();
        ReadAttackInput();
    }
    private void ReadAttackInput()
{
    bool lightAttackPressed =
        Input.GetMouseButtonDown(0);

    bool heavyAttackPressed =
        Input.GetMouseButtonDown(2);

    if (!lightAttackPressed &&
        !heavyAttackPressed)
    {
        return;
    }

    if (playerMovement != null &&
        (playerMovement.IsBlocking ||
         playerMovement.IsDodging))
    {
        return;
    }

    if (currentState == AttackState.Idle)
    {
        WeaponData equippedWeapon =
            GetEquippedWeapon();

        if (equippedWeapon == null)
        {
            Debug.LogWarning(
                "[PlayerAttack] Attack ignored. " +
                "No weapon is equipped."
            );

            return;
        }

        if (heavyAttackPressed)
        {
            if (!TrySpendStamina(
                    equippedWeapon.HeavyStaminaCost,
                    "heavy attack"))
            {
                return;
            }

            StartHeavyAttack(
                equippedWeapon
            );
        }
        else if (lightAttackPressed)
        {
            if (!TrySpendStamina(
                    equippedWeapon.FirstLightStaminaCost,
                    "light attack 1"))
            {
                return;
            }

            StartLightCombo(
                equippedWeapon
            );
        }

        return;
    }

    if (currentAttackType == AttackType.Light &&
        lightAttackPressed)
    {
        QueueNextLightAttack();
    }
}

    /*
     * Drives everything after the hit in code so the combo can never be lost to
     * a late press or a dropped animation event:
     *  - Windup: AnimationAttackHit resolves the hit. If it never arrives, a
     *    code fallback resolves it after windupFallbackCap (safety only).
     *  - Recovery: a buffered light press continues the combo immediately; with
     *    no buffered press the swing ends once comboWindowEnd passes.
     */
    private void TickAttackState()
    {
        switch (currentState)
        {
            case AttackState.Windup:

                if (Time.time - stateEnteredTime >
                    windupFallbackCap / AttackSpeedMultiplier)
                {
                    Debug.LogWarning(
                        "[PlayerAttack] Windup exceeded the fallback cap " +
                        "(missing AnimationAttackHit?). Resolving the hit in code."
                    );

                    PerformAttackHit();
                    BeginAttackRecovery();
                }

                break;

            case AttackState.Recovery:

                TickRecovery();
                break;
        }
    }

    private void TickRecovery()
    {
        if (currentAttackType == AttackType.Light &&
            nextAttackQueued &&
            currentComboStep < MaxComboSteps)
        {
            TryAdvanceLightCombo();
            return;
        }

        if (Time.time >= comboWindowEnd)
        {
            FinishAttackSequence();
        }
    }

    private void TryAdvanceLightCombo()
    {
        int nextComboStep = currentComboStep + 1;

        if (!TrySpendStamina(
                GetLightStaminaCost(nextComboStep),
                $"light attack {nextComboStep}"))
        {
            nextAttackQueued = false;
            return;
        }

        currentComboStep = nextComboStep;
        nextAttackQueued = false;

        BeginAttackWindup();

        Debug.Log(
            $"[PlayerAttack] Chained into light attack {currentComboStep}."
        );
    }

    private void StartLightCombo(
        WeaponData weapon)
    {
        activeWeapon = weapon;

        currentAttackType =
            AttackType.Light;

        currentComboStep = 1;
        nextAttackQueued = false;

        BeginAttackWindup();

        Debug.Log(
            $"[PlayerAttack] Light combo started " +
            $"with {activeWeapon.WeaponName}."
        );
    }

    private void StartHeavyAttack(
        WeaponData weapon)
    {
        activeWeapon = weapon;

        currentAttackType =
            AttackType.Heavy;

        currentComboStep = 0;
        nextAttackQueued = false;

        BeginAttackWindup();

        Debug.Log(
            $"[PlayerAttack] Heavy attack started " +
            $"with {activeWeapon.WeaponName}."
        );
    }

    private void QueueNextLightAttack()
    {
        if (currentComboStep >= MaxComboSteps)
            return;

        if (nextAttackQueued)
            return;

        nextAttackQueued = true;

        Debug.Log(
            $"[PlayerAttack] Light attack " +
            $"{currentComboStep + 1} queued."
        );
    }

    private void BeginAttackWindup()
    {
        if (activeWeapon == null)
        {
            CancelAttackSequence();
            return;
        }

        currentState =
            AttackState.Windup;

        stateEnteredTime = Time.time;

        // Light attacks get a small coded forward step; the heavy attack's lunge
        // comes from the animation's root motion (PlayerRootMotion) instead.
        if (playerMovement != null &&
            currentAttackType == AttackType.Light)
        {
            playerMovement.QueueAttackStep(
                GetCurrentForwardStep()
            );
        }

        if (currentAttackType ==
            AttackType.Heavy)
        {
            Debug.Log(
                "[PlayerAttack] Heavy attack animation started."
            );
        }
        else
        {
            Debug.Log(
                $"[PlayerAttack] Light attack " +
                $"{currentComboStep} animation started."
            );
        }
    }

    private void PerformAttackHit()
    {
        if (activeWeapon == null)
            return;

        /*
         * Forward commitment occurs whether the
         * attack hits or misses.
         */
        bool hitEnemy = false;

        int currentDamage =
            GetCurrentAttackDamage();

        float currentRange =
            GetCurrentAttackRange();

        float currentRadius =
            GetCurrentAttackRadius();

        Vector3 attackCenter =
            transform.position +
            transform.forward *
            currentRange;

        Collider[] hits =
            Physics.OverlapSphere(
                attackCenter,
                currentRadius,
                enemyLayer
            );

        foreach (Collider hit in hits)
        {
            IDamageable target =
                hit.GetComponentInParent<IDamageable>();

            if (target == null)
                continue;

            bool heavy = currentAttackType == AttackType.Heavy;

            if (heavy)
            {
                target.TakeDamage(
                    currentDamage,
                    transform.forward,
                    activeWeapon.HeavyKnockbackMultiplier,
                    activeWeapon.HeavyHitReactionDuration
                );
            }
            else
            {
                target.TakeDamage(
                    currentDamage,
                    transform.forward,
                    1f,
                    lightHitReactionDuration
                );
            }

            Vector3 contact = hit.ClosestPoint(attackCenter);

            CombatVfx.Play(
                heavy ? heavyHitVfx : lightHitVfx,
                contact,
                -transform.forward
            );

            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.AddTrauma(
                    heavy ? heavyHitTrauma : lightHitTrauma
                );
            }

            hitEnemy = true;
            break;
        }

        if (hitEnemy)
        {
            if (currentAttackType ==
                AttackType.Heavy)
            {
                Debug.Log(
                    $"[PlayerAttack] Heavy attack hit " +
                    $"for {currentDamage} damage."
                );
            }
            else
            {
                Debug.Log(
                    $"[PlayerAttack] Light attack " +
                    $"{currentComboStep} hit " +
                    $"for {currentDamage} damage."
                );
            }

            StartHitStop();
        }
        else
        {
            Debug.Log(
                currentAttackType ==
                AttackType.Heavy
                    ? "[PlayerAttack] Heavy attack missed."
                    : $"[PlayerAttack] Light attack " +
                      $"{currentComboStep} missed."
            );
        }
    }

    private void BeginAttackRecovery()
    {
        if (activeWeapon == null)
        {
            CancelAttackSequence();
            return;
        }

        currentState =
            AttackState.Recovery;

        stateEnteredTime = Time.time;

        float recovery = GetCurrentRecoveryDuration();

        if (currentAttackType == AttackType.Light)
        {
            recovery += comboBufferGrace;
        }

        comboWindowEnd =
            Time.time + recovery / AttackSpeedMultiplier;

        Debug.Log(
            currentAttackType ==
            AttackType.Heavy
                ? "[PlayerAttack] Heavy attack recovery."
                : $"[PlayerAttack] Light attack " +
                $"{currentComboStep} recovery."
        );
    }

    private void FinishAttackSequence()
    {
        if (currentAttackType ==
            AttackType.Heavy)
        {
            Debug.Log(
                "[PlayerAttack] Heavy attack finished."
            );
        }
        else
        {
            Debug.Log(
                $"[PlayerAttack] Light combo finished " +
                $"after attack {currentComboStep}."
            );
        }

        ResetAttackState();
    }

    private void CancelAttackSequence()
    {
        Debug.LogWarning(
            "[PlayerAttack] Attack sequence cancelled " +
            "because no active weapon was available."
        );

        ResetAttackState();
    }

    private void ResetAttackState()
    {
        currentState =
            AttackState.Idle;

        currentAttackType =
            AttackType.Light;

        activeWeapon = null;

        currentComboStep = 0;
        nextAttackQueued = false;
    }

    private WeaponData GetEquippedWeapon()
    {
        if (playerEquipment == null)
            return null;

        return playerEquipment.EquippedWeapon;
    }

    private float GetCurrentWindupDuration()
    {
        if (activeWeapon == null)
            return 0f;

        if (currentAttackType ==
            AttackType.Heavy)
        {
            return activeWeapon.HeavyWindupDuration;
        }

        switch (currentComboStep)
        {
            case 1:
                return activeWeapon.FirstWindupDuration;

            case 2:
                return activeWeapon.SecondWindupDuration;

            case 3:
                return activeWeapon.ThirdWindupDuration;

            default:
                return activeWeapon.FirstWindupDuration;
        }
    }

    private float GetCurrentRecoveryDuration()
    {
        if (activeWeapon == null)
            return 0f;

        if (currentAttackType ==
            AttackType.Heavy)
        {
            return activeWeapon.HeavyRecoveryDuration;
        }

        switch (currentComboStep)
        {
            case 1:
                return activeWeapon.FirstRecoveryDuration;

            case 2:
                return activeWeapon.SecondRecoveryDuration;

            case 3:
                return activeWeapon.ThirdRecoveryDuration;

            default:
                return activeWeapon.FirstRecoveryDuration;
        }
    }

    private float GetCurrentDamageMultiplier()
    {
        if (activeWeapon == null)
            return 1f;

        if (currentAttackType ==
            AttackType.Heavy)
        {
            return activeWeapon.HeavyDamageMultiplier;
        }

        switch (currentComboStep)
        {
            case 1:
                return activeWeapon.FirstDamageMultiplier;

            case 2:
                return activeWeapon.SecondDamageMultiplier;

            case 3:
                return activeWeapon.ThirdDamageMultiplier;

            default:
                return 1f;
        }
    }

    private float GetCurrentForwardStep()
    {
        if (activeWeapon == null)
            return 0f;

        if (currentAttackType ==
            AttackType.Heavy)
        {
            return activeWeapon.HeavyForwardStep;
        }

        switch (currentComboStep)
        {
            case 1:
                return activeWeapon.FirstForwardStep;

            case 2:
                return activeWeapon.SecondForwardStep;

            case 3:
                return activeWeapon.ThirdForwardStep;

            default:
                return activeWeapon.FirstForwardStep;
        }
    }

    private int GetCurrentAttackDamage()
    {
        if (activeWeapon == null)
            return 0;

        float damage =
            activeWeapon.BaseDamage *
            GetCurrentDamageMultiplier();

        return Mathf.Max(
            1,
            Mathf.RoundToInt(damage)
        );
    }

    private float GetCurrentAttackRange()
    {
        if (activeWeapon == null)
            return 0f;

        return currentAttackType ==
               AttackType.Heavy
            ? activeWeapon.HeavyRange
            : activeWeapon.LightAttackRange;
    }

    private float GetCurrentAttackRadius()
    {
        if (activeWeapon == null)
            return 0f;

        return currentAttackType ==
               AttackType.Heavy
            ? activeWeapon.HeavyRadius
            : activeWeapon.LightAttackRadius;
    }

    private float GetCurrentHitStopDuration()
    {
        if (activeWeapon == null)
            return 0f;

        return currentAttackType ==
               AttackType.Heavy
            ? activeWeapon.HeavyHitStopDuration
            : activeWeapon.LightHitStopDuration;
    }
    private float GetLightStaminaCost(
        int comboStep)
    {
        if (activeWeapon == null)
            return 0f;

        switch (comboStep)
        {
            case 1:
                return activeWeapon
                    .FirstLightStaminaCost;

            case 2:
                return activeWeapon
                    .SecondLightStaminaCost;

            case 3:
                return activeWeapon
                    .ThirdLightStaminaCost;

            default:
                return activeWeapon
                    .FirstLightStaminaCost;
        }
    }

    private bool TrySpendStamina(
        float amount,
        string actionName)
    {
        if (playerStamina == null)
        {
            /*
            * Fail-safe so combat does not become unusable
            * if the component was accidentally omitted.
            */
            return true;
        }

        if (playerStamina.TrySpend(amount))
            return true;

        Debug.Log(
            $"[PlayerAttack] Not enough stamina " +
            $"for {actionName}. " +
            $"Required: {amount:0.0}, " +
            $"Available: " +
            $"{playerStamina.CurrentStamina:0.0}."
        );

        return false;
    }
    private void StartHitStop()
    {
        if (activeWeapon == null)
            return;

        if (hitStopCoroutine != null)
        {
            StopCoroutine(
                hitStopCoroutine
            );

            RestoreTimeScale();
        }

        float duration =
            GetCurrentHitStopDuration();

        hitStopCoroutine =
            StartCoroutine(
                HitStopRoutine(duration)
            );
    }

    private IEnumerator HitStopRoutine(
        float duration)
    {
        Time.timeScale =
            hitStopTimeScale;

        Time.fixedDeltaTime =
            normalFixedDeltaTime *
            hitStopTimeScale;

        yield return new WaitForSecondsRealtime(
            duration
        );

        RestoreTimeScale();

        hitStopCoroutine = null;
    }

    private void RestoreTimeScale()
    {
        Time.timeScale =
            GameUIController.IsPaused
                ? 0f
                : 1f;

        Time.fixedDeltaTime =
            normalFixedDeltaTime;
    }

    private void OnDisable()
    {
        if (hitStopCoroutine != null)
        {
            StopCoroutine(
                hitStopCoroutine
            );

            hitStopCoroutine = null;
        }

        RestoreTimeScale();
    }

    private void OnDrawGizmosSelected()
    {
        PlayerEquipment equipment =
            playerEquipment;

        if (equipment == null)
        {
            equipment =
                GetComponent<PlayerEquipment>();
        }

        WeaponData weapon =
            equipment != null
                ? equipment.EquippedWeapon
                : null;

        if (weapon == null)
            return;

        Gizmos.color = Color.yellow;

        Vector3 lightAttackCenter =
            transform.position +
            transform.forward *
            weapon.LightAttackRange;

        Gizmos.DrawWireSphere(
            lightAttackCenter,
            weapon.LightAttackRadius
        );

        Gizmos.color = Color.red;

        Vector3 heavyAttackCenter =
            transform.position +
            transform.forward *
            weapon.HeavyRange;

        Gizmos.DrawWireSphere(
            heavyAttackCenter,
            weapon.HeavyRadius
        );
    }
    public void AnimationAttackHit()
    {
        if (currentState != AttackState.Windup)
            return;

        if (activeWeapon == null)
            return;

        PerformAttackHit();
        BeginAttackRecovery();
    }

    /*
     * Combo continuation and swing end are now code-driven (see TickRecovery).
     * These handlers are kept as no-ops so the existing clip events don't warn,
     * and so re-timed or re-authored clips stay compatible.
     */
    public void AnimationAttackFinished(int expectedComboStep)
    {
    }

    public void AnimationComboChain(int expectedComboStep)
    {
    }
}