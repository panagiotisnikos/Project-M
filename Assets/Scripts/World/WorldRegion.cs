using System;
using UnityEngine;

/// <summary>
/// One region's local adaptation state - the reusable foundation for moving the
/// world from a single global toggle (WorldAdaptationManager, left completely
/// intact and still driving the legacy atmosphere) toward many independently
/// reacting areas.
///
/// A region tallies the same kind of defensive-mastery events
/// PlayerPerformanceTracker already does, scoped to whoever is standing inside
/// its trigger volume. PlayerPerformanceTracker forwards its existing
/// RegisterParry/RegisterCleanDodge/RegisterBlock/RegisterDamageTaken calls
/// here automatically (see that class) - no combat script needs to know
/// regions exist.
///
/// Anti-farming: the region's committed score persists across visits, but a
/// single continuous visit can only move it by up to
/// RegionAdaptationProfile.maxScoreChangePerVisit. Leaving and re-entering
/// opens a fresh budget, so the state genuinely evolves over repeated play
/// instead of being forced by grinding one encounter in a single sitting.
/// State is only re-evaluated every recalcInterval seconds, not every frame.
///
/// To build a new adaptive region yourself: add this component (a trigger
/// Collider is required - Reset() sets it up for you) to any GameObject,
/// assign a RegionAdaptationProfile, and give it a RegionVisualAdapter (or
/// your own listener) subscribed to StateChanged. See Camp_AdaptiveSecond in
/// Prototype_01 for a complete worked example (enemy config + reward +
/// visuals all driven from one region).
/// </summary>
[RequireComponent(typeof(Collider))]
public class WorldRegion : MonoBehaviour
{
    [Tooltip("Stable id for save/debug. Defaults to the GameObject name if left blank.")]
    [SerializeField] private string regionId = "";
    [SerializeField] private RegionAdaptationProfile profile;
    [SerializeField] private RegionWorldState currentState = RegionWorldState.Balanced;

    /// <summary>
    /// Whichever region the player is currently standing inside, or null.
    /// Regions are assumed not to overlap. PlayerPerformanceTracker reads this
    /// to forward its existing Register* calls without any combat script
    /// needing a direct reference to a region.
    /// </summary>
    public static WorldRegion ActiveRegion { get; private set; }

    public string RegionId => string.IsNullOrEmpty(regionId) ? gameObject.name : regionId;
    public RegionWorldState CurrentState => currentState;
    public bool IsPlayerInside { get; private set; }
    public float CommittedScore => committedScore;

    /// <summary>Fired whenever this region's committed state changes.</summary>
    public event Action<WorldRegion, RegionWorldState> StateChanged;

    private float committedScore;
    private float visitContribution;
    private float nextRecalcTime;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        IsPlayerInside = true;
        visitContribution = 0f;
        ActiveRegion = this;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        IsPlayerInside = false;

        if (ActiveRegion == this)
            ActiveRegion = null;
    }

    private void Update()
    {
        if (!IsPlayerInside || profile == null)
            return;

        if (Time.time < nextRecalcTime)
            return;

        nextRecalcTime = Time.time + profile.recalcInterval;
        Recalculate();
    }

    public void RegisterParry() =>
        AddContribution(profile != null ? profile.parryWeight : 0f);

    public void RegisterCleanDodge() =>
        AddContribution(profile != null ? profile.dodgeWeight : 0f);

    public void RegisterBlock() =>
        AddContribution(profile != null ? profile.blockWeight : 0f);

    public void RegisterDamageTaken(int damage)
    {
        if (profile == null)
            return;

        AddContribution(-(profile.hitPenalty + damage * profile.damagePenalty));
    }

    /// <summary>
    /// Applies a signed score delta, clamped so this visit's total contribution
    /// never exceeds the profile's per-visit budget. This is the actual
    /// anti-farming mechanism - once a visit has spent its budget, further
    /// events (in either direction) do nothing until the player leaves and
    /// re-enters.
    /// </summary>
    private void AddContribution(float delta)
    {
        if (!IsPlayerInside || profile == null || delta == 0f)
            return;

        float room = profile.maxScoreChangePerVisit - Mathf.Abs(visitContribution);
        if (room <= 0f)
            return;

        float applied = Mathf.Clamp(delta, -room, room);
        visitContribution += applied;
        committedScore += applied;
    }

    private void Recalculate()
    {
        RegionWorldState next =
            committedScore <= profile.decayedThreshold ? RegionWorldState.Decayed :
            committedScore >= profile.blossomThreshold ? RegionWorldState.Blossom :
            RegionWorldState.Balanced;

        if (next == currentState)
            return;

        currentState = next;

        DevLog.Log($"[WorldRegion:{RegionId}] state -> {currentState} (score {committedScore:0.0})");

        StateChanged?.Invoke(this, currentState);
    }
}
