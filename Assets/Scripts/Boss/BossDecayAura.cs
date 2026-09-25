using UnityEngine;

/// <summary>
/// A close-range decay field around the boss. While active it ticks unblockable
/// chip damage on the player - spacing pressure so you cannot just camp in melee.
///
/// Active only while the world state / camps leave it enabled
/// (BossController.HasDecayAura), the fight is running, and the boss is alive.
/// </summary>
public class BossDecayAura : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private BossController bossController;
    [SerializeField] private BossCombat bossCombat;
    [SerializeField] private BossHealth bossHealth;

    [Header("Aura")]
    [SerializeField] private float radius = 4f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private int tickDamage = 4;

    [Header("Visual (optional placeholder)")]
    [SerializeField] private Transform auraVisual;

    private float nextTickTime;

    private void Awake()
    {
        if (bossController == null)
            bossController = GetComponent<BossController>();

        if (bossCombat == null)
            bossCombat = GetComponent<BossCombat>();

        if (bossHealth == null)
            bossHealth = GetComponent<BossHealth>();

        if (player == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
                player = pm.transform;
        }

        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        bool active = IsAuraActive();

        if (auraVisual != null && auraVisual.gameObject.activeSelf != active)
        {
            auraVisual.gameObject.SetActive(active);
        }

        if (!active)
            return;

        if (Time.time < nextTickTime)
            return;

        nextTickTime = Time.time + tickInterval;

        if (playerHealth == null || playerHealth.IsDead || player == null)
            return;

        if (Vector3.Distance(transform.position, player.position) <= radius)
        {
            playerHealth.TakeDamage(tickDamage, Vector3.zero);
            DevLog.Log($"[Boss] Decay aura ticked {tickDamage}.");
        }
    }

    private bool IsAuraActive()
    {
        if (bossController == null || !bossController.HasDecayAura)
            return false;

        if (bossCombat != null && !bossCombat.FightActive)
            return false;

        if (bossHealth != null && bossHealth.IsDead)
            return false;

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.1f, 0.6f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
