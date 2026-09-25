using System;

/// <summary>
/// Everything the save system persists, as one flat JSON-friendly blob
/// (JsonUtility - no nested reference types, primitives and arrays only).
/// Bump `version` if the shape ever changes in a way old saves can't read into.
/// </summary>
[Serializable]
public class SaveData
{
    public int version = 1;

    // Player
    public float posX, posY, posZ;
    public int currentHealth;

    // Inventory - parallel arrays, one entry per slot. Empty slot = "" / 0.
    public string[] inventoryItemIds = Array.Empty<string>();
    public int[] inventoryCounts = Array.Empty<int>();

    // Equipped gear, by the wrapping ItemData's id ("" = nothing equipped).
    public string equippedWeaponItemId = "";
    public string equippedShieldItemId = "";
    public string equippedArmorItemId = "";

    // Performance tracker (report stats + the defensive-mastery signal inputs -
    // restoring these lets WorldAdaptationManager re-derive the correct world
    // state on its own, no need to save the state itself).
    public int enemiesKilled;
    public float timeAlive;
    public int parriesLanded;
    public int cleanDodges;
    public int blocksHeld;
    public int hitsTaken;
    public int damageTaken;

    // Camps - parallel arrays keyed by Camp.CampId.
    public string[] campIds = Array.Empty<string>();
    public bool[] campCleared = Array.Empty<bool>();

    // Points of Interest - parallel arrays keyed by PointOfInterest.PoiId.
    public string[] poiIds = Array.Empty<string>();
    public bool[] poiDiscovered = Array.Empty<bool>();
    public bool[] poiCompleted = Array.Empty<bool>();

    // Boss (single boss in the slice - one flag is enough for now).
    public bool bossDefeated;

    // Refuge - the HearthEmber charge count (see RefugeZone/HearthEmber) so banked
    // "second chances" survive a quit/reload instead of being re-earned for free.
    public int emberCharges;

    // Progression - which ProgressionUnlocks have been purchased. Vestige (the currency
    // they're bought with) needs no separate field - it's a plain inventory item, already
    // covered by inventoryItemIds/inventoryCounts above.
    public string[] unlockedProgressionIds = Array.Empty<string>();
}
