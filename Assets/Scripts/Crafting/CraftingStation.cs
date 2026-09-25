using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A world crafting station (Forge, etc). Walk up, press the interact key,
/// opens CraftingUI listing the recipes this station offers. Builds its own
/// "Press E" prompt exactly like LootChest - no scene UI wiring needed.
/// </summary>
public class CraftingStation : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string stationName = "Crafting Station";
    [SerializeField] private CraftingStationType stationType;
    [Min(1)] [SerializeField] private int stationLevel = 1;

    [Header("Recipes offered here")]
    [SerializeField] private CraftingRecipe[] recipes;

    [Header("Interact")]
    [SerializeField] private float interactRange = 2.6f;
    [SerializeField] private Transform player;

    [Header("Audio")]
    [SerializeField] private AudioClip openSfx;
    [Range(0f, 1f)] [SerializeField] private float openVolume = 0.45f;

    private bool isOpen;
    private bool playerInRange;

    private CanvasGroup promptGroup;
    private TMP_Text promptText;
    private Collider[] solidColliders;

    public string StationName => stationName;
    public CraftingStationType StationType => stationType;
    public int StationLevel => stationLevel;
    public CraftingRecipe[] Recipes => recipes;

    /// <summary>True if at least one recipe here is unlocked and fully affordable right now.</summary>
    public bool HasCraftableRecipe(Inventory inventory)
    {
        if (recipes == null || inventory == null) return false;
        foreach (var r in recipes)
            if (r != null && r.IsUnlocked && r.MatchesStation(this) && r.HasMaterials(inventory))
                return true;
        return false;
    }

    private void Awake()
    {
        if (player == null)
        {
            var pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) player = pm.transform;
        }

        solidColliders = InteractionRange.CollectSolidColliders(this);
        BuildPrompt();
    }

    private void Update()
    {
        if (isOpen || player == null)
        {
            SetPromptVisible(false);
            return;
        }

        // Measured to the nearest collider surface, not the root, so it follows the visuals.
        float dist = InteractionRange.Distance(transform, solidColliders, player.position);
        playerInRange = dist <= interactRange;
        bool blocked = GameUIController.IsPaused || InventoryUI.IsOpen || CraftingUI.IsOpen || ProgressionUI.IsOpen;
        SetPromptVisible(playerInRange && !blocked);

        if (playerInRange && !blocked && Input.GetKeyDown(KeyBindings.Get(GameAction.Interact)))
        {
            OpenUI();
        }
    }

    private void OpenUI()
    {
        var ui = CraftingUI.Instance;
        if (ui == null)
        {
            Debug.LogWarning($"[CraftingStation] {stationName}: no CraftingUI found in scene.");
            return;
        }

        isOpen = true;
        SetPromptVisible(false);
        CombatAudio.Play(openSfx, transform.position, openVolume);
        ui.Open(this, () => isOpen = false);
    }

    // ---- self-contained "Press E" prompt (same pattern as LootChest) -------
    private void BuildPrompt()
    {
        var canvasGO = new GameObject("CraftingStationPrompt");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;
        canvasGO.AddComponent<CanvasScaler>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        var rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -140f);
        rect.sizeDelta = new Vector2(420f, 50f);

        promptText = textGO.AddComponent<TextMeshProUGUI>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 30f;
        promptText.color = UIPalette.Teal;
        promptText.fontStyle = FontStyles.Bold;

        var shadow = textGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        promptGroup = canvasGO.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f;
        promptGroup.blocksRaycasts = false;
        promptGroup.interactable = false;

        RefreshPromptText();
    }

    private void OnEnable()
    {
        KeyBindings.Changed += RefreshPromptText;
    }

    private void OnDisable()
    {
        KeyBindings.Changed -= RefreshPromptText;
    }

    private void RefreshPromptText()
    {
        if (promptText != null) promptText.text = $"Press {KeyBindings.Get(GameAction.Interact)} to craft ({stationName})";
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptGroup == null) return;
        promptGroup.alpha = visible ? 1f : 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(UIPalette.Teal.r, UIPalette.Teal.g, UIPalette.Teal.b, 0.5f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
