using System;

/// <summary>
/// A fixed-slot inventory. Plain C# - no Unity dependency, so the add/stack/split
/// logic is easy to reason about and test. Slot order is meaningful (slot 0..N).
/// </summary>
[Serializable]
public class Inventory
{
    public struct Slot
    {
        public ItemData item;
        public int count;

        public bool IsEmpty => item == null || count <= 0;
        public int Space => item == null ? 0 : item.maxStack - count;

        public static readonly Slot Empty = new Slot { item = null, count = 0 };
    }

    private readonly Slot[] slots;

    /// <summary>Raised after any change to the contents.</summary>
    public event Action Changed;

    public int Capacity => slots.Length;

    public Inventory(int capacity)
    {
        slots = new Slot[capacity];
        for (int i = 0; i < capacity; i++) slots[i] = Slot.Empty;
    }

    public Slot GetSlot(int index) =>
        (index >= 0 && index < slots.Length) ? slots[index] : Slot.Empty;

    /// <summary>Total weight of everything carried.</summary>
    public float TotalWeight()
    {
        float w = 0f;
        foreach (var s in slots)
            if (!s.IsEmpty) w += s.item.weight * s.count;
        return w;
    }

    public int CountOf(ItemData item)
    {
        int c = 0;
        foreach (var s in slots)
            if (s.item == item) c += s.count;
        return c;
    }

    /// <summary>
    /// Adds up to <paramref name="count"/> of an item, merging into existing
    /// stacks first then filling empty slots. Returns how many did NOT fit.
    /// </summary>
    public int TryAdd(ItemData item, int count)
    {
        if (item == null || count <= 0) return count;
        int remaining = count;

        // merge into partial stacks
        if (item.Stackable)
        {
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].item != item) continue;
                int put = Math.Min(remaining, slots[i].Space);
                slots[i].count += put;
                remaining -= put;
            }
        }

        // fill empty slots
        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (!slots[i].IsEmpty) continue;
            int put = Math.Min(remaining, item.maxStack);
            slots[i] = new Slot { item = item, count = put };
            remaining -= put;
        }

        if (remaining != count) Changed?.Invoke();
        return remaining;
    }

    public bool CanFit(ItemData item, int count)
    {
        if (item == null) return true;
        int room = 0;
        foreach (var s in slots)
        {
            if (s.IsEmpty) room += item.maxStack;
            else if (s.item == item) room += s.Space;
            if (room >= count) return true;
        }
        return room >= count;
    }

    /// <summary>Removes up to <paramref name="count"/>. Returns how many were removed.</summary>
    public int Remove(ItemData item, int count)
    {
        int removed = 0;
        for (int i = 0; i < slots.Length && removed < count; i++)
        {
            if (slots[i].item != item) continue;
            int take = Math.Min(count - removed, slots[i].count);
            slots[i].count -= take;
            removed += take;
            if (slots[i].count <= 0) slots[i] = Slot.Empty;
        }
        if (removed > 0) Changed?.Invoke();
        return removed;
    }

    public bool RemoveAt(int index, int count)
    {
        if (index < 0 || index >= slots.Length || slots[index].IsEmpty) return false;
        slots[index].count -= Math.Max(1, count);
        if (slots[index].count <= 0) slots[index] = Slot.Empty;
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Drag-and-drop: move/merge the stack at <paramref name="from"/> onto
    /// <paramref name="to"/>. Merges same-item stacks, otherwise swaps.
    /// </summary>
    public void MoveOrMerge(int from, int to)
    {
        if (from == to || !Valid(from) || !Valid(to)) return;
        if (slots[from].IsEmpty) return;

        if (slots[to].IsEmpty)
        {
            slots[to] = slots[from];
            slots[from] = Slot.Empty;
        }
        else if (slots[to].item == slots[from].item && slots[to].item.Stackable)
        {
            int move = Math.Min(slots[from].count, slots[to].Space);
            slots[to].count += move;
            slots[from].count -= move;
            if (slots[from].count <= 0) slots[from] = Slot.Empty;
        }
        else
        {
            (slots[to], slots[from]) = (slots[from], slots[to]);
        }
        Changed?.Invoke();
    }

    /// <summary>Split half of the stack at <paramref name="index"/> into the first empty slot.</summary>
    public void SplitHalf(int index)
    {
        if (!Valid(index) || slots[index].IsEmpty || slots[index].count < 2) return;
        int half = slots[index].count / 2;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty) continue;
            slots[i] = new Slot { item = slots[index].item, count = half };
            slots[index].count -= half;
            Changed?.Invoke();
            return;
        }
    }

    private bool Valid(int i) => i >= 0 && i < slots.Length;
}
