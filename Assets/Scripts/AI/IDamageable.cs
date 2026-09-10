using UnityEngine;

/// <summary>
/// Anything the player can damage with a melee hit.
///
/// PlayerAttack resolves this from the collider it overlaps, so both regular
/// enemies (EnemyHealth) and the boss (BossHealth) take hits through the same path.
/// </summary>
public interface IDamageable
{
    void TakeDamage(
        int damage,
        Vector3 hitDirection,
        float knockbackMultiplier,
        float reactionDuration);
}
