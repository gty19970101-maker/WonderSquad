# Sprint002A Preflight Report

## 1. 报告信息

- Sprint：Sprint002A
- 目标：Interaction Detection
- 检查日期：2026-08-02
- Unity 基线：`6000.3.21f1`
- 当前分支：`feature/sprint001d-camera-follow`
- 当前工作区：干净
- Character Foundation Gate：`GO`
- 本次行为：只进行静态预检并生成报告
- Unity 代码与资产修改：无
- 最终结论：**CONDITIONAL GO**

检查基线：

- `CONTRIBUTING.md`
- `CODE_STYLE.md`
- `BRANCH_STRATEGY.md`
- `REVIEW_CHECKLIST.md`
- `Docs/00_Project/Development_Workflow.md`
- `Design/Technical_Architecture_v1.1.md`
- `Docs/01_Project/Prototype_Task_Breakdown.md`
- `Docs/01_Project/Sprint001D_Gate_Review.md`
- `Docs/02_Gameplay/Player_System.md`
- `Docs/02_Gameplay/Interaction_System.md`
- `Docs/04_Technical/Network_Authority_Rules.md`
- `Docs/05_Data/Data_Driven_Design.md`

## 2. 结论摘要

Interaction Detection 的系统边界已经足够明确，可以在不破坏 Character Foundation 的前提下实施：

- 工程已存在空的 `WonderSquad.Interaction` 程序集。
- 该程序集当前只依赖 `WonderSquad.Core`，方向符合技术架构。
- Player、Movement、Camera 和 Spawner 均未依赖 Interaction。
- 全仓库没有已有的 Interaction Runtime 实现，不存在重复实现。
- New Input System 已存在 `Gameplay/Interact`，绑定为键盘 `E` 与手柄 `buttonWest`；Sprint002A Detection 不需要修改输入资产。
- `GameplaySandbox` 是计划文档指定的 Interaction 隔离验证场景。
- Character Foundation 已通过 Unity 编译、自动化与人工 Gate。

技术上不存在必须先重构的阻塞项。当前不能直接开始实现的原因是 Git 治理基线尚未完成：

1. 当前仍位于 Sprint001D 分支，不能复用该分支开发 Sprint002A。
2. 本地 `develop` 比当前 Character Foundation 提交少 2 个提交，尚不包含 Camera 与最终 Gate。
3. 现有 Tag `v0.3-character-foundation` 指向 `f4beb0f`，早于 Sprint001D Camera 提交 `6285514`。如果该 Tag 被视为正式 Character Foundation 版本节点，它目前不能完整代表已验收范围。

完成第 15 节的 Git 条件后，可以开始 Sprint002A 实现。

## 3. 当前工程状态

| 检查项 | 当前结果 | 判定 |
|---|---|---|
| Unity 版本 | `6000.3.21f1` | 通过 |
| 工作区 | 干净 | 通过 |
| Character Foundation | 整体 Gate 为 `GO` | 通过 |
| Interaction 程序集 | 已存在，只有 asmdef | 通过 |
| Interaction 业务代码 | 不存在 | 通过 |
| 重复 Detector 或 IInteractable | 未发现 | 通过 |
| Player 对 Interaction 的引用 | 不存在 | 通过 |
| Interact Input Action | 已存在：`E` / Gamepad `buttonWest` | 通过 |
| 推荐 Sandbox | `GameplaySandbox` 已存在 | 通过 |
| 当前开发分支 | 仍为 Sprint001D 分支 | 条件未满足 |
| `develop` 基线 | 未包含当前 Sprint001D 的 2 个提交 | 条件未满足 |
| Character Foundation Tag | 未包含 Camera 完成提交 | 条件待确认 |

## 4. Interaction 系统所属程序集

### 4.1 结论

Interaction Detection 应属于现有程序集：

```text
Client/Assets/WonderSquad/Runtime/Interaction/
└── WonderSquad.Interaction.asmdef
```

建议根命名空间：

```text
WonderSquad.Interaction
WonderSquad.Interaction.Detection
```

不需要再创建第二个 Interaction Runtime 程序集。当前功能规模不足以支持把 Detection、Request 和 Execution 拆成多个 asmdef；过早拆分会增加小团队维护成本。

### 4.2 允许的程序集引用

`WonderSquad.Interaction` 保持：

```text
WonderSquad.Interaction
└── WonderSquad.Core
```

本阶段不应增加以下引用：

- `WonderSquad.Player`
- `WonderSquad.Camera`
- `WonderSquad.Puzzle`
- `WonderSquad.Item`
- `WonderSquad.Inventory`
- `WonderSquad.Ability`
- `WonderSquad.Network`
- `Unity.Cinemachine`
- Photon Fusion
- UI

## 5. 与 Player 程序集的依赖方向

### 5.1 目标方向

```text
WonderSquad.Player ───────→ WonderSquad.Core

WonderSquad.Interaction ──→ WonderSquad.Core
```

Player 与 Interaction 之间不建立直接编译时引用。

### 5.2 Player Prefab 组合规则

未来可将 `InteractionDetector` 作为组件挂到 Player Prefab 或其独立 `InteractionOrigin` 子对象。Prefab 同时组合 Player 与 Interaction 组件，不等于 `WonderSquad.Player` 程序集引用 `WonderSquad.Interaction`。

必须保持：

- `PlayerMovement` 不读取 Interaction 状态。
- `PlayerInputReader` 不承担目标检测。
- `PlayerSpawner` 不注册或查找 Interaction 目标。
- Camera 不提供 Interaction 朝向或执行结果。
- Interaction Detector 使用显式 Detection Origin，不通过 Player 具体类型获取位置。

若未来需要本地/远端玩家开关 Detector，应由外层玩家组合或 Network Adapter 控制组件启用状态，不把 Photon 类型放入 Interaction。

## 6. IInteractable 接口设计位置

### 6.1 结论

`IInteractable` 是 Interaction、Item、Puzzle、Status、Crafting 与未来 Network Adapter 都需要理解的跨领域契约，应位于：

```text
Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/
```

建议命名空间：

```text
WonderSquad.Core.Contracts.Interaction
```

它仍编译进现有 `WonderSquad.Core` 程序集，不新建 `WonderSquad.Contracts` asmdef。

当前 Core 尚无 `Contracts/` 目录。创建最小 Interaction Contracts 目录属于 Sprint002A 的预计实现范围，不是现有工程错误。

### 6.2 接口职责

`IInteractable` 只应表达跨领域可见的最小、只读目标能力：

- 稳定的目标标识。
- 交互锚点或由 Interaction Adapter 提供的空间参考。
- 本地候选优先级。
- 基于查询上下文返回可用性、拒绝原因键和提示语义键。
- 必要的目标 Revision 查询边界。

不得在接口中加入：

- 直接修改目标状态的无条件 `Interact()`。
- Button、Door、ResourceNode 等具体类型。
- Inventory、Puzzle 或 Ability 实现对象。
- UI Widget、已本地化字符串或图标实例。
- Photon、RPC、`NetworkObject` 或 Network Runner。
- 把本地 Focus 当成执行成功的布尔结果。

### 6.3 配套契约

按最小需求建议预留同一目录：

- `InteractionTargetId`
- `InteractionQueryContext`
- `InteractionAvailability`
- `InteractionCandidateSnapshot`
- `InteractionCommand`
- `IInteractionRequestPort`

Sprint002A 只实现 Detection 所需的查询与候选契约。`InteractionCommand` 可以定义为网络无关的不可变数据形状，用于锁定 Detection 与后续 Request 层边界，但 Detector 不得自动执行或发送该命令。

物理 Collider 与 Transform 的 Unity 适配应留在 `WonderSquad.Interaction`，不把物理扫描规则放进 Core Contracts。

## 7. Detection 与 Execution 分离

### 7.1 Detection

Sprint002A 的 Detection 流程：

```text
读取 Detection Origin
→ 进行有限范围物理查询
→ 将 Collider 映射为 IInteractable
→ 按目标标识去重
→ 检查距离、方向、遮挡和本地可用性
→ 使用稳定规则排序
→ 输出只读 Focused Candidate
→ 仅在 Focus 改变时发布本地事件
```

Detection 只回答：

- 当前有哪些候选目标。
- 哪个目标最适合作为本地 Focus。
- 当前本地提示应为可用还是不可用。

### 7.2 Execution

Execution 属于后续切片：

```text
玩家产生 Interact 意图
→ Requester 读取当前 Focus
→ 构造 InteractionCommand
→ 离线 Authority Adapter 或未来 Network Adapter 接收
→ 权威端重新校验
→ 目标所属领域执行规则
→ 返回 Accepted / Rejected
```

Execution 必须重新检查：

- 请求者。
- TargetId。
- 距离与遮挡。
- 目标状态与 Revision。
- 权限、角色能力、资源和多人槽位。
- RequestId 是否重复。

Sprint002A 不实现 Interaction 输入读取、请求发送、目标状态变更、机关执行、资源采集或网络 RPC。

## 8. 未来多人合作支持

### 8.1 本地 Detection

以下数据只属于本地表现，不需要同步：

- 当前候选列表。
- Focused Target。
- 本地高亮。
- 本地距离、方向和遮挡评分。
- 本地提示选择。

每个本地玩家客户端可以独立检测和选择目标。远端玩家对象不应运行本地输入对应的 Detector。

### 8.2 主机权威 Execution

未来联机时：

- 客户端只发送稳定 `PlayerId`、`TargetId`、`RequestId`、必要输入和目标 Revision。
- 主机重新验证空间、目标状态和请求权限。
- 共享交互开始、多人槽位、持续进度、完成、取消和拒绝由主机决定。
- 两人同时请求同一目标时只能产生一个权威结果。
- 重复 RequestId 返回已有结果，不重复发放、消耗或触发。
- 客户端 Focus 过期时，主机拒绝并由本地表现回退。

这使 Sprint002A 的本地 Detection 可被未来 2～4 人联机复用，但不提前引入 Network 代码。

## 9. 避免 Player 直接依赖具体机关

Player 只提供空间组合位置，不识别机关类型。

禁止：

```text
Player → Button
Player → Door
Player → ResourceNode
Player → Puzzle
Player → CraftingStation
```

允许：

```text
InteractionDetector
→ IInteractable
→ InteractionCandidateSnapshot
```

后续具体机关、物品或救援对象通过 Core Contract 或各自 Adapter 提供交互目标，不让 Player 保存具体目标类型或调用具体业务方法。

Sprint002A 测试目标只能是通用占位 `Interactable Test Target`，不能顺带实现 Button、Door、Resource、Inventory 或 Puzzle。

## 10. 是否需要事件总线

### 10.1 结论

**不需要全局事件总线。**

Sprint002A 只需要局部、显式事件，例如：

- `FocusedTargetChanged`
- `CandidateAvailabilityChanged`，仅在确有测试或表现需求时加入

事件发布者是具体 Detector 实例，订阅者在生命周期内成对订阅和取消订阅。

后续执行请求使用显式 `IInteractionRequestPort`，不使用静态事件总线、全局 Singleton 或字符串消息。

原因：

- 当前只有一个本地 Detector 消费链路。
- 全局事件总线会隐藏依赖和生命周期。
- 多人环境下静态事件容易混淆不同本地玩家与场景实例。
- UI 尚不属于 Sprint002A，不需要提前建立跨场景消息基础设施。

## 11. Physics 检测方案

### 11.1 推荐方案

推荐使用显式、非分配的物理查询：

```text
Physics.OverlapSphereNonAlloc
→ LayerMask 筛选
→ IInteractable 去重
→ 距离与方向评分
→ Physics.Raycast 或 Linecast 遮挡检查
```

Unity 6 的 Physics API 仍提供 `OverlapSphereNonAlloc`，可把结果写入调用方提供的 Collider 缓冲区；Raycast/Linecast 支持 `LayerMask` 与 `QueryTriggerInteraction`。参考：

- [Unity 6 Physics API](https://docs.unity3d.com/6000.0/ScriptReference/Physics.html)
- [Unity 6 Physics.Raycast](https://docs.unity3d.com/6000.0/ScriptReference/Physics.Raycast.html)
- [Unity 6 Physics.Linecast](https://docs.unity3d.com/6000.0/ScriptReference/Physics.Linecast.html)

检测参数应进入 `InteractionDetectionSettings`：

- 检测半径。
- 最大面对角或最小方向点积。
- Focus 切换滞后阈值。
- Interactable LayerMask。
- Occlusion LayerMask。
- Trigger 查询策略。
- 候选缓冲容量。
- 必要时的检测刷新间隔。

候选排序必须稳定，建议顺序：

1. 本地可用性。
2. 明确的目标优先级。
3. 面对方向。
4. 距离。
5. 稳定 TargetId 作为最终平局规则。

同一目标存在多个 Collider 时必须按目标标识去重。缓冲区达到上限时应提供明确诊断，不能静默产生随机 Focus。

### 11.2 不推荐将 Trigger 作为默认方案

本阶段不推荐给 Player 增加大范围 Trigger Collider 与 Rigidbody 作为主要候选缓存。

Unity 的 Trigger 回调要求满足 Collider、Trigger 和物理体组合条件；这会给当前以 `CharacterController` 为唯一移动核心的 Player 增加额外物理生命周期和配置风险。参考：

- [Unity 6 OnTriggerEnter](https://docs.unity3d.com/6000.0/ScriptReference/MonoBehaviour.OnTriggerEnter.html)
- [Unity 6 Collider.isTrigger](https://docs.unity3d.com/6000.0/ScriptReference/Collider-isTrigger.html)

Trigger 方案还需要额外处理：

- 目标禁用或销毁后候选残留。
- 子 Collider 重复进入与退出。
- Domain Reload 或组件禁用时缓存清理。
- Rigidbody 与 CharacterController 所有权混淆。

若后续性能测试证明显式查询不足，再用独立专项比较 Trigger 缓存；Sprint002A 不为假设性能问题增加复杂度。

## 12. 对 Character Foundation 的影响

### 12.1 不应改变

- `PlayerSpawner` 的本地单实例生成行为。
- `PlayerInputReader` 的 Move Input 生命周期。
- `PlayerMovement` 的 CharacterController 位移、旋转、重力与接地。
- `GroundDetector`。
- `PlayerCameraTarget`。
- `CameraTargetBinder` 与 Cinemachine Rig。
- `MovementSettings` 和 `CameraFollowSettings`。

### 12.2 允许的最小组合变化

实施阶段允许：

- 在 Player Prefab 增加独立 `InteractionOrigin` 子对象。
- 在 Player Prefab 组合 `InteractionDetector`。
- 为 Detector 配置 `InteractionDetectionSettings`。
- 在 `GameplaySandbox` 增加通用测试目标和遮挡物。

这些变化不能：

- 修改 Player Transform 的移动所有权。
- 添加 Rigidbody 作为玩家移动核心。
- 改变 CharacterController 尺寸来迁就 Interaction。
- 改变 Camera 构图、跟随或输入。
- 让 Interaction Detector 移动、旋转或禁用 Player。

Character Foundation 的现有 EditMode `37/37` 与 PlayMode `19/19` 必须作为 Sprint002A 回归基线。

## 13. 推荐实施文件范围

以下只是预检清单，本次没有创建：

### Core Contracts

- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/IInteractable.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionTargetId.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionQueryContext.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionAvailability.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionCandidateSnapshot.cs`
- `InteractionCommand` 仅在本 Sprint 明确包含标准命令数据形状时创建

### Interaction Runtime

- `Client/Assets/WonderSquad/Runtime/Interaction/Detection/InteractionDetector.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Detection/InteractionTarget.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Detection/InteractionCandidateSelector.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Detection/InteractionDetectionSettings.cs`

最终类型数量应以最小可测试职责为准，不为列表中的名称机械创建空抽象。

### Assets

- `Client/Assets/WonderSquad/ScriptableObjects/Configuration/InteractionDetectionSettings.asset`
- `Client/Assets/WonderSquad/Prefabs/Interaction/PF_Interactable_Test.prefab`
- `Client/Assets/WonderSquad/Prefabs/Player/Player.prefab`
- `Client/Assets/WonderSquad/Scenes/Tests/GameplaySandbox.unity`
- 所有新增资产对应的 `.meta`

### Tests

- `Client/Assets/WonderSquad/Tests/EditMode/InteractionDetectionEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionDetectionPlayModeTests.cs`
- EditMode 与 PlayMode 测试 asmdef 只增加必要的 `WonderSquad.Interaction` 引用

### 本阶段不需要修改

- `WonderSquad.inputactions`
- `PlayerInputReader.cs`
- `PlayerMovement.cs`
- `GroundDetector.cs`
- `PlayerSpawner.cs`
- Camera Runtime
- Puzzle、Item、Inventory、Ability、Network 或 UI 程序集

## 14. 测试方案

### 14.1 EditMode

至少覆盖：

1. `InteractionDetectionSettings` 默认配置有效。
2. 非法半径、角度、LayerMask、缓冲容量或非有限值被拒绝。
3. 距离、面对方向、优先级和稳定 TargetId 排序正确。
4. 相同目标的多个 Collider 被去重。
5. 无目标时返回空 Focus。
6. 不可用、越距、背向和被遮挡目标被正确过滤。
7. Focus 滞后规则避免相邻目标轻微抖动时频繁切换。
8. Detection 查询不产生执行副作用。
9. `WonderSquad.Interaction` 只引用 `WonderSquad.Core`。
10. Player 程序集未增加 `WonderSquad.Interaction` 引用。

### 14.2 PlayMode

至少覆盖：

1. 玩家靠近单一目标后获得 Focus。
2. 离开范围后 Focus 清空。
3. 同时面对两个目标时选择结果稳定。
4. 隔墙目标不能成为可执行 Focus。
5. 遮挡移除后目标可以重新被发现。
6. 目标禁用或销毁后安全清理，不产生 NullReferenceException。
7. 同一目标多个 Collider 不产生重复候选。
8. Detector Disable 后清空 Focus，重新 Enable 后恢复检测。
9. Detection 不移动或旋转 Player。
10. PlayerSpawner、PlayerInputReader、PlayerMovement 和 Camera 回归继续通过。
11. 场景中不生成额外 Main Camera、Player 或 Rigidbody 移动对象。
12. Console Error 为 0。

### 14.3 GameplaySandbox 人工验证

按 `Prototype_Task_Breakdown.md` 放置：

- 近距目标。
- 远距目标。
- 遮挡目标。
- 重叠目标。
- 不可用目标。

人工检查：

1. 使用 Character Foundation 正常移动和观察。
2. 接近、离开和绕过五类目标。
3. 验证 Focus 选择稳定。
4. 验证墙后目标不可选。
5. 验证 Player 移动和 Camera 手感未改变。
6. 确认当前只有检测或调试可视化，没有目标状态变化。
7. Console Error 为 0。

Sprint002A 不需要键盘 `E` 或手柄交互执行验收；输入和 Execution 应在后续独立切片验证。

## 15. 实施前必须关闭的条件

### C-01：完成 Character Foundation 回流

- 当前：`develop` 位于 `f4beb0f`，当前 Sprint001D 完成提交为 `6285514`。
- 差异：当前分支领先 `develop` 2 个提交。
- 要求：按 Review 流程将已通过 Gate 的 Character Foundation 合并到 `develop`，并确认 `develop` 仍可编译和通过相关回归。
- 本报告不执行 Merge。

### C-02：创建 Sprint002A 独立分支

- 当前：`feature/sprint001d-camera-follow`。
- 要求：从包含完整 Character Foundation 的最新 `develop` 创建：

```text
feature/sprint002a-interaction-detection
```

- 不得继续复用 Sprint001D 分支。
- 本报告不创建或切换分支。

### C-03：确认 Character Foundation 版本节点

- 当前 Tag：`v0.3-character-foundation` → `f4beb0f`。
- 问题：该提交不包含 Sprint001D Camera 与最终 Gate。
- 要求：若该 Tag 是正式版本节点，应通过项目治理流程建立一个包含完整 Character Foundation 的新不可变版本节点，或明确记录现有 Tag 仅代表 Sprint001A～C。
- 不应在未确认远程发布状态时直接移动或覆盖现有 Tag。
- 本报告不修改 Tag。

## 16. 风险与实施约束

| 风险 | 级别 | 预防措施 |
|---|---|---|
| 本地 Focus 被误当作权威成功 | 高 | Detection 与 Execution 完全分离，主机重校验 |
| Player 直接引用按钮或机关 | 高 | 只依赖 Core Contracts，不引用具体领域 |
| 为 Detector 添加 Rigidbody 干扰 CharacterController | 高 | 默认使用显式非分配 Physics 查询 |
| 相邻目标间 Focus 闪烁 | 中 | 稳定排序、TargetId 平局规则和切换滞后 |
| 多 Collider 造成重复候选 | 中 | 按 IInteractable/TargetId 去重 |
| 遮挡 Layer 配置错误 | 中 | 独立 Interactable 与 Occlusion LayerMask，加场景测试 |
| 非分配缓冲区满后漏目标 | 中 | 配置容量、饱和诊断和密集候选测试 |
| 全局事件污染多个玩家 | 高 | 只使用 Detector 实例事件，不建立静态事件总线 |
| Sprint002A 顺带实现执行或机关 | 高 | 把输入、命令发送、机关结果和网络列为非目标 |
| 在旧 Sprint 分支继续开发 | 阻塞 | 关闭 C-01、C-02 后才开始实现 |

## 17. 最终结论

# CONDITIONAL GO

Interaction Detection 的模块、契约、依赖、物理检测和测试方案均已明确，不需要重构 Character Foundation。

在以下条件全部关闭前不要开始 Sprint002A 实现：

1. Character Foundation 合并并验证到 `develop`。
2. 从最新 `develop` 创建 `feature/sprint002a-interaction-detection`。
3. 明确完整 Character Foundation 的版本节点，避免现有 Tag 与已验收范围不一致。

本次只新增本预检报告，没有修改 Unity 代码、Prefab、Scene、Input Actions、Package 或其他业务文档。
