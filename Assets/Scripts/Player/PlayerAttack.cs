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

    private const int MaxComboSteps = 3;

    [Header("Base Attack")]
    [SerializeField] private int attackDamage = 10;

    [Header("Attack Range")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackRadius = 0.7f;

    [Header("Combo Step 1")]
    [SerializeField] private float firstWindupDuration = 0.15f;
    [SerializeField] private float firstRecoveryDuration = 0.25f;
    [SerializeField] private float firstDamageMultiplier = 1f;

    [Header("Combo Step 2")]
    [SerializeField] private float secondWindupDuration = 0.12f;
    [SerializeField] private float secondRecoveryDuration = 0.25f;
    [SerializeField] private float secondDamageMultiplier = 1f;

    [Header("Combo Step 3")]
    [SerializeField] private float thirdWindupDuration = 0.18f;
    [SerializeField] private float thirdRecoveryDuration = 0.45f;
    [SerializeField] private float thirdDamageMultiplier = 1.25f;

    [Header("Hit Stop")]
    [SerializeField] private float hitStopDuration = 0.06f;

    [Range(0.01f, 0.5f)]
    [SerializeField] private float hitStopTimeScale = 0.05f;

    [Header("References")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private PlayerMovement playerMovement;

    private AttackState currentState = AttackState.Idle;

    private int currentComboStep;
    private bool nextAttackQueued;

    private float stateEndTime;

    private Coroutine hitStopCoroutine;
    private float normalFixedDeltaTime;

    public bool IsAttacking =>
        currentState != AttackState.Idle;

    public int CurrentComboStep =>
        currentState == AttackState.Idle
            ? 0
            : currentComboStep;

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        normalFixedDeltaTime =
            Time.fixedDeltaTime;
    }

    private void Update()
    {
        ReadAttackInput();
        UpdateAttackState();
    }

    private void ReadAttackInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (playerMovement != null &&
            playerMovement.IsBlocking)
        {
            return;
        }

        if (currentState == AttackState.Idle)
        {
            StartCombo();
            return;
        }

        QueueNextAttack();
    }

    private void UpdateAttackState()
    {
        switch (currentState)
        {
            case AttackState.Idle:
                break;

            case AttackState.Windup:

                if (Time.time >= stateEndTime)
                {
                    PerformAttackHit();
                    BeginAttackRecovery();
                }

                break;

            case AttackState.Recovery:

                if (Time.time >= stateEndTime)
                {
                    ResolveRecoveryEnd();
                }

                break;
        }
    }

    private void StartCombo()
    {
        currentComboStep = 1;
        nextAttackQueued = false;

        BeginAttackWindup();

        Debug.Log("[PlayerAttack] Combo started.");
    }

    private void QueueNextAttack()
    {
        if (currentComboStep >= MaxComboSteps)
            return;

        if (nextAttackQueued)
            return;

        nextAttackQueued = true;

        Debug.Log(
            $"[PlayerAttack] Attack " +
            $"{currentComboStep + 1} queued."
        );
    }

    private void BeginAttackWindup()
    {
        currentState = AttackState.Windup;

        float windupDuration =
            GetCurrentWindupDuration();

        stateEndTime =
            Time.time + windupDuration;

        Debug.Log(
            $"[PlayerAttack] Attack {currentComboStep} started. " +
            $"Hit in {windupDuration:0.00}s."
        );
    }

    private void PerformAttackHit()
    {
        bool hitEnemy = false;

        int currentDamage =
            GetCurrentAttackDamage();

        Vector3 attackCenter =
            transform.position +
            transform.forward * attackRange;

        Collider[] hits =
            Physics.OverlapSphere(
                attackCenter,
                attackRadius,
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

            enemyHealth.TakeDamage(
                currentDamage,
                transform.forward
            );

            hitEnemy = true;
            break;
        }

        if (hitEnemy)
        {
            Debug.Log(
                $"[PlayerAttack] Attack {currentComboStep} hit " +
                $"for {currentDamage} damage."
            );

            StartHitStop();
        }
        else
        {
            Debug.Log(
                $"[PlayerAttack] Attack " +
                $"{currentComboStep} missed."
            );
        }
    }

    private void BeginAttackRecovery()
    {
        currentState = AttackState.Recovery;

        float recoveryDuration =
            GetCurrentRecoveryDuration();

        stateEndTime =
            Time.time + recoveryDuration;

        Debug.Log(
            $"[PlayerAttack] Attack {currentComboStep} recovery. " +
            $"Duration: {recoveryDuration:0.00}s."
        );
    }

    private void ResolveRecoveryEnd()
    {
        if (nextAttackQueued &&
            currentComboStep < MaxComboSteps)
        {
            currentComboStep++;
            nextAttackQueued = false;

            BeginAttackWindup();
            return;
        }

        FinishCombo();
    }

    private void FinishCombo()
    {
        Debug.Log(
            $"[PlayerAttack] Combo finished " +
            $"after attack {currentComboStep}."
        );

        currentState = AttackState.Idle;
        currentComboStep = 0;
        nextAttackQueued = false;
        stateEndTime = 0f;
    }

    private float GetCurrentWindupDuration()
    {
        switch (currentComboStep)
        {
            case 1:
                return firstWindupDuration;

            case 2:
                return secondWindupDuration;

            case 3:
                return thirdWindupDuration;

            default:
                return firstWindupDuration;
        }
    }

    private float GetCurrentRecoveryDuration()
    {
        switch (currentComboStep)
        {
            case 1:
                return firstRecoveryDuration;

            case 2:
                return secondRecoveryDuration;

            case 3:
                return thirdRecoveryDuration;

            default:
                return firstRecoveryDuration;
        }
    }

    private float GetCurrentDamageMultiplier()
    {
        switch (currentComboStep)
        {
            case 1:
                return firstDamageMultiplier;

            case 2:
                return secondDamageMultiplier;

            case 3:
                return thirdDamageMultiplier;

            default:
                return 1f;
        }
    }

    private int GetCurrentAttackDamage()
    {
        float damage =
            attackDamage *
            GetCurrentDamageMultiplier();

        return Mathf.Max(
            1,
            Mathf.RoundToInt(damage)
        );
    }

    private void StartHitStop()
    {
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
            RestoreTimeScale();
        }

        hitStopCoroutine =
            StartCoroutine(HitStopRoutine());
    }

    private IEnumerator HitStopRoutine()
    {
        Time.timeScale =
            hitStopTimeScale;

        Time.fixedDeltaTime =
            normalFixedDeltaTime *
            hitStopTimeScale;

        yield return new WaitForSecondsRealtime(
            hitStopDuration
        );

        RestoreTimeScale();

        hitStopCoroutine = null;
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = 1f;

        Time.fixedDeltaTime =
            normalFixedDeltaTime;
    }

    private void OnDisable()
    {
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
            hitStopCoroutine = null;
        }

        RestoreTimeScale();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 attackCenter =
            transform.position +
            transform.forward * attackRange;

        Gizmos.DrawWireSphere(
            attackCenter,
            attackRadius
        );

        Gizmos.DrawLine(
            transform.position,
            attackCenter
        );
    }
}