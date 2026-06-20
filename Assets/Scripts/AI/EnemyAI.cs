using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Chase,
        Attack
    }

[Header("References")]
    [SerializeField] private Transform player;

[Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 12f;

[Header("Ranges")]
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float losePlayerRange = 10f;

[Header("Attack")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.2f;
[Header("Role")]
    [SerializeField] private EnemyRole role;
[Header("Adaptation")]
[SerializeField] private WorldAdaptationManager worldAdaptationManager;
private float lastAttackTime;

    private Rigidbody rb;
    private EnemyState currentState = EnemyState.Idle;
    [SerializeField] private float stoppingDistance = 2.6f;
    [SerializeField] private float resumeChaseDistance = 3.2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (player == null)
        {
            PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();

            if (playerMovement != null)
            {
                player = playerMovement.transform;
            }
        }

        if (worldAdaptationManager == null)
        {
            worldAdaptationManager = FindFirstObjectByType<WorldAdaptationManager>();
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
                    currentState = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                if (distanceToPlayer <= stoppingDistance)
                {
                    currentState = EnemyState.Attack;
                }
                else if (distanceToPlayer >= GetAdaptedLosePlayerRange())
                {
                    currentState = EnemyState.Idle;
                }
                break;

            case EnemyState.Attack:
                rb.linearVelocity = Vector3.zero;
                FacePlayer();

                if (distanceToPlayer > resumeChaseDistance)
                {
                    currentState = EnemyState.Chase;
                }
                break;
        }
    }
    public enum EnemyRole
    {
        Stalker,
        Brute
    }
    private void RunState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                // Do nothing for now.
                break;

            case EnemyState.Chase:
                ChasePlayer();
                break;

            case EnemyState.Attack:
                FacePlayer();
                TryAttack();
                break;
        }
    }

    private void ChasePlayer()
    {
        if (GetDistanceToPlayer() <= stoppingDistance)
        {
            rb.linearVelocity = Vector3.zero;
            FacePlayer();
            return;
        }

        Vector3 direction = GetDirectionToPlayer();

        Vector3 newPosition = rb.position + direction * GetAdaptedMoveSpeed() * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        RotateTowards(direction);
    }

    private void FacePlayer()
    {
        Vector3 direction = GetDirectionToPlayer();
        RotateTowards(direction);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction == Vector3.zero)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Quaternion smoothRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(smoothRotation);
    }
    private void ConfigureByRole()
    {
        switch (role)
        {
            case EnemyRole.Stalker:
                moveSpeed = 5f;
                detectionRange = 9f;
                attackDamage = 8;
                break;

            case EnemyRole.Brute:
                moveSpeed = 2f;
                detectionRange = 6f;
                attackDamage = 20;
                break;
        }
    }
    private float GetDistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        return Vector3.Distance(transform.position, player.position);
    }

    private Vector3 GetDirectionToPlayer()
    {
        if (player == null)
            return Vector3.zero;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        return direction.normalized;
    }
    private void TryAttack()
    {
        if (player == null)
            return;

       if (Time.time < lastAttackTime + GetAdaptedAttackCooldown())
            return;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsDead)
            return;

        Vector3 hitDirection = (player.position - transform.position).normalized;
        playerHealth.TakeDamage(attackDamage, hitDirection);
        lastAttackTime = Time.time;
    }
    public void SetWorldAdaptationManager(WorldAdaptationManager manager)
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

    private float GetAdaptedAttackCooldown()
    {
        return attackCooldown * GetAttackCooldownModifier();
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