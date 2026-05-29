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
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float losePlayerRange = 10f;

    [Header("Attack")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.2f;
    [Header("Role")]
    [SerializeField] private EnemyRole role;
private float lastAttackTime;

    private Rigidbody rb;
    private EnemyState currentState = EnemyState.Idle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        ConfigureByRole();
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
                if (distanceToPlayer <= detectionRange)
                    currentState = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                if (distanceToPlayer <= attackRange)
                    currentState = EnemyState.Attack;
                else if (distanceToPlayer >= losePlayerRange)
                    currentState = EnemyState.Idle;
                break;

            case EnemyState.Attack:
                if (distanceToPlayer > attackRange)
                    currentState = EnemyState.Chase;
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
        Vector3 direction = GetDirectionToPlayer();

        Vector3 newPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
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
                attackRange = 1.3f;
                attackDamage = 8;
                break;

            case EnemyRole.Brute:
                moveSpeed = 2f;
                detectionRange = 6f;
                attackRange = 2f;
                attackDamage = 20;
                break;
        }
    }
    private float GetDistanceToPlayer()
    {
        return Vector3.Distance(transform.position, player.position);
    }

    private Vector3 GetDirectionToPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        return direction.normalized;
    }
    private void TryAttack()
{
    if (Time.time < lastAttackTime + attackCooldown)
        return;

    PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

    if (playerHealth == null)
        return;

    playerHealth.TakeDamage(attackDamage);
    lastAttackTime = Time.time;
}
}