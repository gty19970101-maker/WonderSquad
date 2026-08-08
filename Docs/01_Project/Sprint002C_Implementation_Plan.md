# Sprint002C Interaction Execution Implementation Plan

## 1. 文档状态

- Sprint：`Sprint002C`
- 主题：Interaction Execution
- 当前分支：`feature/sprint002c-interaction-execution`
- Unity 基线：`6000.3.21f1`
- 依赖基线：
  - Sprint002A Interaction Detection：`PASSED / GO`
  - Sprint002B Interaction Prompt：`PASSED / GO`
  - Sprint002C Preflight：`CONDITIONAL GO`
- 本计划结论：`READY FOR IMPLEMENTATION`

本计划关闭 `Sprint002C_Preflight_Report.md` 中列出的实施前条件。实施阶段必须严格按照本计划完成本地单机交互执行闭环，不得扩展到具体机关、库存、谜题或联网实现。

## 2. 目标

实现本地单机环境中的最小交互执行链路：

```text
玩家按下 Interact
→ 捕获当前 Detector 目标
→ 创建稳定请求
→ 执行前重新验证
→ 调用独立执行契约
→ 返回结构化结果
```

该链路必须满足：

- 一次按下最多执行一次。
- 长按不重复执行，松开后可以再次执行。
- 执行目标必须与按键瞬间的检测目标一致。
- 距离、遮挡、Layer 和目标存活状态必须在执行前重新验证。
- Prompt 只负责展示，不参与执行授权。
- 本地实现使用与未来 Host Authority 相同的请求语义。

## 3. Sprint 范围

### 3.1 只实现

- 独立可执行交互契约。
- InteractionContext。
- InteractionRequest。
- InteractionResult 与结果代码。
- PlayerId 与 RequestId 最小值类型。
- 独立 Interaction Input Reader。
- Interaction 请求编排。
- 本地请求端口。
- InteractionExecutor。
- 执行前验证。
- RequestId 去重与输入边沿门控。
- 通用 ExecutionProbe。
- PlayerSandbox 验证组合。
- EditMode 与 PlayMode 测试。

### 3.2 禁止实现

- Door
- Lever
- Chest
- Bridge
- Puzzle
- Inventory
- Ability
- Network、RPC、Fusion Adapter 或 NetworkObject
- 持续交互与多人参与槽位
- 自动触发交互
- 时间冷却或长按连发
- Interaction Result UI
- UI 动画
- 音效
- 全局事件总线
- PlayerMovement、Camera 或 Prompt 行为修改

## 4. 已锁定的关键设计

| 设计项 | 最终决定 |
|---|---|
| Detection 契约 | `IInteractable` 保持不变 |
| Execution 契约 | 新增 `IExecutableInteraction` |
| Executor 所属程序集 | `WonderSquad.Interaction` |
| 输入读取 | 新增独立 `PlayerInteractionInputReader` |
| 移动输入 | `PlayerInputReader` 保持 Move-only |
| 请求模型 | 新增不可变 `InteractionRequest` |
| 上下文 | 新增不可变 `InteractionContext` |
| 结果 | 使用结构化 `InteractionResult`，禁止只返回 bool |
| 执行目标来源 | Detector 提供焦点；本地请求端口解析相同目标 |
| 执行前校验 | Executor 使用独立 Validator 重新验证 |
| 去重 | Press 边沿、Release 重置、RequestId 结果缓存 |
| 冷却 | 本 Sprint 不增加时间冷却 |
| 本地身份 | Player Prefab 显式提供非零 `PlayerId` |
| 未来联网 | 网络适配器替换本地请求端口，Host 重新解析与验证 |

## 5. 契约分离

### 5.1 IInteractable

`IInteractable` 不修改，继续只提供：

- `TargetId`
- `DetectionPosition`
- `IsDetectionEnabled`
- `DetectionPriority`

它只表达“可以被发现和选择”，不表达“可以执行”，也不得新增 `Interact()`、`Execute()` 或目标状态修改方法。

### 5.2 IExecutableInteraction

新增 `IExecutableInteraction`，名称按本计划固定，不再使用 `IInteractionExecutionTarget`。

职责：

- 暴露与 Detection 一致的稳定 `InteractionTargetId`。
- 接收只读 `InteractionContext`。
- 返回结构化 `InteractionResult`。
- 实现目标自身的最小执行行为。

约束：

- 不包含 Prompt 文本、Binding、UI 类型或本地化字符串。
- 不包含 InputAction、Keyboard、Gamepad 等输入类型。
- 不包含 Photon、Fusion、RPC 或 NetworkObject 类型。
- 不要求所有 `IInteractable` 都必须可执行。
- Detection TargetId 与 Execution TargetId 不一致时禁止执行。

目标对象可以通过不同组件分别实现 Detection 与 Execution，但两个组件必须属于同一逻辑目标并使用相同稳定 ID。

## 6. 总体运行链路

```text
WonderSquad.inputactions / Gameplay/Interact
                    ↓
PlayerInteractionInputReader
                    ↓ IInteractionInputSource
InteractionExecutor
                    ↓
InteractionExecutionValidator
                    ↓
IInteractionRequestPort
                    ↓
LocalInteractionRequestPort
                    ↓
IExecutableInteraction
                    ↓
InteractionResult
```

Prompt 链路保持独立：

```text
InteractionDetector
        ↓
InteractionPromptPresenter
        ↓
InteractionPromptView
```

执行链路不得读取 Prompt Presenter 或 Prompt View。Prompt 是否可见不构成执行授权。

## 7. InteractionExecutor

### 7.1 所属位置

- Assembly：`WonderSquad.Interaction`
- Namespace：`WonderSquad.Interaction.Execution`
- 类型形态：MonoBehaviour 只负责显式组合与 Unity 生命周期，验证规则下沉到普通 C# Validator

### 7.2 唯一职责

InteractionExecutor 只负责：

1. 订阅设备无关的 `IInteractionInputSource` Press 事实。
2. 从显式 Detector 捕获当前明确目标并构造 `InteractionRequest`。
3. 接收外部已构造的 `InteractionRequest` 与已选择目标。
4. 调用 `InteractionExecutionValidator` 完成执行前重新验证。
5. 将验证通过的请求提交到 `IInteractionRequestPort`。
6. 保存并发布 `InteractionResult`。

### 7.3 明确禁止

InteractionExecutor 不得：

- 使用 `FindObjectOfType`、`FindFirstObjectByType`、Tag、名称或全场景扫描寻找目标。
- 从范围查询中选择目标。
- 读取 Keyboard、Gamepad 或 InputAction。
- 订阅 PlayerMovement。
- 修改 Transform、CharacterController 或 Rigidbody。
- 查找或显示 UI。
- 读取或修改 Prompt。
- 知道 Door、Lever、Chest、Puzzle、Inventory 等具体类型。
- 引用 Player、UI、Network 或具体 Fusion 类型。

目标发现属于 Detector；Executor 只解析 Detector 已选对象上的执行契约；执行验证属于 Executor/Validator；目标调用与 RequestId 去重属于 Local Request Port。

## 8. InteractionContext

### 8.1 设计

`InteractionContext` 是不可变、网络 SDK 无关的请求上下文。字段固定为：

| 字段 | 类型语义 | 规则 |
|---|---|---|
| `PlayerId` | 请求玩家稳定 ID | 非零；本地由 Player 身份组件提供 |
| `RequestId` | 单次按压请求 ID | 非零；单局内单调递增并跳过零 |
| `RequestTimeSeconds` | 请求发生的单调会话时间 | `double`；必须有限且不小于零 |
| `InteractionOrigin` | 按键瞬间的交互原点 | 世界坐标；必须为有限值 |
| `InteractionDirection` | 按键瞬间的玩家交互方向 | 世界方向；必须有限且非零，构造时归一化 |
| `ExpectedTargetRevision` | 未来 Authority 版本预留 | `uint`；本地 Sprint002C 使用 `0` 表示未提供版本 |

### 8.2 Time 语义

- 本地单机使用 `Time.unscaledTimeAsDouble` 形成单调会话时间。
- 不使用系统日期时间，不依赖本地时区。
- Sprint002C 不根据该时间实现超时或冷却。
- 未来联网由 Network Adapter 使用 Fusion Network Time 或 Tick 转换相同语义。
- Host 不得仅相信客户端上报时间；它只能作为请求上下文和诊断信息。

### 8.3 Origin 与 Direction 语义

- Origin 来自玩家显式交互原点，不来自 Camera。
- Direction 来自玩家表现对象的前向，不从 Camera 或移动输入推导。
- 本 Sprint 不新增方向锥筛选；Direction 只建立稳定请求语义并为后续能力或 Authority 验证预留。
- 执行验证使用权威端当前交互原点检查距离和遮挡，不把客户端 Origin 当作权威位置。

### 8.4 Authority 预留原则

本阶段只预留 `ExpectedTargetRevision`，不增加以下可伪造字段：

- `IsHost`
- `IsAuthority`
- `HostId`
- Photon PlayerRef
- NetworkObject 引用

未来 Host Adapter 负责补充会话、Input Authority、State Authority、Network Tick 和实际 Target Revision。客户端不能通过 Context 自称具有权威。

## 9. PlayerId 与 RequestId

### 9.1 PlayerId

- 位于 `WonderSquad.Core` 的 Identifiers。
- 使用无符号整数值语义，与 `Technical_Architecture_v1.1.md` 保持一致。
- `0` 为 Invalid。
- Sandbox Player Prefab 通过显式身份组件提供非零值。
- 不使用 Unity Instance ID、对象名称、Tag 或 Executor 内的硬编码常量。
- 未来由 Network Spawn/Session 层提供同一 ID 契约。

### 9.2 RequestId

- 位于 `WonderSquad.Core` 的 Identifiers。
- 使用无符号整数值语义。
- `0` 为 Invalid。
- 每次有效 Press 生成一个新 ID。
- 长按期间不生成新 ID。
- 释放并再次按下才生成下一个 ID。
- 序列溢出时跳过 `0`。
- 本地生成器仅保证当前本地会话内唯一；未来 Host 以 PlayerId 与 RequestId 组合作为幂等键。

## 10. InteractionRequest

### 10.1 是否需要

**需要。**

如果只把 TargetId 塞进 Context，Context 会同时承担“谁、何时、从哪里发起”与“要对谁执行”的两个职责。独立 Request 能明确分离：

- Context：请求者和请求发生环境。
- Request：目标明确的交互命令。

### 10.2 字段

`InteractionRequest` 固定包含：

| 字段 | 用途 |
|---|---|
| `Context` | 完整请求上下文 |
| `TargetId` | 本次请求唯一目标 |

Sprint002C 只有 Primary Interaction，不提前加入 InteractionKind、持续时间、资源成本或机关参数。

### 10.3 约束

InteractionRequest 不得包含：

- GameObject
- Transform
- Collider
- `IInteractable` 或 `IExecutableInteraction` 引用
- Prompt 文本
- InputAction
- UI 状态
- Photon/Fusion 类型

本地 Unity 目标引用只能存在于 `WonderSquad.Interaction` 内部的短生命周期“已解析候选”对象中，不进入 Core 请求契约，也不跨网络传输。

### 10.4 请求端口语义

`IInteractionRequestPort` 是权威执行边界：

- 接收 `InteractionRequest` 与已经解析、验证的 `IExecutableInteraction`。
- 返回结构化 `InteractionResult`，不得返回 bool。
- Local Port 在当前同步调用栈内执行和返回。
- Future Host Adapter 在主机解析权威目标后复用同一端口语义。
- 客户端到主机的异步传输由未来 Network Request Adapter 负责，不把 RPC 或异步 SDK 类型放入该 Core 端口。
- 不使用静态事件或全局事件总线。

InteractionExecutor 调用端口并保存 Result；Local Port 负责幂等调用目标。

## 11. InteractionResult

### 11.1 结果结构

`InteractionResult` 是不可变结果，至少包含：

| 字段 | 用途 |
|---|---|
| `RequestId` | 对应请求 |
| `TargetId` | 对应目标 |
| `Code` | 稳定结果代码 |

可以提供由 `Code == Success` 计算得到的只读便利属性，但执行方法不得只返回 bool。

### 11.2 结果代码

结果 Enum 固定以安全默认开始：

| Code | 含义 |
|---|---|
| `Unknown = 0` | 未分类错误或目标返回非法结果 |
| `Success` | 权威执行成功 |
| `TargetInvalid` | 目标为空、销毁、ID 不一致或不可检测 |
| `OutOfRange` | 当前权威位置超过 Detection Radius |
| `Occluded` | 当前射线路径被遮挡 |
| `LayerRejected` | 目标 Layer 不在允许范围 |
| `Busy` | 目标当前不能接受新执行 |
| `Cancelled` | 请求在执行前或执行中被取消 |
| `NotSupported` | 目标可检测但没有支持的执行契约 |

执行前验证产生 `TargetInvalid`、`OutOfRange`、`Occluded` 或 `LayerRejected`。目标执行契约主要返回 `Success`、`Busy`、`Cancelled`、`NotSupported` 或 `Unknown`。

重复 RequestId 不重复调用目标，而是返回第一次缓存的原始结果。

## 12. Input 到 Request 的组合边界

根据最终实施指令，Input 到 Request 的本地组合职责由 `InteractionExecutor` 的 MonoBehaviour 外壳承担，不额外创建 `InteractionRequestController`，避免两个相邻组件重复订阅同一输入。

职责：

- 通过 Inspector 注入实现 `IInteractionInputSource` 的 Player 组件。
- 订阅 Press 事件并在 Disable 时取消订阅。
- 从显式注入的 InteractionDetector 捕获当前焦点 TargetId。
- 从显式身份来源读取 PlayerId。
- 捕获请求时间、交互原点和方向。
- 生成 RequestId。
- 创建 InteractionContext 与 InteractionRequest。
- 将 Request 提交给 `IInteractionRequestPort`。

禁止：

- 在 Update 中轮询按住状态。
- 在 Request 构造阶段直接执行目标。
- 绕过 Validator 或 Request Port。
- 读取 Prompt。
- 修改 PlayerMovement。
- 使用全局事件总线或静态 Player 引用。

无当前目标时形成并返回 `TargetInvalid`，不得调用执行目标。未来网络阶段可把 Request 构造和发送提取到 Network Adapter，但不会修改 Input Reader、Context 或 Result 契约。

## 13. 输入设计

### 13.1 PlayerInteractionInputReader

- Assembly：`WonderSquad.Player`
- Namespace：`WonderSquad.Player.Input`
- 复用现有 `Gameplay/Interact` Button Action。
- 不修改 `Gameplay/Move`。
- 不修改现有 `PlayerInputReader`。
- 实现 Core 中最小 `IInteractionInputSource`。
- 只发布 Press 与 Release 生命周期事实。
- 不读取 Detector、Executor、Prompt 或 Camera。

### 13.2 生命周期

```text
OnEnable
→ 校验 Action
→ 创建一次 Runtime Action
→ 订阅 performed / canceled
→ Enable

performed
→ 若尚未等待释放，发布一次 Press
→ 进入等待释放

持续按住
→ 不发布新 Press

canceled
→ 清除等待释放
→ 发布或记录 Release

OnDisable
→ 取消订阅
→ Disable / Dispose
→ 清除等待释放
```

禁止：

- 在 Update 中调用 `IsPressed()` 持续触发。
- 每帧创建 InputAction 或订阅事件。
- 直接使用 `Keyboard.current` 或 `Gamepad.current`。
- 让 Input Reader 调用 Executor。

## 14. 请求去重与重入

采用三层最小保护：

### 14.1 输入层

- 只响应 Press 边沿。
- Press 后等待 Release。
- 长按不重复发布。
- Disable 时清除按压状态。

### 14.2 Controller 层

- 每次有效 Press 只创建一个 RequestId。
- 同一 Press 不重复 Submit。
- 不在 Update 中提交。

### 14.3 Executor 层

- 使用最小 `isExecuting` 重入保护。
- 缓存最近一次 RequestId、PlayerId 与 Result。
- 相同 PlayerId + RequestId 重复到达时返回缓存结果。
- 重复请求不得再次调用 `IExecutableInteraction`。

Sprint002C 只保存最近一次本地结果，满足单玩家同步调用测试。未来 Host 使用有界、多玩家、按 Tick 过期的幂等缓存替换，不复用该最小缓存作为正式网络实现。

## 15. 执行前重新验证

### 15.1 固定流程

产品逻辑必须体现以下顺序：

```text
Detector 当前目标用于 Prompt
        ↓
玩家按下 Interact
        ↓
重新验证距离
        ↓
重新验证遮挡
        ↓
重新验证 Layer
        ↓
重新验证目标仍存在且与按键目标一致
        ↓
执行 IExecutableInteraction
        ↓
返回并缓存 InteractionResult
```

代码内部必须在任何位置、距离或射线访问前先做空值和 Unity Object 存活保护；这是安全前置检查，不改变上述四项业务验证都必须在执行前重新完成的要求。

### 15.2 验证输入

Local Request Port 只解析本次明确目标，不做范围候选发现：

- 读取显式注入 Detector 的当前目标。
- 确认 CurrentTarget.TargetId 与 Request.TargetId 一致。
- 从该目标自身或父级解析 `IExecutableInteraction`。
- 不执行全场景查找。
- 不自动切换到另一个目标。

若目标 A 在按键后变为 B，本次 A 请求返回 `TargetInvalid`，不得静默执行 B。

### 15.3 Validator 检查顺序

`InteractionExecutionValidator` 按以下顺序返回第一个明确失败：

1. RequestId、PlayerId、TargetId 与 Context 合法。
2. Context Time、Origin、Direction 为有限合法值。
3. Detection 目标与 Execution 目标仍存活。
4. 两个目标 ID 与 Request.TargetId 完全一致。
5. `IsDetectionEnabled` 仍为 true。
6. 目标 Layer 在 Interaction Layer Mask 中。
7. 当前权威交互原点到 DetectionPosition 未超过 Detection Radius。
8. 从当前权威原点到目标的 Raycast 未被其他 Collider 遮挡。
9. Execution Contract 可用。

虽然流程图按产品语言描述为距离、遮挡、Layer、目标存在，但代码验证必须先做空值和存活保护，再执行 Physics 检查，避免对销毁对象访问。

### 15.4 Physics 规则

- 距离和 Layer 规则必须与现有 `InteractionSettings` 一致。
- 使用同一 Detection Radius、Interaction Layer Mask、Raycast Layer Mask 和 TriggerInteraction 配置。
- 遮挡射线命中同一 TargetId 的 Collider 不算遮挡。
- Trigger 处理遵循现有 Settings，不硬编码。
- 不调用 `Physics.OverlapSphere`。
- 不在执行时重新运行候选选择。

## 16. 本地单机与未来 Network Authority

### 16.1 Sprint002C 本地路径

```text
PlayerInteractionInputReader
→ InteractionExecutor
→ IInteractionRequestPort
→ LocalInteractionRequestPort
→ IExecutableInteraction
```

InteractionExecutor 与 Local Request Port 共同形成离线权威适配：

- 同步处理请求。
- 使用当前本地玩家权威位置。
- Executor 捕获 Detector 当前明确目标并重新验证。
- Local Port 接收已验证执行契约。
- Local Port 基于 PlayerId + RequestId 去重并调用目标。
- 返回 InteractionResult。

### 16.2 未来网络路径

```text
PlayerInteractionInputReader
→ Future Client Request Adapter
→ Network Interaction Request Adapter
→ Host Authority Target Resolver
→ InteractionExecutor
→ Host IInteractionRequestPort
```

未来 Network Adapter：

- 传输相同 InteractionRequest。
- 使用 PlayerId + RequestId 去重。
- Host 根据 TargetId 从权威注册表解析目标。
- Host 使用权威 Player Position、Layer、遮挡、Target Revision 和状态重新验证。
- Host 调用同一执行契约语义。
- Host 返回相同 InteractionResult Code。

Gameplay 与 Core 契约不得引用 Fusion。Network Adapter 只替换请求传输和 Authority 数据来源，不复制第二套 Interaction Gameplay 规则。

### 16.3 信任边界

- 客户端 Prompt 和 Detector Focus 只是请求目标建议。
- Request 中的 Origin、Direction、Time 和 Revision 不能单独证明合法。
- Host 最终决定共享执行结果。
- Sprint002C 的 Local Port 只模拟离线 Authority，不宣称完成网络权威。

## 17. ExecutionProbe

### 17.1 用途

只允许创建通用 `InteractionExecutionProbe`，用于自动化与 PlayerSandbox 人工验证。

允许行为：

- 成功时增加只读执行计数。
- 保存最后一次 RequestId、PlayerId 和 Result Code 供 Inspector 查看。
- 可选切换占位 Renderer 颜色，提供无 UI 的视觉反馈。
- 可配置返回 `Success`、`Busy`、`Cancelled`、`NotSupported` 或 `Unknown`。
- 可在单次执行时输出明确诊断日志，但不得每帧输出。

### 17.2 禁止行为

- 开门
- 拉动机关
- 发放或消耗物品
- 推进谜题
- 修改 Inventory
- 使用 Ability
- 修改 Level 状态
- 播放业务 UI、动画或音效
- 自动执行
- 引用 Network

Probe 位于 `WonderSquad.Interaction.Diagnostics`，不得被后续正式玩法直接当作机关基类。

## 18. 程序集关系

### 18.1 运行时依赖

```text
WonderSquad.Player      → WonderSquad.Core
WonderSquad.Interaction → WonderSquad.Core
WonderSquad.UI          → WonderSquad.Core
WonderSquad.UI          → WonderSquad.Interaction
WonderSquad.UI          → WonderSquad.Player
```

现有 Runtime asmdef 引用不需要变化：

| Assembly | Sprint002C 后依赖 |
|---|---|
| `WonderSquad.Core` | 无新增依赖 |
| `WonderSquad.Player` | `WonderSquad.Core`、`Unity.InputSystem` |
| `WonderSquad.Interaction` | `WonderSquad.Core` |
| `WonderSquad.UI` | 保持现状 |

### 18.2 禁止方向

- `WonderSquad.Player → WonderSquad.Interaction`
- `WonderSquad.Interaction → WonderSquad.Player`
- `WonderSquad.Interaction → WonderSquad.UI`
- `WonderSquad.Core → 任一领域程序集`
- Gameplay → Network/Fusion

跨领域组合通过 Core 接口与 Prefab 显式引用完成，不使用静态全局引用、反射或场景查找绕过依赖边界。

## 19. 预计新增文件

文件名与职责按本计划锁定；实现时必须同时生成 Unity `.meta`。

### 19.1 Core

- `Client/Assets/WonderSquad/Runtime/Core/Identifiers/PlayerId.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Identifiers/RequestId.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Player/IPlayerIdentitySource.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/IExecutableInteraction.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/IInteractionInputSource.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/IInteractionRequestPort.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionContext.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionRequest.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionResult.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionResultCode.cs`

### 19.2 Player

- `Client/Assets/WonderSquad/Runtime/Player/Identity/LocalPlayerIdentity.cs`
- `Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInteractionInputReader.cs`
- `Client/Assets/WonderSquad/Runtime/Player/Input/InteractionInputConfigurationValidator.cs`

### 19.3 Interaction

- `Client/Assets/WonderSquad/Runtime/Interaction/Execution/LocalInteractionRequestPort.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Execution/InteractionExecutor.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Execution/InteractionExecutionValidator.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Execution/InteractionRequestIdGenerator.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Diagnostics/InteractionExecutionProbe.cs`

短生命周期 Unity 目标引用只存在于 Executor 的单次调用栈中，不进入 Core Request 或 Network 契约。

### 19.4 Tests

- `Client/Assets/WonderSquad/Tests/EditMode/InteractionExecutionEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionExecutionPlayModeTests.cs`

### 19.5 Sprint 文档

- `Docs/01_Project/Sprint002C_Implementation_Report.md`
- `Docs/01_Project/Sprint002C_Review_Checklist.md`

## 20. 预计修改文件与资产

- Player Prefab
  - 增加 LocalPlayerIdentity。
  - 增加 PlayerInteractionInputReader。
  - 增加 InteractionExecutor。
  - 组合 LocalInteractionRequestPort 所需引用。
- `Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`
  - 组合本地执行验证环境。
- EditMode/PlayMode Test asmdef
  - 仅在现有引用不足时增加必要引用。
- `CHANGELOG.md`
- Sprint002C 实施报告与 Review Checklist。

预计不修改：

- `IInteractable.cs`
- `InteractionTargetId.cs`
- `InteractionDetector.cs`
- `InteractionValidator.cs`
- `InteractionSettings.cs`
- `PlayerInputReader.cs`
- `PlayerMovement.cs`
- Camera Runtime
- Interaction Prompt Runtime
- Prompt Prefab
- `WonderSquad.inputactions`
- Package manifest 或 package lock

## 21. 推荐实施顺序

1. 新增 PlayerId、RequestId 与有效性测试。
2. 新增 InteractionContext、InteractionRequest、InteractionResult 与 ResultCode。
3. 新增 IExecutableInteraction、IInteractionInputSource、IInteractionRequestPort。
4. 新增 LocalPlayerIdentity。
5. 新增并测试 PlayerInteractionInputReader 生命周期。
6. 新增 RequestId Generator。
7. 新增 InteractionExecutionValidator。
8. 新增 LocalInteractionRequestPort 与幂等缓存。
9. 新增 InteractionExecutor。
10. 新增 ExecutionProbe。
11. 组合 Player Prefab 与 PlayerSandbox。
12. 完成 Sprint002C EditMode 测试。
13. 完成 Sprint002C PlayMode 测试。
14. 运行 Character Foundation、Detection 与 Prompt 全量回归。
15. 更新 CHANGELOG、实施报告与 Review Checklist。

任何步骤发现需要修改 Movement、Camera、Prompt 或具体机关时必须停止并重新评审范围。

## 22. EditMode 测试计划

至少覆盖：

1. `IInteractable` 不包含执行方法。
2. `IExecutableInteraction` 与 Detection/Prompt 契约分离。
3. PlayerId 的零值无效，非零值有效。
4. RequestId 的零值无效，序列生成跳过零。
5. InteractionContext 拒绝非法 PlayerId。
6. InteractionContext 拒绝非法 RequestId。
7. InteractionContext 拒绝 NaN、Infinity 或负时间。
8. InteractionContext 拒绝非有限 Origin。
9. InteractionContext 拒绝零或非有限 Direction。
10. Direction 被正确归一化。
11. InteractionRequest 拒绝无效 TargetId。
12. InteractionResult 所有 Code 映射稳定，`Unknown = 0`。
13. Result 成功状态只由 `Success` 推导。
14. Validator 正确返回 TargetInvalid。
15. Validator 正确返回 OutOfRange。
16. Validator 正确返回 LayerRejected。
17. Validator 正确返回 Occluded。
18. Detection TargetId 与 Execution TargetId 不一致时拒绝。
19. 不支持执行契约时返回 NotSupported。
20. 相同 PlayerId + RequestId 返回缓存结果，不再次调用 Probe。
21. Executor 重入时返回 Busy 或既定安全结果。
22. Interaction Action 配置必须是 `Gameplay/Interact` Button。
23. Input Reader 配置缺失或 Action 类型错误时安全禁用。
24. Runtime asmdef 依赖保持无环且没有新增反向引用。
25. Core 契约不引用 UI、Player、Interaction、Network 或 Fusion 类型。

## 23. PlayMode 测试计划

至少覆盖：

1. 无目标按下 Interact，不执行 Probe。
2. 有效目标按一次，Probe 只执行一次。
3. 持续按住，Probe 计数不继续增加。
4. 松开后再次按下，Probe 再执行一次。
5. 键盘 `E` 使用统一执行路径。
6. 手柄西侧按钮使用统一执行路径。
7. 超出范围后执行返回 OutOfRange 或 TargetInvalid，Probe 不增加。
8. Layer 不匹配时返回 LayerRejected 或 TargetInvalid，Probe 不增加。
9. 遮挡后返回 Occluded，Probe 不增加。
10. 遮挡解除后可以重新执行。
11. 目标对象销毁后安全返回 TargetInvalid。
12. Prompt 目标 A 切换为 B 时，不误执行旧 A 或静默执行错误目标。
13. Detection 与 Execution TargetId 不一致时不执行。
14. 可检测但不支持 Execution 时返回 NotSupported。
15. Probe 配置 Busy、Cancelled、NotSupported、Unknown 时结果正确。
16. 同一 RequestId 重复提交不重复增加计数。
17. 禁用 Input Reader 后不执行。
18. 禁用 Controller、Local Port 或 Detector 后不执行。
19. 重新启用后只恢复一组订阅。
20. 按下 Interact 不改变 Player Transform。
21. Interaction 执行不改变 PlayerMovement 输入或速度。
22. Prompt 继续按 Detection 状态显示和隐藏。
23. Player Spawn、Input、Movement、Camera 回归通过。
24. Sprint002A Detection 回归通过。
25. Sprint002B Prompt 回归通过。
26. Console Error 为 `0`。

## 24. 人工验收方案

在 Unity `6000.3.21f1` 中打开 PlayerSandbox：

1. Play 后玩家、Movement、Camera 与 Prompt 正常。
2. 确认 Probe 初始执行计数为 `0`。
3. 无目标时短按 `E`，计数保持 `0`。
4. 靠近 Probe，Prompt 正常显示。
5. 短按一次 `E`，Probe 计数变为 `1`。
6. 长按 `E`，计数仍为 `1`。
7. 松开后再次按下，计数变为 `2`。
8. 使用手柄西侧按钮重复验证。
9. 离开范围后按键，计数不变。
10. 用墙体遮挡后按键，计数不变。
11. 清除遮挡后再次按键，执行恢复。
12. 将 Probe 放到不匹配 Layer，确认不执行。
13. 在两个目标之间切换，确认只执行按键时 Detector 的明确目标。
14. 运行时销毁当前目标，再按键不抛异常。
15. 禁用 PlayerInteractionInputReader 后按键不执行。
16. 重新启用后一次按键只执行一次。
17. 检查 Inspector 中最后 RequestId、PlayerId 和 Result Code。
18. 确认按 `E` 不移动玩家、不改变 Camera、不修改 Prompt 内容。
19. 确认没有 Door、Lever、Chest、Puzzle、Inventory 或 Network 行为。
20. Console Error = `0`。
21. Profiler 中稳定帧没有 Interaction Execution 持续 `GC.Alloc`。

## 25. 完成标准

Sprint002C 实现完成必须同时满足：

- Unity `6000.3.21f1` 编译无错误。
- `IInteractable` 未修改。
- Detection、Execution 与 Prompt 契约保持分离。
- Executor 不搜索目标、不读取输入、不接触 UI。
- 执行前距离、遮挡、Layer 和目标存活均被重新验证。
- InteractionResult 不是 bool-only。
- 长按不重复执行，释放后可再次执行。
- 重复 RequestId 不重复调用目标。
- Player、Interaction、UI 程序集无循环或反向依赖。
- 未引入具体机关或其他禁止系统。
- Sprint002C EditMode 与 PlayMode 测试全部通过。
- Character Foundation、Detection 与 Prompt 回归全部通过。
- Unity 人工验收通过。
- CHANGELOG、Implementation Report 与 Review Checklist 已更新。

## 26. 主要风险与控制

| 风险 | 控制措施 |
|---|---|
| Execution 被加回 IInteractable | Gate 检查契约成员，IInteractable 文件不得修改 |
| Executor 通过场景扫描寻找目标 | 只接受 Local Port 提供的已解析目标 |
| Prompt 被当作执行授权 | 执行只读取 Detector 的稳定 TargetId |
| 客户端 Origin 被未来 Host 信任 | Host 使用权威 Player Query 重新验证 |
| Layer、距离和遮挡规则漂移 | 使用同一 InteractionSettings，并用回归测试锁定 |
| 长按或重复订阅多次执行 | Press/Release 状态机、成对订阅、RequestId 缓存 |
| Local PlayerId 被隐式硬编码 | Prefab 显式身份来源，Core 中只定义 Invalid 规则 |
| Probe 演变为业务机关 | Diagnostics 命名空间与行为白名单 |
| Result Code 被改成字符串 | 使用稳定 Enum，`Unknown = 0` |
| Future Network 复制玩法规则 | Network Adapter 只传输和解析，继续调用同一 Executor 语义 |

## 27. Preflight 条件关闭结果

| Preflight 条件 | 本计划决定 | 状态 |
|---|---|---|
| 保留 IInteractable | 完全不修改 | 已关闭 |
| 独立执行契约 | 使用 IExecutableInteraction | 已关闭 |
| Context 字段 | PlayerId、RequestId、Time、Origin、Direction、Revision | 已关闭 |
| Result 与失败原因 | InteractionResult + 稳定 ResultCode | 已关闭 |
| PlayerId/RequestId | Core 值类型，零值无效 | 已关闭 |
| 本地 PlayerId 来源 | Player Prefab 显式 LocalPlayerIdentity | 已关闭 |
| 独立交互输入 | PlayerInteractionInputReader | 已关闭 |
| 输入端口 | IInteractionInputSource | 已关闭 |
| 请求端口 | IInteractionRequestPort + Local Adapter | 已关闭 |
| 执行前验证 | Executor + InteractionExecutionValidator | 已关闭 |
| 目标切换 | ID 不一致拒绝，不自动切换 | 已关闭 |
| 去重 | Press/Release + RequestId 缓存 | 已关闭 |
| 时间冷却 | 本 Sprint 不实现 | 已关闭 |
| Probe 范围 | 计数、结果、可选占位颜色 | 已关闭 |

## 28. 最终状态

# READY FOR IMPLEMENTATION

Sprint002C Preflight 的全部条件已在本计划中形成明确、可测试的设计决定。后续可以开始 Sprint002C Interaction Execution 实现，但只能按照本计划范围进行；不得顺带开始 Door、Lever、Puzzle、Inventory、Network 或其他后续 Sprint。
