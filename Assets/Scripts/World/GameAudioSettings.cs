using UnityEngine;

/// <summary>
/// Persisted (PlayerPrefs) audio mix levels the options menu edits: Master,
/// Music, SFX, Ambience - all 0..1. CombatAudio (one-shot SFX), MusicManager
/// (the looping tracks) and AmbienceController (the looping environment beds)
/// all read this instead of hard-coding their own volumes, so one slider set
/// controls the whole game's mix. This is the entire "audio manager" - no
/// AudioMixer asset, no routing graph; every consumer just multiplies its own
/// base volume by Master * (its category) each time it plays or ticks.
/// </summary>
public static class GameAudioSettings
{
    private const string MasterKey = "opt_audio_master";
    private const string MusicKey = "opt_audio_music";
    private const string SfxKey = "opt_audio_sfx";
    private const string AmbienceKey = "opt_audio_ambience";

    private static float master = PlayerPrefs.GetFloat(MasterKey, 1f);
    private static float music = PlayerPrefs.GetFloat(MusicKey, 1f);
    private static float sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
    private static float ambience = PlayerPrefs.GetFloat(AmbienceKey, 1f);

    public static float Master => master;
    public static float Music => music;
    public static float Sfx => sfx;
    public static float Ambience => ambience;

    /// <summary>Fired whenever a level changes, so playing loops (music) can re-apply live.</summary>
    public static event System.Action Changed;

    public static void SetMaster(float value)
    {
        master = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterKey, master);
        Changed?.Invoke();
    }

    public static void SetMusic(float value)
    {
        music = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, music);
        Changed?.Invoke();
    }

    public static void SetSfx(float value)
    {
        sfx = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, sfx);
        Changed?.Invoke();
    }

    public static void SetAmbience(float value)
    {
        ambience = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(AmbienceKey, ambience);
        Changed?.Invoke();
    }
}
