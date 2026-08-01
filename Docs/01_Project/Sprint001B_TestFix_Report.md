# Sprint001B Player Input Reader 测试修复报告

## 1. 修复范围

- Sprint：Sprint001B — Player Input Reader
- Unity：6000.3.21f1
- 目标：修复 `PlayerInputReaderPlayModeTests` 的两项失败
- 范围约束：仅调整玩家输入读取层，不实现或修改 Movement、Camera、CharacterController、Interaction、Inventory、Ability、Puzzle、Network

## 2. 原始失败与原因

### 2.1 DisabledReader_ClearsInputAndReceivesInputAfterReenable

- 原始结果：期望 `(0, 1)`，实际为 `(1, 0)`。
- 直接原因：Unity 的多帧 PlayMode 测试会将 `Press` 和 `Release` 输入事件排队到下一次 PlayerLoop。组件重新启用时，`Value` 类型 Action 的初始状态检查先读取到了禁用期间尚未处理完成的 D 键旧状态。
- 生命周期问题：原实现禁用 Action 后仍保留运行时 Action 实例，重新启用时可能携带旧的解析状态。

### 2.2 EnabledReader_ReceivesClampedInputWithoutMovingPlayer

- 原始结果：期望输入长度小于或等于 `1.0`，实际为 `1.00000012`。
- 直接原因：`Vector2.ClampMagnitude` 对对角向量执行浮点运算后产生了极小的精度上溢。
- 玩家位置断言未指出移动问题；输入组件没有修改 Transform。

## 3. 修改内容

### 3.1 PlayerInputReader 生命周期

- `OnDisable` 取消 Action 事件订阅并禁用 Action。
- 禁用后立即释放运行时 Action 实例，避免旧状态跨越禁用/启用周期。
- `OnDisable` 继续显式把 `MoveInput` 归零，归零不依赖后续帧或 `canceled` 回调。
- `OnEnable` 从已配置的 Input Action Reference 重新建立独立运行时 Action，并重新订阅一次事件。
- 运行时 Action 使用 `PassThrough` 读取模式，但完整复用已批准 Move Action 的绑定、Interactions、Processors 和控制类型。项目 Input Actions 资产仍保持原有 `Value` 配置，未被本次修复修改。
- `PassThrough` 运行时读取避免在重新启用瞬间采集禁用期间排队的旧设备状态，并能继续接收启用后的 WASD 与方向键事件。

### 3.2 输入严格限幅

- 保留 `Vector2.ClampMagnitude` 的正常限幅行为。
- 当结果因浮点精度导致平方长度仍大于 `1` 时，使用具名精度余量执行安全归一化。
- 零输入、轴向输入、非法浮点输入和对角输入的原有规则保持不变。

## 4. 受影响文件

- `Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInputReader.cs`
- `Client/Assets/WonderSquad/Runtime/Player/Input/MoveInputValue.cs`
- `Docs/01_Project/Sprint001B_TestFix_Report.md`

未修改：

- Player Spawn 生产代码与测试
- `PlayerSandbox.unity`
- Movement、Camera、CharacterController
- Interaction、Inventory、Ability、Puzzle、Network

## 5. PlayerController 与输入消费审计

- Runtime 中不存在提前实现的 `PlayerController`。
- `MoveInput` 没有被任何移动、旋转或 CharacterController 逻辑消费。
- 当前唯一运行时观察者为 `PlayerInputDebugLogger`，仅输出输入变化，不修改玩家 Transform。
- Player 运行时目录中未发现 Transform 移动调用。

## 6. Unity 测试结果

测试环境：

- Unity Editor：6000.3.21f1
- 测试方式：使用当前源文件的隔离项目副本执行目标 PlayMode 测试类
- 测试过滤器：`WonderSquad.Tests.PlayMode.PlayerInputReaderPlayModeTests`

结果：

| 测试 | 结果 |
|---|---|
| `DisabledReader_ClearsInputAndReceivesInputAfterReenable` | PASSED |
| `EnabledReader_ReceivesClampedInputWithoutMovingPlayer` | PASSED |
| `RepeatedEnableCycles_EmitOneInputChangePerValue` | PASSED |

汇总：3 Passed，0 Failed，0 Skipped。

本次测试同时确认：

- Disable 后输入立即归零。
- Disable 期间不会更新公开输入值。
- Enable 后能接收新的方向键输入。
- 对角输入长度不超过 `1`。
- 重复启用不会产生重复输入通知。
- 玩家位置不会因输入读取层发生变化。
- 目标程序集在 Unity 6000.3.21f1 中编译成功。

## 7. 范围审计结论

- 未修改 Player Spawn。
- 未引入 Player Movement 或 Sprint001C 内容。
- 未直接使用 `Keyboard.current`。
- 未增加 Update 中的分配、订阅或设备读取。
- 未增加对 Camera、CharacterController 或后续玩法模块的依赖。

## 8. 最终结论

**PASSED**

Sprint001B 的本次 PlayMode 测试失败已修复。工作在 Player Input Reader 范围内结束，不开始 Sprint001C。
