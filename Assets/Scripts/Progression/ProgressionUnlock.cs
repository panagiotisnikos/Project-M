using UnityEngine;

/// <summary>
/// One permanent, one-time unlock - Project M's whole progression model. Pure identity +
/// cost; deliberately does NOT know what it unlocks. Every consumer polls
/// ProgressionSystem.HasUnlock(this) directly - a recipe checking whether it's available
/// (CraftingRecipe.IsUnlocked), a POI checking whether it's accessible (its requiredUnlock
/// field), HearthEmber checking whether its capacity bonus applies. Deliberately NOT an
/// event-driven "onPurchased" hook - tried that first, found a real gotcha (see this
/// session's memory): a UnityEvent persistent listener wired from a ScriptableObject asset
/// to a scene object does not survive the Editor's Edit-to-Play domain reload (the target
/// reference silently nulls). Polling is simpler and provably correct instead.
///
/// No XP, no levels, no stat curve - see ProgressionSystem for why. Cost reuses
/// CraftingRecipe.Ingredient directly (same shape, zero duplication) - spending Vestiges
/// to unlock something is mechanically identical to spending materials to craft something,
/// it just doesn't hand back an inventory item.
/// </summary>
[CreateAssetMenu(fileName = "Unlock_", menuName = "Project M/Progression/Unlock")]
public class ProgressionUnlock : ScriptableObject
{
    /// <summary>Purely descriptive/grouping for the Progression UI - drives no logic.</summary>
    public enum Category
    {
        Crafting,
        Preparation,
        Access
    }

    [Header("Identity")]
    [Tooltip("Stable id used by the save system and by HasUnlock() lookups. Defaults to the asset name if left blank.")]
    [SerializeField] private string unlockId = "";
    public string displayName = "Unlock";
    [TextArea] public string description = "";
    public Category category = Category.Crafting;

    [Header("Cost")]
    public CraftingRecipe.Ingredient[] cost = new CraftingRecipe.Ingredient[0];

    public string UnlockId => string.IsNullOrEmpty(unlockId) ? name : unlockId;

    public bool CanAfford(Inventory inventory)
    {
        if (inventory == null || cost == null) return false;
        foreach (var c in cost)
        {
            if (c.item == null) continue;
            if (inventory.CountOf(c.item) < c.quantity) return false;
        }
        return true;
    }

    public void Pay(Inventory inventory)
    {
        if (inventory == null || cost == null) return;
        foreach (var c in cost)
        {
            if (c.item == null) continue;
            inventory.Remove(c.item, c.quantity);
        }
    }
}
