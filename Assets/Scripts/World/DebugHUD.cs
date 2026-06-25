using UnityEngine;

public class DebugHUD : MonoBehaviour
{
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;
    [SerializeField] private DemoObjectiveManager demoObjectiveManager;
    [SerializeField] private BossController bossController;
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private CampSpawner campSpawner;

    private void OnGUI()
    {
        if (enemyAI != null)
        {
            GUI.Label(new Rect(25, 180, 290, 20), "Enemy Adaptation:");
            GUI.Label(new Rect(25, 200, 290, 20), $"Move: {enemyAI.GetMoveSpeedModifier():0.00}x");
            GUI.Label(new Rect(25, 220, 290, 20), $"Detection: {enemyAI.GetDetectionModifier():0.00}x");
            GUI.Label(new Rect(25, 240, 290, 20), $"Attack Cooldown: {enemyAI.GetAttackCooldownModifier():0.00}x");
        }
        if (performanceTracker == null || worldAdaptationManager == null)
            return;

        float score = performanceTracker.GetPerformanceScore();

        GUI.Box(new Rect(10, 10, 340, 270), "Adaptive Debug");

        GUI.Label(new Rect(25, 40, 240, 20), $"World State: {worldAdaptationManager.CurrentState}");
        GUI.Label(new Rect(25, 60, 240, 20), $"Score: {score:F1}");
        GUI.Label(new Rect(25, 80, 240, 20), $"Kills: {performanceTracker.EnemiesKilled}");
        GUI.Label(new Rect(25, 100, 240, 20), $"Damage: {performanceTracker.DamageTaken}");

        if (demoObjectiveManager != null &&
            demoObjectiveManager.CurrentObjective == "Demo complete" &&
            performanceTracker != null)
        {
            GUI.Box(new Rect(360, 150, 320, 170), "Final Performance Report");

            GUI.Label(new Rect(375, 180, 290, 20),
                $"Kills: {performanceTracker.EnemiesKilled}");

            GUI.Label(new Rect(375, 200, 290, 20),
                $"Damage Taken: {performanceTracker.DamageTaken}");

            GUI.Label(new Rect(375, 220, 290, 20),
                $"Time Alive: {performanceTracker.TimeAlive:0.0}s");

            GUI.Label(new Rect(375, 240, 290, 20),
                $"Score: {performanceTracker.GetPerformanceScore():0.0}");

            GUI.Label(new Rect(375, 260, 290, 20),
                $"Final State: {worldAdaptationManager.CurrentState}");

            if (bossController != null)
            {
                GUI.Label(new Rect(375, 280, 290, 20),
                    $"Boss Profile: {bossController.CurrentProfile}");
            }
        }
        if (campSpawner != null)
        {
            GUI.Box(new Rect(360, 10, 320, 130), "Adaptive Decision");

            if (!campSpawner.HasSpawned)
            {
                GUI.Label(new Rect(375, 40, 290, 20), "Adaptive Camp: Waiting");
                GUI.Label(new Rect(375, 60, 290, 20), "Reason: Clear first camp");
            }
            else
            {
                GUI.Label(new Rect(375, 40, 290, 20), $"State: {campSpawner.LastGeneratedState}");
                GUI.Label(new Rect(375, 60, 290, 20), $"Score: {campSpawner.LastScore:0.0}");
                GUI.Label(new Rect(375, 80, 290, 20), $"Generated: {campSpawner.LastComposition}");
                GUI.Label(new Rect(375, 100, 290, 20), "Reason: kills, damage, time");
            }
        }
            }
}