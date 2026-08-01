# Sprint001-B Review Checklist

## 1. 范围

- [x] 唯一目标为 Player Input Reader。
- [x] 只实现二维移动输入读取、绑定、限幅、生命周期和调试输出。
- [x] 未实现 Transform 移动。
- [x] 未实现 CharacterController、旋转、重力、Ground Check、跳跃、Camera 或动画。
- [x] 未引入 Interaction、Inventory、Ability、Puzzle、Network。
- [x] 未开始 Sprint001-C Movement。
- [x] 未新增第三方插件、SDK 或服务。

## 2. Unity 与资源

- [x] Unity 基线固定为 `6000.3.21f1`。
- [x] New Input System 包继续使用项目锁定版本。
- [x] 复用现有 `WonderSquad.inputactions`。
- [x] 只增加方向键所需 Move Binding。
- [x] Input Actions JSON 静态解析通过。
- [x] Player Prefab 层级与 Transform 未改变。
- [x] PlayerSandbox Scene 文件未修改。
- [x] 新增 Unity 资源均包含 `.meta`。
- [ ] 主 Editor 导入后无 Missing Script 或 Missing Reference。
- [ ] Console Error 为 `0`。

## 3. 代码质量

- [x] Namespace 与目录为 `WonderSquad.Player.Input`。
- [x] 类型和布尔命名符合 `CODE_STYLE.md`。
- [x] 对外只暴露只读 `MoveInput`。
- [x] 输入长度始终不超过 `1`。
- [x] 无效浮点输入安全归零。
- [x] 缺失或错误 Action 配置可检测并安全停用。
- [x] 不直接读取 `Keyboard.current`。
- [x] 不存在 `Update`、`FixedUpdate` 或 `LateUpdate` 轮询。
- [x] 不在帧循环中创建对象或订阅事件。
- [x] `OnEnable`/`OnDisable` 订阅与取消订阅成对。
- [x] 禁用后输入归零。
- [x] Reader 使用独立 Action Clone，不改变共享资产启用状态。
- [x] 没有 Magic Number、God Class、Region 或静态可变状态。

## 4. 架构

- [x] Player Runtime 没有依赖其他 WonderSquad Gameplay 实现程序集。
- [x] Player Runtime 没有依赖 UI、Editor 或 Network。
- [x] 只新增获批准的 `Unity.InputSystem` 基础设施引用。
- [x] 网络权威边界未改变：本地读取输入，未来主机决定最终移动结果。
- [x] 输入组件不持有位置权威。
- [x] 不存在程序集循环依赖。
- [x] Input System 类型没有泄漏到公开 `MoveInput` 值属性或变化事件。

## 5. 测试代码

### EditMode

- [x] 零输入测试已编写。
- [x] 超长输入限幅测试已编写。
- [x] 对角输入单位圆测试已编写。
- [x] 非有限输入测试已编写。
- [x] 非法配置测试已编写。
- [x] WASD 与方向键资产绑定测试已编写。
- [x] Player Prefab 输入引用有效性测试已编写。
- [x] EditMode 测试程序集使用 Unity 6000.3.21f1 编译参数验证通过。
- [ ] 相关 EditMode Tests 已在 Unity Test Runner 运行通过。

### PlayMode

- [x] 启用后模拟输入测试已编写。
- [x] 禁用归零测试已编写。
- [x] 重新启用恢复读取测试已编写。
- [x] 重复启停不产生重复变化通知测试已编写。
- [x] Player 位置不变测试已编写。
- [x] PlayMode 测试程序集使用 Unity 6000.3.21f1 编译参数验证通过。
- [ ] 相关 PlayMode Tests 已在 Unity Test Runner 运行通过。
- [ ] 完整 EditMode 回归通过。
- [ ] 完整 PlayMode 回归通过。

## 6. 人工 PlayerSandbox

- [ ] PlayerSandbox 可独立进入 Play Mode。
- [ ] Player 仍只生成一个实例。
- [ ] WASD 日志方向正确。
- [ ] 方向键日志方向正确。
- [ ] 松开后输入归零。
- [ ] W+D 时 Magnitude 不超过 `1.00`。
- [ ] Player 胶囊和 Transform 不移动。
- [ ] 禁用 Reader 后归零，重新启用后可继续输入。
- [ ] Console Error 为 `0`。

## 7. 文档与 Git

- [x] `CHANGELOG.md` 的 Unreleased 已更新。
- [x] `Sprint001B_Implementation_Report.md` 已生成。
- [x] 实施范围、限制和未验证项已如实记录。
- [x] 当前分支名称符合用户指定分支。
- [x] 推荐 Commit 信息符合 Conventional Commits。
- [ ] 合并前确认分支已同步最新 develop。
- [ ] 合并前确认工作区只包含已说明改动。

## 8. Review 结论

当前结论：

**CONDITIONAL — IMPLEMENTATION COMPLETE, UNITY RUNTIME VALIDATION REQUIRED**

合并前阻塞项：

1. 在主 Unity Editor 运行相关 EditMode Tests。
2. 在主 Unity Editor 运行相关 PlayMode Tests。
3. 运行完整 EditMode、PlayMode 回归。
4. 完成 PlayerSandbox 人工验证并确认 Console Error 为 `0`。

在以上证据完成前，不建议提交合并，也不开始 Sprint001-C Movement。
