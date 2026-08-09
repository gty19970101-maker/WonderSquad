# Sprint003D — Sleeping Forest Slice Completion Implementation Report

## 1. 当前状态

`PASSED / GO — VERTICAL SLICE COMPLETE`

- Unity 正式基线：`6000.3.21f1`
- 实现分支：`feature/sprint003-gameplay-vertical-slice`
- 前置基线：Sprint003A/B/C 均为 `PASSED / GO`
- 已在仅包含当前 `Assets`、`Packages`、`ProjectSettings` 的隔离临时工程中完成 Unity `6000.3.21f1` 编译、显式 Authoring、专项测试与完整回归。
- 正式 Unity Editor 已完成专项测试、完整回归、A—G 人工走图、Sprint003C 路线/Geometry 复验与 Console 验收。
- 正式结果为全部 Passed、`0 Failed`、`0 Skipped`、Console Error `0`；仓库没有保存本次 Test Runner XML，因此精确数量标记为 `TEST COUNT REQUIRES MANUAL RECORD`，不复用旧计数。

## 2. 实现结果

Sleeping Forest 现在具备以下场景局部闭环：

```text
ForestSignalTrial Completed
            +
正式 Player 位于 Slice End Trigger
            ↓
SleepingForest Slice Completed
            ↓
Completion Feedback + Completed Marker
```

只有两个条件同时成立才完成。Root Bridge Advantage 激活不是额外门槛；Main Route 与 Advantage Route 汇入同一个 Slice End Trigger。

## 3. Runtime 组件职责

### `SleepingForestSliceEndTrigger`

- 显式引用场景 `PlayerSpawner` 和 `BoxCollider`；
- 只接受 `PlayerSpawner.SpawnedPlayer` 根或子节点上的 Collider；
- 环境、Beacon、Bridge 或任意其他 Collider 无法改变 Presence；
- 暴露只读 `IsPlayerPresent` 和实例事件 `PlayerPresenceChanged`；
- 多 Collider Presence 通过实例集合处理，相同状态不重复发布；
- 不读取 Trial、不判断 Completion、不显示 UI、不移动或传送 Player。

### `SleepingForestCompletionController`

- 场景实例级持有 `NotCompleted → Completed`；
- 订阅 `ForestSignalTrial.TrialStateChanged` 后立即读取 `CurrentSnapshot`；
- 同时只读消费 `SleepingForestSliceEndTrigger` Presence；
- 无 `Update` 轮询、全局查找、static 可变状态或 Global Event Bus；
- Completion 不可逆，`CompletionRevision` 只从 `0` 增加到 `1`；
- 记录 `TrialRevisionAtCompletion`，不创建第二套 Trial Revision；
- 发布实例级 `CompletionChanged`、`Completed` 和 `FeedbackStateChanged`。

### Completion Snapshot 与反馈状态

- `SleepingForestCompletionSnapshot` 是 readonly value，仅包含 State、Completion Revision 和 Trial Revision at Completion；
- 不包含 MonoBehaviour、Collider、Renderer、Canvas 或可写 Unity Object；
- `SleepingForestCompletionFeedbackState` 仅表达 `Hidden / ConditionsUnmet / Completed`，不是第二套 Gameplay 状态机。

## 4. Completion 状态机

| Trial | PlayerAtEnd | Slice State | Feedback |
|---|---:|---|---|
| 未完成或 IncorrectOrder | false | NotCompleted | Hidden |
| 未完成或 IncorrectOrder | true | NotCompleted | ConditionsUnmet |
| Completed | false | NotCompleted | Hidden |
| Completed | true | Completed | Completed |

Completed 后离开或重复进入 Trigger 不会逆转或重复完成。Scene reload 创建新场景实例，重新从 NotCompleted 开始；本 Sprint 不实现跨 Scene 或 Save 持久化。

## 5. Trial Snapshot / Revision 生命周期

Controller 的启用顺序固定为：

```text
订阅 Trial 实例事件
→ 订阅 Trigger 实例事件
→ 读取 Trial.CurrentSnapshot
→ 读取 Trigger.IsPlayerPresent
→ 统一 EvaluateCompletion
```

- Controller 晚于 Trial 启用时仍能恢复当前 Completed Snapshot；
- IncorrectOrder Revision 只更新最后观察值，不会屏蔽后续 Completed Revision；
- 相同或旧 Revision 不会重复完成；
- Player 已在 End 时收到 Completed Revision，会立即完成，无需离开后重进；
- Controller 不读取 Trial 私有字段，也不修改 Trial、Beacon、Revision 或 Root Bridge。

## 6. PlayerAtEnd + Trial 双条件

双条件由 Controller 唯一判断：

- Trigger 只报告 Presence；
- Trial 只拥有 Forest Signal Trial 真相；
- Presenter 只读取 Controller；
- Completion View 不读取任何 Gameplay 对象。

未完成 Trial 提前进入 End 时，玩家只收到条件不足反馈，可以自由离开、完成 A → B 并返回。没有 Lock、失败状态、Scene reload、Trial reset、Player reset、Teleport 或路线关闭。

## 7. Requirement 与 Completion Feedback

Screen-space uGUI Prefab：

- `PF_SleepingForestCompletionFeedback`
- 初始隐藏；
- Graphic 与 Text 的 `raycastTarget = false`；
- 不创建 EventSystem，不阻挡 WASD/Interaction；
- 相同 ViewData 不重复刷新或创建 UI。

锁定文本边界：

| 状态 | Stable Key | Fallback |
|---|---|---|
| ConditionsUnmet | `sleeping_forest.completion.requirement_incomplete` | `Forest signal incomplete` |
| Completed | `sleeping_forest.completion.complete` | `Sleeping Forest Complete` |

World Marker 为无 Collider 的独立完成 Marker，不修改既有 Tower、Arch、Ground Material 或 Collider。当前不接入完整本地化、动画、音效、奖励或下一关按钮。

## 8. Scene Authoring

正式菜单：

```text
Wonder Squad > Sprint003D > Apply Sleeping Forest Completion
```

规则：

- 菜单显式执行，带覆盖说明和确认对话框；
- 先调用 Unity 保存确认，取消时不修改资产；
- 无 `InitializeOnLoad` 或自动入口；
- 先创建/复用 inactive `SleepingForestCompletion` 根；
- 用 `SerializedObject` 写完 Trial、Spawner、Trigger、Controller、View 和 Marker 引用，并执行 `ApplyModifiedPropertiesWithoutUndo`；
- 必要的 Prefab 实例引用使用 `RecordPrefabInstancePropertyModifications`；
- 最后统一启用 Hierarchy，避免 OnEnable 早于序列化配置；
- 重复 Apply 复用唯一根、Trigger、Controller、Canvas 和 Marker；发现重复对象时停止并报错，不静默生成第三套内容；
- 另有仅允许 `Application.isBatchMode` 的显式隔离验证入口，不会在交互 Editor 或项目启动时自动运行。

隔离 Unity 实际执行 Authoring 成功并保存 Scene。生成资产已回填正式工作区。

## 9. Scene 与 Serialized 引用

正式 `SleepingForest.unity` 新增：

```text
SleepingForestCompletion
├── SleepingForestCompletionController
├── SleepingForestSliceEndTrigger
├── SleepingForestCompletionMarkerView
│   └── SleepingForestCompletedMarker
└── SleepingForestCompletionCanvas
```

Trigger 配置：

- Position：`(0, 1.5, 54.75)`
- Size：`(16, 3, 2.5)`
- `isTrigger = true`

Scene 静态验证确认：

- 一个 Controller；
- 一个 Trigger；
- 一个 Completion Presenter/View/Marker；
- Trial、Spawner 和 View 引用完整；
- Trigger 覆盖 `SliceEndGround` 路线汇合区域；
- 既有 `SliceEndLandmark_Tower`、`SliceEndLandmark_Arch`、`SliceEndGround` 均保留。

## 10. Physics 与 Soft-lock 审计

- Trigger 没有 Renderer，不作为 Ground Collider，不阻挡 CharacterController；
- Completed Marker 没有 Collider；
- 没有新增双层 Ground、承载面或共面 Renderer，不引入 Z-fighting；
- 未修改 PlayerMovement、GroundDetector、Camera 或 FallRecovery；
- Main Route 与 Advantage Route 均继续物理连通；
- Completion 不移动、锁定或重置 Player；
- Trigger 附近跌落仍由已有 FallRecovery 处理，Recovery 后可再次进入 End。

正式 Editor 已确认 Trigger 不阻挡 CharacterController、GroundDetector 无抖动，FallRecovery 后能够继续完成切片。

## 11. 程序集与架构

新增内容程序集：

```text
WonderSquad.SleepingForest
├── WonderSquad.Core
├── WonderSquad.Player
└── WonderSquad.Puzzle
```

表现依赖：

```text
WonderSquad.UI → WonderSquad.SleepingForest
```

- SleepingForest Runtime 不引用 UI、Interaction、Camera、Network 或 Editor；
- Player、Puzzle、Interaction 和 Camera 不反向引用 SleepingForest；
- Editor 与 Tests 只在各自隔离程序集引用新增内容；
- 没有循环依赖或跨层写入。

## 12. 新增与修改文件

### 新增 Runtime

- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/WonderSquad.SleepingForest.asmdef`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestCompletionState.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestCompletionFeedbackState.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestCompletionSnapshot.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestSliceEndTrigger.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestCompletionController.cs`
- `Client/Assets/WonderSquad/Runtime/UI/SleepingForest/SleepingForestCompletionViewData.cs`
- `Client/Assets/WonderSquad/Runtime/UI/SleepingForest/SleepingForestCompletionPresenter.cs`
- `Client/Assets/WonderSquad/Runtime/UI/SleepingForest/SleepingForestCompletionView.cs`
- `Client/Assets/WonderSquad/Runtime/UI/SleepingForest/SleepingForestCompletionMarkerView.cs`

### 新增 Editor、Prefab 与 Tests

- `Client/Assets/WonderSquad/Editor/SleepingForestCompletionAuthoring.cs`
- `Client/Assets/WonderSquad/Prefabs/UI/PF_SleepingForestCompletionFeedback.prefab`
- `Client/Assets/WonderSquad/Tests/EditMode/SleepingForestCompletionEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/SleepingForestCompletionPlayModeTests.cs`

所有 Unity 文件均包含对应 `.meta`。

### 修改

- `Client/Assets/WonderSquad/Runtime/UI/WonderSquad.UI.asmdef`
- `Client/Assets/WonderSquad/Editor/WonderSquad.Editor.asmdef`
- `Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef`
- `Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef`
- `Client/Assets/WonderSquad/Tests/EditMode/SleepingForestGreyboxEditModeTests.cs`
- `Client/Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity`
- `CHANGELOG.md`

旧 Greybox 边界测试只做精确迁移：继续禁止 Sandbox、ExecutionProbe、标准 Probe、未知 Completion 与未批准 Gameplay，同时要求唯一正式 Completion Controller/Trigger/Presenter。

## 13. 实现期间问题与修复

### 测试标识符 namespace

首次隔离编译发现新测试缺少 `WonderSquad.Core.Identifiers`，导致 `PlayerId`/`RequestId` 无法解析。仅补充 using；Runtime/Editor 程序集无编译错误。

### Marker Authoring 的 Unity 伪 null

Authoring 使用 `GetComponent() ?? AddComponent()` 时，Unity 缺失组件对象的伪 null 不适用于 CLR `??`，导致 `MeshFilter.sharedMesh` 抛出 `MissingComponentException`。改为显式 Unity null 判断后再 `AddComponent`，隔离 Authoring 成功。

### EditMode Fixture 生命周期

纯 EditMode 激活 GameObject 不保证自动运行 `OnEnable`。最初 Fixture 未建立 Controller 订阅，Presence/Trial 事件无法到达 Consumer。测试夹具调整为按 Runtime 顺序显式调用 Trigger、Controller、View 与 Presenter 的 `OnEnable`；没有直接调用私有 Complete/Apply 绕过真实链路。

### PlayMode Exit 测试方式

禁用 CharacterController 后瞬移不会产生正常 `OnTriggerExit`。测试离开步骤改为使用已启用 CharacterController 的 `Move`，验证真实退出行为；生产 Trigger 和 PlayerMovement 未修改。

## 14. 自动化验证结果

Unity `6000.3.21f1` 隔离临时工程实测：

| 范围 | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Sprint003D EditMode | 13 | 0 | 0 |
| Sprint003D PlayMode | 4 | 0 | 0 |
| 完整 EditMode | 121 | 0 | 0 |
| 完整 PlayMode | 62 | 0 | 0 |

- Authoring BatchMode：成功，退出码 `0`；
- 编译：无 C# 错误；
- 完整 PlayMode 日志：未发现 `NullReferenceException`、`MissingReferenceException` 或未处理异常；
- `git diff --check`：最终静态审计要求为通过；
- 测试数量未预设，以上为 Unity Test Runner 实际发现数量。

这些计数来自清理前的隔离副本，仅保留为实现阶段历史记录，不作为最终正式 Test Runner 计数。

### 正式 Unity Test Runner 最终结果

Unity `6000.3.21f1` 正式 Editor 已确认：

| 范围 | 结果 | Failed | Skipped | 精确数量 |
|---|---|---:|---:|---|
| Sprint003C 专项 EditMode | 全部 Passed | 0 | 0 | `TEST COUNT REQUIRES MANUAL RECORD` |
| Sprint003C 专项 PlayMode | 全部 Passed | 0 | 0 | `TEST COUNT REQUIRES MANUAL RECORD` |
| Sprint003D 专项 EditMode | 全部 Passed | 0 | 0 | `TEST COUNT REQUIRES MANUAL RECORD` |
| Sprint003D 专项 PlayMode | 全部 Passed | 0 | 0 | `TEST COUNT REQUIRES MANUAL RECORD` |
| 完整 EditMode | 全部 Passed | 0 | 0 | `TEST COUNT REQUIRES MANUAL RECORD` |
| 完整 PlayMode | 全部 Passed | 0 | 0 | `TEST COUNT REQUIRES MANUAL RECORD` |
| Console Error | `0` | — | — | — |

仓库 `Client/TestResults` 只有 2026-08-01 的旧结果，没有本次正式 Test Runner XML；因此未沿用 `122/63`、`121/62` 或更早总数冒充最终结果。

## 15. 正式 Unity 人工验收结果

| 流程 | 正式验收结果 |
|---|---|
| A：A → B → Main Route → Slice End | Trial Completed；永久 Main Route 可完成；Completion Feedback 正常显示。 |
| B：A → B → Advantage Route → Slice End | Shortcut 明显更直接；与 Main Route 使用同一 Completion 条件。 |
| C：未完成 Trial → End → Requirement → 离开 → A → B → 返回 | 第一次不完成并显示 Requirement；玩家可离开；之后正常完成。 |
| D：Player 已在 End 内 → Trial Completed | 无需退出重进，立即完成。 |
| E：B → IncorrectOrder → A → B → End | IncorrectOrder 可恢复，不造成永久失败。 |
| F：Fall → FallRecovery → 继续 → Completion | 恢复后 Movement、GroundDetector、Camera 和 Completion 正常。 |
| G：Completed → 离开 → 再进入 | Completed、事件和 UI 均不重复。 |

额外确认：只有一个正式 Slice End Trigger；`isTrigger = true`；只认可正式 Player；Requirement/Completion Feedback 不阻挡输入；Scene reload 后恢复 NotCompleted/隐藏反馈；Console Error 为 `0`。

### Sprint003C 路线与可见 Geometry 闭环

Sprint003D 人工验收期间发现并关闭两轮内容问题：

1. 第一轮：旧 Advantage Span 与 Main Route 过度重合/并排，技术上有两条路但缺少可感知收益。最终 Main Route 调整为约 `46.24m` 三段绕行，Advantage Route 为 `27.00m` 中央直连，跳过外侧长段和两次转向。
2. 第二轮：绿色捷径两侧仍有两个悬空棕色长条。精确定位为 `SleepingForestRoot/Landmarks/RootBridgeLandmark_LeftRoot` 与 `RootBridgeLandmark_RightRoot`，来源为 003A Builder 的 `CreateRootBridgeLandmark`，不是 Guardrail，也没有 Gameplay 职责。

最终已从 Scene、003A Builder 创建源和 003C Authoring 生命周期完整清理 Renderer、MeshFilter、BoxCollider；中央 Shortcut Corridor 审计测试防止回归，重复 Apply 不会恢复旧对象。该修复没有修改 ForestSignalTrial、Beacon、Character/Interaction/Camera Foundation、FallRecovery 或 Completion 条件。

## 16. 已知限制与后续边界

- 本 Sprint 不实现 Reward、Save、Network、Achievement、下一关、Scene Transition 或 Build 入口；
- Completion 只在当前 Scene 实例内保存；
- 当前只验证本地单 Player，未来网络由 Host Authority 确认 Trial 与 Presence；
- UI 使用英文 fallback 与稳定 key，未接完整本地化；
- 不暂停游戏、不锁定输入，也没有动画、音效或计时消失；
- Sprint003D 之后才评估 Build/Release 入口，不自动开始 Sprint004。

## 17. Vertical Slice 完成定义

《沉睡森林》第一可玩切片现已具备：

```text
进入 → 探索 → Beacon Trial → 环境变化
→ Main/Advantage Route → Slice End → 明确完成反馈
```

代码、Scene Authoring、Prefab、测试、正式走图与文档均已完成；最终状态：

# SPRINT003D — PASSED / GO

# SLEEPING FOREST GAMEPLAY VERTICAL SLICE — COMPLETE
