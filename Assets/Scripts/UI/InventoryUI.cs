using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The carved-stone inventory grid. Builds itself at runtime. Real-time (the
/// world keeps running); Tab toggles it. Owns all drag / split / drop logic and
/// drives PlayerInventory for equip / use / drop.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] private int columns = 6;
    [SerializeField] private int rows = 4;
    [SerializeField] private float cell = 62f;
    [SerializeField] private float gap = 6f;

    [Header("Audio")]
    [SerializeField] private AudioClip toggleSfx;
    [Range(0f, 1f)] [SerializeField] private float toggleVolume = 0.4f;

    private PlayerInventory playerInventory;
    private PlayerEquipment playerEquipment;
    private Inventory inv;

    private RectTransform panel;
    private InventorySlotUI[] slots;
    private Image weightFill;
    private TMP_Text weightText;
    private GameObject tooltip;
    private TMP_Text tooltipText;
    private Image dragGhost;

    private int dragFrom = -1;
    private TMP_FontAsset anton;

    private static readonly Color Stone = new Color(0.14f, 0.14f, 0.16f, 1f);
    private static readonly Color StoneTint = new Color(0.62f, 0.63f, 0.68f, 1f);
    private static readonly Color Parch = new Color(0.87f, 0.83f, 0.74f);
    private static readonly Color Title = UIPalette.Lichen;
    private static readonly Color Accent = UIPalette.Teal;

    private void Awake()
    {
        playerInventory = FindFirstObjectByType<PlayerInventory>();
        playerEquipment = FindFirstObjectByType<PlayerEquipment>();
        anton = LoadFont();
        Build();
        SetOpen(false);
    }

    private void OnEnable()
    {
        if (playerInventory != null && playerInventory.Inventory != null)
        {
            inv = playerInventory.Inventory;
            inv.Changed += Refresh;
        }
    }

    private void OnDisable()
    {
        if (inv != null) inv.Changed -= Refresh;
        if (IsOpen) SetOpen(false);
    }

    private void Start()
    {
        if (inv == null && playerInventory != null) { inv = playerInventory.Inventory; if (inv != null) inv.Changed += Refresh; }
        Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyBindings.Get(GameAction.ToggleInventory)) && !GameUIController.IsPaused)
            SetOpen(!IsOpen);

        if (IsOpen && dragFrom >= 0 && dragGhost != null)
            dragGhost.rectTransform.position = Input.mousePosition;
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

        var canvasGo = new GameObject("InventoryCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        float w = columns * cell + (columns - 1) * gap + 44f;
        float h = rows * cell + (rows - 1) * gap + 120f;

        panel = NewRect("Panel", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        panel.sizeDelta = new Vector2(w, h);
        panel.anchoredPosition = new Vector2(-260f, 0f);
        var pImg = panel.gameObject.AddComponent<Image>();
        pImg.sprite = slate; pImg.type = Image.Type.Sliced; pImg.color = StoneTint;

        var fr = NewRect("Frame", panel, Vector2.zero, Vector2.one);
        fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
        var frImg = fr.gameObject.AddComponent<Image>();
        frImg.sprite = frame; frImg.type = Image.Type.Sliced; frImg.raycastTarget = false;

        var header = NewText("Header", panel, "PACK", 22, Title, TextAlignmentOptions.TopLeft);
        header.rectTransform.anchorMin = new Vector2(0f, 1f); header.rectTransform.anchorMax = new Vector2(1f, 1f);
        header.rectTransform.pivot = new Vector2(0f, 1f);
        header.rectTransform.anchoredPosition = new Vector2(22f, -14f);
        header.rectTransform.sizeDelta = new Vector2(w - 44f, 30f);
        header.characterSpacing = 8f; header.fontStyle = FontStyles.UpperCase;

        // grid
        slots = new InventorySlotUI[columns * rows];
        float gridW = columns * cell + (columns - 1) * gap;
        float startX = (w - gridW) * 0.5f;
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < columns; c++)
        {
            int idx = r * columns + c;
            var cellRt = NewRect($"Slot{idx}", panel, new Vector2(0f, 1f), new Vector2(0f, 1f));
            cellRt.pivot = new Vector2(0f, 1f);
            cellRt.sizeDelta = new Vector2(cell, cell);
            cellRt.anchoredPosition = new Vector2(startX + c * (cell + gap), -52f - r * (cell + gap));

            var bg = cellRt.gameObject.AddComponent<Image>();
            bg.color = Stone;

            var iconRt = NewRect("Icon", cellRt, Vector2.zero, Vector2.one);
            iconRt.offsetMin = new Vector2(6f, 6f); iconRt.offsetMax = new Vector2(-6f, -6f);
            var icon = iconRt.gameObject.AddComponent<Image>();
            icon.raycastTarget = false; icon.preserveAspect = true; icon.enabled = false;

            var count = NewText("Count", cellRt, "", 15, Parch, TextAlignmentOptions.BottomRight);
            count.rectTransform.offsetMin = new Vector2(2f, 2f); count.rectTransform.offsetMax = new Vector2(-4f, -2f);
            count.raycastTarget = false;

            var equipRt = NewRect("Equipped", cellRt, new Vector2(0f, 1f), new Vector2(0f, 1f));
            equipRt.pivot = new Vector2(0f, 1f); equipRt.sizeDelta = new Vector2(10f, 10f); equipRt.anchoredPosition = new Vector2(3f, -3f);
            var eqImg = equipRt.gameObject.AddComponent<Image>(); eqImg.color = Accent; eqImg.raycastTarget = false;
            equipRt.gameObject.SetActive(false);

            var hotRt = NewRect("Hotbar", cellRt, new Vector2(0f, 0f), new Vector2(1f, 0f));
            hotRt.pivot = new Vector2(0.5f, 0f); hotRt.sizeDelta = new Vector2(0f, 3f); hotRt.anchoredPosition = Vector2.zero;
            var hotImg = hotRt.gameObject.AddComponent<Image>(); hotImg.color = UIPalette.MossDim; hotImg.raycastTarget = false;

            var s = cellRt.gameObject.AddComponent<InventorySlotUI>();
            s.Init(this, idx, bg, icon, count, equipRt.gameObject, hotRt.gameObject);
            slots[idx] = s;
        }

        // weight bar
        var wbBg = NewRect("WeightBar", panel, new Vector2(0f, 0f), new Vector2(1f, 0f));
        wbBg.pivot = new Vector2(0.5f, 0f); wbBg.sizeDelta = new Vector2(-44f, 14f); wbBg.anchoredPosition = new Vector2(0f, 40f);
        var wbBgImg = wbBg.gameObject.AddComponent<Image>(); wbBgImg.color = new Color(0.05f, 0.05f, 0.06f, 1f);
        var wbFill = NewRect("Fill", wbBg, Vector2.zero, new Vector2(1f, 1f));
        wbFill.offsetMin = Vector2.zero; wbFill.offsetMax = Vector2.zero;
        weightFill = wbFill.gameObject.AddComponent<Image>();
        weightFill.color = UIPalette.Sage;
        weightFill.type = Image.Type.Filled; weightFill.fillMethod = Image.FillMethod.Horizontal; weightFill.fillAmount = 0f;
        weightText = NewText("WeightText", panel, "0 / 0", 13, Parch, TextAlignmentOptions.Center);
        weightText.rectTransform.anchorMin = new Vector2(0f, 0f); weightText.rectTransform.anchorMax = new Vector2(1f, 0f);
        weightText.rectTransform.pivot = new Vector2(0.5f, 0f);
        weightText.rectTransform.anchoredPosition = new Vector2(0f, 56f);
        weightText.rectTransform.sizeDelta = new Vector2(-44f, 16f);

        var hint = NewText("Hint", panel, "[Tab] close      right-click use / equip      drag to move      drag out to drop", 12, new Color(0.55f, 0.52f, 0.46f), TextAlignmentOptions.Center);
        hint.rectTransform.anchorMin = new Vector2(0f, 0f); hint.rectTransform.anchorMax = new Vector2(1f, 0f);
        hint.rectTransform.pivot = new Vector2(0.5f, 0f);
        hint.rectTransform.anchoredPosition = new Vector2(0f, 16f);
        hint.rectTransform.sizeDelta = new Vector2(-24f, 16f);

        // tooltip
        tooltip = NewRect("Tooltip", canvasGo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f)).gameObject;
        var ttRt = (RectTransform)tooltip.transform;
        ttRt.pivot = new Vector2(0f, 1f); ttRt.sizeDelta = new Vector2(280f, 130f);
        var ttImg = tooltip.AddComponent<Image>(); ttImg.sprite = slate; ttImg.type = Image.Type.Sliced; ttImg.color = new Color(0.5f, 0.5f, 0.55f, 1f); ttImg.raycastTarget = false;
        tooltipText = NewText("Text", tooltip.transform, "", 14, Parch, TextAlignmentOptions.TopLeft);
        tooltipText.rectTransform.offsetMin = new Vector2(12f, 10f); tooltipText.rectTransform.offsetMax = new Vector2(-12f, -10f);
        tooltipText.textWrappingMode = TextWrappingModes.Normal; tooltipText.raycastTarget = false;
        tooltip.SetActive(false);

        // drag ghost
        var dgRt = NewRect("DragGhost", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        dgRt.sizeDelta = new Vector2(cell - 8f, cell - 8f);
        dragGhost = dgRt.gameObject.AddComponent<Image>();
        dragGhost.raycastTarget = false; dragGhost.preserveAspect = true; dragGhost.enabled = false;
    }

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

    // ------------------------------------------------------------- open/close
    private void SetOpen(bool open)
    {
        IsOpen = open;
        CombatAudio.PlayUI(toggleSfx, toggleVolume);
        if (panel != null) panel.gameObject.SetActive(open);
        if (tooltip != null) tooltip.SetActive(false);
        if (dragGhost != null) { dragGhost.enabled = false; dragFrom = -1; }

        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
        if (open) Refresh();
    }

    // --------------------------------------------------------------- refresh
    private void Refresh()
    {
        if (slots == null || inv == null) return;
        var w = playerEquipment != null ? playerEquipment.EquippedWeapon : null;
        var s = playerEquipment != null ? playerEquipment.EquippedShield : null;

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = inv.GetSlot(i);
            bool equipped = !slot.IsEmpty &&
                ((slot.item.weaponData != null && slot.item.weaponData == w) ||
                 (slot.item.shieldData != null && slot.item.shieldData == s));
            slots[i].Render(slot, equipped, i < columns);
        }

        float total = inv.TotalWeight();
        float limit = playerInventory != null ? playerInventory.CarryLimit : 1f;
        if (weightFill != null) weightFill.fillAmount = Mathf.Clamp01(total / Mathf.Max(1f, limit));
        if (weightFill != null) weightFill.color = total > limit ? UIPalette.Rose : UIPalette.Sage;
        if (weightText != null) weightText.text = $"{total:0}  /  {limit:0}";
    }

    // ------------------------------------------------------------ interactions
    public void RightClickSlot(int index)
    {
        if (playerInventory != null) playerInventory.UseSlot(index);
        Refresh();
    }

    public void ShiftClickSlot(int index)
    {
        inv?.SplitHalf(index);
    }

    public void HoverSlot(int index, bool entered)
    {
        slots[index].SetHighlight(entered);
        if (!entered) { tooltip.SetActive(false); return; }

        var slot = inv.GetSlot(index);
        if (slot.IsEmpty) { tooltip.SetActive(false); return; }

        tooltipText.text = BuildTooltip(slot);
        tooltip.SetActive(true);
        var rt = (RectTransform)slots[index].transform;
        ((RectTransform)tooltip.transform).position = rt.position + new Vector3(rt.rect.width * 0.5f, 0f, 0f);
    }

    private string BuildTooltip(Inventory.Slot slot)
    {
        var it = slot.item;
        string s = $"<b>{it.displayName}</b>\n<size=11><color=#8a8578>{it.type}</color></size>\n";
        if (!string.IsNullOrEmpty(it.description)) s += $"<size=12>{it.description}</size>\n";
        if (it.type == ItemType.Weapon && it.weaponData != null) s += $"<size=12>Damage {it.weaponData.BaseDamage}</size>\n";
        if (it.type == ItemType.Consumable && it.buffDuration > 0f)
            s += $"<size=12>Buff for {it.buffDuration:0}s</size>\n";
        s += $"<size=11>Weight {it.weight:0.#}" + (slot.count > 1 ? $"  x{slot.count}" : "") + "</size>";
        return s;
    }

    public void BeginDrag(int index, PointerEventData e)
    {
        var slot = inv.GetSlot(index);
        if (slot.IsEmpty) return;
        dragFrom = index;
        dragGhost.sprite = slot.item.icon;
        dragGhost.color = slot.item.icon != null ? Color.white : new Color(0.55f, 0.5f, 0.42f, 0.9f);
        dragGhost.enabled = true;
        dragGhost.rectTransform.position = e.position;
        tooltip.SetActive(false);
    }

    public void Drag(PointerEventData e)
    {
        if (dragGhost.enabled) dragGhost.rectTransform.position = e.position;
    }

    public void DropOnSlot(int index)
    {
        if (dragFrom >= 0 && dragFrom != index)
            inv.MoveOrMerge(dragFrom, index);
        dragFrom = -1;
    }

    public void EndDrag(PointerEventData e)
    {
        if (dragFrom >= 0)
        {
            // released outside the panel -> drop to the world
            if (!RectTransformUtility.RectangleContainsScreenPoint(panel, e.position))
            {
                playerInventory?.DropSlot(dragFrom);
            }
        }
        dragFrom = -1;
        if (dragGhost != null) dragGhost.enabled = false;
    }
}
