# main 合入与兼容性验证

日期：2026-10-30。环境：Unity 6000.3.23f1，Windows 64 位开发构建。

## 结果

已将新版 Mission6 两关、评分、照片、敌人及对应资源合入用户提供的 DreamsOfAdultshood-main.zip 工程副本。未发现 Mission6 与 main 公共脚本或资源 GUID 的兼容冲突。Windows 构建成功，错误数为 0；实际程序的 MISSION 6 入口、PLAN 失败和重试均正常。

## 修正的 main 原有问题

1. **选关按钮文字与场景目标错位**：Mission2 场景的 LevelSelectMenu 将场景数组序列化为构建顺序，但按钮文字是 MISSION 1 到 MISSION 6。原先点击 MISSION 6 会进入 Mission1。已仅调整该组件的六项场景名称，使文字、索引和实际目标一致；构建场景顺序保留。
2. **Mission3 编辑器工具依赖另一台电脑**：电梯预览输出写死在 C:/Users/j1a2m/...，新电脑打开工程时报 UnauthorizedAccessException；启动升级还会自动重写已保存的场景。已改为项目内 docs/Mission3/mission3-lift.png，升级由原 Mission3 菜单手动执行。Mission3 场景本身与原压缩包一致。

上述修复文件：

- Assets/Scenes/Mission2_TianyuTang.unity（仅选关数组顺序）
- Assets/Scripts/Mission3/Editor/mission3Lift_Editor_ShengChiehLee.cs（路径和自动执行入口）

新版关卡资源集中在 Assets/Mission6_Jizhijin/；该目录新增 main 集成检查工具 Mission6_MainAudit_Jizhijin.cs。公共移动、背包、怀表等原运行脚本未修改，原 Packages 与 ProjectSettings 保留。

## 验证证据

| 检查 | 结果 | 记录 |
| --- | --- | --- |
| 六个构建场景的缺失脚本检查 | 6 项通过，缺失数均为 0 | main-scene-inspection.txt |
| main 实际菜单绑定、进入/离开 Mission6、暂停恢复、其他场景加载、Mission2 导航绑定 | 21 项通过，无运行错误 | main-runtime-verification.txt |
| Mission6 两关完整流程 | 76 项通过，无运行错误 | campaign-verification.txt |
| Windows 打包 | Succeeded，0 错误 | main-player-build.txt |
| Windows 程序实际点击菜单、显示地图、PLAN 超时、点击重试 | 5 项通过 | windows-player-manual-verification.txt |

两关流程包含 7/10 与 11/15 的最低通过边界、评分、实体开门与通道、照片次数和 5 秒时长、全部字母拾取、完整背包滚动选择、敌人巡逻/追逐/遮挡/捕获、第二关重试和两关成绩结算。截图以 campaign- 开头。

## 仍然存在的原工程警告与验证范围

最终 Windows 构建报告 486 条警告，主要来自原 Unity AI/Sentis 推理包的着色器变体；编辑器还可见原 ExitPuzzle.cs 的 CS8321、其他关卡负缩放碰撞体及灯光阴影图集警告。没有为消除这些原警告而改写其他关卡机制。Mission2 启动的导航警告已额外核查：进入运行后敌人都在其自身 NavMesh 上。

Mission6 在 main 的 Unity Play 模式中完整验证；其他任务做了场景加载检查，并未逐一完成所有任务玩法。Windows 程序做了菜单、Mission6 启动和失败重试验证；本机没有 WebGL 构建支持，未验证 WebGL。

## 打开方式

使用 Unity Hub 打开本压缩包解压后的 DreamsOfAdultshood-main 工程。直接打开 Assets/Mission6_Jizhijin/Scenes/Mission6_Jizhijin.unity 可以游玩两关；完整程序保持原构建顺序，从 Mission2 的选关面板点击 MISSION 6 进入。

当前构建顺序：Mission2_TianyuTang → Mission3 → ZhuohengWu Mission5 → Mission4 → Mission6_Jizhijin → Mission1。因此 Mission6 最终 NEXT MISSION 进入 Mission1。

两关规则见同目录 README.md。检查记录和报告都随工程打包；Library、Temp、Logs 和本地缓存未包含在工程压缩包中。
