# Sprint001C Review Checklist

## 1. 范围

- [x] Sprint 目标限定为水平移动、朝向、CharacterController、接地和重力。
- [x] 未实现 Jump。
- [x] 未实现 Camera。
- [x] 未实现 Animation。
- [x] 未实现 Interaction、Inventory、Ability、Puzzle 或 Network。
- [x] 未引入第三方包、SDK 或服务。

## 2. Unity 与资产

- [x] 使用 Unity `6000.3.21f1` 编译。
- [x] 隔离项目编译未发现 C# Error。
- [x] Player Prefab 包含 CharacterController。
- [x] Player Prefab 包含 GroundDetector。
- [x] Player Prefab 包含 PlayerMovement。
- [x] Player Prefab 引用 MovementSettings 资产。
- [x] 新增 Unity 资产与脚本均包含 `.meta`。
- [x] 未修改 Build Settings 或 Build Profiles。
- [x] 未修改 `PlayerSandbox.unity`。
- [x] 主 Unity Editor 中运行未发现 Missing Script 或 Missing Reference。
- [x] 主 Unity Editor Console Error 为 0。

## 3. 输入与移动职责

- [x] 输入链路为 PlayerInputReader → PlayerMovement → CharacterController。
- [x] Movement 不直接读取 Keyboard。
- [x] Movement 不直接读取 Gamepad。
- [x] Movement 不引用或修改 InputAction。
- [x] PlayerInputReader 不移动 Transform。
- [x] PlayerSpawner 不参与移动计算。
- [x] CharacterController 是唯一位移执行核心。
- [x] 未加入 Rigidbody 运动核心。

## 4. 参数与代码质量

- [x] Walking Speed 位于 MovementSettings。
- [x] Rotation Speed 位于 MovementSettings。
- [x] Gravity Acceleration 位于 MovementSettings。
- [x] Maximum Fall Speed 位于 MovementSettings。
- [x] Grounded Vertical Speed 位于 MovementSettings。
- [x] 未在 PlayerMovement 中散落可调玩法 Magic Number。
- [x] Namespace 与目录为 `WonderSquad.Player.Movement`。
- [x] MonoBehaviour 默认 sealed。
- [x] Inspector 引用使用 `[SerializeField] private` 和 Tooltip。
- [x] 配置无效时安全禁用并输出明确错误。
- [x] 未使用 Region、God Class、全局 Manager 或静态可变状态。
- [x] Update 中无场景查找、事件订阅或每帧对象创建。

## 5. 架构

- [x] 符合 `Design/Technical_Architecture_v1.1.md` 的 Player 职责。
- [x] 符合 `Docs/02_Gameplay/Player_System.md` 的本地输入和移动边界。
- [x] Movement 不依赖 UI、Editor、具体网络 SDK 或场景对象名称。
- [x] 未增加 Assembly Definition 引用。
- [x] 未形成循环依赖。
- [x] 本地输入权与未来主机位置权边界未被改变。
- [x] 纯移动计算可以接受设备无关的 Vector2 输入。
- [x] 未提前实现网络 Tick、预测或校正。

## 6. EditMode Tests

- [x] 默认移动参数有效。
- [x] 输入方向计算正确。
- [x] 对角输入正确限幅。
- [x] 水平速度不超过 Walking Speed。
- [x] 重力参数有效。
- [x] 完整 EditMode 回归通过。

结果：

```text
32 Passed
0 Failed
0 Skipped
```

## 7. PlayMode Tests

- [x] 玩家可以移动。
- [x] 玩家朝移动方向旋转。
- [x] 无输入时停止水平移动。
- [x] 不产生自动水平漂移。
- [x] 重力正常推进。
- [x] 落地后接地状态正常。
- [x] CharacterController 不穿透地面。
- [x] 禁用移动组件后玩家不移动。
- [x] PlayerSpawner 保持单实例且不参与移动。
- [x] PlayerInputReader 隔离测试仍证明 Reader 本身不移动玩家。
- [x] 完整 PlayMode 回归通过。

结果：

```text
13 Passed
0 Failed
0 Skipped
```

## 8. PlayerSandbox 人工验证

- [x] PlayerSpawner 回归确认只生成一个 Player。
- [x] WASD 方向正确。
- [x] 角色朝移动方向旋转且无明显抖动。
- [x] 松开按键后立即停止且不漂移。
- [x] 斜向移动速度正常。
- [x] 从高处下落并停在地面。
- [x] 接地后无持续抖动。
- [x] 未穿透地面。
- [x] 禁用 PlayerMovement 后立即停止。
- [x] 重新启用 PlayerMovement 后恢复正常。
- [x] Console Error 为 0。

## 9. 文档与 Git

- [x] `CHANGELOG.md` 的 `[Unreleased]` 已更新。
- [x] `Sprint001C_Implementation_Report.md` 已生成。
- [x] 本 Review Checklist 已生成。
- [x] 分支为 `feature/sprint001-character-controller-v2`。
- [x] 未修改 PlayerSpawner 生产代码。
- [x] 未修改 PlayerInputReader 生产代码。
- [ ] 合并前确认分支已同步最新 develop。
- [ ] 按 Conventional Commits 提交。

## 10. Review 结论

当前结论：

**PASSED**

未完成阻塞项：

无。

Sprint001C 自动化和人工验收均已通过，可以执行 Sprint001C Gate Review。当前不开始 Sprint001D Camera。

## 11. Sprint001C Gate Review 建议检查项

- [ ] PlayerMovement 只负责移动计算、角色朝向和 CharacterController 驱动。
- [ ] 输入链路保持 PlayerInputReader → PlayerMovement → CharacterController。
- [ ] Movement 不读取或修改 Keyboard、Gamepad 和 InputAction。
- [ ] CharacterController 是唯一位移核心，Player Prefab 不包含 Rigidbody。
- [ ] GroundDetector 不执行位移，不与 PlayerMovement 争夺运动所有权。
- [ ] MovementSettings 覆盖全部可调移动参数，且不保存运行时状态。
- [ ] Disable/Enable 行为稳定，不存在速度残留或静态状态污染。
- [ ] 无输入、斜向限速、重力、接地和地面穿透测试完整。
- [ ] PlayerSpawner 与 PlayerInputReader 未发生隐藏耦合。
- [ ] 未引入 Camera、Jump、Animation 或后续 Gameplay/Network 系统。
- [ ] 未来 Network Adapter 可以采样设备无关输入，Movement 未持有网络权威。
- [ ] Unity `6000.3.21f1` 自动化与人工验证证据一致。
