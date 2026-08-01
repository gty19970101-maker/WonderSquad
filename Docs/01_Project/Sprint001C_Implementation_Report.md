# Sprint001C Character Controller Movement 实施报告

## 1. 实施结论

- Sprint：Sprint001C — Character Controller Movement
- 分支：`feature/sprint001-character-controller-v2`
- Unity：`6000.3.21f1`
- 自动化状态：**PASSED**
- 人工 PlayerSandbox 验收：**PASSED**
- 最终状态：**PASSED**

本阶段实现了水平移动、角色朝向、CharacterController 驱动、地面状态和重力。未实现 Camera，也未开始后续 Sprint。

## 2. 输入与移动链路

```text
PlayerInputReader
→ 只读 Vector2 MoveInput
→ PlayerMovement
→ PlayerMovementMath
→ CharacterController.Move
→ CollisionFlags
→ GroundDetector
```

边界说明：

- `PlayerInputReader` 仍只读取设备无关的移动输入。
- `PlayerMovement` 不读取 Keyboard、Gamepad 或 InputAction。
- `PlayerMovement` 不修改 Input Action 配置或生命周期。
- `CharacterController` 是唯一的位移执行组件。
- `GroundDetector` 只保存最近一次 CharacterController 碰撞产生的接地结果，不执行位移。
- `PlayerSpawner` 仍只负责 Sandbox 本地生成，不参与移动。

## 3. 实现内容

### 3.1 MovementSettings

新增数据化参数：

| 参数 | MVP 默认值 | 单位与用途 |
|---|---:|---|
| Walking Speed | 4 | 世界单位/秒，水平移动速度 |
| Rotation Speed | 540 | 度/秒，角色最大转向速度 |
| Gravity Acceleration | -25 | 世界单位/秒²，向下加速度 |
| Maximum Fall Speed | 30 | 世界单位/秒，最大下落速度 |
| Grounded Vertical Speed | -2 | 世界单位/秒，保持接地的小幅向下速度 |

`MovementSettings` 只保存静态配置，不保存玩家位置、速度或其他运行时权威状态。无效或非有限参数会使配置校验失败。

### 3.2 PlayerMovementMath

提供无设备依赖的纯计算：

- 将二维输入映射到世界 XZ 平面。
- 再次执行安全限幅，保证未来其他输入来源不会突破单位圆。
- 根据 Walking Speed 计算受限水平速度。
- 应用重力、接地向下速度和最大下落速度。

当前世界方向规则：

- 输入 X → 世界 X。
- 输入 Y → 世界 Z。

Camera 尚未实现，因此本阶段不做相机相对移动。

### 3.3 PlayerMovement

- 每帧只读取一次 `PlayerInputReader.MoveInput`。
- 使用 `CharacterController.Move` 应用水平速度与垂直速度。
- 有水平输入时通过 `Quaternion.RotateTowards` 朝移动方向转向。
- 无输入时水平速度立即归零，不产生水平漂移。
- 禁用组件后清空缓存速度，不再调用 CharacterController。
- 配置缺失或无效时记录明确错误并安全禁用。

### 3.4 GroundDetector

- 使用 CharacterController 的 `isGrounded` 和最近一次 `CollisionFlags.Below` 确认接地。
- 不执行额外位移、射线分配或场景查找。
- 禁用时清除接地状态。
- 为未来替换或扩展接地策略保留单一职责边界。

### 3.5 Player Prefab

Player 根对象新增：

- `CharacterController`
- `GroundDetector`
- `PlayerMovement`

CharacterController 当前占位体积与胶囊视觉匹配：

- Height：1.8
- Radius：0.45
- Center：`(0, 0.9, 0)`
- Skin Width：0.1
- Step Offset：0.3
- Slope Limit：45

Player Prefab 引用项目级 `MovementSettings.asset`，没有把速度和重力散落在组件字段中。

## 4. 新增文件

### Runtime

- `Client/Assets/WonderSquad/Runtime/Player/Movement/MovementSettings.cs`
- `Client/Assets/WonderSquad/Runtime/Player/Movement/PlayerMovementMath.cs`
- `Client/Assets/WonderSquad/Runtime/Player/Movement/GroundDetector.cs`
- `Client/Assets/WonderSquad/Runtime/Player/Movement/PlayerMovement.cs`
- 上述文件和目录对应的 `.meta`

### Data

- `Client/Assets/WonderSquad/ScriptableObjects/Configuration/MovementSettings.asset`
- `Client/Assets/WonderSquad/ScriptableObjects/Configuration/MovementSettings.asset.meta`

### Tests

- `Client/Assets/WonderSquad/Tests/EditMode/PlayerMovementEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/EditMode/PlayerMovementEditModeTests.cs.meta`
- `Client/Assets/WonderSquad/Tests/PlayMode/PlayerMovementPlayModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/PlayerMovementPlayModeTests.cs.meta`

### Documentation

- `Docs/01_Project/Sprint001C_Implementation_Report.md`
- `Docs/01_Project/Sprint001C_Review_Checklist.md`

## 5. 修改文件

- `Client/Assets/WonderSquad/Prefabs/Player/Player.prefab`
  - 增加 CharacterController、GroundDetector 和 PlayerMovement。
- `Client/Assets/WonderSquad/Tests/EditMode/PlayerInputReaderEditModeTests.cs`
  - 修复全新 Library 下 InputActionReference 测试资产的隔离方式；未修改输入生产逻辑。
- `Client/Assets/WonderSquad/Tests/PlayMode/PlayerInputReaderPlayModeTests.cs`
  - 在“InputReader 本身不得移动玩家”的隔离测试中显式禁用 PlayerMovement。
- `Client/Assets/WonderSquad/Tests/PlayMode/PlayerSpawnerPlayModeTests.cs`
  - 出生位置断言改用毫米级容差，以允许生成后同帧发生合法重力更新；PlayerSpawner 生产逻辑未修改。
- `CHANGELOG.md`
  - 在 `[Unreleased]` 记录 Sprint001C。

未修改：

- `PlayerSpawner.cs`
- `PlayerSpawnPoint.cs`
- `PlayerInputReader.cs`
- Input Actions 资产
- `PlayerSandbox.unity`
- 任何 Camera、Interaction、Inventory、Ability、Puzzle 或 Network 文件

## 6. 测试覆盖

### 6.1 EditMode

新增覆盖：

- 默认 MovementSettings 资产有效。
- 二维输入到世界 XZ 方向的计算正确。
- 对角和超长输入不会突破 Walking Speed。
- 重力、最大下落速度和接地速度参数有效。

完整 EditMode 结果：

- 32 Passed
- 0 Failed
- 0 Skipped

首轮完整回归发现两个既有 InputReader 测试依赖了不稳定的 InputActionReference 构造方式。仅修复测试隔离后，完整 EditMode 回归通过。

### 6.2 PlayMode

新增覆盖：

- 输入能够驱动玩家水平移动。
- 玩家朝移动方向转向。
- 松开输入后水平速度归零。
- 多帧观察不产生自动水平漂移。
- 玩家从高处受重力影响下落。
- 落地后 GroundDetector 报告接地。
- CharacterController 不穿透 Sandbox 地面。
- 禁用 PlayerMovement 后玩家不移动。
- PlayerSpawner 仍保留同一个生成实例，不产生重复玩家。

完整 PlayMode 结果：

- 13 Passed
- 0 Failed
- 0 Skipped

回归包含：

- Bootstrap PlayMode 测试。
- PlayerInputReader PlayMode 测试。
- PlayerSpawner PlayMode 测试。
- PlayerMovement PlayMode 测试。

## 7. 验证环境

- Unity Editor：`6000.3.21f1`
- 测试方式：使用当前源文件的隔离项目副本执行完整 EditMode 和 PlayMode 测试
- 编译扫描：未发现 C# 编译错误
- 自动测试日志：未发现未说明的测试错误
- 主 Unity Editor 人工验证：已完成
- 主 Unity Editor Console Error：0

隔离测试副本仅用于避免干扰当前已打开的主 Unity 项目，测试结束后删除，不属于提交内容。

## 8. PlayerSandbox 人工验证结果

人工验证环境：Unity `6000.3.21f1`。

| 验证项 | 结果 |
|---|---|
| WASD 移动 | PASSED |
| 松开输入后立即停止，无水平漂移 | PASSED |
| 角色转向正常，无明显抖动 | PASSED |
| 斜向移动速度正常 | PASSED |
| 高处下落与重力正常 | PASSED |
| 接地后无持续抖动 | PASSED |
| CharacterController 未穿透地面 | PASSED |
| 禁用 PlayerMovement 后立即停止 | PASSED |
| 重新启用 PlayerMovement 后恢复正常 | PASSED |
| Console Error | 0 |

自动化回归同时确认 PlayerSpawner、PlayerInputReader 和 Bootstrap 保持通过。

## 9. 范围审计

已确认没有实现：

- Jump
- Camera
- Animation
- Interaction
- Inventory
- Ability
- Puzzle
- Network

运行时 Movement 目录未引用：

- Keyboard 或 Gamepad 设备 API
- InputAction
- Rigidbody
- Camera
- Photon Fusion
- 后续玩法模块

收尾范围审计还确认：

- Player Prefab 不包含 Rigidbody。
- Player Prefab 的运动核心为 CharacterController。
- `PlayerInputReader` 未增加 Transform 或 CharacterController 操作。
- `PlayerSpawner` 未增加 PlayerMovement、GroundDetector、CharacterController 或 MoveInput 依赖。
- 本 Sprint 未修改 Camera、Jump、Interaction、Inventory、Ability、Puzzle 或 Network 模块。

## 10. 当前限制

- 移动使用世界坐标，不是 Camera 相对坐标。
- 没有加速、减速、跑步、跳跃或空中控制。
- GroundDetector 当前只封装 CharacterController 的接地结果。
- 本地 Update 驱动尚未映射到 Fusion Tick；未来网络层应采样输入意图并由主机决定最终位置。
- CharacterController 体积和移动参数仍需在手感 Sprint 中调优。

## 11. 推荐 Commit

```text
feat(player): add character controller movement
```

## 12. Sprint001C Gate Review 建议检查项

1. `PlayerMovement` 是否只负责移动计算、转向和 CharacterController 驱动。
2. 输入链路是否仍严格为 `PlayerInputReader → PlayerMovement → CharacterController`。
3. Movement 是否完全不读取 Keyboard、Gamepad 或 InputAction。
4. CharacterController 是否为唯一位移核心，Prefab 是否不存在 Rigidbody。
5. `GroundDetector` 是否只封装接地结果，不重复执行移动或引入额外物理所有权。
6. MovementSettings 是否覆盖全部可调移动参数，运行时状态是否未写回资产。
7. Disable/Enable 是否清理速度并稳定恢复，是否存在静态状态污染。
8. 无输入停止、斜向限速、重力、接地和不穿透测试是否覆盖充分。
9. PlayerSpawner 与 PlayerInputReader 的既有职责是否保持不变。
10. 是否存在 Camera、Jump、Animation 或后续玩法系统的范围越界。
11. 是否保持未来网络输入与主机位置权威边界，且未提前引入 Fusion。
12. Unity `6000.3.21f1` 的自动化与人工验收证据是否完整。

## 13. 最终结论

**PASSED**

Sprint001C 已完成自动化和人工验收，可以进入 Sprint001C Gate Review。本次不开始 Sprint001D Camera。
