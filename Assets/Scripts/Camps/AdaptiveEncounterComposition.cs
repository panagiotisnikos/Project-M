using UnityEngine;

/// <summary>
/// Lets Balanced/Blossom/Decayed select a different EncounterComposition, with zero
/// region-specific code in CampSpawner - exact mirror of AdaptiveRewardTable's pattern
/// (see Assets/Scripts/Items/AdaptiveRewardTable.cs), applied to enemy composition
/// instead of loot. This is the "AdaptiveRegion integration" hook for encounter
/// composition: assign this instead of a plain EncounterComposition and the camp's
/// makeup follows whichever WorldRegion is relevant, purely through data.
/// </summary>
[CreateAssetMenu(fileName = "AdaptiveEncounter_", menuName = "Project M/Camps/Adaptive Encounter Composition")]
public class AdaptiveEncounterComposition : ScriptableObject
{
    [Tooltip("Used when there's no region context, or the region currently reads Balanced.")]
    public EncounterComposition balanced;

    [Tooltip("A different flavour of encounter, not simply 'easier' - see CampSpawner.SpawnForRegionState for the legacy precedent this generalizes.")]
    public EncounterComposition blossom;

    [Tooltip("A different flavour of encounter, not simply 'harder'.")]
    public EncounterComposition decayed;

    public EncounterComposition Resolve(RegionWorldState state)
    {
        switch (state)
        {
            case RegionWorldState.Blossom: return blossom != null ? blossom : balanced;
            case RegionWorldState.Decayed: return decayed != null ? decayed : balanced;
            default: return balanced;
        }
    }
}
