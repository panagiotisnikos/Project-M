using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A reusable, shareable loot/reward table - generalizes LootDropper's inline
/// per-prefab Entry[] into a weighted-pick model any reward source (enemy,
/// camp, chest, future boss) can reference. LootDropper itself is untouched
/// and still used by existing enemy/chest content - this is the more general
/// system for encounters that want a guaranteed+weighted mix or region-
/// adaptive selection (see AdaptiveRewardTable).
/// </summary>
[CreateAssetMenu(fileName = "RewardTable_", menuName = "Project M/Loot/Reward Table")]
public class RewardTable : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public ItemData item;
        [Min(1)] public int min;
        [Min(1)] public int max;
        [Tooltip("Relative weight within the weighted pool. Ignored for guaranteed entries.")]
        [Min(0f)] public float weight;
    }

    [Header("Always granted")]
    public Entry[] guaranteed;

    [Header("Weighted pool - rollCount picks are drawn from here")]
    public Entry[] weighted;
    [Min(0)] public int rollCount = 1;
    [Tooltip("If false, a weighted entry picked once is removed from the pool for the " +
             "remaining rolls this call (no duplicates in a single grant).")]
    public bool allowRepeats = true;

    /// <summary>Resolves this table into a concrete list of (item, count) to grant. Pure - does
    /// not spawn anything; RewardSource does the actual world spawning.</summary>
    public List<(ItemData item, int count)> Roll()
    {
        var result = new List<(ItemData, int)>();

        if (guaranteed != null)
        {
            foreach (var e in guaranteed)
                if (e.item != null)
                    result.Add((e.item, Random.Range(e.min, Mathf.Max(e.min, e.max) + 1)));
        }

        if (weighted == null || weighted.Length == 0 || rollCount <= 0)
            return result;

        var pool = new List<Entry>(weighted);

        for (int i = 0; i < rollCount && pool.Count > 0; i++)
        {
            float totalWeight = 0f;
            foreach (var e in pool) totalWeight += Mathf.Max(0f, e.weight);
            if (totalWeight <= 0f) break;

            float roll = Random.value * totalWeight;
            float cursor = 0f;
            int pickedIndex = pool.Count - 1;

            for (int j = 0; j < pool.Count; j++)
            {
                cursor += Mathf.Max(0f, pool[j].weight);
                if (roll <= cursor) { pickedIndex = j; break; }
            }

            Entry picked = pool[pickedIndex];
            if (picked.item != null)
                result.Add((picked.item, Random.Range(picked.min, Mathf.Max(picked.min, picked.max) + 1)));

            if (!allowRepeats)
                pool.RemoveAt(pickedIndex);
        }

        return result;
    }
}
