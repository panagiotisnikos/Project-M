using UnityEngine;

public class DebugHUD : MonoBehaviour
{
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;
    [SerializeField] private DemoObjectiveManager demoObjectiveManager;

    private void OnGUI()
    {
        if (performanceTracker == null || worldAdaptationManager == null)
            return;

        float score = performanceTracker.GetPerformanceScore();

        GUI.Box(new Rect(10, 10, 320, 170), "Adaptive Debug");

        GUI.Label(new Rect(25, 40, 240, 20), $"World State: {worldAdaptationManager.CurrentState}");
        GUI.Label(new Rect(25, 60, 240, 20), $"Score: {score:F1}");
        GUI.Label(new Rect(25, 80, 240, 20), $"Kills: {performanceTracker.EnemiesKilled}");
        GUI.Label(new Rect(25, 100, 240, 20), $"Damage: {performanceTracker.DamageTaken}");

       if (demoObjectiveManager != null)
        {
            GUI.Label(new Rect(25, 120, 290, 20), $"Objective: {demoObjectiveManager.CurrentObjective}");
            GUI.Label(new Rect(25, 140, 290, 20), demoObjectiveManager.StatusMessage);
        }
    }
}