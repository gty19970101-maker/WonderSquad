# Sprint002A Interaction Detection Gate Review

## 1. Review 信息

- Review 日期：2026-08-02
- Unity 基线：`6000.3.21f1`
- 当前分支：`feature/sprint002a-interaction-detection`
- 基线：`v0.3-character-foundation-final`
- Review 范围：Core Interaction Contract、Interaction Detection、相关程序集边界、测试与 PlayerSandbox 验证环境
- 自动化证据：EditMode `44 Passed / 0 Failed / 0 Skipped`；PlayMode `26 Passed / 0 Failed / 0 Skipped`
- 人工证据：PlayerSandbox 检测、清除、遮挡恢复、Layer、Trigger 和 Character Foundation 回归通过，Console Error 为 0
- 代码变更：本次 Gate Review 未新增或修改 Unity 生产代码
- 最终结论：**GO**

## 2. 总体结论

Sprint002A 已形成只读、单向且可测试的交互检测链路：

```text
Player Prefab 组合
      │ 提供 Detection Origin
      ▼
InteractionDetector
      │ NonAlloc 候选与射线证据
      ▼
InteractionValidator
      │ 只读规则判定
      ▼
IInteractable / InteractionTargetId
```

检测层只产生“当前候选目标”，不代表交互已执行，也不改变目标或玩家状态。Player Runtime 不引用 Interaction Runtime；Interaction Runtime 只引用 Core。未发现循环依赖、输入越界、具体机关耦合或进入 Sprint002B 前必须重构的问题。

## 3. Gate 检查

### 3.1 IInteractable 契约最小化和只读

**结果：通过。**

- 契约仅公开 `TargetId`、`DetectionPosition`、`IsDetectionEnabled` 和 `DetectionPriority`。
- 所有成员均为只读属性。
- 不包含 `Interact()`、输入、UI、具体机关、背包、谜题或网络类型。
- 契约只表达检测所需事实，不赋予客户端改变共享状态的能力。

### 3.2 InteractionTargetId 稳定性

**结果：通过。**

- ID 使用显式序列化字符串，不依赖 GameObject 名称、Tag、Transform 层级或 Unity 实例 ID。
- 使用 `StringComparison.Ordinal`、`StringComparer.Ordinal` 和序号比较，具备确定性相等、哈希与排序语义。
- 空白 ID 被视为无效，Detector 和 Validator 均拒绝无效 ID。
- 同一目标的多个 Collider 按 TargetId 去重。

非阻塞约束：场景与数据作者必须保证不同逻辑目标的 ID 唯一。未来数据驱动关卡工具可增加重复 ID 编辑器校验；该工具不属于 Sprint002A，也不阻塞当前 Gate。

### 3.3 Detector 与 Validator 职责分离

**结果：通过。**

- `InteractionDetector` 负责 Physics 候选收集、遮挡射线证据、去重和稳定目标选择。
- `InteractionValidator` 负责目标存活、配置、ID、允许检测、距离、Layer、遮挡证据和有限数值检查。
- Validator 不依赖 MonoBehaviour 生命周期，不执行 Physics Query，也不修改传入对象。
- Detector 不把具体机关规则写入 Validator。

### 3.4 Detector 不执行交互

**结果：通过。**

- Runtime 中不存在 `InteractionExecutor` 或交互执行方法调用。
- Detector 不读取 Input、InputAction、Keyboard 或 Gamepad。
- Detector 不调用目标状态变更，不移动 Player，不操作 CharacterController 或 Rigidbody。
- `Update` 仅调用 `RefreshDetection()` 更新只读候选。

### 3.5 程序集依赖方向

**结果：通过。**

```text
WonderSquad.Player      → WonderSquad.Core
WonderSquad.Interaction → WonderSquad.Core
```

- `WonderSquad.Player.asmdef` 不引用 `WonderSquad.Interaction`。
- `WonderSquad.Interaction.asmdef` 只引用 `WonderSquad.Core`。
- Runtime 不引用 Tests、Editor、Puzzle、Inventory、Ability、UI 或 Network。
- Player Prefab 通过 Unity 资产组合挂载 Detector，不改变程序集依赖方向。

### 3.6 每帧分配与禁止查找

**结果：通过。**

- 候选查询使用 `Physics.OverlapSphereNonAlloc`。
- Collider 与已处理 TargetId 缓冲区只在初始化或容量改变时创建。
- Runtime 不使用 LINQ、`GameObject.Find`、Tag 查找、`FindObjectsOfType`、字符串对象查找、`ToList()` 或 `ToArray()`。
- Runtime 不在 Update 中创建集合或订阅事件。
- PlayMode 的预热后检测循环托管分配测试通过。

说明：缓冲区饱和警告只首次记录，不属于稳定帧循环分配路径。

### 3.7 多目标选择稳定性

**结果：通过。**

选择顺序固定为：

1. 通过 Validator。
2. `DetectionPriority` 较高。
3. 距离较近。
4. `InteractionTargetId` 序号较小。

该规则不依赖 `Physics.OverlapSphereNonAlloc` 返回 Collider 的顺序。同一 TargetId 的多个 Collider 被去重；在不同逻辑目标 ID 唯一的前提下，结果具有确定性。

### 3.8 遮挡、自身 Collider 与 Trigger

**结果：通过。**

- 射线命中属于候选自身且 TargetId 相同的 Collider 时，不将自身判为遮挡。
- 射线命中其他对象或无法映射到同一目标时，候选被判为遮挡。
- 范围查询与射线的 `QueryTriggerInteraction` 分别数据化。
- PlayerSandbox 默认范围查询为 `Collide`，可发现 Trigger 目标；遮挡射线为 `Ignore`，测试目标不会成为自身遮挡或阻挡玩家移动。
- 墙体遮挡、遮挡清除恢复与 Trigger 人工验收通过。

首次失败的 `OccludedTarget_IsNotDetectedAndRecoversWhenClear` 射线经过 PlayerSandbox 既有 `P0_VisibleMarker` Collider。该对象按规则构成真实遮挡；最小修复仅改变测试目标方向以隔离场景干扰，生产遮挡逻辑未修改。修复后 PlayMode `26/26` 通过。

### 3.9 未来 InteractionExecutor 与网络权威边界

**结果：通过。**

- 当前输出仅为本地候选，不是权威执行结果。
- `IInteractable` 不提供客户端直接改变共享状态的入口。
- 后续 Executor 可以读取候选并提交带 TargetId 的交互意图，无需让 Detector 依赖输入或网络 SDK。
- 联机阶段必须由主机重新校验玩家、目标、距离、Layer、遮挡、状态与权限，再决定共享结果。
- Detector 保持本地预测/提示能力；Network Adapter 负责传输请求，领域层不直接引用 Photon Fusion 类型。

该边界符合 `Network_Authority_Rules.md` 的“客户端表达意图、主机决定共享结果”原则。

## 4. Gate 矩阵

| 检查项 | 结果 | 证据 |
|---|---|---|
| IInteractable 最小且只读 | 通过 | 4 个只读检测属性，无执行方法 |
| TargetId 稳定 | 通过 | 显式字符串、Ordinal 相等/排序、无效值拒绝 |
| Detector/Validator 分离 | 通过 | Physics 证据与规则校验职责分开 |
| Detector 不执行交互 | 通过 | 无输入、UI、执行或目标写入 |
| Player 不引用 Interaction | 通过 | asmdef 与依赖测试通过 |
| 无每帧 LINQ/查找/GC | 通过 | NonAlloc 固定缓冲区，分配测试通过 |
| 多目标选择稳定 | 通过 | 优先级、距离、TargetId 固定排序 |
| 遮挡与 Trigger 规则 | 通过 | 自身 ID 识别、策略数据化、自动与人工验证通过 |
| Executor 扩展边界 | 通过 | 检测结果与执行职责分离 |
| 网络权威扩展边界 | 通过 | 本地候选，主机未来重新校验 |
| Character Foundation 回归 | 通过 | Player Spawn、Input、Movement、Camera 正常 |
| 自动化回归 | 通过 | EditMode 44/44；PlayMode 26/26 |
| Unity 人工验收 | 通过 | 全部指定场景步骤通过，Console Error 为 0 |

## 5. 范围审计

未实现或引入：

- 交互按键或其他输入消费
- `InteractionExecutor`
- UI Prompt、提示文字、高亮或图标
- Door、Lever、Chest 或具体机关
- Puzzle、Inventory、Item、Ability 或 Network
- Event Bus
- 分配版 `Physics.OverlapSphere`
- Player、Movement、Camera 或 Input 生产逻辑变更

Sprint002A 改动严格限制在 Core 交互检测契约、Interaction Detection、数据资产、Player Prefab 组合、PlayerSandbox 临时验证对象、测试和文档。

## 6. 是否需要在 Sprint002B 前重构

**不需要。**

当前职责、依赖方向、分配边界、确定性选择和网络权威扩展点满足 Sprint002A 目标。唯一需要持续遵守的非阻塞约束是目标 ID 唯一性，以及后续执行层必须重新验证并由权威端决定共享结果；这些不要求当前预先实现编辑器校验、Executor 或 Network 代码。

## 7. Gate 最终结论

# GO

Sprint002A 状态为 `PASSED`，可在新的明确指令与实施前检查后进入 Sprint002B。本次工作到此停止，未开始 Sprint002B。
