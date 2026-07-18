using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("Currently Equipped")]
    [SerializeField] private WeaponData equippedWeapon;
    [SerializeField] private ShieldData equippedShield;

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

    public bool HasWeapon =>
        equippedWeapon != null;

    public bool HasShield =>
        equippedShield != null;

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
            Debug.Log(
                $"[PlayerEquipment] Equipped weapon: " +
                $"{weapon.WeaponName}."
            );
        }
        else
        {
            Debug.Log(
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
            Debug.Log(
                $"[PlayerEquipment] Equipped shield: " +
                $"{shield.ShieldName}."
            );
        }
        else
        {
            Debug.Log(
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
            Debug.Log(
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

        Debug.Log(
            $"[PlayerEquipment] {loadoutName} equipped: " +
            $"{weapon.WeaponName} + " +
            $"{shield.ShieldName}."
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

        Debug.Log(
            $"[PlayerEquipment] {prefix}: " +
            $"{weaponName} + {shieldName}."
        );
    }
}