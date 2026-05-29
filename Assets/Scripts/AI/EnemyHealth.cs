using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private EnemyAI.EnemyRole role;
    private int currentHealth;

    private void Awake()
    {
        ConfigureHealth();
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} took {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void ConfigureHealth()
    {
        switch (role)
        {
            case EnemyAI.EnemyRole.Stalker:
                maxHealth = 20;
                break;

            case EnemyAI.EnemyRole.Brute:
                maxHealth = 60;
                break;
        }
    }
    private void Die()
    {
        Debug.Log($"{gameObject.name} died.");
        Destroy(gameObject);
    }
}