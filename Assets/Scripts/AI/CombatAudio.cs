using UnityEngine;

/// <summary>
/// One-shot SFX player, same idea as CombatVfx: every script keeps its own
/// serialized AudioClip reference and calls this; a null clip is a silent no-op.
///
/// Volumes are kept deliberately conservative (callers pass ~0.35-0.6) and
/// world sounds are always spatial (3D, distance falloff) so nothing blasts at
/// full volume regardless of where the camera is. On top of that, every call is
/// scaled by GameAudioSettings.Master * .Sfx - the options menu's SFX slider.
/// </summary>
public static class CombatAudio
{
    private static float Mix => GameAudioSettings.Master * GameAudioSettings.Sfx;

    /// <summary>A positioned, spatial (3D, distance-attenuated) one-shot.</summary>
    public static void Play(AudioClip clip, Vector3 position, float volume = 0.5f,
        float minDistance = 4f, float maxDistance = 25f)
    {
        if (clip == null)
            return;

        var go = new GameObject("SFX_" + clip.name);
        go.transform.position = position;

        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.volume = Mathf.Clamp01(volume * Mix);
        source.Play();

        Object.Destroy(go, clip.length + 0.1f);
    }

    /// <summary>A flat, non-positional one-shot for UI feedback.</summary>
    public static void PlayUI(AudioClip clip, float volume = 0.45f)
    {
        if (clip == null)
            return;

        var go = new GameObject("SFX2D_" + clip.name);
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 0f;
        source.volume = Mathf.Clamp01(volume * Mix);
        source.Play();

        Object.Destroy(go, clip.length + 0.1f);
    }
}
