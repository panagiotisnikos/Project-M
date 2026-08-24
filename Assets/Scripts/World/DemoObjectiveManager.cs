using UnityEngine;

public class DemoObjectiveManager : MonoBehaviour
{
    [SerializeField] private Camp requiredCamp;
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    public string CurrentObjective { get; private set; } = "Clear the camp";
    private bool demoCompleted;
    public bool IsDemoCompleted =>
    demoCompleted;
    public string StatusMessage { get; private set; } = "";

    public void TryCompleteDemo()
    {
        if (demoCompleted)
            return;

        if (requiredCamp != null && !requiredCamp.IsCleared)
        {
            Debug.Log("[DemoObjective] Boss arena reached, but required camp is not cleared yet.");
            CurrentObjective = "Clear the camp";
            StatusMessage = "Boss arena locked. Clear the camp first.";
            return;
        }

        demoCompleted = true;

        if (performanceTracker != null)
        {
            performanceTracker.StopTracking();
        }

        Debug.Log("[DemoObjective] Demo completed. Camp cleared and adaptive boss state demonstrated.");
        CurrentObjective = "Demo complete";
        StatusMessage = "Demo completed successfully.";
    }
    private void Update()
    {
    if (demoCompleted)
        return;

    if (requiredCamp != null && requiredCamp.IsCleared)
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