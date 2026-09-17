using System.Collections.Generic;
using UnityEngine;

public class Camp : MonoBehaviour
{
    public enum BossWeakeningReward
    {
        DisableHealing,
        DisableSummons,
        DisableDecayAura
    }

    [Header("Camp Info")]
    [SerializeField] private string campName = "Unnamed Camp";
    [Tooltip("Stable id used by the save system. Defaults to the camp name if left blank.")]
    [SerializeField] private string campId = "";

    [Header("Enemies")]
    [SerializeField] private List<EnemyHealth> enemies = new List<EnemyHealth>();

    [Header("Boss Effect")]
    [SerializeField] private BossController boss;
    [SerializeField] private BossWeakeningReward reward;

    [Header("Encounter Reward (optional)")]
    [Tooltip("If assigned, clearing this camp grants whatever this RewardSource " +
             "resolves - if it references an AdaptiveRewardTable, the current " +
             "region's state (Balanced/Blossom/Decayed) picks the pool with no " +
             "region-specific code here at all.")]
    [SerializeField] private RewardSource rewardSource;

    private bool isCleared;
    private bool hasRegisteredEnemies;

    public bool IsCleared => isCleared;
    public string CampId => string.IsNullOrEmpty(campId) ? campName : campId;

    /// <summary>Fired once, the moment this camp is cleared (live combat OR a restored save).</summary>
    public event System.Action Cleared;

    private void Awake()
    {
        RefreshEnemies();
    }

    private void Update()
    {
        if (isCleared)
            return;

        // Important for spawned camps:
        // If the camp starts empty, do not clear it before enemies are spawned.
        if (!hasRegisteredEnemies)
            return;

        enemies.RemoveAll(enemy => enemy == null);

        if (enemies.Count == 0)
        {
            ClearCamp();
        }
    }

    public void RefreshEnemies()
    {
        enemies.Clear();
        enemies.AddRange(GetComponentsInChildren<EnemyHealth>());

        if (enemies.Count > 0)
        {
            hasRegisteredEnemies = true;
            isCleared = false;
        }
    }

    private void ClearCamp()
    {
        isCleared = true;

        Debug.Log($"[Camp] {campName} cleared!");

        ApplyBossEffect();
        GrantEncounterReward();
        Cleared?.Invoke();
    }

    /// <summary>
    /// Instantly mark this camp cleared from a save file - no combat, no re-triggering
    /// the Cleared event (the save already reflects that this camp's reward was applied).
    /// Any enemies still present (a fresh scene load always re-creates the scene-authored
    /// ones) are removed so the player doesn't have to re-fight an already-cleared camp.
    /// </summary>
    public void RestoreCleared()
    {
        if (isCleared)
            return;

        foreach (var enemy in enemies)
            if (enemy != null)
                Destroy(enemy.gameObject);

        enemies.Clear();
        hasRegisteredEnemies = true;
        isCleared = true;

        ApplyBossEffect();
    }

    private void ApplyBossEffect()
    {
        if (boss == null)
        {
            Debug.LogWarning($"[Camp] {campName} has no boss reference.");
            return;
        }

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
}