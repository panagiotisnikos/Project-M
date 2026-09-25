using UnityEngine;

/// <summary>
/// One craftable recipe: materials in, item out, optionally gated by a
/// station type/level. Pure data + pure logic (no MonoBehaviour dependency),
/// mirroring RewardTable.Roll() - CraftingStation/CraftingUI just call
/// HasMaterials/Consume, the recipe asset owns the rules.
/// </summary>
[CreateAssetMenu(fileName = "Recipe_", menuName = "Project M/Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [System.Serializable]
    public struct Ingredient
    {
        public ItemData item;
        [Min(1)] public int quantity;
    }

    [Header("Output")]
    public ItemData output;
    [Min(1)] public int outputQuantity = 1;

    [Header("Ingredients")]
    public Ingredient[] ingredients;

    [Header("Station requirement (optional)")]
    [Tooltip("Leave empty to allow crafting at any station.")]
    public CraftingStationType requiredStationType;
    [Min(1)] public int requiredStationLevel = 1;

    [Header("Future: upgrade gate (not enforced yet)")]
    [Tooltip("Placeholder for a future requirement such as 'must already own/equip this item' " +
             "(e.g. crafting an upgraded weapon from its base version). Not read by V1 - reserved " +
             "so recipe assets authored now don't need to be redone when upgrades are implemented.")]
    public ItemData requiredUpgradeBase;

    [Header("Progression gate (optional)")]
    [Tooltip("If assigned, this recipe doesn't appear until the player has purchased this " +
             "ProgressionUnlock - the 'crafting capability' progression category. Leave empty " +
             "for a recipe available from the start.")]
    public ProgressionUnlock requiredUnlock;

    /// <summary>False only while a Progression gate is assigned and not yet purchased.</summary>
    public bool IsUnlocked => requiredUnlock == null || ProgressionSystem.HasUnlock(requiredUnlock);

    public bool MatchesStation(CraftingStation station)
    {
        if (station == null) return requiredStationType == null;
        if (requiredStationType != null && station.StationType != requiredStationType) return false;
        return station.StationLevel >= requiredStationLevel;
    }

    public bool HasMaterials(Inventory inventory)
    {
        if (inventory == null || ingredients == null) return false;
        foreach (var ing in ingredients)
        {
            if (ing.item == null) continue;
            if (inventory.CountOf(ing.item) < ing.quantity) return false;
        }
        return true;
    }

    public void Consume(Inventory inventory)
    {
        if (inventory == null || ingredients == null) return;
        foreach (var ing in ingredients)
        {
            if (ing.item == null) continue;
            inventory.Remove(ing.item, ing.quantity);
        }
    }
}
