using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string keyId;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject worldModel;
    [SerializeField] private bool stackable = true;
    [SerializeField, Min(1)] private int maxStackSize = 99;

    public string Id => id;
    public string KeyId => keyId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public virtual GameObject WorldModel => worldModel;
    public bool Stackable => stackable;
    public int MaxStackSize => stackable ? Mathf.Max(1, maxStackSize) : 1;
}