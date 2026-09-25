using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, IStaggerable
{
    private enum EnemyState
    {
        Idle,
        Alerted,
        Chase,
        AttackWindup,
        AttackRecovery,
        Retreat,
        HitReact,
        Staggered
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;

    [Header("Data")]
    [Tooltip("Stat block for this enemy type. Overwrites the fields below at Awake if assigned.")]
    [SerializeField] private EnemyData enemyData;

    /// <summary>The stat-block asset this enemy was configured from, if any.</summary>
    public EnemyData Data => enemyData;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Ranges")]
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float losePlayerRange = 10f;
    [SerializeField] private float stoppingDistance = 2.6f;
    [SerializeField] private float attackHitRange = 3f;

    [Header("Attack Timing")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackWindupDuration = 0.6f;
    [SerializeField] private float attackRecoveryDuration = 0.8f;

    [Header("Attack Telegraph")]
    [SerializeField] private Color attackTelegraphColor = Color.yellow;

    [Header("Stagger")]
    [SerializeField] private Color staggerColor = Color.cyan;

    [Header("Audio")]
    [Tooltip("Played the instant this enemy notices the player (role-specific: Brute/StalkerAggro).")]
    [SerializeField] private AudioClip aggroSfx;
    [Range(0f, 1f)] [SerializeField] private float aggroVolume = 0.5f;

    [Header("Idle Life")]
    [Tooltip("How far the enemy drifts from its start point while idle.")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float wanderMinPause = 2.5f;
    [SerializeField] private float wanderMaxPause = 6.5f;
    [Tooltip("Fraction of move speed used for an idle stroll.")]
    [SerializeField] private float wanderSpeedMultiplier = 0.35f;
    [Tooltip("Beat where the enemy notices the player and squares up before charging.")]
    [SerializeField] private float alertDuration = 0.5f;

    private Vector3 homePosition;
    private Vector3 wanderPoint;
    private bool hasWanderPoint;
    private float nextWanderTime;
    private float idleLookYaw;
    private float nextIdleLookTime;
    private float alertStartTime;
    private float combatStoppingDistance = 2.2f;

    /// <summary>Fired the instant the enemy notices the player (for a roar / aggro sting).</summary>
    public event System.Action AggroReacted;

private float staggerEndTime;
    private Rigidbody rb;
    private Renderer enemyRenderer;
    private Transform modelTransform;
    private Vector3 modelBaseScale = Vector3.one;


    /*
     * The attack lands when the attack animation reaches its impact frame
     * (AnimationEnemyHit event -> NotifyAnimationHit). stateEndTime is only a
     * fallback, so the hit always matches the visible swing and the player can
     * read the wind-up to time a parry.
     */
    private bool animHitPending;

    private EnemyState currentState = EnemyState.Idle;
    private float stateEndTime;

    /// <summary>The attack chosen for the current windup, if EnemyData.attacks is
    /// populated - null means "use the legacy single-attack fields" (attackDamage/
    /// attackWindupDuration/attackRecoveryDuration), keeping old archetypes unchanged.</summary>
    private EnemyAttackDefinition currentAttackChoice;

    // --- Adaptive Enemy Variants V1 ---
    // The fields below mirror the handful of EnemyData values that aren't already
    // copied into a plain runtime field by ApplyEnemyData() (moveSpeed, stoppingDistance,
    // etc. already are, and ApplyVariant() below overwrites those same fields directly).
    // Every state-machine method reads these "active*" fields instead of enemyData.xxx
    // directly, so a variant swap never needs to touch the shared EnemyData/EnemyVariantData
    // assets themselves - only this instance's copies.
    private EnemyVariantData activeVariant;
    private RegionWorldState? lastAppliedRegionState;
    private EnemyAttackDefinition[] activeAttacks;
    private PostAttackBehaviour activePostAttackBehaviour = PostAttackBehaviour.Reengage;
    private float activeRetreatDistance = 4f;
    private float activeRetreatDuration = 1.2f;
    private float activeStaggerDurationMultiplier = 1f;
    private float activePostAttackVulnerabilityWindow = 0f;
    private float activePostAttackVulnerabilityMultiplier = 1f;
    private float baseAlertDuration;
    private Material baseMaterialAsset;
    private Material variantMaterialInstance;
    private EnemyVariantData appliedVisualVariant;

    /// <summary>The variant currently applied for this instance's resolved region state, or
    /// null if this archetype doesn't use variants (or the current state has no variant asset
    /// assigned, i.e. it's running the archetype's own base stat block).</summary>
    public EnemyVariantData ActiveVariant => activeVariant;

    /// <summary>The RegionWorldState this instance last resolved its variant against.</summary>
    public RegionWorldState ResolvedRegionState => lastAppliedRegionState ?? RegionWorldState.Balanced;

    private Color originalColor;
    private NavMeshAgent agent;

    private Vector3 lastPosition;
    private float measuredSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyRenderer = GetComponentInChildren<Renderer>();
        agent = GetComponent<NavMeshAgent>();
        lastPosition = transform.position;
        homePosition = transform.position;
        nextWanderTime = Time.time + Random.Range(wanderMinPause, wanderMaxPause);

        // The NavMeshAgent drives the transform every frame; an interpolated
        // kinematic Rigidbody fights that and the agent appears frozen.
        rb.interpolation = RigidbodyInterpolation.None;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.isKinematic = true;

        baseAlertDuration = alertDuration;

        if (enemyRenderer != null)
        {
            baseMaterialAsset = enemyRenderer.sharedMaterial;
            originalColor = enemyRenderer.material.color;
            modelTransform = enemyRenderer.transform;
            modelBaseScale = modelTransform.localScale;
        }

        ApplyEnemyData();

        // NavMesh drives movement; we still face the player ourselves.
        agent.updateRotation = false;   // we face the player ourselves
        agent.updateUpAxis = false;
        agent.speed = moveSpeed;
        agent.acceleration = 40f;
        agent.angularSpeed = 999f;      // internal steering still needs this > 0 to move
        RefreshCombatStoppingDistance();
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            agent.baseOffset = cap.height * 0.5f + cap.center.y;
            agent.radius = Mathf.Max(0.15f, cap.radius * 0.85f);
            agent.height = cap.height;
        }

        if (player == null)
        {
            PlayerMovement playerMovement =
                FindFirstObjectByType<PlayerMovement>();

            if (playerMovement != null)
            {
                player = playerMovement.transform;
            }
        }

        if (worldAdaptationManager == null)
        {
            worldAdaptationManager =
                FindFirstObjectByType<WorldAdaptationManager>();
        }
    }

    private void Update()
    {
        UpdateVariant();
        UpdateState();
        UpdateTelegraphPunch();
        TrackRealSpeed();
    }

    private void TrackRealSpeed()
    {
        float dt = Mathf.Max(0.0001f, Time.deltaTime);
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        // low-pass so the run/idle blend does not flicker
        measuredSpeed = Mathf.Lerp(measuredSpeed, delta.magnitude / dt, 12f * dt);
        lastPosition = transform.position;
    }

    private void UpdateTelegraphPunch()
    {
        // The old model-scale "punch" read as a deflating balloon. The wind-up
        // tell is now the Attack animation's own anticipation + the colour flash
        // (SetTelegraphVisual). Keep the model at its authored scale.
        if (modelTransform == null)
            return;

        if (modelTransform.localScale != modelBaseScale)
            modelTransform.localScale = modelBaseScale;
    }

    private void FixedUpdate()
    {
        RunState();
    }

    private void UpdateState()
    {
        float distanceToPlayer = GetDistanceToPlayer();

        switch (currentState)
        {
            case EnemyState.Idle:

                if (distanceToPlayer <= GetAdaptedDetectionRange())
                {
                    EnterAlerted();
                }
                else
                {
                    UpdateIdleWander();
                }

                break;

            case EnemyState.Alerted:

                if (Time.time >= stateEndTime)
                {
                    ChangeState(EnemyState.Chase);
                }

                break;

            case EnemyState.Chase:

                if (distanceToPlayer >= GetAdaptedLosePlayerRange())
                {
                    ChangeState(EnemyState.Idle);
                }
                else if (distanceToPlayer <= stoppingDistance)
                {
                    BeginAttackWindup();
                }

                break;

            case EnemyState.AttackWindup:

                if (animHitPending || Time.time >= stateEndTime)
                {
                    animHitPending = false;
                    PerformAttack();

                    /*
                    * A successful parry changes the enemy state
                    * to Staggered during PerformAttack().
                    *
                    * Do not overwrite that state with recovery.
                    */
                    if (currentState != EnemyState.Staggered)
                    {
                        BeginAttackRecovery();
                    }
                }

                break;

            case EnemyState.AttackRecovery:

                if (Time.time >= stateEndTime)
                {
                    if (activePostAttackBehaviour == PostAttackBehaviour.Retreat)
                    {
                        BeginRetreat();
                    }
                    else if (distanceToPlayer <= stoppingDistance)
                    {
                        BeginAttackWindup();
                    }
                    else
                    {
                        ChangeState(EnemyState.Chase);
                    }
                }

                break;

            case EnemyState.Retreat:

                if (Time.time >= stateEndTime)
                {
                    ChangeState(EnemyState.Chase);
                }

                break;

            case EnemyState.HitReact:

                if (Time.time >= stateEndTime)
                {
                    if (distanceToPlayer <= GetAdaptedLosePlayerRange())
                    {
                        ChangeState(EnemyState.Chase);
                    }
                    else
                    {
                        ChangeState(EnemyState.Idle);
                    }
                }

                break;                

            case EnemyState.Staggered:

                if (Time.time >= staggerEndTime)
                {
                    SetStaggerVisual(false);

                    if (distanceToPlayer <= GetAdaptedLosePlayerRange())
                    {
                        ChangeState(EnemyState.Chase);
                    }
                    else
                    {
                        ChangeState(EnemyState.Idle);
                    }
                }

                break;
        }
    }

    private void RunState()
    {
        switch (currentState)
        {
            case EnemyState.Chase:

                ChasePlayer();
                break;

            case EnemyState.AttackWindup:
            case EnemyState.AttackRecovery:

                StopMoving();
                FacePlayer();
                break;

            case EnemyState.Retreat:

                RetreatFromPlayer();
                break;

            case EnemyState.Alerted:

                StopMoving();
                FacePlayer();
                break;

            case EnemyState.Idle:

                RunIdle();
                break;

            case EnemyState.HitReact:
            case EnemyState.Staggered:

                StopMoving();
                break;
        }
    }

    private void EnterAlerted()
    {
        ChangeState(EnemyState.Alerted);
        stateEndTime = Time.time + alertDuration;
        alertStartTime = Time.time;
        hasWanderPoint = false;
        SetTelegraphVisual(false);

        AudioClip aggroClip =
            (activeVariant != null && activeVariant.aggroSfxOverride != null)
                ? activeVariant.aggroSfxOverride
                : aggroSfx;
        CombatAudio.Play(aggroClip, transform.position, aggroVolume);

        if (activeVariant != null && activeVariant.aggroVfxPrefab != null)
        {
            Instantiate(activeVariant.aggroVfxPrefab, transform.position + Vector3.up * 0.9f, Quaternion.identity);
        }

        AggroReacted?.Invoke();
    }

    /// <summary>
    /// Idle isn't "stand frozen": the enemy strolls short distances around its
    /// spawn point and glances about between strolls, so a camp reads as alive.
    /// </summary>
    private void UpdateIdleWander()
    {
        if (hasWanderPoint)
        {
            Vector3 flat = wanderPoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude <= 0.6f * 0.6f)
            {
                hasWanderPoint = false;
                nextWanderTime = Time.time + Random.Range(wanderMinPause, wanderMaxPause);
                nextIdleLookTime = Time.time + Random.Range(0.8f, 2f);
            }
            return;
        }

        if (Time.time >= nextWanderTime)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(Mathf.Max(2.5f, stoppingDistance + 1f), wanderRadius);
            Vector3 candidate = homePosition + new Vector3(dir.x, 0f, dir.y) * dist;
            if (NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas))
            {
                wanderPoint = hit.position;
                hasWanderPoint = true;
            }
            else
            {
                nextWanderTime = Time.time + 1f;
            }
        }
    }

    private void RunIdle()
    {
        if (hasWanderPoint && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.autoBraking = true;
            agent.stoppingDistance = 0.1f;   // combat stopping distance is restored in ChasePlayer
            agent.speed = Mathf.Max(0.5f, GetAdaptedMoveSpeed() * wanderSpeedMultiplier);
            agent.SetDestination(wanderPoint);

            Vector3 v = agent.velocity;
            v.y = 0f;
            if (v.sqrMagnitude > 0.04f)
                RotateTowards(v.normalized);
            return;
        }

        StopMoving();

        // idle glances - retarget a lazy look now and then
        if (Time.time >= nextIdleLookTime)
        {
            idleLookYaw = transform.eulerAngles.y + Random.Range(-80f, 80f);
            nextIdleLookTime = Time.time + Random.Range(2f, 4.5f);
        }
        Quaternion look = Quaternion.Euler(0f, idleLookYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, 2.5f * Time.fixedDeltaTime);
    }

    /// <summary>True while chasing the player.</summary>
    public bool IsChasing => currentState == EnemyState.Chase;

    /// <summary>
    /// 0..1 locomotion for the run/idle blend. Uses the agent's own (smooth)
    /// velocity, but forced to 0 if the transform hasn't actually moved for a
    /// beat - so a stuck agent plays idle, not a running-in-place loop.
    /// </summary>
    public float LocomotionSpeed01
    {
        get
        {
            // idle stroll -> a gentle walk, never a run
            if (currentState == EnemyState.Idle)
                return hasWanderPoint && measuredSpeed > 0.15f ? 0.16f : 0f;

            if (currentState != EnemyState.Chase)
                return 0f;

            float agentSpeed = agent != null && agent.isOnNavMesh
                ? agent.velocity.magnitude
                : 0f;

            // if the agent claims to move but the body isn't, it's wedged
            if (agentSpeed > 0.4f && measuredSpeed < 0.15f)
                return 0f;

            return Mathf.Clamp01(agentSpeed / Mathf.Max(1f, moveSpeed));
        }
    }

    /// <summary>Fired when a wind-up begins, so the animator can play the attack clip.</summary>
    public event System.Action AttackStarted;

    /// <summary>Called from the attack animation's impact frame (via EnemyAnimEventRelay).</summary>
    public void NotifyAnimationHit()
    {
        if (currentState == EnemyState.AttackWindup)
        {
            animHitPending = true;
        }
    }

    /// <summary>Weighted-random pick from EnemyData.attacks, or null if that array
    /// is empty (the archetype uses the single legacy attack fields instead).</summary>
    private EnemyAttackDefinition ChooseAttack()
    {
        if (activeAttacks == null || activeAttacks.Length == 0)
            return null;

        if (activeAttacks.Length == 1)
            return activeAttacks[0];

        float totalWeight = 0f;
        foreach (var atk in activeAttacks)
            totalWeight += Mathf.Max(0.01f, atk.weight);

        float roll = Random.value * totalWeight;

        foreach (var atk in activeAttacks)
        {
            roll -= Mathf.Max(0.01f, atk.weight);
            if (roll <= 0f)
                return atk;
        }

        return activeAttacks[activeAttacks.Length - 1];
    }

    private void BeginAttackWindup()
    {
        ChangeState(EnemyState.AttackWindup);

        currentAttackChoice = ChooseAttack();

        float baseWindup =
            currentAttackChoice != null
                ? currentAttackChoice.windupDuration
                : attackWindupDuration;

        float adaptedWindupDuration =
            baseWindup * GetAttackCooldownModifier();

        animHitPending = false;

        // Fallback only - the hit really fires on the AnimationEnemyHit event.
        stateEndTime = Time.time + adaptedWindupDuration * 2.5f + 0.3f;

        SetTelegraphVisual(true);
        AttackStarted?.Invoke();

        DevLog.Log(
            $"[{gameObject.name}] Attack wind-up started. " +
            $"Hit in {adaptedWindupDuration:0.00}s."
        );
    }

    private void PerformAttack()
    {
        SetTelegraphVisual(false);

        if (player == null)
            return;

        float distanceToPlayer = GetDistanceToPlayer();

        if (distanceToPlayer > attackHitRange)
        {
            DevLog.Log($"[{gameObject.name}] Attack missed.");
            return;
        }

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsDead)
            return;

        Vector3 hitDirection =
            player.position - transform.position;

        hitDirection.y = 0f;
        hitDirection.Normalize();

        int damage = currentAttackChoice != null ? currentAttackChoice.damage : attackDamage;

        playerHealth.TakeDamage(
            damage,
            hitDirection,
            this
        );

        DevLog.Log(
            $"[{gameObject.name}] {(currentAttackChoice != null ? currentAttackChoice.attackName : "Attack")} " +
            $"hit for {damage} damage."
        );
    }

    private void BeginAttackRecovery()
    {
        ChangeState(EnemyState.AttackRecovery);

        float baseRecovery =
            currentAttackChoice != null
                ? currentAttackChoice.recoveryDuration
                : attackRecoveryDuration;

        float adaptedRecoveryDuration =
            baseRecovery * GetAttackCooldownModifier() * GetRegionRecoveryModifier();

        stateEndTime = Time.time + adaptedRecoveryDuration;

        // "The clean opening right after the swing" - see EnemyVariantData.postAttackVulnerabilityWindow.
        if (activePostAttackVulnerabilityWindow > 0f)
            vulnerableUntil = Time.time + activePostAttackVulnerabilityWindow;
        else
            vulnerableUntil = -1f;
    }

    private float vulnerableUntil = -1f;

    /// <summary>
    /// Opt-in atmospheric flavor: an archetype can set decayedRecoveryMultiplier/
    /// blossomRecoveryMultiplier away from 1 to feel a touch more relentless or
    /// more sluggish depending on the region the player is currently standing in
    /// (see WorldRegion). Defaults to 1 (no effect) - this deliberately does NOT
    /// revive the old global difficulty dial (GetAttackCooldownModifier stays
    /// pinned at 1); it only ever affects the archetypes that explicitly opt in.
    /// </summary>
    private float GetRegionRecoveryModifier()
    {
        if (enemyData == null)
            return 1f;

        RegionWorldState? state = WorldRegion.ActiveRegion?.CurrentState;

        if (state == RegionWorldState.Decayed)
            return enemyData.decayedRecoveryMultiplier;

        if (state == RegionWorldState.Blossom)
            return enemyData.blossomRecoveryMultiplier;

        return 1f;
    }

    private Vector3 lastDestination = new Vector3(9999f, 9999f, 9999f);

    private void ChasePlayer()
    {
        RotateTowards(GetDirectionToPlayer());

        if (agent == null || !agent.isOnNavMesh || player == null)
            return;

        agent.speed = GetAdaptedMoveSpeed();
        agent.stoppingDistance = combatStoppingDistance;

        if (GetDistanceToPlayer() <= stoppingDistance)
        {
            StopMoving();
            return;
        }

        if (agent.isStopped)
            agent.isStopped = false;

        // Only re-path when the target has actually moved - calling SetDestination
        // every frame thrashes the path and makes the agent stutter.
        if ((player.position - lastDestination).sqrMagnitude > 0.35f)
        {
            lastDestination = player.position;
            agent.SetDestination(player.position);
        }
    }

    private void FacePlayer()
    {
        RotateTowards(GetDirectionToPlayer());
    }

    /// <summary>PostAttackBehaviour.Retreat: back off to a point away from the
    /// player instead of immediately re-engaging - creates a clean gap after an
    /// attack instead of an unbroken chain of them (the DEFENSIVE archetype's
    /// "spacing" identity). Keeps facing the player while backing away, same as
    /// every other state - only the agent's destination is behind the enemy.</summary>
    private void BeginRetreat()
    {
        ChangeState(EnemyState.Retreat);
        stateEndTime = Time.time + activeRetreatDuration;

        if (agent == null || !agent.isOnNavMesh || player == null)
            return;

        float distance = activeRetreatDistance;
        Vector3 awayDir = (transform.position - player.position);
        awayDir.y = 0f;
        awayDir = awayDir.sqrMagnitude > 0.0001f ? awayDir.normalized : -transform.forward;

        Vector3 candidate = transform.position + awayDir * distance;

        if (NavMesh.SamplePosition(candidate, out var hit, distance + 1f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.stoppingDistance = 0.1f;
            agent.SetDestination(hit.position);
        }
    }

    private void RetreatFromPlayer()
    {
        RotateTowards(GetDirectionToPlayer());

        if (agent == null || !agent.isOnNavMesh)
            return;

        agent.speed = GetAdaptedMoveSpeed();
    }

    private void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
    }

    private void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    /// <summary>A small shove along the navmesh, used for hit knockback.</summary>
    public void Nudge(Vector3 direction, float distance)
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        direction.y = 0f;
        agent.Move(direction.normalized * Mathf.Clamp(distance, 0f, 0.6f));
    }

    private void OnDisable()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
    }

    private void SetTelegraphVisual(bool isActive)
    {
        if (enemyRenderer == null)
            return;

        enemyRenderer.material.color =
            isActive ? attackTelegraphColor : originalColor;
    }
    private void SetStaggerVisual(bool isActive)
    {
        if (enemyRenderer == null)
            return;

        enemyRenderer.material.color =
            isActive ? staggerColor : originalColor;
    }
    /// <summary>Configures this instance to the archetype's own base stat block - both the
    /// original per-species setup at Awake, and (via ApplyVariant) what "no variant assigned
    /// for this region state" falls back to.</summary>
    private void ApplyEnemyData()
    {
        if (enemyData == null)
            return;

        moveSpeed = enemyData.moveSpeed;
        rotationSpeed = enemyData.rotationSpeed;
        detectionRange = enemyData.detectionRange;
        losePlayerRange = enemyData.losePlayerRange;
        stoppingDistance = enemyData.stoppingDistance;
        attackHitRange = enemyData.attackHitRange;
        attackDamage = enemyData.attackDamage;
        attackWindupDuration = enemyData.attackWindupDuration;
        attackRecoveryDuration = enemyData.attackRecoveryDuration;
        alertDuration = baseAlertDuration;

        activeVariant = null;
        activeAttacks = enemyData.attacks;
        activePostAttackBehaviour = enemyData.postAttackBehaviour;
        activeRetreatDistance = enemyData.retreatDistance;
        activeRetreatDuration = enemyData.retreatDuration;
        activeStaggerDurationMultiplier = enemyData.staggerDurationMultiplier;
        activePostAttackVulnerabilityWindow = enemyData.postAttackVulnerabilityWindow;
        activePostAttackVulnerabilityMultiplier = enemyData.postAttackVulnerabilityMultiplier;
    }

    private void RefreshCombatStoppingDistance()
    {
        combatStoppingDistance = Mathf.Max(0.2f, stoppingDistance - 0.4f);
        if (agent != null)
            agent.stoppingDistance = combatStoppingDistance;
    }

    /// <summary>
    /// Adaptive Enemy Variants V1: re-checked every frame (two cheap reads) but only actually
    /// re-applies when the resolved RegionWorldState changes - the same automatic-selection
    /// signal EnemyData.decayedRecoveryMultiplier/RewardSource's AdaptiveRewardTable already use
    /// (WorldRegion.ActiveRegion, "whichever region the player currently stands in"). A region
    /// flipping state mid-fight is picked up on the enemy's next Update - it doesn't retroactively
    /// change an attack already in flight (currentAttackChoice is cached separately), only what it
    /// does next.
    /// </summary>
    private void UpdateVariant()
    {
        if (enemyData == null)
            return;

        RegionWorldState resolvedState =
            WorldRegion.ActiveRegion != null
                ? WorldRegion.ActiveRegion.CurrentState
                : RegionWorldState.Balanced;

        if (lastAppliedRegionState.HasValue && lastAppliedRegionState.Value == resolvedState)
            return;

        lastAppliedRegionState = resolvedState;

        EnemyVariantData variant = resolvedState switch
        {
            RegionWorldState.Blossom => enemyData.blossomVariant,
            RegionWorldState.Decayed => enemyData.decayedVariant,
            _ => enemyData.balancedVariant
        };

        ApplyVariant(variant);
    }

    private void ApplyVariant(EnemyVariantData variant)
    {
        if (variant == null)
        {
            // No authored variant for this state - the archetype's own base stat block IS the fallback.
            ApplyEnemyData();
            RefreshCombatStoppingDistance();
            ApplyVariantVisuals(null);
            return;
        }

        activeVariant = variant;

        moveSpeed = variant.moveSpeed;
        rotationSpeed = variant.rotationSpeed;
        detectionRange = variant.detectionRange;
        losePlayerRange = variant.losePlayerRange;
        stoppingDistance = variant.stoppingDistance;
        attackHitRange = variant.attackHitRange;
        attackDamage = variant.attackDamage;
        attackWindupDuration = variant.attackWindupDuration;
        attackRecoveryDuration = variant.attackRecoveryDuration;
        alertDuration = variant.alertDuration;

        activeAttacks = variant.attacks;
        activePostAttackBehaviour = variant.postAttackBehaviour;
        activeRetreatDistance = variant.retreatDistance;
        activeRetreatDuration = variant.retreatDuration;
        activeStaggerDurationMultiplier = variant.staggerDurationMultiplier;
        activePostAttackVulnerabilityWindow = variant.postAttackVulnerabilityWindow;
        activePostAttackVulnerabilityMultiplier = variant.postAttackVulnerabilityMultiplier;

        RefreshCombatStoppingDistance();
        ApplyVariantVisuals(variant);

        DevLog.Log($"[{gameObject.name}] Variant applied: {variant.variantLabel} (region state {lastAppliedRegionState}).");
    }

    /// <summary>Swaps in the variant's material (a fresh instance, never the shared asset - the
    /// same MaterialPropertyBlock-adjacent safety pattern RegionVisualAdapter uses, needed here
    /// because SetTelegraphVisual/SetStaggerVisual/StartHitFlash all mutate .material.color
    /// directly). No-ops entirely for archetypes/states that don't assign a materialOverride.</summary>
    private void ApplyVariantVisuals(EnemyVariantData variant)
    {
        if (enemyRenderer == null || variant == appliedVisualVariant)
            return;

        appliedVisualVariant = variant;

        if (variantMaterialInstance != null)
        {
            Destroy(variantMaterialInstance);
            variantMaterialInstance = null;
        }

        Material sourceMaterial =
            (variant != null && variant.materialOverride != null)
                ? variant.materialOverride
                : baseMaterialAsset;

        if (sourceMaterial == null)
            return;

        variantMaterialInstance = new Material(sourceMaterial);
        enemyRenderer.material = variantMaterialInstance;
        originalColor = variantMaterialInstance.color;
    }

    private float GetDistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        return Vector3.Distance(
            transform.position,
            player.position
        );
    }

    private Vector3 GetDirectionToPlayer()
    {
        if (player == null)
            return Vector3.zero;

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        return direction.normalized;
    }
    /// <summary>
    /// Called when the enemy takes a hit. If it was minding its own business it
    /// spins to face the attacker and aggros instead of shrugging it off.
    /// </summary>
    public void NotifyDamaged(Vector3 fromDirection)
    {
        if (currentState != EnemyState.Idle)
            return;

        fromDirection.y = 0f;
        if (fromDirection.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(fromDirection.normalized);

        EnterAlerted();
    }

    public void HitReact(float duration)
    {
        /*
         * A committed attack (windup/recovery) must not flinch: HitReact would
         * knock it back to Chase, which - since the player is already in melee
         * range - immediately calls BeginAttackWindup() again. That let every
         * player hit restart the enemy's swing from scratch, so attacking more
         * made the enemy visibly attack more often instead of just taking
         * damage. A real parry still interrupts via Stagger().
         */
        if (currentState == EnemyState.Staggered ||
            currentState == EnemyState.AttackWindup ||
            currentState == EnemyState.AttackRecovery)
            return;

        SetTelegraphVisual(false);

        currentState = EnemyState.HitReact;
        stateEndTime = Time.time + duration;

        StopMoving();

        DevLog.Log(
            $"[{gameObject.name}] Hit reaction " +
            $"for {duration:0.00}s."
        );
    }
    /// <summary>Fired when the enemy is staggered, with the stagger duration (drives the recoil).</summary>
    public event System.Action<float> Staggered;

    public void Stagger(float duration)
    {
        SetTelegraphVisual(false);

        currentState = EnemyState.Staggered;

        float adaptedDuration =
            duration * activeStaggerDurationMultiplier;

        bool caughtInOpening = vulnerableUntil > 0f && Time.time <= vulnerableUntil;
        if (caughtInOpening)
            adaptedDuration *= activePostAttackVulnerabilityMultiplier;

        /*
        * Refresh the stagger duration even if the enemy
        * was already staggered.
        */
        staggerEndTime = Time.time + adaptedDuration;

        StopMoving();
        SetStaggerVisual(true);
        Staggered?.Invoke(adaptedDuration);

        DevLog.Log(
            $"[{gameObject.name}] STAGGERED " +
            $"for {adaptedDuration:0.00}s (base {duration:0.00}s)" +
            $"{(caughtInOpening ? " - caught in the post-attack opening!" : "")}."
        );
    }
    public void SetWorldAdaptationManager(
        WorldAdaptationManager manager)
    {
        worldAdaptationManager = manager;
    }

    // Adaptation no longer scales enemy combat stats - the world's response to
    // the player is atmospheric / narrative (see WorldVisualAdapter), not a
    // hidden difficulty dial. These are kept at 1 so the call sites are unchanged.
    public float GetMoveSpeedModifier() => 1f;
    public float GetDetectionModifier() => 1f;
    public float GetAttackCooldownModifier() => 1f;

    private float GetAdaptedMoveSpeed()
    {
        return moveSpeed * GetMoveSpeedModifier();
    }

    private float GetAdaptedDetectionRange()
    {
        return detectionRange * GetDetectionModifier();
    }

    private float GetAdaptedLosePlayerRange()
    {
        return losePlayerRange * GetDetectionModifier();
    }
}