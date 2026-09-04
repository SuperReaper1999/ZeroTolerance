using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }
    [SerializeField] private UIDocument promptDocument;
    private Label promptLabel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (promptDocument == null) return;
        promptLabel = promptDocument.rootVisualElement.Q<Label>("InteractionPromptLabel");
        Hide();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Show(string prompt)
    {
        if (promptDocument == null || promptLabel == null) return;
        promptLabel.text = "[E] " + prompt;
        promptDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (promptDocument != null) promptDocument.rootVisualElement.style.display = DisplayStyle.None;
    }
}