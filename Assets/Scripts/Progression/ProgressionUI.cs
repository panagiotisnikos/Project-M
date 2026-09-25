using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The progression panel - lists whichever unlocks the open ProgressionAltar offers, cost
/// colored green/red by whether the player can afford it, greyed + "OWNED" once purchased.
/// Deliberately debug/functional quality (per the task brief) - built at runtime the same way
/// CraftingUI/OptionsMenuUI are, sharing the same sprite kit, so it needs no scene UI wiring.
/// Final visual polish is explicitly left for the user - see the memory/report for this task.
/// </summary>
public class ProgressionUI : MonoBehaviour
{
    public static ProgressionUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [SerializeField] private float panelWidth = 640f;

    [Header("Audio")]
    [SerializeField] private AudioClip purchaseSuccessSfx;
    [Range(0f, 1f)] [SerializeField] private float purchaseSuccessVolume = 0.5f;

    private RectTransform panel;
    private GameObject canvasGo;
    private RectTransform contentRt;
    private TMP_Text headerText;
    private Action onClose;
    private ProgressionAltar currentAltar;
    private PlayerInventory playerInventory;

    private static readonly Color StoneTint = new Color(0.62f, 0.63f, 0.68f, 1f);
    private static readonly Color Parch = new Color(0.87f, 0.83f, 0.74f);
    private static readonly Color ParchDim = new Color(0.60f, 0.57f, 0.50f);
    private static readonly Color Accent = new Color(0.55f, 0.78f, 0.95f);
    private static readonly Color Good = new Color(0.55f, 0.85f, 0.45f);
    private static readonly Color Bad = new Color(0.85f, 0.35f, 0.30f);

    private TMP_FontAsset anton;
    private const float RowHeight = 96f;
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

    public void Open(ProgressionAltar altar, Action closeCallback)
    {
        currentAltar = altar;
        onClose = closeCallback;
        headerText.text = altar != null ? altar.AltarName.ToUpperInvariant() : "PROGRESSION";
        canvasGo.SetActive(true);
        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshList();
    }

    public void Close()
    {
        canvasGo.SetActive(false);
        IsOpen = false;
        currentAltar = null;
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

        canvasGo = new GameObject("ProgressionCanvas");
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

        headerText = NewText("Header", panel, "PROGRESSION", 24, Accent, TextAlignmentOptions.TopLeft);
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

    // ------------------------------------------------------------ unlock list
    private void RefreshList()
    {
        for (int i = contentRt.childCount - 1; i >= 0; i--)
            Destroy(contentRt.GetChild(i).gameObject);

        float cursorY = 0f;

        if (currentAltar != null && currentAltar.Unlocks != null)
        {
            foreach (var unlock in currentAltar.Unlocks)
            {
                if (unlock == null) continue;
                AddUnlockRow(unlock, cursorY);
                cursorY += RowHeight + 8f;
            }
        }

        if (cursorY == 0f)
        {
            var empty = NewText("Empty", contentRt, "Nothing to unlock here.", 16, ParchDim, TextAlignmentOptions.TopLeft);
            empty.rectTransform.anchorMin = new Vector2(0f, 1f); empty.rectTransform.anchorMax = new Vector2(1f, 1f);
            empty.rectTransform.pivot = new Vector2(0f, 1f);
            empty.rectTransform.anchoredPosition = new Vector2(SidePad, 0f);
            empty.rectTransform.sizeDelta = new Vector2(-SidePad * 2f, 28f);
            cursorY += 28f;
        }

        contentRt.sizeDelta = new Vector2(0f, cursorY);
    }

    private void AddUnlockRow(ProgressionUnlock unlock, float cursorY)
    {
        var rowRt = NewRect("Unlock_" + unlock.name, contentRt, new Vector2(0f, 1f), new Vector2(1f, 1f));
        rowRt.pivot = new Vector2(0f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, -cursorY);
        rowRt.sizeDelta = new Vector2(0f, RowHeight);

        var bgImg = rowRt.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.18f);
        bgImg.raycastTarget = false;

        bool owned = ProgressionSystem.HasUnlock(unlock);

        var nameText = NewText("Name", rowRt, $"{unlock.displayName}  <size=70%>[{unlock.category}]</size>", 17,
            owned ? ParchDim : Accent, TextAlignmentOptions.TopLeft);
        nameText.rectTransform.anchorMin = new Vector2(0f, 1f); nameText.rectTransform.anchorMax = new Vector2(1f, 1f);
        nameText.rectTransform.pivot = new Vector2(0f, 1f);
        nameText.rectTransform.anchoredPosition = new Vector2(SidePad, -8f);
        nameText.rectTransform.sizeDelta = new Vector2(-SidePad - 140f, 24f);
        if (anton != null) nameText.font = anton;
        nameText.fontStyle = FontStyles.UpperCase; nameText.characterSpacing = 2f;
        nameText.raycastTarget = false;

        var descText = NewText("Description", rowRt, unlock.description, 13, ParchDim, TextAlignmentOptions.TopLeft);
        descText.rectTransform.anchorMin = new Vector2(0f, 1f); descText.rectTransform.anchorMax = new Vector2(1f, 1f);
        descText.rectTransform.pivot = new Vector2(0f, 1f);
        descText.rectTransform.anchoredPosition = new Vector2(SidePad, -32f);
        descText.rectTransform.sizeDelta = new Vector2(-SidePad - 140f, 20f);
        descText.raycastTarget = false;

        var inv = playerInventory != null ? playerInventory.Inventory : null;

        var costText = NewText("Cost", rowRt, BuildCostText(unlock, inv), 14, ParchDim, TextAlignmentOptions.TopLeft);
        costText.rectTransform.anchorMin = new Vector2(0f, 0f); costText.rectTransform.anchorMax = new Vector2(1f, 1f);
        costText.rectTransform.pivot = new Vector2(0f, 1f);
        costText.rectTransform.anchoredPosition = new Vector2(SidePad, -54f);
        costText.rectTransform.sizeDelta = new Vector2(-SidePad - 140f, 24f);
        costText.raycastTarget = false;

        bool canBuy = !owned && unlock.CanAfford(inv);

        var btnRt = NewRect("BuyButton", rowRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        btnRt.pivot = new Vector2(1f, 0.5f);
        btnRt.anchoredPosition = new Vector2(-SidePad, 0f);
        btnRt.sizeDelta = new Vector2(110f, 40f);
        var btnImg = btnRt.gameObject.AddComponent<Image>();
        btnImg.sprite = Spr("ui_button"); btnImg.type = Image.Type.Sliced;
        btnImg.color = canBuy ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        var btn = btnRt.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.interactable = canBuy;
        SetSpriteState(btn, Spr("ui_button"), Spr("ui_button_hover"));

        var btnLabel = NewText("Label", btnRt, owned ? "OWNED" : "UNLOCK", 15, canBuy ? Parch : ParchDim, TextAlignmentOptions.Center);
        btnLabel.raycastTarget = false;
        if (anton != null) btnLabel.font = anton;
        btnLabel.characterSpacing = 3f;

        btn.onClick.AddListener(() => PurchaseUnlock(unlock));
    }

    private string BuildCostText(ProgressionUnlock unlock, Inventory inv)
    {
        var sb = new StringBuilder();
        if (unlock.cost == null) return sb.ToString();

        bool first = true;
        foreach (var c in unlock.cost)
        {
            if (c.item == null) continue;
            int have = inv != null ? inv.CountOf(c.item) : 0;
            bool enough = have >= c.quantity;
            string hex = ColorUtility.ToHtmlStringRGB(enough ? Good : Bad);
            if (!first) sb.Append("   ");
            first = false;
            sb.Append($"<color=#{hex}>{c.item.displayName} {have}/{c.quantity}</color>");
        }
        return sb.ToString();
    }

    private void PurchaseUnlock(ProgressionUnlock unlock)
    {
        if (playerInventory == null) return;
        var inv = playerInventory.Inventory;

        if (!ProgressionSystem.TryPurchase(unlock, inv)) return;

        CombatAudio.PlayUI(purchaseSuccessSfx, purchaseSuccessVolume);

        DevLog.Log($"[Progression] Purchased {unlock.displayName} at " +
                  $"{(currentAltar != null ? currentAltar.AltarName : "?")}.");

        RefreshList();
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
