using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class QuestMenuController : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private QuestJournalUIToolkit journal;
    [SerializeField] private string gameplayActionMapName = "Player";
    [SerializeField] private string uiActionMapName = "UI";
    [SerializeField] private string questLogActionName = "QuestLog";
    [SerializeField] private string cancelActionName = "Cancel";

    private InputAction questLogAction;
    private InputAction cancelAction;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (journal == null) journal = FindAnyObjectByType<QuestJournalUIToolkit>();
        CacheActions();
    }

    private void OnEnable()
    {
        if (questLogAction != null) { questLogAction.performed += ToggleFromInput; questLogAction.Enable(); }
        if (cancelAction != null) cancelAction.performed += CloseFromInput;
    }

    private void OnDisable()
    {
        if (questLogAction != null) questLogAction.performed -= ToggleFromInput;
        if (cancelAction != null) cancelAction.performed -= CloseFromInput;
    }

    public void Toggle()
    {
        if (isOpen) Close(); else Open();
    }

    public void Open()
    {
        if (isOpen || journal == null) return;
        isOpen = true;
        InvokeUiInputGate("Enter");
        questLogAction?.Enable();
        journal.Open();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        journal?.Close();
        InvokeUiInputGate("Exit");
    }

    private void CacheActions()
    {
        if (inputActions == null) return;
        InputActionMap gameplay = inputActions.FindActionMap(gameplayActionMapName, false);
        InputActionMap ui = inputActions.FindActionMap(uiActionMapName, false);
        questLogAction = gameplay?.FindAction(questLogActionName, false);
        cancelAction = ui?.FindAction(cancelActionName, false);
    }

    private void ToggleFromInput(InputAction.CallbackContext _) => Toggle();
    private void CloseFromInput(InputAction.CallbackContext _) => Close();

    private static void InvokeUiInputGate(string method)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType("UIInputGate");
            MethodInfo gateMethod = type?.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            if (gateMethod == null) continue;
            gateMethod.Invoke(null, null);
            return;
        }
    }
}