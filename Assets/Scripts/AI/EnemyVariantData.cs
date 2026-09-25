using UnityEngine;

/// <summary>
/// One RegionWorldState's full behavioural override for an existing EnemyData
/// archetype - the actual mechanism behind Adaptive Enemy Variants V1. An
/// archetype opts in by assigning one of these to EnemyData.balancedVariant/
/// blossomVariant/decayedVariant; EnemyAI picks the right one automatically
/// from WorldRegion.ActiveRegion.CurrentState (same signal already used by
/// EnemyData.decayedRecoveryMultiplier/RewardSource's AdaptiveRewardTable).
///
/// Leave a slot empty on EnemyData and that state falls back to the
/// archetype's own base stat block unchanged - existing archetypes
/// (Stalker/Brute/Pressure/Duelist/Heavy) need zero changes.
///
/// Deliberately mirrors EnemyData's own field shapes/defaults (absolute
/// values, not multipliers) so a variant reads as "this state's complete
/// stat block", the same authoring model already used everywhere else in
/// this project - not a delta/patch system.
/// </summary>
[CreateAssetMenu(fileName = "EnemyVariant_", menuName = "Project M/Enemy Variant")]
public class EnemyVariantData : ScriptableObject
{
    [Header("Identity")]
    public string variantLabel = "Variant";

    [Header("Aggression")]
    [Min(0f)] public float detectionRange = 12f;
    [Min(0f)] public float losePlayerRange = 28f;
    [Tooltip("How long the enemy squares up after first noticing the player before it commits to the chase. Lower = snappier/more aggressive reaction.")]
    [Min(0f)] public float alertDuration = 0.5f;

    [Header("Movement / Positioning")]
    [Min(0f)] public float moveSpeed = 9f;
    [Min(0f)] public float rotationSpeed = 12f;
    [Tooltip("Preferred combat distance - how close the enemy tries to stand before it starts an attack.")]
    [Min(0f)] public float stoppingDistance = 2.6f;
    [Tooltip("Max distance from the player an attack can still land.")]
    [Min(0f)] public float attackHitRange = 3f;

    [Header("Attack Cadence (legacy single attack, used only if Attacks below is empty)")]
    [Min(0)] public int attackDamage = 8;
    [Min(0f)] public float attackWindupDuration = 0.65f;
    [Min(0f)] public float attackRecoveryDuration = 0.6f;

    [Header("Attacks (optional weighted pool - special-attack availability lives here)")]
    [Tooltip("Leave empty to use the single legacy attack fields above. A variant can give this " +
             "state its own attack(s) - e.g. a fast low-damage pair only Blossom throws, or a " +
             "single slow high-damage lunge only Decayed throws - without touching the other states.")]
    public EnemyAttackDefinition[] attacks = new EnemyAttackDefinition[0];

    [Header("Defensive Behaviour")]
    public PostAttackBehaviour postAttackBehaviour = PostAttackBehaviour.Reengage;
    [Tooltip("Retreat only: how far the enemy backs off before it's willing to close again.")]
    [Min(0f)] public float retreatDistance = 4f;
    [Min(0f)] public float retreatDuration = 1.2f;

    [Header("Stagger Resistance / Parry Interaction")]
    [Tooltip("Multiplies incoming parry/stagger duration. This is the primary 'how rewarding is it " +
             "to parry this state' lever - keep it lateral (a fast/evasive state can be LESS " +
             "rewarding to parry, not just 'easier').")]
    [Min(0f)] public float staggerDurationMultiplier = 1f;
    [Tooltip("Seconds after an attack's recovery begins during which a stagger lands with a bonus " +
             "multiplier - 'the exploitable opening'. 0 disables this entirely.")]
    [Min(0f)] public float postAttackVulnerabilityWindow = 0f;
    [Min(1f)] public float postAttackVulnerabilityMultiplier = 1f;

    [Header("Visual Hooks (optional - leave empty for a placeholder-free default)")]
    [Tooltip("Whole-material swap for this state (e.g. a Blossom/Decayed look), instantiated at " +
             "runtime so the shared asset is never mutated by the existing telegraph/stagger/hit-flash " +
             "colour changes. Same rig/Animator as the base archetype - only the material differs.")]
    public Material materialOverride;
    [Tooltip("Spawned once at the enemy's position the instant it notices the player (the aggro beat).")]
    public GameObject aggroVfxPrefab;
    [Tooltip("Replaces the archetype's default aggro sound if assigned.")]
    public AudioClip aggroSfxOverride;
}
