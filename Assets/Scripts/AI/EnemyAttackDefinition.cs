using UnityEngine;

/// <summary>
/// One attack an enemy archetype can throw - damage + timing only, no new
/// Animator work required (all attacks in an archetype still fire the same
/// shared "Attack" trigger via EnemyAnimationBridge; a rig with several
/// distinct swings could later map attackName to a specific clip, but V1
/// intentionally reuses one clip per rig, same as before this framework).
///
/// EnemyData.attacks holds an array of these. Leave it empty and an enemy
/// falls back to EnemyData's single legacy attackDamage/Windup/Recovery
/// fields unchanged - existing archetypes (Stalker, Brute) need no changes
/// to keep behaving exactly as they did before this framework existed.
/// </summary>
[System.Serializable]
public class EnemyAttackDefinition
{
    public string attackName = "Attack";
    [Min(0)] public int damage = 10;
    [Min(0f)] public float windupDuration = 0.6f;
    [Min(0f)] public float recoveryDuration = 0.8f;

    [Tooltip("Relative chance this attack is picked when more than one is available. " +
             "Equal weights = uniform random; a higher weight just means more often, not exclusively.")]
    [Min(0.01f)] public float weight = 1f;
}
