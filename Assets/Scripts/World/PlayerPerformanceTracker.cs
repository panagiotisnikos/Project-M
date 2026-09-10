using UnityEngine;

/// <summary>
/// Tracks how the player performs during gameplay - the "intelligence input" of
/// the adaptive system.
///
/// The adaptive signal is a DEFENSIVE MASTERY read: parries, clean dodges and
/// blocks push it up; getting hit pushes it down. No kill count, no time-alive
/// (both were exploitable / punished exploration). Kills and time are still
/// tracked for the end-of-run report only.
/// </summary>
public class PlayerPerformanceTracker : MonoBehaviour
{
    [Header("Run stats (report only)")]
    [SerializeField] private int enemiesKilled;
    [SerializeField] private float timeAlive;

    [Header("Defensive mastery inputs")]
    [SerializeField] private int parriesLanded;
    [SerializeField] private int cleanDodges;
    [SerializeField] private int blocksHeld;
    [SerializeField] private int hitsTaken;
    [SerializeField] private int damageTaken;

    [Header("Signal weights")]
    [SerializeField] private float parryWeight = 3f;
    [SerializeField] private float dodgeWeight = 1.5f;
    [SerializeField] private float blockWeight = 0.5f;
    [SerializeField] private float hitPenalty = 2.5f;
    [SerializeField] private float damagePenalty = 0.15f;

    private bool isTracking = true;

    public int EnemiesKilled => enemiesKilled;
    public int DamageTaken => damageTaken;
    public float TimeAlive => timeAlive;
    public int ParriesLanded => parriesLanded;
    public int CleanDodges => cleanDodges;
    public int HitsTaken => hitsTaken;

    private void Update()
    {
        if (isTracking) timeAlive += Time.deltaTime;
    }

    public void RegisterEnemyKilled()
    {
        if (!isTracking) return;
        enemiesKilled++;
    }

    public void RegisterParry()
    {
        if (!isTracking) return;
        parriesLanded++;
    }

    public void RegisterCleanDodge()
    {
        if (!isTracking) return;
        cleanDodges++;
    }

    public void RegisterBlock()
    {
        if (!isTracking) return;
        blocksHeld++;
    }

    public void RegisterDamageTaken(int damage)
    {
        if (!isTracking) return;
        damageTaken += damage;
        hitsTaken++;
    }

    public void StopTracking()
    {
        isTracking = false;
    }

    /// <summary>
    /// The defensive-mastery signal. Higher = the player is fighting cleanly and
    /// in control; the world "notices" and turns. Lower = the player is getting
    /// worn down; the land stays quiet.
    /// </summary>
    public float GetPerformanceScore()
    {
        return parriesLanded * parryWeight
             + cleanDodges * dodgeWeight
             + blocksHeld * blockWeight
             - hitsTaken * hitPenalty
             - damageTaken * damagePenalty;
    }

    public string GetPerformanceReport()
    {
        return
            $"Parries: {parriesLanded}\n" +
            $"Clean Dodges: {cleanDodges}\n" +
            $"Hits Taken: {hitsTaken}\n" +
            $"Damage Taken: {damageTaken}\n" +
            $"Mastery Signal: {GetPerformanceScore():0.0}";
    }
}
