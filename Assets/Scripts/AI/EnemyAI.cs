using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyRole
    {
        Stalker,
        Brute
    }

    private enum EnemyState
    {
        Idle,
        Chase,
        AttackWindup,
        AttackRecovery,
        HitReact,
        Staggered
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;

    [Header("Role")]
    [SerializeField] private EnemyRole role;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Ranges")]
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float losePlayerRange = 10f;
    [SerializeField] private float stoppingDistance = 2.6f;
    [SerializeField] private float resumeChaseDistance = 3.2f;
    [SerializeField] private float attackHitRange = 3f;

    [Header("Attack Timing")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackWindupDuration = 0.6f;
    [SerializeField] private float attackRecoveryDuration = 0.8f;

    [Header("Attack Telegraph")]
    [SerializeField] private Color attackTelegraphColor = Color.yellow;

    [Header("Stagger")]
    [SerializeField] private Color staggerColor = Color.cyan;

private float staggerEndTime;
    private Rigidbody rb;
    private Renderer enemyRenderer;

    private EnemyState currentState = EnemyState.Idle;
    private float stateEndTime;

    private Color originalColor;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyRenderer = GetComponentInChildren<Renderer>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        ConfigureByRole();

        if (player == null)
        {
            PlayerMovement playerMovement =
                FindFirstObjectByType<PlayerMovement>();

            if (playerMovement != null)
            {
                player = playerMovement.transform;
            }
        }

        if (worldAdaptationManager == null)
        {
            worldAdaptationManager =
                FindFirstObjectByType<WorldAdaptationManager>();
        }
    }

    private void Update()
    {
        UpdateState();
    }

    private void FixedUpdate()
    {
        RunState();
    }

    private void UpdateState()
    {
        float distanceToPlayer = GetDistanceToPlayer();

        switch (currentState)
        {
            case EnemyState.Idle:

                if (distanceToPlayer <= GetAdaptedDetectionRange())
                {
                    ChangeState(EnemyState.Chase);
                }

                break;

            case EnemyState.Chase:

                if (distanceToPlayer >= GetAdaptedLosePlayerRange())
                {
                    ChangeState(EnemyState.Idle);
                }
                else if (distanceToPlayer <= stoppingDistance)
                {
                    BeginAttackWindup();
                }

                break;

            case EnemyState.AttackWindup:

                if (Time.time >= stateEndTime)
                {
                    PerformAttack();
                    BeginAttackRecovery();
                }

                break;

            case EnemyState.AttackRecovery:

                if (Time.time >= stateEndTime)
                {
                    if (distanceToPlayer <= stoppingDistance)
                    {
                        BeginAttackWindup();
                    }
                    else
                    {
                        ChangeState(EnemyState.Chase);
                    }
                }

                break;
            case EnemyState.HitReact:

                if (Time.time >= stateEndTime)
                {
                    if (distanceToPlayer <= GetAdaptedLosePlayerRange())
                    {
                        ChangeState(EnemyState.Chase);
                    }
                    else
                    {
                        ChangeState(EnemyState.Idle);
                    }
                }

                break;                

            case EnemyState.Staggered:

                if (Time.time >= staggerEndTime)
                {
                    SetStaggerVisual(false);

                    if (distanceToPlayer <= GetAdaptedLosePlayerRange())
                    {
                        ChangeState(EnemyState.Chase);
                    }
                    else
                    {
                        ChangeState(EnemyState.Idle);
                    }
                }

                break;
        }
    }

    private void RunState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:

                StopMoving();
                break;

            case EnemyState.Chase:

                ChasePlayer();
                break;

            case EnemyState.AttackWindup:
            case EnemyState.AttackRecovery:

                StopMoving();
                FacePlayer();
                break;

            case EnemyState.HitReact:
            case EnemyState.Staggered:

                StopMoving();
                break;
                    }
    }

    private void BeginAttackWindup()
    {
        ChangeState(EnemyState.AttackWindup);

        float adaptedWindupDuration =
            attackWindupDuration * GetAttackCooldownModifier();

        stateEndTime = Time.time + adaptedWindupDuration;

        SetTelegraphVisual(true);

        Debug.Log(
            $"[{gameObject.name}] Attack wind-up started. " +
            $"Hit in {adaptedWindupDuration:0.00}s."
        );
    }

    private void PerformAttack()
    {
        SetTelegraphVisual(false);

        if (player == null)
            return;

        float distanceToPlayer = GetDistanceToPlayer();

        if (distanceToPlayer > attackHitRange)
        {
            Debug.Log($"[{gameObject.name}] Attack missed.");
            return;
        }

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsDead)
            return;

        Vector3 hitDirection =
            player.position - transform.position;

        hitDirection.y = 0f;
        hitDirection.Normalize();

        playerHealth.TakeDamage(
            attackDamage,
            hitDirection,
            this
        );

        Debug.Log(
            $"[{gameObject.name}] Attack hit for {attackDamage} damage."
        );
    }

    private void BeginAttackRecovery()
    {
        ChangeState(EnemyState.AttackRecovery);

        float adaptedRecoveryDuration =
            attackRecoveryDuration * GetAttackCooldownModifier();

        stateEndTime = Time.time + adaptedRecoveryDuration;
    }

    private void ChasePlayer()
    {
        if (GetDistanceToPlayer() <= stoppingDistance)
        {
            StopMoving();
            FacePlayer();
            return;
        }

        Vector3 direction = GetDirectionToPlayer();

        Vector3 newPosition =
            rb.position +
            direction *
            GetAdaptedMoveSpeed() *
            Time.fixedDeltaTime;

        rb.MovePosition(newPosition);

        RotateTowards(direction);
    }

    private void FacePlayer()
    {
        RotateTowards(GetDirectionToPlayer());
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction == Vector3.zero)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        Quaternion smoothRotation =
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );

        rb.MoveRotation(smoothRotation);
    }

    private void StopMoving()
    {
        rb.linearVelocity = Vector3.zero;
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
    }

    private void SetTelegraphVisual(bool isActive)
    {
        if (enemyRenderer == null)
            return;

        enemyRenderer.material.color =
            isActive ? attackTelegraphColor : originalColor;
    }
    private void SetStaggerVisual(bool isActive)
    {
        if (enemyRenderer == null)
            return;

        enemyRenderer.material.color =
            isActive ? staggerColor : originalColor;
    }
    private void ConfigureByRole()
    {
        switch (role)
        {
            case EnemyRole.Stalker:

                moveSpeed = 5f;
                detectionRange = 9f;
                attackDamage = 8;

                attackWindupDuration = 0.4f;
                attackRecoveryDuration = 0.6f;
                break;

            case EnemyRole.Brute:

                moveSpeed = 2f;
                detectionRange = 6f;
                attackDamage = 20;

                attackWindupDuration = 0.9f;
                attackRecoveryDuration = 1.1f;
                break;
        }
    }

    private float GetDistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        return Vector3.Distance(
            transform.position,
            player.position
        );
    }

    private Vector3 GetDirectionToPlayer()
    {
        if (player == null)
            return Vector3.zero;

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        return direction.normalized;
    }
    public void HitReact(float duration)
    {
        if (currentState == EnemyState.Staggered)
            return;

        SetTelegraphVisual(false);

        currentState = EnemyState.HitReact;
        stateEndTime = Time.time + duration;

        StopMoving();

        Debug.Log(
            $"[{gameObject.name}] Hit reaction " +
            $"for {duration:0.00}s."
        );
    }
    public void Stagger(float duration)
    {
        if (currentState == EnemyState.Staggered)
            return;

        SetTelegraphVisual(false);

        currentState = EnemyState.Staggered;
        staggerEndTime = Time.time + duration;

        StopMoving();
        SetStaggerVisual(true);

        Debug.Log(
            $"[{gameObject.name}] STAGGERED " +
            $"for {duration:0.00}s."
        );
    }
    public void SetWorldAdaptationManager(
        WorldAdaptationManager manager)
    {
        worldAdaptationManager = manager;
    }

    public float GetMoveSpeedModifier()
    {
        if (worldAdaptationManager == null)
            return 1f;

        switch (worldAdaptationManager.CurrentState)
        {
            case WorldAdaptationManager.WorldState.Stable:
                return 0.75f;

            case WorldAdaptationManager.WorldState.Decaying:
                return 1.35f;

            default:
                return 1f;
        }
    }

    public float GetDetectionModifier()
    {
        if (worldAdaptationManager == null)
            return 1f;

        switch (worldAdaptationManager.CurrentState)
        {
            case WorldAdaptationManager.WorldState.Stable:
                return 0.8f;

            case WorldAdaptationManager.WorldState.Decaying:
                return 1.3f;

            default:
                return 1f;
        }
    }

    public float GetAttackCooldownModifier()
    {
        if (worldAdaptationManager == null)
            return 1f;

        switch (worldAdaptationManager.CurrentState)
        {
            case WorldAdaptationManager.WorldState.Stable:
                return 1.4f;

            case WorldAdaptationManager.WorldState.Decaying:
                return 0.7f;

            default:
                return 1f;
        }
    }

    private float GetAdaptedMoveSpeed()
    {
        return moveSpeed * GetMoveSpeedModifier();
    }

    private float GetAdaptedDetectionRange()
    {
        return detectionRange * GetDetectionModifier();
    }

    private float GetAdaptedLosePlayerRange()
    {
        return losePlayerRange * GetDetectionModifier();
    }
}