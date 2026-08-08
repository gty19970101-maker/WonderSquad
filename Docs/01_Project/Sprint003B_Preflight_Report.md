# Sprint003B — Forest Beacon Interaction State Preflight Report

> 实施回填（2026-08-08）：Preflight 的 `GO` 决策已转入实现。Runtime、Editor authoring 与自动化测试代码已完成静态编译检查；正式 Unity Editor 仍需执行显式菜单接入资产并运行 Test Runner。当前交付状态为 `IMPLEMENTED — UNITY VERIFICATION REQUIRED`，Preflight 原始结论与范围约束保持不变。

## 1. 最终结论

# GO

Sprint003B 可以进入独立的 Implementation Plan 阶段。

现有 Sprint002A–D Interaction Foundation 与已通过 Gate 的 Sprint003A SleepingForest Greybox 已提供实现两处森林信标所需的全部基础边界。无需修改 Character Foundation、Interaction Foundation 或现有 Core Interaction 契约，也无需预先实现 Root Bridge 后果、Puzzle Framework、Inventory、Ability 或 Network。

本 Preflight 只完成设计与架构审计，没有修改业务代码、Scene、Prefab、ScriptableObject、Input Actions 或其他 Unity 资产，也没有开始 Sprint003B 实现。

## 2. 审查基线

- Unity：`6000.3.21f1`
- 当前分支：`feature/sprint003-gameplay-vertical-slice`
- Sprint002A–D Interaction Foundation：`PASSED / GO`
- Sprint003A SleepingForest Greybox：`PASSED / GO`
- Sprint003A 自动化基线：EditMode `82/82`、PlayMode `50/50`
- 正式场景：`Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity`
- 现有 Beacon 预留对象：
  - `BeaconA_Reserved`
  - `BeaconB_Reserved`
  - `BeaconA_Reserved_InteractionAnchor_Reserved`
  - `BeaconB_Reserved_InteractionAnchor_Reserved`
- 现有 `WonderSquad.Puzzle` 程序集为空实现壳，只依赖 `WonderSquad.Core` 与 `WonderSquad.Content`。

## 3. Sprint003B 唯一目标

在正式 SleepingForest Scene 中建立两个可交互森林信标，使既有链路能够产生明确、有限、可测试的本地关卡状态：

```text
InteractionDetector
→ InteractionPromptPresenter / View
→ InteractionExecutor
→ LocalInteractionRequestPort
→ ForestBeaconInteraction
→ ForestSignalTrial
→ ForestBeaconVisual
```

Sprint003B 验证的是：

- Beacon 可以被现有 Detector 发现；
- 现有 Prompt 可以显示“校准/激活信标”；
- 现有 Executor 可以重新验证并执行；
- 两个 Beacon 形成有限顺序状态；
- 正确、错误及重复交互有确定结果；
- 每个 Beacon 有独立且清晰的本地视觉状态。

Sprint003B 不让信标改变 Root Bridge、路线 Collider、关卡完成或其他环境对象。环境后果属于 Sprint003C。

## 4. 现有 Interaction Foundation 适配审计

### 4.1 `IInteractable`

`IInteractable` 已满足 Beacon 检测需求：

- `TargetId`：为 Beacon A/B 提供唯一稳定身份；
- `DetectionPosition`：使用 003A 预留 Anchor；
- `IsDetectionEnabled`：内容组件可通过显式引用的 Target `MonoBehaviour.enabled` 状态，使已激活目标退出正常检测链；
- `DetectionPriority`：维持多目标选择稳定性。

不需要向该契约加入 Beacon 状态、顺序、Prompt 或执行方法。

### 4.2 `IExecutableInteraction`

`ForestBeaconInteraction` 可直接实现现有执行契约：

- `TargetId` 与同 Prefab 的 `IInteractable.TargetId` 保持一致；
- `IsExecutionAvailable` 从 `ForestSignalTrial` 当前快照查询；
- `Execute(context)` 只把已验证请求提交给本 Scene 的 Trial；
- 返回现有结构化 `InteractionResult`。

不修改 `IExecutableInteraction`、`InteractionContext`、`InteractionRequest`、`InteractionResult` 或 `InteractionResultCode`。

### 4.3 Prompt

- 继续使用现有 `InteractionPromptSource` 与 `InteractionPromptDefinition`。
- Beacon A/B 可共享一个只读静态 Prompt Definition，因为动作语义相同。
- 建议稳定 Prompt ID：`sleeping_forest.beacon.activate`。
- 建议本地化键：`interaction.sleeping_forest.beacon.activate`。
- MVP 回退文本可使用“校准信标”。
- 已激活 Beacon 退出检测后 Prompt 隐藏；不为动态“已激活”文本修改 Presenter 或 View。

### 4.4 Detector 与 Executor

- Detector 仍只发现目标，不读取 Trial 状态或改变 Beacon。
- Executor 仍使用当前稳定目标，不查找具体 Beacon 类型。
- Executor 在执行前继续验证目标存活、TargetId、Layer、距离和遮挡。
- `LocalInteractionRequestPort` 继续负责当前单机 RequestId 去重。
- Beacon、Trial、Visual 不反向引用 Detector、Executor、Prompt 或 UI。

结论：现有 Interaction Foundation 无需修改。

## 5. 组件组合方案

Sprint003B 应沿用 Sprint002D 已验证的组合模式，但不得把诊断 Probe 行为复制为正式 Gameplay 规则，也不得建立通用交互基类。

```text
PF_ForestBeacon
├── InteractionTarget                    现有；只读检测身份
├── InteractionPromptSource              现有；静态提示来源
├── ForestBeaconInteraction              新增；IExecutableInteraction 适配
├── ForestBeaconVisual                   新增；只读视觉映射
├── Trigger Collider (Interactable Layer)现有检测方式
├── DetectionAnchor                      显式锚点
└── Visual
    ├── DormantMarker
    ├── ActivatedMarker
    └── IncorrectMarker
```

禁止新增：

- `InteractionObject`
- `BaseInteractable`
- `BasePuzzle`
- `GenericPuzzleObject`
- 通用状态机
- 全局 EventBus
- Service Locator
- 全局 Gameplay Manager

`InteractionExecutionProbe` 继续只作为 Sandbox 诊断夹具；`PF_InteractionProbe` 只提供组合方式参考，不作为森林信标父类或运行时依赖。

## 6. Beacon 与 Trial 状态模型

### 6.1 静态定义

建议新增内容专用 `ForestSignalTrialDefinition`，只保存静态、只读配置：

- Schema Version；
- 稳定 Trial ID；
- First Beacon TargetId；
- Second Beacon TargetId。

003B 固定顺序为 Beacon A → Beacon B。定义必须验证：

- 两个 ID 均有效；
- 两个 ID 不相同；
- 与 Scene 中两个 `InteractionTarget` 完全匹配；
- 不包含 Root Bridge、路线 Collider、完成 Trigger、能力或网络字段。

该 Definition 位于 `WonderSquad.Content`，不能保存运行时激活状态、最后请求、Revision 或 Scene 引用。

### 6.2 运行时逻辑状态

运行时状态由 Scene 内唯一 `ForestSignalTrial` 实例持有。建议使用以下有限状态：

```text
ForestSignalTrialPhase
- Unknown
- AwaitingFirstBeacon
- AwaitingSecondBeacon
- Completed

ForestBeaconLogicalState
- Unknown
- Dormant
- Activated

ForestSignalTrialOutcome
- None
- FirstBeaconActivated
- IncorrectOrder
- TrialCompleted
```

建议不可变只读快照至少包含：

- Trial Phase；
- Beacon A Logical State；
- Beacon B Logical State；
- Last Attempted TargetId；
- Last Outcome；
- Revision。

`Revision` 只在一次新的、已接受的 Trial 尝试产生结果时递增。被 Executor 拒绝、重复 RequestId 或已激活目标的陈旧请求不得递增 Revision。

### 6.3 状态转换

| 当前状态 | 交互 | InteractionResult | 新逻辑状态 | 视觉结果 |
| --- | --- | --- | --- | --- |
| A Dormant、B Dormant、AwaitingFirst | A | `Success` | A Activated；AwaitingSecond | A 显示 Activated；B 保持 Dormant |
| A Dormant、B Dormant、AwaitingFirst | B | `Success` | 两者仍 Dormant；仍 AwaitingFirst；Revision +1 | B 显示 Incorrect |
| A Activated、B Dormant、AwaitingSecond | B | `Success` | A/B Activated；Completed | 两者显示 Activated |
| A Activated | 再次请求 A | 正常输入链无当前目标；陈旧请求经 Executor 为 `TargetInvalid` | 不变 | 不变 |
| Completed | 请求任一 Beacon | 正常输入链无当前目标；陈旧请求经 Executor 为 `TargetInvalid` | 不变 | 不变 |
| 任意 | 同 PlayerId + RequestId 重放 | 返回缓存结果 | 不变；Revision 不变 | 不重复刷新 |

错误顺序返回 `InteractionResultCode.Success`，含义是“请求被合法执行并产生了 Trial 结果”，不表示 Trial 已完成。`IncorrectOrder` 是内容领域结果，由 Trial Snapshot 表达；不能为了增加 `WrongOrder` 修改 Core 的通用 Result Enum。

错误交互不激活 B、不重置 Scene、不影响 Player，也不改变路线。B 的 Incorrect 表现保持到下一次有效 Trial 状态变化；当 A 被正确激活时，B 恢复 Dormant 表现并继续可交互。

## 7. 重复交互规则

必须区分三类“重复”：

1. **长按产生的重复帧**：由现有 `PlayerInteractionInputReader` 按下边沿规则阻止，只执行一次。
2. **相同 RequestId 重放**：由当前 `LocalInteractionRequestPort` 返回缓存结果，不再次调用 Beacon 或递增 Revision。
3. **玩家松开后再次按下**：这是新的请求。若目标仍合法，例如 B 在错误顺序后仍 Dormant，则产生新的 IncorrectOrder；已 Activated 的目标不再被 Detector 选中。绕过 Executor 直接调用 Request Port 的隔离测试应得到 `Busy`，同样不能改变状态。

Beacon 不复制 RequestId 缓存，不在内容组件中建立第二套去重系统。未来 Network Adapter 的乱序/重放窗口仍由 Host Authority 请求端口负责。

不需要冷却、长按计时或持续交互。

## 8. 两个 Beacon 的独立性

Beacon A 与 Beacon B 必须是两个独立 Gameplay 实例：

- 使用不同稳定 TargetId：建议 `sleeping_forest.beacon.a` 与 `sleeping_forest.beacon.b`；
- 各自拥有 InteractionTarget、Collider、Interaction Adapter、Visual 和 MaterialPropertyBlock；
- 各自从 Trial Snapshot 读取自身 Logical State；
- A 激活不能直接把 B 设为 Activated；
- B 的 Incorrect 表现不能污染 A 的 Material 或 Visual；
- 销毁或禁用一个 Beacon 不得让另一个产生空引用循环。

二者并非两个独立 Trial。顺序合法性、完成与 Revision 由同一个 `ForestSignalTrial` 协调，否则两个组件各自保存顺序会产生分叉状态。

两个实例可以安全共享只读 Prompt Definition 和静态材质资产，但不得共享可变运行时状态或修改 `sharedMaterial`。

## 9. 状态所有权

| 数据 | 所有者 | 生命周期 |
| --- | --- | --- |
| Trial ID、A/B 顺序 | `ForestSignalTrialDefinition` | 静态内容资产，只读 |
| Trial Phase、两 Beacon Logical State、Outcome、Revision | Scene 内 `ForestSignalTrial` | 当前 SleepingForest Scene；重载时重置 |
| TargetId、Anchor、检测优先级 | 每个 `InteractionTarget` | Prefab/Scene 配置，只读检测数据 |
| 交互适配 | 每个 `ForestBeaconInteraction` | 不拥有 Trial 真值，只转发和查询 |
| 当前显示状态与 PropertyBlock 缓存 | 每个 `ForestBeaconVisual` | 本地表现；可从 Snapshot 重建 |
| Prompt 文本键与回退文本 | `InteractionPromptDefinition` | 静态只读资产 |

运行时状态不能写入 ScriptableObject，不能放入静态字段或 `DontDestroyOnLoad` 对象，也不能由两个 Visual 或 Interaction Adapter 分别维护副本。

## 10. 视觉反馈边界

`ForestBeaconVisual` 只把已确认 Snapshot 映射为三种显示：

- `Dormant`：中性材质覆盖 + Dormant 形状标记；
- `Activated`：激活材质覆盖 + 明确的环/顶部标记；
- `Incorrect`：错误材质覆盖 + 与 Activated 不同的叉形或倾斜标记。

要求：

- 使用每 Renderer 的 `MaterialPropertyBlock`，不访问 `Renderer.material`，不写 `sharedMaterial`；
- 反馈不能只依赖颜色，必须同时改变可见形状或标记；
- 不使用 Animator、Tween、粒子、音效、复杂 Shader 或 UI 动画；
- Visual 不决定状态、不调用 Trial 写方法、不调用 Executor；
- 相同 Snapshot/Revision 不重复刷新 Renderer；
- 每个 Beacon 的 Visual 和 PropertyBlock 独立。

003B 只允许信标自身的局部视觉变化。Root Bridge、路线标记、终点、全局 Lighting 与环境 Collider 不得响应 Trial 状态。

## 11. 程序集与依赖方向

推荐保持现有程序集，不新增 asmdef：

```text
WonderSquad.Core
        ▲
        ├──────── WonderSquad.Interaction
        └──────── WonderSquad.Content
                         ▲
WonderSquad.Core ────────┴──────── WonderSquad.Puzzle

WonderSquad.UI → Core / Interaction / Player
```

- `ForestSignalTrialDefinition`：`WonderSquad.Content`。
- `ForestSignalTrial`、`ForestBeaconInteraction`、`ForestBeaconVisual` 及内容专用状态/快照：`WonderSquad.Puzzle`。
- `WonderSquad.Puzzle` 继续只引用 Core/Content，不引用 Interaction、Player、UI、Network 或 SleepingForest Greybox 实现。
- Prefab 可以在 Unity 资产层组合 Puzzle 组件与现有 Interaction 组件；这不构成 Puzzle 程序集对 Interaction 的代码依赖。
- `ForestBeaconInteraction` 使用显式 `MonoBehaviour` 引用并校验其实现 `IInteractable`，不得引用具体 `InteractionTarget` 类型。
- Interaction 不引用 Forest Beacon 或 Puzzle。

允许测试程序集在 003B 实施时增加 `WonderSquad.Puzzle`/`WonderSquad.Content` 测试引用；生产程序集方向不得改变。

## 12. Scene 与 Prefab 接入

实施阶段建议新增组合式 `PF_ForestBeacon`，并在现有 A/B 预留位置生成两个实例。每个实例必须配置：

- Interactable Layer；
- Trigger Collider；
- 唯一 TargetId；
- 003A 预留 Detection Anchor；
- 共享的 Forest Beacon Prompt Definition；
- 同 Scene `ForestSignalTrial` 显式引用；
- 各自的 Beacon Slot/TargetId；
- 各自独立的 Visual Renderer 与形状标记。

`ForestBeaconInteraction` 应在 Trial Snapshot 变化后同步其显式 Target `MonoBehaviour` 的启用状态：Dormant/Incorrect 保持检测，Activated 禁止检测。它不得修改 `InteractionTarget` 的生产实现，也不得按名称查找 Target。

不得让 Prefab 使用 GameObject 名称、Tag、`FindObjectOfType`、静态 Trial 或全局 Manager 查找依赖。

003A 的 `Rebuild Sleeping Forest Greybox` 是开发期全场景重建命令。003B 接入后不得再次运行该命令，否则会清除 Gameplay 组合。若后续确需重新生成几何，必须先作为独立 Scene Authoring 变更审查并重新集成 003B 内容；本 Sprint 不恢复任何自动重建入口。

## 13. 003C 预留接口

003C 只需要读取 003B 已确认状态，不需要 003B 预先实现 Root Bridge 后果。

建议 `ForestSignalTrial` 提供：

```text
CurrentSnapshot                       只读当前快照
TrialStateChanged(snapshot)           实例级、已发生事件
```

Snapshot 使用有限 Enum、稳定 TargetId 和 `uint Revision`，不包含 GameObject、Transform、Collider、本地化文本或 Photon 类型。

003C 的 `ForestRouteReveal` 应通过 Inspector 显式引用同 Scene Trial，订阅 `TrialStateChanged`，并只在 Snapshot 进入 Completed 后切换预配置环境对象。003C 的错误路线/叶垫后果可依据新的 `IncorrectOrder` Revision 执行一次。

本 Preflight 不要求新增 Core 接口或全局事件总线，因为 Trial 与 003C 消费者属于同一 SleepingForest 内容域。若未来 Network 需要复制状态，应由 Host Adapter 复制 Snapshot，而不是让 Visual、Prompt 或 Route 直接同步。

## 14. 现有测试影响

Sprint003A 有两项测试明确断言正式 Scene 不含 InteractionTarget：

- EditMode：`SleepingForestScene_HasNoSandboxOrFutureGameplayFixtures`
- PlayMode：`SleepingForest_HasNoActiveInteractionOrCompletionGameplay`

003B 实施后这些断言会按预期失效。实施计划必须只调整其过时边界：

- 正式 Scene 应恰有两个森林信标 InteractionTarget；
- 仍不得包含 `InteractionExecutionProbe`、`InteractionProbeBehaviour`、Sandbox Target、Root Bridge Gameplay 或 Completion Gameplay；
- 可重命名测试以表达“没有测试夹具及后续环境/完成玩法”，不能删除 003A 的 Player、Camera、FallRecovery 与几何回归。

这属于测试规格随批准内容演进，不是 Foundation 回归，也不是 Preflight 阻塞项。

## 15. 自动化测试边界

### 15.1 EditMode

至少覆盖：

1. Definition 的 Trial ID、A/B TargetId 有效且唯一。
2. 初始状态为 A/B Dormant、AwaitingFirst、Revision 0。
3. A 首次交互后只有 A Activated，进入 AwaitingSecond。
4. B 在初始状态交互产生 IncorrectOrder，不激活任何 Beacon。
5. A → B 后进入 Completed，两个 Beacon 均 Activated。
6. 已激活 Beacon 的再次请求不改变状态或 Revision。
7. 相同 RequestId 经现有 Request Port 重放不重复推进。
8. 两个 Beacon 的 Visual 与 MaterialPropertyBlock 互不污染。
9. 相同 Snapshot/Revision 不重复刷新视觉。
10. Prefab 必需组件、Layer、Trigger、Anchor、Prompt 与 TargetId 配置完整。
11. `IInteractable`、`IExecutableInteraction`、Context、Request、Result 契约签名未改变。
12. Puzzle asmdef 仍只依赖 Core/Content，Interaction 不引用 Puzzle。
13. ScriptableObject 在运行时不保存 Trial 可变状态。

### 15.2 PlayMode

至少覆盖：

1. SleepingForest 加载后恰有两个有效森林信标目标。
2. 靠近 A/B 时现有 Detector 与 Prompt 正常。
3. A 首次按键只执行一次；长按不重复。
4. B 先执行显示 Incorrect，但 Trial 不完成、环境不改变。
5. 错误后仍可执行 A，再执行 B 完成 Trial。
6. A 激活后 A 不再显示激活 Prompt，B 仍可检测。
7. A/B 视觉状态独立且与 Trial Snapshot 一致。
8. 重复 RequestId、目标销毁、遮挡、超距和组件禁用安全。
9. Trial Completed 后 Root Bridge、路线 Collider、终点和 Scene Completion 均保持 003A 状态。
10. Main Route 与 Advantage Route Reserved 继续物理可达，不因 003B 被封堵。
11. 单 Player、单 Main Camera、FallRecovery、Movement、Camera 与 003A 几何回归通过。
12. Sprint002A–D Interaction Foundation 完整回归通过。
13. Console Error `0`，稳定状态无新增持续 `GC.Alloc`。

### 15.3 人工验收

在正式 SleepingForest Scene：

1. 从 Spawn 走到 Beacon A/B，确认检测与 Prompt 正常。
2. 先操作 B，确认只出现 Beacon 自身 Incorrect 反馈，路线和 Root Bridge 不变化。
3. 操作 A，确认 A 独立 Activated、B 回到 Dormant 且仍可交互。
4. 操作 B，确认两者 Activated、Trial Completed。
5. 对激活后的 Beacon 再按交互键，确认不重复推进、不切回 Dormant。
6. 验证两 Beacon 不共享视觉状态或 Material 污染。
7. 确认 Main/Advantage 两条物理路径、FallRecovery、WASD 与 Camera 仍正常。
8. 确认没有 Root Bridge 后果、路线封锁、完成 UI、Puzzle/Ability/Inventory/Network 行为。
9. Console Error `0`。

## 16. 预计新增与修改范围

以下仅为 Implementation Plan 输入，本 Preflight 未创建：

### 预计新增

- `Runtime/Content/SleepingForest/ForestSignalTrialDefinition.cs`
- `Runtime/Puzzle/SleepingForest/ForestSignalTrial.cs`
- `Runtime/Puzzle/SleepingForest/ForestBeaconInteraction.cs`
- `Runtime/Puzzle/SleepingForest/ForestBeaconVisual.cs`
- 内容专用 Enum/Snapshot 文件，按实际职责拆分
- `ScriptableObjects/Content/SleepingForest/ForestSignalTrialDefinition.asset`
- `ScriptableObjects/Content/Interaction/InteractionPrompt_ForestBeacon.asset`
- `Prefabs/SleepingForest/PF_ForestBeacon.prefab`
- Sprint003B EditMode / PlayMode 测试
- Sprint003B Implementation Report / Review Checklist

### 预计修改

- `SleepingForest.unity`：只接入两个 Beacon 与唯一 Trial。
- EditMode/PlayMode 测试 asmdef：只增加 Content/Puzzle 测试依赖（如测试直接引用新类型）。
- 两项过时的 Sprint003A“无 InteractionTarget”测试断言。
- `CHANGELOG.md` 与 Sprint003B 文档。

### 禁止修改

- Character Foundation 全部生产代码和资产。
- `IInteractable`、`IExecutableInteraction`、InteractionContext/Request/Result。
- InteractionDetector、InteractionValidator、Prompt Presenter/View、InteractionExecutor、LocalInteractionRequestPort。
- Player Prefab、Input Actions、Movement、GroundDetector、Camera Foundation。
- Root Bridge、路线 Collider、Completion、Puzzle Framework、Inventory、Ability、Network。

## 17. 风险与控制措施

| 风险 | 级别 | 控制措施 |
| --- | --- | --- |
| 把错误顺序塞入通用 InteractionResult | 高 | InteractionResult 只表达执行接受；IncorrectOrder 保留在内容 Snapshot。 |
| 两 Beacon 各自维护顺序造成状态分叉 | 高 | Scene 中唯一 ForestSignalTrial 持有逻辑真值。 |
| Puzzle 代码依赖 Interaction 实现 | 高 | 只经 Core 契约和 `MonoBehaviour` 显式引用组合；asmdef 测试锁定方向。 |
| Activated Beacon 仍显示“激活”Prompt | 中 | Trial 状态映射为该 Target 不可检测/不可执行；不修改 Prompt Runtime。 |
| Incorrect 视觉与逻辑状态混淆 | 中 | Beacon Logical State 仅 Dormant/Activated；Incorrect 是由 LastOutcome 派生的 Visual State。 |
| 共享材质导致 A/B 串色 | 中 | 每 Renderer 独立 MaterialPropertyBlock，并增加双实例测试。 |
| 003B 意外触发 Root Bridge/完成 | 高 | Trial 不持有环境对象；PlayMode 显式断言环境保持 003A 状态。 |
| 003A Rebuild 清除新 Gameplay | 高 | 003B 接入后冻结开发期 Rebuild；几何重建必须单独审查。 |
| 既有 003A 测试因批准内容变化失败 | 中 | 精确更新两项过时断言，保留全部 Foundation/几何回归。 |
| 单机状态被误称为多人权威 | 高 | 当前只报告本地状态；Snapshot/Revision 仅预留 Host 复制边界，不实现 Network。 |

## 18. Definition of Ready

- [x] Sprint003A 正式 Scene、Beacon 平台与 Anchor 已通过 Gate。
- [x] Sprint002A–D Detection、Prompt、Execution、Prefab 组合模式已通过 Gate。
- [x] Core Interaction 契约无需修改。
- [x] `WonderSquad.Puzzle` 与 `WonderSquad.Content` 依赖方向可承载内容实现。
- [x] Beacon A → Beacon B 的最小顺序、错误与重复交互规则已明确。
- [x] 两 Beacon 的独立状态与唯一 Trial 所有权已明确。
- [x] 静态 Definition、Scene 运行时状态与本地 Visual 状态已分离。
- [x] 003C 只读 Snapshot/Event 边界已明确。
- [x] 现有 003A 测试影响已识别。
- [x] Root Bridge、路线、Completion、Puzzle Framework、Inventory、Ability 与 Network 已明确排除。

没有发现必须先修改 Foundation 或扩大范围才能实施的阻塞项。

## 19. Preflight 最终决定

# GO

下一步只能是生成 Sprint003B Implementation Plan 或在新的明确指令下实施本报告锁定的最小 Beacon Interaction State。当前停止，不修改 Unity 资产，不开始 Sprint003B 实现，也不开始 Sprint003C。
