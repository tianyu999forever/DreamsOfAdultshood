using UnityEngine;
using UnityEngine.InputSystem;

public class HeadBob : MonoBehaviour
{
    public float walkAmount = 0.06f;
    public float runAmount = 0.11f;

    public float walkSpeed = 8f;
    public float runSpeed = 13f;

    Vector3 startPosition;
    float timer;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (Keyboard.current == null)
            return;

        bool moving =
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.dKey.isPressed;

        bool running =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;

        Vector3 targetPosition = startPosition;

        if (moving)
        {
            float speed = running ? runSpeed : walkSpeed;
            float amount = running ? runAmount : walkAmount;

            timer += Time.deltaTime * speed;

            float x = Mathf.Cos(timer * 0.5f) * amount * 0.5f;
            float y = Mathf.Sin(timer) * amount;

            targetPosition += new Vector3(x, y, 0);
        }
        else
        {
            timer = 0;
        }

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            12f * Time.deltaTime
        );
    }
}