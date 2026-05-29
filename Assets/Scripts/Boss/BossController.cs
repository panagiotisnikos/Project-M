using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Boss Abilities")]
    [SerializeField] private bool canHeal = true;
    [SerializeField] private bool canSummonMinions = true;
    [SerializeField] private bool hasDecayAura = true;

    public void DisableHealing()
    {
        canHeal = false;
        Debug.Log("Boss healing disabled.");
    }

    public void DisableSummons()
    {
        canSummonMinions = false;
        Debug.Log("Boss summons disabled.");
    }

    public void DisableDecayAura()
    {
        hasDecayAura = false;
        Debug.Log("Boss decay aura disabled.");
    }
}