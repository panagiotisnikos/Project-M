using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackRadius = 0.7f;
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private LayerMask enemyLayer;

    private float lastAttackTime;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        Vector3 attackCenter = transform.position + transform.forward * attackRange;

        Collider[] hits = Physics.OverlapSphere(
            attackCenter,
            attackRadius,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
                break;
            }
        }

        lastAttackTime = Time.time;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 attackCenter = transform.position + transform.forward * attackRange;
        Gizmos.DrawWireSphere(attackCenter, attackRadius);
    }
}