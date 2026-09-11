using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One cell of the inventory grid. Created at runtime by InventoryUI, which owns
/// all the actual inventory logic - this just reports interactions back to it.
/// </summary>
public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int Index { get; private set; }
    private InventoryUI owner;

    private Image background;
    private Image iconImage;
    private TMP_Text countText;
    private GameObject equippedMark;
    private GameObject hotbarMark;

    public void Init(InventoryUI ui, int index, Image bg, Image icon, TMP_Text count, GameObject equipped, GameObject hotbar)
    {
        owner = ui;
        Index = index;
        background = bg;
        iconImage = icon;
        countText = count;
        equippedMark = equipped;
        hotbarMark = hotbar;
    }

    public void Render(Inventory.Slot slot, bool equipped, bool isHotbar)
    {
        bool has = !slot.IsEmpty;
        if (iconImage != null)
        {
            iconImage.enabled = has && slot.item.icon != null;
            iconImage.sprite = has ? slot.item.icon : null;
            // fall back to a tinted block if no icon
            if (has && slot.item.icon == null) { iconImage.enabled = true; iconImage.sprite = null; iconImage.color = new Color(0.55f, 0.5f, 0.42f, 1f); }
            else if (iconImage.enabled) iconImage.color = Color.white;
        }
        if (countText != null)
            countText.text = (has && slot.count > 1) ? slot.count.ToString() : "";
        if (equippedMark != null) equippedMark.SetActive(has && equipped);
        if (hotbarMark != null) hotbarMark.SetActive(isHotbar);
    }

    public void SetHighlight(bool on)
    {
        if (background != null)
            background.color = on ? new Color(0.42f, 0.38f, 0.30f, 1f) : new Color(0.14f, 0.14f, 0.16f, 1f);
    }

    // ---- interaction -> owner ----
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right) owner.RightClickSlot(Index);
        else if (e.button == PointerEventData.InputButton.Left && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            owner.ShiftClickSlot(Index);
    }

    public void OnPointerEnter(PointerEventData e) => owner.HoverSlot(Index, true);
    public void OnPointerExit(PointerEventData e) => owner.HoverSlot(Index, false);

    public void OnBeginDrag(PointerEventData e) => owner.BeginDrag(Index, e);
    public void OnDrag(PointerEventData e) => owner.Drag(e);
    public void OnEndDrag(PointerEventData e) => owner.EndDrag(e);
    public void OnDrop(PointerEventData e) => owner.DropOnSlot(Index);
}
