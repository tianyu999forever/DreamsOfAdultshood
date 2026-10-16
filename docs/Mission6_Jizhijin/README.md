# Mission6 20×20 交叉填字迷宫

打开 `Assets/Mission6_Jizhijin/Scenes/Mission6_Jizhijin.unity`，点击 Play。

## PLAN 阶段

开局有 30 秒规划时间，玩家可以正常移动、转动视角、跑步和跳跃。
地图板放在起点房间的墙上，可以走近查看。
从右侧开口走出起点房间，会立即提前结束 PLAN，并开始 90 秒探索倒计时。
如果一直留在房间直到 PLAN 结束，则与原玩法一样判定失败。
离开后起点地图隐藏；再次回房间不会重置 PLAN。

## 交叉填字

参考提供的图片，采用横竖交叉、共享字母格的布局。
横向为 YOUR、OF、HAVE、HER；纵向为 COME、FROM、MAKE、HE。
棋盘有编号、已给字母和四个空白格，需要 O、R、A、E。
交叉处是同一个格子，只需一个字母道具，就会同时补全两个方向。
迷宫共有九个字母，其中四个用于填字，另外五个为干扰项，不必全部收集。
地图和照片直接标明全部九个字母及位置。抵达 E 出口后在空格中选择收集的字母。
相同字母的不同道具可以分别使用；同一个实体道具不能放入两个不同格子。
点击 CHECK CROSSWORD 判定，错误可以重选，缺少字母时点击 BACK TO GRID 返回。
打开出口面板即显示 PLAY AGAIN 和 NEXT LEVEL，答错或修改答案也不会隐藏。
PLAY AGAIN 可直接重开；NEXT LEVEL 在填字全部正确后可跳转，默认加载 Build Settings 中当前场景之后的下一场景，并恢复暂停时间。
若要指定任务，在 CrosswordPanel_Jizhijin 的脚本组件中填写 Next Scene Name；目标场景必须加入构建列表。
Unity 菜单 Mission6 Jizhijin > Add or repair NEXT LEVEL button 可补加或修复按钮，不需要重建地图。
填字界面上方显示全部已收集字母；展开选项的列表独立加宽，最多九个字母可一次看全。

## 操作和照片

WASD 移动，Shift 跑步，Space 跳跃，E 查看怀表。
探索阶段按 Z 查看地图照片，每局限用一次、持续 5 秒，倒计时继续运行。
PLAN 阶段不消耗照片；重开后恢复一次使用机会。

## 场景和地图

保留 20×20 格子地形、交错通道、平台和缓坡。
场景、墙上地图和照片从同一份布局生成，包含交叉棋盘的方向、编号、共享格和缺字。
关闭场景雾和镜头光晕，使用 Unity 内置几何体和材质。

Unity 菜单 Mission6 Jizhijin > Rebuild 20x20 word grid 可重建本关地形、界面和地图。
修改前的版本已保存在 before-crossword.zip；原公共资源保持原样。
Verify gameplay 菜单会运行 PLAN 移动、提前离开、超时失败、交叉格、照片和重开检查。
结果记录在 verification.txt，最新画面为 letters- 开头的 PNG。

与原版的详细对照见 changes-from-original.md。
