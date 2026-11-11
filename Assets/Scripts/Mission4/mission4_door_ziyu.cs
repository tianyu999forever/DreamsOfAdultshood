using UnityEngine;

// 出口大门：交付成功后向上升起
public class Mission4_Door_ziyu : MonoBehaviour
{
    public float openHeight = 4.5f;
    public float openTime = 1.5f;

    bool opening;
    float t;
    Vector3 start;

    void Start() { start = transform.position; }

    public void Open() { opening = true; }

    void Update()
    {
        if (!opening || t >= 1f) return;
        t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, openTime));
        transform.position = start + Vector3.up * (openHeight * t);
    }
}
