using System.Collections;
using UnityEngine;

/// <summary>
/// Translates EnemyAI / EnemyHealth state into the creature Animator. Same idea
/// as PlayerAnimationController and BossAnimationBridge.
///
/// Animator contract (EnemyAnimator_*.controller):
///   float Speed   - 0 idle, ~1 chasing
///   trigger Attack
///   bool  Dead
///
/// The Barry rigs have no stagger clip, so a stagger is done procedurally here:
/// the animator freezes and the model recoils backward for the stagger duration.
/// </summary>
public class EnemyAnimationBridge : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private EnemyHealth enemyHealth;

    [SerializeField] private float speedDamp = 10f;

    [Header("Idle Breathing")]
    [Tooltip("Vertical bob applied to the model while standing still, so idle enemies read as alive.")]
    [SerializeField] private float idleBobAmount = 0.03f;
    [SerializeField] private float idleBobSpeed = 1.6f;

    [Header("Procedural Stagger")]
    [SerializeField] private float staggerTiltDegrees = 22f;
    [SerializeField] private float staggerSink = 0.08f;
    [SerializeField] private float staggerFrozenAnimSpeed = 0.05f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private Transform model;
    private Quaternion modelBaseRot;
    private Vector3 modelBasePos;

    private float speed;
    private bool dead;
    private Coroutine staggerRoutine;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (enemyAI == null) enemyAI = GetComponent<EnemyAI>();
        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();

        if (animator != null)
        {
            model = animator.transform;
            modelBaseRot = model.localRotation;
            modelBasePos = model.localPosition;
        }
    }

    private void OnEnable()
    {
        if (enemyAI != null)
        {
            enemyAI.AttackStarted += HandleAttack;
            enemyAI.Staggered += HandleStagger;
        }
        if (enemyHealth != null) enemyHealth.Died += HandleDied;
    }

    private void OnDisable()
    {
        if (enemyAI != null)
        {
            enemyAI.AttackStarted -= HandleAttack;
            enemyAI.Staggered -= HandleStagger;
        }
        if (enemyHealth != null) enemyHealth.Died -= HandleDied;
    }

    private void Update()
    {
        if (animator == null || dead || staggerRoutine != null)
            return;

        float target = enemyAI != null ? enemyAI.LocomotionSpeed01 : 0f;
        speed = Mathf.MoveTowards(speed, target, speedDamp * Time.deltaTime);
        animator.SetFloat(SpeedHash, speed);

        // Subtle breathing bob while idle - the freeze-pose Idle clip is otherwise dead still.
        if (model != null)
        {
            if (speed < 0.05f && idleBobAmount > 0f)
            {
                float bob = Mathf.Sin(Time.time * idleBobSpeed * Mathf.PI) * idleBobAmount;
                model.localPosition = modelBasePos + Vector3.up * bob;
            }
            else if (model.localPosition != modelBasePos)
            {
                model.localPosition = modelBasePos;
            }
        }
    }

    private void HandleAttack()
    {
        if (animator != null && !dead && staggerRoutine == null)
            animator.SetTrigger(AttackHash);
    }

    private void HandleDied()
    {
        dead = true;
        if (staggerRoutine != null) { StopCoroutine(staggerRoutine); staggerRoutine = null; }
        RestoreModel();
        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetBool(DeadHash, true);
        }
    }

    private void HandleStagger(float duration)
    {
        if (dead || model == null)
            return;

        if (staggerRoutine != null)
            StopCoroutine(staggerRoutine);

        staggerRoutine = StartCoroutine(StaggerRoutine(duration));
    }

    private IEnumerator StaggerRoutine(float duration)
    {
        if (animator != null)
            animator.speed = staggerFrozenAnimSpeed;

        // the enemy faces the player, so a backward lean is a negative pitch
        Quaternion recoiled =
            modelBaseRot * Quaternion.Euler(-staggerTiltDegrees, 0f, 0f);
        Vector3 sunk = modelBasePos + Vector3.down * staggerSink;

        float inTime = Mathf.Min(0.09f, duration * 0.3f);
        yield return LerpModel(recoiled, sunk, inTime);

        float hold = Mathf.Max(0f, duration - inTime - 0.15f);
        yield return new WaitForSeconds(hold);

        yield return LerpModel(modelBaseRot, modelBasePos, 0.15f);

        RestoreModel();
        if (animator != null) animator.speed = 1f;
        staggerRoutine = null;
    }

    private IEnumerator LerpModel(Quaternion rot, Vector3 pos, float time)
    {
        Quaternion r0 = model.localRotation;
        Vector3 p0 = model.localPosition;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = time > 0f ? t / time : 1f;
            model.localRotation = Quaternion.Slerp(r0, rot, k);
            model.localPosition = Vector3.Lerp(p0, pos, k);
            yield return null;
        }
        model.localRotation = rot;
        model.localPosition = pos;
    }

    private void RestoreModel()
    {
        if (model == null)
            return;

        model.localRotation = modelBaseRot;
        model.localPosition = modelBasePos;
    }
}
