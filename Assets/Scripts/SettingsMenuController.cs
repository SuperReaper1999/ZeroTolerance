using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class SettingsMenuController : MonoBehaviour
{
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string SfxVolumeKey = "Settings.SfxVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string ScreenShakeKey = "Settings.ScreenShake";
    private const string ResolutionIndexKey = "Settings.ResolutionIndex";

    [SerializeField] private UIDocument settingsDocument;
    [SerializeField] private PauseController pauseController;

    private VisualElement draggedSlider;
    private Resolution[] resolutions;
    private int resolutionIndex;
    private bool fullscreen;
    private bool screenShake;

    private void Start()
    {
        VisualElement root = settingsDocument.rootVisualElement;
        SetupSlider(root.Q<VisualElement>("MasterVolumeSlider"), MasterVolumeKey, PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f));
        SetupSlider(root.Q<VisualElement>("MusicVolumeSlider"), MusicVolumeKey, PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f));
        SetupSlider(root.Q<VisualElement>("SfxVolumeSlider"), SfxVolumeKey, PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f));
        AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);

        fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        screenShake = PlayerPrefs.GetInt(ScreenShakeKey, 1) == 1;
        Button fullscreenToggle = root.Q<Button>("FullscreenToggle");
        Button screenShakeToggle = root.Q<Button>("ScreenShakeToggle");
        fullscreenToggle.clicked += ToggleFullscreen;
        screenShakeToggle.clicked += ToggleScreenShake;
        UpdateToggle(fullscreenToggle, fullscreen);
        UpdateToggle(screenShakeToggle, screenShake);

        resolutions = Screen.resolutions;
        resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionIndexKey, FindCurrentResolution()), 0, Mathf.Max(0, resolutions.Length - 1));
        root.Q<Button>("ResolutionPreviousButton").clicked += PreviousResolution;
        root.Q<Button>("ResolutionNextButton").clicked += NextResolution;
        UpdateResolutionLabel();
        root.Q<Button>("BackButton").clicked += GoBack;
    }

    private void SetupSlider(VisualElement slider, string key, float value)
    {
        SetSlider(slider, key, value);
        slider.RegisterCallback<PointerDownEvent>(evt =>
        {
            draggedSlider = slider;
            slider.CapturePointer(evt.pointerId);
            SetSliderFromPointer(slider, key, evt.position);
            evt.StopPropagation();
        });
        slider.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (draggedSlider == slider) SetSliderFromPointer(slider, key, evt.position);
        });
        slider.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (draggedSlider != slider) return;
            SetSliderFromPointer(slider, key, evt.position);
            slider.ReleasePointer(evt.pointerId);
            draggedSlider = null;
        });
    }

    private void SetSliderFromPointer(VisualElement slider, string key, Vector2 worldPosition)
    {
        float value = Mathf.Clamp01(slider.WorldToLocal(worldPosition).x / slider.resolvedStyle.width);
        SetSlider(slider, key, value);
    }

    private void SetSlider(VisualElement slider, string key, float value)
    {
        value = Mathf.Clamp01(value);
        slider.Q<VisualElement>("SliderKnob").style.left = new Length(Mathf.Lerp(3f, 87f, value), LengthUnit.Percent);
        PlayerPrefs.SetFloat(key, value);
        if (key == MasterVolumeKey) AudioListener.volume = value;
        PlayerPrefs.Save();
    }

    private void ToggleFullscreen()
    {
        fullscreen = !fullscreen;
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
        Screen.fullScreen = fullscreen;
        UpdateToggle(settingsDocument.rootVisualElement.Q<Button>("FullscreenToggle"), fullscreen);
    }

    private void ToggleScreenShake()
    {
        screenShake = !screenShake;
        PlayerPrefs.SetInt(ScreenShakeKey, screenShake ? 1 : 0);
        PlayerPrefs.Save();
        UpdateToggle(settingsDocument.rootVisualElement.Q<Button>("ScreenShakeToggle"), screenShake);
    }

    private static void UpdateToggle(Button toggle, bool enabled)
    {
        toggle.EnableInClassList("toggle-on", enabled);
        toggle.EnableInClassList("toggle-off", !enabled);
    }

    private int FindCurrentResolution()
    {
        for (int i = 0; i < resolutions.Length; i++)
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height) return i;
        return 0;
    }

    private void PreviousResolution() => ChangeResolution(-1);
    private void NextResolution() => ChangeResolution(1);

    private void ChangeResolution(int direction)
    {
        if (resolutions.Length == 0) return;
        resolutionIndex = (resolutionIndex + direction + resolutions.Length) % resolutions.Length;
        PlayerPrefs.SetInt(ResolutionIndexKey, resolutionIndex);
        PlayerPrefs.Save();
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
        UpdateResolutionLabel();
    }

    private void UpdateResolutionLabel()
    {
        Label label = settingsDocument.rootVisualElement.Q<Label>("ResolutionLabel");
        if (resolutions.Length == 0) { label.text = Screen.width + " x " + Screen.height; return; }
        Resolution resolution = resolutions[resolutionIndex];
        label.text = resolution.width + " x " + resolution.height;
    }

    private void GoBack()
    {
        if (pauseController != null) { pauseController.ReturnToPauseMenu(); return; }
        SceneManager.LoadScene("MainMenu");
    }
}