using UnityEngine;

/// <summary>
/// Sits on the Golem model (the GameObject with the Animator) so animation
/// events on the attack clips can reach BossCombat, which is on the parent.
/// </summary>
public class BossAnimEventRelay : MonoBehaviour
{
    private BossCombat bossCombat;

    private void Awake()
    {
        bossCombat = GetComponentInParent<BossCombat>();
    }

    /// <summary>Animation event placed on each attack clip's impact frame.</summary>
    public void AnimationBossHit()
    {
        if (bossCombat != null)
        {
            bossCombat.NotifyAnimationHit();
        }
    }
}
