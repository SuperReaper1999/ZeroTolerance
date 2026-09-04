using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class ControlsMenuController : MonoBehaviour
{
    private readonly struct BindingTarget
    {
        public readonly string buttonName, mapName, actionName, partName, devicePrefix;
        public BindingTarget(string button, string map, string action, string part, string device)
        { buttonName = button; mapName = map; actionName = action; partName = part; devicePrefix = device; }
    }

    private static readonly BindingTarget[] Targets =
    {
        new BindingTarget("MoveLeftButton", "Player", "Move", "left", "<Keyboard>"),
        new BindingTarget("MoveRightButton", "Player", "Move", "right", "<Keyboard>"),
        new BindingTarget("JumpButton", "Player", "Jump", string.Empty, "<Keyboard>")
    };

    [SerializeField] private UIDocument controlsDocument;
    [SerializeField] private StyleSheet controlsStyleSheet;
    [SerializeField] private PauseController pauseController;
    [SerializeField] private UIDocument returnDocument;
    [SerializeField] private PlayerInput playerInput;

    private readonly Dictionary<Button, BindingTarget> targetsByButton = new();
    private readonly Dictionary<Button, Action> buttonHandlers = new();
    private InputBindingPersistence persistence;
    private InputActionRebindingExtensions.RebindingOperation activeRebind;
    private InputAction activeAction;
    private int activeBindingIndex = -1;
    private string previousOverridePath;
    private Label statusLabel;
    private Button backButton;
    private Button resetAllButton;
    private bool styleAttached;

    private void Awake()
    {
        if (playerInput == null) playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            persistence = playerInput.GetComponent<InputBindingPersistence>();
            if (persistence == null) persistence = playerInput.gameObject.AddComponent<InputBindingPersistence>();
        }
    }

    private void OnEnable()
    {
        UnbindUi();
        AttachAuthoredStyle();
        persistence?.LoadOverrides();
        Bind();
        Refresh();
    }

    private void OnDisable()
    {
        CancelActiveRebind();
        UnbindUi();
    }

    private void AttachAuthoredStyle()
    {
        if (styleAttached || controlsDocument == null || controlsStyleSheet == null) return;
        controlsDocument.rootVisualElement.styleSheets.Add(controlsStyleSheet);
        styleAttached = true;
    }

    public void Refresh()
    {
        if (targetsByButton.Count == 0) Bind();
        foreach (KeyValuePair<Button, BindingTarget> entry in targetsByButton) RefreshButton(entry.Key, entry.Value);
    }

    private void Bind()
    {
        if (controlsDocument == null || playerInput == null || playerInput.actions == null || targetsByButton.Count > 0) return;
        VisualElement root = controlsDocument.rootVisualElement;
        statusLabel = root.Q<Label>("RebindStatusLabel");
        if (statusLabel == null) return;
        foreach (BindingTarget target in Targets)
            if (root.Q<Button>(target.buttonName) == null) return;

        foreach (BindingTarget target in Targets)
        {
            Button key = root.Q<Button>(target.buttonName);
            Action keyHandler = () => BeginRebind(key);
            key.clicked += keyHandler;
            buttonHandlers.Add(key, keyHandler);
            targetsByButton.Add(key, target);

            Button reset = root.Q<Button>(target.buttonName.Replace("Button", "ResetButton"));
            if (reset == null) continue;
            Action resetHandler = () => ResetBinding(target);
            reset.clicked += resetHandler;
            buttonHandlers.Add(reset, resetHandler);
        }

        backButton = root.Q<Button>("ControlsBackButton");
        if (backButton != null) backButton.clicked += Return;
        resetAllButton = root.Q<Button>("ResetBindingsButton");
        if (resetAllButton != null) resetAllButton.clicked += ResetAll;
    }

    private void UnbindUi()
    {
        foreach (KeyValuePair<Button, Action> entry in buttonHandlers)
            if (entry.Key != null) entry.Key.clicked -= entry.Value;
        buttonHandlers.Clear();
        targetsByButton.Clear();
        if (backButton != null) backButton.clicked -= Return;
        if (resetAllButton != null) resetAllButton.clicked -= ResetAll;
        backButton = null;
        resetAllButton = null;
        statusLabel = null;
    }

    private void BeginRebind(Button button)
    {
        if (activeRebind != null || !targetsByButton.TryGetValue(button, out BindingTarget target)) return;
        if (!TryGetBinding(target, out InputAction action, out int index)) { SetStatus("This binding is unavailable."); return; }
        activeAction = action;
        activeBindingIndex = index;
        previousOverridePath = action.bindings[index].overridePath;
        button.text = "PRESS A KEY";
        SetStatus("Press a key for " + FormatActionName(target) + " — Esc cancels");
        action.Disable();
        activeRebind = action.PerformInteractiveRebinding(index).WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation => FinishRebind(operation, false))
            .OnComplete(operation => FinishRebind(operation, true));
        activeRebind.Start();
    }

    private void FinishRebind(InputActionRebindingExtensions.RebindingOperation operation, bool completed)
    {
        operation.Dispose();
        InputAction action = activeAction;
        int index = activeBindingIndex;
        activeRebind = null;
        activeAction = null;
        activeBindingIndex = -1;
        if (action != null && index >= 0)
        {
            if (completed && HasConflict(action, index)) { RestorePreviousOverride(action, index); SetStatus("That key is already assigned. Binding unchanged."); }
            else if (completed) { persistence?.SaveOverrides(); SetStatus("Binding saved."); }
            else SetStatus("Rebind cancelled.");
            action.Enable();
        }
        previousOverridePath = null;
        Refresh();
    }

    private bool HasConflict(InputAction source, int sourceIndex)
    {
        string path = source.bindings[sourceIndex].effectivePath;
        if (string.IsNullOrWhiteSpace(path)) return false;
        foreach (InputActionMap map in playerInput.actions.actionMaps)
        foreach (InputAction action in map.actions)
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action == source && i == sourceIndex) continue;
            InputBinding binding = action.bindings[i];
            if (!binding.isComposite && !string.IsNullOrWhiteSpace(binding.effectivePath) && string.Equals(binding.effectivePath, path, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private void ResetBinding(BindingTarget target)
    {
        if (activeRebind != null || !TryGetBinding(target, out InputAction action, out int index)) return;
        action.RemoveBindingOverride(index);
        persistence?.SaveOverrides();
        SetStatus(FormatActionName(target) + " restored to default.");
        Refresh();
    }

    private void RestorePreviousOverride(InputAction action, int index)
    {
        if (string.IsNullOrEmpty(previousOverridePath)) action.RemoveBindingOverride(index);
        else action.ApplyBindingOverride(index, new InputBinding { overridePath = previousOverridePath });
    }

    private void CancelActiveRebind()
    {
        if (activeRebind == null) return;
        activeRebind.Dispose();
        activeRebind = null;
        if (activeAction != null) activeAction.Enable();
        activeAction = null;
        activeBindingIndex = -1;
        previousOverridePath = null;
    }

    private void ResetAll()
    {
        if (activeRebind != null) return;
        persistence?.ResetOverrides();
        SetStatus("Controls restored to defaults.");
        Refresh();
    }

    private void Return()
    {
        if (pauseController != null) { pauseController.ReturnToPauseMenu(); return; }
        gameObject.SetActive(false);
        if (returnDocument == null) return;
        MainMenuController mainMenu = returnDocument.GetComponent<MainMenuController>();
        if (mainMenu != null) mainMenu.ShowMenu();
        else returnDocument.enabled = true;
    }

    private void RefreshButton(Button button, BindingTarget target)
    {
        if (TryGetBinding(target, out InputAction action, out int index))
        {
            button.SetEnabled(activeRebind == null);
            button.text = action.GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontUseShortDisplayNames).ToUpperInvariant();
        }
        else { button.SetEnabled(false); button.text = "UNAVAILABLE"; }
    }

    private bool TryGetBinding(BindingTarget target, out InputAction action, out int index)
    {
        action = playerInput.actions.FindAction(target.mapName + "/" + target.actionName, false);
        index = -1;
        if (action == null) return false;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            bool matches = string.IsNullOrEmpty(target.partName) ? !binding.isComposite && !binding.isPartOfComposite : binding.isPartOfComposite && string.Equals(binding.name, target.partName, StringComparison.OrdinalIgnoreCase);
            if (matches && binding.effectivePath.StartsWith(target.devicePrefix, StringComparison.OrdinalIgnoreCase)) { index = i; return true; }
        }
        return false;
    }

    private void SetStatus(string message) { if (statusLabel != null) statusLabel.text = message; }
    private static string FormatActionName(BindingTarget target) => string.IsNullOrEmpty(target.partName) ? target.actionName : target.actionName + " " + target.partName;
}
