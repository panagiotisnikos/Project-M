using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

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

    private static readonly int MoveZHash =
        Animator.StringToHash("MoveZ");
    private void Awake()
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }
    }

    private void Update()
    {
        if (animator == null ||
            playerMovement == null)
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
    }
}