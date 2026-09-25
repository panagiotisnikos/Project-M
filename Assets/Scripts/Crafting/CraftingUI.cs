using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The crafting panel - lists whichever recipes the open CraftingStation
/// offers, with each ingredient colored green/red by whether the player has
/// enough, and a Craft button that's only clickable when every ingredient is
/// satisfied. Builds itself at runtime the same way OptionsMenuUI does (same
/// sprite kit, same helper pattern) so it needs no scene UI wiring - drop one
/// instance in the scene and every CraftingStation shares it via Instance.
/// </summary>
public class CraftingUI : MonoBehaviour
{
    public static CraftingUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [SerializeField] private float panelWidth = 620f;

    [Header("Audio")]
    [SerializeField] private AudioClip craftSuccessSfx;
    [Range(0f, 1f)] [SerializeField] private float craftSuccessVolume = 0.5f;

    private RectTransform panel;
    private GameObject canvasGo;
    private RectTransform contentRt;
    private TMP_Text headerText;
    private Action onClose;
    private CraftingStation currentStation;
    private PlayerInventory playerInventory;

    private static readonly Color StoneTint = new Color(0.62f, 0.63f, 0.68f, 1f);
    private static readonly Color Parch = new Color(0.87f, 0.83f, 0.74f);
    private static readonly Color ParchDim = new Color(0.60f, 0.57f, 0.50f);
    private static readonly Color Title = UIPalette.Lichen;
    private static readonly Color Accent = UIPalette.Teal;
    private static readonly Color Good = new Color(0.55f, 0.85f, 0.45f);
    private static readonly Color Bad = new Color(0.85f, 0.35f, 0.30f);

    private TMP_FontAsset anton;
    private const float RowHeight = 92f;
    private const float SidePad = 26f;

    private void Awake()
    {
        Instance = this;
        anton = LoadFont();
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        Build();
        gameObject.SetActive(true);
        canvasGo.SetActive(false);
    }

    public void Open(CraftingStation station, Action closeCallback)
    {
        currentStation = station;
        onClose = closeCallback;
        headerText.text = station != null ? station.StationName.ToUpperInvariant() : "CRAFTING";
        canvasGo.SetActive(true);
        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshRecipeList();
    }

    public void Close()
    {
        canvasGo.SetActive(false);
        IsOpen = false;
        currentStation = null;
        onClose?.Invoke();
        onClose = null;
    }

    private void Update()
    {
        if (canvasGo != null && canvasGo.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    // ---------------------------------------------------------------- build
    private static TMP_FontAsset LoadFont()
    {
        var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");
#if UNITY_EDITOR
        if (f == null) f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Anton SDF.asset");
#endif
        return f;
    }

    private Sprite Spr(string n)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/UI/" + n + ".png");
#else
        return null;
#endif
    }

    private void Build()
    {
        var slate = Spr("ui_slate");
        var frame = Spr("ui_frame");
        var buttonSpr = Spr("ui_button");
        var buttonHover = Spr("ui_button_hover");

        canvasGo = new GameObject("CraftingCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 420;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<UIClickSound>();

        var scrim = NewRect("Scrim", canvasGo.transform, Vector2.zero, Vector2.one);
        scrim.offsetMin = Vector2.zero; scrim.offsetMax = Vector2.zero;
        var scrimImg = scrim.gameObject.AddComponent<Image>();
        scrimImg.color = new Color(0f, 0f, 0f, 0.55f);

        panel = NewRect("Panel", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        panel.sizeDelta = new Vector2(panelWidth, 640f);
        panel.anchoredPosition = Vector2.zero;
        var pImg = panel.gameObject.AddComponent<Image>();
        pImg.sprite = slate; pImg.type = Image.Type.Sliced; pImg.color = StoneTint;

        var fr = NewRect("Frame", panel, Vector2.zero, Vector2.one);
        fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
        var frImg = fr.gameObject.AddComponent<Image>();
        frImg.sprite = frame; frImg.type = Image.Type.Sliced; frImg.raycastTarget = false;

        headerText = NewText("Header", panel, "CRAFTING", 24, Title, TextAlignmentOptions.TopLeft);
        headerText.rectTransform.anchorMin = new Vector2(0f, 1f); headerText.rectTransform.anchorMax = new Vector2(1f, 1f);
        headerText.rectTransform.pivot = new Vector2(0f, 1f);
        headerText.rectTransform.anchoredPosition = new Vector2(SidePad, -16f);
        headerText.rectTransform.sizeDelta = new Vector2(panelWidth - SidePad * 2f, 32f);
        headerText.characterSpacing = 8f; headerText.fontStyle = FontStyles.UpperCase;

        var scrollRt = NewRect("Body", panel, new Vector2(0f, 0f), new Vector2(1f, 1f));
        scrollRt.offsetMin = new Vector2(0f, 64f);
        scrollRt.offsetMax = new Vector2(0f, -58f);
        var scrollRect = scrollRt.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        var viewportRt = NewRect("Viewport", scrollRt, Vector2.zero, Vector2.one);
        viewportRt.offsetMin = Vector2.zero; viewportRt.offsetMax = Vector2.zero;
        viewportRt.gameObject.AddComponent<RectMask2D>();
        contentRt = NewRect("Content", viewportRt, new Vector2(0f, 1f), new Vector2(1f, 1f));
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 900f);
        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;

        var backRt = NewRect("BackButton", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        backRt.pivot = new Vector2(0.5f, 0f);
        backRt.sizeDelta = new Vector2(180f, 40f);
        backRt.anchoredPosition = new Vector2(0f, 16f);
        var backImg = backRt.gameObject.AddComponent<Image>();
        backImg.sprite = buttonSpr; backImg.type = Image.Type.Sliced;
        var backBtn = backRt.gameObject.AddComponent<Button>();
        backBtn.targetGraphic = backImg;
        SetSpriteState(backBtn, buttonSpr, buttonHover);
        backBtn.onClick.AddListener(Close);
        var backLabel = NewText("Label", backRt, "CLOSE", 16, Parch, TextAlignmentOptions.Center);
        backLabel.raycastTarget = false;
        if (anton != null) backLabel.font = anton;
        backLabel.fontStyle = FontStyles.UpperCase; backLabel.characterSpacing = 4f;
    }

    // ------------------------------------------------------------ recipe list
    private void RefreshRecipeList()
    {
        for (int i = contentRt.childCount - 1; i >= 0; i--)
            Destroy(contentRt.GetChild(i).gameObject);

        float cursorY = 0f;

        if (currentStation != null && currentStation.Recipes != null)
        {
            foreach (var recipe in currentStation.Recipes)
            {
                if (recipe == null || recipe.output == null) continue;
                if (!recipe.MatchesStation(currentStation)) continue;
                if (!recipe.IsUnlocked) continue;
                AddRecipeRow(recipe, cursorY);
                cursorY += RowHeight + 8f;
            }
        }

        if (cursorY == 0f)
        {
            var empty = NewText("Empty", contentRt, "No recipes available here.", 16, ParchDim, TextAlignmentOptions.TopLeft);
            empty.rectTransform.anchorMin = new Vector2(0f, 1f); empty.rectTransform.anchorMax = new Vector2(1f, 1f);
            empty.rectTransform.pivot = new Vector2(0f, 1f);
            empty.rectTransform.anchoredPosition = new Vector2(SidePad, 0f);
            empty.rectTransform.sizeDelta = new Vector2(-SidePad * 2f, 28f);
            cursorY += 28f;
        }

        contentRt.sizeDelta = new Vector2(0f, cursorY);
    }

    private void AddRecipeRow(CraftingRecipe recipe, float cursorY)
    {
        var rowRt = NewRect("Recipe_" + recipe.name, contentRt, new Vector2(0f, 1f), new Vector2(1f, 1f));
        rowRt.pivot = new Vector2(0f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, -cursorY);
        rowRt.sizeDelta = new Vector2(0f, RowHeight);

        var bgImg = rowRt.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.18f);
        bgImg.raycastTarget = false;

        var iconRt = NewRect("Icon", rowRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(SidePad, 0f);
        iconRt.sizeDelta = new Vector2(56f, 56f);
        var iconImg = iconRt.gameObject.AddComponent<Image>();
        iconImg.sprite = recipe.output.icon;
        iconImg.preserveAspect = true;
        iconImg.color = recipe.output.icon != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
        iconImg.raycastTarget = false;

        var nameText = NewText("Name", rowRt, $"{recipe.output.displayName} x{recipe.outputQuantity}", 17, Accent, TextAlignmentOptions.TopLeft);
        nameText.rectTransform.anchorMin = new Vector2(0f, 1f); nameText.rectTransform.anchorMax = new Vector2(1f, 1f);
        nameText.rectTransform.pivot = new Vector2(0f, 1f);
        nameText.rectTransform.anchoredPosition = new Vector2(SidePad + 66f, -8f);
        nameText.rectTransform.sizeDelta = new Vector2(-(SidePad + 66f) - 130f, 24f);
        if (anton != null) nameText.font = anton;
        nameText.fontStyle = FontStyles.UpperCase; nameText.characterSpacing = 2f;
        nameText.raycastTarget = false;

        var inv = playerInventory != null ? playerInventory.Inventory : null;
        bool canCraft = recipe.HasMaterials(inv);

        var ingText = NewText("Ingredients", rowRt, BuildIngredientText(recipe, inv), 14, ParchDim, TextAlignmentOptions.TopLeft);
        ingText.rectTransform.anchorMin = new Vector2(0f, 0f); ingText.rectTransform.anchorMax = new Vector2(1f, 1f);
        ingText.rectTransform.pivot = new Vector2(0f, 1f);
        ingText.rectTransform.anchoredPosition = new Vector2(SidePad + 66f, -34f);
        ingText.rectTransform.sizeDelta = new Vector2(-(SidePad + 66f) - 130f, 50f);
        ingText.raycastTarget = false;

        var btnRt = NewRect("CraftButton", rowRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        btnRt.pivot = new Vector2(1f, 0.5f);
        btnRt.anchoredPosition = new Vector2(-SidePad, 0f);
        btnRt.sizeDelta = new Vector2(110f, 40f);
        var btnImg = btnRt.gameObject.AddComponent<Image>();
        btnImg.sprite = Spr("ui_button"); btnImg.type = Image.Type.Sliced;
        btnImg.color = canCraft ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        var btn = btnRt.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.interactable = canCraft;
        SetSpriteState(btn, Spr("ui_button"), Spr("ui_button_hover"));

        var btnLabel = NewText("Label", btnRt, "CRAFT", 15, canCraft ? Parch : ParchDim, TextAlignmentOptions.Center);
        btnLabel.raycastTarget = false;
        if (anton != null) btnLabel.font = anton;
        btnLabel.characterSpacing = 3f;

        btn.onClick.AddListener(() => CraftRecipe(recipe));
    }

    private string BuildIngredientText(CraftingRecipe recipe, Inventory inv)
    {
        var sb = new StringBuilder();
        if (recipe.ingredients == null) return sb.ToString();

        bool first = true;
        foreach (var ing in recipe.ingredients)
        {
            if (ing.item == null) continue;
            int have = inv != null ? inv.CountOf(ing.item) : 0;
            bool enough = have >= ing.quantity;
            string hex = ColorUtility.ToHtmlStringRGB(enough ? Good : Bad);
            if (!first) sb.Append("   ");
            first = false;
            sb.Append($"<color=#{hex}>{ing.item.displayName} {have}/{ing.quantity}</color>");
        }
        return sb.ToString();
    }

    private void CraftRecipe(CraftingRecipe recipe)
    {
        if (playerInventory == null) return;
        var inv = playerInventory.Inventory;
        if (!recipe.HasMaterials(inv)) return;

        recipe.Consume(inv);
        inv.TryAdd(recipe.output, recipe.outputQuantity);

        CombatAudio.PlayUI(craftSuccessSfx, craftSuccessVolume);

        DevLog.Log($"[Crafting] Crafted {recipe.output.displayName} x{recipe.outputQuantity} at " +
                  $"{(currentStation != null ? currentStation.StationName : "?")}.");

        RefreshRecipeList();
    }

    private static void SetSpriteState(Button btn, Sprite normal, Sprite hover)
    {
        btn.transition = Selectable.Transition.SpriteSwap;
        var state = btn.spriteState;
        state.highlightedSprite = hover != null ? hover : normal;
        state.pressedSprite = hover != null ? hover : normal;
        btn.spriteState = state;
    }

    // --------------------------------------------------------------- helpers
    private RectTransform NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        return rt;
    }

    private TMP_Text NewText(string name, Transform parent, string text, float size, Color col, TextAlignmentOptions align)
    {
        var rt = NewRect(name, parent, Vector2.zero, Vector2.one);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = col; t.alignment = align;
        if (anton != null) t.font = anton;
        return t;
    }
}
