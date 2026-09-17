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

    // Boss (single boss in the slice - one flag is enough for now).
    public bool bossDefeated;

    // Refuge - the HearthEmber charge (see RefugeZone/HearthEmber) so a banked
    // "second chance" survives a quit/reload instead of being re-earned for free.
    public bool hasEmberCharge;
}
