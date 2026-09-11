using UnityEngine;

/// <summary>
/// Persisted (PlayerPrefs) audio mix levels the options menu edits: Master,
/// Music, SFX - all 0..1. CombatAudio (one-shot SFX) and MusicManager (the two
/// looping tracks) both read this instead of hard-coding their own volumes, so
/// one slider set controls the whole game's mix.
/// </summary>
public static class GameAudioSettings
{
    private const string MasterKey = "opt_audio_master";
    private const string MusicKey = "opt_audio_music";
    private const string SfxKey = "opt_audio_sfx";

    private static float master = PlayerPrefs.GetFloat(MasterKey, 1f);
    private static float music = PlayerPrefs.GetFloat(MusicKey, 1f);
    private static float sfx = PlayerPrefs.GetFloat(SfxKey, 1f);

    public static float Master => master;
    public static float Music => music;
    public static float Sfx => sfx;

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
}
