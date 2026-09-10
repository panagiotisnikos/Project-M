using UnityEngine;

/// <summary>
/// The boss encounter. A telegraphed 3-attack fight the player answers with
/// block / dodge / parry. Modeled on EnemyAI's state-timer + rb.MovePosition style.
///
/// The fight is dormant until BossArenaTrigger calls BeginFight().
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BossCombat : MonoBehaviour, IStaggerable
{
    private enum BossState
    {
        Dormant,
        Idle,
        Reposition,
        Windup,
        Lunging,
        Recovery,
        PhaseTransition,
        Staggered,
        Dead
    }

    private enum BossAttack
    {
        Slam,
        Sweep,
        Lunge
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private BossController bossController;
    [SerializeField] private BossDecayAura decayAura;
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float preferredRange = 2.6f;
    [SerializeField] private float repositionRange = 3.4f;

    [Tooltip("Player beyond this range from the boss: the boss keeps closing but will not attack.")]
    [SerializeField] private float maxAttackRange = 12f;

    [Tooltip("If the boss strays this far from where the fight began, it walks back to the arena instead of chasing.")]
    [SerializeField] private float leashRange = 16f;

    [Header("Pacing")]
    [SerializeField] private float firstAttackDelay = 1.2f;
    [SerializeField] private float idleBetweenAttacks = 1.2f;

    [Header("Telegraph Colors")]
    [SerializeField] private Color slamColor = new Color(1f, 0.55f, 0.1f);
    [SerializeField] private Color sweepColor = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color lungeColor = new Color(1f, 0.25f, 0.9f);
    [SerializeField] private Color staggerColor = Color.cyan;
    [SerializeField] private Color phaseColor = new Color(0.6f, 0.15f, 0.7f);

    [Header("Slam (heavy overhead, big punish window)")]
    [SerializeField] private float slamWindup = 1.1f;
    [SerializeField] private float slamRecovery = 1.3f;
    [SerializeField] private float slamReach = 2.6f;
    [SerializeField] private float slamRadius = 2.2f;
    [SerializeField] private int slamDamage = 24;

    [Header("Sweep (wide frontal arc, dodge it)")]
    [SerializeField] private float sweepWindup = 0.7f;
    [SerializeField] private float sweepRecovery = 0.85f;
    [SerializeField] private float sweepRadius = 3.6f;
    [SerializeField] private float sweepArcDegrees = 200f;
    [SerializeField] private int sweepDamage = 14;

    [Header("Lunge (gap closer)")]
    [SerializeField] private float lungeWindup = 0.8f;
    [SerializeField] private float lungeRecovery = 1.1f;
    [SerializeField] private float lungeDuration = 0.35f;
    [SerializeField] private float lungeSpeed = 14f;
    [SerializeField] private float lungeContactRadius = 1.7f;
    [SerializeField] private float lungeMinRange = 5f;
    [SerializeField] private int lungeDamage = 18;

    [Header("Stagger")]
    [SerializeField] private float staggerDuration = 1.5f;

    [Header("Phase Transition (at 50% HP)")]
    [SerializeField] private float phaseTransitionDuration = 1.6f;
    [SerializeField] private float healFraction = 0.3f;
    [SerializeField] private GameObject stalkerPrefab;
    [SerializeField] private Transform[] minionSpawnPoints;

    [Header("Death")]
    [SerializeField] private float deathOutDuration = 1f;

    [Header("Feel / VFX")]
    [SerializeField] private BossTelegraph telegraph;
    [SerializeField] private ParticleSystem slamImpactVfx;
    [SerializeField] private ParticleSystem lungeImpactVfx;
    [SerializeField] private float slamTrauma = 0.5f;
    [SerializeField] private float sweepTrauma = 0.18f;
    [SerializeField] private float lungeTrauma = 0.45f;
    [SerializeField] private float phaseTrauma = 0.3f;

    private Rigidbody rb;
    private Renderer bossRenderer;
    private Color originalColor;

    private BossState currentState = BossState.Dormant;
    private float stateEndTime;
    private float staggerEndTime;
    private float nextAttackTime;

    private BossAttack currentAttack;
    private BossAttack lastAttack;
    private int sameAttackCount;

    private Vector3 lungeDirection;
    private bool lungeHitDone;
    private bool chargeImpactFired;

    /*
     * The strike fires when the attack animation reaches its impact frame
     * (AnimationBossHit event -> NotifyAnimationHit). stateEndTime is only a
     * fallback in case that event is missing, so damage always lines up with
     * the visible swing.
     */
    private bool animHitPending;

    private Vector3 deathStartScale;
    private float deathElapsed;

    private Vector3 arenaAnchor;

    public bool FightActive { get; private set; }

    /// <summary>True while the boss is closing distance (drives the walk animation).</summary>
    public bool IsRepositioning =>
        currentState == BossState.Reposition ||
        currentState == BossState.Lunging;

    /// <summary>Fired when a wind-up starts. 0 = Lunge (charge), 1 = Slam (overhead), 2 = Sweep (stomp).</summary>
    public event System.Action<int> AttackAnimTriggered;

    /// <summary>Fired when a lunge finishes closing - the charge animation resolves into its hit.</summary>
    public event System.Action ChargeImpact;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bossRenderer = GetComponentInChildren<Renderer>();

        if (bossRenderer != null)
        {
            originalColor = bossRenderer.material.color;
        }

        if (bossHealth == null)
        {
            bossHealth = GetComponent<BossHealth>();
        }

        if (bossController == null)
        {
            bossController = GetComponent<BossController>();
        }

        if (decayAura == null)
        {
            decayAura = GetComponent<BossDecayAura>();
        }

        if (telegraph == null)
        {
            telegraph = GetComponentInChildren<BossTelegraph>();
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

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHalfHealthReached += HandleHalfHealthReached;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHalfHealthReached -= HandleHalfHealthReached;
        }
    }

    public void BeginFight()
    {
        if (currentState != BossState.Dormant)
            return;

        FightActive = true;
        arenaAnchor = transform.position;
        nextAttackTime = Time.time + firstAttackDelay;
        ChangeState(BossState.Idle);

        Debug.Log("[Boss] Fight started.");
    }

    /// <summary>Called by BossHealth when the boss dies.</summary>
    public void OnDefeated()
    {
        if (currentState == BossState.Dead)
            return;

        ChangeState(BossState.Dead);
        FightActive = false;

        StopMoving();
        SetColor(originalColor);

        if (telegraph != null)
        {
            telegraph.Clear();
        }

        if (decayAura != null)
        {
            decayAura.enabled = false;
        }

        deathStartScale = transform.localScale;
        deathElapsed = 0f;

        Debug.Log("[Boss] Death sequence started.");
    }

    private void Update()
    {
        if (GameUIController.IsPaused)
            return;

        switch (currentState)
        {
            case BossState.Dormant:
                break;

            case BossState.Idle:
                TickIdle();
                break;

            case BossState.Reposition:
                TickReposition();
                break;

            case BossState.Windup:
                ReassertTelegraph();
                if (animHitPending || Time.time >= stateEndTime)
                {
                    BeginStrike();
                }
                break;

            case BossState.Lunging:
                ReassertTelegraph();
                if (Time.time >= stateEndTime &&
                    currentState == BossState.Lunging)
                {
                    EnterRecovery(lungeRecovery);
                }
                break;

            case BossState.Recovery:
                if (Time.time >= stateEndTime)
                {
                    EnterIdle(idleBetweenAttacks * GetPaceMultiplier());
                }
                break;

            case BossState.PhaseTransition:
                ReassertTelegraph();
                if (Time.time >= stateEndTime)
                {
                    FinishPhaseTransition();
                }
                break;

            case BossState.Staggered:
                ReassertTelegraph();
                if (Time.time >= staggerEndTime)
                {
                    SetColor(originalColor);
                    EnterIdle(idleBetweenAttacks * 0.6f);
                }
                break;

            case BossState.Dead:
                TickDeathOut();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (GameUIController.IsPaused)
            return;

        switch (currentState)
        {
            case BossState.Reposition:
                if (IsLeashed())
                {
                    Vector3 toAnchor = arenaAnchor - transform.position;
                    toAnchor.y = 0f;
                    MoveToward(toAnchor.normalized, moveSpeed);
                }
                else
                {
                    MoveToward(DirectionToPlayer(), moveSpeed);
                    FacePlayer();
                }
                break;

            case BossState.Windup:
                StopMoving();
                FacePlayer();
                break;

            case BossState.Lunging:
                TickLungeMovement();
                break;

            case BossState.Idle:
            case BossState.Recovery:
            case BossState.PhaseTransition:
            case BossState.Staggered:
            case BossState.Dead:
            case BossState.Dormant:
                StopMoving();
                break;
        }
    }

    // --- Idle / reposition -------------------------------------------------

    private void TickIdle()
    {
        FacePlayerSmooth();

        if (TryStartAttack())
            return;

        if (DistanceToPlayer() > repositionRange)
        {
            ChangeState(BossState.Reposition);
        }
    }

    private void TickReposition()
    {
        // Keep pressing even while closing - the lunge is the anti-kite answer.
        if (TryStartAttack())
            return;

        if (DistanceToPlayer() <= preferredRange)
        {
            ChangeState(BossState.Idle);
        }
    }

    /// <summary>Begins an attack if the cooldown is up and the player is in range. Returns true if an attack started.</summary>
    private bool TryStartAttack()
    {
        if (Time.time < nextAttackTime)
            return false;

        if (IsLeashed())
            return false;

        if (DistanceToPlayer() > maxAttackRange)
            return false;

        BeginAttack(ChooseAttack());
        return true;
    }

    private bool IsLeashed()
    {
        return Vector3.Distance(transform.position, arenaAnchor) > leashRange;
    }

    private void EnterIdle(float delayUntilNextAttack)
    {
        nextAttackTime = Time.time + Mathf.Max(0.1f, delayUntilNextAttack);
        ChangeState(BossState.Idle);
    }

    // --- Attack selection ------------------------------------------------

    private BossAttack ChooseAttack()
    {
        float d = DistanceToPlayer();

        BossAttack pick =
            d >= lungeMinRange
                ? BossAttack.Lunge
                : (Random.value < 0.5f ? BossAttack.Slam : BossAttack.Sweep);

        if (pick == lastAttack && sameAttackCount >= 2)
        {
            pick = pick switch
            {
                BossAttack.Lunge => BossAttack.Slam,
                BossAttack.Slam => BossAttack.Sweep,
                _ => BossAttack.Slam
            };
        }

        if (pick == lastAttack)
        {
            sameAttackCount++;
        }
        else
        {
            sameAttackCount = 1;
            lastAttack = pick;
        }

        return pick;
    }

    private void BeginAttack(BossAttack attack)
    {
        currentAttack = attack;

        float windup = attack switch
        {
            BossAttack.Slam => slamWindup,
            BossAttack.Sweep => sweepWindup,
            _ => lungeWindup
        };

        SetColor(TelegraphColor());
        ShowTelegraph(attack, windup);

        int animId = attack switch
        {
            BossAttack.Lunge => 0,
            BossAttack.Slam => 1,
            _ => 2
        };
        AttackAnimTriggered?.Invoke(animId);

        animHitPending = false;

        // Lunge is timer-driven (the run anim has no impact frame). Slam / Sweep
        // strike on their AnimationBossHit event; stateEndTime is only a fallback.
        stateEndTime = attack == BossAttack.Lunge
            ? Time.time + windup
            : Time.time + windup * 2f + 0.5f;

        ChangeState(BossState.Windup);

        Debug.Log($"[Boss] Wind-up: {attack}.");
    }

    /// <summary>Called from the attack animation's impact frame (via BossAnimationBridge).</summary>
    public void NotifyAnimationHit()
    {
        if (currentState == BossState.Windup)
        {
            animHitPending = true;
        }
    }

    private void BeginStrike()
    {
        animHitPending = false;

        if (telegraph != null)
        {
            telegraph.Strike();
        }

        if (currentAttack == BossAttack.Lunge)
        {
            lungeDirection = DirectionToPlayer();
            lungeHitDone = false;
            chargeImpactFired = false;
            stateEndTime = Time.time + lungeDuration;
            ChangeState(BossState.Lunging);
            return;
        }

        AddTrauma(
            currentAttack == BossAttack.Slam ? slamTrauma : sweepTrauma
        );

        if (currentAttack == BossAttack.Slam)
        {
            CombatVfx.Play(
                slamImpactVfx,
                transform.position + transform.forward * slamReach
            );
        }

        ResolveMeleeStrike();

        // A parry during ResolveMeleeStrike puts the boss in Staggered - do
        // not stomp that with recovery.
        if (currentState == BossState.Windup)
        {
            SetColor(originalColor);
            EnterRecovery(
                currentAttack == BossAttack.Slam ? slamRecovery : sweepRecovery
            );
        }
    }

    private void ShowTelegraph(BossAttack attack, float windup)
    {
        if (telegraph == null)
            return;

        switch (attack)
        {
            case BossAttack.Slam:
                telegraph.Begin(
                    BossTelegraph.Shape.Ring,
                    transform.position + transform.forward * slamReach,
                    transform.forward, slamRadius, 0f, windup);
                break;

            case BossAttack.Sweep:
                telegraph.Begin(
                    BossTelegraph.Shape.Arc,
                    transform.position,
                    transform.forward, sweepRadius, sweepArcDegrees, windup);
                break;

            case BossAttack.Lunge:
                telegraph.Begin(
                    BossTelegraph.Shape.Line,
                    transform.position,
                    DirectionToPlayer(),
                    Mathf.Clamp(DistanceToPlayer() + 1.5f, 3f, lungeSpeed * lungeDuration + 3f),
                    0f, windup);
                break;
        }
    }

    private void AddTrauma(float amount)
    {
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.AddTrauma(amount);
        }
    }

    private void ResolveMeleeStrike()
    {
        if (player == null)
            return;

        bool isSlam = currentAttack == BossAttack.Slam;

        float radius = isSlam ? slamRadius : sweepRadius;
        int damage = isSlam ? slamDamage : sweepDamage;

        Vector3 center =
            isSlam
                ? transform.position + transform.forward * slamReach
                : transform.position;

        float distance = DistanceToPlayer();

        if (distance > radius + 1f)
        {
            Debug.Log($"[Boss] {currentAttack} whiffed.");
            return;
        }

        Vector3 toPlayer = DirectionToPlayer();

        if (!isSlam)
        {
            float angle = Vector3.Angle(transform.forward, toPlayer);
            if (angle > sweepArcDegrees * 0.5f)
            {
                Debug.Log("[Boss] Sweep missed (player behind).");
                return;
            }
        }
        else if (Vector3.Distance(player.position, center) > slamRadius)
        {
            Debug.Log("[Boss] Slam missed (player out of the impact).");
            return;
        }

        DealDamageToPlayer(damage, toPlayer);
    }

    private void EnterRecovery(float duration)
    {
        SetColor(originalColor);

        // A whiffed charge still needs its animation to resolve out of the run.
        if (currentAttack == BossAttack.Lunge)
        {
            FireChargeImpact();
        }

        stateEndTime = Time.time + duration;
        ChangeState(BossState.Recovery);
    }

    private void FireChargeImpact()
    {
        if (chargeImpactFired)
            return;

        chargeImpactFired = true;
        ChargeImpact?.Invoke();
    }

    // --- Lunge ----------------------------------------------------------

    private void TickLungeMovement()
    {
        Vector3 next = rb.position + lungeDirection * lungeSpeed * Time.fixedDeltaTime;
        rb.MovePosition(next);

        if (!lungeHitDone &&
            DistanceToPlayer() <= lungeContactRadius)
        {
            lungeHitDone = true;
            FireChargeImpact();
            AddTrauma(lungeTrauma);
            CombatVfx.Play(lungeImpactVfx, transform.position + Vector3.up * 0.5f);
            DealDamageToPlayer(lungeDamage, DirectionToPlayer());
        }
    }

    // --- Damage / parry ------------------------------------------------

    private void DealDamageToPlayer(int damage, Vector3 direction)
    {
        if (player == null)
            return;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsDead)
            return;

        playerHealth.TakeDamage(damage, direction, this);

        Debug.Log($"[Boss] {currentAttack} connected for {damage}.");
    }

    public void Stagger(float duration)
    {
        if (currentState == BossState.Dead)
            return;

        staggerEndTime = Time.time + Mathf.Max(duration, staggerDuration);
        StopMoving();
        SetColor(staggerColor);

        if (telegraph != null)
        {
            telegraph.Clear();
        }

        ChangeState(BossState.Staggered);

        Debug.Log($"[Boss] STAGGERED for {duration:0.00}s.");
    }

    // --- Phase transition --------------------------------------------

    private void HandleHalfHealthReached()
    {
        if (currentState == BossState.Dead ||
            currentState == BossState.PhaseTransition)
        {
            return;
        }

        StopMoving();
        SetColor(phaseColor);

        if (telegraph != null)
        {
            telegraph.Clear();
        }

        AddTrauma(phaseTrauma);

        if (bossHealth != null)
        {
            bossHealth.SetInvulnerable(true);
        }

        stateEndTime = Time.time + phaseTransitionDuration;
        ChangeState(BossState.PhaseTransition);

        Debug.Log("[Boss] Phase transition.");
    }

    private void FinishPhaseTransition()
    {
        if (bossHealth != null)
        {
            bossHealth.SetInvulnerable(false);
        }

        SetColor(originalColor);

        bool healed = false;
        bool summoned = false;

        if (bossController != null && bossController.CanHeal && bossHealth != null)
        {
            bossHealth.Heal(healFraction);
            healed = true;
        }

        if (bossController != null && bossController.CanSummonMinions)
        {
            SummonMinions();
            summoned = true;
        }

        Debug.Log(
            $"[Boss] Phase 2. Healed: {healed}, Summoned: {summoned}, " +
            $"Aura: {(bossController != null && bossController.HasDecayAura)}."
        );

        EnterIdle(idleBetweenAttacks * 0.5f);
    }

    private void SummonMinions()
    {
        if (stalkerPrefab == null)
            return;

        int count =
            minionSpawnPoints != null && minionSpawnPoints.Length > 0
                ? minionSpawnPoints.Length
                : 2;

        for (int i = 0; i < count; i++)
        {
            Vector3 position;
            Quaternion rotation;

            if (minionSpawnPoints != null &&
                i < minionSpawnPoints.Length &&
                minionSpawnPoints[i] != null)
            {
                position = minionSpawnPoints[i].position;
                rotation = minionSpawnPoints[i].rotation;
            }
            else
            {
                float side = i % 2 == 0 ? -1f : 1f;
                position = transform.position + transform.right * (3f * side) + transform.forward * 2f;
                rotation = Quaternion.identity;
            }

            GameObject minion = Instantiate(stalkerPrefab, position, rotation);
            minion.name = stalkerPrefab.name;
            minion.SetActive(true);
        }

        Debug.Log($"[Boss] Summoned {count} minions.");
    }

    // --- Death out ----------------------------------------------------

    private void TickDeathOut()
    {
        deathElapsed += Time.deltaTime;

        float t = deathOutDuration > 0f
            ? Mathf.Clamp01(deathElapsed / deathOutDuration)
            : 1f;

        transform.localScale = Vector3.Lerp(deathStartScale, Vector3.zero, t);

        if (t >= 1f)
        {
            gameObject.SetActive(false);
        }
    }

    // --- Movement helpers -------------------------------------------

    private void MoveToward(Vector3 direction, float speed)
    {
        if (direction == Vector3.zero)
            return;

        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }

    private void FacePlayer()
    {
        RotateTowards(DirectionToPlayer(), rotationSpeed);
    }

    private void FacePlayerSmooth()
    {
        RotateTowards(DirectionToPlayer(), rotationSpeed * 0.5f);
    }

    private void RotateTowards(Vector3 direction, float speed)
    {
        if (direction == Vector3.zero)
            return;

        Quaternion target = Quaternion.LookRotation(direction);
        rb.MoveRotation(
            Quaternion.Slerp(rb.rotation, target, speed * Time.fixedDeltaTime)
        );
    }

    private void StopMoving()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    // --- Small helpers ---------------------------------------------

    private float GetPaceMultiplier()
    {
        if (worldAdaptationManager == null)
            return 1f;

        return worldAdaptationManager.CurrentState switch
        {
            WorldAdaptationManager.WorldState.Stable => 1.25f,
            WorldAdaptationManager.WorldState.Decaying => 0.8f,
            _ => 1f
        };
    }

    private Color TelegraphColor()
    {
        return currentAttack switch
        {
            BossAttack.Slam => slamColor,
            BossAttack.Sweep => sweepColor,
            _ => lungeColor
        };
    }

    private void ReassertTelegraph()
    {
        switch (currentState)
        {
            case BossState.Windup:
            case BossState.Lunging:
                SetColor(TelegraphColor());
                break;

            case BossState.Staggered:
                SetColor(staggerColor);
                break;

            case BossState.PhaseTransition:
                SetColor(phaseColor);
                break;
        }
    }

    private void SetColor(Color color)
    {
        if (bossRenderer != null)
        {
            bossRenderer.material.color = color;
        }
    }

    private void ChangeState(BossState newState)
    {
        currentState = newState;
    }

    private float DistanceToPlayer()
    {
        return player == null
            ? Mathf.Infinity
            : Vector3.Distance(transform.position, player.position);
    }

    private Vector3 DirectionToPlayer()
    {
        if (player == null)
            return Vector3.zero;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        return dir.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = slamColor;
        Gizmos.DrawWireSphere(
            transform.position + transform.forward * slamReach, slamRadius
        );

        Gizmos.color = sweepColor;
        Gizmos.DrawWireSphere(transform.position, sweepRadius);

        Gizmos.color = lungeColor;
        Gizmos.DrawWireSphere(transform.position, lungeMinRange);
    }
}
