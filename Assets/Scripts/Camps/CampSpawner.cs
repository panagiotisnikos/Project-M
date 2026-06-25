using UnityEngine;

public class CampSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camp camp;
    [SerializeField] private Camp requiredCamp;
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject stalkerPrefab;
    [SerializeField] private GameObject brutePrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private PlayerPerformanceTracker performanceTracker;

    private bool hasSpawned;
    public bool HasSpawned { get; private set; }
    public string LastGeneratedState { get; private set; } = "Not generated yet";
    public string LastComposition { get; private set; } = "Waiting for first camp";
    public float LastScore { get; private set; }
    private void Update()
    {
        if (hasSpawned)
            return;

        if (requiredCamp != null && !requiredCamp.IsCleared)
            return;

        SpawnCamp();
    }

    private void SpawnCamp()
    {
        hasSpawned = true;
        HasSpawned = true;

        WorldAdaptationManager.WorldState state = WorldAdaptationManager.WorldState.Balanced;

        if (worldAdaptationManager != null)
        {
            state = worldAdaptationManager.CurrentState;
        }

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

        LastGeneratedState = state.ToString();

        if (performanceTracker != null)
        {
            LastScore = performanceTracker.GetPerformanceScore();
        }

        if (camp != null)
        {
            camp.RefreshEnemies();
        }

        Debug.Log($"[CampSpawner] Adaptive camp generated: {LastGeneratedState} | {LastComposition} | Score: {LastScore:0.0}");
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

        Debug.Log($"[CampSpawner] Spawned {enemy.name} at {spawnPoint.position}");
    }
}