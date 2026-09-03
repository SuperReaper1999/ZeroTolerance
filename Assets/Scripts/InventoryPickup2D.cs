using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class InventoryPickup2D : MonoBehaviour
{
    [SerializeField] private string itemId;
    [SerializeField, Min(1)] private int count = 1;

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    public static InventoryPickup2D Spawn(string id, int amount, Vector3 position)
    {
        GameObject go = new GameObject("Dropped Inventory Item");
        go.transform.position = position;
        go.AddComponent<SpriteRenderer>().sortingOrder = 20;
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        InventoryPickup2D pickup = go.AddComponent<InventoryPickup2D>();
        pickup.itemId = id;
        pickup.count = amount;
        pickup.ApplyVisual();
        return pickup;
    }

    private void Start() => ApplyVisual();

    private void ApplyVisual()
    {
        ItemDefinition definition = ItemRegistry.Shared != null ? ItemRegistry.Shared.Get(itemId) : null;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && definition != null && definition.Icon != null)
            renderer.sprite = definition.Icon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Inventory inventory = other.GetComponent<Inventory>();
        if (inventory == null) inventory = other.GetComponentInParent<Inventory>();
        if (inventory == null) return;

        int added = inventory.AddItem(itemId, count);
        if (added <= 0) return;

        count -= added;
        if (count <= 0) Destroy(gameObject);
    }
}