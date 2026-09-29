using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    Transform mainCamera;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null)
            return;

        Vector3 direction =
            transform.position - mainCamera.position;

        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(direction);
        }
    }
}