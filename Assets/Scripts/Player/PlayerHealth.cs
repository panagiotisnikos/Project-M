using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        Debug.Log($"Player Health: {currentHealth}/{maxHealth}");
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player died.");
    }
}