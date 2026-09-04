using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument mainMenuDocument;
    [SerializeField] private UIDocument controlsDocument;

    private Button playButton;
    private Button settingsButton;
    private Button controlsButton;

    private void Start() => BindUi();

    private void OnDestroy() => UnbindUi();

    public void ShowMenu()
    {
        if (mainMenuDocument == null) return;
        mainMenuDocument.enabled = true;
        BindUi();
    }

    private void BindUi()
    {
        UnbindUi();
        if (mainMenuDocument == null) return;
        VisualElement root = mainMenuDocument.rootVisualElement;
        playButton = root.Q<Button>("PlayButton");
        settingsButton = root.Q<Button>("SettingsButton");
        controlsButton = root.Q<Button>("ControlsButton");
        if (playButton != null) playButton.clicked += OpenLevelSelect;
        if (settingsButton != null) settingsButton.clicked += OpenSettings;
        if (controlsButton != null) controlsButton.clicked += OpenControls;
    }

    private void UnbindUi()
    {
        if (playButton != null) playButton.clicked -= OpenLevelSelect;
        if (settingsButton != null) settingsButton.clicked -= OpenSettings;
        if (controlsButton != null) controlsButton.clicked -= OpenControls;
        playButton = null;
        settingsButton = null;
        controlsButton = null;
    }

    private void OpenLevelSelect() => SceneManager.LoadScene("LevelSelect");
    private void OpenSettings() => SceneManager.LoadScene("Settings");

    private void OpenControls()
    {
        if (controlsDocument == null) return;
        mainMenuDocument.enabled = false;
        controlsDocument.gameObject.SetActive(true);
        controlsDocument.GetComponent<ControlsMenuController>()?.Refresh();
    }
}