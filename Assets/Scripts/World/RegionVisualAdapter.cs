using UnityEngine;

/// <summary>
/// Region-scoped visual response to a WorldRegion's state. Deliberately much
/// simpler than the global WorldVisualAdapter: no fog/sky/post-processing,
/// since those are scene-wide render settings with no clean per-region
/// equivalent without volume-based rendering tricks - out of scope for this
/// pass (the global atmosphere system is untouched and keeps running).
///
/// Tints only the renderers assigned to this region via MaterialPropertyBlock,
/// never the shared material asset itself - unlike the global adapter's
/// tint list (which mutates shared .mat assets and has to carefully restore
/// them in OnDisable), a property block is per-renderer and per-instance by
/// design, so there's nothing to leak or restore.
/// </summary>
public class RegionVisualAdapter : MonoBehaviour
{
    [SerializeField] private WorldRegion region;

    [Header("Renderers tinted for this region only (Standard-shader compatible)")]
    [SerializeField] private Renderer[] tintedRenderers;
    [SerializeField] private Color balancedTint = Color.white;
    [SerializeField] private Color blossomTint = new Color(0.75f, 1f, 0.82f);
    [SerializeField] private Color decayedTint = new Color(0.55f, 0.52f, 0.40f);

    [Header("Per-state local detail (e.g. motes / spores)")]
    [SerializeField] private GameObject blossomDetail;
    [SerializeField] private GameObject decayedDetail;

    [SerializeField] private float transitionDuration = 2f;

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock block;
    private Color fromTint, toTint, liveTint;
    private float t = 1f;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        if (region == null)
            region = GetComponent<WorldRegion>();
    }

    private void OnEnable()
    {
        if (region != null)
            region.StateChanged += HandleStateChanged;

        RegionWorldState state = region != null ? region.CurrentState : RegionWorldState.Balanced;
        fromTint = toTint = liveTint = TintFor(state);
        t = 1f;
        ApplyTint(liveTint);
        SetDetail(state);
    }

    private void OnDisable()
    {
        if (region != null)
            region.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(WorldRegion _, RegionWorldState newState)
    {
        fromTint = liveTint;
        toTint = TintFor(newState);
        t = 0f;
        SetDetail(newState);
    }

    private void Update()
    {
        if (t >= 1f)
            return;

        t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, transitionDuration));
        liveTint = Color.Lerp(fromTint, toTint, Mathf.SmoothStep(0f, 1f, t));
        ApplyTint(liveTint);
    }

    private void ApplyTint(Color color)
    {
        if (tintedRenderers == null)
            return;

        foreach (var r in tintedRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(ColorId, color);
            r.SetPropertyBlock(block);
        }
    }

    private void SetDetail(RegionWorldState state)
    {
        if (blossomDetail != null) blossomDetail.SetActive(state == RegionWorldState.Blossom);
        if (decayedDetail != null) decayedDetail.SetActive(state == RegionWorldState.Decayed);
    }

    private Color TintFor(RegionWorldState state)
    {
        switch (state)
        {
            case RegionWorldState.Blossom: return blossomTint;
            case RegionWorldState.Decayed: return decayedTint;
            default: return balancedTint;
        }
    }
}
