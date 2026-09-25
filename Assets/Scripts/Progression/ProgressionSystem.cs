using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player Progression V1 - the whole model in one sentence: spend a tangible, world-earned
/// resource (Vestige, a plain inventory item - see Assets/Data/Progression) at a
/// ProgressionAltar to permanently unlock ONE concrete new possibility (a recipe, a
/// capability, an accessible location). No XP, no character level, no stat curve - every
/// unlock is a discrete, named, permanent "you can now do X" rather than a number going up.
///
/// Static class, not a MonoBehaviour/manager - same shape as GameSession/ExplorationTracker
/// and for the identical reason (pure bookkeeping, no per-frame behaviour, no scene wiring
/// needed to reach it from anywhere).
///
/// Deliberately does NOT track currency itself - Vestige is a real Inventory item, so "how
/// many do I have" is just inventory.CountOf(vestigeItem), already covered by the existing
/// inventory save/restore. This class only remembers WHICH unlocks have been purchased.
/// </summary>
public static class ProgressionSystem
{
    private static readonly HashSet<string> unlockedIds = new HashSet<string>();

    /// <summary>Fired once, the moment any unlock is purchased (live, or a restored save).</summary>
    public static event System.Action<ProgressionUnlock> Unlocked;

    public static bool HasUnlock(ProgressionUnlock unlock) =>
        unlock != null && unlockedIds.Contains(unlock.UnlockId);

    public static bool HasUnlock(string unlockId) =>
        !string.IsNullOrEmpty(unlockId) && unlockedIds.Contains(unlockId);

    /// <summary>Attempts to buy an unlock: must not already be owned, must be affordable.
    /// On success, pays the cost, records the unlock, fires its onPurchased hook, then the
    /// Unlocked event. Returns whether the purchase happened.</summary>
    public static bool TryPurchase(ProgressionUnlock unlock, Inventory inventory)
    {
        if (unlock == null || inventory == null) return false;
        if (HasUnlock(unlock)) return false;
        if (!unlock.CanAfford(inventory)) return false;

        unlock.Pay(inventory);
        unlockedIds.Add(unlock.UnlockId);

        DevLog.Log($"[Progression] Unlocked: {unlock.displayName}");

        Unlocked?.Invoke(unlock);
        return true;
    }

    /// <summary>All currently-unlocked ids, for the save system to capture.</summary>
    public static IEnumerable<string> UnlockedIds => unlockedIds;

    /// <summary>Silent restore from a save file - no re-firing onPurchased/Unlocked (already
    /// applied in the session that earned it).</summary>
    public static void RestoreUnlocked(string[] ids)
    {
        unlockedIds.Clear();
        if (ids == null) return;

        foreach (var id in ids)
            if (!string.IsNullOrEmpty(id))
                unlockedIds.Add(id);
    }

    /// <summary>Resets all unlocks - called from GameUIController.Start() alongside
    /// GameSession.Reset()/ExplorationTracker.Reset(), same reasoning: static fields survive
    /// a scene reload within the same Play session, and a fresh/new game must not inherit a
    /// prior session's unlocks. A restored save re-populates this right afterward via
    /// GameSaveController's (later-executing) RestoreUnlocked() call.</summary>
    public static void Reset()
    {
        unlockedIds.Clear();
    }
}
