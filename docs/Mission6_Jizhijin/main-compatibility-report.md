# Mission6 最新 main 兼容检查与黑夜迷宫更新

日期：2026-11-06。Unity 6000.3.23f1，Windows 64 位开发构建。

本次从 GitHub 下载的 main 为 `8211d746dad3ac91d2553214f544815e6f4fd0a5`（Fixed Mission 3 GUID）。已在保留新版 Mission6 的基础上合入该版本，包含 Mission3 的资源目录调整与场景 GUID 修复。前次选关菜单映射和 Mission3 编辑器便携路径修复继续保留。

## 本次实现

- 第一关 PLAN 默认可直接点击 ENTER LEVEL 2，同时可以 WASD 移动；Tab 切换鼠标视角和可点击指针。按 2、点入口按钮、第一关答题界面的 NEXT LEVEL，或沿开放通道步行，都无需完成第一关解谜即可进入第二关。离开 PLAN 自动恢复普通第一人称操作。
- 第一关达到 7/10 时保留本关评分；未达标进入第二关记 SKIPPED/0 分，最终平均分包含该 0 分。第二关仍需 11/15 才能完成。
- 第二关 28×28 采用用户确认的 B 四区串联布局，左下 → 右下 → 右上 → 左上，最少七次转向。175 个墙格（含边界）、四个编号地标，填字移到左下区域，字母与巡逻点重新安排；地图、场景和 NavMesh 同步。
- 字母下拉框按剩余实体道具过滤，选中后从其他格移除，清空或改选归还。多枚相同字母仍可分别使用，x2/x3 显示剩余数量，当前格保留自身选择。
- 仅第二关探索启用 30 秒白天 → 10 秒黑夜循环。顶部倒计时，黑夜降低环境光与视距、隐藏场景和照片中的字母，敌人速度 ×1.6；天亮恢复。PLAN 不触发，答题时与敌人一起暂停；通关计时不暂停，重试重新计时。
- 修复敌人装饰眼睛的碰撞体阻挡自身近距离视线，确保近距离捕获正常。

## 验证

- `campaign-verification.txt`：166 项通过，无运行错误。包含物理通道、第一关可跳过、评分边界、地图逐格一致、全部拾取物与出口的 NavMesh 可达性、字母分配与恢复、重复字母用量、滚动选择、敌人行为、两轮实际 30/10 秒周期、黑夜暂停/重置/加速/恢复及两关结算。
- `main-runtime-verification.txt`：25 项通过，无运行错误。包含最新 main 菜单、直接点击/Tab/2 入口、公共移动控制、暂停恢复、跨场景、Mission2 导航及其他任务场景加载。
- `main-scene-inspection.txt`：6 个构建场景均无丢失脚本。资源 GUID 扫描未发现冲突，Mission3 场景与最新 main 的修复一致。
- `main-player-build.txt`：Windows 构建 Succeeded，0 错误。487 条警告主要来自原 Unity AI/Sentis 着色器与原 ExitPuzzle.cs 的 CS8321；原公共运行脚本未因本次功能而改写。
- `windows-player-manual-verification.txt`：前版真实 Windows 程序鼠标操作验证，主菜单进入 Mission6、第一关直接点击按钮、零字母零答案进入第二关与独立计时均正常。本次地图回调另完成 Unity 全流程检查与 Windows 重新构建。

`campaign-12-day-before-night.png`、`campaign-13-night-maze.png` 和 `campaign-14-night-photo.png` 分别记录白天、夜间字母隐藏与夜间照片。其余 campaign 截图记录地图、填字和结算。

测试范围为 Unity Play 模式完整 Mission6 流程、Windows 构建及实际入口操作。其他任务做场景加载兼容检查；未逐一通关，也未进行 WebGL 构建验证。Unity 已退出运行，恢复正常多窗口编辑布局。

打开 `Assets/Mission6_Jizhijin/Scenes/Mission6_Jizhijin.unity` 即可游玩。任务构建顺序保留最新 main，Mission6 最终 NEXT MISSION 仍按顺序进入 Mission1。所有关卡专用运行脚本与生成资源在 `Assets/Mission6_Jizhijin/`。

## 地图难度回调

用户比较三份草案后选择 B 四区串联。按该方案保留完整墙体，填字格不再自动打通墙；主路线至少七次转向。重新安排起终点、字母、平台、PLAN 地图板、提前离开触发区与巡逻点，并加入四个编号地标。检查每个拾取物、填字格、出口和巡逻点的可达性，以及第二关实际步行离开 PLAN。黑夜、评分、直接进入第二关和字母分配规则沿用本版。
