using UnityEngine;

public class mission3Lift_Prop_ShengChiehLee : MonoBehaviour
{
    public Transform leftDoor, rightDoor;
    public float travel = .78f;
    public float speed = 1.5f;
    public bool IsOpen { get; private set; }
    [Header("Passenger space used for score settlement")]
    public Vector3 settlementCenter=new Vector3(0,2.4f,.95f);
    public Vector3 settlementSize=new Vector3(2.24f,4.7f,1.6f);
    public bool ContainsPassenger(Vector3 world)
    {
        var p=transform.InverseTransformPoint(world)-settlementCenter;var half=settlementSize*.5f;
        return Mathf.Abs(p.x)<=half.x&&Mathf.Abs(p.y)<=half.y&&Mathf.Abs(p.z)<=half.z;
    }
    Vector3 leftClosed, rightClosed;

    void Awake()
    {
        if (leftDoor != null) leftClosed = leftDoor.localPosition;
        if (rightDoor != null) rightClosed = rightDoor.localPosition;
    }

    // Open-only: the exit remains accessible and never closes on the player.
    public void Open() { IsOpen = true; }

    void Update()
    {
        if (leftDoor == null || rightDoor == null) return;
        leftDoor.localPosition = Vector3.MoveTowards(leftDoor.localPosition,
            leftClosed + (IsOpen ? Vector3.left * travel : Vector3.zero), speed * Time.deltaTime);
        rightDoor.localPosition = Vector3.MoveTowards(rightDoor.localPosition,
            rightClosed + (IsOpen ? Vector3.right * travel : Vector3.zero), speed * Time.deltaTime);
    }
}
