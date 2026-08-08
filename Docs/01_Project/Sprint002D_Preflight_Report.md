# Sprint002D Interaction Probe Preflight Report

> 实施后回填（2026-08-08）：Sprint002D 已在 Unity `6000.3.21f1` 完成专项、完整回归和人工验收，最终状态为 **PASSED**。本报告保留原始实施前 `GO` 判断及其依据。

## 1. 预检结论

- 预检日期：2026-08-08
- Unity 基线：`6000.3.21f1`
- 当前分支：`feature/sprint002d-interaction-probe`
- 当前版本：`v0.6-interaction-foundation`
- 工作区：干净
- 结论：**GO**

Sprint002C 已建立稳定的检测、提示、输入、执行、重新验证和本地请求端口边界。Sprint002D 可以创建第一个可复用的标准交互对象模板，但其范围必须是一个组合式、可测试的交互对象样板，不是 Door、Chest、Puzzle 或通用机关框架。

## 2. 已核对的基础

| 项目 | 结论 |
|---|---|
| `IInteractable` | 稳定；仅描述检测所需只读信息。 |
| `IExecutableInteraction` | 稳定；仅描述执行可用性与结构化执行结果。 |
| `InteractionExecutor` | 不依赖具体机关或 UI，并在执行前重新验证。 |
| Prompt | 只读消费 Detector 当前目标，不调用执行。 |
| Network 边界 | `IInteractionRequestPort` 已可由未来 Host Authority 端口替换。 |
| 程序集 | Interaction 仅依赖 Core；Player 不反向依赖 Interaction。 |

## 3. ExecutionProbe 的定位

### 3.1 是否升级为正式测试交互对象

**不直接升级。** 现有 `InteractionExecutionProbe` 的职责明确为 PlayerSandbox 与自动化测试的诊断夹具：它同时承担检测、执行计数和颜色切换，用于证明执行链路而非提供正式内容对象语义。

Sprint002D 应保留它的现有文件、名称和回归用途，并新建一个正式的 **Interaction Probe 模板**。新模板可以复用已稳定的 Detection、Prompt 与 Execution 契约，但不得让既有测试夹具演变为未来机关的父类或内容定义来源。

### 3.2 标准对象模板的最小构成

建议第一个模板采用组件组合：

```text
PF_InteractionProbe
├── Collider / 可见占位模型
├── InteractionTarget                 (IInteractable)
├── InteractionPromptSource           (只读提示语义)
└── InteractionProbeBehaviour         (IExecutableInteraction；仅诊断状态)
```

`InteractionProbeBehaviour` 只应在成功执行后更新自身可读的诊断状态，例如计数或两态状态。它不能执行门、宝箱、谜题、库存、任务或网络行为。InteractionTarget、PromptSource 和 Behaviour 必须共享同一稳定 TargetId；该一致性由配置校验和测试保障。

## 4. 契约稳定性

### 4.1 IInteractable

**保持不变。** 其职责仅为发现和排序候选目标。不得加入提示文本、可执行方法、状态、事件、网络字段或具体机关数据。

### 4.2 IExecutableInteraction

**保持不变。** 其职责仅为在已验证的 InteractionContext 下返回 InteractionResult。不得加入 Door、Lever、Button、Puzzle Object 等类型分支，也不得引入 `bool` 结果或 UI 字段。

当未来具体对象确实需要额外能力时，先在其所属领域定义最小领域端口；不能通过膨胀这两个基础交互契约解决。

## 5. 是否需要 InteractionObject 基类

**不需要，且本 Sprint 不应创建。**

原因：

- 当前唯一正式模板尚不足以证明跨 Door、Lever、Button 和 Puzzle Object 的共同运行时行为。
- `InteractionTarget`、`InteractionPromptSource` 和执行行为已可组合，新增基类会强迫不同领域共享生命周期、状态和序列化字段。
- 项目规范要求优先组合，避免为“未来可能扩展”预建空抽象层。

未来对象通过同一 Prefab 组合约定接入：检测组件、提示组件、所属领域的执行组件。若至少两个实际对象出现经验证的、完全相同的生命周期或配置规则，再在独立设计审查中评估抽取共用组件，而不是基类。

## 6. 是否需要 ScriptableObject 交互定义

**本 Sprint 不新增通用 InteractionDefinition ScriptableObject。**

现有 `InteractionPromptDefinition` 已承载静态提示语义；Detection 范围、Layer 与遮挡规则由 `InteractionSettings` 承载。Interaction Probe 的稳定目标 ID、Anchor 和本地诊断状态是场景实例配置或运行时状态，不应进入共享 ScriptableObject。

后续 Door、Lever、Button 或 Puzzle Object 只有在拥有可复用、静态且需版本化的内容数据时，才在其所属领域新增 `DoorDefinition`、`LeverDefinition`、`ButtonDefinition` 或 `PuzzleDefinition`。定义资产只保存静态规则与稳定内容 ID，绝不保存局内开关、计数、占用或权威网络状态。

## 7. 如何避免每个机关重复实现

采用“共享接入协议 + 领域行为组件”而非“万能机关类”：

| 复用层 | 已有或建议组件 | 责任 |
|---|---|---|
| 发现 | `InteractionTarget` | TargetId、Anchor、检测启用、优先级。 |
| 展示 | `InteractionPromptSource` | 静态、可本地化的提示语义。 |
| 请求与校验 | `InteractionExecutor`、Validator、RequestPort | 单次输入、重新验证、幂等与结果。 |
| 具体结果 | 领域执行组件 | 只实现该对象自己的状态转换。 |
| 内容数据 | 领域 Definition | 仅在已有真正静态规则时新增。 |

未来 Door、Lever、Button、Puzzle Object 的实现只替换“具体结果”层，并通过上述稳定协议接入；Interaction 程序集不需要认识这些类型。

## 8. 未来对象支持方向

| 未来对象 | 本 Sprint 预留方式 | 不在本 Sprint 实现 |
|---|---|---|
| Door | 可实现 IExecutableInteraction 的 Door 领域行为组件复用同一请求链。 | 开关、动画、阻挡体、钥匙或权限。 |
| Lever | Lever 领域行为组件可返回结构化结果并发布其领域状态。 | 拉杆状态、连线机关和动画。 |
| Button | Button 行为可复用目标、提示与单次执行边界。 | 压力、持续按住、多人槽位和门联动。 |
| Puzzle Object | Puzzle 领域适配组件可把已验证请求提交给 Puzzle 端口。 | 谜题状态机、解法、奖励与关卡完成。 |

这保证交互入口统一，同时不让 Interaction 域保存或决定其他领域的玩法状态。

## 9. 状态接口

**本 Sprint 不新增通用状态接口。** `IExecutableInteraction.IsExecutionAvailable` 已足够表达能否接受当前请求；InteractionResult 已足够表达单次结果。

Probe 所需的计数或两态诊断信息可以作为其自身的只读属性，仅用于测试和 PlayerSandbox 验证，不应成为跨领域标准状态。未来对象若存在迟加入者必须重建的共享状态，应由该对象所属领域定义带 Revision 的权威状态快照，而不是把状态塞进 `IInteractable` 或 `IExecutableInteraction`。

## 10. 事件通知

**不新增全局事件总线，也不在本 Sprint 增加通用机关事件。**

现有 `InteractionExecutor.InteractionResolved` 已可表达一次请求已获得结构化结果；Probe 本身不需要向 Door、Puzzle、UI 或 Level 广播事件。未来领域状态改变时，由领域发布命名明确、可携带稳定 ID/Revision 的实例事件或通过端口供外层编排；短暂表现不能作为权威状态来源。

## 11. Network Authority 边界

不改变边界：

```text
Local Player Intent
→ InteractionExecutor
→ IInteractionRequestPort
→ LocalInteractionRequestPort (Sprint002D 单机)
→ IExecutableInteraction
```

Interaction Probe 只实现一个可替换端口后的本地执行目标，不增加 RPC、Fusion、NetworkObject、Host 逻辑或客户端权威状态。未来网络版本由 Host 请求端口重做目标状态、距离、权限、Revision 与 RequestId 去重校验；客户端 Probe 的本地状态不得成为共享结果来源。

## 12. 对既有系统的影响

| 系统 | 影响 | 约束 |
|---|---|---|
| Player | 无程序集或代码依赖变化。 | 不修改 Input、Movement、Spawn 或 Camera。 |
| Prompt | 无行为变化。 | 只继续读取 InteractionTarget 提供的当前目标与 PromptSource 定义。 |
| Executor | 无职责变化。 | 不加入对 Probe 类型、Prefab 名称或场景对象名称的引用。 |
| Interaction | 新增模板行为仅依赖 Core 契约。 | 不反向依赖 UI、Player、Puzzle 或 Network。 |

## 13. 建议实现范围与文件

后续实施只应在确认以下最小清单后进行：

- `Runtime/Interaction/Diagnostics/InteractionProbeBehaviour.cs`：标准测试对象的最小执行行为；不实现正式机关。
- `Prefabs/Interaction/PF_InteractionProbe.prefab`：由既有 Target、PromptSource 与新 Behaviour 组成。
- 必要的 Prompt Definition 资产：仅复用或增加测试用静态提示语义。
- `Tests/EditMode/InteractionProbeEditModeTests.cs`：配置、TargetId 一致性、契约与禁止依赖检查。
- `Tests/PlayMode/InteractionProbePlayModeTests.cs`：检测、提示、单次执行、拒绝路径与现有系统回归。
- `Docs/01_Project/Sprint002D_Implementation_Plan.md`：先定义具体状态、数据、Prefab 结构和验收，不在预检阶段写代码。

现有 `InteractionExecutionProbe`、`IInteractable`、`IExecutableInteraction`、Input Actions、程序集定义和 Network 适配层均不应作为 Sprint002D 的修改目标，除非 Implementation Plan 明确提出并重新预检。

## 14. 自动化与人工验收建议

### EditMode

- Probe Prefab 包含且只包含预期的标准交互组件。
- TargetId、Detection Anchor、Prompt Definition 和执行行为配置完整且稳定。
- Probe 的检测目标与执行目标 ID 一致。
- Probe 不引用 Door、Chest、Puzzle、Inventory、Quest、Network、UI 或 Player 类型。
- `IInteractable` 和 `IExecutableInteraction` 签名未变化。

### PlayMode

- 进入范围后 Prompt 显示，离开或遮挡后隐藏。
- 单次按压只改变 Probe 一次；长按不重复；松开后可再次执行。
- 无目标、超距、遮挡、禁用或销毁目标返回安全的 InteractionResult。
- Probe 不会移动 Player，不改变 Camera，不写入 Prompt，也不影响现有 ExecutionProbe 回归。

### 人工验收

在 PlayerSandbox 中放置一个标准 Interaction Probe：确认提示语义、执行反馈、目标切换和拒绝路径正常；确认 Console Error = 0，且没有 Door、Chest、Puzzle、Inventory、Quest 或网络行为出现。

## 15. 风险与后续决策

- 过早抽象基类会把 Door、Lever、Button 和 Puzzle Object 的不同状态机耦合在一起；本 Sprint 通过组件组合规避。
- 若 Probe 的实例 TargetId 被复制后重复，未来必须通过内容/场景校验器检测；Sprint002D 至少应测试配置一致性。
- 同一对象同时有 InteractionTarget 和执行组件时，必须确保其共享 TargetId，不能依赖 GameObject 名称。
- Renderer 颜色、日志或计数只能是本地诊断表现；后续机关的共享状态必须由权威端决定。
- 网络幂等缓存的乱序/重放窗口仍是未来 Network Sprint 的职责，不在当前本地 Probe 扩张。

## 16. 最终结论

# PASSED

实施前 `GO` 条件已全部满足，并已由 Unity 实测确认。Probe 仍仅为标准测试交互对象模板，不扩展为具体机关或跨系统玩法。
