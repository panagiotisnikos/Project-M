using UnityEngine;

/// <summary>
/// Gives the visible model a small vertical hop during the heavy attack's leap,
/// so the "jumping slice" reads as an actual jump. Purely cosmetic - it offsets
/// the model child's local Y only, never the Rigidbody / collider / movement, so
/// physics, gravity, the camera and where the player lands are all unaffected.
///
/// Lives on the model GameObject (the one with the Animator), like PlayerRootMotion.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerHeavyHop : MonoBehaviour
{
    [Tooltip("Name of the heavy-attack state in the base layer of the player Animator.")]
    [SerializeField] private string heavyStateName = "HAttack";

    [Header("Hop")]
    [Tooltip("Peak extra height of the model during the leap, in metres.")]
    [SerializeField] private float hopHeight = 0.25f;
    [Tooltip("Start of the hop as a fraction of the HAttack clip.")]
    [Range(0f, 1f)] [SerializeField] private float hopStart = 0.22f;
    [Tooltip("End of the hop as a fraction of the HAttack clip.")]
    [Range(0f, 1f)] [SerializeField] private float hopEnd = 0.62f;
    [Tooltip("How fast the model eases back down if the hop is cut short.")]
    [SerializeField] private float returnSpeed = 14f;

    private Animator animator;
    private float baseY;
    private bool haveBase;

    /// <summary>Diagnostic: the largest hop offset applied so far this session.</summary>
    public float DebugPeakOffset { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        baseY = transform.localPosition.y;
        haveBase = true;
    }

    private void LateUpdate()
    {
        if (!haveBase) return;

        float offset = 0f;

        // The Animator only enters the heavy-attack state on a real heavy attack,
        // so gating on the state name (not a PlayerAttack flag) keeps the hop in
        // sync with the clip even if the attack's internal timers finish first.
        var st = animator.GetCurrentAnimatorStateInfo(0);
        if (st.IsName(heavyStateName) && hopEnd > hopStart)
        {
            float t = Mathf.Repeat(st.normalizedTime, 1f);
            if (t >= hopStart && t <= hopEnd)
            {
                float p = Mathf.InverseLerp(hopStart, hopEnd, t);
                offset = Mathf.Sin(p * Mathf.PI) * hopHeight;
            }
        }

        if (offset > DebugPeakOffset) DebugPeakOffset = offset;

        Vector3 lp = transform.localPosition;
        lp.y = offset > 0.0001f
            ? baseY + offset
            : Mathf.MoveTowards(lp.y, baseY, returnSpeed * Time.deltaTime);
        transform.localPosition = lp;
    }
}
