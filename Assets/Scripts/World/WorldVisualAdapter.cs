using UnityEngine;

/// <summary>
/// The world's response to the player IS the adaptation - not a difficulty dial.
/// As the defensive-mastery signal rises the region "wakes" and turns: fog sours
/// and closes in, the light dims and greens, the ground darkens. Everything
/// lerps toward the current state's target so the shift is felt, not snapped.
/// </summary>
public class WorldVisualAdapter : MonoBehaviour
{
    [System.Serializable]
    public class Atmosphere
    {
        public Material groundMaterial;
        public Color fogColor = new Color(0.72f, 0.78f, 0.85f);
        public float fogStart = 45f;
        public float fogEnd = 210f;
        public Color sunColor = new Color(1f, 0.95f, 0.85f);
        public float sunIntensity = 1.35f;
        public float ambientIntensity = 0.55f;
    }

    [Header("References")]
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Light sun;

    [Header("Atmosphere per state")]
    [SerializeField] private Atmosphere stable;
    [SerializeField] private Atmosphere balanced;
    [SerializeField] private Atmosphere decaying;

    [SerializeField] private float lerpSpeed = 0.5f;

    private WorldAdaptationManager.WorldState lastState;
    private Atmosphere current;

    private void Start()
    {
        if (sun == null)
        {
            var go = GameObject.Find("Directional Light");
            if (go != null) sun = go.GetComponent<Light>();
        }
        current = Resolve(worldAdaptationManager != null ? worldAdaptationManager.CurrentState : WorldAdaptationManager.WorldState.Balanced);
        lastState = worldAdaptationManager != null ? worldAdaptationManager.CurrentState : lastState;
        ApplyGround(current);
    }

    private void Update()
    {
        if (worldAdaptationManager == null) return;

        if (worldAdaptationManager.CurrentState != lastState)
        {
            lastState = worldAdaptationManager.CurrentState;
            current = Resolve(lastState);
            ApplyGround(current);
            Debug.Log($"[WorldVisualAdapter] the land shifts -> {lastState}");
        }

        if (current == null) return;

        float k = lerpSpeed * Time.deltaTime;
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, current.fogColor, k);
        RenderSettings.fogStartDistance = Mathf.Lerp(RenderSettings.fogStartDistance, current.fogStart, k);
        RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, current.fogEnd, k);
        RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, current.ambientIntensity, k);

        if (sun != null)
        {
            sun.color = Color.Lerp(sun.color, current.sunColor, k);
            sun.intensity = Mathf.Lerp(sun.intensity, current.sunIntensity, k);
        }
    }

    private Atmosphere Resolve(WorldAdaptationManager.WorldState state)
    {
        switch (state)
        {
            case WorldAdaptationManager.WorldState.Stable: return stable;
            case WorldAdaptationManager.WorldState.Decaying: return decaying;
            default: return balanced;
        }
    }

    private void ApplyGround(Atmosphere atmo)
    {
        if (targetRenderer != null && atmo != null && atmo.groundMaterial != null)
            targetRenderer.material = atmo.groundMaterial;
    }
}
