using UnityEngine;

/// <summary>
/// One enemy to spawn as part of an EncounterComposition - a prefab plus which of the
/// CampSpawner's own Spawn Points to place it at. Plain serializable data, same shape
/// convention as EnemyAttackDefinition/RewardTable.Entry elsewhere in this project.
/// </summary>
[System.Serializable]
public class EncounterSpawnEntry
{
    public GameObject prefab;
    [Tooltip("Index into the CampSpawner's Spawn Points array.")]
    [Min(0)] public int spawnPointIndex;
}
