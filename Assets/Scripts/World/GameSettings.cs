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
    private const string VSyncKey = "opt_vsync";
    private const string SensitivityKey = "opt_sensitivity";
    private const string InvertYKey = "opt_invert_y";
    private const string ShowToastsKey = "opt_show_toasts";
    private const string ShakeIntensityKey = "opt_shake_intensity";
    private const string ReduceCameraMotionKey = "opt_reduce_camera_motion";
    private const string HoldToBlockKey = "opt_hold_to_block";
    private const string UIScaleKey = "opt_ui_scale";

    private static bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
    private static int qualityLevel = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
    private static int resolutionIndex = PlayerPrefs.GetInt(ResolutionKey, -1); // -1 = current/native, resolved on first use
    private static bool vSyncEnabled = PlayerPrefs.GetInt(VSyncKey, 1) == 1;
    private static float mouseSensitivity = PlayerPrefs.GetFloat(SensitivityKey, 3f);
    private static bool invertY = PlayerPrefs.GetInt(InvertYKey, 0) == 1;
    private static bool showToasts = PlayerPrefs.GetInt(ShowToastsKey, 1) == 1;
    private static float shakeIntensity = PlayerPrefs.GetFloat(ShakeIntensityKey, 1f);
    private static bool reduceCameraMotion = PlayerPrefs.GetInt(ReduceCameraMotionKey, 0) == 1;
    private static bool holdToBlock = PlayerPrefs.GetInt(HoldToBlockKey, 1) == 1;
    private static float uiScale = PlayerPrefs.GetFloat(UIScaleKey, 1f);

    public static bool Fullscreen => fullscreen;
    public static int QualityLevel => qualityLevel;
    public static int ResolutionIndex => resolutionIndex;
    public static bool VSyncEnabled => vSyncEnabled;
    public static float MouseSensitivity => mouseSensitivity;
    public static bool InvertY => invertY;
    public static bool ShowToasts => showToasts;
    public static float ShakeIntensity => shakeIntensity;
    public static bool ReduceCameraMotion => reduceCameraMotion;
    public static bool HoldToBlock => holdToBlock;
    public static float UIScale => uiScale;

    public static event System.Action Changed;

    /// <summary>Pushes the saved video prefs to Unity once at process launch. Without this,
    /// a saved Fullscreen/Quality/Resolution/VSync choice only takes effect once the player
    /// touches Options that session - it wouldn't actually apply on a fresh launch.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnLaunch() => ApplyVideo();

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

    public static void SetVSyncEnabled(bool value)
    {
        vSyncEnabled = value;
        PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0);
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

    public static void SetShakeIntensity(float value)
    {
        shakeIntensity = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(ShakeIntensityKey, shakeIntensity);
        Changed?.Invoke();
    }

    public static void SetReduceCameraMotion(bool value)
    {
        reduceCameraMotion = value;
        PlayerPrefs.SetInt(ReduceCameraMotionKey, value ? 1 : 0);
        Changed?.Invoke();
    }

    public static void SetHoldToBlock(bool value)
    {
        holdToBlock = value;
        PlayerPrefs.SetInt(HoldToBlockKey, value ? 1 : 0);
        Changed?.Invoke();
    }

    public static void SetUIScale(float value)
    {
        uiScale = Mathf.Clamp(value, 0.85f, 1.25f);
        PlayerPrefs.SetFloat(UIScaleKey, uiScale);
        Changed?.Invoke();
    }

    /// <summary>Pushes Fullscreen/Quality/Resolution/VSync to Unity. Safe to call anytime (e.g. on boot).</summary>
    public static void ApplyVideo()
    {
        QualitySettings.SetQualityLevel(qualityLevel, true);

        // In a browser the page owns the canvas size, fullscreen needs a user gesture and
        // frame pacing follows requestAnimationFrame - forcing any of them at boot misbehaves.
#if !UNITY_WEBGL
        QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;

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
#endif
    }
}
