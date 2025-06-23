using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(5, 0, -10);

    void LateUpdate()
    {
        if (target == null)
            return;
            
        Vector3 desiredPosition = new Vector3(target.position.x, 0, 0) + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
