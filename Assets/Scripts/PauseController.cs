using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class PauseController : MonoBehaviour
{
    [SerializeField] private UIDocument pauseMenuDocument;
    [SerializeField] private UIDocument pauseSettingsDocument;
    [SerializeField] private UIDocument pauseControlsDocument;

    private bool isPaused;
    private Button resumeButton;
    private Button settingsButton;
    private Button controlsButton;
    private Button restartLevelButton;
    private Button mainMenuButton;

    private void Awake()
    {
        Time.timeScale = 1f;
        isPaused = false;
        pauseMenuDocument.enabled = false;
        if (pauseSettingsDocument != null) pauseSettingsDocument.gameObject.SetActive(false);
        if (pauseControlsDocument != null) pauseControlsDocument.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isPaused) BindButtons();
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if ((pauseSettingsDocument != null && pauseSettingsDocument.gameObject.activeSelf) ||
            (pauseControlsDocument != null && pauseControlsDocument.gameObject.activeSelf))
            ReturnToPauseMenu();
        else TogglePause();
    }

    private void OnDisable() => Time.timeScale = 1f;
    public void TogglePause() => SetPaused(!isPaused);
    public void Pause() => SetPaused(true);
    public void Resume() => SetPaused(false);

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OpenMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OpenSettings()
    {
        if (!isPaused || pauseSettingsDocument == null) return;
        UnbindButtons();
        pauseMenuDocument.enabled = false;
        if (pauseControlsDocument != null) pauseControlsDocument.gameObject.SetActive(false);
        pauseSettingsDocument.gameObject.SetActive(true);
    }

    public void OpenControls()
    {
        if (!isPaused || pauseControlsDocument == null) return;
        UnbindButtons();
        pauseMenuDocument.enabled = false;
        if (pauseSettingsDocument != null) pauseSettingsDocument.gameObject.SetActive(false);
        pauseControlsDocument.gameObject.SetActive(true);
        pauseControlsDocument.GetComponent<ControlsMenuController>()?.Refresh();
    }

    public void ReturnToPauseMenu()
    {
        if (!isPaused) return;
        if (pauseSettingsDocument != null) pauseSettingsDocument.gameObject.SetActive(false);
        if (pauseControlsDocument != null) pauseControlsDocument.gameObject.SetActive(false);
        pauseMenuDocument.enabled = true;
        BindButtons();
    }

    private void SetPaused(bool paused)
    {
        if (isPaused == paused) return;
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        UnbindButtons();
        if (pauseSettingsDocument != null) pauseSettingsDocument.gameObject.SetActive(false);
        if (pauseControlsDocument != null) pauseControlsDocument.gameObject.SetActive(false);
        pauseMenuDocument.enabled = paused;
    }

    private void BindButtons()
    {
        if (!pauseMenuDocument.enabled) return;
        VisualElement root = pauseMenuDocument.rootVisualElement;
        Button resume = root.Q<Button>("ResumeButton");
        Button settings = root.Q<Button>("SettingsButton");
        Button controls = root.Q<Button>("ControlsButton");
        Button restart = root.Q<Button>("RestartLevelButton");
        Button mainMenu = root.Q<Button>("MainMenuButton");
        if (resume == resumeButton && settings == settingsButton && controls == controlsButton && restart == restartLevelButton && mainMenu == mainMenuButton) return;
        UnbindButtons();
        resumeButton = resume;
        settingsButton = settings;
        controlsButton = controls;
        restartLevelButton = restart;
        mainMenuButton = mainMenu;
        if (resumeButton != null) { resumeButton.clicked += Resume; resumeButton.Focus(); }
        if (settingsButton != null) settingsButton.clicked += OpenSettings;
        if (controlsButton != null) controlsButton.clicked += OpenControls;
        if (restartLevelButton != null) restartLevelButton.clicked += RestartLevel;
        if (mainMenuButton != null) mainMenuButton.clicked += OpenMainMenu;
    }

    private void UnbindButtons()
    {
        if (resumeButton != null) resumeButton.clicked -= Resume;
        if (settingsButton != null) settingsButton.clicked -= OpenSettings;
        if (controlsButton != null) controlsButton.clicked -= OpenControls;
        if (restartLevelButton != null) restartLevelButton.clicked -= RestartLevel;
        if (mainMenuButton != null) mainMenuButton.clicked -= OpenMainMenu;
        resumeButton = null;
        settingsButton = null;
        controlsButton = null;
        restartLevelButton = null;
        mainMenuButton = null;
    }
}
