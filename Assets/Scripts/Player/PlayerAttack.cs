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

    [Header("Safety")]
    [Tooltip("If a single attack state (windup or recovery) lasts longer than this in seconds, " +
             "the swing is force-ended and a warning is logged. Watchdog against a missing or " +
             "mis-authored animation event - it should never fire in normal play.")]
    [SerializeField] private float attackStuckTimeout = 4f;

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

        TickStuckAttackWatchdog();
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
     * Safety net only. The attack lifecycle is driven by animation events
     * (AnimationAttackHit / AnimationComboChain / AnimationAttackFinished).
     * If one of those never arrives - a missing or mis-authored event - the
     * player would otherwise be locked in an attack forever. This forces the
     * swing to end after attackStuckTimeout and logs where it happened.
     */
    private void TickStuckAttackWatchdog()
    {
        if (currentState == AttackState.Idle)
            return;

        if (Time.time - stateEnteredTime <= attackStuckTimeout)
            return;

        Debug.LogWarning(
            $"[PlayerAttack] {currentState} exceeded " +
            $"{attackStuckTimeout:0.0}s (missing animation event?). " +
            $"Force-ending the attack."
        );

        FinishAttackSequence();
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

        if (playerMovement != null)
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
            EnemyHealth enemyHealth =
                hit.GetComponent<EnemyHealth>();

            if (enemyHealth == null)
            {
                enemyHealth =
                    hit.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth == null)
                continue;

            if (currentAttackType ==
                AttackType.Heavy)
            {
                enemyHealth.TakeDamage(
                    currentDamage,
                    transform.forward,
                    activeWeapon.HeavyKnockbackMultiplier,
                    activeWeapon.HeavyHitReactionDuration
                );
            }
            else
            {
                enemyHealth.TakeDamage(
                    currentDamage,
                    transform.forward
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

    public void AnimationAttackFinished(
        int expectedComboStep)
    {
        if (currentState == AttackState.Idle)
            return;

        if (currentAttackType == AttackType.Light &&
            currentComboStep != expectedComboStep)
        {
            return;
        }

        FinishAttackSequence();
    }
    public void AnimationComboChain(int expectedComboStep)
    {
        if (currentAttackType != AttackType.Light)
            return;

        if (currentComboStep != expectedComboStep)
            return;

        if (!nextAttackQueued)
            return;

        if (currentComboStep >= MaxComboSteps)
            return;

        int nextComboStep =
            currentComboStep + 1;

        float staminaCost =
            GetLightStaminaCost(
                nextComboStep
            );

        if (!TrySpendStamina(
                staminaCost,
                $"light attack {nextComboStep}"))
        {
            nextAttackQueued = false;
            return;
        }

        currentComboStep =
            nextComboStep;

        nextAttackQueued = false;

        BeginAttackWindup();

        Debug.Log(
            $"[PlayerAttack] Chained into light attack " +
            $"{currentComboStep}."
        );
    }
}