using UnityEngine;

public class DemoObjectiveManager : MonoBehaviour
{
    [SerializeField] private Camp requiredCamp;
    [SerializeField] private BossCombat bossCombat;
    [SerializeField] private PlayerPerformanceTracker performanceTracker;

    public string CurrentObjective { get; private set; } = "Clear the camp";

    private bool demoCompleted;
    public bool IsDemoCompleted => demoCompleted;

    public string StatusMessage { get; private set; } = "";

    /// <summary>Called by BossHealth when the boss is defeated. Ends the slice.</summary>
    public void RegisterBossDefeated()
    {
        if (demoCompleted)
            return;

        demoCompleted = true;

        if (performanceTracker != null)
        {
            performanceTracker.StopTracking();
        }

        Debug.Log("[DemoObjective] Boss defeated. Vertical slice complete.");
        CurrentObjective = "Boss defeated";
        StatusMessage = "The boss is dead. Slice complete.";
    }

    private void Update()
    {
        if (demoCompleted)
            return;

        if (bossCombat != null && bossCombat.FightActive)
        {
            CurrentObjective = "Defeat the boss";
        }
        else if (requiredCamp != null && requiredCamp.IsCleared)
        {
            CurrentObjective = "Enter boss arena";
        }
        else
        {
            CurrentObjective = "Clear the camp";
        }
    }

    public void ShowStatus(string message)
    {
        StatusMessage = message;
    }
}
