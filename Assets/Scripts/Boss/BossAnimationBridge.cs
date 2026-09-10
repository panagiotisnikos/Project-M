using UnityEngine;

/// <summary>
/// Pushes BossCombat / BossHealth state into the Golem's Animator. Mirrors
/// PlayerAnimationController: the gameplay scripts stay animation-agnostic, this
/// component does the translation.
///
/// Animator contract (BossAnimator.controller):
///   float Speed      - 0 idle, ~1 walking
///   trigger Attack   - fire a swing
///   int   AttackType - 0 quick (Mutant Punch), 1 heavy (Mutant Swiping)
///   bool  Dead       - death
/// </summary>
public class BossAnimationBridge : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private BossCombat bossCombat;
    [SerializeField] private BossHealth bossHealth;

    [SerializeField] private float speedDamp = 8f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
    private static readonly int ChargeHitHash = Animator.StringToHash("ChargeHit");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private float speed;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (bossCombat == null)
            bossCombat = GetComponentInParent<BossCombat>();

        if (bossHealth == null)
            bossHealth = GetComponentInParent<BossHealth>();
    }

    private void OnEnable()
    {
        if (bossCombat != null)
        {
            bossCombat.AttackAnimTriggered += HandleAttack;
            bossCombat.ChargeImpact += HandleChargeImpact;
        }

        if (bossHealth != null)
            bossHealth.OnBossDefeated += HandleDefeated;
    }

    private void OnDisable()
    {
        if (bossCombat != null)
        {
            bossCombat.AttackAnimTriggered -= HandleAttack;
            bossCombat.ChargeImpact -= HandleChargeImpact;
        }

        if (bossHealth != null)
            bossHealth.OnBossDefeated -= HandleDefeated;
    }

    private void Update()
    {
        if (animator == null)
            return;

        float target =
            bossCombat != null && bossCombat.IsRepositioning ? 1f : 0f;

        speed = Mathf.MoveTowards(speed, target, speedDamp * Time.deltaTime);
        animator.SetFloat(SpeedHash, speed);
    }

    private void HandleAttack(int attackType)
    {
        if (animator == null)
            return;

        animator.ResetTrigger(ChargeHitHash);
        animator.SetInteger(AttackTypeHash, attackType);
        animator.SetTrigger(AttackHash);
    }

    private void HandleChargeImpact()
    {
        if (animator != null)
            animator.SetTrigger(ChargeHitHash);
    }

    private void HandleDefeated()
    {
        if (animator != null)
            animator.SetBool(DeadHash, true);
    }
}
