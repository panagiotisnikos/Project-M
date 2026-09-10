using UnityEngine;

/// <summary>
/// Sits on the creature model (the GameObject with the Animator) so an
/// AnimationEnemyHit event on the Attack clip can reach EnemyAI on the parent.
/// </summary>
public class EnemyAnimEventRelay : MonoBehaviour
{
    private EnemyAI enemyAI;

    private void Awake()
    {
        enemyAI = GetComponentInParent<EnemyAI>();
    }

    /// <summary>Animation event on the Attack clip's impact frame.</summary>
    public void AnimationEnemyHit()
    {
        if (enemyAI != null)
        {
            enemyAI.NotifyAnimationHit();
        }
    }
}
