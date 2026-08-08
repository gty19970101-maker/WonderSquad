# Sprint002C Interaction Execution Implementation Report

## 1. 实施结论

- 实施日期：2026-08-02
- Unity 验收日期：2026-08-08
- Unity 基线：`6000.3.21f1`
- 分支：`feature/sprint002c-interaction-execution`
- 基线提交：`3633775`（`v0.5-interaction-prompt`）
- 实施范围：本地单机 Interaction Execution
- 当前状态：**PASSED**

Sprint002C 已形成以下本地执行链路：

```text
PlayerInteractionInputReader
        ↓ 单次 Press
InteractionExecutor
        ↓ Detector 当前明确目标
InteractionExecutionValidator
        ↓ 结构化请求
IInteractionRequestPort
        ↓
LocalInteractionRequestPort
        ↓
IExecutableInteraction
        ↓
InteractionResult
```

代码、测试与 Unity 资产已经完成，并已在 Unity `6000.3.21f1` 中通过专项测试、完整回归和 PlayerSandbox 人工验收。Sprint002C 已达到本地单机 Interaction Execution 的验收标准。

## 2. 完成内容

### 2.1 契约与数据

- 保持 `IInteractable` 原文件不变，Detection 与 Execution 继续分离。
- 新增 `IExecutableInteraction`：
  - 提供稳定 TargetId。
  - 提供只读执行可用性。
  - 接收 InteractionContext。
  - 返回 InteractionResult，不返回 bool。
- 新增 PlayerId 与 RequestId：
  - 使用只读值语义。
  - `0` 为 Invalid。
  - 不依赖 GameObject 名称、Instance ID 或临时字符串。
- 新增 InteractionContext：
  - PlayerId
  - RequestId
  - RequestTimeSeconds
  - InteractionOrigin
  - InteractionDirection
  - ExpectedTargetRevision
- 新增 InteractionRequest，将 Context 与 TargetId 组合为明确命令。
- 新增 InteractionResult 与 InteractionResultCode：
  - Unknown
  - Success
  - TargetInvalid
  - OutOfRange
  - Occluded
  - LayerRejected
  - Busy
  - Cancelled
  - NotSupported

所有 Core 契约均不包含 UI、Input System、场景名称、Photon 或 Fusion 类型。

### 2.2 独立交互输入

`PlayerInteractionInputReader`：

- 复用现有 `Gameplay/Interact` Button。
- 键盘绑定保持 `<Keyboard>/e`。
- 手柄绑定保持 `<Gamepad>/buttonWest`。
- 使用 performed/canceled 形成 Press/Release 状态机。
- 一次按下只发布一次事件。
- 长按不重复触发。
- Disable 时取消订阅、释放 Runtime Action 并清除 Held 状态。
- Re-enable 后重新建立单一订阅。
- 不读取 Detector，不执行目标，不引用 UI，不修改 PlayerMovement。

现有 `PlayerInputReader` 和 `Gameplay/Move` 均未修改。

### 2.3 InteractionExecutor

Executor 通过 Player Prefab 显式引用：

- IInteractionInputSource
- InteractionDetector
- IPlayerIdentitySource
- Interaction Origin
- IInteractionRequestPort

它只执行以下编排：

1. 接收 Press 或明确 InteractionRequest。
2. 读取 Detector 已选中的当前目标，不执行目标发现。
3. 创建唯一 RequestId 和不可变 Context。
4. 重新验证目标。
5. 将验证通过的请求提交到端口。
6. 保存并发布结构化 Result。

Executor 不使用范围查询、场景搜索、Keyboard.current、Gamepad.current、UI、Prompt、PlayerMovement 或具体机关类型。

### 2.4 执行前重新验证

`InteractionExecutionValidator` 对一个明确目标重新检查：

1. Context、PlayerId、RequestId 和 TargetId 有效。
2. Detection 与 Execution 对象仍存活。
3. Detection TargetId、Execution TargetId 与 Request TargetId 一致。
4. Detection 仍启用。
5. Layer 仍属于 Interaction Layer Mask。
6. 当前 Authority Origin 到目标仍在 Detection Radius 内。
7. 使用既有 Raycast Layer Mask 与 Trigger 设置重新检查遮挡。
8. 执行契约仍可用。

Validator 不运行 Overlap 查询，不寻找或切换候选目标。若按键目标 A 已变化为 B，本次请求拒绝，不静默执行 B。

### 2.5 本地请求端口与幂等

`LocalInteractionRequestPort`：

- 实现 IInteractionRequestPort。
- 接收已验证的 Request 与执行契约。
- 基于 PlayerId + RequestId 缓存最近结果。
- 重复请求返回原结果，不再次调用目标。
- 端口禁用返回 Cancelled。
- 目标不可用返回 Busy。
- 目标 ID 不一致返回 TargetInvalid。
- 目标返回错误 RequestId 或 TargetId 时规范化为 Unknown。

本地端口不包含 RPC、Server、Client、NetworkObject 或 Fusion。未来 Host Authority Adapter 可以在解析权威目标后替换此端口语义。

### 2.6 ExecutionProbe

`InteractionExecutionProbe` 同时实现：

- IInteractable
- IExecutableInteraction

允许的诊断行为：

- 成功执行计数。
- 诊断布尔状态切换。
- 占位 Renderer 颜色切换。
- 记录最后 RequestId 与 PlayerId。
- 返回配置化结构结果。

Probe 只存在于 Diagnostics、Sandbox 和测试语义中，没有 Door、Lever、Chest、Bridge、Puzzle、Inventory、Ability、Network、奖励或存档逻辑。

### 2.7 Prefab 与 Sandbox

Player Prefab 新增：

- LocalPlayerIdentity
- PlayerInteractionInputReader
- LocalInteractionRequestPort
- InteractionExecutor

PlayerSandbox：

- 将最近的 Interaction 测试目标替换为 ExecutionProbe。
- Prompt Source 保持只读。
- Probe 成功执行时计数与占位颜色变化。
- 第二个 Prompt 测试目标继续保持 Detection/Prompt-only。

没有修改 PlayerSpawner、Movement、Camera 或 Prompt Runtime。

## 3. 新增文件

### Core

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
- 对应目录与 `.meta`

### Player

- `Client/Assets/WonderSquad/Runtime/Player/Identity/LocalPlayerIdentity.cs`
- `Client/Assets/WonderSquad/Runtime/Player/Input/InteractionInputConfigurationValidator.cs`
- `Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInteractionInputReader.cs`
- 对应目录与 `.meta`

### Interaction

- `Client/Assets/WonderSquad/Runtime/Interaction/Execution/InteractionRequestIdGenerator.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Execution/InteractionExecutionValidator.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Execution/LocalInteractionRequestPort.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Execution/InteractionExecutor.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Diagnostics/InteractionExecutionProbe.cs`
- 对应目录与 `.meta`

### Tests

- `Client/Assets/WonderSquad/Tests/EditMode/InteractionExecutionEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionExecutionPlayModeTests.cs`
- 对应 `.meta`

### Documents

- `Docs/01_Project/Sprint002C_Preflight_Report.md`
- `Docs/01_Project/Sprint002C_Implementation_Plan.md`
- `Docs/01_Project/Sprint002C_Implementation_Report.md`
- `Docs/01_Project/Sprint002C_Review_Checklist.md`

## 4. 修改文件

- `Client/Assets/WonderSquad/Prefabs/Player/Player.prefab`
- `Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionDetectionPlayModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionPromptPlayModeTests.cs`
  - 仅增加 ExecutionProbe 的测试隔离清理。
- `Docs/01_Project/Sprint002C_Preflight_Report.md`
- `CHANGELOG.md`

## 5. Input Actions 变化

**无变化。**

- 复用现有 `Gameplay/Interact`。
- 键盘继续为 `E`。
- 手柄继续为 `buttonWest`。
- Move、Camera 和其他 Action 未修改。
- Sprint002B Binding 显示与本阶段实际输入继续来自同一 Input Actions 资产。

## 6. 程序集变化

**Runtime 与 Tests asmdef 均无变化。**

依赖方向保持：

```text
WonderSquad.Player      → WonderSquad.Core
WonderSquad.Interaction → WonderSquad.Core
WonderSquad.UI          → WonderSquad.Interaction / Player / Core
```

Player 未引用 Interaction 或 UI；Interaction 未引用 Player、UI 或 Network；没有循环依赖。

## 7. 验证结果

### 7.1 编译

使用 Unity `6000.3.21f1` 自带 Roslyn 编译器和项目现有 Bee response 参数，按程序集顺序验证：

| Assembly | 结果 |
|---|---|
| WonderSquad.Core | 静态编译通过 |
| WonderSquad.Player | 静态编译通过 |
| WonderSquad.Interaction | 静态编译通过 |
| WonderSquad.Tests.EditMode | 静态编译通过 |
| WonderSquad.Tests.PlayMode | 静态编译通过 |

Unity Editor 已完成真实导入和测试执行，Console Error 为 `0`。

### 7.2 EditMode

- Sprint002C EditMode Tests：**11 Passed，0 Failed**。
- 完整 EditMode：**66 Passed，0 Failed**。

### 7.3 PlayMode

- Sprint002C PlayMode Tests：**7 Passed，0 Failed**。
- 完整 PlayMode：**44 Passed，0 Failed**。

### 7.4 完整回归

- PlayerMovement：人工回归正常。
- Camera：人工回归正常。
- Interaction Prompt：人工回归正常。
- Detection、Character Foundation 与既有测试包含在完整回归中，全部通过。
- Console Error：`0`。

### 7.5 人工验收

- ExecutionProbe 可正常执行。
- 单次按压只执行一次；长按不会重复执行；松开后可再次执行。
- 无目标、超距、遮挡和目标销毁场景均安全拒绝执行。
- Prompt 目标与执行目标一致。
- InteractionResult 正常返回结构化结果。
- PlayerMovement、Camera 与 Prompt 回归正常。
- Console Error = `0`。

## 8. 自动化覆盖

EditMode 已覆盖：

- Detection 与 Execution 契约分离。
- Context 合法性与方向归一化。
- PlayerId/RequestId 零值规则。
- RequestId 唯一与溢出跳零。
- Request/Result 结构。
- ResultCode 完整性。
- RequestId 去重。
- 距离、Layer、遮挡与 NotSupported 映射。
- Interact Button 与键鼠/手柄 Binding。
- Player Prefab 执行组合。
- Executor 禁止场景搜索、范围查询、设备读取、Movement 与 Prompt 引用。

PlayMode 已覆盖：

- 键盘按压一次、长按不重复、释放后重置。
- 手柄单次执行。
- 无目标、NotSupported 与销毁。
- 距离、Layer 与遮挡重新验证。
- Input Reader 与 Executor 启停。
- 重复请求不重复执行。
- Player 不移动，Prompt 目标保持只读一致。

## 9. Unity 人工验收步骤

1. 使用 Unity `6000.3.21f1` 打开 `PlayerSandbox`。
2. Play 后靠近 `InteractionTestTarget` ExecutionProbe。
3. 确认 Prompt 正常显示。
4. 短按 `E`，Probe 颜色/状态只变化一次。
5. 长按 `E`，确认不会持续变化。
6. 松开后再次按 `E`，确认再次变化一次。
7. 使用手柄西侧按钮重复单次触发检查。
8. 离开 Detection Radius 后按键，确认不执行。
9. 用墙体遮挡后按键，确认不执行。
10. 绕开墙体后，确认执行恢复。
11. 修改目标 Layer 为非 Interactable 后确认不执行。
12. 运行时删除 Probe，按键不产生异常。
13. 在两个目标之间切换，只执行当前 Prompt 对应目标。
14. 禁用 PlayerInteractionInputReader 后不执行，重新启用后恢复。
15. 禁用 InteractionExecutor 后不执行，重新启用后恢复。
16. 确认 WASD、Camera 与 Prompt 行为正常。
17. 确认场景没有正式机关、Inventory、Puzzle 或 Network 行为。
18. Console Error = `0`。
19. Profiler 稳定帧无持续 Interaction Execution `GC.Alloc`。
20. 运行新增专项测试和完整 EditMode/PlayMode 回归。

## 10. 已知限制

- 当前只有一个本地 PlayerId 来源。
- Local Request Port 为同步单机适配器。
- 本地幂等缓存只保存最近一个 PlayerId + RequestId 结果。
- ExpectedTargetRevision 当前固定为 `0`，尚无权威目标 Revision。
- Context Time 是本地 unscaled time，不是 Network Time。
- 不实现持续交互、多人参与槽位或争用。
- 不实现 Result UI、动画或音效。
- ExecutionProbe 不是正式机关。
- 未实现任何网络传输或 Host Authority。

## 11. 范围审计

- `IInteractable.cs`：无修改。
- Input Actions：无修改。
- Runtime/Test asmdef：无修改。
- Packages：无修改。
- PlayerInputReader：无修改。
- PlayerMovement：无修改。
- Camera Runtime：无修改。
- Prompt Runtime/View：无修改。
- 没有 Door、Lever、Chest、Bridge、Puzzle、Inventory、Ability 或 Network 实现。
- 没有全局静态 Manager 或全局事件总线。
- Runtime 新代码没有 Update 循环、LINQ、每帧组件查找或设备直读。

## 12. 推荐 Commit

```text
feat(interaction): implement local interaction execution
```

## 13. 最终状态

# PASSED

Sprint002C 已在 Unity `6000.3.21f1` 中通过 11/11 专项 EditMode、7/7 专项 PlayMode、66/66 完整 EditMode、44/44 完整 PlayMode 与 PlayerSandbox 人工验收。未发现阻塞 Sprint Gate 的问题。
