# Sprint002A Interaction Detection 实施报告

## 1. 实施结论

- Sprint：Sprint002A — Interaction Detection
- 分支：`feature/sprint002a-interaction-detection`
- 基线：`v0.3-character-foundation-final` / `9af2dba`
- Unity：`6000.3.21f1`
- 实施状态：**PASSED**
- 独立编译检查：**PASSED**
- Unity Test Runner：**PASSED**
- PlayerSandbox 人工验收：**PASSED**

本次已完成只读交互目标契约、非分配候选查询、距离/Layer/遮挡验证、稳定目标选择、数据化配置及 PlayerSandbox 验证环境。没有实现交互输入、执行、UI 或具体玩法对象。

## 2. 实现范围

### 2.1 Core Contract

`WonderSquad.Core.Contracts.Interaction` 新增：

- `IInteractable`：仅提供稳定目标标识、检测位置、是否允许检测和检测优先级。
- `InteractionTargetId`：使用稳定字符串值标识目标，并提供序号比较和相等语义。

`IInteractable` 不包含 `Interact()`、输入、UI、机关、背包、谜题或网络成员。

### 2.2 Interaction Detection

`WonderSquad.Interaction.Detection` 新增：

- `InteractionDetector`：通过 `Physics.OverlapSphereNonAlloc` 收集候选，复用固定容量缓冲区，按 TargetId 去重，执行遮挡射线并选择当前有效目标。
- `InteractionValidator`：以无副作用规则验证目标存活状态、配置、稳定 ID、允许检测状态、距离、Layer 和遮挡证据。
- `InteractionTarget`：连接 Unity Collider 与只读 `IInteractable` 契约的最小适配组件，不执行任何玩法结果。
- `InteractionSettings`：保存检测半径、目标 LayerMask、最大候选数量、范围查询 Trigger 策略、射线 LayerMask 和射线 Trigger 策略。

目标选择顺序固定为：

1. 通过 Validator。
2. 检测优先级较高。
3. 距离较近。
4. `InteractionTargetId` 序号较小。

因此选择结果不依赖 Physics 返回 Collider 的先后顺序。

### 2.3 生命周期

- `OnEnable` 校验 Origin 与 Settings，并只在容量变化时创建候选缓冲区。
- `Update` 只刷新检测，不读取输入、不执行目标行为。
- `OnDisable` 立即清空当前目标。
- 目标禁用、销毁或离开范围后，在下一次刷新时清除或切换。
- 候选缓冲区饱和时只记录一次诊断警告，不越界、不扩容。

## 3. 数据与场景配置

新增 `Interactable` Layer，使用第一个可用用户层 `8`。

`InteractionSettings.asset` 默认配置：

| 参数 | 值 |
|---|---:|
| Detection Radius | `2.75` |
| Interaction Layer | `Interactable` |
| Max Candidate Count | `16` |
| Range Trigger Policy | `Collide` |
| Raycast Layer | `Default` |
| Raycast Trigger Policy | `Ignore` |

Player Prefab 仅通过 Prefab 组合增加：

```text
Player
├── InteractionDetector
└── InteractionOrigin
```

`InteractionOrigin` 位于 Player 本地坐标 `(0, 1, 0)`。Player 程序集没有引用 Interaction 程序集，PlayerSpawner、PlayerInputReader、PlayerMovement、GroundDetector 和 Camera 代码均未修改。

PlayerSandbox 新增 `InteractionValidationRoot/InteractionTestTarget`。该占位物使用 Trigger Collider，避免改变 CharacterController 的移动碰撞；目标只用于观察检测结果，没有提示文字、按键交互或状态变化。运行时可在 Player 的 `InteractionDetector.currentTargetComponent` 只读观察槽中查看当前目标。

## 4. 程序集变化

Runtime 依赖保持：

```text
WonderSquad.Player      → WonderSquad.Core
WonderSquad.Interaction → WonderSquad.Core
```

- `WonderSquad.Core.asmdef`：未修改。
- `WonderSquad.Interaction.asmdef`：仍只引用 `WonderSquad.Core`。
- `WonderSquad.Player.asmdef`：未修改，未引用 `WonderSquad.Interaction`。
- EditMode 与 PlayMode 测试程序集：各新增 `WonderSquad.Interaction` 测试引用。
- 未形成循环依赖或跨领域 Runtime 反向引用。

## 5. 新增文件

### 5.1 Runtime

- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/IInteractable.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionTargetId.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Detection/InteractionDetector.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Detection/InteractionSettings.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Detection/InteractionTarget.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Detection/InteractionValidator.cs`

### 5.2 Assets

- `Client/Assets/WonderSquad/ScriptableObjects/Configuration/InteractionSettings.asset`
- `Client/Assets/WonderSquad/Prefabs/Interaction/PF_Interactable_Test.prefab`
- 上述目录、文件及脚本对应的 `.meta`

### 5.3 Tests

- `Client/Assets/WonderSquad/Tests/EditMode/InteractionDetectionEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionDetectionPlayModeTests.cs`
- 对应 `.meta`

### 5.4 Documents

- `Docs/01_Project/Sprint002A_Implementation_Report.md`
- `Docs/01_Project/Sprint002A_Review_Checklist.md`

## 6. 修改文件

- `Client/Assets/WonderSquad/Prefabs/Player/Player.prefab`
- `Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`
- `Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef`
- `Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef`
- `Client/ProjectSettings/TagManager.asset`
- `CHANGELOG.md`

没有修改 Input Actions、Packages、Player Runtime、Camera Runtime 或其他 Gameplay 系统。

## 7. 测试覆盖

### 7.1 EditMode

- `IInteractable` 是只读契约且不含 `Interact()`。
- `InteractionSettings` 默认实例与资产配置有效。
- 非法半径、空 LayerMask 和零候选容量被拒绝。
- `InteractionDetector` 必须具有 Origin 与有效 Settings。
- `InteractionValidator` 接受有效距离、Layer 与可见目标。
- `InteractionValidator` 拒绝超距、Layer 不匹配和遮挡目标。
- Interaction 与 Player 程序集依赖方向保持单向。

### 7.2 PlayMode

- 附近目标可以检测，目标状态不被改变。
- 超出半径的目标不被检测。
- Layer 不匹配的目标不被检测。
- 墙体遮挡时不检测，移除遮挡后恢复。
- 检测启用时 PlayerMovement 仍按原链路工作，Player 不增加 Rigidbody。
- 预热后的稳定检测循环不产生托管内存分配。
- Detector 禁用时清空目标，重新启用后恢复检测。
- 现有 Character Foundation 测试保留在完整回归集中。

## 8. 验证结果

### 8.1 已完成

- Core Runtime 独立编译：通过。
- Interaction Runtime 独立编译：通过。
- 新增 EditMode 测试程序集独立编译：通过。
- 新增 PlayMode 测试程序集独立编译：通过。
- Unity 资产 `.meta` GUID 重复检查：通过。
- Runtime 禁用 API 与越界系统引用扫描：通过。
- `git diff --check`：无空白错误。

独立编译过程中发现 PlayMode 测试的 `System.Object` / `UnityEngine.Object` 名称歧义，已通过移除 `using System` 并显式使用 `System.GC` 修复；生产代码未因此改变。

### 8.2 Unity Test Runner 最终结果

已在 Unity `6000.3.21f1` 中完成全量自动化验证：

```text
EditMode
44 Passed
0 Failed
0 Skipped

PlayMode
26 Passed
0 Failed
0 Skipped
```

`OccludedTarget_IsNotDetectedAndRecoversWhenClear` 的首次失败来自测试射线与 PlayerSandbox 既有 `P0_VisibleMarker` Collider 重叠。修复仅调整了测试目标方向，使射线路径与测试无关场景物隔离；未修改 `InteractionDetector` 或 `InteractionValidator` 生产逻辑。修复后 PlayMode 全量回归为 `26/26 Passed`。

## 9. 范围审计

以下内容均未实现：

- `E` 键或其他 Interaction 输入消费
- `InteractionExecutor`
- UI Prompt、提示文字、高亮或图标
- Door、Lever、Chest 或具体机关
- Puzzle、Inventory、Item、Ability、Crafting
- Network、RPC、Photon Fusion 或权威执行
- Event Bus

Runtime 检测代码中不存在：

- 分配版 `Physics.OverlapSphere`
- `FindObjectsOfType`
- `FindGameObjectsWithTag`
- `Keyboard.current`
- Transform 移动
- CharacterController 写入
- Rigidbody

Player Spawn、Input、Movement 与 Camera 的生产代码未修改。Player Prefab 的变化严格限于 Detection Origin 和 Detector 组合。

## 10. Unity 人工验收结果

已在 Unity `6000.3.21f1` 的 PlayerSandbox 中完成：

- 靠近 `InteractionTestTarget` 时检测正常。
- 离开范围后当前目标正常清除。
- 墙体遮挡时目标不可检测。
- 清除遮挡后目标恢复检测。
- Layer 过滤正常。
- Trigger 测试目标不阻挡玩家移动。
- 未出现交互输入、UI 提示或自动执行。
- Player Spawn、Input、Movement 与 Camera 均正常。
- Console Error 为 `0`。

## 11. 当前限制

- 当前检测结果是本地只读候选，不代表交互执行成功。
- 当前仅使用距离、Layer、遮挡、优先级和稳定 ID；没有输入朝向权重或目标切换滞后。
- 候选缓冲区容量固定为 `16`；饱和时记录诊断，MVP 调优阶段再依据场景密度调整。
- PlayerSandbox 目标为临时测试占位物，不是 Door、Lever、Chest 或正式关卡对象。
- 网络权威端未来必须在执行层重新校验；Sprint002A 不建立 Network Spawn 或权威交互。

## 12. 最终状态

**PASSED**

实现、静态架构检查、Unity Test Runner 全量回归和 PlayerSandbox 人工验收均已完成。Sprint002A Interaction Detection 已通过；本报告只完成 Sprint002A 收尾，不代表已开始 Sprint002B。
