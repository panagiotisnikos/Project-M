using UnityEngine;

/// <summary>
/// The one place that knows how to turn live scene state into a SaveData and back.
/// Auto-loads on scene start, auto-saves at natural checkpoints (camp cleared, boss
/// defeated, quitting) - no manual "Save" button needed for v1.
///
/// DefaultExecutionOrder(1000): must run its Start() after every other script's
/// Start() (e.g. PlayerInventory.Start() seeding startingItems) so a restore always
/// has the final word instead of racing a fresh session's default setup.
/// </summary>
[DefaultExecutionOrder(1000)]
public class GameSaveController : MonoBehaviour
{
    [Header("References (auto-found if left empty)")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerPerformanceTracker performanceTracker;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private Camp[] camps;
    [SerializeField] private HearthEmber hearthEmber;

    private bool hasLoadedThisSession;

    private void Awake()
    {
        if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (playerEquipment == null) playerEquipment = FindFirstObjectByType<PlayerEquipment>();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (performanceTracker == null) performanceTracker = FindFirstObjectByType<PlayerPerformanceTracker>();
        if (playerRigidbody == null && playerHealth != null) playerRigidbody = playerHealth.GetComponent<Rigidbody>();
        if (bossHealth == null) bossHealth = FindFirstObjectByType<BossHealth>();
        if (camps == null || camps.Length == 0) camps = FindObjectsByType<Camp>(FindObjectsSortMode.None);
        if (hearthEmber == null) hearthEmber = FindFirstObjectByType<HearthEmber>();
    }

    private void Start()
    {
        LoadGame();

        if (camps != null)
            foreach (var camp in camps)
                if (camp != null)
                    camp.Cleared += OnCheckpoint;

        if (bossHealth != null)
            bossHealth.OnBossDefeated += OnCheckpoint;
    }

    private void OnDestroy()
    {
        if (camps != null)
            foreach (var camp in camps)
                if (camp != null)
                    camp.Cleared -= OnCheckpoint;

        if (bossHealth != null)
            bossHealth.OnBossDefeated -= OnCheckpoint;
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnCheckpoint()
    {
        SaveGame();
    }

    // ------------------------------------------------------------------
    // Save
    // ------------------------------------------------------------------

    public void SaveGame()
    {
        var data = new SaveData();

        if (playerRigidbody != null)
        {
            Vector3 pos = playerRigidbody.position;
            data.posX = pos.x;
            data.posY = pos.y;
            data.posZ = pos.z;
        }

        if (playerHealth != null)
            data.currentHealth = playerHealth.CurrentHealth;

        CaptureInventory(data);
        CaptureEquipment(data);
        CapturePerformance(data);
        CaptureCamps(data);

        data.bossDefeated = bossHealth != null && bossHealth.IsDead;
        data.hasEmberCharge = hearthEmber != null && hearthEmber.HasCharge;

        SaveSystem.Save(data);
    }

    private void CaptureInventory(SaveData data)
    {
        if (playerInventory == null || playerInventory.Inventory == null) return;

        var inv = playerInventory.Inventory;
        data.inventoryItemIds = new string[inv.Capacity];
        data.inventoryCounts = new int[inv.Capacity];

        for (int i = 0; i < inv.Capacity; i++)
        {
            var slot = inv.GetSlot(i);
            data.inventoryItemIds[i] = slot.IsEmpty ? "" : slot.item.id;
            data.inventoryCounts[i] = slot.IsEmpty ? 0 : slot.count;
        }
    }

    private void CaptureEquipment(SaveData data)
    {
        if (playerEquipment == null) return;
        var db = ItemDatabase.Instance;
        if (db == null) return;

        var weaponItem = db.FindByWeapon(playerEquipment.EquippedWeapon);
        var shieldItem = db.FindByShield(playerEquipment.EquippedShield);
        var armorItem = db.FindByArmor(playerEquipment.EquippedArmor);

        data.equippedWeaponItemId = weaponItem != null ? weaponItem.id : "";
        data.equippedShieldItemId = shieldItem != null ? shieldItem.id : "";
        data.equippedArmorItemId = armorItem != null ? armorItem.id : "";
    }

    private void CapturePerformance(SaveData data)
    {
        if (performanceTracker == null) return;

        data.enemiesKilled = performanceTracker.EnemiesKilled;
        data.timeAlive = performanceTracker.TimeAlive;
        data.parriesLanded = performanceTracker.ParriesLanded;
        data.cleanDodges = performanceTracker.CleanDodges;
        data.blocksHeld = performanceTracker.BlocksHeld;
        data.hitsTaken = performanceTracker.HitsTaken;
        data.damageTaken = performanceTracker.DamageTaken;
    }

    private void CaptureCamps(SaveData data)
    {
        if (camps == null) return;

        data.campIds = new string[camps.Length];
        data.campCleared = new bool[camps.Length];

        for (int i = 0; i < camps.Length; i++)
        {
            data.campIds[i] = camps[i] != null ? camps[i].CampId : "";
            data.campCleared[i] = camps[i] != null && camps[i].IsCleared;
        }
    }

    // ------------------------------------------------------------------
    // Load
    // ------------------------------------------------------------------

    public void LoadGame()
    {
        if (hasLoadedThisSession) return;
        hasLoadedThisSession = true;

        SaveData data = SaveSystem.Load();
        if (data == null) return; // fresh game - nothing to restore

        if (playerRigidbody != null)
        {
            Vector3 pos = new Vector3(data.posX, data.posY, data.posZ);
            playerRigidbody.position = pos;
            playerRigidbody.transform.position = pos;
        }

        if (playerHealth != null && data.currentHealth > 0)
            playerHealth.RestoreHealth(data.currentHealth);

        RestoreInventory(data);
        RestoreEquipment(data);

        if (performanceTracker != null)
        {
            performanceTracker.RestoreStats(
                data.enemiesKilled, data.timeAlive, data.parriesLanded,
                data.cleanDodges, data.blocksHeld, data.hitsTaken, data.damageTaken);
        }

        RestoreCamps(data);

        if (data.bossDefeated && bossHealth != null)
            bossHealth.RestoreDefeated();

        if (hearthEmber != null)
            hearthEmber.SetCharge(data.hasEmberCharge);

        Debug.Log("[GameSaveController] Save loaded.");
    }

    private void RestoreInventory(SaveData data)
    {
        if (playerInventory == null || playerInventory.Inventory == null || data.inventoryItemIds == null)
            return;

        var db = ItemDatabase.Instance;
        if (db == null)
        {
            Debug.LogWarning("[GameSaveController] No ItemDatabase in Resources/ - inventory not restored.");
            return;
        }

        var inv = playerInventory.Inventory;
        int count = Mathf.Min(data.inventoryItemIds.Length, inv.Capacity);

        for (int i = 0; i < count; i++)
        {
            string id = data.inventoryItemIds[i];
            ItemData item = string.IsNullOrEmpty(id) ? null : db.FindById(id);
            inv.SetSlot(i, item, data.inventoryCounts[i]);
        }
    }

    private void RestoreEquipment(SaveData data)
    {
        if (playerEquipment == null) return;
        var db = ItemDatabase.Instance;
        if (db == null) return;

        if (!string.IsNullOrEmpty(data.equippedWeaponItemId))
        {
            var item = db.FindById(data.equippedWeaponItemId);
            if (item != null && item.weaponData != null)
                playerEquipment.EquipWeaponItem(item.weaponData);
        }

        if (!string.IsNullOrEmpty(data.equippedShieldItemId))
        {
            var item = db.FindById(data.equippedShieldItemId);
            if (item != null && item.shieldData != null)
                playerEquipment.EquipShieldItem(item.shieldData);
        }

        if (!string.IsNullOrEmpty(data.equippedArmorItemId))
        {
            var item = db.FindById(data.equippedArmorItemId);
            if (item != null && item.armorData != null)
                playerEquipment.EquipArmorItem(item.armorData);
        }
    }

    private void RestoreCamps(SaveData data)
    {
        if (camps == null || data.campIds == null) return;

        for (int i = 0; i < data.campIds.Length; i++)
        {
            if (!data.campCleared[i]) continue;

            Camp camp = FindCampById(data.campIds[i]);
            if (camp == null) continue;

            // An adaptively-spawned camp must be told "don't spawn" BEFORE
            // RestoreCleared(), or its own CampSpawner.RefreshEnemies() would
            // populate fresh enemies and flip IsCleared back to false.
            var spawner = camp.GetComponent<CampSpawner>();
            if (spawner != null) spawner.MarkAlreadySpawned();

            camp.RestoreCleared();
        }
    }

    private Camp FindCampById(string id)
    {
        if (camps == null) return null;
        foreach (var c in camps)
            if (c != null && c.CampId == id)
                return c;
        return null;
    }
}
