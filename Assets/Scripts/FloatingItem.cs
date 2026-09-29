using UnityEngine;

public class FloatingItem : MonoBehaviour
{
    public float height = 0.2f;
    public float speed = 1.5f;

    Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * speed) * height;

        transform.position =
            startPosition + Vector3.up * y;
    }
}