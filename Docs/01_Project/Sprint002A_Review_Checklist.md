# Sprint002A Interaction Detection Review Checklist

## 1. 范围

- [x] 只实现交互目标发现与验证。
- [x] 未实现按 `E` 或其他交互输入。
- [x] 未实现 InteractionExecutor。
- [x] 未实现 UI Prompt、提示文字、高亮或图标。
- [x] 未实现 Door、Lever、Chest 或具体机关。
- [x] 未实现 Puzzle、Inventory、Ability 或 Network。
- [x] 未修改 Input Actions。

## 2. Contract

- [x] `IInteractable` 位于 `WonderSquad.Core`。
- [x] 契约成员全部只读。
- [x] 契约不含 Input、UI 或具体机关类型。
- [x] 契约不含无条件改变状态的 `Interact()`。
- [x] `InteractionTargetId` 不依赖 GameObject 名称。
- [x] 稳定 ID 支持确定性相等和排序。

## 3. Detector

- [x] `InteractionDetector` 位于 `WonderSquad.Interaction`。
- [x] 使用 `Physics.OverlapSphereNonAlloc`。
- [x] 使用固定容量、可复用 Collider 缓冲区。
- [x] 不使用分配版 `Physics.OverlapSphere`。
- [x] 不使用 `FindObjectsOfType` 或 Tag 查找候选。
- [x] 不读取 Keyboard、Gamepad 或 InputAction。
- [x] 不执行目标行为或修改目标状态。
- [x] 不移动 Player Transform 或 CharacterController。
- [x] 同一 TargetId 的多个 Collider 会去重。
- [x] 选择顺序不依赖 Physics 返回顺序。
- [x] Disabled 时清空当前目标。
- [x] Buffer 饱和不会越界或运行时扩容。

## 4. Validator

- [x] `InteractionValidator` 与 MonoBehaviour 生命周期解耦。
- [x] 验证 Settings 和目标引用有效性。
- [x] 验证稳定 TargetId。
- [x] 验证目标允许被检测。
- [x] 验证距离。
- [x] 验证 Layer。
- [x] 验证遮挡证据。
- [x] 验证过程无副作用。

## 5. Settings

- [x] Detection Radius 数据化。
- [x] Interaction LayerMask 数据化。
- [x] Max Candidate Count 数据化。
- [x] Range Query Trigger 策略数据化。
- [x] Raycast LayerMask 数据化。
- [x] Raycast Trigger 策略数据化。
- [x] Settings 不保存当前目标或其他运行时状态。
- [x] 非法半径、Mask 和容量会被拒绝。

## 6. 程序集与依赖

- [x] `WonderSquad.Interaction` 只引用 `WonderSquad.Core`。
- [x] `WonderSquad.Player` 没有引用 `WonderSquad.Interaction`。
- [x] Player 和 Interaction Runtime 没有相互引用。
- [x] 没有形成循环依赖。
- [x] 测试程序集新增 Interaction 引用。
- [x] Runtime 没有引用 Tests。
- [x] 没有引用 Puzzle、Inventory、Ability、UI 或 Network。

## 7. Player Prefab 与 PlayerSandbox

- [x] Player Prefab 只增加 `InteractionOrigin` 和 `InteractionDetector`。
- [x] PlayerSpawner 未修改。
- [x] PlayerInputReader 未修改。
- [x] PlayerMovement 与 GroundDetector 未修改。
- [x] Camera Runtime 未修改。
- [x] Player 没有新增 Rigidbody。
- [x] PlayerSandbox 只增加临时 Interaction 测试目标。
- [x] 测试目标没有 UI 或执行行为。
- [x] 新增 Layer 没有覆盖已有 Layer。
- [x] 新增 Unity 脚本和资产具有 `.meta`。

## 8. EditMode Tests

- [x] `IInteractable` 契约检查已编写。
- [x] Settings 默认值与资产有效性测试已编写。
- [x] Detector 配置验证测试已编写。
- [x] Validator 距离、Layer 和遮挡规则测试已编写。
- [x] 程序集依赖方向测试已编写。
- [x] 新增 EditMode 测试程序集独立编译通过。
- [x] Unity Test Runner 中新增 EditMode 测试通过。
- [x] Unity Test Runner 中完整 EditMode 回归通过：`44 Passed / 0 Failed / 0 Skipped`。

## 9. PlayMode Tests

- [x] 附近目标检测测试已编写。
- [x] 超距目标过滤测试已编写。
- [x] Layer 不匹配过滤测试已编写。
- [x] 墙体遮挡与恢复测试已编写。
- [x] PlayerMovement 兼容回归测试已编写。
- [x] 稳定检测托管分配测试已编写。
- [x] Detector Disable/Enable 生命周期测试已编写。
- [x] 新增 PlayMode 测试程序集独立编译通过。
- [x] Unity Test Runner 中新增 PlayMode 测试通过。
- [x] PlayerSpawner、PlayerInputReader、PlayerMovement、Camera 与 Bootstrap 完整回归通过：`26 Passed / 0 Failed / 0 Skipped`。

## 10. 静态质量检查

- [x] Core Runtime 独立编译通过。
- [x] Interaction Runtime 独立编译通过。
- [x] EditMode 测试源码独立编译通过。
- [x] PlayMode 测试源码独立编译通过。
- [x] `.meta` GUID 无重复。
- [x] Runtime 禁用 API 扫描无命中。
- [x] `git diff --check` 无空白错误。
- [x] 无 Magic Number 形式的可调检测参数散落在运行逻辑。
- [x] 无静态可变目标状态、万能 Manager、God Class 或 Region。

## 11. Unity 人工验收

- [x] Unity `6000.3.21f1` 项目编译无错误。
- [x] PlayerSandbox Play 后生成一个 Player。
- [x] 接近测试目标时 Detector 能发现目标。
- [x] 离开检测半径时 Detector 清空目标。
- [x] 返回范围后 Detector 恢复目标。
- [x] 墙体遮挡时不检测，遮挡清除后恢复检测。
- [x] Layer 过滤正常。
- [x] Trigger 测试目标不阻挡玩家移动。
- [x] Player Spawn、Input、Movement 和 Camera 没有回归。
- [x] 没有交互输入、UI 提示或自动执行。
- [x] Console Error 为 `0`。

## 12. 测试修复记录

`OccludedTarget_IsNotDetectedAndRecoversWhenClear` 首次失败的原因是测试射线与 PlayerSandbox 既有 `P0_VisibleMarker` Collider 重叠。该 Collider 对射线而言是有效遮挡物，因此失败反映的是测试场景隔离不足，不是生产遮挡规则错误。

最小修复只调整测试目标方向，避开与测试无关的既有 Collider。`InteractionDetector` 与 `InteractionValidator` 未修改。修复后 PlayMode `26/26` 通过。

## 13. 最终状态

**PASSED**

Unity 自动化、人工验收、范围审计与 Character Foundation 回归均已通过。当前只完成 Sprint002A 收尾，不开始 Sprint002B。
