using UnityEngine;

/// <summary>
/// A named, reusable "this is what spawns" data asset for a CampSpawner - the actual
/// mechanism behind "configurable encounter composition, no custom code per camp".
/// Authoring a new encounter shape is "create this asset, list prefabs + spawn point
/// indices", never a new switch-case in CampSpawner.
/// </summary>
[CreateAssetMenu(fileName = "Encounter_", menuName = "Project M/Camps/Encounter Composition")]
public class EncounterComposition : ScriptableObject
{
    [Tooltip("Purely descriptive - shown in CampSpawner's debug fields, no gameplay effect.")]
    public string compositionName = "Composition";

    public EncounterSpawnEntry[] spawns = new EncounterSpawnEntry[0];
}
