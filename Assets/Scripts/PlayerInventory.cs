using System;
using UnityEngine;

[Serializable]
public sealed class InventorySlot
{
    public string itemId;
    public int count;

    public bool IsEmpty => string.IsNullOrEmpty(itemId) || count <= 0;

    public InventorySlot() { }

    public InventorySlot(string id, int amount)
    {
        itemId = id;
        count = amount;
    }
}

// Keeps the existing 2D player prefab intact. It is an Inventory, so 3D
// gameplay and save components can depend on the shared base type directly.
[DisallowMultipleComponent]
public sealed class PlayerInventory : Inventory { }