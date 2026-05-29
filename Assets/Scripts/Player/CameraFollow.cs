using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 8f, -8f);
    [SerializeField] private float followSpeed = 8f;

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 targetPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        transform.LookAt(target.position);
    }
}