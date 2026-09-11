using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The player's carried inventory + the actions on it (equip gear, eat food,
/// drop, auto-pickup, hotbar keys) and the encumbrance check.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int capacity = 24;
    [Tooltip("Carry weight above which dodge is disabled and movement slows.")]
    [SerializeField] private float carryLimit = 200f;
    [Tooltip("Number of hotbar slots (the top row), bound to keys 1..N.")]
    [SerializeField] private int hotbarSlots = 6;
    [Tooltip("Items the player spawns holding (one each).")]
    [SerializeField] private ItemData[] startingItems;

    [Header("Refs")]
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private ItemPickup pickupPrefab;
    [SerializeField] private Transform dropOrigin;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private AudioClip potionSfx;
    [Range(0f, 1f)] [SerializeField] private float pickupVolume = 0.45f;
    [Range(0f, 1f)] [SerializeField] private float potionVolume = 0.55f;

    public Inventory Inventory { get; private set; }
    public float CarryLimit => carryLimit;
    public bool IsOverEncumbered => Inventory != null && Inventory.TotalWeight() > carryLimit;

    /// <summary>Fires with a short line when something is picked up (for a toast).</summary>
    public event System.Action<string> OnPickedUp;

    // --- consumable buffs ---
    private class Buff { public float healPerSec; public float endTime; public float healAccum; }
    private readonly List<Buff> buffs = new List<Buff>();

    private void Awake()
    {
        Inventory = new Inventory(capacity);
        if (playerEquipment == null) playerEquipment = GetComponent<PlayerEquipment>();
        if (playerStamina == null) playerStamina = GetComponent<PlayerStamina>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (dropOrigin == null) dropOrigin = transform;
    }

    private void Start()
    {
        if (startingItems != null)
            foreach (var it in startingItems)
                if (it != null) Inventory.TryAdd(it, 1);
    }

    private void Update()
    {
        TickBuffs();
        ReadHotbar();
    }

    private void ReadHotbar()
    {
        if (GameUIController.IsPaused) return;
        for (int i = 0; i < hotbarSlots && i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                UseSlot(i);
    }

    // ---- pickups ----------------------------------------------------------
    /*
     * OnTriggerStay (not OnTriggerEnter): a pickup can spawn already inside this
     * radius (a chest opened at close range, a close-quarters kill) - Enter would
     * never fire again for it since the player never left+re-entered the trigger.
     * ItemPickup.IsCollectable gates the actual pop-out delay.
     */
    private void OnTriggerStay(Collider other)
    {
        var pickup = other.GetComponentInParent<ItemPickup>();
        if (pickup == null || pickup.Item == null || !pickup.IsCollectable) return;

        int left = Inventory.TryAdd(pickup.Item, pickup.Count);
        int taken = pickup.Count - left;
        if (taken <= 0) return;                 // inventory full - leave it on the ground

        OnPickedUp?.Invoke(taken > 1 ? $"{pickup.Item.displayName}  x{taken}" : pickup.Item.displayName);
        CombatAudio.Play(pickupSfx, transform.position, pickupVolume);

        if (left > 0) pickup.SetCount(left);
        else Destroy(pickup.gameObject);
    }

    // ---- slot actions ---------------------------------------------------
    /// <summary>Right-click / hotbar: equip gear or eat a consumable.</summary>
    public void UseSlot(int index)
    {
        var s = Inventory.GetSlot(index);
        if (s.IsEmpty) return;

        switch (s.item.type)
        {
            case ItemType.Weapon:
                if (s.item.weaponData != null && playerEquipment != null)
                    playerEquipment.EquipWeaponItem(s.item.weaponData);
                break;

            case ItemType.Shield:
                if (s.item.shieldData != null && playerEquipment != null)
                    playerEquipment.EquipShieldItem(s.item.shieldData);
                break;

            case ItemType.Armor:
                if (s.item.armorData != null && playerEquipment != null)
                    playerEquipment.EquipArmorItem(s.item.armorData);
                break;

            case ItemType.Consumable:
                Consume(index, s.item);
                break;
        }
    }

    private void Consume(int index, ItemData item)
    {
        if (playerStamina != null && item.staminaRegenBonus > 0f && item.buffDuration > 0f)
            playerStamina.ApplyRegenBuff(1f + item.staminaRegenBonus, item.buffDuration);

        if (item.healPerSecond > 0f && item.buffDuration > 0f)
            buffs.Add(new Buff { healPerSec = item.healPerSecond, endTime = Time.time + item.buffDuration });
        else if (item.healPerSecond > 0f && playerHealth != null)
            playerHealth.Heal(Mathf.RoundToInt(item.healPerSecond));

        if (item.healPerSecond > 0f)
            CombatAudio.Play(potionSfx, transform.position, potionVolume);

        Inventory.RemoveAt(index, 1);
        OnPickedUp?.Invoke($"Ate {item.displayName}");
    }

    public void DropSlot(int index)
    {
        var s = Inventory.GetSlot(index);
        if (s.IsEmpty || pickupPrefab == null) return;

        Vector3 pos = dropOrigin.position + dropOrigin.forward * 1.2f + Vector3.up * 0.3f;
        var drop = Instantiate(pickupPrefab, pos, Quaternion.identity);
        drop.Configure(s.item, s.count);
        drop.Toss(dropOrigin.forward);

        Inventory.RemoveAt(index, s.count);
    }

    // ---- consumable tick ----------------------------------------------
    private void TickBuffs()
    {
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            var b = buffs[i];
            if (Time.time >= b.endTime) { buffs.RemoveAt(i); continue; }
            if (playerHealth == null) continue;
            b.healAccum += b.healPerSec * Time.deltaTime;
            int whole = Mathf.FloorToInt(b.healAccum);
            if (whole > 0) { playerHealth.Heal(whole); b.healAccum -= whole; }
        }
    }
}
