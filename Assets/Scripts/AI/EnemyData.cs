using UnityEngine;

/// <summary>
/// What an enemy does after its attack recovery ends. Reengage (the original,
/// only behaviour before this framework) immediately swings again if still in
/// range, or resumes the chase - a pressing, relentless feel. Retreat backs
/// off to give the player room before closing again - a deliberate, spacing
/// feel that creates a clean window between attacks instead of an unbroken
/// stream of them.
/// </summary>
public enum PostAttackBehaviour
{
    Reengage,
    Retreat
}

/// <summary>
/// One enemy type's full stat block + behaviour profile - moves the per-species
/// numbers that used to live in EnemyAI/EnemyHealth's hardcoded switch statements
/// into data, the same way WeaponData/ShieldData already do for gear. Adding a
/// new enemy archetype is "create an asset (+ maybe an attacks[] entry or two)",
/// not "add a case to a switch statement" or "write a new AI script".
///
/// Every field added for the enemy-framework pass (attacks[], staggerDurationMultiplier,
/// postAttackBehaviour, the world-state pacing multipliers) defaults to the exact
/// value that reproduces EnemyAI's pre-framework behaviour, so existing archetypes
/// (Enemy_Stalker, Enemy_Brute) work unchanged without touching their assets.
/// </summary>
[CreateAssetMenu(fileName = "Enemy_", menuName = "Project M/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Enemy";

    [Header("Health")]
    [Min(1)] public int maxHealth = 30;

    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 3f;
    [Tooltip("How fast the enemy turns to face its target/movement direction.")]
    [Min(0f)] public float rotationSpeed = 12f;

    [Header("Ranges")]
    [Min(0f)] public float detectionRange = 7f;
    [Min(0f)] public float losePlayerRange = 10f;
    [Tooltip("Preferred combat distance - how close the enemy tries to stand before it starts an attack.")]
    [Min(0f)] public float stoppingDistance = 2.6f;
    [Tooltip("Max distance from the player an attack can still land - the actual hit-connect check.")]
    [Min(0f)] public float attackHitRange = 3f;

    [Header("Attack (used only if Attacks below is empty)")]
    [Min(0)] public int attackDamage = 10;
    [Min(0f)] public float attackWindupDuration = 0.6f;
    [Min(0f)] public float attackRecoveryDuration = 0.8f;

    [Header("Attacks (optional - a weighted pool this archetype picks from instead of the single attack above)")]
    [Tooltip("Leave empty to use the single legacy attack fields above unchanged. " +
             "Populate this to give an archetype multiple attacks with their own damage/timing " +
             "(e.g. a fast jab mixed with a slower haymaker) - the core mechanism behind making " +
             "archetypes feel different through TIMING rather than just bigger numbers.")]
    public EnemyAttackDefinition[] attacks = new EnemyAttackDefinition[0];

    [Header("Stagger / Parry Interaction")]
    [Tooltip("Multiplies incoming parry/stagger duration. >1 stays dazed longer (a bigger " +
             "punish reward for a well-timed parry); <1 shrugs it off faster. 1 = unchanged. " +
             "This is the primary 'how rewarding is it to parry this archetype' lever.")]
    [Min(0f)] public float staggerDurationMultiplier = 1f;
    [Tooltip("Seconds after an attack's recovery begins during which a stagger lands with a bonus " +
             "multiplier on top of staggerDurationMultiplier - 'the clean opening right after the " +
             "swing'. 0 disables this entirely (the default - Pressure/Heavy don't use it).")]
    [Min(0f)] public float postAttackVulnerabilityWindow = 0f;
    [Tooltip("Extra multiplier applied to a stagger landed inside postAttackVulnerabilityWindow. " +
             "Only matters if that window is > 0.")]
    [Min(1f)] public float postAttackVulnerabilityMultiplier = 1f;

    [Header("Post-Attack Behaviour")]
    public PostAttackBehaviour postAttackBehaviour = PostAttackBehaviour.Reengage;
    [Tooltip("Retreat only: how far the enemy backs off before it's willing to close again.")]
    [Min(0f)] public float retreatDistance = 4f;
    [Min(0f)] public float retreatDuration = 1.2f;

    [Header("World-State Pacing (opt-in atmospheric flavor, not a difficulty dial - see WorldRegion)")]
    [Tooltip("Multiplies attack recovery duration while the player is standing in a Decayed region. 1 = no effect.")]
    [Min(0.1f)] public float decayedRecoveryMultiplier = 1f;
    [Tooltip("Multiplies attack recovery duration while the player is standing in a Blossom region. 1 = no effect.")]
    [Min(0.1f)] public float blossomRecoveryMultiplier = 1f;

    [Header("World-State Variants (optional - Adaptive Enemy Variants V1, see EnemyVariantData)")]
    [Tooltip("Full behavioural override while the player's region reads Balanced. Leave empty to use " +
             "this archetype's own base stat block above - Balanced is the reference point the other " +
             "two states diverge from, not a fourth flavor that needs its own asset.")]
    public EnemyVariantData balancedVariant;
    [Tooltip("Full behavioural override while the player's region reads Blossom. Leave empty to fall " +
             "back to the base stat block above.")]
    public EnemyVariantData blossomVariant;
    [Tooltip("Full behavioural override while the player's region reads Decayed. Leave empty to fall " +
             "back to the base stat block above.")]
    public EnemyVariantData decayedVariant;
}
