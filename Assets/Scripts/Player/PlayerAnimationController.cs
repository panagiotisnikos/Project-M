using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAttack playerAttack;
    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");
    private static readonly int IsBlockingHash =
        Animator.StringToHash("IsBlocking");
    private static readonly int IsMovingForwardHash =
        Animator.StringToHash("IsMovingForward");

    private static readonly int IsMovingBackwardHash =
        Animator.StringToHash("IsMovingBackward");
    private static readonly int MoveXHash =
        Animator.StringToHash("MoveX");

    private static readonly int IsDodgingHash =
        Animator.StringToHash("IsDodging");
    private static readonly int MoveZHash =
        Animator.StringToHash("MoveZ");
    private static readonly int IsAttackingHash =
        Animator.StringToHash("IsAttacking");

    private static readonly int IsHeavyAttackHash =
        Animator.StringToHash("IsHeavyAttack");

    private static readonly int ComboStepHash =
        Animator.StringToHash("ComboStep");
    private void Awake()
    {
        if (animator == null)
        {
            animator =
                GetComponent<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement =
                GetComponentInParent<PlayerMovement>();
        }

        if (playerAttack == null)
        {
            playerAttack =
                GetComponentInParent<PlayerAttack>();
        }
    }

    private void Update()
    {
        if (animator == null ||
            playerMovement == null ||
            playerAttack == null)
        {
            return;
        }

        animator.SetBool(
            IsMovingHash,
            playerMovement.IsMoving
        );
        animator.SetBool(
            IsBlockingHash,
            playerMovement.IsBlocking      
);
        animator.SetBool(
            IsMovingForwardHash,
            playerMovement.IsMovingForward
        );
        animator.SetBool(
            IsMovingBackwardHash,
            playerMovement.IsMovingBackward
        );
        animator.SetFloat(
            MoveXHash,
            playerMovement.MoveX
        );

        animator.SetFloat(
            MoveZHash,
            playerMovement.MoveZ
        );
        animator.SetBool(
            IsDodgingHash,
            playerMovement.IsDodging
        );
        animator.SetBool(
            IsAttackingHash,
            playerAttack.IsAttacking
        );

        animator.SetBool(
            IsHeavyAttackHash,
            playerAttack.IsUsingHeavyAttack
        );

        animator.SetInteger(
            ComboStepHash,
            playerAttack.CurrentComboStep
        );
    }
    public void AnimationAttackHit()
    {
        if (playerAttack != null)
        {
            playerAttack.AnimationAttackHit();
        }
    }

    public void AnimationAttackFinished(int comboStep)
    {
        if (playerAttack != null)
        {
            playerAttack.AnimationAttackFinished(comboStep);
        }
    }
    public void AnimationComboChain(int comboStep)
    {
        if (playerAttack != null)
        {
            playerAttack.AnimationComboChain(comboStep);
        }
    }
}