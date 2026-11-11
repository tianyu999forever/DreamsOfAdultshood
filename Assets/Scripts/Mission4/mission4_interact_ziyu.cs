using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 挂在玩家摄像机上：准星射线、左键交互、数字键放置、按住 C 切菜
public class Mission4_Interact_ziyu : MonoBehaviour
{
    public Mission4_Game_ziyu game;
    public Mission4_Bag_ziyu bag;
    public TMP_Text promptText;

    public float lookRange = 4.5f;   // 左键点击距离
    public float workRange = 4.5f;   // 案板/灶台/切菜有效距离

    Camera cam;
    Transform player;

    void Start()
    {
        cam = GetComponent<Camera>();
        player = transform.parent != null ? transform.parent : transform;
    }

    void Update()
    {
        if (game == null || bag == null || !game.MazeRunning)
        {
            SetPrompt("");
            return;
        }

        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (kb == null) return;

        Mission4_Station_ziyu looked = LookedStation();

        // 数字键：背包打开时，把对应格子的食物放到附近的案板/灶台
        if (bag.IsOpen)
        {
            for (int i = 0; i < 4; i++)
            {
                if (DigitKey(kb, i).wasPressedThisFrame)
                {
                    Mission4_Station_ziyu target = PlaceTarget(looked);
                    if (target == null)
                        game.Toast("Get closer to the cutting board or the stove.");
                    else
                        target.TryPlace(i, bag);
                }
            }
        }

        // 左键：拿东西 / 取回 / 递交
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && looked != null)
            looked.OnClick(bag, game);

        // 按住 C：在案板附近累计切菜时间
        Mission4_Station_ziyu board = NearestBoardCutting();
        if (board != null && kb.cKey.isPressed)
            board.Cut(Time.deltaTime);

        // 提示文字
        if (board != null)
            SetPrompt(board.CutProgressText());
        else if (looked != null)
            SetPrompt(looked.GetPrompt(bag));
        else
            SetPrompt("");
    }

    static UnityEngine.InputSystem.Controls.KeyControl DigitKey(Keyboard kb, int i)
    {
        switch (i)
        {
            case 0: return kb.digit1Key;
            case 1: return kb.digit2Key;
            case 2: return kb.digit3Key;
            default: return kb.digit4Key;
        }
    }

    Mission4_Station_ziyu LookedStation()
    {
        if (cam == null) return null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, lookRange, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponentInParent<Mission4_Station_ziyu>();

        return null;
    }

    float FlatDistance(Mission4_Station_ziyu s)
    {
        Vector3 d = s.transform.position - player.position;
        d.y = 0f;
        return d.magnitude;
    }

    Mission4_Station_ziyu PlaceTarget(Mission4_Station_ziyu looked)
    {
        if (looked != null &&
            (looked.type == M4StationType.Board || looked.type == M4StationType.Stove) &&
            FlatDistance(looked) <= workRange)
            return looked;

        Mission4_Station_ziyu best = null;
        float bestD = workRange;
        foreach (Mission4_Station_ziyu s in Mission4_Station_ziyu.All)
        {
            if (s.type != M4StationType.Board && s.type != M4StationType.Stove) continue;
            float d = FlatDistance(s);
            if (d <= bestD) { bestD = d; best = s; }
        }
        return best;
    }

    Mission4_Station_ziyu NearestBoardCutting()
    {
        foreach (Mission4_Station_ziyu s in Mission4_Station_ziyu.All)
        {
            if (s.type == M4StationType.Board && s.IsCuttingNow && FlatDistance(s) <= workRange)
                return s;
        }
        return null;
    }

    void SetPrompt(string s)
    {
        if (promptText != null) promptText.text = s;
    }
}
