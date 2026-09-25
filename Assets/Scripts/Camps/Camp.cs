using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A reusable gameplay encounter: activation, enemy registration, reliable completion
/// detection, rewards, persistence and optional visual/audio feedback hooks, all
/// configurable from the Inspector. Any visually-designed area becomes a functional
/// camp by adding this component - no per-camp code.
///
/// Authoring workflow: build the environment -> add this component -> (optionally) set
/// Activation Mode + assign an Encounter Bounds trigger -> place enemies as children
/// (zero-config) or wire a CampSpawner for spawned/adaptive composition -> assign a
/// RewardSource -> done. See the CampEditor buttons for one-click bounds/enemy setup.
/// </summary>
public class Camp : MonoBehaviour
{
    public enum BossWeakeningReward
    {
        DisableHealing,
        DisableSummons,
        DisableDecayAura
    }

    /// <summary>Always: active the instant the scene loads - the original, zero-config
    /// behaviour every existing camp still uses unchanged. OnPlayerEnterBounds: stays
    /// Inactive (no enemies registered, cannot complete) until the player enters
    /// Encounter Bounds - the actual "encounter activation" mechanism.</summary>
    public enum EncounterActivationMode
    {
        Always,
        OnPlayerEnterBounds
    }

    /// <summary>The camp's whole lifecycle, deliberately explicit rather than a bare bool -
    /// "clear gameplay state" other systems (HUD, debug, future triggers) can read directly
    /// instead of inferring it from IsCleared alone.</summary>
    public enum EncounterState
    {
        Inactive,
        Active,
        Cleared
    }

    [Header("Camp Info")]
    [SerializeField] private string campName = "Unnamed Camp";
    [Tooltip("Stable id used by the save system. Defaults to the camp name if left blank.")]
    [SerializeField] private string campId = "";

    [Header("Activation")]
    [SerializeField] private EncounterActivationMode activationMode = EncounterActivationMode.Always;
    [Tooltip("Required for OnPlayerEnterBounds - a trigger Collider on THIS GameObject defining " +
             "the encounter area. Also drives the editor gizmo. Use the CampEditor 'Add Encounter " +
             "Bounds Trigger' button to auto-size one from this camp's children.")]
    [SerializeField] private Collider encounterBounds;

    [Header("Enemies")]
    [Tooltip("Zero-config default: every EnemyHealth found under this GameObject at activation. " +
             "A CampSpawner (if present) populates this instead - see NotifyPopulated().")]
    [SerializeField] private List<EnemyHealth> enemies = new List<EnemyHealth>();

    [Header("Encounter Reward (optional)")]
    [Tooltip("If assigned, clearing this camp grants whatever this RewardSource " +
             "resolves - if it references an AdaptiveRewardTable, the current " +
             "region's state (Balanced/Blossom/Decayed) picks the pool with no " +
             "region-specific code here at all.")]
    [SerializeField] private RewardSource rewardSource;

    [Header("Boss Effect (optional)")]
    [SerializeField] private BossController boss;
    [SerializeField] private BossWeakeningReward reward;

    [Header("Feedback Hooks (optional - wire VFX/audio/animation here, no code required)")]
    [SerializeField] private UnityEvent onActivated = new UnityEvent();
    [SerializeField] private UnityEvent onCleared = new UnityEvent();

    private EncounterState state = EncounterState.Inactive;
    private bool hasRegisteredEnemies;

    /// <summary>The camp's current lifecycle state.</summary>
    public EncounterState State => state;
    /// <summary>True once activated and until cleared - the window during which completion is possible.</summary>
    public bool IsActive => state == EncounterState.Active;
    /// <summary>True once this encounter has been cleared (live combat OR a restored save).</summary>
    public bool IsCleared => state == EncounterState.Cleared;
    public string CampId => string.IsNullOrEmpty(campId) ? campName : campId;
    public string CampName => campName;
    /// <summary>True if clearing this camp strips one of the boss's abilities (see BossEffect).</summary>
    public bool WeakensBoss => boss != null;
    public BossWeakeningReward BossEffect => reward;
    public IReadOnlyList<EnemyHealth> Enemies => enemies;

    /// <summary>Fired once, the moment this camp activates (Always mode: immediately at Awake).</summary>
    public event System.Action Activated;
    /// <summary>Fired once, the moment this camp is cleared (live combat OR a restored save).</summary>
    public event System.Action Cleared;

    private void OnValidate()
    {
        if (activationMode == EncounterActivationMode.OnPlayerEnterBounds && encounterBounds == null)
            Debug.LogWarning($"[Camp] {campName}: OnPlayerEnterBounds needs an Encounter Bounds " +
                              "trigger Collider assigned, or the encounter will never activate.", this);

        if (encounterBounds != null && !encounterBounds.isTrigger)
            Debug.LogWarning($"[Camp] {campName}: Encounter Bounds collider is not marked Is Trigger - " +
                              "activation will never fire.", this);
    }

    private void Awake()
    {
        if (activationMode == EncounterActivationMode.Always)
            Activate();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activationMode != EncounterActivationMode.OnPlayerEnterBounds)
            return;

        if (state != EncounterState.Inactive)
            return;

        if (!other.CompareTag("Player"))
            return;

        Activate();
    }
    // Deliberately no OnTriggerExit handling - once activated, the encounter stays
    // active until cleared. Leaving bounds must never reset or re-trigger it (that
    // would be an exploitable "walk out to reset the fight" loophole).

    private void Activate()
    {
        if (state != EncounterState.Inactive)
            return;

        state = EncounterState.Active;

        // Zero-config default: pick up whatever's already placed as a child. A
        // CampSpawner populating asynchronously overwrites this via NotifyPopulated()
        // once it finishes - see that method for why this call alone isn't enough for
        // spawner-driven camps.
        RefreshEnemies();

        DevLog.Log($"[Camp] {campName} activated.");
        onActivated?.Invoke();
        Activated?.Invoke();
    }

    private void Update()
    {
        // Two independent guards against premature/duplicate completion:
        // (1) must actually be Active - not before activation, not after already Cleared.
        if (state != EncounterState.Active)
            return;

        // (2) population must have genuinely happened at least once - a camp that starts
        // with zero children (e.g. a CampSpawner hasn't run yet) must NOT be mistaken for
        // "already empty, therefore cleared". This is the actual premature-completion bug
        // this framework exists to prevent.
        if (!hasRegisteredEnemies)
            return;

        enemies.RemoveAll(enemy => enemy == null);

        if (enemies.Count == 0)
        {
            ClearCamp();
        }
    }

    /// <summary>Zero-config enemy registration: re-scans children for EnemyHealth right now.
    /// Safe to call any time (editor tooling calls this in Edit mode too, to preview the
    /// list without entering Play mode). Only marks the camp "populated" if it actually
    /// found something - an empty result here means "nobody's placed enemies yet", not
    /// "this encounter is intentionally empty" (see NotifyPopulated for that case).</summary>
    public void RefreshEnemies()
    {
        enemies.Clear();
        enemies.AddRange(GetComponentsInChildren<EnemyHealth>());

        if (enemies.Count > 0)
            hasRegisteredEnemies = true;
    }

    /// <summary>Explicit alternative to RefreshEnemies() for spawner-driven camps: re-scans
    /// children AND marks the encounter populated regardless of the resulting count, so an
    /// intentionally-empty composition (e.g. an empty Blossom tier) still resolves cleanly
    /// instead of sitting "in progress" forever. Call this once, after all spawning for this
    /// activation is finished - never mid-spawn, or a partially-spawned batch could read as
    /// already cleared.</summary>
    public void NotifyPopulated()
    {
        enemies.Clear();
        enemies.AddRange(GetComponentsInChildren<EnemyHealth>());
        hasRegisteredEnemies = true;
    }

    /// <summary>Registers a single enemy directly (e.g. a boss summoning adds that should
    /// count toward this same camp's clear condition) without a full child re-scan.
    /// Idempotent - re-registering the same instance is a no-op.</summary>
    public void RegisterEnemy(EnemyHealth enemy)
    {
        if (enemy == null || enemies.Contains(enemy))
            return;

        enemies.Add(enemy);
        hasRegisteredEnemies = true;
    }

    private void ClearCamp()
    {
        state = EncounterState.Cleared;

        DevLog.Log($"[Camp] {campName} cleared!");

        ApplyBossEffect();
        GrantEncounterReward();
        onCleared?.Invoke();
        Cleared?.Invoke();
    }

    /// <summary>
    /// Instantly mark this camp cleared from a save file - no combat, no re-triggering
    /// the Cleared event or feedback hooks (the save already reflects that this camp's
    /// reward was applied in a prior session). Any enemies still present (a fresh scene
    /// load always re-creates the scene-authored ones) are removed so the player doesn't
    /// have to re-fight an already-cleared camp.
    /// </summary>
    public void RestoreCleared()
    {
        if (state == EncounterState.Cleared)
            return;

        foreach (var enemy in enemies)
            if (enemy != null)
                Destroy(enemy.gameObject);

        enemies.Clear();
        hasRegisteredEnemies = true;
        state = EncounterState.Cleared;

        ApplyBossEffect();
    }

    private void ApplyBossEffect()
    {
        // Boss-weakening is optional - most camps in a reusable encounter framework
        // won't reference a boss at all, so this is a silent no-op, not a warning.
        if (boss == null)
            return;

        switch (reward)
        {
            case BossWeakeningReward.DisableHealing:
                boss.DisableHealing();
                break;

            case BossWeakeningReward.DisableSummons:
                boss.DisableSummons();
                break;

            case BossWeakeningReward.DisableDecayAura:
                boss.DisableDecayAura();
                break;
        }
    }

    private void GrantEncounterReward()
    {
        if (rewardSource == null)
            return;

        rewardSource.Grant();
    }

    // ------------------------------------------------------------------
    // Editor-only visualization (never runs in a build - Unity never invokes
    // OnDrawGizmos* outside the Editor).
    // ------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (encounterBounds == null)
            return;

        Gizmos.color = state == EncounterState.Cleared
            ? new Color(0.4f, 0.4f, 0.4f, 1f)
            : activationMode == EncounterActivationMode.OnPlayerEnterBounds
                ? new Color(1f, 0.65f, 0f, 1f)
                : new Color(0.2f, 0.8f, 1f, 1f);

        DrawColliderGizmo(encounterBounds);
    }

    private static void DrawColliderGizmo(Collider col)
    {
        if (col is BoxCollider box)
        {
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = box.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = prev;
        }
        else if (col is SphereCollider sphere)
        {
            float scale = Mathf.Max(sphere.transform.lossyScale.x,
                          Mathf.Max(sphere.transform.lossyScale.y, sphere.transform.lossyScale.z));
            Gizmos.DrawWireSphere(sphere.transform.TransformPoint(sphere.center), sphere.radius * scale);
        }
        else
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
