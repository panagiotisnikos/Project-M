using UnityEngine;

/// <summary>
/// Applies animation root motion to the player ONLY during attacks and dodges,
/// so the heavy swing lunges and the dodge roll travels exactly as animated.
/// Normal locomotion stays input-driven (root motion from walk/run is ignored).
///
/// Lives on the model GameObject (the one with the Animator). The Rigidbody is
/// on the parent.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerRootMotion : MonoBehaviour
{
    [SerializeField] private Rigidbody body;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerMovement playerMovement;

    [Tooltip("Multiplier on the animated travel distance during attacks/dodges.")]
    [SerializeField] private float motionScale = 1f;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (body == null) body = GetComponentInParent<Rigidbody>();
        if (playerAttack == null) playerAttack = GetComponentInParent<PlayerAttack>();
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
    }

    /// <summary>True while an animation should be driving the player's position.</summary>
    public bool RootMotionActive =>
        (playerAttack != null && playerAttack.IsAttacking) ||
        (playerMovement != null && playerMovement.IsDodging);

    private void OnAnimatorMove()
    {
        if (animator == null || body == null || !RootMotionActive)
            return;

        Vector3 delta = animator.deltaPosition * motionScale;
        delta.y = 0f;

        body.MovePosition(body.position + delta);
    }
}
