using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class InventoryUIController : MonoBehaviour
{
    private const int VisibleSlotCount = 20;

    [SerializeField] private UIDocument inventoryDocument;
    [SerializeField] private Inventory inventory;
    [SerializeField] private UIDocument playerHudDocument;
    [SerializeField] private bool pauseWhileOpen = true;

    private VisualElement root;
    private VisualElement dragGhost;
    private VisualElement dragGhostIcon;
    private int selectedIndex = -1;
    private int carriedIndex = -1;
    private bool bound;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (inventoryDocument != null) inventoryDocument.enabled = true;
    }

    private void Start()
    {
        if (inventory == null) inventory = FindAnyObjectByType<Inventory>();
        if (inventory != null) inventory.OnInventoryChanged += Refresh;

        BindIfNeeded();
        SetOverlayVisible(false);

        if (playerHudDocument != null)
            playerHudDocument.rootVisualElement.Q<Button>("InventoryOpenButton").clicked += Toggle;
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        if (pauseWhileOpen) Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.iKey.wasPressedThisFrame) Toggle();
        if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (inventory == null || inventoryDocument == null) return;

        BindIfNeeded();
        isOpen = true;
        SetOverlayVisible(true);
        Refresh();

        if (pauseWhileOpen) Time.timeScale = 0f;
    }

    public void Close()
    {
        if (!IsOpen) return;

        EndCarry();
        selectedIndex = -1;
        isOpen = false;
        SetOverlayVisible(false);

        if (pauseWhileOpen) Time.timeScale = 1f;
    }

    private void SetOverlayVisible(bool visible)
    {
        if (root != null)
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BindIfNeeded()
    {
        if (bound || inventoryDocument == null) return;

        root = inventoryDocument.rootVisualElement;
        dragGhost = root.Q<VisualElement>("DragGhost");
        dragGhostIcon = root.Q<VisualElement>("DragGhostIcon");
        if (dragGhost != null) dragGhost.pickingMode = PickingMode.Ignore;

        root.RegisterCallback<PointerDownEvent>(OnInventoryPointerDown, TrickleDown.TrickleDown);
        root.RegisterCallback<PointerMoveEvent>(OnInventoryPointerMove, TrickleDown.TrickleDown);
        root.Q<Button>("SplitButton").clicked += SplitSelected;
        root.Q<Button>("DropButton").clicked += DropSelected;
        root.Q<Button>("CloseButton").clicked += Close;
        bound = true;
    }

    private void OnInventoryPointerDown(PointerDownEvent evt)
    {
        int targetIndex = GetSlotAtPosition(evt.position);
        if (targetIndex < 0) return;

        evt.StopImmediatePropagation();
        HandleSlotPlacement(targetIndex, evt.position);
    }

    private void HandleSlotPlacement(int targetIndex, Vector3 panelPosition)
    {
        if (carriedIndex < 0)
        {
            InventorySlot source = inventory.GetSlot(targetIndex);
            if (source == null || source.IsEmpty) return;
            BeginCarry(targetIndex, panelPosition);
            return;
        }

        if (targetIndex != carriedIndex)
        {
            inventory.MoveOrSwap(carriedIndex, targetIndex);
            selectedIndex = targetIndex;
        }

        EndCarry();
        Refresh();
    }

    private void OnInventoryPointerMove(PointerMoveEvent evt)
    {
        if (carriedIndex >= 0)
            UpdateCarryGhost(evt.position);
    }

    private int GetSlotAtPosition(Vector3 panelPosition)
    {
        Vector2 point = new Vector2(panelPosition.x, panelPosition.y);
        for (int index = 0; index < VisibleSlotCount; index++)
        {
            Button slot = root.Q<Button>($"Slot{index:00}");
            if (slot != null && slot.worldBound.Contains(point))
                return index;
        }

        return -1;
    }

    private void BeginCarry(int index, Vector3 panelPosition)
    {
        InventorySlot source = inventory.GetSlot(index);
        ItemDefinition definition = source != null && !source.IsEmpty ? inventory.GetDefinition(source.itemId) : null;
        if (definition == null || dragGhost == null || dragGhostIcon == null) return;

        carriedIndex = index;
        selectedIndex = index;
        dragGhostIcon.style.backgroundImage = definition.Icon != null ? new StyleBackground(definition.Icon) : StyleKeyword.None;
        dragGhost.style.display = DisplayStyle.Flex;
        UpdateCarryGhost(panelPosition);
        Refresh();
    }

    private void UpdateCarryGhost(Vector3 panelPosition)
    {
        if (dragGhost == null) return;
        dragGhost.style.left = panelPosition.x - 35f;
        dragGhost.style.top = panelPosition.y - 35f;
    }

    private void EndCarry()
    {
        carriedIndex = -1;
        if (dragGhost == null || dragGhostIcon == null) return;
        dragGhost.style.display = DisplayStyle.None;
        dragGhostIcon.style.backgroundImage = StyleKeyword.None;
    }

    private void SplitSelected()
    {
        if (selectedIndex < 0) return;
        inventory.SplitStack(selectedIndex);
    }

    private void DropSelected()
    {
        if (selectedIndex < 0) return;
        InventorySlot slot = inventory.GetSlot(selectedIndex);
        if (slot == null || slot.IsEmpty) return;

        string itemId = slot.itemId;
        if (inventory.RemoveFromSlot(selectedIndex, 1) > 0)
            InventoryPickup2D.Spawn(itemId, 1, inventory.transform.position + Vector3.right * 0.6f);
    }

    private void Refresh()
    {
        if (!bound || inventory == null) return;

        for (int index = 0; index < VisibleSlotCount; index++)
        {
            InventorySlot slot = inventory.GetSlot(index);
            ItemDefinition definition = slot != null && !slot.IsEmpty ? inventory.GetDefinition(slot.itemId) : null;
            VisualElement icon = root.Q<VisualElement>($"Icon{index:00}");
            Label count = root.Q<Label>($"Count{index:00}");
            Button button = root.Q<Button>($"Slot{index:00}");

            icon.style.backgroundImage = definition != null && definition.Icon != null
                ? new StyleBackground(definition.Icon)
                : StyleKeyword.None;

            count.text = slot != null && !slot.IsEmpty && slot.count > 1 ? slot.count.ToString() : string.Empty;
            button.EnableInClassList("inventory-slot-selected", index == selectedIndex);
        }

        InventorySlot selected = selectedIndex >= 0 ? inventory.GetSlot(selectedIndex) : null;
        ItemDefinition detail = selected != null && !selected.IsEmpty ? inventory.GetDefinition(selected.itemId) : null;
        VisualElement detailIcon = root.Q<VisualElement>("DetailIcon");
        Label detailName = root.Q<Label>("DetailName");
        Label detailDescription = root.Q<Label>("DetailDescription");

        if (detail != null)
        {
            detailIcon.style.backgroundImage = detail.Icon != null ? new StyleBackground(detail.Icon) : StyleKeyword.None;
            detailName.text = detail.DisplayName;
            detailDescription.text = detail.Description + (selected.count > 1 ? "\nStack: " + selected.count : string.Empty);
        }
        else
        {
            detailIcon.style.backgroundImage = StyleKeyword.None;
            detailName.text = "SELECT AN ITEM";
            detailDescription.text = "Click an item to pick it up, then click a slot to place it.";
        }
    }
}