using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    public Transform player;
    public float sensitivity = 0.1f;

    private float horizontalRotation;
    private float verticalRotation;

    // 忽略刚进入 Play 时不正常的鼠标位移
    private int ignoreMouseFrames = 2;

    private void Start()
    {
        if (player == null)
        {
            player = transform.parent;
        }

        horizontalRotation = player.eulerAngles.y;

        // 使用编辑器中已经设置好的相机上下角度
        verticalRotation =
            NormalizeAngle(transform.localEulerAngles.x);

        verticalRotation =
            Mathf.Clamp(verticalRotation, -80f, 80f);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (ignoreMouseFrames > 0)
        {
            ignoreMouseFrames--;
            return;
        }

        Vector2 mouse =
            Mouse.current.delta.ReadValue();

        mouse *= sensitivity;

        horizontalRotation += mouse.x;
        verticalRotation -= mouse.y;

        verticalRotation =
            Mathf.Clamp(verticalRotation, -80f, 80f);

        player.rotation =
            Quaternion.Euler(
                0f,
                horizontalRotation,
                0f
            );

        transform.localRotation =
            Quaternion.Euler(
                verticalRotation,
                0f,
                0f
            );
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}