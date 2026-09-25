using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Turns any visually-designed area into a functional Point of Interest: discovery,
/// completion, loot, an optional wrapped encounter, persistence, and AdaptiveRegion
/// eligibility, all configurable from the Inspector. No custom code per POI - the same
/// "build it visually, add one component, configure, done" philosophy as Camp (see that
/// class), applied to exploration instead of combat.
///
/// Deliberately NOT a map-marker/checklist system - there is no UI, no waypoint, no
/// registry the player can browse. ExplorationTracker only keeps aggregate counts for
/// other systems (HUD text, debug, a future adaptive-world hook) to react to; discovery
/// stays purely a world-space event (walk up, trigger fires).
/// </summary>
public class PointOfInterest : MonoBehaviour
{
    /// <summary>Purely descriptive/categorization - drives ExplorationTracker grouping and
    /// the default gizmo colour, never a code path. The actual behaviour of any POI comes
    /// entirely from which optional modules below are configured.</summary>
    public enum POIType
    {
        AbandonedStructure,
        ResourceLocation,
        LoreLocation,
        HiddenCache,
        SmallEncounter,
        EnvironmentalDiscovery,
        MiniDungeonEntrance,
        AdaptiveAnomaly
    }

    public enum POICompletionMode
    {
        /// <summary>This POI has no distinct "done" state - discovery is all it tracks.</summary>
        None,
        /// <summary>Completes the instant it's discovered (lore markers, environmental discoveries).</summary>
        OnDiscovery,
        /// <summary>Completes when the wrapped Encounter (a Camp) fires Cleared.</summary>
        OnEncounterCleared,
        /// <summary>Completes only when something external calls Complete() - a chest opening
        /// (see LootChest.Opened), a resource-gathering interaction, a future lore/dialogue
        /// choice. The escape hatch for interactions not yet built.</summary>
        Manual
    }

    public enum POIState
    {
        Undiscovered,
        Discovered,
        Completed
    }

    [Header("POI Info")]
    [SerializeField] private string poiName = "Unnamed Point of Interest";
    [Tooltip("Stable id used by the save system. Defaults to the POI name if left blank.")]
    [SerializeField] private string poiId = "";
    [SerializeField] private POIType poiType = POIType.EnvironmentalDiscovery;
    [TextArea] [SerializeField] private string description = "";

    [Header("Discovery")]
    [Tooltip("If on and no Collider already exists on this GameObject, a trigger SphereCollider " +
             "is auto-added at Awake using Discovery Radius - no manual collider setup needed. " +
             "Add your own Collider first if you want a custom shape (e.g. a box for a building).")]
    [SerializeField] private bool enableDiscovery = true;
    [SerializeField] private float discoveryRadius = 8f;

    [Header("Completion")]
    [SerializeField] private POICompletionMode completionMode = POICompletionMode.None;
    [Tooltip("Required for On Encounter Cleared - the Camp this POI wraps for 'small encounters' " +
             "or 'entrances to mini-dungeons' that gate on a fight.")]
    [SerializeField] private Camp encounter;

    [Header("Loot (optional)")]
    [Tooltip("Granted once, the moment this POI completes.")]
    [SerializeField] private RewardSource rewardSource;

    [Header("AdaptiveRegion (optional - 'unusual adaptive-world locations')")]
    [Tooltip("If assigned, this POI is only discoverable/completable while the region reads one " +
             "of Active In States (empty = always eligible regardless of region). Subscribes to " +
             "this region's StateChanged, so eligibility updates live with no per-frame polling.")]
    [SerializeField] private WorldRegion region;
    [SerializeField] private RegionWorldState[] activeInStates = new RegionWorldState[0];

    [Header("Progression Gate (optional - 'access')")]
    [Tooltip("If assigned, this POI isn't discoverable until the player has purchased this " +
             "ProgressionUnlock - e.g. a sealed cache that only opens up once you've earned the way in.")]
    [SerializeField] private ProgressionUnlock requiredUnlock;

    [Header("Lore Hook (optional - for a future dedicated lore/dialogue system)")]
    [Tooltip("Free-form id a future lore system can look up. Purely data - no lore system exists yet.")]
    [SerializeField] private string loreEntryId = "";

    [Header("Feedback Hooks (optional - wire VFX/audio/animation here, no code required)")]
    [SerializeField] private UnityEvent onDiscovered = new UnityEvent();
    [SerializeField] private UnityEvent onCompleted = new UnityEvent();
    [Tooltip("Fired when Active In States eligibility flips true - e.g. reveal a 'corrupted' effect.")]
    [SerializeField] private UnityEvent onBecameRegionEligible = new UnityEvent();
    [Tooltip("Fired when Active In States eligibility flips false.")]
    [SerializeField] private UnityEvent onBecameRegionIneligible = new UnityEvent();

    private POIState state = POIState.Undiscovered;
    private bool cachedEligible;

    public POIState State => state;
    public bool IsDiscovered => state != POIState.Undiscovered;
    public bool IsCompleted => state == POIState.Completed;
    public bool IsRegionEligible => cachedEligible;
    public string PoiId => string.IsNullOrEmpty(poiId) ? poiName : poiId;
    public POIType Type => poiType;
    public string LoreEntryId => loreEntryId;

    /// <summary>Fired once, the moment this POI is discovered (live, or a restored save).</summary>
    public event System.Action Discovered;
    /// <summary>Fired once, the moment this POI is completed (live, or a restored save).</summary>
    public event System.Action Completed;
    /// <summary>Fired whenever Active In States eligibility flips, for a code-based listener.</summary>
    public event System.Action<bool> RegionEligibilityChanged;

    private void OnValidate()
    {
        if (completionMode == POICompletionMode.OnEncounterCleared && encounter == null)
            Debug.LogWarning($"[POI] {poiName}: On Encounter Cleared needs an Encounter (Camp) " +
                              "reference assigned, or this POI will never complete.", this);

        if (activeInStates != null && activeInStates.Length > 0 && region == null)
            Debug.LogWarning($"[POI] {poiName}: Active In States is set but no Region is assigned - " +
                              "the eligibility gate only applies once a WorldRegion is wired.", this);
    }

    private void Awake()
    {
        if (enableDiscovery && GetComponent<Collider>() == null)
        {
            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = discoveryRadius;
        }

        cachedEligible = ComputeEligibility();
    }

    private void OnEnable()
    {
        if (region != null)
            region.StateChanged += HandleRegionStateChanged;

        if (requiredUnlock != null)
            ProgressionSystem.Unlocked += HandleProgressionUnlocked;

        if (completionMode == POICompletionMode.OnEncounterCleared && encounter != null)
            encounter.Cleared += HandleEncounterCleared;
    }

    private void OnDisable()
    {
        if (region != null)
            region.StateChanged -= HandleRegionStateChanged;

        if (requiredUnlock != null)
            ProgressionSystem.Unlocked -= HandleProgressionUnlocked;

        if (completionMode == POICompletionMode.OnEncounterCleared && encounter != null)
            encounter.Cleared -= HandleEncounterCleared;
    }

    private bool ComputeEligibility()
    {
        if (requiredUnlock != null && !ProgressionSystem.HasUnlock(requiredUnlock))
            return false;

        if (activeInStates == null || activeInStates.Length == 0)
            return true;

        // The region gate only applies once a region is actually wired - see OnValidate's warning.
        if (region == null)
            return true;

        return System.Array.IndexOf(activeInStates, region.CurrentState) >= 0;
    }

    private void HandleRegionStateChanged(WorldRegion changedRegion, RegionWorldState newState)
    {
        RecheckEligibility();
    }

    private void HandleEncounterCleared()
    {
        Complete();
    }

    private void HandleProgressionUnlocked(ProgressionUnlock unlock)
    {
        if (unlock != requiredUnlock)
            return;

        RecheckEligibility();
    }

    private void RecheckEligibility()
    {
        bool eligible = ComputeEligibility();
        if (eligible == cachedEligible)
            return;

        cachedEligible = eligible;
        RegionEligibilityChanged?.Invoke(eligible);

        if (eligible) onBecameRegionEligible?.Invoke();
        else onBecameRegionIneligible?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enableDiscovery || state != POIState.Undiscovered)
            return;

        if (!other.CompareTag("Player"))
            return;

        // Not eligible in the region's current state - stays silently undiscoverable
        // ("unusual adaptive-world locations" gate).
        if (!cachedEligible)
            return;

        Discover();
    }

    /// <summary>Marks this POI found. Public so a scripted event/cutscene can reveal a POI
    /// without a physical walk-up trigger, in addition to the normal OnTriggerEnter path.</summary>
    public void Discover()
    {
        if (state != POIState.Undiscovered)
            return;

        state = POIState.Discovered;
        ExplorationTracker.RegisterDiscovery(this);

        DevLog.Log($"[POI] {poiName} discovered.");
        onDiscovered?.Invoke();
        Discovered?.Invoke();

        if (completionMode == POICompletionMode.OnDiscovery)
            CompleteInternal();
    }

    /// <summary>Manual completion trigger - wire this to a chest's "opened" event, an
    /// interaction prompt, a future puzzle/lore system, or call it directly. Also how
    /// Completion Mode = Manual is meant to be driven. Auto-discovers first if needed.</summary>
    public void Complete()
    {
        if (state == POIState.Undiscovered)
            Discover();

        CompleteInternal();
    }

    private void CompleteInternal()
    {
        if (state == POIState.Completed)
            return;

        state = POIState.Completed;
        ExplorationTracker.RegisterCompletion(this);

        DevLog.Log($"[POI] {poiName} completed.");

        if (rewardSource != null)
            rewardSource.Grant();

        onCompleted?.Invoke();
        Completed?.Invoke();
    }

    /// <summary>Instantly restores discovered/completed state from a save file - no
    /// re-triggering events/rewards (already applied in the session that earned them).
    /// Still registers with ExplorationTracker so restored progress counts correctly.</summary>
    public void RestoreState(bool discovered, bool completed)
    {
        if (completed)
        {
            state = POIState.Completed;
            ExplorationTracker.RegisterDiscovery(this);
            ExplorationTracker.RegisterCompletion(this);
        }
        else if (discovered)
        {
            state = POIState.Discovered;
            ExplorationTracker.RegisterDiscovery(this);
        }
        else
        {
            state = POIState.Undiscovered;
        }

        // Re-sync the eligibility cache directly (no event fire - silent restore, same
        // contract as the state above). Awake() computed it before any save restore ran
        // (ProgressionSystem/region state may have been populated after this POI's own
        // Awake), so it can be stale by the time this runs - defense in depth regardless
        // of restore call order elsewhere.
        cachedEligible = ComputeEligibility();
    }

    // ------------------------------------------------------------------
    // Editor-only visualization (never runs in a build).
    // ------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (!enableDiscovery)
            return;

        Gizmos.color = GizmoColor();
        Gizmos.DrawWireSphere(transform.position, GizmoRadius());
    }

    private float GizmoRadius()
    {
        var sphere = GetComponent<SphereCollider>();
        if (sphere == null)
            return discoveryRadius;

        Vector3 scale = transform.lossyScale;
        float maxScale = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        return sphere.radius * maxScale;
    }

    private Color GizmoColor()
    {
        if (Application.isPlaying && state == POIState.Completed)
            return new Color(0.5f, 0.5f, 0.5f, 1f);

        if (!ComputeEligibility())
            return new Color(0.55f, 0.15f, 0.75f, 0.6f); // ineligible in the current region state

        switch (poiType)
        {
            case POIType.LoreLocation: return new Color(0.3f, 0.6f, 1f, 1f);
            case POIType.ResourceLocation: return new Color(0.4f, 0.9f, 0.4f, 1f);
            case POIType.HiddenCache: return new Color(1f, 0.85f, 0.2f, 1f);
            case POIType.SmallEncounter: return new Color(1f, 0.3f, 0.3f, 1f);
            case POIType.MiniDungeonEntrance: return new Color(0.8f, 0.2f, 0.9f, 1f);
            case POIType.AdaptiveAnomaly: return new Color(1f, 0.5f, 0f, 1f);
            case POIType.AbandonedStructure: return new Color(0.6f, 0.5f, 0.4f, 1f);
            default: return new Color(0.2f, 0.8f, 1f, 1f); // EnvironmentalDiscovery
        }
    }
}
