using UnityEngine;

public class CampSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camp camp;
    [SerializeField] private Camp requiredCamp;
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;
    [Tooltip("If assigned, this region's state drives composition instead of the " +
             "global WorldAdaptationManager - the region-based adaptation path. " +
             "Leave empty to keep the existing global-adaptation behaviour.")]
    [SerializeField] private WorldRegion region;

    [Header("Encounter Composition (optional - takes priority over the legacy fields below)")]
    [Tooltip("Region-driven composition, resolved against Region's current state (or " +
             "WorldRegion.ActiveRegion if Region is unassigned, or Balanced with no region " +
             "context at all) - the generic, no-code replacement for the hardcoded tiers below.")]
    [SerializeField] private AdaptiveEncounterComposition adaptiveComposition;
    [Tooltip("A single fixed composition with no region-awareness.")]
    [SerializeField] private EncounterComposition composition;

    [Header("Legacy Enemy Prefabs (used only if no Composition above is assigned)")]
    [SerializeField] private GameObject stalkerPrefab;
    [SerializeField] private GameObject brutePrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private PlayerPerformanceTracker performanceTracker;

    private bool hasSpawned;
    public bool HasSpawned { get; private set; }

    /// <summary>
    /// Tell this spawner its camp was already cleared in a save file - skip the
    /// adaptive spawn entirely. Must be called BEFORE Camp.RestoreCleared(), or
    /// this spawner would still populate fresh enemies and Camp.RefreshEnemies()
    /// would flip the just-restored IsCleared back to false.
    /// </summary>
    public void MarkAlreadySpawned()
    {
        hasSpawned = true;
        HasSpawned = true;
    }

    public string LastGeneratedState { get; private set; } = "Not generated yet";
    public string LastComposition { get; private set; } = "Waiting for first camp";
    public float LastScore { get; private set; }
    private void Update()
    {
        if (hasSpawned)
            return;

        // Wait for the camp's own activation gate - a no-op for every existing (Always-mode)
        // camp, since IsActive is already true by the time this Update() ever runs (Awake()
        // for every object in the scene completes before the first Update() begins). For an
        // OnPlayerEnterBounds camp, this correctly holds spawning until the player triggers it.
        if (camp != null && !camp.IsActive)
            return;

        if (requiredCamp != null && !requiredCamp.IsCleared)
            return;

        SpawnCamp();
    }

    private void SpawnCamp()
    {
        hasSpawned = true;
        HasSpawned = true;

        if (adaptiveComposition != null)
        {
            RegionWorldState resolvedState = region != null ? region.CurrentState
                : WorldRegion.ActiveRegion != null ? WorldRegion.ActiveRegion.CurrentState
                : RegionWorldState.Balanced;

            LastGeneratedState = resolvedState.ToString();
            SpawnComposition(adaptiveComposition.Resolve(resolvedState));
            if (region != null) LastScore = region.CommittedScore;
        }
        else if (composition != null)
        {
            LastGeneratedState = "Fixed";
            SpawnComposition(composition);
        }
        else if (region != null)
        {
            LastGeneratedState = region.CurrentState.ToString();
            SpawnForRegionState(region.CurrentState);
            LastScore = region.CommittedScore;
        }
        else
        {
            WorldAdaptationManager.WorldState state = WorldAdaptationManager.WorldState.Balanced;

            if (worldAdaptationManager != null)
            {
                state = worldAdaptationManager.CurrentState;
            }

            LastGeneratedState = state.ToString();
            SpawnForGlobalState(state);

            if (performanceTracker != null)
            {
                LastScore = performanceTracker.GetPerformanceScore();
            }
        }

        if (camp != null)
        {
            // NotifyPopulated (not RefreshEnemies) - marks the camp populated even if this
            // composition genuinely resolved to zero enemies, so an intentionally-empty tier
            // still completes cleanly instead of hanging "in progress" forever.
            camp.NotifyPopulated();
        }

        DevLog.Log($"[CampSpawner] Adaptive camp generated: {LastGeneratedState} | {LastComposition} | Score: {LastScore:0.0}");
    }

    private void SpawnComposition(EncounterComposition comp)
    {
        if (comp == null)
        {
            LastComposition = "(empty)";
            return;
        }

        int count = comp.spawns != null ? comp.spawns.Length : 0;
        LastComposition = $"{comp.compositionName} ({count} enemies)";

        if (comp.spawns == null)
            return;

        foreach (var entry in comp.spawns)
            SpawnEnemy(entry.prefab, entry.spawnPointIndex);
    }

    private void SpawnForGlobalState(WorldAdaptationManager.WorldState state)
    {
        switch (state)
        {
            case WorldAdaptationManager.WorldState.Stable:
                LastComposition = "1 Stalker";
                SpawnEnemy(stalkerPrefab, 0);
                break;

            case WorldAdaptationManager.WorldState.Balanced:
                LastComposition = "2 Stalkers + 1 Brute";
                SpawnEnemy(stalkerPrefab, 0);
                SpawnEnemy(brutePrefab, 1);
                SpawnEnemy(stalkerPrefab, 2);
                break;

            case WorldAdaptationManager.WorldState.Decaying:
                LastComposition = "1 Stalker + 2 Brutes";
                SpawnEnemy(stalkerPrefab, 0);
                SpawnEnemy(brutePrefab, 1);
                SpawnEnemy(brutePrefab, 2);
                break;
        }
    }

    /// <summary>Same three-tier shape as SpawnForGlobalState, re-keyed to the
    /// region-adaptation enum: Blossom is the gentlest tier, Decayed the harshest -
    /// matching the legacy Stable/Decaying polarity so the difficulty curve doesn't
    /// change, only which system is driving it.</summary>
    private void SpawnForRegionState(RegionWorldState state)
    {
        switch (state)
        {
            case RegionWorldState.Blossom:
                LastComposition = "1 Stalker";
                SpawnEnemy(stalkerPrefab, 0);
                break;

            case RegionWorldState.Balanced:
                LastComposition = "2 Stalkers + 1 Brute";
                SpawnEnemy(stalkerPrefab, 0);
                SpawnEnemy(brutePrefab, 1);
                SpawnEnemy(stalkerPrefab, 2);
                break;

            case RegionWorldState.Decayed:
                LastComposition = "1 Stalker + 2 Brutes";
                SpawnEnemy(stalkerPrefab, 0);
                SpawnEnemy(brutePrefab, 1);
                SpawnEnemy(brutePrefab, 2);
                break;
        }
    }

    private void SpawnEnemy(GameObject prefab, int spawnPointIndex)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[CampSpawner] Enemy prefab is missing.");
            return;
        }

        if (spawnPoints == null || spawnPointIndex >= spawnPoints.Length || spawnPoints[spawnPointIndex] == null)
        {
            Debug.LogWarning($"[CampSpawner] Spawn point {spawnPointIndex} is missing.");
            return;
        }

        Transform spawnPoint = spawnPoints[spawnPointIndex];

        GameObject enemy = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation,
            camp.transform
        );

        enemy.name = prefab.name;
        enemy.SetActive(true);

        DevLog.Log($"[CampSpawner] Spawned {enemy.name} at {spawnPoint.position}");
    }

    // Editor-only visualization (never runs in a build).
    private void OnDrawGizmos()
    {
        if (spawnPoints == null)
            return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform sp = spawnPoints[i];
            if (sp == null)
                continue;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(sp.position, 0.35f);
            Gizmos.DrawRay(sp.position + Vector3.up * 0.05f, sp.forward * 0.8f);
        }
    }
}