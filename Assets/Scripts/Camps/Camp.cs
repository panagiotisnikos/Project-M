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

    [Header("Enemies")]
    [SerializeField] private List<EnemyHealth> enemies = new List<EnemyHealth>();

    [Header("Boss Effect")]
    [SerializeField] private BossController boss;
    [SerializeField] private BossWeakeningReward reward;

    private bool isCleared;
    private bool hasRegisteredEnemies;

    public bool IsCleared => isCleared;

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
}