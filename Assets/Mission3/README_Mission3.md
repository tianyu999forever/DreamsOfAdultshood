# Mission3 — Hotel 24 Lost and Found

作者：Sheng-Chieh Lee / senionlee。独立关卡，以原 Hotel 24 的酒店探索、入口地图和返回前台为基础，改为收集失物、读住客线索、出口归还结算。

## 打开和试玩

1. Unity Hub → Add project from disk，选择本仓库根目录。
2. 使用 Unity **6000.3.23f1**（与项目一致）。
3. 打开 `Assets/Scenes/Mission3.unity`，按 Play，点击 Begin the night shift。
4. WASD 移动，Shift 跑步，E 交互，Tab 日志，M 地图，Esc 暂停。默认 Moving 脚本也保留 Space 跳跃。
5. 读 301 / 302 / 303 的住客留言，寻找六件失物。物品出现的房间不等于物主房间。
6. 收集至少三件即可在大厅前台或电梯处结算。建议找齐六件再归还。每件物品指定物主，确认后本次得分锁定。

没有倒计时、自动失败或负重减速。入口墙面有可查看的真实楼层平面图，地图与关卡由同一布局生成；地图北向固定，红点是入口地图位置，不是实时玩家追踪。

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

自有脚本全部在 `Assets/Scripts/Mission3/`，按 mission3 + 功能 + 挂载对象 + ShengChiehLee 命名。自有材质和地图在 `Assets/Mission3/`；地板/墙面材质从公共材质复制成独立实例，天花板引用公共材质，公共素材本身不修改。

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
