using UnityEngine;

/// <summary>
/// Tunable weights/thresholds for one or more WorldRegions, authored as an
/// asset so a designer can build a new adaptive region entirely in the
/// Inspector - no code. Mirrors PlayerPerformanceTracker's signal shape
/// (defensive mastery: parries/dodges/blocks push up, hits/damage push down)
/// so a region's local state is legible in exactly the same terms as the
/// existing global one. Share one profile across many regions, or give each
/// region its own for a harsher/gentler local personality.
/// </summary>
[CreateAssetMenu(fileName = "RegionProfile_", menuName = "Project M/World/Region Adaptation Profile")]
public class RegionAdaptationProfile : ScriptableObject
{
    [Header("Signal weights (mirrors PlayerPerformanceTracker)")]
    public float parryWeight = 3f;
    public float dodgeWeight = 1.5f;
    public float blockWeight = 0.5f;
    public float hitPenalty = 2.5f;
    public float damagePenalty = 0.15f;

    [Header("State thresholds")]
    [Tooltip("Committed score at/below this -> Decayed.")]
    public float decayedThreshold = -5f;
    [Tooltip("Committed score at/above this -> Blossom. Between the two thresholds is Balanced.")]
    public float blossomThreshold = 20f;

    [Header("Anti-farming")]
    [Tooltip("A single continuous visit can move the region's score by at most this " +
             "much (either direction) - stops a player parking in a corner and " +
             "grinding one encounter endlessly to force a state in one sitting. " +
             "Leaving and re-entering opens a fresh budget, so the state can still " +
             "genuinely evolve over repeated play, just not be forced instantly.")]
    public float maxScoreChangePerVisit = 12f;

    [Tooltip("State is only re-evaluated on this cadence while the player is inside, " +
             "not every frame - a pattern of behaviour over time, not an instant toggle.")]
    public float recalcInterval = 4f;

    private void OnValidate()
    {
        maxScoreChangePerVisit = Mathf.Max(0f, maxScoreChangePerVisit);
        recalcInterval = Mathf.Max(0.5f, recalcInterval);
    }
}
