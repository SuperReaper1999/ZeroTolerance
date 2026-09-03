using UnityEngine;

public static class SlotOps
{
    public static void MoveOrSwap(InventorySlot[] fromSlots, int fromIndex, InventorySlot[] toSlots, int toIndex, int maxStackAtTarget)
    {
        InventorySlot from = fromSlots[fromIndex];
        if (from == null || from.IsEmpty) return;

        InventorySlot to = toSlots[toIndex];
        if (to.IsEmpty)
        {
            toSlots[toIndex] = from;
            fromSlots[fromIndex] = new InventorySlot();
        }
        else if (to.itemId == from.itemId)
        {
            int space = maxStackAtTarget - to.count;
            if (space > 0)
            {
                int moved = Mathf.Min(space, from.count);
                to.count += moved;
                from.count -= moved;
                if (from.count <= 0) fromSlots[fromIndex] = new InventorySlot();
            }
            else
            {
                fromSlots[fromIndex] = to;
                toSlots[toIndex] = from;
            }
        }
        else
        {
            fromSlots[fromIndex] = to;
            toSlots[toIndex] = from;
        }
    }

    public static bool Split(InventorySlot[] slots, int index)
    {
        InventorySlot source = slots[index];
        if (source == null || source.IsEmpty || source.count < 2) return false;

        int emptyIndex = -1;
        for (int candidate = 0; candidate < slots.Length; candidate++)
        {
            if (candidate != index && slots[candidate].IsEmpty)
            {
                emptyIndex = candidate;
                break;
            }
        }

        if (emptyIndex < 0) return false;

        int moved = source.count / 2;
        slots[emptyIndex] = new InventorySlot(source.itemId, moved);
        source.count -= moved;
        return true;
    }
}