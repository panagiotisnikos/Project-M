using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 12f;

    private Rigidbody rb;
    private Vector3 movementInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        movementInput = new Vector3(horizontal, 0f, vertical).normalized;
    }

    private void FixedUpdate()
    {
        Move();
        RotateTowardsMovement();
    }

    private void Move()
    {
        Vector3 newPosition = rb.position + movementInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    private void RotateTowardsMovement()
    {
        if (movementInput == Vector3.zero)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(movementInput);
        Quaternion smoothRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(smoothRotation);
    }
}