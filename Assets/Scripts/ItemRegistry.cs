using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "Inventory/Item Registry")]
public sealed class ItemRegistry : ScriptableObject
{
    private const string ResourceName = "ItemRegistry";

    [SerializeField] private List<ItemDefinition> items = new();

    private static ItemRegistry shared;
    private Dictionary<string, ItemDefinition> byId;

    public static ItemRegistry Shared
    {
        get
        {
            if (shared == null) shared = Resources.Load<ItemRegistry>(ResourceName);
            return shared;
        }
    }

    public IReadOnlyList<ItemDefinition> Items => items;

    public ItemDefinition Get(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return null;
        EnsureLookup();
        return byId.TryGetValue(itemId, out ItemDefinition item) ? item : null;
    }

    private void OnEnable() => RebuildLookup();

    private void EnsureLookup()
    {
        if (byId == null) RebuildLookup();
    }

    private void RebuildLookup()
    {
        byId = new Dictionary<string, ItemDefinition>();
        foreach (ItemDefinition item in items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id) || byId.ContainsKey(item.Id)) continue;
            byId.Add(item.Id, item);
        }
    }
}