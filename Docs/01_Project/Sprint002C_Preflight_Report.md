# Sprint002C Interaction Execution Preflight Report

## 1. Preflight 结论

- 检查日期：2026-08-02
- Unity 基线：`6000.3.21f1`
- 当前分支：`feature/sprint002c-interaction-execution`
- 当前提交：`3633775`（`v0.5-interaction-prompt`）
- `develop`：`3633775`
- 工作区：检查开始时干净
- Sprint002A：`PASSED`，Gate Review 为 `GO`
- Sprint002B：`PASSED`，Gate Review 为 `GO`
- 本阶段目标：实现本地单机交互执行链路，不实现具体玩法机关
- 原预检结论：**CONDITIONAL GO**
- 当前实施状态：**IMPLEMENTED — UNITY VERIFICATION REQUIRED**

现有 Detection、Prompt、Player Input、Input Actions 与程序集边界能够承载 Sprint002C，不需要重构 Sprint002A 或 Sprint002B。原预检要求的执行契约、独立交互输入组件、离线请求端口以及本地 `PlayerId`/`RequestId` 来源，已由 `Sprint002C_Implementation_Plan.md` 锁定并完成实现。当前仍需 Unity Test Runner 与 PlayerSandbox 人工验收。

预检阶段只完成静态检查与设计决策；后续 Sprint002C 实施没有修改 Input Actions、程序集引用或 Package，但新增了执行代码、测试并组合 Player Prefab 与 PlayerSandbox。

## 2. 当前基础

### 2.1 已具备

- `IInteractable` 是只读检测契约，只公开：
  - `TargetId`
  - `DetectionPosition`
  - `IsDetectionEnabled`
  - `DetectionPriority`
- `InteractionDetector` 已提供：
  - 非分配候选检测。
  - 距离、Layer 与射线遮挡验证。
  - 确定性目标选择。
  - `CurrentTarget` 只读状态。
  - 去重的 `CurrentTargetChanged` 实例事件。
  - 可显式调用的 `RefreshDetection()`。
- `InteractionPromptPresenter` 与 `InteractionPromptView` 已形成只读表现链路。
- `Gameplay/Interact` 已存在：
  - Action Type：`Button`
  - 键鼠：`<Keyboard>/e`
  - 手柄：`<Gamepad>/buttonWest`
- `PlayerInputReader` 当前只负责 `Gameplay/Move`，生命周期和回归测试稳定。
- `WonderSquad.Player` 只依赖 `WonderSquad.Core` 与 `Unity.InputSystem`。
- `WonderSquad.Interaction` 只依赖 `WonderSquad.Core`。
- Player Prefab 已通过 Unity 资产组合挂载 Player 与 Interaction 组件，不要求程序集互相引用。

### 2.2 当前缺口

- 没有 Interaction Execution 输入源。
- 没有 `InteractionExecutor`。
- 没有 `InteractionContext`。
- 没有 `InteractionResult` 或稳定失败原因。
- 没有可执行目标契约。
- 没有网络无关的 Interaction Request Port。
- 没有本地离线权威适配器。
- Core 中尚无正式 `PlayerId` 与 `RequestId` 实现。
- 没有只用于执行验证的通用 Sandbox Probe。
- 没有重复输入、长按、目标切换竞态和执行拒绝测试。

## 3. 推荐执行链路

```text
Gameplay/Interact Input Action
        │
        ▼
PlayerInteractionInputReader
        │ InteractionPressed（输入事实）
        ▼
InteractionExecutor
        │ 捕获当前 TargetId，创建 InteractionContext
        ▼
IInteractionRequestPort
        │
        ▼
LocalInteractionRequestPort
        │ 强制刷新并重新验证 Detector
        ▼
IInteractionExecutionTarget
        │ 目标所属领域决定自身结果
        ▼
InteractionResult
```

Prompt 继续保持独立旁路：

```text
InteractionDetector
        ▼
InteractionPromptPresenter
        ▼
InteractionPromptView
```

执行链路不得读取 Prompt View、Prompt Presenter 或本地化文字。Prompt 是否显示不构成执行授权。

## 4. InteractionExecutor 所属程序集

**结论：属于现有 `WonderSquad.Interaction`。**

理由：

- Executor 编排交互意图、当前目标与请求结果，属于 Interaction 领域。
- 它不应进入 Player，否则 Player 将开始决定目标或具体业务结果。
- 它不应进入 UI，否则 Gameplay 执行将依赖表现层。
- 它不应进入 Core；Core 只定义稳定、网络无关的契约和值类型。
- 本阶段不新建额外 Execution 程序集，避免为单一窄切片过度拆分。

建议命名空间：

- `WonderSquad.Interaction.Execution`

## 5. Executor、Input、Detector 与 Core 的依赖方向

### 5.1 允许方向

```text
WonderSquad.Player      → WonderSquad.Core
WonderSquad.Interaction → WonderSquad.Core
WonderSquad.UI          → WonderSquad.Interaction / Player / Core
```

运行时组合：

```text
PlayerInteractionInputReader
        │ 实现 Core 输入端口
        ▼
InteractionExecutor
        │ 使用同一 Player 上的 Detector
        ▼
LocalInteractionRequestPort
```

### 5.2 明确禁止

- `WonderSquad.Player → WonderSquad.Interaction`
- `WonderSquad.Interaction → WonderSquad.Player`
- `WonderSquad.Interaction → WonderSquad.UI`
- `PlayerInputReader → InteractionExecutor`
- `PlayerMovement → InteractionExecutor`
- `InteractionExecutor → PlayerMovement`
- 用静态事件、Singleton、反射或场景查找规避程序集边界

### 5.3 PlayerInputReader 决策

现有 `PlayerInputReader` 保持 Move-only，不增加 Interact Action，不增加交互事件。

建议在 `WonderSquad.Player.Input` 新增独立 `PlayerInteractionInputReader`：

- 只读取现有 `Gameplay/Interact`。
- 实现 Core 中最小 `IInteractionInputSource`。
- 发布一次性的 `InteractionPressed` 输入事实。
- 不读取 Detector。
- 不构造 InteractionContext。
- 不调用 Executor。
- 不移动 Transform 或 CharacterController。

Executor 只依赖 `IInteractionInputSource`，不依赖具体 `PlayerInteractionInputReader` 类型。这样 Player 与 Interaction 继续平行依赖 Core，不产生反向引用。

## 6. IInteractable 与可执行契约

**结论：不修改 `IInteractable`，拆分独立可执行契约。**

`IInteractable` 已通过 Sprint002A Gate，语义是“可被检测”，不是“可被无条件执行”。向其中增加 `Interact()` 或 `Execute()` 会产生以下问题：

- 任何检测目标都被迫具备执行行为。
- 客户端 Detector 获得直接改变目标状态的入口。
- Prompt、Detection 与 Authority 重新耦合。
- 未来 Network Adapter 难以阻止客户端直接调用。

建议新增 Core 契约：

- `IInteractionExecutionTarget`
  - 提供与 Detection 一致的稳定 `InteractionTargetId`。
  - 接收不可变 `InteractionContext`。
  - 返回 `InteractionResult`。
  - 只由权威请求处理路径调用。
- `IInteractionRequestPort`
  - 接收 Context 并返回 Result。
  - 本阶段由本地离线适配器实现。
  - 未来由 Network Adapter 发送同一语义请求。
- `IInteractionInputSource`
  - 只发布输入边沿事实。
  - 不暴露 Input System、Keyboard、Gamepad 或 Unity UI 类型。

不建议把 Prompt 文本、图标、Binding、Collider、GameObject、MonoBehaviour 或 Photon 类型放进上述契约。

## 7. InteractionContext

**结论：需要。**

Context 是输入、执行、离线权威与未来 Network Adapter 之间的稳定请求边界。最小字段应为：

| 字段 | 用途 |
|---|---|
| `RequestId` | 一次物理按压对应一个唯一请求；用于去重 |
| `PlayerId` | 标识请求者；未来由主机验证 Input Authority |
| `InteractionTargetId` | 执行目标的稳定 ID |

本阶段只有一个 Primary Interaction，不提前增加采集、制作、救援等大量 `InteractionKind`。当真实第二种命令语义进入开发时，再以版本化 Enum 或稳定 Action ID 扩展。

Context 禁止包含：

- `GameObject`
- `Transform`
- `Collider`
- `IInteractable` 引用
- InputAction 或设备对象
- Prompt 文本或本地化字符串
- UI 状态
- Photon/Fusion 类型

### 7.1 标识缺口

当前 Core 尚无 `PlayerId` 与 `RequestId` 代码。Technical Architecture 与 Network Authority Rules 已要求共享请求包含两者，因此不应使用：

- Unity Instance ID
- GameObject 名称
- 固定字符串 `"Player"`
- 隐式数组索引
- 每次执行随机 GUID

建议在 Core 增加最小只读 `PlayerId` 与 `RequestId` 值类型，并由 Player 上的只读本地身份组件提供单机 ID。该身份组件只提供标识，不负责网络生成、角色选择或账号。

此项属于实现前必须锁定的契约变化。

## 8. InteractionResult

**结论：需要。**

Result 至少表达：

| 字段 | 用途 |
|---|---|
| `RequestId` | 与请求对应 |
| `TargetId` | 明确结果针对的目标 |
| `IsAccepted` | 是否执行成功 |
| `FailureReason` | 稳定、非本地化的拒绝原因 |

最小失败原因建议覆盖：

- `None`
- `NoFocusedTarget`
- `InvalidRequester`
- `InvalidTarget`
- `TargetMismatch`
- `OutOfRange`
- `LayerRejected`
- `Occluded`
- `Unavailable`
- `DuplicateRequest`
- `ExecutionRejected`

Result 不应包含本地化提示文本、UI 颜色、动画名称或具体机关状态。Sprint002C 只需返回本地结果与测试证据，不实现结果 UI、动画或音效。

## 9. 执行前重新验证

**结论：必须重新验证。**

Prompt 和上一个检测帧都不是执行授权。一次按压的本地离线流程应为：

1. Executor 捕获按压瞬间的当前 Target 引用与 `TargetId`。
2. 如果没有当前目标，直接返回 `NoFocusedTarget`。
3. 创建带唯一 RequestId、PlayerId、TargetId 的 Context。
4. Local Request Port 强制调用 `InteractionDetector.RefreshDetection()`。
5. 使用刷新后的当前目标重新确认：
   - 目标仍存活。
   - TargetId 有效且与请求一致。
   - Layer 仍允许。
   - 距离仍在范围内。
   - 射线仍无遮挡。
   - `IsDetectionEnabled` 仍为 true。
   - 当前选择仍是按压时的同一逻辑目标。
   - 目标实现有效的 `IInteractionExecutionTarget`。
   - Detection TargetId 与 Execution TargetId 一致。
6. 任一条件变化时拒绝本次按压，不自动改为执行新目标。
7. 全部通过后才调用可执行目标端口。

强制刷新复用 Sprint002A 已验证的距离、Layer 与遮挡规则，避免在 Execution 中复制第二套 Physics 规则。未来联机时，主机不能信任客户端 Detector，必须通过主机目标解析与 Authority Validator 重做同等检查。

## 10. 重复、长按、销毁与目标不一致

### 10.1 一次按键触发多次

- Input Reader 只使用 Button 的按下边沿。
- 一次物理按压只生成一个 RequestId。
- 不在 `Update` 轮询按住状态。
- 生命周期内只允许一次订阅。
- Executor 使用最小 `isExecuting` 重入保护。
- 本地请求端口缓存最近一次 RequestId/Result，重复提交返回既有结果，不再次调用目标。

### 10.2 长按重复触发

- 输入源在首次按下后进入“等待释放”状态。
- 收到 canceled/释放后才允许下一次按下。
- 不增加基于时间的自动重复。
- 禁用输入源时重置按压状态。

### 10.3 已销毁目标

- 捕获与执行前均使用 Unity Object 存活语义。
- 强制刷新后已销毁目标不能继续成为 CurrentTarget。
- 不缓存跨帧 Unity 目标引用作为未来请求数据。
- 目标执行过程中若同步销毁，Result 必须安全结束，不抛 `NullReferenceException`。

### 10.4 Prompt 与执行目标不一致

- Executor 只捕获 Detector 的稳定 TargetId，不从 Prompt View 读取。
- 刷新后如果目标从 A 变为 B，本次 A 请求返回 `TargetMismatch`。
- 禁止在同一次按压中静默改为执行 B。
- Prompt 由 Detector 的目标变化事件同步更新；玩家下一次按压才可请求 B。

## 11. Input Action 接入

**结论：复用现有 Input Actions，不修改 Movement。**

- 使用现有 `Gameplay/Interact`。
- 键鼠与手柄绑定保持不变。
- 不修改 `Gameplay/Move`。
- 不修改 `PlayerInputReader`。
- 不让 Executor 直接读取 `Keyboard.current`、`Gamepad.current` 或 InputAction。
- 独立 Interaction Input Reader 负责 InputAction Enable/Disable、订阅与释放。
- Prompt Binding Provider 继续只读解析 Binding，不消费输入。
- 两个组件可以引用同一 Input Actions 资产，但各自生命周期独立。

当前 Interact Action 是 Button，未配置 Hold 或 Repeat Interaction。Sprint002C 不修改 Input Actions 资产；输入源自身的按下/释放门控必须保证长按只触发一次。

## 12. 未来 Network Authority 请求边界

**结论：需要端口，不实现 Network。**

本阶段：

```text
InteractionExecutor
→ IInteractionRequestPort
→ LocalInteractionRequestPort
→ IInteractionExecutionTarget
```

未来联机：

```text
InteractionExecutor
→ IInteractionRequestPort
→ Network Interaction Request Adapter
→ Host Authority Validator
→ IInteractionExecutionTarget
```

约束：

- Executor 不引用 Photon Fusion。
- Context 不含 Unity Object 或本地化字符串。
- 客户端 Focus 只用于选择请求目标，不代表成功。
- 未来 Network Adapter 必须附带或映射 Input Authority。
- 主机重新校验 PlayerId、TargetId、RequestId、距离、Layer、遮挡、状态和目标 Revision。
- 本地最近 RequestId 去重只是 Sprint002C 最小保护，不代替未来主机的有界幂等缓存。
- 本阶段不创建 RPC、NetworkObject、NetworkRunner 或 Fusion Adapter。

## 13. 冷却与防抖

**结论：不增加时间冷却，只实现最小边沿门控。**

原因：

- 当前只有即时 Primary Interaction。
- 时间冷却会改变交互手感并需要新的可调 Settings。
- 未来不同目标可能有不同持续时间或冷却，不能由通用 Executor 硬编码。

Sprint002C 只允许：

- Press-only 边沿。
- 等待释放后重新武装。
- Executor 重入保护。
- RequestId 去重。

不允许：

- 硬编码毫秒冷却。
- 长按自动连发。
- 在 Update 中按时间轮询。
- 把具体目标冷却放入 Executor。

## 14. 测试用可交互目标

**结论：需要一个通用 Execution Probe，但不能实现具体机关。**

Probe 只用于：

- 记录成功执行次数。
- 记录最后一次 Context/Result。
- 配置接受或拒绝。
- 暴露只读调试状态供测试和 Inspector 验证。

Probe 不得：

- 打开门或改变路线。
- 发放物品或修改 Inventory。
- 推进 Puzzle。
- 触发 Ability。
- 播放 UI 动画或音效。
- 自动执行。
- 依赖 Network。

建议将 Probe 明确放入 Sandbox/Diagnostics 命名空间，并只组合到 PlayerSandbox 或由 PlayMode Test 运行时创建。禁止用修改 `InteractionTarget` 的方式把测试执行状态塞入 Detection 组件。

## 15. Prompt 只读边界

Sprint002C 不修改以下职责：

- `InteractionPromptData` 仍为不可变显示数据。
- `InteractionPromptPresenter` 仍只读取 Detector 与 Prompt Source。
- `InteractionPromptView` 仍只显示。
- Binding Provider 仍只解析并缓存显示字符串。
- Prompt 不订阅 InteractionResult。
- Prompt 不调用 Executor。
- Executor 不读取 Prompt。
- Prompt 显示不代表 Authority 接受。

本阶段不增加成功/失败提示 UI。执行结果仅通过测试 Probe、只读 Inspector 状态或测试断言验证。

## 16. 实现前必须关闭的条件

### 阻塞条件

以下条件必须在 `Sprint002C_Implementation_Plan.md` 中明确锁定：

1. 保留 `IInteractable` 不变，新增独立 `IInteractionExecutionTarget`。
2. 确认 `InteractionContext` 的最小字段为 RequestId、PlayerId、TargetId。
3. 确认 `InteractionResult` 与非本地化 FailureReason 集合。
4. 确认 Core 中 `PlayerId`、`RequestId` 的值类型和无效值规则。
5. 确认单机 PlayerId 的显式来源，不使用 Instance ID、对象名称或隐式常量。
6. 确认独立 `PlayerInteractionInputReader`，不修改现有 `PlayerInputReader`。
7. 确认 `IInteractionInputSource` 与 `IInteractionRequestPort` 的最小职责。
8. 确认 `LocalInteractionRequestPort` 强制刷新 Detector，并拒绝焦点切换后的旧请求。
9. 确认只实现按下边沿、释放重置、重入保护和最近 RequestId 去重，不增加时间冷却。
10. 确认 Sandbox Probe 只记录执行，不实现具体玩法结果。

### 已满足条件

- 当前分支不是 `main` 或 `develop`。
- 当前分支基于检查时最新 `develop`。
- 工作区干净。
- Unity 版本基线一致。
- Sprint002A 与 Sprint002B 均为 `PASSED / GO`。
- Interact Action、键鼠与手柄 Binding 已存在。
- Detection 与 Prompt 无需重构。
- 不需要新增 Package 或第三方插件。

## 17. 预计新增文件

文件名可在 Implementation Plan 中微调，但职责不得改变。

### Core / Contracts

- `Client/Assets/WonderSquad/Runtime/Core/Identifiers/PlayerId.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Identifiers/RequestId.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionContext.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionResult.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionFailureReason.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/IInteractionInputSource.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/IInteractionRequestPort.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/IInteractionExecutionTarget.cs`
- 对应目录与 `.meta`

### Player / Input

- `Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInteractionInputReader.cs`
- `Client/Assets/WonderSquad/Runtime/Player/Input/InteractionInputConfigurationValidator.cs`
- 最小只读本地 PlayerId 提供组件，具体名称在 Implementation Plan 锁定
- 对应 `.meta`

### Interaction / Execution

- `Client/Assets/WonderSquad/Runtime/Interaction/Execution/InteractionExecutor.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Execution/LocalInteractionRequestPort.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Execution/InteractionExecutionValidator.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Diagnostics/InteractionExecutionProbe.cs`
- 对应目录与 `.meta`

### Tests

- `Client/Assets/WonderSquad/Tests/EditMode/InteractionExecutionEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionExecutionPlayModeTests.cs`
- 对应 `.meta`

### Documents

- `Docs/01_Project/Sprint002C_Implementation_Plan.md`
- `Docs/01_Project/Sprint002C_Implementation_Report.md`
- `Docs/01_Project/Sprint002C_Review_Checklist.md`

## 18. 预计修改文件

- Player Prefab
  - 组合独立交互输入、Executor、Local Request Port 与本地身份来源。
- `PlayerSandbox.unity`
  - 增加或调整通用 Execution Probe 验证环境。
- `PF_Interactable_Test.prefab`
  - 仅在需要时组合 Execution Probe，不增加具体机关。
- EditMode/PlayMode Test asmdef
  - 仅在实际需要时增加契约或程序集引用；当前已有 Core、Player、Interaction 引用。
- `CHANGELOG.md`
- Sprint002C 报告与 Checklist。

预计不修改：

- `IInteractable.cs`
- `InteractionDetector.cs` 的检测与选择规则
- `InteractionValidator.cs`
- `InteractionSettings.cs`
- `PlayerInputReader.cs`
- `PlayerMovement.cs`
- Camera Runtime
- Interaction Prompt Runtime
- `WonderSquad.inputactions`
- UI Prefab
- Package 清单

## 19. 程序集依赖变化

### 推荐结果

| Assembly | 实施后依赖 |
|---|---|
| `WonderSquad.Core` | 无 |
| `WonderSquad.Player` | `WonderSquad.Core`、`Unity.InputSystem` |
| `WonderSquad.Interaction` | `WonderSquad.Core` |
| `WonderSquad.UI` | 保持现状 |

如果按推荐端口实现，Runtime asmdef 不需要新增引用：

- Player 实现 Core 输入/身份端口。
- Interaction 消费 Core 端口。
- Unity Prefab 负责组合两者。
- Interaction 不直接引用 Input System。
- Player 不直接引用 Interaction。

任何要求 `WonderSquad.Interaction → WonderSquad.Player` 或 `WonderSquad.Player → WonderSquad.Interaction` 的实现方案都必须停止并重新设计。

## 20. 契约变化摘要

| 契约 | 处理 |
|---|---|
| `IInteractable` | 保持不变，仅用于 Detection |
| `IInteractionPromptSource` | 保持不变，仅用于 Prompt |
| `IInteractionExecutionTarget` | 新增，目标所属领域的权威执行端口 |
| `IInteractionInputSource` | 新增，Player 到 Interaction 的输入事实边界 |
| `IInteractionRequestPort` | 新增，离线与未来网络请求替换边界 |
| `InteractionContext` | 新增，不可变、网络无关请求 |
| `InteractionResult` | 新增，不可变执行结果 |
| `InteractionFailureReason` | 新增，稳定、非本地化拒绝语义 |
| `PlayerId` | 新增，稳定请求者标识 |
| `RequestId` | 新增，幂等请求标识 |

## 21. 风险

| 风险 | 级别 | 控制 |
|---|---|---|
| 把 `Interact()` 加入 IInteractable | 阻塞 | 强制拆分 Execution Target |
| Interaction 直接依赖 Player Input 类型 | 阻塞 | 通过 Core 输入端口组合 |
| 本地 Focus 被当作未来权威结果 | 高 | Local Port 强制刷新；Network 主机重验 |
| Prompt 与执行目标不同 | 高 | 捕获 TargetId，刷新后不一致则拒绝 |
| 长按或重复订阅多次执行 | 高 | Press 边沿、释放重置、订阅成对、RequestId 去重 |
| 已销毁目标仍被调用 | 高 | Unity 存活检查、刷新与 ID 验证 |
| Context 携带 Unity 引用 | 阻塞 | 只允许稳定 ID 与值类型 |
| 用临时常量伪造 PlayerId | 高 | 显式本地身份来源 |
| Executor 知道 Door/Puzzle 等规则 | 阻塞 | 只调用通用 Execution Target 端口 |
| 复制 Detection Physics 规则 | 高 | 复用 `RefreshDetection()` 与既有 Validator |
| 时间冷却硬编码 | 中 | 本阶段不实现时间冷却 |
| Probe 演变为正式机关 | 中 | 只记录次数与 Context，放入 Sandbox/Diagnostics |
| 本地去重被误认为网络幂等 | 中 | 文档明确未来主机仍需有界结果缓存 |

## 22. 自动化测试方案

### 22.1 EditMode

至少覆盖：

1. `IInteractable` 仍无执行方法。
2. `IInteractionExecutionTarget` 与 Prompt 契约分离。
3. Context 的有效/无效 PlayerId、RequestId、TargetId。
4. Result 的 Accepted/Rejected 与 FailureReason 一致性。
5. Input Action 配置必须为 `Gameplay/Interact` Button。
6. 键鼠和手柄 Binding 均可被独立输入源使用。
7. Interaction Input Reader 不引用 Detector、Executor 或 Movement。
8. Executor 不引用具体 `PlayerInteractionInputReader`。
9. Player/Interaction/Core/UI asmdef 依赖保持无环。
10. 最近 RequestId 重复提交返回相同结果。
11. 非可执行目标被拒绝。
12. Execution TargetId 与 Detection TargetId 不一致时拒绝。

### 22.2 PlayMode

至少覆盖：

1. 无目标按下 Interact 不执行。
2. 有效目标按一次只执行一次。
3. 长按不重复执行。
4. 释放后再次按下可执行第二次。
5. 键盘 `E` 与手柄西侧按钮均可触发同一请求路径。
6. 距离超出后拒绝。
7. Layer 不匹配时拒绝。
8. 遮挡后拒绝，遮挡清除后可执行。
9. 目标销毁后安全拒绝，无 NullReferenceException。
10. 目标 A 在按压前切换为 B 时不误执行 B。
11. Detector 禁用时拒绝。
12. Input Reader、Executor 或 Local Port 禁用时不执行。
13. 重新启用后只恢复一次订阅。
14. 同一 RequestId 不重复增加 Probe 计数。
15. Prompt 仍正确显示/隐藏，不因结果改变。
16. 按 Interact 不移动 Player。
17. Player Spawn、Input、Movement、Camera、Detection、Prompt 全量回归通过。
18. Console Error 为 `0`。

## 23. 人工验收方案

在 Unity `6000.3.21f1` 的 PlayerSandbox：

1. Play 后玩家与 Prompt 正常初始化，Probe 计数为 0。
2. 无目标时按 `E`，计数不变。
3. 靠近通用 Probe，Prompt 显示。
4. 短按 `E`，计数只增加 1。
5. 长按 `E`，计数仍只增加 1。
6. 释放再按，计数增加到 2。
7. 使用手柄西侧按钮重复验证。
8. 离开范围后按键，计数不变。
9. 使用墙体遮挡后按键，计数不变；清除遮挡后恢复。
10. 在两个目标之间切换时按键，不执行错误目标。
11. 运行时删除目标，按键不抛异常。
12. 禁用输入源、Executor、Detector 或 Local Port 时不执行。
13. 重新启用后一次按键只执行一次。
14. 按键不移动 Player，不影响 WASD 与 Camera。
15. Prompt 继续只读显示，不展示执行动画或结果 UI。
16. 场景中不存在 Door、Lever、Chest、Bridge、Puzzle、Inventory、Ability 或 Network 实现。
17. Console Error 为 `0`。
18. Profiler 中稳定帧没有 Interaction Execution 持续 `GC.Alloc`。

## 24. 范围禁令

Sprint002C 禁止：

- Door
- Lever
- Chest
- Bridge
- Puzzle
- Inventory
- Ability
- Network 实现
- UI 动画
- 音效
- 全局事件总线
- 自动触发交互
- 持续交互与多人槽位
- Interaction Result UI
- 修改 Player Movement 或 Camera
- 修改 Prompt 为可执行层

## 25. 最终状态

# IMPLEMENTED — UNITY VERIFICATION REQUIRED

第 16 节的契约、身份、输入端口、离线权威、按压语义和 Probe 范围条件已由 Implementation Plan 关闭并完成实现。同版本 Unity 编译参数静态编译已通过；EditMode、PlayMode、完整回归和 PlayerSandbox 人工验收完成前，不得标记为 `PASSED`。
