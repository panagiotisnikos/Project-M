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
            Spawn(stepDustPrefab, footWidth * footSide);
            footSide = -footSide;
            nextStepTime = Time.time + stepInterval;
        }
    }

    private void Spawn(ParticleSystem prefab, float sideOffset)
    {
        if (prefab == null) return;
        // at the feet, offset to the stepping foot and slightly behind the direction of travel
        Vector3 p = transform.position
                    - Vector3.up * legLength
                    + transform.right * sideOffset
                    - transform.forward * trailBias;
        CombatVfx.Play(prefab, p, Vector3.up);
    }
}
