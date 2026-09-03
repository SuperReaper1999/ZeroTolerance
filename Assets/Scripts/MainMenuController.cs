using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument mainMenuDocument;
    private Button playButton;
    private Button settingsButton;

    private void Start()
    {
        playButton = mainMenuDocument.rootVisualElement.Q<Button>("PlayButton");
        settingsButton = mainMenuDocument.rootVisualElement.Q<Button>("SettingsButton");
        if (playButton != null) playButton.clicked += OpenLevelSelect;
        if (settingsButton != null) settingsButton.clicked += OpenSettings;
    }

    private void OnDestroy()
    {
        if (playButton != null) playButton.clicked -= OpenLevelSelect;
        if (settingsButton != null) settingsButton.clicked -= OpenSettings;
    }

    private void OpenLevelSelect() => SceneManager.LoadScene("LevelSelect");
    private void OpenSettings() => SceneManager.LoadScene("Settings");
}