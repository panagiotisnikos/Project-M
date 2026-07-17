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
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private float minDistance = 4f;
    [SerializeField] private float maxDistance = 14f;

    private void LateUpdate()
    {
        if (target == null)
            return;

        // The camera now rotates freely with the mouse.
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        distance -= Input.GetAxis("Mouse ScrollWheel") * 4f;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint =
            target.position + Vector3.up * targetHeight;

        transform.position =
            focusPoint - rotation * Vector3.forward * distance;

        transform.LookAt(focusPoint);
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