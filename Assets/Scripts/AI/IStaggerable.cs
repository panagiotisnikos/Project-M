/// <summary>
/// An attacker that a successful player parry can stagger.
///
/// PlayerHealth.HandleParry calls this on whoever landed the parried attack, so
/// a parry works the same against a regular enemy (EnemyAI) and the boss (BossCombat).
/// </summary>
public interface IStaggerable
{
    void Stagger(float duration);
}
