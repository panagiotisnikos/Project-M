using UnityEngine;

/// <summary>
/// Tracks how the player performs during gameplay.
/// This is the "intelligence input" of the adaptive system.
/// Other systems can use these values to make the game react.
/// </summary>
public class PlayerPerformanceTracker : MonoBehaviour
{
    [Header("Performance Data")]
    [SerializeField] private int enemiesKilled;
    [SerializeField] private int damageTaken;
    [SerializeField] private float timeAlive;

    private bool isTracking = true;

    public int EnemiesKilled => enemiesKilled;
    public int DamageTaken => damageTaken;
    public float TimeAlive => timeAlive;

    private void Update()
    {
        if (!isTracking)
            return;
        timeAlive += Time.deltaTime;
    }

    public void RegisterEnemyKilled()
    {
        if (!isTracking)
            return;
        enemiesKilled++;
        Debug.Log($"Enemies killed: {enemiesKilled}");
    }

    public void RegisterDamageTaken(int damage)
    {
        if (!isTracking)
            return;
        damageTaken += damage;
        Debug.Log($"Total damage taken: {damageTaken}");
    }
    public void StopTracking()
    {
        isTracking = false;
        Debug.Log("Performance tracking stopped. Player is dead.");
    }
    public float GetPerformanceScore()
    {
        /*
         * Simple rule-based performance score:
         * - Killing enemies increases score.
         * - Taking damage decreases score.
         */
        float killScore = enemiesKilled * 10f;
        float damagePenalty = damageTaken * 0.5f;

        return killScore - damagePenalty;
    }
}