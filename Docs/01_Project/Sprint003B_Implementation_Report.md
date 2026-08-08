# Sprint003B — Forest Signal Trial Implementation Report

## 1. 当前状态

`PASSED / GO`

### 2026-08-09：Unity 验证定向修复

PlayMode 错误定位为 Sprint003B Trial 的早期 Scene 生命周期校验，而非 Player、Camera 或 FallRecovery。有效的序列化 Scene 引用曾在 `Scene.isLoaded` 变为 true 前被拒绝；现在同一有效 Scene 的引用可在初始化期通过，同时保持状态机、Revision、Snapshot 和来源校验语义不变。

Authoring 命令现会在专用 Gameplay Root 保持 inactive 时完成并持久化 Trial/Beacon 引用、校验组合，再启用对象。EditMode fixture 在 Trial 有效后显式配置 Visual，因此 Dormant 断言不再依赖 EditMode 消息时序。

Puzzle、Editor、EditMode Tests 和 PlayMode Tests 已完成静态编译（0 error）。Unity `6000.3.21f1` 已完成 Test Runner 与人工验收：Sprint003B EditMode `18/18`、Sprint003B PlayMode `4/4`、完整 EditMode `100/100`、完整 PlayMode `54/54`，均为 Passed，Console Error 为 `0`。详见 `Sprint003B_Unity_Failure_Audit.md`。

Sprint003B 的运行时状态、Interaction 适配、实例视觉、Editor authoring 和测试代码已完成。由于正式项目当前被 Unity Editor 占用，且隔离 batch Editor 的 Licensing Client 无法保持连接，本轮没有手工编辑 Unity YAML，也没有把未完成的隔离资产回拷到正式工程。

在当前 Unity `6000.3.21f1` 完成脚本导入后，必须显式执行：

```text
Wonder Squad → Sprint003B → Apply Forest Signal Trial
```

该命令会显示确认框、调用 Unity 官方未保存 Scene 处理流程，并创建/更新 Definition、Prompt、`PF_ForestBeacon` 与正式 SleepingForest Scene 组合。命令不会自动运行。

## 2. 实现范围

已实现：

- 固定 A → B 的 Forest Signal Trial；
- B 先执行时的 IncorrectOrder 与可恢复流程；
- Activated 单向终态与重复交互保护；
- Scene 实际 Beacon 组件、Slot、TargetId 和 Scene 归属联合校验；
- 不可变 Beacon Runtime State 与 Trial Snapshot；
- 确定性 `uint Revision`；
- 实例级 `TrialStateChanged`；
- Beacon A/B 独立的检测、执行和视觉映射；
- MaterialPropertyBlock 颜色覆盖与 Marker 非颜色反馈；
- 只读 003C 接入边界；
- 003A 过时 InteractionTarget 断言的精确调整；
- 专项 EditMode/PlayMode 测试源码；
- 显式、可重复执行、不会自动触发的 Sprint003B Unity authoring 命令。

未实现：Root Bridge 后果、路线开关、Slice Completion、Puzzle Framework、Ability、Inventory、Network、Save 或 Sprint003C。

## 3. 新增文件

### Content

- `Client/Assets/WonderSquad/Runtime/Content/SleepingForest/ForestBeaconSlot.cs`
- `Client/Assets/WonderSquad/Runtime/Content/SleepingForest/ForestBeaconDefinition.cs`
- `Client/Assets/WonderSquad/Runtime/Content/SleepingForest/ForestSignalTrialDefinition.cs`

### Puzzle Runtime

- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestBeaconLogicalState.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestSignalTrialPhase.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestSignalTrialOutcome.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestBeaconRuntimeState.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestSignalTrialSnapshot.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestSignalTrial.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestBeaconVisualMode.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestBeaconInteraction.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestBeaconVisual.cs`

### Editor 与测试

- `Client/Assets/WonderSquad/Editor/SleepingForestSignalTrialAuthoring.cs`
- `Client/Assets/WonderSquad/Tests/EditMode/ForestSignalTrialEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/EditMode/ForestBeaconConfigurationEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/ForestSignalTrialPlayModeTests.cs`

以上新增脚本和目录均包含对应 `.meta`。

## 4. 修改文件

- `Client/Assets/WonderSquad/Editor/WonderSquad.Editor.asmdef`
- `Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef`
- `Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef`
- `Client/Assets/WonderSquad/Tests/EditMode/SleepingForestGreyboxEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/SleepingForestGreyboxPlayModeTests.cs`
- `CHANGELOG.md`
- `Docs/01_Project/Sprint003B_Preflight_Report.md`
- `Docs/01_Project/Sprint003B_Implementation_Plan.md`

Unity 菜单执行后预期新增/修改：

- `Client/Assets/WonderSquad/ScriptableObjects/Content/SleepingForest/ForestSignalTrialDefinition.asset`
- `Client/Assets/WonderSquad/ScriptableObjects/Content/Interaction/InteractionPrompt_ForestBeacon.asset`
- `Client/Assets/WonderSquad/Prefabs/SleepingForest/PF_ForestBeacon.prefab`
- `Client/Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity`
- 对应 Unity `.meta` 文件。

## 5. 程序集变化

生产依赖保持：

```text
WonderSquad.Core
├─ WonderSquad.Interaction
└─ WonderSquad.Content
   └─ WonderSquad.Puzzle
```

- `WonderSquad.Puzzle` 仍只引用 Core/Content；没有引用 Interaction、Player、UI 或 Network。
- `WonderSquad.Interaction` 没有引用 Puzzle。
- `WonderSquad.Editor` 增加 Content、Interaction、Puzzle 引用，只用于显式内容 authoring。
- EditMode/PlayMode 测试程序集增加 Content/Puzzle 引用。
- 没有新增生产 asmdef，没有循环或领域反向依赖。

## 6. ScriptableObject Definition

`ForestSignalTrialDefinition` 只保存：

- Trial stable ID；
- schema version；
- Beacon A Definition；
- Beacon B Definition；
- A/B 的稳定 Slot 与 TargetId。

默认内容：

- Trial：`sleeping_forest.forest_signal_trial`
- A：`sleeping_forest.beacon.a`
- B：`sleeping_forest.beacon.b`

Definition 不保存运行时 Phase、Outcome、Revision、Request 或 Scene 引用。运行时代码只读 Definition；自动测试覆盖执行前后 Definition 值不变。

## 7. 状态机

| 当前状态 | 请求 | 结果 | 新状态 | Revision |
| --- | --- | --- | --- | ---: |
| A/B Dormant，AwaitingFirst | A | Success | A Activated，AwaitingSecond | +1 |
| A/B Dormant，AwaitingFirst | B | Success + IncorrectOrder | A/B 仍 Dormant，仍 AwaitingFirst | +1 |
| 错序后 AwaitingFirst | A | Success | A Activated，B 恢复 Dormant 表现 | +1 |
| A Activated/B Dormant，AwaitingSecond | B | Success | A/B Activated，Completed | +1 |
| Activated 或 Completed | 重复/陈旧请求 | Busy 或 Executor 的 TargetInvalid | 不变 | 不变 |
| 未配置、伪造或跨 Scene source | 请求 | TargetInvalid | 不变 | 不变 |
| 相同 PlayerId + RequestId | Request Port 重放 | 缓存 Result | 不变 | 不变 |

Activated 不存在返回 Dormant 的转换。IncorrectOrder 不激活 B、不重载 Scene、不形成软锁；玩家仍可执行 A → B。

## 8. Snapshot、Revision 与事件

`ForestSignalTrialSnapshot` 为只读值类型，包含：

- Phase；
- Beacon A Runtime State；
- Beacon B Runtime State；
- LastAttemptedTargetId / Slot；
- LastOutcome；
- Revision；
- 派生 `HasIncorrectOrder` / `IsCompleted`。

Revision 从 0 开始，只在新的合法 Trial 尝试提交结果时递增。初始化、重复 RequestId、Foundation 验证失败、未知 source、Activated 重复请求、组件启停与视觉刷新不递增。

`TrialStateChanged(snapshot)` 是 `ForestSignalTrial` 实例事件：提交新 Revision 后发送一次，无订阅者安全；组件启用时采用“订阅并读取 CurrentSnapshot”同步初态。

## 9. Beacon 来源验证

Trial 不提供仅以任意 TargetId 推进的公共写入口。请求必须来自 Inspector 中显式配置的 `ForestBeaconInteraction` A 或 B，且同时满足：

- source 引用与注册实例相同；
- source 与 Trial 位于同一已加载 Scene；
- Slot 与 Definition 一致；
- TargetId 与 Definition 一致；
- 当前逻辑状态允许执行。

未知第三个 Beacon 即使伪装成 A/B Slot，也不能推进 Trial。

## 10. Prefab 结构

显式 authoring 命令创建：

```text
PF_ForestBeacon (Interactable Layer)
├─ InteractionTarget
├─ InteractionPromptSource
├─ ForestBeaconInteraction
├─ ForestBeaconVisual
├─ DetectionAnchor
├─ DetectionVolume (Sphere Trigger)
├─ BeaconBody
├─ DormantMarker
├─ ActivatedRing
└─ IncorrectMarker
   ├─ IncorrectBar_A
   └─ IncorrectBar_B
```

Scene 的 A/B 实例分别覆盖稳定 TargetId、Slot 与 Trial 引用。Prompt 共用只读 Definition；执行、Target、Renderer、Marker 和 MaterialPropertyBlock 均为实例所有。

## 11. Beacon 视觉

- Dormant：蓝绿色 PropertyBlock + Dormant Marker；
- Activated：绿色 PropertyBlock + Activated Ring；
- Incorrect：红色 PropertyBlock + X 形 Marker；
- 不访问 `Renderer.material`；
- 不写入 `sharedMaterial`；
- 不使用 Animator、Tween、Particle 或正式 VFX；
- 相同 Revision 不重复刷新 Renderer。

颜色与几何 Marker 同时表达状态，满足不能只依赖颜色的要求。

## 12. Scene 修改策略

authoring 命令会：

1. 使用 Unity 官方保存确认流程保护当前未保存 Scene；
2. 创建/更新 Definition、Prompt 和 Prefab；
3. 打开正式 SleepingForest；
4. 读取 003A 预留 Beacon 位置；
5. 用 `ForestBeacon_A` / `ForestBeacon_B` 正式实例替换 Reserved Visual/Anchor；
6. 保留 `BeaconA_Reserved_Base` / `BeaconB_Reserved_Base`、路线、Root Bridge Placeholder、Slice End 和 FallRecovery；
7. 创建唯一 `ForestSignalTrialGameplay/ForestSignalTrial`；
8. 保存 Scene。

命令不自动执行，不使用名称决定运行时 A/B；名称只用于一次性 Editor authoring 定位 003A 已知预留对象。运行时身份由显式实例引用、Slot 与稳定 TargetId 决定。

## 13. Sprint003A 测试调整

原“SleepingForest 不包含任何 InteractionTarget”断言改为：

- 恰好允许两个正式 Target；
- ID 必须为 Beacon A/B；
- 必须有唯一 Trial 与两个 ForestBeaconInteraction；
- 继续禁止 P0/Sandbox、InteractionExecutionProbe、InteractionProbeBehaviour；
- 继续禁止 RootBridgeGameplay 与 CompletionGameplay；
- 保留单 Player、单 Main Camera、FallRecovery 和几何回归。

没有删除或整体放宽原边界测试。

## 14. 验证结果

### 独立静态编译

使用 Unity `6000.3.21f1` 的 Managed 程序集引用执行：

| 范围 | Warning | Error | 结果 |
| --- | ---: | ---: | --- |
| Content Runtime | 0 | 0 | 通过 |
| Puzzle Runtime | 0 | 0 | 通过 |
| Sprint003B Editor Authoring | 0 | 0 | 通过 |
| 新增/修改测试源码 | 0 | 0 | 通过 |

### Unity Test Runner

- Sprint003B EditMode：`UNITY VERIFICATION REQUIRED`
- 完整 EditMode：`UNITY VERIFICATION REQUIRED`
- Sprint003B PlayMode：`UNITY VERIFICATION REQUIRED`
- 完整 PlayMode：`UNITY VERIFICATION REQUIRED`

未宣称 Test Runner 已通过。隔离 batch Editor 因本机 Licensing Client 重连失败被安全终止，没有回拷任何未完成资产。

## 15. Unity 验证步骤

1. 回到当前正式 Unity Editor，等待 Console 编译完成；
2. 确认 Console 没有编译错误；
3. 执行 `Wonder Squad → Sprint003B → Apply Forest Signal Trial`；
4. 在确认框选择 Apply；如有未保存 Scene，按 Unity 官方提示保存或取消；
5. 检查生成的 Definition、Prompt、Prefab 和 SleepingForest Scene 引用；
6. 运行 `ForestSignalTrialEditModeTests` 与 `ForestBeaconConfigurationEditModeTests`；
7. 运行完整 EditMode；
8. 运行 `ForestSignalTrialPlayModeTests`；
9. 运行完整 PlayMode；
10. 按本文第 16 节人工验收；
11. 记录真实 Passed/Failed/Skipped 与 Console 结果后再进入 Gate。

## 16. 人工验收

- A → B 正确完成；
- B → A 首步产生 IncorrectOrder，B 不激活；
- 错序后仍能通过 A → B 完成；
- 已激活 Beacon 重复按键不推进、不回到 Dormant；
- 长按只执行一次；
- A/B 颜色与 Marker 不互相污染；
- Prompt 正常出现和隐藏；
- Movement、Camera、FallRecovery 正常；
- Main/Advantage Route 仍可物理通行；
- Root Bridge、路线和 Slice End 无 Gameplay 变化；
- Console Error = 0；
- 稳定状态无持续 Beacon GC.Alloc。

## 17. 已知限制

- 当前只实现本地单机 Scene 状态；没有 Host Authority 或网络复制。
- 重新进入 Scene 会从 Revision 0 重置；没有 Save/Persistence。
- IncorrectOrder 只有最小颜色与 X Marker 反馈，无动画、音效或 VFX。
- Trial Completed 仅为只读状态，不改变环境；003C 才能消费 Snapshot/Event。
- 正式 Unity 资产尚需通过显式 authoring 菜单生成并由 Test Runner 验证。

## 18. 003C 只读接入点

003C 只允许：

- 显式引用同 Scene 的 `ForestSignalTrial`；
- 读取 `CurrentSnapshot`；
- 订阅实例级 `TrialStateChanged`；
- 使用 `IsCompleted`、`HasIncorrectOrder`、A/B state 与 Revision 幂等处理环境后果。

003C 不得读取 Trial 私有字段，也不得让环境对象反向推进 Trial。

## 19. 范围审计

未修改 InteractionDetector、InteractionValidator、Prompt Presenter/View、InteractionExecutor、InteractionInputReader、Core Interaction 契约、PlayerSpawner、PlayerInputReader、PlayerMovement、GroundDetector 或 Camera Foundation。

未实现 Root Bridge Gameplay、路线开启/封锁、Slice Completion、Puzzle Framework、Inventory、Ability、Network、Save、Quest、Audio、Animation Framework 或正式 VFX。

## 20. 最终状态

# PASSED / GO

最终 Unity 验证和人工验收均已完成。`ForestSignalTrial Completed` 仅表示 A → B 的信标试炼完成；它不等于整个 SleepingForest Slice 完成。Root Bridge 环境后果与路线意义属于 Sprint003C，Slice End Trigger 和 Completion UI 属于 Sprint003D。因此 A → B 后走到终点尚无完成提示是已批准的范围边界，不是 Sprint003B 缺陷。

停止于 Sprint003B，不开始 Sprint003C。
