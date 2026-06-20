using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private CameraFollow cameraFollow;

    private Rigidbody rb;
    private Vector3 movementDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;

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

    private void FixedUpdate()
    {
        rb.angularVelocity = Vector3.zero;

        Move();

        if (movementDirection.sqrMagnitude > 0.01f)
        {
            RotateTowardsMovement();
        }
    }

    private void Move()
    {
        if (movementDirection.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 newPosition = rb.position + movementDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    private void RotateTowardsMovement()
    {
        if (movementDirection.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
        Quaternion smoothRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(smoothRotation);
    }
}