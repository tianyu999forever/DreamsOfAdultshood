using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// The same Mission1 hand/watch model; Q inspects elapsed shift time without conflicting with E pickups.
public class mission3Watch_Player_ShengChiehLee : MonoBehaviour
{
    public mission3Hotel_Manager_ShengChiehLee manager;
    public GameObject visuals;
    public TMP_Text timer;
    public Vector3 hiddenPosition, shownPosition, hiddenRotation, shownRotation;
    bool showing;
    void Start() { transform.localPosition=hiddenPosition; transform.localRotation=Quaternion.Euler(hiddenRotation); visuals.SetActive(false); }
    void Update()
    {
        if(manager==null) return;
        if(manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.None && Keyboard.current!=null && Keyboard.current.qKey.wasPressedThisFrame) showing=!showing;
        bool visible=showing && manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.None;
        if(visible) visuals.SetActive(true);
        transform.localPosition=Vector3.Lerp(transform.localPosition,visible?shownPosition:hiddenPosition,Time.unscaledDeltaTime*9);
        transform.localRotation=Quaternion.Slerp(transform.localRotation,Quaternion.Euler(visible?shownRotation:hiddenRotation),Time.unscaledDeltaTime*9);
        if(!visible && Vector3.Distance(transform.localPosition,hiddenPosition)<.01f) visuals.SetActive(false);
        if(timer!=null) timer.text=System.TimeSpan.FromSeconds(manager.ElapsedSeconds).ToString(@"mm\:ss");
    }
}
