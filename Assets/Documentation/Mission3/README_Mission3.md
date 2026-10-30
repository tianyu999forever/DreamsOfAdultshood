# Mission3 — Hotel 24 Lost and Found

作者：Sheng-Chieh Lee / senionlee。独立关卡，以原 Hotel 24 的酒店探索、入口地图和返回前台为基础，改为收集失物、读住客线索、出口归还结算。

## 打开和试玩

1. Unity Hub → Add project from disk，选择本仓库根目录。
2. 使用 Unity **6000.3.23f1**（与项目一致）。
3. 打开 `Assets/Scenes/Mission3.unity`，按 Play，点击 Begin the night shift。
4. WASD 移动，Shift 跑步，E 交互，Q 查看怀表，Tab 日志，M 地图，Esc 暂停。Space 跳跃。M / Tab 再按一次可关闭对应面板。
5. 读 301 / 302 / 303 的住客留言，寻找六件失物。物品出现的房间不等于物主房间。
6. 收集至少三件即可在大厅前台或电梯处结算。建议找齐六件再归还。每件物品指定物主，确认后本次得分锁定。

没有倒计时、自动失败或负重减速。怀表显示本轮探索用时。入口墙面和 M 面板使用实际关卡布局生成的楼层图：六件收集物以 1–6 编号，三份住客留言以琥珀色标示。M 面板的红色箭头追踪玩家位置和朝向；收集后对应标记变为淡色 +。地图不显示隐藏服务门、门锁或隐藏通路。

## Mission1 风格更新

- 正式场景只使用 `Assets/Scenes/Mission3.unity`，Build Settings 也指向此文件。旧版场景归档在 `Assets/Scenes/Backups/Mission3/`，不会自动同步，也不加入 Build Settings。
- Canvas 与 Player 由 Mission1 场景实例复制，包括移动速度、碰撞体、镜头、HeadBob、手部和怀表模型。Mission1 的算术与死亡脚本不带入本关。
- Canvas 的 WelcomePanel、MapPanel、JournalPanel、NotePanel、ExitPanel、ConfirmPanel、ResultPanel 和 PausePanel 都可在 Hierarchy 中编辑。背包延用 Mission1 的 InventoryPanel，功能由 Mission3 自有管理器驱动。
- 关卡为外圈回路与交错内支路，连接三间客房、展示间和休息区；隐藏门关闭时仍可抵达所有六件失物与三份留言。新增酒店路牌、收集物模型细节与静态建筑标记。
- `Mission3 > Apply Mission1 Player, Canvas and Maze Upgrade` 可重新生成完整正式场景，会覆盖该场景的手动布局；先保存或备份手动修改。
- `Mission3 > Validate Mission1 Presentation Upgrade` 检查相机、UI 引用、Player、地标、地图连通性、收集规则与下一关按钮。
- 验证工具可执行完整 Play Mode 流程并自动停止播放，不会关闭手动使用的 Unity 编辑器。

## 结算规则（100 分）

- 每件正确归还：10 分，6 件共 60 分。
- 每位住客的两件物品全部归还正确：额外 10 分，3 组共 30 分。
- 从北侧客房方向打开服务通道：10 分。
- 错误归还及未找到物品：0 分，不额外扣分。
- 归还之前可以更改分配，最终确认之后锁分；结果页显示每件失物的物主。

物主答案（开发参考）：301 旅行者 → Brass Compass / Coastal Postcard；302 音乐家 → Pocket Metronome / Vinyl Record；303 植物学家 → Seed Envelope / Pressed Fern。

## 团队集成

正式场景为 `Assets/Scenes/Mission3.unity`，不是备份场景。未修改公共 Build Settings；最终连接时请由负责整合的人将此场景加入列表。

默认玩家脚本 `Moving` 和 `MouseLook` 原样保留并挂在 Mission3 的玩家/相机上。暂停仅改变这些场景实例的 enabled 状态，不修改源码或其他场景。没有使用公共 `GameManager` / `ExitPuzzle` 的全局状态，避免与 Mission1 的数字玩法串扰。

自有脚本全部在 `Assets/Scripts/Mission3/`，按 Gameplay、Interactions、Player、UI、Editor 分类，按 mission3 + 功能 + 挂载对象 + ShengChiehLee 命名。自有材质在 `Assets/Materials/Mission3/`，地图与界面贴图在 `Assets/Textures/Mission3/`；地板/墙面材质从公共材质复制成独立实例，天花板引用公共材质，公共素材本身不修改。

在 `Mission3_Manager_ShengChiehLee` 上：

- `FinalScore` / `IsCompleted` 可读取本关结果。
- `onMissionCompleted(int)` 在玩家最终确认结算时只触发一次，供总分管理器连接。
- `nextSceneName` 默认留空。填入确切的下一场景名并加入 Build Settings 后，结果页才显示 Continue。
- 不写入其他关卡的存档，不主动跳转不存在的场景。

重新生成：Unity 菜单 `Mission3 > Build Hotel 24 Scene`。这会覆盖 Mission3 的场景布局；请先保存手工编辑版本。共享资源不会被编辑。生成器不修改公共 Build Settings。

## 验证

`Mission3 > Validate Hotel Scene` 检查六个唯一物品、三份留言、必要引用、缺失脚本、地图连通性，以及满分/错误归还/部分归还的分数。

`mission3Verify_Editor_ShengChiehLee.SmokeTest` 是批处理 Play Mode 测试入口，完成后会退出该 Unity 进程，请勿在正在手工编辑的 Unity 窗口中运行。

## AI 使用

Codex assisted with the Mission3 level layout, scene generation, custom Unity scripts, local integration, debugging, and validation. All additions are isolated from shared scripts and assets.


## 資料夾導覽（依 Mission1 的資源分類方式）

```text
Assets/
  Scenes/
    Mission3.unity                    正式場景／Build Settings 入口
    Backups/Mission3/
      Mission3_PreUpgrade.unity       介面與地圖改版前的舊場景
      Mission3_Prototype.unity        最早的原型場景
  Scripts/Mission3/
    Gameplay/                        玩法流程、計分規則
    Interactions/                    失物、住客留言、隱藏門、電梯互動
    Player/                          本關懷錶控制
    UI/                              Canvas、背包、地圖、結算面板
    Editor/                          關卡生成、路徑、整理與驗證工具
  Materials/Mission3/
    Environment/                     牆面、木作、地毯、黃銅等場景材質
    UI/                              入口地圖看板材質
  Textures/Mission3/
    Maps/                            平面圖底圖與入口看板地圖
    UI/                              玩家位置箭頭
  Documentation/Mission3/
    README_Mission3.md                本文件
```

日常編輯請開啟正式場景；Backups 只用於參考與回復。Mission1 共用的 Moving、MouseLook、HeadBob 等腳本保留原位。

生成器的資源路徑統一由 `Scripts/Mission3/Editor/mission3Paths_Editor_ShengChiehLee.cs` 管理，重新生成會沿用上述分類。
`Mission3 > Organize Mission3 Folders` 可執行整理；`Mission3 > Validate Mission3 Folder Organization` 檢查 GUID、正式場景、打包設定和資源引用。


## 空間與展示台調整（2026-10-29）

- 全館、服務通道與電梯的室內淨高提高至 4.8 公尺；牆面、門框、天花板、燈具一起調整。
- 平面長寬各擴大 15%，走道同步加寬；Player 體型和移動速度維持原設定。
- 六件失物移到房間側邊的展示台，保留原物主與計分規則。拾取感應區可穿過，展示台保持實體碰撞，避免卡住玩家。
- 入口地圖與 M 面板同步使用放大後的世界座標，隱藏門仍不顯示。
- 使用 `Mission3 > Play Guided Demo` 試玩。`Mission3 > Test and Play Guided Demo` 會先檢查教程、地圖、六個展示台的站立空間與拾取視線、結算，再開始全新試玩。
- 正式場景 `Assets/Scenes/Mission3.unity` 和試玩場景 `Assets/Scenes/Demos/Mission3_Demo.unity` 都已套用；重新生成時也會保留這套尺寸與擺放規則。
