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
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactRange = 2.6f;
    [SerializeField] private Transform player;

    private bool isOpen;
    private bool playerInRange;

    private CanvasGroup promptGroup;
    private TMP_Text promptText;

    public string StationName => stationName;
    public CraftingStationType StationType => stationType;
    public int StationLevel => stationLevel;
    public CraftingRecipe[] Recipes => recipes;

    private void Awake()
    {
        if (player == null)
        {
            var pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) player = pm.transform;
        }

        BuildPrompt();
    }

    private void Update()
    {
        if (isOpen || player == null)
        {
            SetPromptVisible(false);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        playerInRange = dist <= interactRange;
        bool blocked = GameUIController.IsPaused || InventoryUI.IsOpen || CraftingUI.IsOpen;
        SetPromptVisible(playerInRange && !blocked);

        if (playerInRange && !blocked && Input.GetKeyDown(interactKey))
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
        promptText.text = $"Press E to craft ({stationName})";
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 30f;
        promptText.color = new Color(0.95f, 0.66f, 0.28f);
        promptText.fontStyle = FontStyles.Bold;

        var shadow = textGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        promptGroup = canvasGO.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f;
        promptGroup.blocksRaycasts = false;
        promptGroup.interactable = false;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptGroup == null) return;
        promptGroup.alpha = visible ? 1f : 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.95f, 0.66f, 0.28f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
