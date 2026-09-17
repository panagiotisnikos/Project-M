using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Camera")]
    [SerializeField] private float distance = 8f;
    [SerializeField] private float targetHeight = 1.6f;
    [SerializeField] private float pitch = 25f;
    [SerializeField] private float yaw = 0f;

    [Header("Control")]
    [Tooltip("Sensitivity and invert-Y now come from GameSettings (set via the options menu).")]
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private float minDistance = 4f;
    [SerializeField] private float maxDistance = 14f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera catches up to the player. Lower = smoother/laggier.")]
    [SerializeField] private float followSmoothTime = 0.12f;
    [Tooltip("How quickly the camera catches up to mouse-look rotation.")]
    [SerializeField] private float rotationLerpSpeed = 18f;

    [Header("References")]
    [SerializeField] private CameraShake cameraShake;

    private Vector3 smoothedFocus;
    private Quaternion smoothedRotation = Quaternion.identity;
    private Vector3 focusVelocity;
    private bool initialised;

    private void Awake()
    {
        if (cameraShake == null)
        {
            cameraShake = GetComponent<CameraShake>();
        }
    }

    private void LateUpdate()
    {
        if (GameUIController.IsPaused)
            return;

        if (target == null)
            return;

        // The mouse drives the UI while the pack/crafting panel is open - keep following, stop looking.
        if (!InventoryUI.IsOpen && !CraftingUI.IsOpen)
        {
            float sensitivity = GameSettings.MouseSensitivity;
            float pitchSign = GameSettings.InvertY ? 1f : -1f;

            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch += Input.GetAxis("Mouse Y") * sensitivity * pitchSign;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            distance -= Input.GetAxis("Mouse ScrollWheel") * 4f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = target.position + Vector3.up * targetHeight;

        if (!initialised)
        {
            smoothedFocus = focusPoint;
            smoothedRotation = targetRotation;
            initialised = true;
        }

        float dt = Time.unscaledDeltaTime;

        smoothedFocus = Vector3.SmoothDamp(
            smoothedFocus, focusPoint, ref focusVelocity, followSmoothTime, Mathf.Infinity, dt);

        smoothedRotation = Quaternion.Slerp(
            smoothedRotation, targetRotation, 1f - Mathf.Exp(-rotationLerpSpeed * dt));

        transform.position = smoothedFocus - smoothedRotation * Vector3.forward * distance;
        transform.rotation = smoothedRotation;

        if (cameraShake != null)
        {
            transform.position +=
                transform.TransformVector(cameraShake.CurrentPositionOffset);

            transform.rotation *=
                Quaternion.Euler(cameraShake.CurrentRotationOffset);
        }
    }

    public Vector3 GetCameraForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        return forward.normalized;
    }

    public Vector3 GetCameraRight()
    {
        Vector3 right = transform.right;
        right.y = 0f;

        return right.normalized;
    }
}