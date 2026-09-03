using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Inventory : MonoBehaviour
{
    [SerializeField] private List<string> excludedItemIds = new();
    [SerializeField, Min(1)] private int capacity = 20;
    [SerializeField] private List<InventorySlot> startingItems = new();

    private InventorySlot[] slots;
    public event Action OnInventoryChanged;

    public int Capacity => capacity;
    public IReadOnlyList<InventorySlot> Slots => slots;
    public bool IsInitialized => slots != null;
    internal InventorySlot[] RawSlots => slots;

    private void Awake()
    {
        slots = new InventorySlot[Mathf.Max(0, capacity)];
        for (int index = 0; index < slots.Length; index++)
            slots[index] = new InventorySlot();

        foreach (InventorySlot starting in startingItems)
            if (starting != null) AddItem(starting.itemId, starting.count);
    }

    internal void NotifyChanged() => OnInventoryChanged?.Invoke();

    public ItemDefinition GetDefinition(string itemId) => ItemRegistry.Shared != null ? ItemRegistry.Shared.Get(itemId) : null;

    public bool AllowsItem(string itemId) =>
        GetDefinition(itemId) != null && (excludedItemIds == null || !excludedItemIds.Contains(itemId));

    public InventorySlot GetSlot(int index) => IsValidIndex(index) ? slots[index] : null;

    public int GetCount(string itemId)
    {
        if (slots == null) return 0;

        int total = 0;
        foreach (InventorySlot slot in slots)
            if (slot != null && slot.itemId == itemId) total += slot.count;
        return total;
    }

    public bool HasItem(string itemId, int count = 1) => GetCount(itemId) >= count;

    public bool CanAddItem(string itemId, int count = 1)
    {
        if (!AllowsItem(itemId) || count <= 0 || slots == null) return false;

        int maxStack = GetMaxStack(itemId);
        int available = 0;
        foreach (InventorySlot slot in slots)
        {
            if (slot.IsEmpty) available += maxStack;
            else if (slot.itemId == itemId) available += Mathf.Max(0, maxStack - slot.count);
            if (available >= count) return true;
        }
        return false;
    }

    public int AddItem(string itemId, int count = 1)
    {
        if (!AllowsItem(itemId) || count <= 0 || slots == null) return 0;

        int remaining = count;
        int maxStack = GetMaxStack(itemId);

        foreach (InventorySlot slot in slots)
        {
            if (remaining <= 0) break;
            if (slot.itemId != itemId || slot.count >= maxStack) continue;

            int added = Mathf.Min(maxStack - slot.count, remaining);
            slot.count += added;
            remaining -= added;
        }

        foreach (InventorySlot slot in slots)
        {
            if (remaining <= 0) break;
            if (!slot.IsEmpty) continue;

            int added = Mathf.Min(maxStack, remaining);
            slot.itemId = itemId;
            slot.count = added;
            remaining -= added;
        }

        int actuallyAdded = count - remaining;
        if (actuallyAdded > 0) NotifyChanged();
        return actuallyAdded;
    }

    public bool RemoveItem(string itemId, int count = 1)
    {
        if (!HasItem(itemId, count) || slots == null) return false;

        int remaining = count;
        for (int index = slots.Length - 1; index >= 0 && remaining > 0; index--)
        {
            InventorySlot slot = slots[index];
            if (slot.itemId != itemId) continue;

            int removed = Mathf.Min(slot.count, remaining);
            slot.count -= removed;
            remaining -= removed;
            if (slot.count <= 0) slots[index] = new InventorySlot();
        }

        NotifyChanged();
        return true;
    }

    public void MoveOrSwap(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex) return;
        if (slots[fromIndex].IsEmpty) return;

        SlotOps.MoveOrSwap(slots, fromIndex, slots, toIndex, GetMaxStack(slots[fromIndex].itemId));
        NotifyChanged();
    }

    public int RemoveFromSlot(int index, int count)
    {
        if (!IsValidIndex(index) || count <= 0) return 0;

        InventorySlot slot = slots[index];
        if (slot.IsEmpty) return 0;

        int removed = Mathf.Min(slot.count, count);
        slot.count -= removed;
        if (slot.count <= 0) slots[index] = new InventorySlot();
        NotifyChanged();
        return removed;
    }

    public void SplitStack(int index)
    {
        if (!IsValidIndex(index)) return;
        if (SlotOps.Split(slots, index)) NotifyChanged();
    }

    public void TransferFrom(Inventory other, int otherIndex, int myIndex)
    {
        if (other == null || !other.IsValidIndex(otherIndex) || !IsValidIndex(myIndex)) return;

        InventorySlot source = other.slots[otherIndex];
        if (source.IsEmpty || !AllowsItem(source.itemId)) return;

        InventorySlot target = slots[myIndex];
        if (!target.IsEmpty && target.itemId != source.itemId && !other.AllowsItem(target.itemId)) return;

        SlotOps.MoveOrSwap(other.slots, otherIndex, slots, myIndex, GetMaxStack(source.itemId));
        other.NotifyChanged();
        NotifyChanged();
    }

    private bool IsValidIndex(int index) => slots != null && index >= 0 && index < slots.Length;

    private int GetMaxStack(string itemId)
    {
        ItemDefinition definition = GetDefinition(itemId);
        return definition != null ? definition.MaxStackSize : 1;
    }
}