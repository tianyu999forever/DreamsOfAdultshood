using UnityEngine;

// Scene-local interactions only. Never attached to shared prefabs.
public class mission3Interact_Prop_ShengChiehLee : MonoBehaviour
{
    public enum Kind { LostProperty, GuestNote, FloorMap, ServiceDoor, Checkout, LiftDoors }
    public Kind kind;
    public int index;
    public string displayName;
    [TextArea] public string description;
    public string Prompt => kind == Kind.LostProperty ? "E  Collect " + displayName : "E  " + displayName;
}
