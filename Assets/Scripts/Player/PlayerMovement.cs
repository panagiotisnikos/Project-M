using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Blocking")]
    [SerializeField] private float blockingMoveSpeedMultiplier = 0.45f;

    [Header("Parry")]
    [SerializeField] private float parryWindowDuration = 0.2f;

    [Header("References")]
    [SerializeField] private CameraFollow cameraFollow;

    private Rigidbody rb;
    private Vector3 movementDirection;

    private float parryWindowEndTime;
    private bool parryConsumed;

    public bool IsBlocking { get; private set; }

    public bool IsParryWindowOpen =>
        IsBlocking &&
        !parryConsumed &&
        Time.time <= parryWindowEndTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        UpdateBlocking();
        ReadMovementInput();
    }

    private void FixedUpdate()
    {
        rb.angularVelocity = Vector3.zero;

        Move();

        if (IsBlocking)
        {
            RotateTowardsCamera();
        }
        else if (movementDirection.sqrMagnitude > 0.01f)
        {
            RotateTowardsMovement();
        }
    }

    private void UpdateBlocking()
    {
        if (Input.GetMouseButtonDown(1))
        {
            parryWindowEndTime =
                Time.time + parryWindowDuration;

            parryConsumed = false;
        }

        IsBlocking = Input.GetMouseButton(1);

        if (!IsBlocking)
        {
            parryWindowEndTime = 0f;
            parryConsumed = false;
        }
    }

    public bool TryConsumeParry()
    {
        if (!IsParryWindowOpen)
            return false;

        parryConsumed = true;
        return true;
    }

    private void ReadMovementInput()
    {
        float horizontal =
            Input.GetAxisRaw("Horizontal");

        float vertical =
            Input.GetAxisRaw("Vertical");

        Vector3 input =
            new Vector3(horizontal, 0f, vertical).normalized;

        if (cameraFollow != null)
        {
            movementDirection =
                cameraFollow.GetCameraForward() * input.z +
                cameraFollow.GetCameraRight() * input.x;
        }
        else
        {
            movementDirection = input;
        }

        movementDirection.Normalize();
    }

    private void Move()
    {
        if (movementDirection.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        float currentMoveSpeed = moveSpeed;

        if (IsBlocking)
        {
            currentMoveSpeed *= blockingMoveSpeedMultiplier;
        }

        Vector3 newPosition =
            rb.position +
            movementDirection *
            currentMoveSpeed *
            Time.fixedDeltaTime;

        rb.MovePosition(newPosition);
    }

    private void RotateTowardsMovement()
    {
        RotateTowardsDirection(movementDirection);
    }

    private void RotateTowardsCamera()
    {
        if (cameraFollow == null)
            return;

        RotateTowardsDirection(
            cameraFollow.GetCameraForward()
        );
    }

    private void RotateTowardsDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        Quaternion smoothRotation =
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );

        rb.MoveRotation(smoothRotation);
    }
}