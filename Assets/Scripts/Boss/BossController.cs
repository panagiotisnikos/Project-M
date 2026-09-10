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

    private void Update()
    {
        AdaptToWorldState();

        if (Input.GetKeyDown(KeyCode.B))
        {
            PrintBossState();
        }
    }

    private void AdaptToWorldState()
    {
        if (worldAdaptationManager == null)
            return;

        bool adaptiveHeal = false;
        bool adaptiveSummons = false;
        bool adaptiveDecayAura = false;

        switch (worldAdaptationManager.CurrentState)
        {
            case WorldAdaptationManager.WorldState.Stable:
                CurrentProfile = "Passive";
                adaptiveHeal = false;
                adaptiveSummons = false;
                adaptiveDecayAura = false;
                break;


            case WorldAdaptationManager.WorldState.Balanced:
                CurrentProfile = "Balanced";
                adaptiveHeal = false;
                adaptiveSummons = true;
                adaptiveDecayAura = false;
                break;

            case WorldAdaptationManager.WorldState.Decaying:
                CurrentProfile = "Aggressive";
                adaptiveHeal = true;
                adaptiveSummons = true;
                adaptiveDecayAura = true;
                break;
        }

        canHeal = adaptiveHeal && !campDisabledHealing;
        canSummonMinions = adaptiveSummons && !campDisabledSummons;
        hasDecayAura = adaptiveDecayAura && !campDisabledDecayAura;
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