using UnityEngine;

/// <summary>
/// Every ItemData asset in the game, in one place, loaded via Resources so the
/// save system can turn a saved string id back into an ItemData reference without
/// any scene wiring. Populate `allItems` with every Item_*.asset when a new one
/// is added - nothing else needs to know about the new item.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Project M/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private ItemData[] allItems;

    private static ItemDatabase instance;

    /// <summary>Lazy-loaded from Assets/Resources/ItemDatabase.asset.</summary>
    public static ItemDatabase Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<ItemDatabase>("ItemDatabase");
            return instance;
        }
    }

    public ItemData FindById(string id)
    {
        if (string.IsNullOrEmpty(id) || allItems == null)
            return null;

        foreach (var item in allItems)
            if (item != null && item.id == id)
                return item;

        return null;
    }

    public ItemData FindByWeapon(WeaponData weapon)
    {
        if (weapon == null || allItems == null) return null;
        foreach (var item in allItems)
            if (item != null && item.weaponData == weapon)
                return item;
        return null;
    }

    public ItemData FindByShield(ShieldData shield)
    {
        if (shield == null || allItems == null) return null;
        foreach (var item in allItems)
            if (item != null && item.shieldData == shield)
                return item;
        return null;
    }

    public ItemData FindByArmor(ArmorData armor)
    {
        if (armor == null || allItems == null) return null;
        foreach (var item in allItems)
            if (item != null && item.armorData == armor)
                return item;
        return null;
    }
}
