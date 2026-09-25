using System.Collections.Generic;
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

/// <summary>
/// The world's response to the player IS the adaptation - not a difficulty dial.
/// As the world state turns, the valley's whole mood shifts: fog colour and depth,
/// sun colour and strength, ambient light, the tint of every leaf and blade, and a
/// layer of per-state detail (drifting pollen for Blossom, spores for Decay).
///
/// Every shift runs through a fog VEIL: during a transition the mist briefly thickens
/// and pales, the far look changes behind it, then it clears to the new state - so the
/// world never visibly "swaps", it emerges from fog.
///
/// State -> visual mapping (the code enum predates the vision's names):
///   WorldState.Stable   -> BLOSSOM  (twilight meadow, warm, motes of light)
///   WorldState.Balanced -> BALANCED (cold grey-gold fog, bare forest)
///   WorldState.Decaying -> DECAYED  (sickly green murk, close and oppressive)
/// </summary>
public class WorldVisualAdapter : MonoBehaviour
{
    [System.Serializable]
    public class Atmosphere
    {
        public Color fogColor = new Color(0.62f, 0.63f, 0.60f);
        public float fogStart = 18f;
        public float fogEnd = 95f;
        public Color sunColor = new Color(0.95f, 0.92f, 0.84f);
        public float sunIntensity = 1.0f;
        public Color ambientSky = new Color(0.52f, 0.55f, 0.58f);
        public Color ambientEquator = new Color(0.42f, 0.42f, 0.40f);
        public Color ambientGround = new Color(0.20f, 0.19f, 0.17f);
        [Range(0f, 2f)] public float ambientIntensity = 0.85f;

        public Atmosphere Clone() => (Atmosphere)MemberwiseClone();
    }

    [System.Serializable]
    public class MaterialTint
    {
        public Material material;
        public Color blossom = Color.white;   // WorldState.Stable
        public Color balanced = Color.white;  // WorldState.Balanced
        public Color decayed = Color.white;   // WorldState.Decaying
        [HideInInspector] public Color original;
    }

    [Header("References")]
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;
    [SerializeField] private Light sun;
    [Tooltip("Camera whose solid-colour background is kept in step with the fog (the 'sky').")]
    [SerializeField] private Camera skyCamera;
    [Range(0.5f, 1.1f)]
    [SerializeField] private float skyBrightness = 0.9f;

    [Header("Atmosphere per state")]
    [SerializeField] private Atmosphere blossom = new Atmosphere();
    [SerializeField] private Atmosphere balanced = new Atmosphere();
    [SerializeField] private Atmosphere decayed = new Atmosphere();

    [Header("Shared-material tints (driven every frame)")]
    [SerializeField] private MaterialTint[] tints;

    [Header("Per-state detail (drifting motes / spores)")]
    [SerializeField] private GameObject blossomDetail;
    [SerializeField] private GameObject decayedDetail;

    [Header("Post-processing volumes (one per state, weight-blended)")]
    [SerializeField] private Behaviour blossomVolume;
    [SerializeField] private Behaviour balancedVolume;
    [SerializeField] private Behaviour decayedVolume;

    [Header("Transition veil")]
    [Tooltip("Seconds a state change takes to fully resolve.")]
    [SerializeField] private float transitionDuration = 3.0f;
    [Tooltip("Colour the fog pales toward at the thickest point of a transition.")]
    [SerializeField] private Color veilColor = new Color(0.76f, 0.77f, 0.79f);
    [Tooltip("How strongly the fog pales toward the veil colour mid-transition.")]
    [SerializeField, Range(0f, 1f)] private float veilTint = 0.6f;
    [Tooltip("Fog end distance is multiplied by this at the thickest point (closer = denser).")]
    [SerializeField, Range(0.15f, 1f)] private float veilFogEndScale = 0.45f;

    private WorldAdaptationManager.WorldState lastState;
    private WorldAdaptationManager.WorldState fromState;   // state we're transitioning away from
    private Atmosphere fromAtmo;      // live snapshot captured at the moment of change
    private Atmosphere toAtmo;        // target for the current transition
    private float t = 1f;             // 0..1 transition progress (1 = settled)
    private readonly Atmosphere live = new Atmosphere();

    private void Awake()
    {
        if (sun == null)
        {
            var go = GameObject.Find("Directional Light");
            if (go != null) sun = go.GetComponent<Light>();
        }
        if (skyCamera == null) skyCamera = Camera.main;
        if (skyCamera != null)
        {
            skyCamera.clearFlags = CameraClearFlags.SolidColor;
            RenderSettings.skybox = null;
        }

        if (tints != null)
            foreach (var mt in tints)
                if (mt != null && mt.material != null)
                    mt.original = mt.material.color;
    }

    private void OnEnable()
    {
        var state = worldAdaptationManager != null
            ? worldAdaptationManager.CurrentState
            : WorldAdaptationManager.WorldState.Balanced;

        lastState = state;
        fromState = state;
        toAtmo = Resolve(state).Clone();
        fromAtmo = toAtmo.Clone();
        CopyAtmo(toAtmo, live);
        t = 1f;
        ApplyAtmosphere(live, 0f);
        ApplyTints(state, state, 1f);
        SetDetail(state);
        SetVolumeWeights(state, state, 1f);
    }

    private void OnDisable()
    {
        // never leave a project material asset recoloured
        if (tints != null)
            foreach (var mt in tints)
                if (mt != null && mt.material != null)
                    mt.material.color = mt.original;
    }

    private void Update()
    {
        if (worldAdaptationManager == null) return;

        var state = worldAdaptationManager.CurrentState;
        if (state != lastState)
        {
            fromState = lastState;
            fromAtmo = live.Clone();
            toAtmo = Resolve(state).Clone();
            t = 0f;
            SetDetail(state);
            DevLog.Log($"[WorldVisualAdapter] the land shifts: {lastState} -> {state}");
            lastState = state;
        }

        if (t < 1f)
            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, transitionDuration));

        float s = Mathf.SmoothStep(0f, 1f, t);
        float mist = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI); // 0 at both ends, 1 mid-transition

        LerpAtmo(fromAtmo, toAtmo, s, live);
        ApplyAtmosphere(live, mist);

        ApplyTints(fromState, lastState, s);
        SetVolumeWeights(fromState, lastState, s);
    }

    // ---- atmosphere -----------------------------------------------------

    private void ApplyAtmosphere(Atmosphere a, float mist)
    {
        Color fog = Color.Lerp(a.fogColor, veilColor, mist * veilTint);
        float end = a.fogEnd * Mathf.Lerp(1f, veilFogEndScale, mist);
        float start = a.fogStart * Mathf.Lerp(1f, 0.55f, mist);

        RenderSettings.fog = true;
        RenderSettings.fogColor = fog;
        RenderSettings.fogStartDistance = start;
        RenderSettings.fogEndDistance = Mathf.Max(start + 1f, end);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = a.ambientSky;
        RenderSettings.ambientEquatorColor = a.ambientEquator;
        RenderSettings.ambientGroundColor = a.ambientGround;
        RenderSettings.ambientIntensity = a.ambientIntensity;

        if (sun != null)
        {
            sun.color = a.sunColor;
            sun.intensity = a.sunIntensity;
        }

        if (skyCamera != null)
            skyCamera.backgroundColor = fog * skyBrightness;
    }

    private static void CopyAtmo(Atmosphere src, Atmosphere dst)
    {
        dst.fogColor = src.fogColor; dst.fogStart = src.fogStart; dst.fogEnd = src.fogEnd;
        dst.sunColor = src.sunColor; dst.sunIntensity = src.sunIntensity;
        dst.ambientSky = src.ambientSky; dst.ambientEquator = src.ambientEquator;
        dst.ambientGround = src.ambientGround; dst.ambientIntensity = src.ambientIntensity;
    }

    private static void LerpAtmo(Atmosphere a, Atmosphere b, float k, Atmosphere dst)
    {
        dst.fogColor = Color.Lerp(a.fogColor, b.fogColor, k);
        dst.fogStart = Mathf.Lerp(a.fogStart, b.fogStart, k);
        dst.fogEnd = Mathf.Lerp(a.fogEnd, b.fogEnd, k);
        dst.sunColor = Color.Lerp(a.sunColor, b.sunColor, k);
        dst.sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, k);
        dst.ambientSky = Color.Lerp(a.ambientSky, b.ambientSky, k);
        dst.ambientEquator = Color.Lerp(a.ambientEquator, b.ambientEquator, k);
        dst.ambientGround = Color.Lerp(a.ambientGround, b.ambientGround, k);
        dst.ambientIntensity = Mathf.Lerp(a.ambientIntensity, b.ambientIntensity, k);
    }

    // ---- tints --------------------------------------------------------

    private void ApplyTints(WorldAdaptationManager.WorldState from,
                            WorldAdaptationManager.WorldState to, float k)
    {
        if (tints == null) return;
        foreach (var mt in tints)
        {
            if (mt == null || mt.material == null) continue;
            mt.material.color = Color.Lerp(TintFor(mt, from), TintFor(mt, to), k);
        }
    }

    private static Color TintFor(MaterialTint mt, WorldAdaptationManager.WorldState s)
    {
        switch (s)
        {
            case WorldAdaptationManager.WorldState.Stable:   return mt.blossom;
            case WorldAdaptationManager.WorldState.Decaying: return mt.decayed;
            default:                                         return mt.balanced;
        }
    }

    // ---- detail + post volumes ---------------------------------------

    private void SetDetail(WorldAdaptationManager.WorldState s)
    {
        if (blossomDetail != null) blossomDetail.SetActive(s == WorldAdaptationManager.WorldState.Stable);
        if (decayedDetail != null) decayedDetail.SetActive(s == WorldAdaptationManager.WorldState.Decaying);
    }

    private void SetVolumeWeights(WorldAdaptationManager.WorldState from,
                                  WorldAdaptationManager.WorldState to, float k)
    {
        SetWeight(blossomVolume,  Mathf.Lerp(W(from, WorldAdaptationManager.WorldState.Stable),   W(to, WorldAdaptationManager.WorldState.Stable),   k));
        SetWeight(balancedVolume, Mathf.Lerp(W(from, WorldAdaptationManager.WorldState.Balanced), W(to, WorldAdaptationManager.WorldState.Balanced), k));
        SetWeight(decayedVolume,  Mathf.Lerp(W(from, WorldAdaptationManager.WorldState.Decaying), W(to, WorldAdaptationManager.WorldState.Decaying), k));
    }

    private static float W(WorldAdaptationManager.WorldState s, WorldAdaptationManager.WorldState match)
        => s == match ? 1f : 0f;

    private static void SetWeight(Behaviour volume, float w)
    {
        if (volume == null) return;
#if UNITY_POST_PROCESSING_STACK_V2
        if (volume is PostProcessVolume ppv) { ppv.weight = w; return; }
#endif
    }

    // ---- helpers -----------------------------------------------------

    private Atmosphere Resolve(WorldAdaptationManager.WorldState state)
    {
        switch (state)
        {
            case WorldAdaptationManager.WorldState.Stable:   return blossom;
            case WorldAdaptationManager.WorldState.Decaying: return decayed;
            default:                                         return balanced;
        }
    }

}
