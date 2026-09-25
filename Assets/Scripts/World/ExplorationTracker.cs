using System.Collections.Generic;

/// <summary>
/// Aggregate exploration bookkeeping - counts, not a checklist. Deliberately NOT a map,
/// waypoint list, or quest log (explicitly out of scope for this framework) - just enough
/// signal for other systems (HUD text, debug, a future adaptive-world hook) to react to
/// "how much has the player found/finished". Same static-class-not-a-manager shape as
/// GameSession, for the same reason (pure bookkeeping, no per-frame behaviour).
///
/// Registration is idempotent (keyed on PointOfInterest.PoiId) - a POI re-registering
/// itself (e.g. a save restore calling RegisterDiscovery again) never double-counts.
/// </summary>
public static class ExplorationTracker
{
    public static int TotalDiscovered { get; private set; }
    public static int TotalCompleted { get; private set; }

    private static readonly Dictionary<PointOfInterest.POIType, int> discoveredByType = new();
    private static readonly HashSet<string> discoveredIds = new();
    private static readonly HashSet<string> completedIds = new();

    /// <summary>Fired whenever any POI is discovered - a hook for HUD/debug/audio, not a map.</summary>
    public static event System.Action<PointOfInterest> Discovered;
    /// <summary>Fired whenever any POI is completed.</summary>
    public static event System.Action<PointOfInterest> Completed;

    public static void RegisterDiscovery(PointOfInterest poi)
    {
        if (poi == null || !discoveredIds.Add(poi.PoiId))
            return;

        TotalDiscovered++;
        discoveredByType[poi.Type] = discoveredByType.TryGetValue(poi.Type, out var count) ? count + 1 : 1;
        Discovered?.Invoke(poi);
    }

    public static void RegisterCompletion(PointOfInterest poi)
    {
        if (poi == null || !completedIds.Add(poi.PoiId))
            return;

        TotalCompleted++;
        Completed?.Invoke(poi);
    }

    public static int GetDiscoveredCount(PointOfInterest.POIType type) =>
        discoveredByType.TryGetValue(type, out var count) ? count : 0;

    /// <summary>Resets all counts - called from GameUIController.Start() alongside
    /// GameSession.Reset(), for the same reason: static fields survive a scene reload
    /// within the same Play session, and a fresh/new game must not inherit a prior
    /// session's discovery counts. A restored save re-populates these right afterward via
    /// GameSaveController's (later-executing) PointOfInterest.RestoreState() calls.</summary>
    public static void Reset()
    {
        TotalDiscovered = 0;
        TotalCompleted = 0;
        discoveredByType.Clear();
        discoveredIds.Clear();
        completedIds.Clear();
    }
}
