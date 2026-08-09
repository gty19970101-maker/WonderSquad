# Sprint003C Preflight Report — Forest Signal Environment Consequence

## 1. 最终结论

# GO

Sprint003C 可以进入 Implementation Plan 阶段。Sprint003A Greybox 与 Sprint003B Forest Signal Trial 已提供完成本 Sprint 所需的场景、恢复机制，以及只读状态消费边界；不需要修改 Character Foundation、Interaction Foundation 或 `ForestSignalTrial` 核心状态机。

本 Preflight 只完成设计与架构审计。未修改代码、Prefab、Scene、ScriptableObject、Input Actions、Packages 或 ProjectSettings。

## 2. 当前稳定基线

- Unity：`6000.3.21f1`
- Sprint003A — `PASSED / GO`
- Sprint003B — `PASSED / GO`
- Sprint003B：EditMode `18/18`、PlayMode `4/4`
- 完整回归：EditMode `100/100`、PlayMode `54/54`
- Console Error：`0`
- 正式场景：`Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity`

Sprint003B 的 `ForestSignalTrial Completed` 仅表示信标试炼的 A → B 顺序已完成；它不是关卡完成，也不改变 Root Bridge、路线或 Slice End。这个边界在 Sprint003C 前继续成立。

## 3. Sprint003C 目标

将既有的信标完成状态转化为一个玩家可观察、可选择且不可软锁的 SleepingForest 环境后果：

```text
ForestSignalTrial Snapshot / TrialStateChanged
→ SleepingForestRootBridgeConsequence
→ 静态场景对象的可见性、Marker 与捷径 Collider 状态
→ 玩家选择 Main Route 或已激活的 Advantage Route
→ 继续走向 Slice End（003D 才判断完成）
```

验证重点为：

- 玩家看见 Trial Completed 改变世界；
- 通用 Main Route 始终可通；
- Advantage Route 成为有明确收益、但非通关必需的选择；
- IncorrectOrder 不永久伤害环境或路线；
- 失败、掉落和重复事件都可以恢复。

## 4. 只读状态消费边界

003C 只能通过 Inspector 显式引用同一 Scene 的 `ForestSignalTrial`，并使用：

- `CurrentSnapshot`：初始化、重新启用与 Scene 进入后的状态对齐；
- `TrialStateChanged(ForestSignalTrialSnapshot)`：新 Revision 的实例级通知；
- `ForestSignalTrialSnapshot.IsCompleted`、`HasIncorrectOrder`、A/B Runtime State 与 `Revision`：只读决策数据。

禁止：

- 读取 Trial 私有字段；
- 调用或修改 Beacon A/B 的状态；
- 改写 A → B 或 IncorrectOrder 规则；
- 每帧/按名称/全局查找 Trial；
- static event、Global Event Bus、Service Locator 或全局 Environment Manager。

建议的消费规则：

1. `OnEnable` 订阅已序列化 Trial 的实例事件，并立即读取 `CurrentSnapshot`。
2. 仅当收到比 `lastAppliedRevision` 更新的 Snapshot 时应用后果；初始化可无条件从当前 Snapshot 重建表现。
3. `IsCompleted == true` 应用 Activated 环境；否则应用 Initial 环境。
4. 同一 Revision 重放不得重复切换对象、产生额外 Collider、写材质资产或重新定位玩家。
5. `IncorrectOrder` 只驱动短暂、本地、非阻塞的提示表现；绝不改变路线通行性。

## 5. Root Bridge 最小状态模型

### 5.1 状态

不建立通用 Bridge/Puzzle 框架。创建一个 SleepingForest 内容专用的组合式 Scene 组件，例如 `SleepingForestRootBridgeConsequence`，只表达：

| 状态 | 条件 | Main Route | Advantage Route | 说明 |
| --- | --- | --- | --- | --- |
| `Initial` | Trial 未完成 | 现有 `RootBridgeTemporaryCrossing` 保持通行 | `RootBridgeAdvantageSpan` 不通行 | 通用路线始终作为保底解。 |
| `Activated` | Snapshot `IsCompleted` | 既有临时跨越保持不变 | 激活独立的捷径 Span 与路线 Marker | 信标完成带来可见世界变化和可选效率收益。 |

`Failed`、`Broken`、`Recovery` 不是 Root Bridge 的持久状态。掉落恢复仍由既有 `SleepingForestFallRecovery` 处理；IncorrectOrder 不是桥失败。

### 5.2 锁定的环境方案

采用**保留静态主路、激活独立捷径段**：

- `RootBridgeTemporaryCrossing` 继续作为宽、稳定、始终可用的 Main Route Crossing；003C 不移动、不缩放、不禁用它的 Collider。
- 新增 `RootBridgeAdvantageSpan`，初始时 Renderer 和 Collider 都关闭；Trial Completed 后同时启用。它连接已存在的高位 Advantage Route 接入口与 Root Bridge Exit / Slice End 侧安全平台，形成更短的连通路径。
- `RootBridgeAdvantageMarkers` 在 Activated 时显示：根系拱、方向性叶片/灯标和非颜色形状标记；它们让“捷径已出现”不依赖颜色或文字。
- `RootBridgeDormantMarker` 仅在 Initial 显示，表达根系尚未响应。Completed 后隐藏。

捷径的收益是**更短的步行距离与更清晰的出口导向**；它不降低 Main Route 的合法性，也不要求任何角色能力。未来可通过预定义 AbilityTag 让特定角色更早辨识或更安全使用该捷径，但 Sprint003C 不实现 Ability System。

## 6. Main / Advantage / Recovery Route 职责

| 路线 | 003C 职责 | 可用性 | 玩法价值 | 禁止退化 |
| --- | --- | --- | --- | --- |
| Main Route | 保底通用路线：Beacon B 后经现有 Root Bridge Temporary Crossing 到 Slice End 侧 | 始终可用 | 稳定、安全、所有玩家可完成 | 不因 Trial、错误或捷径激活而断开。 |
| Advantage Route | 可选效率路线：激活后经高位入口与新 Advantage Span 更快到达出口侧 | 仅 Trial Completed 后完整可用 | 明显缩短距离、由视觉 Marker 清晰指向 | 不能成为唯一通路，也不能只是颜色不同的同一条路。 |
| Recovery Route | 对掉落、走错或离开几何的恢复环路 | 不承担推进/捷径 | 回到安全地面和可继续位置 | 不能成为第三条等价主路线或绕过 Trial 的通关解。 |

现有 Greybox 在 003A 阶段允许 Main / Advantage 都有物理可达性。003C 不回改该历史验收；Implementation 必须通过新增专用 Advantage Span 的“距离/导向优势”创造实际路线差异，而不是强行封死当前保底几何或改变既有 Main Route Collider。

## 7. IncorrectOrder 环境处理

`IncorrectOrder` 不改变桥、路线 Collider 或任何永久环境状态。

可选且推荐的最小反馈为 `RootBridgeIncorrectMarker`：

- 仅在 `snapshot.HasIncorrectOrder` 且当前 Revision 首次应用时显示一个短暂的、局部根系收缩/X 形 Marker；
- 可使用独立 MaterialPropertyBlock 与 Marker 显隐；
- 不关闭 Main Route、不破坏 Advantage Span、不触发 FallRecovery、不修改 Trial；
- 当后续 A 的合法激活或最终 Completed Snapshot 到来时，标记清除；
- 重复收到同一 Revision 不重复播放或分配。

此反馈不是必需的失败系统。若 Implementation 评审认定短暂表现会引入额外计时/动画复杂度，可保留 Beacon B 已有 Incorrect Marker，并让 Root Bridge 在非 Completed 时保持 Initial；不得以此扩大为 Failure Framework。

## 8. 动态几何、物理与软锁审计

### 8.1 锁定的安全策略

**不在运行时移动、旋转、缩放、销毁或禁用任何玩家可能站立的现有 Main Route / RootBridgeTemporaryCrossing Collider。**

环境变化只允许：

- 启用初始 inactive 的、与 Main Route 不重叠的 `RootBridgeAdvantageSpan` Collider；
- 切换纯视觉 Marker / Renderer；
- 切换不承载玩家的 Decorator 对象。

因此 Trial 完成时，站在桥上的玩家仍站在原有稳定地面上；新增捷径不会把 Player 夹入 Collider 或从脚下移走几何。

### 8.2 Collider / Renderer 规则

- Span 的 Renderer 与 Collider 同一状态切换；Initial 时二者均 disabled，Activated 时二者均 enabled。
- `RootBridgeAdvantageSpan` 必须是独立的单层 Ground Collider；不得在 `RootBridgeTemporaryCrossing` 或 `SliceEndGround` 顶面下/上叠加第二个共面大平面。
- 接缝只允许在明确 Junction Platform 边界相接，不能把 Span Cube 插入既有平台内部。
- 连接面高度与现有路面一致；不采用全局 Y 偏移掩盖问题。若必需局部过渡，使用单一、无台阶感的斜坡/承接平台，并在 Implementation 进行 Bounds 审计。
- Decoration、Marker 与错误反馈均不加阻挡 Player 的 Collider。
- 视觉颜色覆盖使用每 Renderer 的 `MaterialPropertyBlock`，不访问 `Renderer.material`，不写 `sharedMaterial`。
- 稳定帧无轮询、LINQ、字符串拼接、对象/集合创建或重复 SetPropertyBlock。

### 8.3 必测软锁场景

| 情形 | 预期结果 |
| --- | --- |
| Player 站在 Main Bridge 时 Trial Completed | Main Bridge 不变；玩家不被移动、夹住或掉落。 |
| Player 位于 Junction / Span 接口附近 | 只增加独立 Span；既有 Collider 不变，仍能退回 Main Route。 |
| 玩家先走 Main Route | 始终可继续走向 Slice End。 |
| 玩家先接近 Advantage Route | 未完成时能返回 Main Route；完成后可选择 Shortcut，不能被困。 |
| IncorrectOrder 后继续 A → B | Initial 保底路线不变；完成后正常 Activated。 |
| FallRecovery 在环境切换前后触发 | 恢复到既有 RecoveryPoint 后可重新走 Main Route；不依赖 Span。 |
| Completed 事件重复 / 同 Revision 重放 | 不重复生成/启用对象，不改变既有 Collider。 |
| Scene 重新进入 | Trial 从 Revision 0 / Initial 环境开始；完成后事件或 Snapshot 可重建 Activated 环境。 |

## 9. Scene 生命周期与 Authoring

- Scene Runtime State 只存在于 `ForestSignalTrial` 和局部环境消费者；不跨 Scene、存档或静态保存。
- 重新进入 SleepingForest 时，`ForestSignalTrial` 重新初始化为 Initial；`SleepingForestRootBridgeConsequence.OnEnable` 从 `CurrentSnapshot` 同步 Initial。
- Trial 完成后，实例事件将消费者切换至 Activated。Disable/Enable 后再次读取 Snapshot 即可恢复正确状态。
- 003C 可以采用一个显式、确认式、幂等的 Sprint003C Scene Authoring 菜单，或在通过审查后手工 Inspector 配置；不得恢复自动场景创建、不得运行 003A Greybox Rebuild。
- Authoring 必须保存 Scene，并验证唯一的 Trial 引用、Main/Advantage/Recovery 对象引用、Renderer/Collider 成对配置与无重复 Consequence 组件。

## 10. 文件与程序集边界建议

### Runtime

建议放入 `WonderSquad.Puzzle` 的 `Runtime/Puzzle/SleepingForest/`：

- `SleepingForestRootBridgeState.cs`：仅 `Initial` / `Activated`；
- `SleepingForestRootBridgeConsequence.cs`：消费 Trial Snapshot，管理显式引用的静态场景表现；
- 可选 `SleepingForestRootBridgeVisual.cs`：仅当表现组件职责与 Collider 状态管理需要拆分时新增，否则保持一个局部组件。

不新增通用 `Bridge`、`Puzzle`、`EnvironmentState` 或 `GameplayObject` 基类。Runtime Puzzle 仍只依赖 Core / Content；不引用 Interaction、Player、Camera、UI 或 Network。

### Editor / Content

- `Editor/SleepingForestRootBridgeAuthoring.cs`：仅显式菜单和 Scene 组合校验；不自动运行。
- `Prefabs/SleepingForest/PF_RootBridgeAdvantageSpan.prefab`：如需要可复用的 Span 几何；只服务该内容，不能发展为通用桥模板。
- 无需为本 Sprint 新建可变 ScriptableObject 状态；静态尺寸、颜色或 Marker 引用可先序列化在局部 Scene 组件。若后续确有多 Variant，才将静态参数迁入版本化 Content Definition。

## 11. 测试计划

### EditMode

至少覆盖：

1. Consequence 的显式 Trial、Main Route、Advantage Span、Marker、Renderer/Collider 配置有效且同 Scene。
2. 初始 Snapshot 应用后：Main Bridge 保持、Advantage Span Renderer/Collider 均 disabled、Dormant Marker 正确。
3. Completed Snapshot 应用后：Main Bridge 不变，Advantage Span Renderer/Collider 同时 enabled，Activated Marker 正确。
4. 相同 Revision 重放幂等：不重复切换、不重复产生对象/Collider。
5. IncorrectOrder 不永久改变路线；后续 Completed Snapshot 正常得到 Activated。
6. 003C 不修改 Beacon A/B Runtime State、Trial Snapshot 或 Definition。
7. Disable/Enable 或重建消费者后从 CurrentSnapshot 正确初始化。
8. Span/Platform Renderer Bounds 与 Collider Bounds 不发生正体积重叠；无共面双层地面。
9. MaterialPropertyBlock 为 Renderer 实例独立，shared Material 保持未修改。
10. asmdef 方向保持；Foundation 与 Core Interaction 合同没有变更。

### PlayMode

至少覆盖：

1. 加载 SleepingForest 后为 Initial：Main Route 可用，Advantage Span 未启用。
2. A → B 后 Trial Completed，Root Bridge Activated 与 Advantage Span 按预期显示/通行。
3. Player 在 Main Bridge / Junction 附近完成 Trial 后不被卡死、不掉落，仍可移动并接地。
4. Main Route 在变化前后均能通行；Advantage Route 在 Activated 后具有预定义的距离/导向优势。
5. Recovery Route 与 FallRecovery 在 Initial / Activated 均可工作。
6. IncorrectOrder → A → B 最终应用 Activated，未保留永久错误环境。
7. 重复 Completed、重复 Revision、组件重新启用不重复应用后果。
8. Beacon Detection、Prompt、Execution、Repeat Protection 回归正常。
9. Movement、GroundDetector、Camera 与单 Player/单 Main Camera 回归正常。
10. 完整 003A/003B、Sprint002A–D 回归通过，Console Error `0`。

### 人工验收

**流程 A：Main Route**

1. A → B 完成 Trial，观察 Root Bridge Marker 与 Advantage Span 激活。
2. 不使用捷径，经 Main Route 走向 Slice End 区域。
3. 确认 Main Bridge 未移动、无卡顿，且到达终点仍无 Completion UI（符合 003D 边界）。

**流程 B：Advantage Route**

1. A → B 完成 Trial。
2. 选择 Advantage Route，确认 Marker 能在不开语音情况下传达捷径方向。
3. 确认其距离/导向优势明显，但返回 Main Route 后仍可继续。

**流程 C：IncorrectOrder 恢复**

1. B → A → B。
2. 确认错误没有永久破坏桥或路线；完成后 Root Bridge 正常 Activated。

**流程 D：FallRecovery**

1. 在环境 Initial 和 Activated 各触发一次安全跌落恢复。
2. 确认 Recovery 后可回到保底 Main Route 并继续流程。

**流程 E：重复与站位安全**

1. 站在 Main Bridge / Junction 附近重复触发或重放 Completed 状态。
2. 确认不会重复改变 Geometry、不会夹住 CharacterController、不会产生双 Collider、Console Error 为 0。

## 12. 严格保留给 Sprint003D 的边界

Sprint003C 不实现：

- Slice End Trigger；
- `Level Completed` 状态；
- Completion UI、弹窗、下一关或结果流程；
- Build Profile 最终入口治理。

003C 的完成定义是“玩家在 A → B 后能看见并使用环境后果，继续可走向 Slice End”，而不是“玩家抵达 Slice End 后得到通关反馈”。

## 13. 风险与 Gate 条件

| 风险 | 级别 | 控制 / Gate 条件 |
| --- | --- | --- |
| 动态 Bridge 改变脚下 Collider 导致卡死或掉落 | 阻塞 | 冻结 Main Bridge Collider；仅启用独立、初始 inactive 的 Advantage Span。 |
| 新 Span 与 003A 地面共面重叠 | 阻塞 | 实施前完成 Renderer/Collider Bounds 审计；连接到 Junction 边缘，不插入既有 Cube。 |
| Advantage 仍是等价路线 | 高 | 必须记录并人工验证具体距离/导向优势；不可只改颜色。 |
| IncorrectOrder 造成永久环境状态 | 高 | 只允许临时本地表现；Completed 始终覆盖为 Activated。 |
| 消费者绕过 Trial 或轮询全局状态 | 高 | Inspector 显式引用 + Snapshot/Event；测试禁止静态/查找依赖。 |
| 003C 越界实现完成流程 | 高 | PlayMode 和 Scene 审计继续要求无 End Trigger / Completion UI。 |
| 内容组件膨胀为通用框架 | 中 | 保持 SleepingForest 专用、组合式、有限状态组件。 |

### Definition of Ready

- [x] 003A / 003B 已通过 Gate，且结果已记录。
- [x] Trial 的 Snapshot、Revision 与实例事件足以作为只读输入。
- [x] 选择了不移动现有承载几何的安全环境方案。
- [x] Main、Advantage、Recovery 路线职责与软锁策略已明确。
- [x] 003D 完成边界已明确保留。
- [ ] Implementation Plan 必须锁定 Span 的最终 Scene 位置、Junction 接缝 Bounds 与 Authoring 操作，再开始实现。

## 14. Gate outcome

# GO

可以进入 Sprint003C Implementation Plan。不得直接开始实现；Implementation Plan 必须先冻结 Scene 对象清单、配置值、Bounds 审计方法和测试断言。
