using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a Forge / Attunement Shrine readable at a glance, without adding a quest
/// marker or tutorial pop-up:
///
///  - A floating name + one-line purpose above the object, fading in as the player
///    approaches ("FORGE - craft gear from gathered materials").
///  - When something becomes affordable there (a recipe you have the materials for,
///    an unlock you have the Vestige for), a short line appears under the name and
///    the station's own light breathes brighter - the world hinting, not a UI badge.
///  - The moment that first becomes true, a single quiet line fades in and out near
///    the bottom of the screen, so the player learns it even from across the map.
///
/// Sits next to a CraftingStation or ProgressionAltar and asks it "is anything
/// available?" - no recipe/unlock logic lives here.
/// </summary>
public class StationHint : MonoBehaviour
{
    [Header("Label")]
    [SerializeField] private string title = "Forge";
    [SerializeField] private string purpose = "Craft gear from gathered materials";
    [SerializeField] private string readyLine = "Something here can be crafted";
    [SerializeField] private TMP_FontAsset titleFont;
    [Tooltip("Soft dark backing behind the label so it stays legible over bright scenery.")]
    [SerializeField] private Sprite backing;
    [SerializeField] private float labelRange = 9f;
    [SerializeField] private float labelHeightOffset = 0.45f;

    [Header("Nudge (shown once each time something becomes available)")]
    [SerializeField] private string nudgeMessage = "Your materials could be worked at the Forge.";

    [Header("World cue")]
    [Tooltip("Optional light that breathes brighter while something is available.")]
    [SerializeField] private Light readyLight;
    [SerializeField] private float readyIntensityBoost = 0.8f;
    [SerializeField] private float pulseSpeed = 2.2f;


    private CraftingStation station;
    private ProgressionAltar altar;
    private PlayerInventory inventory;
    private Transform player;
    private Camera cam;

    private Canvas canvas;
    private CanvasGroup labelGroup;
    private RectTransform labelRoot;
    private TMP_Text readyText;
    private Vector3 labelAnchor;

    private bool available;
    private bool polledOnce;
    private float nextPollTime;
    private float baseLightIntensity;

    // The Shrine speaks in violet (magic / Vestige), the Forge in lichen + teal - see UIPalette.
    private Color TitleColor => altar != null ? UIPalette.Violet : UIPalette.Lichen;
    private Color AccentColor => altar != null ? UIPalette.Violet : UIPalette.Teal;

    private void Awake()
    {
        station = GetComponent<CraftingStation>();
        altar = GetComponent<ProgressionAltar>();
        inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory != null) player = inventory.transform;
        if (readyLight != null) baseLightIntensity = readyLight.intensity;

        BuildLabel();
    }

    private void Start()
    {
        // After every renderer is placed/enabled - label sits just above the visuals.
        labelAnchor = InteractionRange.VisualTop(this) + Vector3.up * labelHeightOffset;
    }

    private void Update()
    {
        if (Time.time >= nextPollTime)
        {
            nextPollTime = Time.time + 0.5f;
            PollAvailability();
        }

        UpdateLight();
        UpdateLabel();
    }

    // ------------------------------------------------------------------

    private void PollAvailability()
    {
        var inv = inventory != null ? inventory.Inventory : null;
        bool now = station != null ? station.HasCraftableRecipe(inv)
                 : altar != null && altar.HasAffordableUnlock(inv);

        // Nudge only on the transition to "available", and never on the very first poll
        // of a scene (a restored/started state isn't news), nor while already standing here.
        if (now && !available && polledOnce && !PlayerIsNear(labelRange * 0.5f))
            HintToast.Show(nudgeMessage);

        available = now;
        polledOnce = true;

        if (readyText != null) readyText.gameObject.SetActive(available);
    }

    private void UpdateLight()
    {
        if (readyLight == null) return;

        float target = baseLightIntensity;
        if (available)
            target *= 1f + readyIntensityBoost * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));

        readyLight.intensity = Mathf.Lerp(readyLight.intensity, target, Time.deltaTime * 4f);
    }

    private void UpdateLabel()
    {
        if (labelGroup == null) return;
        if (cam == null) cam = Camera.main;

        bool blocked = GameUIController.IsPaused || InventoryUI.IsOpen || CraftingUI.IsOpen || ProgressionUI.IsOpen;
        float alpha = 0f;

        if (!blocked && cam != null && player != null)
        {
            Vector3 screen = cam.WorldToScreenPoint(labelAnchor);
            if (screen.z > 0f)
            {
                float dist = Vector3.Distance(player.position, labelAnchor);
                // Full strength within 60% of the range, fading out to nothing at the edge.
                alpha = 1f - Mathf.InverseLerp(labelRange * 0.6f, labelRange, dist);
                labelRoot.position = screen;
            }
        }

        if (available && readyText != null)
            readyText.alpha = 0.65f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));

        canvas.scaleFactor = GameSettings.UIScale * Screen.height / 1080f;
        labelGroup.alpha = Mathf.MoveTowards(labelGroup.alpha, alpha, Time.unscaledDeltaTime * 3f);
    }

    private bool PlayerIsNear(float range)
    {
        return player != null && Vector3.Distance(player.position, labelAnchor) <= range;
    }

    // ------------------------------------------------------------------

    private void BuildLabel()
    {
        var canvasGO = new GameObject("StationLabel");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // under the "Press E" prompts (350)

        labelGroup = canvasGO.AddComponent<CanvasGroup>();
        labelGroup.alpha = 0f;
        labelGroup.blocksRaycasts = false;
        labelGroup.interactable = false;

        var root = new GameObject("Label", typeof(RectTransform));
        root.transform.SetParent(canvasGO.transform, false);
        labelRoot = (RectTransform)root.transform;
        labelRoot.anchorMin = labelRoot.anchorMax = Vector2.zero;
        labelRoot.pivot = new Vector2(0.5f, 0f);
        labelRoot.sizeDelta = new Vector2(420f, 90f);

        if (backing != null)
        {
            var bg = new GameObject("Backing", typeof(RectTransform));
            bg.transform.SetParent(root.transform, false);
            var bgRect = (RectTransform)bg.transform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(-60f, -26f);
            bgRect.offsetMax = new Vector2(60f, 16f);
            var img = bg.AddComponent<Image>();
            img.sprite = backing;
            img.color = new Color(0f, 0f, 0f, 0.8f);
            img.raycastTarget = false;
            bg.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        var layout = root.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 0f;

        var titleText = AddLine(root.transform, title.ToUpperInvariant(), 24f, TitleColor, titleFont);
        titleText.characterSpacing = 6f;
        AddLine(root.transform, purpose, 16f, UIPalette.Parchment, null);
        readyText = AddLine(root.transform, readyLine, 15f, AccentColor, null);
        readyText.fontStyle = FontStyles.Italic;
        readyText.gameObject.SetActive(false);
    }

    private static TMP_Text AddLine(Transform parent, string text, float size, Color color, TMP_FontAsset font)
    {
        var go = new GameObject("Line", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Bottom;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }
}
