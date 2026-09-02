using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class PauseController : MonoBehaviour
{
    [SerializeField] private UIDocument pauseMenuDocument;

    private bool isPaused;

    private void Awake() => pauseMenuDocument.enabled = false;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    private void OnDisable() => Time.timeScale = 1f;

    public void TogglePause() => SetPaused(!isPaused);
    public void Pause() => SetPaused(true);
    public void Resume() => SetPaused(false);

    private void SetPaused(bool paused)
    {
        if (isPaused == paused) return;
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        pauseMenuDocument.enabled = paused;
    }
}