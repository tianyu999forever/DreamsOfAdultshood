using UnityEngine;
using UnityEngine.InputSystem;

// Tutorial-local copy of the shared MouseLook behaviour. The shared asset is unchanged.
public class mission3TutorialLook_Player_ShengChiehLee : MonoBehaviour
{
    public Transform player;
    public float sensitivity = .1f;
    float horizontalRotation, verticalRotation;
    int ignoreMouseFrames = 2;
    void Start() { if (player == null) player = transform.parent; SynchronizeRotation(); }
    public void SynchronizeRotation()
    {
        if (player == null) return;
        horizontalRotation = player.eulerAngles.y;
        float pitch = transform.localEulerAngles.x;
        verticalRotation = Mathf.Clamp(pitch > 180 ? pitch - 360 : pitch, -80f, 80f);
        ignoreMouseFrames = 2;
    }
    void Update()
    {
        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;
        if (ignoreMouseFrames > 0) { ignoreMouseFrames--; return; }
        var delta = Mouse.current.delta.ReadValue() * sensitivity;
        horizontalRotation += delta.x; verticalRotation = Mathf.Clamp(verticalRotation - delta.y, -80f, 80f);
        player.rotation = Quaternion.Euler(0, horizontalRotation, 0);
        transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        Physics.SyncTransforms();
    }
}
