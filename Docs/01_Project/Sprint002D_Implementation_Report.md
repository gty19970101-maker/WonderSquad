# Sprint002D Standard Interaction Probe Implementation Report

## 1. 实施结论

- 实施日期：2026-08-08
- Unity 基线：`6000.3.21f1`
- 分支：`feature/sprint002d-interaction-probe`
- 基线：`v0.6-interaction-foundation`
- 实施范围：组合式 Standard Interaction Probe
- 当前状态：**PASSED**

已创建 `PF_InteractionProbe`，用于验证新的 Gameplay Object 能通过现有 Detection → Prompt → Execution 链路接入。对象采用显式组件组合，不建立 `InteractionObject`、`BaseInteractable`、`BaseInteractionObject` 或其他通用继承基类。

现有 `InteractionExecutionProbe` 未被修改，继续作为 Sandbox 和自动化回归的诊断夹具。

## 2. 完成内容

### 2.1 标准组合

```text
PF_InteractionProbe (Interactable Layer)
├── MeshFilter / MeshRenderer
├── BoxCollider (Trigger)
├── InteractionTarget
├── InteractionPromptSource
├── InteractionProbeVisualState
└── InteractionProbeBehaviour
```

- `InteractionTarget`：维持稳定 TargetId、检测 Anchor、检测启用和优先级。
- `InteractionPromptSource`：引用新的静态 `InteractionPrompt_Probe` Definition，仅提供 `Toggle Probe` 提示语义。
- `InteractionProbeBehaviour`：实现 `IExecutableInteraction`；只切换 Active/Inactive，记录执行次数和最近 RequestId，并返回结构化 InteractionResult。
- `InteractionProbeVisualState`：仅按显式调用把已确认状态映射为 Renderer 颜色，不决定或修改交互状态。
- BoxCollider：位于 `Interactable` Layer，保持 Trigger，不阻挡 CharacterController，也不负责自动交互。

### 2.2 状态与执行规则

```text
Inactive
  → 有效 InteractionContext / Success → Active
  → 有效 InteractionContext / Success → Inactive
```

无效 Context 返回 `Unknown`，不可用的 Probe 返回 `Busy`；两种情况都不改变状态、执行次数或最近 RequestId。距离、Layer、遮挡、目标销毁和 RequestId 去重仍由既有 Executor、Validator 和 Request Port 在 Probe 之前处理。

视觉更新是 `InteractionProbeBehaviour` 成功更新局部状态后的直接、显式局部调用。Detector、Prompt、Input 和 Visual 都不会直接修改 Probe 状态。

### 2.3 PlayerSandbox

在 `PlayerSandbox` 中增加了一个 Prefab 实例 `InteractionProbe_Reference`，初始位置远离出生点，避免干扰既有 Character Foundation 与 Interaction 夹具。PlayMode 专项测试会在运行时将它移动至检测范围内并隔离其他测试目标。

## 3. 新增文件

### Runtime

- `Client/Assets/WonderSquad/Runtime/Interaction/Diagnostics/InteractionProbeBehaviour.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Diagnostics/InteractionProbeVisualState.cs`

### Assets

- `Client/Assets/WonderSquad/Prefabs/Interaction/PF_InteractionProbe.prefab`
- `Client/Assets/WonderSquad/ScriptableObjects/Content/Interaction/InteractionPrompt_Probe.asset`
- 对应 `.meta` 文件。

### Tests

- `Client/Assets/WonderSquad/Tests/EditMode/InteractionProbeEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionProbePlayModeTests.cs`
- 对应 `.meta` 文件。

### Documents

- `Docs/01_Project/Sprint002D_Preflight_Report.md`
- `Docs/01_Project/Sprint002D_Implementation_Plan.md`
- `Docs/01_Project/Sprint002D_Implementation_Report.md`
- `Docs/01_Project/Sprint002D_Review_Checklist.md`

## 4. 修改文件

- `Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`
  - 仅增加 `PF_InteractionProbe` 参考实例。
- `CHANGELOG.md`
  - 记录 Sprint002D 已实现内容与待 Unity 验证状态。

未修改 Core Interaction 契约、InteractionExecutor、Detection、Prompt、Player、Input、Movement、Camera、asmdef、Input Actions、Package 或 ProjectSettings。

## 5. 程序集与依赖

没有新增或修改程序集定义。

```text
WonderSquad.Interaction → WonderSquad.Core
WonderSquad.UI          → WonderSquad.Interaction / Player / Core
WonderSquad.Player      → WonderSquad.Core / Unity.InputSystem
```

Probe Runtime 仅位于 `WonderSquad.Interaction`，不引用 Player、UI、Puzzle、Inventory、Ability、Quest、Save、Network 或 Editor。Player 不引用 Probe；Executor 也不引用 Probe 的具体类型或 Prefab 名称。

## 6. 自动化覆盖

### EditMode（新增 8 项）

- 默认 Inactive 与视觉一致性。
- 首次执行切换 Active、执行次数和最近 RequestId。
- 第二次执行恢复 Inactive。
- 无效 Context 的安全结果与无状态变化。
- Prefab 的 Target、Prompt、Visual、执行行为、Collider 和 Layer 完整性。
- Sprint002A–C 核心契约签名保持稳定。
- 新运行时代码无全局查找、Service Locator、禁止领域、Animator 或 AudioSource 依赖。
- 两个 Probe 的 MaterialPropertyBlock 颜色覆盖相互独立，且 Runtime 源码不访问 `Renderer.material`。

### PlayMode（新增 3 项）

- 接近 Probe 后 Detection 与 Prompt 正常；按压一次切换、长按不重复、松开再按恢复。
- 遮挡时拒绝执行；销毁目标后 Prompt 清除且执行安全返回。
- Probe 执行不移动 Player，Main Camera 保持可用。

## 7. 验证结果

- 运行中的 Unity `6000.3.21f1` 已完成脚本导入与编译：`WonderSquad.Interaction.dll`、`WonderSquad.Tests.EditMode.dll` 与 `WonderSquad.Tests.PlayMode.dll` 的生成时间均更新为 2026-08-08 17:51，且程序集大小随新增 Probe 与测试代码变化。
- 修复后的 Interaction、EditMode Tests 与 PlayMode Tests 已使用 Unity `6000.3.21f1` 自带 .NET Roslyn、项目现有 Bee response 参数和系统临时输出目录重新静态编译通过；未覆盖 Unity `Library` 输出。
- 新 Runtime 代码已检查：无 `FindObjectOfType`、`FindFirstObjectByType`、Tag/名称查找、Service Locator、静态全局 Manager、网络 SDK、Animator 或 AudioSource。
- 新脚本、Prefab、ScriptableObject 与测试均已提供 `.meta` 文件；新增 GUID 引用已静态核对。
- 新 Prefab 使用现有 `Interactable` Layer 与已批准的 Trigger 检测约定。
- `git diff --check` 未发现补丁格式错误。

### 7.1 修复前记录

首次 Unity EditMode 验证为 73 项中 69 Passed、4 Failed；失败均位于 `InteractionProbeEditModeTests`。根因是 EditMode 中访问 `Renderer.material`，触发 Unity 的材质实例泄漏保护错误。修复已改为每个 Renderer 独立的 `MaterialPropertyBlock`，详情见 `Sprint002D_EditMode_TestFix_Report.md`。

### 7.2 Unity 最终实测

| 验证项 | 结果 |
| --- | --- |
| Sprint002D InteractionProbe EditMode | `8 Passed / 0 Failed` |
| 完整 EditMode | `74 Passed / 0 Failed` |
| Sprint002D InteractionProbe PlayMode | `8 Passed / 0 Failed` |
| 完整 PlayMode | `47 Passed / 0 Failed` |
| Console | `0 Error` |

PlayerSandbox 人工验收已通过：两个 `PF_InteractionProbe` 均能独立被检测和执行，Prompt、范围/遮挡拒绝、长按抑制、PlayerMovement 与 Camera 均正常。两个 Probe 的 Active/Inactive 颜色互不影响；未发现 shared Material 污染、EditMode material instance leak 或稳定帧持续 `GC.Alloc`。

## 8. Unity 人工验收步骤

1. 用 Unity `6000.3.21f1` 打开项目与 `PlayerSandbox`。
2. 确认 `InteractionProbe_Reference` 来自 `PF_InteractionProbe`，Layer 为 `Interactable`，Collider 为 Trigger。
3. Play 后走近 Probe，确认 Prompt 显示 `Toggle Probe` 及当前设备 Binding。
4. 短按 `E`，确认颜色从 Inactive 变为 Active，执行次数只增加一次。
5. 长按 `E`，确认不会连续切换。
6. 松开后再次按 `E`，确认颜色恢复 Inactive，执行次数再增加一次。
7. 离开范围或使用墙体遮挡后按 `E`，确认状态不改变。
8. 运行时删除 Probe，确认 Prompt 隐藏且 Console 无异常。
9. 确认 WASD、PlayerMovement、Camera、既有 ExecutionProbe 与 Prompt 测试目标继续正常。
10. 运行 Sprint002D 专项与完整 EditMode/PlayMode 回归，确认 Console Error = `0`。

## 9. 已知限制

- Probe 只表达本地两态诊断状态，不是 Door、Lever、Button、Chest 或 Puzzle Object。
- 不实现动画、音效、粒子、UI 动画、自动触发、持续交互、资源消耗或奖励。
- 不实现网络复制、Host Authority、多人争用、状态 Revision 或网络重放缓存。
- 颜色仅为本地占位视觉；未来共享状态必须由所属领域和主机权威决定。
- 未新增通用状态接口、全局事件总线或通用交互基类。

## 10. 推荐 Commit

```text
feat(interaction): add standard interaction probe template
```

## 11. 最终状态

# PASSED

在 Unity `6000.3.21f1` 中，专项 EditMode `8/8`、完整 EditMode `74/74`、专项 PlayMode `8/8`、完整 PlayMode `47/47` 均通过，Console Error 为 `0`。PlayerSandbox 人工验收确认双 Probe 独立视觉状态、交互生命周期及 Character Foundation 回归均正常；Sprint002D 最终通过。
