using System.Text;
using UnityEngine;

/// <summary>
/// Grants a reward (spawns ItemPickups) when triggered - the reusable core of
/// Loot & Reward System V1. Reference either a plain RewardTable (fixed) or an
/// AdaptiveRewardTable (resolved against a WorldRegion's current state, or
/// whichever region the player is currently standing in if none is assigned,
/// or Balanced if there's no region context at all). This is the seam that
/// lets Balanced/Blossom/Decayed influence rewards without any per-region code
/// - see AdaptiveRewardTable.
///
/// Not a replacement for LootDropper (still used by existing enemy/chest
/// content) - this is for encounters that want a guaranteed+weighted mix
/// and/or region-adaptive selection: camps, future bosses, special drops.
/// </summary>
public class RewardSource : MonoBehaviour
{
    [Header("Table (assign exactly one)")]
    [SerializeField] private RewardTable table;
    [SerializeField] private AdaptiveRewardTable adaptiveTable;

    [Tooltip("Region to resolve the adaptive table against. Leave empty to use " +
             "whichever region the player currently stands in (WorldRegion.ActiveRegion).")]
    [SerializeField] private WorldRegion region;

    [Header("Spawning")]
    [SerializeField] private ItemPickup pickupPrefab;
    [SerializeField] private float scatter = 0.6f;

    [Header("Debug")]
    [Tooltip("Logs which table/state was resolved and what was rolled - the visibility hook for verifying loot during development.")]
    [SerializeField] private bool logRolls = true;

    private bool granted;

    /// <summary>Grants the reward exactly once. Safe to call more than once - no-ops after the first.</summary>
    public void Grant()
    {
        if (granted)
            return;

        granted = true;

        RegionWorldState state = ResolveState();
        RewardTable resolved = ResolveTable(state);

        if (resolved == null)
        {
            if (logRolls) DevLog.Log($"[RewardSource:{gameObject.name}] no table resolved - nothing granted.");
            return;
        }

        var rewards = resolved.Roll();

        if (logRolls)
        {
            var sb = new StringBuilder();
            sb.Append($"[RewardSource:{gameObject.name}] state={state} table={resolved.name} -> ");
            if (rewards.Count == 0)
            {
                sb.Append("(nothing rolled)");
            }
            else
            {
                foreach (var (item, count) in rewards)
                    sb.Append($"{item.displayName} x{count}, ");
            }
            DevLog.Log(sb.ToString());
        }

        if (pickupPrefab == null)
        {
            Debug.LogWarning($"[RewardSource:{gameObject.name}] no pickupPrefab assigned - rolled rewards were not spawned.");
            return;
        }

        foreach (var (item, count) in rewards)
            SpawnPickup(item, count);
    }

    private RegionWorldState ResolveState()
    {
        WorldRegion activeRegion = region != null ? region : WorldRegion.ActiveRegion;
        return activeRegion != null ? activeRegion.CurrentState : RegionWorldState.Balanced;
    }

    private RewardTable ResolveTable(RegionWorldState state)
    {
        if (adaptiveTable != null)
            return adaptiveTable.Resolve(state);

        return table;
    }

    private void SpawnPickup(ItemData item, int count)
    {
        if (item == null || count <= 0)
            return;

        Vector2 o = Random.insideUnitCircle * scatter;
        Vector3 pos = transform.position + new Vector3(o.x, 0.4f, o.y);

        var p = Instantiate(pickupPrefab, pos, Quaternion.identity);
        p.Configure(item, count);
        p.Toss(new Vector3(o.x, 0f, o.y).normalized);
    }
}
