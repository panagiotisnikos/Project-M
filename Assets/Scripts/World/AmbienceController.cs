using UnityEngine;

/// <summary>
/// Environment ambience beds - deliberately separate from MusicManager's music tracks (its own
/// Ambience slider, its own AudioSources) even though the crossfade technique is copied
/// verbatim from it (poll RefugeZone/WorldRegion each frame, MoveTowards each source's volume
/// toward its target - no event wiring needed, same "avoid unnecessary complexity" reasoning).
///
/// Two beds that never play together: Outside (the default) and Refuge (RefugeZone.Main.
/// IsPlayerInside) - the refuge is deliberately insulated from the adaptive world, so it never
/// gets a region-flavour layer. On top of the Outside bed, one optional flavour layer fades in
/// depending on WorldRegion.ActiveRegion.CurrentState: Blossom or Decayed. Balanced plays no
/// extra layer at all - "restrained/neutral" is the absence of a layer, not a third audible
/// texture - matching this project's own established Balanced/Blossom/Decayed language. This is
/// the "layered ambience" the region system supports: a base bed plus at most one state layer,
/// not N independent simultaneous layers.
///
/// All four AudioClip slots are placeholder-empty by default (see the task report's Placeholder
/// Audio List) - a null clip on a looping AudioSource is simply silent, no error, exactly the
/// same "honest empty slot" pattern already used elsewhere in this project (e.g. Vestige's icon).
/// The mechanism (crossfade math, live volume) is fully functional and testable without needing
/// real audio content yet.
/// </summary>
public class AmbienceController : MonoBehaviour
{
    [Header("Sources (looped, 2D/non-spatial - see Awake)")]
    [SerializeField] private AudioSource outsideSource;
    [SerializeField] private AudioSource refugeSource;
    [SerializeField] private AudioSource blossomLayerSource;
    [SerializeField] private AudioSource decayedLayerSource;

    [Header("Peak volumes (at Master=Ambience=1)")]
    [Range(0f, 1f)] [SerializeField] private float outsideVolume = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float refugeVolume = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float blossomLayerVolume = 0.18f;
    [Range(0f, 1f)] [SerializeField] private float decayedLayerVolume = 0.18f;

    [Tooltip("Volume units per second for every crossfade - deliberately gentle, ambience " +
             "shifting is a background cue, not a snappy sting like the boss music crossfade.")]
    [SerializeField] private float fadeSpeed = 0.15f;

    private void Awake()
    {
        Prepare(outsideSource);
        Prepare(refugeSource);
        Prepare(blossomLayerSource);
        Prepare(decayedLayerSource);
    }

    private static void Prepare(AudioSource s)
    {
        if (s == null) return;
        s.loop = true;
        s.playOnAwake = false;
        s.spatialBlend = 0f; // ambience is a background bed, not a positioned point source
        s.volume = 0f;
        s.Play();
    }

    private void Update()
    {
        bool inRefuge = RefugeZone.Main != null && RefugeZone.Main.IsPlayerInside;
        RegionWorldState state = WorldRegion.ActiveRegion != null
            ? WorldRegion.ActiveRegion.CurrentState
            : RegionWorldState.Balanced;

        float mix = GameAudioSettings.Master * GameAudioSettings.Ambience;

        float targetOutside = inRefuge ? 0f : outsideVolume * mix;
        float targetRefuge = inRefuge ? refugeVolume * mix : 0f;
        float targetBlossom = (!inRefuge && state == RegionWorldState.Blossom) ? blossomLayerVolume * mix : 0f;
        float targetDecayed = (!inRefuge && state == RegionWorldState.Decayed) ? decayedLayerVolume * mix : 0f;

        float step = fadeSpeed * Time.unscaledDeltaTime;
        Fade(outsideSource, targetOutside, step);
        Fade(refugeSource, targetRefuge, step);
        Fade(blossomLayerSource, targetBlossom, step);
        Fade(decayedLayerSource, targetDecayed, step);
    }

    private static void Fade(AudioSource s, float target, float step)
    {
        if (s == null) return;
        s.volume = Mathf.MoveTowards(s.volume, target, step);
    }
}
