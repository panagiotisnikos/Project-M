using UnityEngine;

public class WorldAdaptationManager : MonoBehaviour
{
    public enum WorldState
    {
        Stable,
        Balanced,
        Decaying
    }

    [SerializeField] private PlayerPerformanceTracker performanceTracker;

    // Defensive-mastery signal (see PlayerPerformanceTracker):
    //   below stableThreshold  -> the land stays dormant / forgiving
    //   above decayingThreshold -> the land has "woken" to a skilled player and turns
    [Header("Adaptation Thresholds")]
    [SerializeField] private float stableThreshold = -5f;
    [SerializeField] private float decayingThreshold = 20f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private float debugLogInterval = 2f;

    [Header("Current State")]
    [SerializeField] private WorldState currentState = WorldState.Balanced;

    private float nextDebugLogTime;

    public WorldState CurrentState => currentState;
    public float StableThreshold => stableThreshold;
    public float DecayingThreshold => decayingThreshold;

    private void Update()
    {
        if (performanceTracker == null)
            return;

        float score = performanceTracker.GetPerformanceScore();

        UpdateWorldState(score);
        //LogDebugInfo(score);
    }

    private void UpdateWorldState(float score)
    {
        if (score <= stableThreshold)
            SetWorldState(WorldState.Stable, score);
        else if (score >= decayingThreshold)
            SetWorldState(WorldState.Decaying, score);
        else
            SetWorldState(WorldState.Balanced, score);
    }

    private void SetWorldState(WorldState newState, float score)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        DevLog.Log($"[WorldAdaptation] STATE CHANGED → {currentState} | Score: {score}");
    }

    private void LogDebugInfo(float score)
    {
        if (!showDebugLogs)
            return;

        if (Time.time < nextDebugLogTime)
            return;

        nextDebugLogTime = Time.time + debugLogInterval;

        DevLog.Log($"[WorldAdaptation] Score: {score} | State: {currentState}");
    }
}