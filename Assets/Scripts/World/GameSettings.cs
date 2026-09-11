using UnityEngine;

/// <summary>
/// Persisted (PlayerPrefs) non-audio options: video, controls, gameplay toggles.
/// Video changes are applied immediately via ApplyVideo(); Controls/Gameplay
/// values are just read by whoever needs them (CameraFollow, GameUIController).
/// </summary>
public static class GameSettings
{
    private const string FullscreenKey = "opt_fullscreen";
    private const string QualityKey = "opt_quality";
    private const string ResolutionKey = "opt_resolution";
    private const string SensitivityKey = "opt_sensitivity";
    private const string InvertYKey = "opt_invert_y";
    private const string ShowToastsKey = "opt_show_toasts";

    private static bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
    private static int qualityLevel = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
    private static int resolutionIndex = PlayerPrefs.GetInt(ResolutionKey, -1); // -1 = current/native, resolved on first use
    private static float mouseSensitivity = PlayerPrefs.GetFloat(SensitivityKey, 3f);
    private static bool invertY = PlayerPrefs.GetInt(InvertYKey, 0) == 1;
    private static bool showToasts = PlayerPrefs.GetInt(ShowToastsKey, 1) == 1;

    public static bool Fullscreen => fullscreen;
    public static int QualityLevel => qualityLevel;
    public static int ResolutionIndex => resolutionIndex;
    public static float MouseSensitivity => mouseSensitivity;
    public static bool InvertY => invertY;
    public static bool ShowToasts => showToasts;

    public static event System.Action Changed;

    public static void SetFullscreen(bool value)
    {
        fullscreen = value;
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        ApplyVideo();
        Changed?.Invoke();
    }

    public static void SetQualityLevel(int value)
    {
        qualityLevel = Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        PlayerPrefs.SetInt(QualityKey, qualityLevel);
        ApplyVideo();
        Changed?.Invoke();
    }

    public static void SetResolutionIndex(int value)
    {
        resolutionIndex = Mathf.Clamp(value, 0, Mathf.Max(0, Screen.resolutions.Length - 1));
        PlayerPrefs.SetInt(ResolutionKey, resolutionIndex);
        ApplyVideo();
        Changed?.Invoke();
    }

    public static void SetMouseSensitivity(float value)
    {
        mouseSensitivity = Mathf.Clamp(value, 0.5f, 10f);
        PlayerPrefs.SetFloat(SensitivityKey, mouseSensitivity);
        Changed?.Invoke();
    }

    public static void SetInvertY(bool value)
    {
        invertY = value;
        PlayerPrefs.SetInt(InvertYKey, value ? 1 : 0);
        Changed?.Invoke();
    }

    public static void SetShowToasts(bool value)
    {
        showToasts = value;
        PlayerPrefs.SetInt(ShowToastsKey, value ? 1 : 0);
        Changed?.Invoke();
    }

    /// <summary>Pushes Fullscreen/Quality/Resolution to Unity. Safe to call anytime (e.g. on boot).</summary>
    public static void ApplyVideo()
    {
        QualitySettings.SetQualityLevel(qualityLevel, true);

        var resolutions = Screen.resolutions;
        if (resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
        {
            var r = resolutions[resolutionIndex];
            Screen.SetResolution(r.width, r.height, fullscreen);
        }
        else
        {
            Screen.fullScreen = fullscreen;
        }
    }
}
