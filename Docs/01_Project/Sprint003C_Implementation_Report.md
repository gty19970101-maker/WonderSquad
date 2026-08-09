# Sprint003C — Root Bridge Advantage / Environment Consequence Implementation Report

## 1. 当前状态

`PASSED / GO`

- Unity 目标版本：`6000.3.21f1`
- 实现基线：Sprint003A `PASSED / GO`、Sprint003B `PASSED / GO`
- 最终验收环境：Unity `6000.3.21f1`
- Sprint003C 专项测试、完整回归与正式 SleepingForest 人工验收均已通过，Console Error 为 `0`。

## 2. 已实现内容

### 只读 Snapshot 消费

`SleepingForestRootBridgeConsequence` 只持有 Inspector 显式配置的 `ForestSignalTrial`、`RootBridgeAdvantageSpan` 与 `RootBridgeAdvantageVisual`。

- 在 `OnEnable` 订阅 Trial **实例**的 `TrialStateChanged`，并读取 `CurrentSnapshot` 完成首次同步。
- 只依据 Snapshot 的 `IsCompleted` 与 `Revision` 决定 `Initial`/`Activated`。
- 相同或旧 Revision 不会重复写视觉、切换 Collider 或创建对象。
- Disable/Enable 后重新读取当前 Snapshot；不写 Trial、不读取 Trial 私有字段、不使用 static 状态、全局事件或 `Update` 轮询。

### 独立 Span 与视觉

- `RootBridgeAdvantageSpan` 只管理独立 `GroundSurface` 的 Renderer 与承载 Collider。
- `RootBridgeAdvantageVisual` 只管理 Marker 和每 Renderer 的 `MaterialPropertyBlock`；没有访问 `Renderer.material`，不写 `sharedMaterial`。
- `Initial`：Span Renderer/Collider 关闭、Dormant Marker 显示。
- `Activated`：Span Renderer/Collider 同时开启、Activated Direction Marker 显示。
- 视觉反馈含颜色覆盖及非颜色的方向性 Marker；没有 Animator、Tween、VFX、音效或 Shader 改造。

### 主桥和 Foundation 边界

`RootBridgeTemporaryCrossing` 没有被 Runtime 组件引用为可写对象，也没有被 Authoring 修改。Player、Camera、Input、Movement、GroundDetector、FallRecovery、Beacon、ForestSignalTrial 及 Interaction Foundation 均未修改。

## 3. 新增文件

Runtime（`WonderSquad.Puzzle`）：

- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/SleepingForestRootBridgeState.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/SleepingForestRootBridgeConsequence.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/RootBridgeAdvantageSpan.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/RootBridgeAdvantageVisual.cs`

Editor（`WonderSquad.Editor`）：

- `Client/Assets/WonderSquad/Editor/SleepingForestRootBridgeConsequenceAuthoring.cs`

测试：

- `Client/Assets/WonderSquad/Tests/EditMode/SleepingForestRootBridgeConsequenceEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/SleepingForestRootBridgeConsequencePlayModeTests.cs`

文档：

- 本报告
- `Docs/01_Project/Sprint003C_Review_Checklist.md`

## 4. Prefab、Scene 与几何 Authoring

编译后，在 Unity 菜单执行：

```text
Wonder Squad > Sprint003C > Apply Root Bridge Consequence
```

命令会先显示确认对话框并走 Unity 的保存确认流程；取消时不执行修改。它只创建/更新：

- `Assets/WonderSquad/Prefabs/SleepingForest/PF_RootBridgeAdvantageSpan.prefab`
- SleepingForest 中的 `ForestSignalRouteConsequence` 根、Start/Exit Junction 和一个 Span Prefab 实例。

它不会运行 003A Greybox Builder，不会修改 PlayerSandbox、测试场景、Build Profile、ProjectSettings，亦不会修改 `RootBridgeTemporaryCrossing`、Recovery 或 Foundation 对象。

几何采用单层、边缘相接策略：

| 对象 | Position | Scale | 连接规则 |
| --- | --- | --- | --- |
| `RootBridgeAdvantageStartJunction` | `(-5, -0.5, 23)` | `(2, 1, 8)` | 只接触既有入口 Junction 的西侧边界 |
| `RootBridgeAdvantageSpan/GroundSurface` | `(-5, -0.5, 34.5)` | `(2, 1, 15)` | 与 Start/Exit Junction 仅在 z 边界相接，位于 Main Bridge 西侧 |
| `RootBridgeAdvantageExitJunction` | `(-7, -0.5, 42.5)` | `(6, 1, 1)` | 只接触 Span 北侧及 SliceEndGround 南侧边界 |

Authoring 会校验 Span 与 `RootBridgeTemporaryCrossing` 的 Renderer/Collider 不存在正体积重叠。最终 Unity 专项测试和人工走图已确认新 Junction、`SliceEndGround` 与入口 Junction 的 Bounds、接地和路线连接正常。

## 5. 自动化测试

已新增专项测试覆盖：

- Initial 状态的 Renderer、Collider、Marker；
- A → B Completed 后的 Span 激活；
- IncorrectOrder 不激活，随后 A → B 可恢复；
- 同一 Revision 的启停/重新订阅幂等；
- `MaterialPropertyBlock` 不污染共享材质；
- 两个独立 Span Visual 在共享材质时仍保持各自的 PropertyBlock/Marker 状态；
- Span 与 Main Bridge 无正体积重叠；
- 正式场景初始组合；
- 正式场景 A → B 后的 Span 激活、主桥不变、Player/Camera/FallRecovery 回归。

Unity `6000.3.21f1` 最终实测结果：

| 测试 | 最终结果 |
| --- | --- |
| Sprint003C EditMode | `8 / 8 Passed`，`0 Failed`，`0 Skipped` |
| Sprint003C PlayMode | `4 / 4 Passed`，`0 Failed`，`0 Skipped` |
| 完整 EditMode 回归 | `108 / 108 Passed`，`0 Failed`，`0 Skipped` |
| 完整 PlayMode 回归 | `58 / 58 Passed`，`0 Failed`，`0 Skipped` |
| Console Error | `0` |

## 6. Unity Verification 问题与修复记录

### 6.1 EditMode Test `CS0104` Object 命名歧义

`SleepingForestRootBridgeConsequenceEditModeTests.cs` 同时使用 `System` 与 `UnityEngine`，测试中的 `Object` 调用产生 C# 命名歧义。

修复方式：Unity 对象相关调用显式使用 `UnityEngine.Object`。该修复只消除测试程序集编译错误，没有修改 Runtime、断言逻辑或测试行为。

### 6.2 Span / Visual 初始状态同步

`SleepingForestRootBridgeState.Initial` 的枚举值为 `0`。若生命周期仅依赖默认字段值判断“状态是否变化”，首次同步可能把默认值误认为已应用的状态，从而跳过 Renderer、Collider 或 Marker 的显式初始化。

修复方式：生命周期初始化显式应用 Initial 状态，不以枚举默认值代替“状态已同步”标记。该修复确保 Scene 初始、Configure 和重新启用时的表现与逻辑状态一致。

### 6.3 最终主要 EditMode 失败：测试夹具生命周期顺序

最终五项 EditMode 失败的共同根因位于 `SleepingForestRootBridgeConsequenceEditModeTests` 测试夹具：

```text
Consequence.Configure 时 Fixture 根对象 inactive
    ↓
isActiveAndEnabled == false
    ↓
Configure 不建立 Trial 实例事件订阅
    ↓
后续 Completed Revision 没有到达 Consumer
```

最终修复顺序：

```text
创建 Consequence
    ↓
激活 Fixture
    ↓
Configure
    ↓
建立实例事件订阅并读取 Initial Snapshot
    ↓
正常接收后续 Revision 事件
```

该问题属于 **003C EditMode 测试夹具生命周期**，不是 `ForestSignalTrial`、Revision、Beacon A/B 或 Interaction Foundation 缺陷。没有为测试通过而直接调用 Span 激活，也没有改写 003B 状态机。

## 7. 人工验收结果

正式 `SleepingForest` Scene 人工验收全部通过：

- ForestSignalTrial Completed 后 `RootBridgeAdvantageSpan` 正确激活；
- Main Route 永久保持可通行；
- Advantage Route 正常形成可选捷径收益；
- IncorrectOrder 不激活捷径，之后 A → B 可恢复并激活；
- 重复相同 Revision 不重复产生环境后果；
- `RootBridgeTemporaryCrossing` 未移动、禁用、替换或修改承载 Collider；
- 玩家位于桥附近激活环境后果时无卡死、掉落或位移异常；
- Renderer / Collider 状态同步正常；
- 无新的 Z-fighting；
- `MaterialPropertyBlock` 视觉隔离正常，无 shared Material 污染；
- FallRecovery、Movement、GroundDetector、Camera 正常；
- Beacon Detection、Prompt、Execution 与 Interaction 回归正常；
- Console Error = `0`。

## 8. 已执行的 Unity 验证步骤

1. 用 Unity `6000.3.21f1` 打开项目，等待编译完成，确认 Console 无 Error。
2. 执行 `Wonder Squad > Sprint003C > Apply Root Bridge Consequence`，确认保存对话框。
3. 在 SleepingForest Hierarchy 检查只有一个 `ForestSignalRouteConsequence`、一个 `RootBridgeAdvantageSpan`，且 `RootBridgeTemporaryCrossing` 的 Transform、Renderer 和 BoxCollider 未变。
4. 运行 `SleepingForestRootBridgeConsequenceEditModeTests`，再运行全部 EditMode 测试。
5. 运行 `SleepingForestRootBridgeConsequencePlayModeTests`，再运行全部 PlayMode 测试。
6. 在 SleepingForest Play Mode 进行 A → B：观察 Span 与方向 Marker 激活；走 Main Route 验证仍可通行；再走 Advantage Route 验证它是独立的空间选择。
7. 在 Main Bridge/Junction 附近完成 A → B，确认无位移、掉落、卡死、GroundDetector 抖动或 CharacterController 异常。
8. 验证 B → A → B、重复 Completed、FallRecovery、Movement、Camera、Prompt 和 Console Error `0`。

以上步骤已在 Unity `6000.3.21f1` 完成，结果已记录在第 5、7 节。

## 9. 已知限制与 Sprint003D 边界

- 003C 不触发 Slice End、Level Completed、Completion UI 或 Build Profile 接入。
- 003C 不实现网络、存档、角色能力、Inventory、通用 Puzzle/Bridge 框架或失败系统。
- Advantage Route 当前只表达《沉睡森林》本地 Vertical Slice 的可选捷径收益；角色能力、网络权威和跨场景状态不属于 003C。
- 当前环境后果仅是 Trial Snapshot 的本地派生表现；未来网络应同步 Trial 的权威 Snapshot/Revision，而不是同步 Visual 私有状态。

## 10. 最终结论

# PASSED / GO

Sprint003C 已通过专项测试、完整回归和正式场景人工验收。Root Bridge Consequence 保持只读、实例级、幂等且无软锁；003D Completion、Slice End Trigger 与 Completion UI 均未提前实现。
