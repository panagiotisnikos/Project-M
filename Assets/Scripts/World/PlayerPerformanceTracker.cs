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
        //Debug.Log($"Total damage taken: {damageTaken}");
    }
    public void StopTracking()
    {
        isTracking = false;
        Debug.Log("Performance tracking stopped.");
    }
    public float GetPerformanceScore()
    {
        float killScore = enemiesKilled * 8f;
        float damagePenalty = damageTaken * 0.35f;
        float timePenalty = timeAlive * 0.05f;

        return killScore - damagePenalty - timePenalty;
    }
    public string GetPerformanceReport()
    {
        return
            $"Kills: {enemiesKilled}\n" +
            $"Damage Taken: {damageTaken}\n" +
            $"Time Alive: {timeAlive:0.0}s\n" +
            $"Performance Score: {GetPerformanceScore():0.0}";
    }
}