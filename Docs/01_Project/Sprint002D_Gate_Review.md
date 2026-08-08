# Sprint002D Standard Interaction Probe Gate Review

## 1. Gate 结论

- 审查日期：2026-08-08
- Unity：`6000.3.21f1`
- 分支：`feature/sprint002d-interaction-probe`
- 前置版本：`v0.6-interaction-foundation`
- Sprint002D 状态：**PASSED**

# GO

标准 Interaction Probe 已验证可在不改动 Interaction Foundation 的前提下，以组件组合接入 Detection → Prompt → Execution。进入真实 Gameplay Object 的设计与实施前，不需要为本 Sprint 进行重构。

## 2. 验证证据

| 项目 | Unity 实测结果 |
| --- | --- |
| Sprint002D EditMode | `8 Passed / 0 Failed` |
| 完整 EditMode | `74 Passed / 0 Failed` |
| Sprint002D PlayMode | `8 Passed / 0 Failed` |
| 完整 PlayMode | `47 Passed / 0 Failed` |
| Console | `0 Error` |
| PlayerSandbox | 双 Probe 独立检测、提示、执行与视觉切换均通过 |

人工验收确认：长按不会重复执行；离开范围或遮挡时无法执行；PlayerMovement 与 Camera 无回归；稳定状态未发现持续 `GC.Alloc`。

## 3. Gate 检查

| 检查项 | 结论 | 依据 |
| --- | --- | --- |
| 保持组件组合，无通用交互基类 | 通过 | `PF_InteractionProbe` 由 `InteractionTarget`、Prompt Source、`InteractionProbeBehaviour`、`InteractionProbeVisualState` 与 Trigger Collider 组成；未创建 `InteractionObject`、`BaseInteractable` 或同类继承模板。 |
| `InteractionExecutionProbe` 仍为诊断夹具 | 通过 | 原夹具未升级、未改作模板父类或内容定义来源；标准 Prefab 使用新 Probe Behaviour。 |
| Prefab 可独立复用 | 通过 | Prefab 带齐检测 Layer、Trigger Collider、Target、Prompt、执行与视觉组件；拖入 Sandbox 后只需配置自身内容字段。 |
| 第二个 Probe 无需修改 Foundation | 通过 | 人工验收中两个实例均通过同一 Detection、Prompt、Input 与 Executor 链路独立工作，未改动 Interaction Foundation。 |
| MaterialPropertyBlock 不污染共享材质 | 通过 | Visual State 为每个 Renderer 使用独立 `MaterialPropertyBlock` 写入颜色属性，不访问 `Renderer.material` 或写入 `sharedMaterial`；修复后 EditMode 和人工检查均无实例泄漏或共享污染。 |
| 多 Probe 视觉与逻辑状态独立 | 通过 | 每个 Behaviour 保存自身 Active/执行记录，每个 Visual State 保存自身 PropertyBlock；专项 EditMode 多实例测试及人工双 Probe 验证均通过。 |
| 核心契约保持不变 | 通过 | `IInteractable`、`IExecutableInteraction`、`InteractionContext`、`InteractionRequest`、`InteractionResult` 未因 Sprint002D 修改。 |
| Detector / Prompt / Executor 无反向耦合 | 通过 | Detector 只发现目标；Prompt 仅展示只读数据；Executor 经既有请求端口重新验证后调用执行契约。三者均不引用具体 Probe 类型，Probe 也不依赖 UI 或 Player。 |
| 无稳定帧持续分配 | 通过 | Probe 没有 `Update` 轮询；PropertyBlock 首次使用后复用。人工 Profiler 检查未发现稳定状态持续 `GC.Alloc`。 |

## 4. 架构与网络边界

Probe 的状态只在 `InteractionExecutor → Request Port → IExecutableInteraction` 的既有执行路径中变化。Detector、Prompt、Input 与 Visual 不可绕过该路径修改状态。当前仍是本地单机实现；未来网络接入应替换或适配 Request Port 的权威提交方式，而不是让 Probe、Prompt 或 Detector 直接同步状态。

`WonderSquad.Interaction` 没有反向引用 Player、UI 或 Gameplay 内容程序集。Player 也未引用 Interaction 或 Probe，符合 `Technical_Architecture_v1.1.md` 的依赖方向。

## 5. 进入真实 Gameplay Object 前的约束

不需要重构，但 Door、Lever、Button 或 Puzzle Object 必须各自经过设计与预检，并遵守以下已验证约定：

- 继续复用最小契约和既有 Executor 重新验证链路；不为内容对象扩展通用交互基类。
- 以显式 Inspector 引用组合目标、提示、执行行为及对象自身的最小视觉/玩法反馈。
- 仅在有多个已验证对象共享相同行为时，另行审查小型共用组件；不得据此预先引入通用状态机、EventBus 或跨领域 Manager。
- 网络权威、持久化、谜题编排和奖励逻辑仍不属于 Probe 模板的职责。

## 6. 范围审计

本 Sprint 没有引入 Door、Lever、Button、Chest、Bridge、Puzzle、Inventory、Ability、Quest、Save、Network、动画系统、音效系统、通用状态机、全局 EventBus 或全局 Manager。没有修改 Sprint002A–C 核心契约、Detector、Executor、Prompt Runtime、Player、Input Actions、程序集边界或 Package 配置。

静态审计发现既有诊断夹具 `InteractionExecutionProbe` 中仍有历史 `Renderer.material` 访问。它不是 Sprint002D 新模板的代码路径，未在本 Sprint 修改，且本次 Unity 完整回归、Console 与双 Probe 验收均通过。为保持既定范围，本 Gate 不修改该旧夹具；若后续需要在 EditMode 中扩展该夹具的视觉测试，应单独建立维护任务，评估同样迁移到 `MaterialPropertyBlock`。这不是进入真实 Gameplay Object 阶段前的重构阻塞项。

## 7. 最终结论

# GO

Sprint002D 已作为真实 Gameplay Object 前的标准组合式交互对象参考通过 Gate。当前无需重构；停止于 Sprint002D，不开始 Sprint003。
