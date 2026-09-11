using UnityEngine;

public enum ItemType
{
    Weapon,
    Shield,
    Armor,
    Consumable,
    Resource
}

/// <summary>
/// One item definition. Gear items point at the existing WeaponData / ShieldData
/// so nothing about combat stats changes. Consumables carry a small timed buff.
/// </summary>
[CreateAssetMenu(fileName = "Item_", menuName = "Project M/Item")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName = "Item";
    [TextArea] public string description;
    public Sprite icon;
    public ItemType type = ItemType.Resource;

    [Header("Stacking / weight")]
    [Min(1)] public int maxStack = 1;
    [Min(0f)] public float weight = 1f;

    [Header("Gear (Weapon / Shield / Armor types)")]
    public WeaponData weaponData;
    public ShieldData shieldData;
    public ArmorData armorData;

    [Header("Consumable buff (Consumable type)")]
    [Tooltip("Extra stamina-regen multiplier while the buff is active (0 = none).")]
    public float staminaRegenBonus = 0f;
    [Tooltip("Health restored per second while the buff is active.")]
    public float healPerSecond = 0f;
    [Tooltip("How long the buff lasts, seconds.")]
    public float buffDuration = 0f;

    public bool Stackable => maxStack > 1;
    public bool IsGear => type == ItemType.Weapon || type == ItemType.Shield || type == ItemType.Armor;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) id = name;
        if (IsGear) maxStack = 1;
        maxStack = Mathf.Max(1, maxStack);
    }
}
