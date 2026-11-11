using UnityEngine;

// 迷宫入口：玩家走进来就开始 3 分钟倒计时
public class Mission4_Entrance_ziyu : MonoBehaviour
{
    public Mission4_Game_ziyu game;

    void OnTriggerEnter(Collider other)
    {
        bool isPlayer =
            other.CompareTag("Player") ||
            other.transform.root.CompareTag("Player");

        if (!isPlayer || game == null) return;

        game.StartMaze();
    }
}
