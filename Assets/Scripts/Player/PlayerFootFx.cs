using UnityEngine;

/// <summary>
/// Small ground feedback for the player: a dust puff each footfall while running,
/// and a bigger kick-up when a dodge roll ends. Self-contained - reads
/// PlayerMovement and spawns a serialized particle prefab (safe when null).
/// </summary>
public class PlayerFootFx : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ParticleSystem stepDustPrefab;
    [SerializeField] private ParticleSystem landDustPrefab;

    [Tooltip("Seconds between footfall puffs while running.")]
    [SerializeField] private float stepInterval = 0.46f;
    [Tooltip("Distance from the player pivot down to the ground.")]
    [SerializeField] private float legLength = 0.95f;
    [Tooltip("How far to the side each footfall alternates.")]
    [SerializeField] private float footWidth = 0.16f;
    [Tooltip("Puff spawns slightly behind the player as they move.")]
    [SerializeField] private float trailBias = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioClip stepLeftSfx;
    [SerializeField] private AudioClip stepRightSfx;
    [Tooltip("Played instead of the grass L/R pair when a downward ray from the foot hits " +
             "something whose name suggests stone (camp paths, standing stones, rock props).")]
    [SerializeField] private AudioClip stoneStepSfx;
    [Range(0f, 1f)] [SerializeField] private float stepVolume = 0.3f;
    [SerializeField] private LayerMask groundRayMask = ~0;

    private float nextStepTime;
    private bool wasDodging;
    private int footSide = 1;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (playerMovement == null) return;

        if (wasDodging && !playerMovement.IsDodging)
        {
            Spawn(landDustPrefab != null ? landDustPrefab : stepDustPrefab, 0f);
            nextStepTime = Time.time + stepInterval;
        }
        wasDodging = playerMovement.IsDodging;

        if (!playerMovement.IsDodging && playerMovement.IsMoving && Time.time >= nextStepTime)
        {
            Vector3 footPos = Spawn(stepDustPrefab, footWidth * footSide);
            PlayFootstepAudio(footPos);
            footSide = -footSide;
            nextStepTime = Time.time + stepInterval;
        }
    }

    private Vector3 Spawn(ParticleSystem prefab, float sideOffset)
    {
        // at the feet, offset to the stepping foot and slightly behind the direction of travel
        Vector3 p = transform.position
                    - Vector3.up * legLength
                    + transform.right * sideOffset
                    - transform.forward * trailBias;
        if (prefab != null) CombatVfx.Play(prefab, p, Vector3.up);
        return p;
    }

    private void PlayFootstepAudio(Vector3 footPos)
    {
        bool onStone = false;
        if (Physics.Raycast(footPos + Vector3.up * 0.5f, Vector3.down, out var hit, 1.5f, groundRayMask, QueryTriggerInteraction.Ignore))
        {
            string n = hit.collider.name.ToLowerInvariant();
            onStone = n.Contains("stone") || n.Contains("rock") || n.Contains("cliff") || n.Contains("path");
        }

        if (onStone && stoneStepSfx != null)
        {
            CombatAudio.Play(stoneStepSfx, footPos, stepVolume);
            return;
        }

        CombatAudio.Play(footSide > 0 ? stepRightSfx : stepLeftSfx, footPos, stepVolume);
    }
}
