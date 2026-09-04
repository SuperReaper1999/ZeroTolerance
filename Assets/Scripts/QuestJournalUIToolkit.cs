using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class QuestJournalUIToolkit : MonoBehaviour
{
    public static QuestJournalUIToolkit Instance { get; private set; }
    [SerializeField] private UIDocument journalDocument;

    private readonly List<Button> rows = new();
    private readonly List<string> questIds = new();
    private Label titleLabel;
    private Label metaLabel;
    private Label descriptionLabel;
    private Label objectivesLabel;
    private Label mapLabel;
    private Button trackButton;
    private object questManager;
    private string selectedQuestId;
    private bool isOpen;
    private float refreshAt;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BindUi();
        SetVisible(false);
    }

    private void Update()
    {
        if (!isOpen) return;
        if (Time.unscaledTime < refreshAt) return;
        Refresh();
        refreshAt = Time.unscaledTime + 0.5f;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Open()
    {
        SetVisible(true);
        Refresh();
    }

    public void Close() => SetVisible(false);

    public void Refresh()
    {
        questManager = FindQuestManager();
        questIds.Clear();
        if (questManager != null)
        {
            AddQuestIds("GetActiveQuestIds");
            AddQuestIds("GetCompletedQuestIds");
        }
        if (string.IsNullOrEmpty(selectedQuestId) || !questIds.Contains(selectedQuestId))
            selectedQuestId = questIds.Count > 0 ? questIds[0] : null;
        RefreshRows();
        RefreshDetails();
    }

    private void BindUi()
    {
        if (journalDocument == null) return;
        VisualElement root = journalDocument.rootVisualElement;
        titleLabel = root.Q<Label>("QuestTitleLabel");
        metaLabel = root.Q<Label>("QuestMetaLabel");
        descriptionLabel = root.Q<Label>("QuestDescriptionLabel");
        objectivesLabel = root.Q<Label>("QuestObjectivesLabel");
        mapLabel = root.Q<Label>("QuestMapLabel");
        trackButton = root.Q<Button>("QuestTrackButton");
        if (trackButton != null) trackButton.clicked += TrackSelected;
        Button closeButton = root.Q<Button>("QuestCloseButton");
        if (closeButton != null) closeButton.clicked += Close;
        for (int i = 0; i < 8; i++)
        {
            Button row = root.Q<Button>("QuestRow" + i);
            if (row == null) continue;
            int index = i;
            row.clicked += () => SelectQuest(index);
            rows.Add(row);
        }
    }

    private void SetVisible(bool visible)
    {
        isOpen = visible;
        if (journalDocument != null) journalDocument.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void AddQuestIds(string methodName)
    {
        object result = questManager.GetType().GetMethod(methodName)?.Invoke(questManager, null);
        if (result is not IEnumerable enumerable) return;
        foreach (object id in enumerable)
            if (id is string questId && !questIds.Contains(questId)) questIds.Add(questId);
    }

    private void RefreshRows()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            bool visible = i < questIds.Count;
            Button row = rows[i];
            row.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            row.EnableInClassList("quest-selected", visible && questIds[i] == selectedQuestId);
            if (visible) row.text = GetQuestTitle(questIds[i]) + "  " + GetQuestState(questIds[i]).ToUpperInvariant();
        }
    }

    private void RefreshDetails()
    {
        if (string.IsNullOrEmpty(selectedQuestId) || questManager == null)
        {
            titleLabel.text = "SELECT A QUEST";
            metaLabel.text = string.Empty;
            descriptionLabel.text = "No active or completed quests are available.";
            objectivesLabel.text = string.Empty;
            mapLabel.text = "NO TRACKED DESTINATION";
            trackButton?.SetEnabled(false);
            return;
        }

        object definition = questManager.GetType().GetMethod("GetQuestDefinition")?.Invoke(questManager, new object[] { selectedQuestId });
        titleLabel.text = ReadMember(definition, "Title", selectedQuestId);
        string category = ReadMember(definition, "Category", "QUEST");
        string state = GetQuestState(selectedQuestId);
        metaLabel.text = category.ToUpperInvariant() + "  •  " + state.ToUpperInvariant();
        descriptionLabel.text = ReadMember(definition, "Description", "");
        objectivesLabel.text = BuildObjectives();
        bool tracked = ReadMember(questManager, "TrackedQuestId", "") == selectedQuestId;
        trackButton?.SetEnabled(state != "Completed");
        if (trackButton != null) trackButton.text = tracked ? "TRACKED" : "TRACK QUEST";
        mapLabel.text = tracked ? GetTrackedDestinationLabel() : "SELECT TRACK QUEST TO SHOW DESTINATION";
    }

    private string BuildObjectives()
    {
        object result = questManager.GetType().GetMethod("GetCurrentObjectives")?.Invoke(questManager, new object[] { selectedQuestId });
        if (result is not IEnumerable objectives) return string.Empty;
        StringBuilder text = new();
        foreach (object objective in objectives)
        {
            string id = ReadMember(objective, "objectiveId", "");
            string description = ReadMember(objective, "description", "Objective");
            int target = ReadIntMember(objective, "targetCount", 1);
            object progress = questManager.GetType().GetMethod("GetObjectiveProgress")?.Invoke(questManager, new object[] { selectedQuestId, id });
            if (text.Length > 0) text.Append('\n');
            text.Append("• ").Append(description).Append("  ").Append(progress ?? 0).Append('/').Append(target);
        }
        return text.ToString();
    }

    private void SelectQuest(int index)
    {
        if (index < 0 || index >= questIds.Count) return;
        selectedQuestId = questIds[index];
        RefreshRows();
        RefreshDetails();
    }

    private void TrackSelected()
    {
        if (questManager == null || string.IsNullOrEmpty(selectedQuestId)) return;
        questManager.GetType().GetMethod("SetTrackedQuest")?.Invoke(questManager, new object[] { selectedQuestId });
        Refresh();
    }

    private string GetQuestTitle(string questId)
    {
        object definition = questManager?.GetType().GetMethod("GetQuestDefinition")?.Invoke(questManager, new object[] { questId });
        return ReadMember(definition, "Title", questId);
    }

    private string GetQuestState(string questId)
    {
        object state = questManager?.GetType().GetMethod("GetQuestState")?.Invoke(questManager, new object[] { questId });
        return state?.ToString() ?? "Unknown";
    }

    private string GetTrackedDestinationLabel()
    {
        MethodInfo targetMethod = questManager.GetType().GetMethod("TryGetTrackedTarget");
        if (targetMethod == null) return "TRACKED QUEST";
        object[] args = { null, false };
        bool hasTarget = targetMethod.Invoke(questManager, args) is bool result && result;
        if (!hasTarget) return "TRACKED QUEST HAS NO MAP TARGET";
        return (args[1] is bool turnIn && turnIn) ? "RETURN TO QUEST GIVER" : "TRACKED DESTINATION";
    }

    private static object FindQuestManager()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType("QuestManager");
            PropertyInfo instance = type?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            object manager = instance?.GetValue(null);
            if (manager != null) return manager;
        }
        return null;
    }

    private static string ReadMember(object source, string memberName, string fallback)
    {
        if (source == null) return fallback;
        Type type = source.GetType();
        PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (property?.GetValue(source) is object propertyValue) return propertyValue.ToString();
        FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
        return field?.GetValue(source)?.ToString() ?? fallback;
    }

    private static int ReadIntMember(object source, string memberName, int fallback)
    {
        if (source == null) return fallback;
        FieldInfo field = source.GetType().GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
        return field?.GetValue(source) is int value ? value : fallback;
    }
}