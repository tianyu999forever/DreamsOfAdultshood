# Mission3 — Hotel 24 Lost and Found

作者：Sheng-Chieh Lee / senionlee。独立关卡，以原 Hotel 24 的酒店探索、入口地图和返回前台为基础，改为收集失物、读住客线索、出口归还结算。

## 打开和试玩

1. Unity Hub → Add project from disk，选择本仓库根目录。
2. 使用 Unity **6000.3.23f1**（与项目一致）。
3. 打开 `Assets/Scenes/Mission3.unity`，按 Play，点击 Begin the night shift。
4. WASD 移动，Shift 跑步，E 交互，Q 查看怀表，Tab 日志，M 地图，Esc 暂停。Space 跳跃。M / Tab 再按一次可关闭对应面板。
5. 读 301 / 302 / 303 的住客留言，寻找六件失物。物品出现的房间不等于物主房间。
6. 收集至少三件後，只能在大廳 Checkout / Lost & Found 桌指定物主並確認交還。確認後收集與物主選擇鎖定；接著走進電梯，使用 SETTLE SCORE 結算，最後在電梯內按 NEXT LEVEL。建議找齊六件再交還。

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


## 關卡結算 Canvas（2026-10-31）

- 獨立 Prefab：`Assets/Prefabs/Mission3/UI/Mission3ResultCanvas.prefab`，已接入正式場景與 Demo。
- `Mission3_ResultCanvas/ResultPanel` 可編輯總分、60/30/10 加分明細、六件物品的歸還結果，以及 PLAY AGAIN / NEXT LEVEL 按鈕。
- `mission3Result_UI_ShengChiehLee` 指向當前場景的 Manager；Manager 的 `Settlement` 提供完整結算明細，`FinalScore` 提供總分，`onMissionCompleted(int)` 仍只觸發一次。
- 結算後停止 Player 並開啟滑鼠，Esc 不會意外關閉結果。下一關尚未設定或未加入 Scene List 時，按鈕停用並保留結算畫面；Demo 的 NEXT LEVEL 也使用正式場景切換入口，不再代替 EXIT DEMO。
- 依照使用者選擇，正式版與 Demo 的 `nextSceneName` 都先設為 `Assets/Scenes/Mission1.unity`，用於實際測試關卡切換；Mission1 已在 Scene List 啟用。之後接正式下一關時，修改此欄位並啟用該場景即可。
- `Mission3 > Preview Score Results` 開啟 100 分範例，PLAY AGAIN 可開始正常新一輪；先停止播放再使用此選單。
- 已測試 100 分與部分歸還 30 分、正確/錯誤/未找到狀態、重複結算防護、實際重玩與 NEXT LEVEL 切換。NEXT LEVEL 使用場景中實際儲存的 Mission1 入口完成切換，沒有在測試時臨時覆寫目的地。


## 櫃台交還與電梯結算（2026-11-02）

- 只有大廳 `Checkout terminal` 能接受物品；電梯內原本的交還功能已移除。
- 交還確認呼叫 `CompleteCheckout()`，設定 `ReturnsSubmitted`，鎖定收集與物主選擇，恢復玩家移動並提示前往電梯。此時不計分、不觸發 `onMissionCompleted`。
- 玩家必須走進電梯的乘客區域，再操作 SETTLE SCORE。`UseLiftTerminal()` 只在已交還且身處電梯時結算分數、開啟結果 Canvas、觸發一次完成事件。
- `LoadNextLevel()` 與結果 Canvas 的 NEXT LEVEL 都再次檢查玩家在電梯內；目前仍接到 Mission1。
- `mission3Lift_Prop_ShengChiehLee` 的 Settlement Center / Size 可調整電梯內判定範圍，座標跟隨電梯的 Transform。
- 大廳電梯旁新增 `Mission3_PropsAndInteractions/CheckoutFlowNotice` 告示。教程目標、交還確認、電梯提醒及 M 地圖也已更新為：R 交還物品、L 結算與下一關。
- `Mission3 > Apply Desk Returns and Lift Settlement` 可套用這套流程；重新生成正式場景也會保留此設定。


## 夜間巡房員試玩原型（本機，2026-11-03）

- 套用於正式 Mission3 與 Mission3_Demo。怪物沿用 Mission2 的 Capsule 身形與原本材質，調整為適合 4.8 公尺天花板的 3.6 公尺高度；未下載新美術素材，也未帶入 Mission2 的手電筒或死亡面板腳本。
- 第 1 件失物仍可安心探索；第 2 件觸發 3 秒腳步／燈光預警，之後怪物從視野外遠處出現。巡邏速度 2、追逐速度 6，玩家奔跑速度維持 8。
- 視線範圍 12 公尺、視角 110 度；牆壁遮擋視線。奔跑會引起調查，隔牆聽覺距離減半，並以可行走路程限制聽覺。
- 失去視線 4 秒後只搜索最後出現位置 6 秒，再返回巡邏。第 4 件失物擴大至東翼和休息區，沒有額外提高追逐速度。
- 大廳櫃台與電梯所在區域完全排除於怪物導航。地圖、便條、日誌、暫停及交還面板開啟時，怪物停止移動與判定；關閉面板有 1 秒保護。
- 被抓會淡出並送回大廳，保留已收集物品、筆記及物主選擇，不增加或扣除原本 100 分制的分數。回到大廳後有 8 秒保護。
- 完成櫃台交還後，巡房員退場；電梯結算維持原流程。個人分支的下一關仍為原先的 Mission1 測試目標。
- 腳步優先使用 Player 已有的腳步音效；沒有音效時由程式生成低沉腳步，不使用網路音檔。
- `Mission3 > Apply and Test Night Patrol Prototype` 重新套用及檢查原型；`Mission3 > Test Night Patrol Demo` 測試已儲存的場景。重新生成整個飯店後，使用前者重新安裝巡房員。
- 場景中的 `Mission3_NightPatrol` 集中放置怪物、巡邏點、安全區與導航；在編輯模式將巡房員子物件停用，可關閉這個試玩原型。
- 本次工作只在個人分支保留未提交的本機檔案，不建立 commit、不推送 GitHub。
