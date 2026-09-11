using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;

    [Header("Boss Abilities")]
    [SerializeField] private bool canHeal = true;
    [SerializeField] private bool canSummonMinions = true;
    [SerializeField] private bool hasDecayAura = true;

    public string CurrentProfile { get; private set; } = "Unknown";

    /*
     * What the fight (BossCombat / BossDecayAura) is allowed to do right now.
     * Each is the adaptive value AND-ed with "no camp has disabled it".
     */
    public bool CanHeal => canHeal;
    public bool CanSummonMinions => canSummonMinions;
    public bool HasDecayAura => hasDecayAura;

    private bool campDisabledHealing;
    private bool campDisabledSummons;
    private bool campDisabledDecayAura;

    private void Awake()
    {
        if (worldAdaptationManager == null)
        {
            worldAdaptationManager = FindFirstObjectByType<WorldAdaptationManager>();
        }
    }

    private void Update()
    {
        RefreshAbilities();

        if (Input.GetKeyDown(KeyCode.B))
        {
            PrintBossState();
        }
    }

    /// <summary>
    /// The boss starts with its full kit; the only thing that weakens it is
    /// clearing camps (the "prepare, then fight" loop). Adaptation no longer
    /// touches the boss's stats - but the decay aura IS the world's reaction to
    /// a skilled player, so it stays gated to the Decaying world state (see
    /// BossDecayAura). Without this check hasDecayAura defaulted true and
    /// stayed true all fight, since neither existing camp grants the
    /// DisableDecayAura reward - the aura ticked unblockable damage on
    /// proximity the whole time, with no visible attack to explain it.
    /// </summary>
    private void RefreshAbilities()
    {
        canHeal = !campDisabledHealing;
        canSummonMinions = !campDisabledSummons;

        bool worldWantsDecayAura =
            worldAdaptationManager != null &&
            worldAdaptationManager.CurrentState == WorldAdaptationManager.WorldState.Decaying;

        hasDecayAura = worldWantsDecayAura && !campDisabledDecayAura;

        int weakened = (campDisabledHealing ? 1 : 0)
                     + (campDisabledSummons ? 1 : 0)
                     + (campDisabledDecayAura ? 1 : 0);
        CurrentProfile = weakened == 0 ? "Full" : weakened >= 3 ? "Broken" : "Weakened";
    }

    public void DisableHealing()
    {
        campDisabledHealing = true;
        canHeal = false;
        Debug.Log("[Boss] Healing disabled by camp.");
    }

    public void DisableSummons()
    {
        campDisabledSummons = true;
        canSummonMinions = false;
        Debug.Log("[Boss] Summons disabled by camp.");
    }

    public void DisableDecayAura()
    {
        campDisabledDecayAura = true;
        hasDecayAura = false;
        Debug.Log("[Boss] Decay aura disabled by camp.");
    }

    public void PrintBossState()
    {
        Debug.Log($"[Boss] Heal: {canHeal}, Summons: {canSummonMinions}, Decay Aura: {hasDecayAura}");
    }
}