# Sprint001-B Player Input Reader 实施报告

## 1. Sprint 目标

在不实现角色移动的前提下，建立稳定、可测试、与具体输入设备读取解耦的本地玩家二维移动输入层。

Unity 基线：

```text
6000.3.21f1
```

分支：

```text
feature/sprint001-character-controller-v2
```

当前状态：

**IMPLEMENTED — UNITY TEST RUN PENDING**

代码已使用 Unity 6000.3.21f1 自带编译器和工程实际程序集引用完成编译验证。由于主工程正被另一个 Unity 实例打开，批处理不能打开主工程；隔离工程又在测试执行前被本机 Unity Licensing IPC 阻塞，因此 EditMode、PlayMode 断言与人工场景验证仍需在当前主 Editor 中执行。

## 2. 已实现范围

- WASD 二维移动输入。
- 方向键二维移动输入。
- 复用现有 `WonderSquad.inputactions` 的 Gameplay/Move Action。
- 输入读取、非有限值防护和单位圆限幅。
- 启用、禁用、重新启用生命周期。
- 对外只读 `Vector2 MoveInput`。
- 输入变化事件 `MoveInputChanged`。
- PlayerSandbox Console 调试日志。
- EditMode 与 PlayMode 测试代码。

## 3. 设计与职责

### 3.1 MoveInputValue

职责：

- 把二维输入限制在单位圆内。
- 保持零输入为零。
- 把 NaN 或 Infinity 等无效值安全降级为零。

这是普通静态 C# 规则，不读取设备、不访问场景、不移动 Player。

### 3.2 PlayerInputConfigurationValidator

Move Action 必须满足：

- 引用存在并解析到 Action。
- Action 类型为 `Value`。
- Expected Control Type 为 `Vector2`。
- 至少存在一个 Binding。

无效配置会返回明确错误，而不是在运行中持续抛出异常。

### 3.3 PlayerInputReader

职责：

- 通过序列化 `InputActionReference` 接收批准的 Move Action。
- 创建该 Action 的独立运行时 Clone，避免修改共享 Input Actions 资产的启用状态。
- 在 `OnEnable` 中订阅并启用。
- 在 `OnDisable` 中取消订阅、禁用并把输入归零。
- 在 `OnDestroy` 中释放运行时 Action。
- 对外提供只读 `MoveInput` 和值变化事件。

Reader 不包含 `Update`、`FixedUpdate` 或 `LateUpdate`，也不直接访问 `Keyboard.current`。

### 3.4 PlayerInputDebugLogger

职责：

- 订阅 `MoveInputChanged`。
- 仅在输入值实际变化时向 Unity Console 输出坐标和长度。
- 在禁用时取消订阅。

它不轮询设备，不修改输入值，也不依赖 WonderSquad UI 模块。

## 4. Input Actions 变化

复用资产：

```text
Client/Assets/WonderSquad/Settings/Input/WonderSquad.inputactions
```

未创建第二套 Action Asset。Gameplay/Move 保留：

- Action Type：`Value`
- Expected Control Type：`Vector2`
- 原 WASD Composite
- 原 Gamepad Left Stick

本 Sprint 只新增：

- Arrow Keys 2DVector Composite
- Up Arrow
- Down Arrow
- Left Arrow
- Right Arrow

Reader 始终执行单位圆限幅，因此 W+D 等对角输入长度不超过 `1`。

## 5. Prefab 与 Sandbox

修改：

```text
Client/Assets/WonderSquad/Prefabs/Player/Player.prefab
```

Player 根节点新增：

- `PlayerInputReader`
- `PlayerInputDebugLogger`

`PlayerInputReader` 指向现有 Gameplay/Move Action。Debug Logger 显式引用同一根节点的 Reader。

没有修改：

- Player Prefab 层级。
- Transform。
- VisualRoot 或 VisualPlaceholder。
- PlayerSandbox Scene 结构。
- PlayerSpawner。
- PlayerSpawnPoint。

PlayerSandbox 继续通过 Sprint001-A 的本地 Spawner 创建 Player；Prefab 实例自动具备输入读取和 Console 调试能力。

## 6. 程序集变化

`WonderSquad.Player` 新增：

```text
Unity.InputSystem
```

Player 没有新增任何 WonderSquad Gameplay、UI、Network 或 Editor 程序集依赖。`Unity.InputSystem` 是本阶段批准的 Unity 输入基础设施依赖。

PlayMode 测试程序集新增：

```text
Unity.InputSystem
Unity.InputSystem.TestFramework
```

用于模拟输入设备和按键，不进入 Player Build。

## 7. 测试覆盖

### 7.1 EditMode

文件：

```text
Client/Assets/WonderSquad/Tests/EditMode/PlayerInputReaderEditModeTests.cs
```

覆盖：

- 零输入保持为零。
- 超长轴输入限制到单位长度。
- 对角输入归一化到单位圆。
- NaN/Infinity 输入安全归零。
- 缺少 Action Reference 时配置无效。
- Action Type 或 Control Type 不合法时配置无效。
- 正式 Move Action 配置合法。
- 正式 Move Action 同时包含 WASD 和方向键绑定。
- Player Prefab 的 Reader 引用有效且包含调试 Logger。

### 7.2 PlayMode

文件：

```text
Client/Assets/WonderSquad/Tests/PlayMode/PlayerInputReaderPlayModeTests.cs
```

覆盖：

- 启用后接收 Input System 模拟输入。
- W+D 输入两个轴均为正且长度不超过 `1`。
- 输入期间 Player Transform 位置不变。
- 松开后恢复为零。
- 禁用后立即归零且不再接收输入。
- 重新启用后方向键输入继续生效。
- 多次启停后一次输入只产生一次非零变化通知。

## 8. 验证结果

| 验证项 | 状态 | 证据 |
|---|---|---|
| Unity 版本 | 已静态确认 | `ProjectVersion.txt` 为 `6000.3.21f1` |
| Input Actions JSON | 通过 | JSON 可解析 |
| Player asmdef JSON | 通过 | JSON 可解析 |
| PlayMode asmdef JSON | 通过 | JSON 可解析 |
| Runtime 编译 | 通过 | Unity 6000.3.21f1 Roslyn + 实际 Player rsp/引用 |
| EditMode 测试程序集编译 | 通过 | Unity 6000.3.21f1 Roslyn + 实际 EditMode rsp/引用 |
| PlayMode 测试程序集编译 | 通过 | Unity 6000.3.21f1 Roslyn + 实际 PlayMode rsp/引用 |
| 相关 EditMode Tests 执行 | 待主 Editor 验证 | 批处理受项目锁/License IPC 阻塞 |
| 相关 PlayMode Tests 执行 | 待主 Editor 验证 | 批处理受项目锁/License IPC 阻塞 |
| PlayerSandbox 人工验证 | 待执行 | 步骤见第 10 节 |

本次没有把“编译成功”误记为“测试断言已通过”。

## 9. 新增和修改文件

### 新增

```text
Client/Assets/WonderSquad/Runtime/Player/Input.meta
Client/Assets/WonderSquad/Runtime/Player/Input/MoveInputValue.cs
Client/Assets/WonderSquad/Runtime/Player/Input/MoveInputValue.cs.meta
Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInputConfigurationValidator.cs
Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInputConfigurationValidator.cs.meta
Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInputReader.cs
Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInputReader.cs.meta
Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInputDebugLogger.cs
Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInputDebugLogger.cs.meta
Client/Assets/WonderSquad/Tests/EditMode/PlayerInputReaderEditModeTests.cs
Client/Assets/WonderSquad/Tests/EditMode/PlayerInputReaderEditModeTests.cs.meta
Client/Assets/WonderSquad/Tests/PlayMode/PlayerInputReaderPlayModeTests.cs
Client/Assets/WonderSquad/Tests/PlayMode/PlayerInputReaderPlayModeTests.cs.meta
Docs/01_Project/Sprint001B_Implementation_Report.md
Docs/01_Project/Sprint001B_Review_Checklist.md
```

### 修改

```text
CHANGELOG.md
Client/Assets/WonderSquad/Prefabs/Player/Player.prefab
Client/Assets/WonderSquad/Runtime/Player/WonderSquad.Player.asmdef
Client/Assets/WonderSquad/Settings/Input/WonderSquad.inputactions
Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef
```

## 10. Unity 人工测试步骤

1. 在 Unity `6000.3.21f1` 等待资源导入和脚本编译完成。
2. 确认 Console 没有编译 Error 或 Missing Script。
3. 打开 `Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`。
4. 进入 Play Mode，确认只生成一个 Player 胶囊。
5. 记录 Player 根节点的 Transform Position。
6. 按 W、A、S、D，观察 Console 的 `Player Move Input` 日志变化。
7. 分别按四个方向键，确认日志方向正确。
8. 松开所有按键，确认最后输入日志恢复为 `(0.00, 0.00)`。
9. 同时按 W+D，确认日志中的 Magnitude 不超过 `1.00`。
10. 确认 Player Transform Position 始终与步骤 5 相同，胶囊不得移动。
11. 在 Player 实例 Inspector 中禁用 `PlayerInputReader`，确认输入归零；重新启用后再次按键，确认可继续读取。
12. 确认 Console Error 为 `0`。
13. 在 Test Runner 运行：
    - `WonderSquad.Tests.EditMode.PlayerInputReaderEditModeTests`
    - `WonderSquad.Tests.PlayMode.PlayerInputReaderPlayModeTests`
14. 再运行完整 EditMode 与 PlayMode 回归。

## 11. 明确未实现

- Transform 移动。
- CharacterController。
- 旋转、重力、Ground Check 或跳跃。
- Camera Follow。
- 动画。
- Interaction。
- Inventory。
- Ability。
- Puzzle。
- Network 或 Photon Fusion。
- Sprint001-C Movement。

## 12. 风险与后续门禁

- 合并前必须取得相关 EditMode、PlayMode 和完整回归的真实 Unity Test Runner 结果。
- 合并前必须完成 PlayerSandbox 键盘人工验证并确认 Console Error 为 `0`。
- 当前 Console 调试组件属于原型可观测性；后续正式表现层建立后应由批准的调试视图替代或移除，不能演变为 Gameplay/UI 耦合。
- 未来 Network Adapter 只读取输入意图，不得让本 Reader 直接引用 Fusion 类型。

## 13. 推荐 Commit 信息

```text
feat(player): add sprint001B input reader
```

建议正文：

```text
Reuse the approved Gameplay Move action for WASD and arrow keys.
Clamp move input and keep enable/disable subscriptions deterministic.
Do not apply movement or reference CharacterController, camera, or network code.
```

本次工作到此停止，不开始 Sprint001-C Movement。
