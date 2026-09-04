using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class QuestNotificationHUD : MonoBehaviour
{
    [SerializeField] private UIDocument notificationDocument;
    [SerializeField, Min(0.5f)] private float visibleSeconds = 3f;

    private object questManager;
    private readonly ArrayList subscriptions = new();
    private Label messageLabel;
    private Coroutine hideRoutine;

    private void Awake()
    {
        if (notificationDocument != null)
        {
            messageLabel = notificationDocument.rootVisualElement.Q<Label>("QuestToastMessage");
            notificationDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    private void Update()
    {
        if (questManager == null) TrySubscribe();
    }

    private void OnDestroy() => Unsubscribe();

    private void TrySubscribe()
    {
        Type managerType = FindRuntimeType("QuestManager");
        PropertyInfo instanceProperty = managerType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        object instance = instanceProperty?.GetValue(null);
        if (instance == null) return;
        questManager = instance;
        Subscribe("OnQuestStarted", nameof(HandleStarted));
        Subscribe("OnQuestObjectivesMet", nameof(HandleReady));
        Subscribe("OnQuestCompleted", nameof(HandleCompleted));
        Subscribe("OnObjectiveProgress", nameof(HandleProgress));
    }

    private void Subscribe(string eventName, string handlerName)
    {
        EventInfo eventInfo = questManager.GetType().GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
        MethodInfo handler = GetType().GetMethod(handlerName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (eventInfo == null || handler == null) return;
        Delegate callback = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, handler, false);
        if (callback == null) return;
        eventInfo.AddEventHandler(questManager, callback);
        subscriptions.Add(new Subscription(eventInfo, callback));
    }

    private void Unsubscribe()
    {
        if (questManager != null)
            foreach (Subscription subscription in subscriptions)
                subscription.eventInfo.RemoveEventHandler(questManager, subscription.callback);
        subscriptions.Clear();
        questManager = null;
    }

    private void HandleStarted(string questId) => Show("QUEST ACCEPTED", GetTitle(questId));
    private void HandleReady(string questId) => Show("READY TO TURN IN", GetTitle(questId));
    private void HandleCompleted(string questId) => Show("QUEST COMPLETED", GetTitle(questId));
    private void HandleProgress(string questId, string objectiveId, int current, int target)
    {
        if (current >= target) Show("OBJECTIVE COMPLETE", current + "/" + target);
    }

    private string GetTitle(string questId)
    {
        object definition = questManager?.GetType().GetMethod("GetQuestDefinition")?.Invoke(questManager, new object[] { questId });
        return definition?.GetType().GetProperty("Title")?.GetValue(definition) as string ?? questId;
    }

    private void Show(string title, string message)
    {
        if (notificationDocument == null || messageLabel == null) return;
        Label titleLabel = notificationDocument.rootVisualElement.Q<Label>(className: "quest-toast-title");
        if (titleLabel != null) titleLabel.text = title;
        messageLabel.text = message;
        notificationDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(visibleSeconds);
        if (notificationDocument != null) notificationDocument.rootVisualElement.style.display = DisplayStyle.None;
        hideRoutine = null;
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

    private readonly struct Subscription
    {
        public readonly EventInfo eventInfo;
        public readonly Delegate callback;
        public Subscription(EventInfo eventInfo, Delegate callback) { this.eventInfo = eventInfo; this.callback = callback; }
    }
}