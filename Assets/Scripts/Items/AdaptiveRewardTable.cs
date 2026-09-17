using UnityEngine;

/// <summary>
/// Lets Balanced/Blossom/Decayed select or reshape a reward pool WITHOUT any
/// region-specific code anywhere in the loot system - the region/world-state
/// influence is entirely data (which RewardTable is assigned to which slot
/// here), never a hardcoded "if region == X" branch in a MonoBehaviour. Any
/// encounter that wants adaptive rewards references one of these assets
/// instead of a plain RewardTable; RewardSource resolves it against whichever
/// WorldRegion is relevant at grant time.
///
/// Philosophy (per design direction): the three tables should offer DIFFERENT
/// opportunities, not a strict power gradient - avoid making one state
/// objectively the "good" or "bad" roll. Leave any table empty to fall back to
/// Balanced.
/// </summary>
[CreateAssetMenu(fileName = "AdaptiveRewardTable_", menuName = "Project M/Loot/Adaptive Reward Table")]
public class AdaptiveRewardTable : ScriptableObject
{
    [Tooltip("Used when there's no region context, or the region currently reads Balanced.")]
    public RewardTable balanced;

    [Tooltip("A different flavour of opportunity, not simply \"better\" - e.g. nature/vitality-leaning resources.")]
    public RewardTable blossom;

    [Tooltip("A different flavour of opportunity, not simply \"worse\" - e.g. harsher, mineral/bone-leaning resources.")]
    public RewardTable decayed;

    public RewardTable Resolve(RegionWorldState state)
    {
        switch (state)
        {
            case RegionWorldState.Blossom: return blossom != null ? blossom : balanced;
            case RegionWorldState.Decayed: return decayed != null ? decayed : balanced;
            default: return balanced;
        }
    }
}
