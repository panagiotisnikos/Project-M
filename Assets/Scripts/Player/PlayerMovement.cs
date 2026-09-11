using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 12f;
    [Tooltip("How quickly the player ramps up to full speed. Higher = snappier.")]
    [SerializeField] private float moveAcceleration = 45f;
    [Tooltip("How quickly the player slows to a stop.")]
    [SerializeField] private float moveDeceleration = 40f;

    private Vector3 planarVelocity;

    [Header("Dodge Roll")]
    [SerializeField] private KeyCode dodgeKey = KeyCode.Space;
    [Tooltip("Minimum roll speed. The dodge animation's root motion adds to this.")]
    [SerializeField] private float dodgeSpeed = 3f;
    [Tooltip("How long the roll state lasts (input lock). Match the dodge clip length.")]
    [SerializeField] private float dodgeDuration = 0.95f;
    [SerializeField] private float dodgeCooldown = 0.35f;
    [Min(0f)]
    [SerializeField] private float dodgeStaminaCost = 18f;

    [Tooltip("Delay after the roll starts before invulnerability begins.")]
    [SerializeField] private float invulnerabilityStartDelay = 0.08f;

    [Tooltip("How long the player remains invulnerable during the roll.")]
    [SerializeField] private float invulnerabilityDuration = 0.55f;

    [SerializeField] private ParticleSystem dodgeVfx;
    [SerializeField] private PlayerRootMotion rootMotion;

    [Header("References")]
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerInventory playerInventory;

    [Tooltip("Move speed multiplier while over the carry limit.")]
    [SerializeField] private float encumberedSpeedMultiplier = 0.5f;

    private Rigidbody rb;

    private Vector3 movementDirection;
    private Vector2 movementInput;

    public float MoveX => movementInput.x;
    public float MoveZ => movementInput.y;
    private Vector3 dodgeDirection;

    private float parryWindowEndTime;
    private bool parryConsumed;

    private float dodgeStartTime;
    private float dodgeEndTime;
    private float nextDodgeAllowedTime;

    [Header("Attack Movement")]
    [SerializeField] private float attackStepDuration = 0.22f;

    private float remainingAttackStepDistance;
    private float attackStepSpeed;

    public bool IsBlocking { get; private set; }
    public bool IsDodging { get; private set; }
    public bool IsMoving =>
    movementDirection.sqrMagnitude > 0.01f;

    public bool IsParryWindowOpen =>
        IsBlocking &&
        !parryConsumed &&
        Time.time <= parryWindowEndTime;

    public bool IsInvulnerable =>
        IsDodging &&
        Time.time >= dodgeStartTime +
                     invulnerabilityStartDelay &&
        Time.time <= dodgeStartTime +
                     invulnerabilityStartDelay +
                     invulnerabilityDuration;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rootMotion == null)
        {
            rootMotion = GetComponentInChildren<PlayerRootMotion>();
        }

        if (playerAttack == null)
        {
            playerAttack =
                GetComponent<PlayerAttack>();
        }

        if (playerEquipment == null)
        {
            playerEquipment =
                GetComponent<PlayerEquipment>();
        }

        if (playerStamina == null)
        {
            playerStamina =
                GetComponent<PlayerStamina>();
        }

        if (playerInventory == null)
        {
            playerInventory =
                GetComponent<PlayerInventory>();
        }

        if (playerStamina == null)
        {
            Debug.LogWarning(
                "[PlayerMovement] PlayerStamina component is missing."
            );
        }
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        if (GameUIController.IsPaused)
        {
            movementDirection =
                Vector3.zero;

            return;
        }

        ReadMovementInput();


        if (IsDodging)
        {
            UpdateDodge();
            return;
        }

        // Pack open: WASD still moves, but no blocking / dodging (mouse is on the UI).
        if (InventoryUI.IsOpen)
        {
            IsBlocking = false;
            return;
        }

        UpdateBlocking();
        TryStartDodge();
    }

    private void FixedUpdate()
    {
        rb.angularVelocity = Vector3.zero;

        if (IsDodging)
        {
            MoveDuringDodge();
            RotateTowardsDirection(
                dodgeDirection
            );

            return;
        }

        Move();

        if (playerAttack != null &&
            playerAttack.IsAttacking)
        {
            RotateTowardsCamera();
        }
        else if (IsBlocking)
        {
            RotateTowardsCamera();
        }
        else if (movementDirection.sqrMagnitude >
                 0.01f)
        {
            RotateTowardsMovement();
        }
    }

    private void ReadMovementInput()
    {
        float horizontal =
            Input.GetAxisRaw("Horizontal");

        float vertical =
            Input.GetAxisRaw("Vertical");

        movementInput =
            new Vector2(
                horizontal,
                vertical
            );

        Vector3 input =
            new Vector3(
                horizontal,
                0f,
                vertical
            ).normalized;

        if (cameraFollow != null)
        {
            movementDirection =
                cameraFollow.GetCameraForward() *
                input.z +
                cameraFollow.GetCameraRight() *
                input.x;
        }
        else
        {
            movementDirection = input;
        }

        movementDirection.y = 0f;

        if (movementDirection.sqrMagnitude >
            0.01f)
        {
            movementDirection.Normalize();
        }
    }

    private void UpdateBlocking()
    {
        if (playerAttack != null &&
            playerAttack.IsAttacking)
        {
            IsBlocking = false;
            ResetParryWindow();
            return;
        }

        ShieldData shield =
            GetEquippedShield();

        /*
         * Without an equipped shield, RMB does not
         * activate block or parry.
         */
        if (shield == null)
        {
            IsBlocking = false;
            ResetParryWindow();
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            parryWindowEndTime =
                Time.time +
                shield.ParryWindowDuration;

            parryConsumed = false;
        }

        IsBlocking =
            Input.GetMouseButton(1);

        if (!IsBlocking)
        {
            ResetParryWindow();
        }
    }

    private void TryStartDodge()
    {
        if (!Input.GetKeyDown(dodgeKey))
            return;

        if (Time.time < nextDodgeAllowedTime)
            return;

        // Over-encumbered: too heavy to roll.
        if (playerInventory != null &&
            playerInventory.IsOverEncumbered)
        {
            return;
        }

        // Dodging out of a block is allowed - BeginDodge drops the guard.
        // Dodging out of an attack is only allowed once the swing has resolved
        // and we're in Recovery endlag - the Windup/telegraph itself still can't
        // be skipped.
        if (playerAttack != null &&
            playerAttack.IsAttacking &&
            !playerAttack.CanCancelIntoDodge)
        {
            return;
        }

        if (playerStamina != null &&
            !playerStamina.TrySpend(
                dodgeStaminaCost
            ))
        {
            Debug.Log(
                $"[PlayerMovement] Not enough stamina " +
                $"to dodge. Required: " +
                $"{dodgeStaminaCost:0.0}, " +
                $"Available: " +
                $"{playerStamina.CurrentStamina:0.0}."
            );

            return;
        }

        BeginDodge();
    }

    private void BeginDodge()
    {
        if (playerAttack != null && playerAttack.IsAttacking)
        {
            playerAttack.CancelForDodge();
        }

        IsDodging = true;
        IsBlocking = false;

        remainingAttackStepDistance = 0f;
        attackStepSpeed = 0f;
        planarVelocity = Vector3.zero;

        ResetParryWindow();

        if (movementDirection.sqrMagnitude >
            0.01f)
        {
            dodgeDirection =
                movementDirection;
        }
        else
        {
            dodgeDirection =
                transform.forward;
        }

        dodgeDirection.y = 0f;
        dodgeDirection.Normalize();

        dodgeStartTime = Time.time;

        dodgeEndTime =
            Time.time + dodgeDuration;

        CombatVfx.Play(
            dodgeVfx,
            transform.position,
            -dodgeDirection
        );

        Debug.Log(
            $"[PlayerMovement] Dodge started. " +
            $"Duration: {dodgeDuration:0.00}s."
        );
    }

    private void UpdateDodge()
    {
        if (Time.time < dodgeEndTime)
            return;

        FinishDodge();
    }

    private void FinishDodge()
    {
        IsDodging = false;

        nextDodgeAllowedTime =
            Time.time + dodgeCooldown;

        rb.linearVelocity =
            Vector3.zero;

        Debug.Log(
            "[PlayerMovement] Dodge finished."
        );
    }

    private void MoveDuringDodge()
    {
        // The dodge animation's root motion (PlayerRootMotion) does most of the
        // travel; this is just a floor so the roll always covers some ground.
        float speed = rootMotion != null && rootMotion.RootMotionActive
            ? dodgeSpeed
            : 11f;

        Vector3 newPosition =
            rb.position +
            dodgeDirection *
            speed *
            Time.fixedDeltaTime;

        rb.MovePosition(
            newPosition
        );
    }

    public bool TryConsumeParry()
    {
        if (!IsParryWindowOpen)
            return false;

        parryConsumed = true;
        return true;
    }

    public void QueueAttackStep(float distance)
    {
        if (distance <= 0f)
            return;

        remainingAttackStepDistance = distance;

        attackStepSpeed =
            attackStepDuration > 0f
                ? distance / attackStepDuration
                : distance;
    }

    private ShieldData GetEquippedShield()
    {
        if (playerEquipment == null)
            return null;

        return playerEquipment.EquippedShield;
    }

    private void ResetParryWindow()
    {
        parryWindowEndTime = 0f;
        parryConsumed = false;
    }

    private void Move()
    {
        Vector3 totalDisplacement =
            Vector3.zero;

        float currentMoveSpeed =
            moveSpeed;

        if (playerInventory != null &&
            playerInventory.IsOverEncumbered)
        {
            currentMoveSpeed *= encumberedSpeedMultiplier;
        }

        if (IsBlocking)
        {
            ShieldData shield =
                GetEquippedShield();

            if (shield != null)
            {
                currentMoveSpeed *=
                    shield.BlockingMoveSpeedMultiplier;
            }
        }
        else if (playerAttack != null &&
                 playerAttack.IsAttacking)
        {
            currentMoveSpeed *=
                playerAttack.CurrentMovementMultiplier;
        }

        Vector3 targetVelocity =
            movementDirection.sqrMagnitude > 0.01f
                ? movementDirection * currentMoveSpeed
                : Vector3.zero;

        float rate =
            targetVelocity.sqrMagnitude > planarVelocity.sqrMagnitude
                ? moveAcceleration
                : moveDeceleration;

        planarVelocity = Vector3.MoveTowards(
            planarVelocity, targetVelocity, rate * Time.fixedDeltaTime);

        totalDisplacement += planarVelocity * Time.fixedDeltaTime;

    if (remainingAttackStepDistance > 0f)
    {
        Vector3 stepDirection =
            transform.forward;

        stepDirection.y = 0f;

        if (stepDirection.sqrMagnitude > 0.01f)
        {
            stepDirection.Normalize();

            float stepThisFrame =
                Mathf.Min(
                    remainingAttackStepDistance,
                    attackStepSpeed *
                    Time.fixedDeltaTime
                );

            totalDisplacement +=
                stepDirection *
                stepThisFrame;

            remainingAttackStepDistance -=
                stepThisFrame;
        }
    }

        if (totalDisplacement.sqrMagnitude <
            0.0001f)
        {
            rb.linearVelocity =
                Vector3.zero;

            return;
        }

        rb.MovePosition(
            rb.position +
            totalDisplacement
        );
    }

    private void RotateTowardsMovement()
    {
        RotateTowardsDirection(
            movementDirection
        );
    }

    private void RotateTowardsCamera()
    {
        if (cameraFollow == null)
            return;

        RotateTowardsDirection(
            cameraFollow.GetCameraForward()
        );
    }

    private void RotateTowardsDirection(
        Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <
            0.01f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );

        Quaternion smoothRotation =
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed *
                Time.fixedDeltaTime
            );

        rb.MoveRotation(
            smoothRotation
        );
    }
    public bool IsMovingForward
    {
        get
        {
            if (!IsMoving)
                return false;

            Vector3 localDirection =
                transform.InverseTransformDirection(
                    movementDirection.normalized
                );

            return localDirection.z > 0.2f;
        }
    }

    public bool IsMovingBackward
    {
        get
        {
            if (!IsMoving)
                return false;

            Vector3 localDirection =
                transform.InverseTransformDirection(
                    movementDirection.normalized
                );

            return localDirection.z < -0.2f;
        }
    }
}