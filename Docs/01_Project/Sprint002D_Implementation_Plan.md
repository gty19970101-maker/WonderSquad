# Sprint002D Standard Interaction Probe Implementation Plan

> 实施后回填（2026-08-08）：本计划已按既定边界实施并通过 Unity `6000.3.21f1` 验收，最终状态为 **PASSED**。下文的规划内容保留为实现与复用参考。

## 1. Sprint 目标

在 Unity `6000.3.21f1` 中建立第一个可复制的“组合式标准交互对象”参考模板，验证新的 Gameplay Object 能以低成本接入既有单机链路：

```text
InteractionTarget
        ↓
InteractionDetector
        ↓
InteractionPromptPresenter / View

PlayerInteractionInputReader
        ↓
InteractionExecutor
        ↓
IInteractionRequestPort
        ↓
IExecutableInteraction
```

本 Sprint 只创建一个双态 Interaction Probe。第一次有效交互从 Inactive 切换为 Active，第二次有效交互恢复 Inactive；视觉只同步这一状态，不加入动画、音效或任何正式玩法结果。

## 2. 前置结论与范围

- 基线：`v0.6-interaction-foundation`
- 分支：`feature/sprint002d-interaction-probe`
- Unity：`6000.3.21f1`
- 前置 Gate：Sprint002A、002B、002C 均为 `GO`
- 本计划原始状态：**READY FOR IMPLEMENTATION**
- 实施后状态：**PASSED**

必须保持不变：

- `IInteractable`
- `IExecutableInteraction`
- `InteractionContext`
- `InteractionRequest`
- `InteractionResult`
- `InteractionExecutor` 的职责
- `InteractionExecutionProbe` 的 Sandbox/自动化诊断定位

不得将现有 `InteractionExecutionProbe` 升格、重命名、移动或作为正式模板父类。

## 3. 架构与职责

### 3.1 参考对象组合

```text
PF_InteractionProbe
│
├── InteractionTarget
│   └── 只提供 IInteractable：TargetId、检测 Anchor、启用状态、优先级
│
├── InteractionPromptSource
│   └── 只提供 Prompt Definition，不执行也不改状态
│
├── InteractionProbeBehaviour
│   └── 实现 IExecutableInteraction；仅切换自身 Active / Inactive
│
├── InteractionProbeVisualState
│   └── 只将已确认的 Probe 状态映射为颜色或对象可见性
│
└── Collider + 占位 Visual
    └── 参与现有 Detection / Occlusion 物理查询
```

`InteractionProbeBehaviour` 是本 Sprint 的最小示例行为，不是未来 Door、Lever、Button 或 Puzzle Object 的通用父类。它通过 Inspector 显式引用 `InteractionTarget` 和 `InteractionProbeVisualState`；不查找场景对象、不依赖对象名称，也不反向读取 Detector、Prompt、Player 或 Executor。

### 3.2 组件职责

| 组件 | 职责 | 不负责 |
|---|---|---|
| `InteractionTarget` | 发现、稳定目标 ID、检测位置、优先级。 | 执行、Prompt、视觉、机关状态。 |
| `InteractionPromptSource` | 指向静态 `InteractionPromptDefinition`。 | 查找目标、执行、修改状态。 |
| `InteractionProbeBehaviour` | 验证 Context，切换 Probe 局部双态，返回 InteractionResult。 | 发现、输入、UI、跨对象编排、网络。 |
| `InteractionProbeVisualState` | 接收明确的 Active/Inactive 值并更新简单视觉。 | 决定玩法状态、执行、播放动画、读取输入。 |
| Collider | 支持现有范围检测与遮挡判定。 | 触发自动交互或改变状态。 |

### 3.3 状态生命周期

```text
Inactive
  │ 已验证请求 + Success
  ▼
Active
  │ 已验证请求 + Success
  ▼
Inactive
```

以下路径不改变 Probe 状态：无效 Context、已禁用、TargetId 不一致、超距、Layer 不匹配、遮挡、目标销毁、重复 RequestId、Busy、Cancelled 或 NotSupported。上述结果由既有 Executor、Validator 和 Request Port 返回；Probe 只在其 `Execute` 被调用且 Context 合法时切换。

本 Sprint 不新增通用状态接口、状态机框架、Puzzle State System 或跨对象状态。Probe 的 `IsActive` 仅是该组件公开的只读诊断状态，用于测试和本地视觉同步。

## 4. Interaction 执行流程

```text
PlayerInteractionInputReader
        │ Press（单次）
        ▼
InteractionExecutor
        │ 取得 Detector 当前目标；不重新选目标
        ▼
InteractionExecutionValidator
        │ 验证目标存活、TargetId、Layer、距离、遮挡、可用性
        ▼
IInteractionRequestPort
        │ 本 Sprint 为 LocalInteractionRequestPort
        ▼
InteractionProbeBehaviour.Execute(context)
        │ 仅切换 IsActive
        ▼
InteractionProbeVisualState.SetActive(isActive)
        │ 仅更新颜色或可见物
        ▼
InteractionResult
```

视觉同步只能由 `InteractionProbeBehaviour` 成功改变自身状态后，经其已序列化的 `InteractionProbeVisualState` 引用执行一次局部调用。Visual 不能修改 Probe、Detector、Prompt 或任何权威状态；Prompt 和 Detector 也不得绕开该请求链直接调用 Visual 或 Probe。

不创建全局 EventBus：对象内直接组合已足以同步双态视觉，额外全局事件会引入生命周期、跨场景和未来多玩家歧义。若后续领域需要通知其他对象，应在实际领域 Sprint 定义命名明确、带稳定 ID 与 Revision 的局部事件或端口。

## 5. Prefab 方案

### 5.1 目标 Prefab

新增 `Client/Assets/WonderSquad/Prefabs/Interaction/PF_InteractionProbe.prefab`。它是开发者接入流程的参考模板，不是关卡机关内容。

### 5.2 建议层级

```text
PF_InteractionProbe (Layer: Interactable)
├── InteractionTarget
├── InteractionPromptSource
├── InteractionProbeBehaviour
├── InteractionProbeVisualState
├── BoxCollider (Trigger；不阻挡 CharacterController)
└── Visual
    └── MeshRenderer / 占位模型
```

若当前 `InteractionSettings` 的 Trigger 查询配置不接受 Trigger，实施阶段必须使用不阻挡玩家的 Collider 布局并在计划测试中确认；不得私自改变全局 Physics 或检测设置。最终选项必须同时满足：

- Collider 位于 `InteractionSettings.InteractionLayerMask` 所包含的 Layer。
- 范围查询和遮挡射线可稳定将 Collider 解析回 `InteractionTarget`。
- 不阻挡 Player 的 CharacterController。
- Collider 不承担自动触发逻辑。

### 5.3 必填配置

| 配置项 | 位置 | 规则 |
|---|---|---|
| `TargetId` | `InteractionTarget` | 非空、场景内稳定且唯一；不使用对象名称。 |
| `DetectionAnchor` | `InteractionTarget` | 显式指向 Root 或专用 Anchor；与 Collider 可见位置一致。 |
| `DetectionPriority` | `InteractionTarget` | 使用默认值，除非测试多目标排序。 |
| Prompt Definition | `InteractionPromptSource` | 使用独立、静态的 Probe 提示定义；支持现有本地化键预留。 |
| Target 引用 | `InteractionProbeBehaviour` | 显式引用同一 Root 的 `InteractionTarget`。 |
| Visual 引用 | `InteractionProbeBehaviour` | 显式引用 `InteractionProbeVisualState`。 |
| Renderer / Visual Object | `InteractionProbeVisualState` | 显式引用；无引用时安全禁用视觉更新并记录配置错误。 |
| Active / Inactive 表现 | `InteractionProbeVisualState` | 仅两种颜色或两组可见性，不使用 Animator、音频或 VFX。 |

实施阶段应增加 Prefab 配置校验，使 Target、执行行为、PromptSource 和 Collider 缺失或不一致时在开发环境中可发现。校验不得通过场景查找修复引用。

## 6. 文件计划

### 新增

- `Client/Assets/WonderSquad/Runtime/Interaction/Diagnostics/InteractionProbeBehaviour.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Diagnostics/InteractionProbeVisualState.cs`
- `Client/Assets/WonderSquad/Prefabs/Interaction/PF_InteractionProbe.prefab`
- 对应所有 Unity `.meta` 文件
- `Client/Assets/WonderSquad/ScriptableObjects/Content/Interaction/InteractionPrompt_Probe.asset`（仅在现有 Prompt Definition 资产不能复用时创建）
- `Client/Assets/WonderSquad/Tests/EditMode/InteractionProbeEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionProbePlayModeTests.cs`
- 本 Sprint 的实施报告和 Review Checklist

### 允许的最小修改

- `Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`：仅增加一份 `PF_InteractionProbe` 实例和必要测试摆放；不破坏现有 ExecutionProbe、Prompt 测试目标或 Character Foundation 配置。
- 既有测试隔离辅助代码：仅在需要避免新 Probe 干扰既有 Detection/Prompt/Execution 测试时调整。
- `CHANGELOG.md`：仅在实施完成后记录本 Sprint。

### 禁止修改

- `IInteractable.cs`、`IExecutableInteraction.cs`、`InteractionContext.cs`、`InteractionRequest.cs`、`InteractionResult.cs`
- `InteractionExecutor`、`InteractionDetector`、`InteractionValidator`、`LocalInteractionRequestPort`
- Player Input、Spawn、Movement、Camera、Prompt Presenter/View
- Input Actions、Package、asmdef、ProjectSettings
- 现有 `InteractionExecutionProbe` 的定位和行为

任何需要改动上述禁止项的发现均为范围阻塞，必须重新预检。

## 7. 程序集关系

不新增程序集，也不修改 asmdef。

```text
WonderSquad.Core
        ▲
        │ contracts only
WonderSquad.Interaction
        ▲
        │ read-only presentation dependency
WonderSquad.UI

WonderSquad.Player → WonderSquad.Core + Unity.InputSystem
```

Probe 行为和视觉组件位于 `WonderSquad.Interaction`，只依赖 `WonderSquad.Core` 与 Unity 运行时类型。它们不得引入对 Player、UI、Puzzle、Inventory、Ability、Save、Network 或 Editor 程序集的引用。UI 继续通过既有依赖单向消费 Interaction 的只读目标信息。

## 8. 开发者创建新 Interactable 的标准流程

这是未来 Door、Lever、Button、Puzzle Object 的接入协议，而不是对它们的实现授权。

1. 创建或复制一个领域对象 Prefab；不要继承 `InteractionObject` 或 `BaseInteractable`。
2. 在可检测 Root 或子节点添加 `InteractionTarget`，填写唯一稳定 TargetId、Anchor、检测优先级和启用规则。
3. 为检测 Collider 配置 `Interactable` Layer，并验证它符合当前 `InteractionSettings` 的范围与射线规则。
4. 添加 `InteractionPromptSource`，连接领域对应的静态 `InteractionPromptDefinition`；Prompt 文本不写入 View 或 Executor。
5. 添加一个由对象所属领域拥有的 `IExecutableInteraction` 实现；该实现显式引用同一个 `InteractionTarget`，以 TargetId 保持一致。
6. 将实际玩法状态保留在所属领域；若需要静态内容配置，创建该领域的 Definition，而不是通用 InteractionDefinition。
7. 为表现添加只读/单向 Visual 组件。Visual 只能响应领域已确认的状态，不得调用 Executor、修改 Detector 或提交请求。
8. 编写配置、成功、拒绝、销毁和回归测试。
9. 放入 PlayerSandbox 或专用测试场景进行 Detection → Prompt → Execution 人工验收。
10. 若对象影响共享状态，后续 Network Sprint 通过 `IInteractionRequestPort` 的 Host Authority 适配器接入；不在对象内加入 SDK 调用。

## 9. 测试计划

### 9.1 EditMode

| 测试 | 验收重点 |
|---|---|
| 默认状态 | 新建 Probe 为 Inactive，Visual 与逻辑一致。 |
| 第一次执行 | 合法 Context 返回 Success，状态变为 Active。 |
| 第二次执行 | 合法 Context 返回 Success，状态恢复 Inactive。 |
| Result | RequestId、TargetId 和 ResultCode 与请求一致。 |
| 无效 Context | 安全返回 Unknown 或既有约定结果，不改变状态。 |
| 执行不可用 | 返回 Busy，不改变状态。 |
| 目标一致性 | 执行行为 TargetId 与 InteractionTarget 一致。 |
| Prefab 完整性 | 必需 Target、PromptSource、执行行为、Visual、Collider 和 Layer 配置完整。 |
| 契约稳定性 | 断言 Sprint002A–C 核心契约签名未变化。 |
| 依赖方向 | 新 Probe Runtime 不引用 Player、UI、Puzzle、Inventory、Ability、Quest、Save、Network 或 Editor。 |

### 9.2 PlayMode

| 测试 | 验收重点 |
|---|---|
| Detection | 玩家靠近 Probe 后 Detector 选择该目标。 |
| Prompt | 当前目标为 Probe 时显示正确 Binding 与 Prompt 文本。 |
| 第一次按压 | `E` 只执行一次，Probe 进入 Active。 |
| 长按 | Held 状态不重复执行。 |
| 释放后重按 | 第二次执行一次，Probe 恢复 Inactive。 |
| Visual 一致性 | 每次状态改变后颜色/可见对象与 `IsActive` 一致。 |
| 超距与遮挡 | 离开范围或被遮挡后不执行、不改状态。 |
| 销毁安全 | 销毁 Probe 后 Prompt 隐藏且不抛异常。 |
| 回归 | Character Foundation、Sprint002A–C 全量 EditMode/PlayMode 回归通过。 |
| Console | Console Error = `0`。 |

测试不得使用 `FindObjectOfType`、名称查找、全局静态状态或反射绕过实际 Prefab 配置。允许测试隔离使用明确持有的对象引用。

## 10. PlayerSandbox 人工验收

1. 使用 Unity `6000.3.21f1` 打开 `PlayerSandbox`。
2. 确认标准 Probe 初始为 Inactive 的占位颜色/可见状态。
3. 靠近 Probe，确认 Prompt 正确显示。
4. 短按 `E`，确认只切换一次至 Active。
5. 长按 `E`，确认不会重复切换。
6. 松开后再按 `E`，确认仅切换一次回 Inactive。
7. 离开范围后按 `E`，确认状态不变。
8. 用墙体遮挡后按 `E`，确认状态不变；清除遮挡后可再次执行。
9. 运行时销毁 Probe，确认 Prompt 清除且 Console 无异常。
10. 确认 Player Spawn、WASD Movement、Camera、现有 Prompt 测试对象和 ExecutionProbe 均正常。
11. 确认场景没有 Door、Lever、Button、Chest、Bridge、Puzzle、Inventory、Ability、Quest、Save、Network、Animator 或 AudioSource 业务行为。
12. 确认 Console Error = `0`。

## 11. 明确禁止范围

本 Sprint 不实现：

- Door、Lever、Button、Chest、Bridge 或任何正式机关。
- Puzzle、Inventory、Ability、Quest、Save 或 Network。
- Animation System、Audio System、VFX 或 UI 动画。
- 通用 InteractionObject / BaseInteractable / InteractableBase 基类。
- 通用状态机、Gameplay State Framework、Puzzle State System。
- 全局 EventBus、Service Locator、全局静态 Manager。
- 自动触发交互、持续交互、多人槽位、资源消耗或奖励。

## 12. Definition of Done

Sprint002D 只有同时满足以下条件才可标记完成：

- 新 Probe 与现有 `InteractionExecutionProbe` 共存，后者仍为诊断夹具。
- `PF_InteractionProbe` 可通过组件组合接入 Detection → Prompt → Execution。
- 有效两次交互可稳定完成 Inactive → Active → Inactive，长按不重复。
- Visual 只反映已确认的 Probe 状态，未建立全局事件或通用状态框架。
- Sprint002A–C 核心契约和职责保持不变。
- 无 Player、Prompt、Executor、Input、Movement、Camera、程序集、Package 或 Network 边界回归。
- 专项 EditMode、专项 PlayMode、完整回归与 PlayerSandbox 人工验收通过，Console Error = `0`。
- Unity 资源、Prefab、`.meta`、测试、实施报告、Review Checklist 与 CHANGELOG 同步更新。
- Scope 审计证明未进入禁止范围，也未开始 Sprint003。

## 13. 最终状态

# PASSED

计划的 Definition of Done 已满足：Sprint002D 专项 EditMode `8/8`、完整 EditMode `74/74`、专项 PlayMode `8/8`、完整 PlayMode `47/47` 全部通过，Console Error 为 `0`，PlayerSandbox 人工验收通过。

计划已关闭所有实施前架构决策。下一步只能按本计划实现 Standard Interaction Probe；若需改动核心契约、执行链、物理全局配置或新增正式机关，必须重新进行预检。
