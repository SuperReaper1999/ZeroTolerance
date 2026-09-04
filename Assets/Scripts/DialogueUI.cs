using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [SerializeField] private UIDocument dialogueDocument;
    private readonly List<Button> choiceButtons = new();
    private readonly List<object> activeChoices = new();
    private Component activeRunner;
    private Label speakerLabel;
    private Label lineLabel;
    private VisualElement choicesRoot;
    private Button nextButton;
    private bool isOpen;
    private bool inputGateHeld;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BindUi();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        ReleaseInputGate();
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            activeRunner?.SendMessage("CancelConversation", SendMessageOptions.DontRequireReceiver);
    }

    // Deliberately accepts Component: DialogueRunner passes itself unchanged, while this UI stays portable in the 2D UI sandbox.
    public void DisplayLine(Component runner, string speakerName, string line)
    {
        activeRunner = runner;
        AcquireInputGate();
        SetVisible(true);
        speakerLabel.text = speakerName ?? string.Empty;
        lineLabel.text = line ?? string.Empty;
        nextButton.style.display = DisplayStyle.Flex;
        choicesRoot.style.display = DisplayStyle.None;
        HideChoices();
    }

    // Generic signature accepts List<DialogueChoice> directly when dropped into the 3D project, without a duplicate data model.
    public void DisplayChoices<T>(Component runner, List<T> choices)
    {
        activeRunner = runner;
        AcquireInputGate();
        SetVisible(true);
        nextButton.style.display = DisplayStyle.None;
        choicesRoot.style.display = DisplayStyle.Flex;
        activeChoices.Clear();
        if (choices != null) foreach (T choice in choices) activeChoices.Add(choice);

        for (int i = 0; i < choiceButtons.Count; i++)
        {
            bool visible = i < activeChoices.Count;
            choiceButtons[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible) choiceButtons[i].text = ReadStringMember(activeChoices[i], "choiceText", "CHOICE");
        }
    }

    public void HideDialogue()
    {
        SetVisible(false);
        activeChoices.Clear();
        activeRunner = null;
        ReleaseInputGate();
    }

    private void BindUi()
    {
        if (dialogueDocument == null) return;
        VisualElement root = dialogueDocument.rootVisualElement;
        speakerLabel = root.Q<Label>("DialogueSpeakerLabel");
        lineLabel = root.Q<Label>("DialogueLineLabel");
        choicesRoot = root.Q<VisualElement>("DialogueChoices");
        nextButton = root.Q<Button>("DialogueNextButton");
        if (nextButton != null) nextButton.clicked += Advance;
        for (int i = 0; i < 6; i++)
        {
            Button button = root.Q<Button>("DialogueChoice" + i);
            if (button == null) continue;
            int index = i;
            button.clicked += () => SelectChoice(index);
            choiceButtons.Add(button);
        }
    }

    private void SetVisible(bool visible)
    {
        isOpen = visible;
        if (dialogueDocument != null)
            dialogueDocument.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void HideChoices()
    {
        foreach (Button button in choiceButtons) button.style.display = DisplayStyle.None;
    }

    private void Advance() => activeRunner?.SendMessage("AdvanceLine", SendMessageOptions.DontRequireReceiver);

    private void SelectChoice(int index)
    {
        if (activeRunner == null || index < 0 || index >= activeChoices.Count) return;
        object choice = activeChoices[index];
        MethodInfo select = activeRunner.GetType().GetMethod("SelectChoice", BindingFlags.Instance | BindingFlags.Public, null, new[] { choice.GetType() }, null);
        select?.Invoke(activeRunner, new[] { choice });
    }

    private void AcquireInputGate()
    {
        if (inputGateHeld) return;
        InvokeUiInputGate("Enter");
        inputGateHeld = true;
    }

    private void ReleaseInputGate()
    {
        if (!inputGateHeld) return;
        InvokeUiInputGate("Exit");
        inputGateHeld = false;
    }

    private static void InvokeUiInputGate(string method)
    {
        Type gate = FindRuntimeType("UIInputGate");
        gate?.GetMethod(method, BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
    }

    private static Type FindRuntimeType(string name)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(name);
            if (type != null) return type;
        }
        return null;
    }

    private static string ReadStringMember(object source, string memberName, string fallback)
    {
        if (source == null) return fallback;
        Type type = source.GetType();
        FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (field?.GetValue(source) is string text) return text;
        PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(source) as string ?? fallback;
    }
}