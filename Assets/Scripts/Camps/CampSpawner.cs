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

    private bool hasSpawned;

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

        WorldAdaptationManager.WorldState state = WorldAdaptationManager.WorldState.Balanced;

        if (worldAdaptationManager != null)
        {
            state = worldAdaptationManager.CurrentState;
        }

        switch (state)
        {
            case WorldAdaptationManager.WorldState.Stable:
                SpawnEnemy(stalkerPrefab, 0);
                break;

            case WorldAdaptationManager.WorldState.Balanced:
                SpawnEnemy(stalkerPrefab, 0);
                SpawnEnemy(brutePrefab, 1);
                SpawnEnemy(stalkerPrefab, 2);
                break;

            case WorldAdaptationManager.WorldState.Decaying:
                SpawnEnemy(stalkerPrefab, 0);
                SpawnEnemy(brutePrefab, 1);
                SpawnEnemy(brutePrefab, 2);
                break;
        }

        if (camp != null)
        {
            camp.RefreshEnemies();
        }

        Debug.Log($"[CampSpawner] Spawned adaptive camp based on state: {state}");
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