# Sprint002D Standard Interaction Probe Review Checklist

## 1. Review 信息

- 日期：2026-08-08
- Unity：`6000.3.21f1`
- 分支：`feature/sprint002d-interaction-probe`
- 范围：组合式 Standard Interaction Probe
- 当前状态：**PASSED**

标记：

- `[x]`：代码、资产、Unity Test Runner 或人工验收已确认。
- `[ ]`：待确认。
- `[N/A]`：明确不属于 Sprint002D。

## 2. 范围与组合

- [x] 新增标准 `PF_InteractionProbe`，可独立作为场景参考对象。
- [x] 现有 `InteractionExecutionProbe` 未修改，仍是诊断夹具。
- [x] 未创建 `InteractionObject`、`BaseInteractable`、`BaseInteractionObject` 或其他通用继承基类。
- [x] Prefab 组合 `InteractionTarget`、Prompt Source、执行行为、Visual State 与 Collider。
- [x] 未实现 Door、Lever、Button、Chest、Bridge、Puzzle、Inventory、Ability、Quest、Save 或 Network。
- [x] 未开始 Sprint003。

## 2.1 Unity 编译

- [x] 运行中的 Unity `6000.3.21f1` 已重新生成 Interaction、EditMode Tests 与 PlayMode Tests 程序集。
- [x] 修复后的 Interaction、EditMode Tests 与 PlayMode Tests 已使用同版本 Unity Roslyn 和 Bee 引用参数静态编译通过。
- [x] Unity Test Runner 专项和完整测试通过：专项 EditMode `8/8`、完整 EditMode `74/74`、专项 PlayMode `8/8`、完整 PlayMode `47/47`。
- [x] PlayerSandbox 人工验收通过，Console Error = `0`。

## 3. 契约与执行边界

- [x] `IInteractable`、`IExecutableInteraction`、InteractionContext、InteractionRequest 与 InteractionResult 未修改。
- [x] Probe 通过 `IExecutableInteraction` 返回结构化 InteractionResult。
- [x] Probe 记录 IsActive、ExecutionCount 与 LastRequestId。
- [x] 只有有效 Context 的执行路径切换 Probe 状态。
- [x] 无效 Context 和不可用 Probe 不改变状态。
- [x] Detector、Prompt、Input 和 Visual 不直接改变 Probe 状态。
- [x] Executor、Validator 与 Request Port 的职责未改变。

## 4. Prefab 与视觉

- [x] Prefab Root 是 `Interactable` Layer。
- [x] Prefab 使用 Trigger Collider，不阻挡 CharacterController。
- [x] Target、Prompt、Visual 与 Behaviour 必需引用已序列化。
- [x] Prompt 使用静态 `InteractionPromptDefinition`。
- [x] Visual 只切换颜色，不使用 Animator、Tween、Audio 或 VFX。
- [x] PlayerSandbox 仅增加远离出生点的参考实例。
- [x] Unity 导入及 PlayerSandbox 运行确认 Prefab 无 Missing Script 或 Missing Reference。

## 5. 架构与性能

- [x] Probe Runtime 只位于 WonderSquad.Interaction，且只依赖 Core。
- [x] Player 不引用 Probe；Probe 不引用 Player、UI、Puzzle、Inventory、Ability、Quest、Save、Network 或 Editor。
- [x] Interaction 不引用具体 Probe 类型；Executor 不引用 Probe Prefab 或场景名称。
- [x] 无 FindObject、Tag/名称查找、Service Locator、全局 EventBus 或全局静态 Manager。
- [x] Runtime 不包含 Update 轮询、Animator、AudioSource 或网络 SDK。
- [x] 未新增或修改 asmdef、Package、Input Actions 或 ProjectSettings。

## 6. 自动化测试

### EditMode

- [x] 已创建 8 项 Sprint002D EditMode Tests。
- [x] 默认状态、双态切换、Result、RequestId、无效 Context、Prefab 和契约测试在 Unity Test Runner 通过（`8/8`）。
- [x] 完整 EditMode 回归通过（`74/74`）。

### PlayMode

- [x] 已创建 3 项 Sprint002D PlayMode Tests。
- [x] 接近、Prompt、按压、长按、释放后重按与 Visual 一致性测试通过。
- [x] 遮挡与目标销毁安全测试通过。
- [x] Player 与 Main Camera 回归测试通过。
- [x] Sprint002A–C 和 Character Foundation 完整回归通过。
- [x] 完整 PlayMode 回归通过（`47/47`；Sprint002D 专项 `8/8`）。
- [x] Console Error = `0`。

## 7. Unity 人工验收

- [x] 两个 Probe 默认均为 Inactive，且 Active/Inactive 视觉切换互不影响。
- [x] 靠近后 Prompt 显示正确文字与 Binding。
- [x] 短按 E 只切换一次；长按 E 不重复切换，松开后可再次切换。
- [x] 离开范围或遮挡时不能执行。
- [x] 目标销毁安全由专项 PlayMode 测试覆盖，无 NullReferenceException。
- [x] WASD、PlayerMovement、Camera 与 Prompt 回归正常。
- [x] 未出现 shared Material 污染、material instance leak 或稳定帧持续 `GC.Alloc`；Console Error = `0`。

## 8. 文档与交付

- [x] Preflight、Implementation Plan、Implementation Report 和 Review Checklist 已创建或更新。
- [x] CHANGELOG 已记录新增模板和测试范围。
- [x] 新资源与新脚本具有 `.meta` 文件。
- [x] Unity 实测结果、人工验收和最终状态已回填。

## 9. 最终状态

# PASSED

首次 EditMode 的材质实例泄漏问题已由 `MaterialPropertyBlock` 修复并通过 Unity 实测。专项 EditMode `8/8`、完整 EditMode `74/74`、专项 PlayMode `8/8`、完整 PlayMode `47/47` 均通过，Console Error 为 `0`；Sprint002D 最终通过。
