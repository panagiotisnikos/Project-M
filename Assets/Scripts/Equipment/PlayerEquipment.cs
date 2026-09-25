using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("Currently Equipped")]
    [SerializeField] private WeaponData equippedWeapon;
    [SerializeField] private ShieldData equippedShield;
    [SerializeField] private ArmorData equippedArmor;

    [Header("Debug Loadout Switching")]
    [SerializeField] private bool enableDebugLoadoutSwitching = true;

    [SerializeField] private KeyCode firstLoadoutKey =
        KeyCode.Alpha1;

    [SerializeField] private WeaponData firstLoadoutWeapon;
    [SerializeField] private ShieldData firstLoadoutShield;

    [SerializeField] private KeyCode secondLoadoutKey =
        KeyCode.Alpha2;

    [SerializeField] private WeaponData secondLoadoutWeapon;
    [SerializeField] private ShieldData secondLoadoutShield;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAttack playerAttack;

    public WeaponData EquippedWeapon =>
        equippedWeapon;

    public ShieldData EquippedShield =>
        equippedShield;

    public ArmorData EquippedArmor =>
        equippedArmor;

    public bool HasWeapon =>
        equippedWeapon != null;

    public bool HasShield =>
        equippedShield != null;

    public bool HasArmor =>
        equippedArmor != null;

    /// <summary>Fired when a loadout is equipped via the debug switch. Payload: "Weapon + Shield".</summary>
    public event System.Action<string> OnLoadoutEquipped;

    /// <summary>Fired whenever the equipped armor changes, so PlayerArmorVisuals can refresh.</summary>
    public event System.Action<ArmorData> OnArmorEquipped;

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        if (playerAttack == null)
        {
            playerAttack =
                GetComponent<PlayerAttack>();
        }
    }

    private void Start()
    {
        LogCurrentLoadout(
            "Starting loadout"
        );
    }

    private void Update()
    {
        if (!enableDebugLoadoutSwitching)
            return;

        if (Input.GetKeyDown(firstLoadoutKey))
        {
            TryEquipDebugLoadout(
                firstLoadoutWeapon,
                firstLoadoutShield,
                "Loadout 1"
            );
        }
        else if (Input.GetKeyDown(secondLoadoutKey))
        {
            TryEquipDebugLoadout(
                secondLoadoutWeapon,
                secondLoadoutShield,
                "Loadout 2"
            );
        }
    }

    public void EquipWeapon(
        WeaponData weapon)
    {
        equippedWeapon =
            weapon;

        if (weapon != null)
        {
            DevLog.Log(
                $"[PlayerEquipment] Equipped weapon: " +
                $"{weapon.WeaponName}."
            );
        }
        else
        {
            DevLog.Log(
                "[PlayerEquipment] Weapon unequipped."
            );
        }
    }

    public void EquipShield(
        ShieldData shield)
    {
        equippedShield =
            shield;

        if (shield != null)
        {
            DevLog.Log(
                $"[PlayerEquipment] Equipped shield: " +
                $"{shield.ShieldName}."
            );
        }
        else
        {
            DevLog.Log(
                "[PlayerEquipment] Shield unequipped."
            );
        }
    }

    public void EquipLoadout(
        WeaponData weapon,
        ShieldData shield)
    {
        equippedWeapon =
            weapon;

        equippedShield =
            shield;

        LogCurrentLoadout(
            "Equipped loadout"
        );
    }

    /// <summary>
    /// Equip a single piece from the inventory. Sets the field and fires
    /// OnLoadoutEquipped so PlayerWeaponVisuals and the HUD update, exactly like
    /// the old loadout swap did.
    /// </summary>
    public void EquipWeaponItem(WeaponData weapon)
    {
        equippedWeapon = weapon;
        NotifyLoadoutChanged();
    }

    public void EquipShieldItem(ShieldData shield)
    {
        equippedShield = shield;
        NotifyLoadoutChanged();
    }

    /// <summary>Equip an armor piece from the inventory. Cosmetic tier swap + damage reduction.</summary>
    public void EquipArmorItem(ArmorData armor)
    {
        equippedArmor = armor;
        DevLog.Log(
            $"[PlayerEquipment] Equipped armor: " +
            $"{(armor != null ? armor.ArmorName : "None")}."
        );
        OnArmorEquipped?.Invoke(equippedArmor);
    }

    private void NotifyLoadoutChanged()
    {
        string w = equippedWeapon != null ? equippedWeapon.WeaponName : "Unarmed";
        string s = equippedShield != null ? equippedShield.ShieldName : "No Shield";
        LogCurrentLoadout("Equipped");
        OnLoadoutEquipped?.Invoke($"{w}  +  {s}");
    }

    public void UnequipWeapon()
    {
        EquipWeapon(null);
    }

    public void UnequipShield()
    {
        EquipShield(null);
    }

    private void TryEquipDebugLoadout(
        WeaponData weapon,
        ShieldData shield,
        string loadoutName)
    {
        if (!CanSwitchLoadout())
        {
            DevLog.Log(
                $"[PlayerEquipment] {loadoutName} switch " +
                "ignored during a combat action."
            );

            return;
        }

        if (weapon == null ||
            shield == null)
        {
            Debug.LogWarning(
                $"[PlayerEquipment] {loadoutName} is " +
                "not fully assigned."
            );

            return;
        }

        equippedWeapon =
            weapon;

        equippedShield =
            shield;

        DevLog.Log(
            $"[PlayerEquipment] {loadoutName} equipped: " +
            $"{weapon.WeaponName} + " +
            $"{shield.ShieldName}."
        );

        OnLoadoutEquipped?.Invoke(
            $"{weapon.WeaponName}  +  {shield.ShieldName}"
        );
    }

    private bool CanSwitchLoadout()
    {
        if (playerAttack != null &&
            playerAttack.IsAttacking)
        {
            return false;
        }

        if (playerMovement != null &&
            (playerMovement.IsDodging ||
             playerMovement.IsBlocking))
        {
            return false;
        }

        return true;
    }

    private void LogCurrentLoadout(
        string prefix)
    {
        string weaponName =
            equippedWeapon != null
                ? equippedWeapon.WeaponName
                : "None";

        string shieldName =
            equippedShield != null
                ? equippedShield.ShieldName
                : "None";

        DevLog.Log(
            $"[PlayerEquipment] {prefix}: " +
            $"{weaponName} + {shieldName}."
        );
    }
}