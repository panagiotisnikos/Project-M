using UnityEngine;

public class BossArenaTrigger : MonoBehaviour
{
    [SerializeField] private BossController bossController;
    [SerializeField] private BossCombat bossCombat;
    [SerializeField] private DemoObjectiveManager demoObjectiveManager;
    [SerializeField] private Camp requiredCamp;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (requiredCamp != null && !requiredCamp.IsCleared)
        {
            Debug.Log("[BossArena] Locked. Clear the camp first.");
            if (demoObjectiveManager != null)
            {
                demoObjectiveManager.ShowStatus("Boss arena locked. Clear the camp first.");
            }
            return;
        }

        hasTriggered = true;

        Debug.Log("[BossArena] Player entered boss arena. Fight begins.");

        if (bossController != null)
        {
            bossController.PrintBossState();
        }

        if (bossCombat != null)
        {
            bossCombat.BeginFight();
        }

        if (demoObjectiveManager != null)
        {
            demoObjectiveManager.ShowStatus("Defeat the boss.");
        }
    }
}
