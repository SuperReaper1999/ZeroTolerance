using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shared New Input System binding-override persistence. This deliberately uses
/// the same preference key and JSON format as the 3D project.
/// </summary>
[DisallowMultipleComponent]
public sealed class InputBindingPersistence : MonoBehaviour
{
    public const string PreferenceKey = "settings.inputBindingOverrides";

    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        LoadOverrides();
    }

    public void LoadOverrides()
    {
        if (playerInput == null || playerInput.actions == null) return;
        string json = PlayerPrefs.GetString(PreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return;

        try
        {
            playerInput.actions.LoadBindingOverridesFromJson(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[InputBindingPersistence] Ignoring invalid saved binding overrides: {exception.Message}", this);
            PlayerPrefs.DeleteKey(PreferenceKey);
            PlayerPrefs.Save();
        }
    }

    public void SaveOverrides()
    {
        if (playerInput == null || playerInput.actions == null) return;
        string json = playerInput.actions.SaveBindingOverridesAsJson();
        if (string.IsNullOrWhiteSpace(json) || json == "[]") PlayerPrefs.DeleteKey(PreferenceKey);
        else PlayerPrefs.SetString(PreferenceKey, json);
        PlayerPrefs.Save();
    }

    public void ResetOverrides()
    {
        if (playerInput == null || playerInput.actions == null) return;
        playerInput.actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(PreferenceKey);
        PlayerPrefs.Save();
    }
}
