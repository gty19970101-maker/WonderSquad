# Sprint003D Discovery Report — Sleeping Forest Slice Completion

## 1. 文档状态

- 阶段：Sprint003D Discovery
- 目标内容：《沉睡森林》第一可玩 Vertical Slice 的终点与完成闭环
- Unity 正式基线：`6000.3.21f1`
- Discovery 结论：`READY FOR SPRINT003D PREFLIGHT`
- 本轮变更边界：只新增本报告；未修改代码、Scene、Prefab、ScriptableObject、Input Actions、Package 或 ProjectSettings

---

## 2. 当前稳定基线

| 基线 | 状态 |
|---|---|
| Sprint001 Character Foundation | `PASSED / GO` |
| Sprint002 Interaction Foundation | `PASSED / GO` |
| Sprint003A Sleeping Forest Greybox | `PASSED / GO` |
| Sprint003B Forest Signal Trial | `PASSED / GO` |
| Sprint003C Root Bridge Consequence | `PASSED / GO` |

Sprint003C 最终 Unity 验证基线：

- Sprint003C EditMode：`8 Passed / 0 Failed / 0 Skipped`
- Sprint003C PlayMode：`4 Passed / 0 Failed / 0 Skipped`
- 完整 EditMode：`108 Passed / 0 Failed / 0 Skipped`
- 完整 PlayMode：`58 Passed / 0 Failed / 0 Skipped`
- Console Error：`0`

Sprint003D 必须在不改变上述 Foundation 与已通过玩法行为的前提下，把现有“可游玩的试炼片段”闭合为完整 Vertical Slice。

---

## 3. 玩家体验目标

Sprint003D 完成后的目标体验为：

```text
进入 Sleeping Forest
        ↓
观察并探索环境
        ↓
发现并按 A → B 完成 Forest Signal Trial
        ↓
观察 Root Bridge Advantage 的环境变化
        ↓
通过 Main Route 或 Advantage Route 抵达 Slice End
        ↓
获得清晰、一次性的完成反馈
```

本 Sprint 验证的不是新机关，而是玩家是否能明确理解以下三件事：

1. 自己完成了试炼；
2. 试炼改变了场景；
3. 自己已经完成这一段《沉睡森林》内容。

完成反馈不得替代环境玩法，也不得把流程退化成单纯“走进 Trigger 即完成”。玩家在终点前仍需完成 Sprint003B 已建立的核心试炼。

---

## 4. Completion 条件方案比较

| 方案 | 条件 | 优点 | 缺点与风险 | 结论 |
|---|---|---|---|---|
| A：仅抵达终点 | 玩家进入 Slice End 即完成 | 成本最低；不会因前置状态造成阻塞 | 可完全绕过 Beacon Trial 与环境后果，无法验证完整 Vertical Slice；003B/003C 退化为可选装饰 | 不采用 |
| B：完成 Trial 后抵达终点 | `ForestSignalTrial Completed` 且玩家位于 Slice End | 闭合“试炼 → 世界变化 → 终点”主循环；条件单一、可解释、易测试 | 玩家提前到达终点时必须得到明确反馈并可安全返回 | **推荐** |
| C：多套逻辑条件均可完成 | 不同路线使用不同完成条件 | 可表达复杂替代解法 | 当前没有角色能力、资源或第二套等价挑战；会提前形成任务/解法框架，且难以判断不同路线是否等价 | 当前不采用 |

### 4.1 推荐规则

Sprint003D 采用方案 B：

```text
CompletionEligible = PlayerInsideSliceEnd
                     AND ForestSignalTrialSnapshot.IsCompleted
```

多路线仍然成立，但体现在**空间选择**而非多套完成规则：

- Main Route 始终是安全、合法的通用路线；
- Advantage Route 是完成 Trial 后由 Sprint003C 提供的更短路线；
- 两条路线汇入同一个 Slice End，并使用相同的完成条件；
- 不要求玩家实际走过 Advantage Route，也不要求特定角色或能力。

### 4.2 提前到达终点

玩家未完成 Trial 而进入 Slice End 时：

- Completion 保持 `NotCompleted`；
- 显示简短、非阻塞的“森林信号尚未完成”反馈；
- 不关闭路线、不冻结输入、不传送玩家、不重置场景；
- 玩家可沿永久开放的 Main Route 返回 Beacon 区域；
- 完成 A → B 后再次抵达终点即可正常完成。

这是一道状态条件，不是物理门、谜题机关或永久失败。

---

## 5. Slice End 设计方案

### 5.1 推荐组成

Slice End 使用三个职责分离的场景对象：

1. **End Marker**：提供可见的终点地标，不持有完成状态；
2. **Completion Volume**：只报告本地玩家进入、停留与离开，不决定完成；
3. **Slice Completion**：组合玩家是否在终点与 Trial Snapshot，唯一提交完成状态。

建议复用 Sprint003A 已存在的 `SliceEndGround` 与终点 Landmark，在其安全中心区域增加 Trigger，而不是移动或重建现有路线。

### 5.2 Trigger 规则

- 使用 `BoxCollider` 且 `isTrigger = true`；
- Trigger 位于 `SliceEndGround` 的安全承载面内，不越过平台边缘，也不向下延伸到 FallRecovery 阈值附近；
- Trigger 不作为地面 Collider，不阻挡 CharacterController；
- End Marker 的表现对象不增加承载 Collider；
- Main Route 与 Advantage Route 都能进入同一个 Volume；
- 只接受明确识别的当前本地玩家，忽略 Beacon、Marker、环境 Trigger 和其他 Collider；
- 不使用 `FindObjectOfType`、字符串对象名或静态全局 Player 引用；
- 本地玩家识别的具体既有标记/组件边界必须在 Preflight 中根据当前 Player Prefab 锁定。

### 5.3 进入、停留和离开

Completion Volume 只维护场景实例内的“玩家当前是否在区域内”。它不得直接显示 UI 或写入 Trial/Bridge 状态。

Slice Completion 在以下时机重新评估条件，不在 `Update` 中轮询：

- 玩家进入 Volume；
- 玩家离开 Volume；
- Volume 已有玩家停留时收到 Trial Snapshot 变化；
- 组件启用时读取一次当前 Trial Snapshot 与当前区域状态。

因此，即使未来多人环境中玩家位于终点时由队友完成 Trial，也不需要退出并重新进入 Trigger；当前 Sprint 仍只实现本地单玩家。

---

## 6. Completion State 方案

### 6.1 状态拥有者

状态应由 Sleeping Forest Scene 中唯一的、场景局部的 Slice Completion 组件拥有。建议职责名称为 `SleepingForestSliceCompletion` 或等价的内容专用名称。

不采用：

- `ForestSignalTrial` 持有关卡完成状态；
- `RootBridgeAdvantageSpan` 持有关卡完成状态；
- 全局 `GameManager`；
- Quest、Objective、Save 或通用 Level State Framework；
- ScriptableObject 持有运行时完成状态。

该组件是《沉睡森林》内容编排，不把一次 Vertical Slice 的需求抽象成整个项目的通用任务系统。未来出现第二个真实关卡后，再根据共同需求提取 Level Completion 契约。

### 6.2 最小状态

```text
NotCompleted
      ↓  条件首次满足
Completed
```

确定规则：

- Scene 实例创建时为 `NotCompleted`；
- 只有 `NotCompleted → Completed` 是合法转换；
- 成功转换时 Revision 从 `0` 增至 `1`；
- 重复进入、重复 Trial Snapshot 或组件重新启用不再递增 Revision；
- `Completed` 不回退到 `NotCompleted`；
- Scene 重新加载会创建新的 Scene 实例并重置为 `NotCompleted / Revision 0`；
- Sprint003D 不实现跨 Scene、存档或会话持久化。

### 6.3 只读边界

Completion 对外提供只读 Snapshot 和实例级状态事件，至少表达：

- 当前 Completion State；
- 是否完成；
- Revision；
- 完成时所消费的 Trial Revision（用于诊断与测试，不用于反写 Trial）。

事件只在首次完成时发布一次。禁止 static 事件、全局 Event Bus 和稳定帧轮询。完成反馈只能读取这个边界，不能直接设置状态。

---

## 7. 完成反馈方案

### 7.1 推荐最小反馈

采用“屏幕提示 + End Marker 状态变化”的双通道反馈：

- 屏幕 Canvas 显示明确文本，例如“沉睡森林 · 信标试炼完成”；
- End Marker 从未完成表现切换为完成表现，例如启用一个完成标记或改变实例级颜色；
- UI 不拦截 Raycast，不暂停 PlayerInput，不冻结 Movement；
- 玩家完成后仍可在场景内移动与观察结果；
- 默认隐藏；仅在状态变化或未满足条件进入终点时更新，不在稳定帧重复写 Text；
- 文本保留稳定 key 与 fallback 文案边界，但不实现完整本地化系统。

世界表现优先使用已有 Renderer 状态、GameObject 显隐或 `MaterialPropertyBlock`，不得使用 `renderer.material` 修改实例材质，也不得污染 shared Material。

### 7.2 未满足反馈

未完成 Trial 进入终点时显示非模态提示，例如“森林信号尚未完成”，离开区域后隐藏。该提示：

- 不计为失败；
- 不修改 Completion Revision；
- 不重复触发音效或创建 UI；
- 不引入任务追踪、指路系统或对话系统。

### 7.3 当前不需要音效

简单占位音效虽在允许范围内，但不是闭环阻塞项。为避免引入 Audio 生命周期和资源配置，Discovery 建议 Sprint003D 首版只使用屏幕与世界视觉反馈；音效可在后续 Polish Sprint 添加。

---

## 8. 与 Sprint003B / Sprint003C 的接口关系

### 8.1 Sprint003B：唯一逻辑前置条件

Slice Completion 只读消费：

- `ForestSignalTrial.CurrentSnapshot`；
- `ForestSignalTrial` 的实例级 Snapshot 变更事件；
- `ForestSignalTrialSnapshot.IsCompleted` 与 Revision。

它不得修改 Trial 状态、Beacon 状态、顺序规则或 Revision。`ForestSignalTrial Completed` 继续表示“信标试炼完成”，只有 Slice Completion 再结合玩家位于终点，才表示 Vertical Slice 完成。

### 8.2 Sprint003C：环境后果，不是第二个完成门槛

`RootBridgeAdvantageSpan Activated` 是 Trial Completed 的既有环境后果，不应成为第二个玩家完成条件：

- Main Route 永久合法，玩家不需要使用 Advantage Route；
- Span 是 Trial 的派生表现/路线状态，不是新的权威目标；
- 把 Span 激活作为硬门槛会让 003C 配置故障变成玩家软锁；
- 003D 不读取 Span 私有字段，不调用其激活方法，也不修改 003C Revision。

003D 测试与人工验收必须把“Trial 完成时 003C 后果已正常出现”作为完整链路回归项，但 Completion 提交的逻辑来源仍是 Trial Snapshot。若 Bridge 后果未出现，应由配置验证或测试暴露为 003C/场景集成错误，而不是让玩家永久无法完成。

### 8.3 数据流

```text
ForestSignalTrial Snapshot/Event ───────┐
                                       ├─> SleepingForestSliceCompletion
Local Player in SliceEndVolume ────────┘              │
                                                      │ read-only snapshot/event
                                                      v
                                      Completion Presenter → Completion View

ForestSignalTrial Snapshot/Event → Sprint003C Consequence → AdvantageSpan
                                      （独立派生链，不反向控制 Completion）
```

### 8.4 程序集风险留给 Preflight 关闭

当前 `ForestSignalTrial` 位于 Puzzle 领域，而架构文档要求通用 Level 模块通过公开事件/契约协作。Preflight 必须在不形成反向依赖的前提下锁定以下边界：

- Completion 编排不得放入 Player、Interaction 或 Camera 程序集；
- 通用 `WonderSquad.Level` 不应为了一个内容对象随意引用 Puzzle 实现；
- 优先评估一个 Sleeping Forest 内容编排边界，单向读取 Puzzle 的既有公开 Snapshot/Event；
- Presentation 可以单向依赖该只读 Completion 边界，Completion 不依赖具体 View；
- 不为了这一个 Slice 创建通用任务、目标或事件框架。

这是 Sprint003D Preflight 的程序集落位决策，不构成重新设计核心玩法的理由。

---

## 9. Soft-lock 分析

| 情况 | 预期行为 | 防软锁规则 |
|---|---|---|
| 1. 未完成 Trial 直接到达终点 | 保持 NotCompleted，显示“森林信号尚未完成” | Main Route 不封锁；玩家可原路返回完成 A → B，再次进入终点 |
| 2. 已完成 Trial 后到达终点 | 首次进入即转换为 Completed 并显示完成反馈 | 只读消费 Trial Snapshot，不要求使用指定路线 |
| 3. 在 Completion Trigger 附近掉落 | 离开 Trigger，不提交错误完成；FallRecovery 按 003A 规则恢复 | Trigger Bounds 保持在安全平台上方且远离 fallYThreshold；恢复后仍可重试 |
| 4. 重复进入 Completion 区域 | 保持 Completed，既有对象引用与 Revision 不变 | 状态转换、事件和反馈均幂等；不重复创建 Canvas/Marker |
| 5. Scene 重新加载 | 新 Scene 实例回到 NotCompleted，Trial 与反馈也按场景初始化 | 003D 不保存完成状态，不使用 static 状态 |
| 6. 玩家先进入终点、停留期间 Trial 变为 Completed | 条件重新评估并完成 | 订阅 Trial 实例事件，不要求 Trigger 重新进入 |
| 7. Completion/反馈组件禁用后重启 | 状态拥有者不因普通 Disable 回退；View 从当前 Snapshot 恢复显示 | 初始化与订阅分离；取消订阅不清除已提交状态 |

Completion 不执行传送、重载、输入禁用或物理状态修改，因此不会破坏 Player、Camera、GroundDetector 或 FallRecovery。

---

## 10. 测试规划

### 10.1 EditMode

至少验证：

- Completion 初始为 `NotCompleted / Revision 0`；
- 条件矩阵：不在终点、Trial 未完成、只满足一项时均不完成；
- 玩家在终点且 Trial Completed 时只转换一次；
- 重复评估、重复 Snapshot 与重复进入不增加 Revision、不重复发布事件；
- 未完成 Trial 的提前到达产生 NotReady 反馈数据，但不修改 Completion State；
- 只读 Snapshot 不允许外部修改运行时状态；
- Scene 中只有一个 Completion 状态拥有者和一个有效 Completion Volume；
- Volume 为 Trigger，位于安全 End Ground Bounds 内，不与承载 Collider 形成冲突；
- Completion、Presenter、View 的依赖方向无循环；
- 003A 原有“禁止 Completion Gameplay”测试在实施时应精确调整为只允许 Sprint003D 的稳定对象，并继续禁止 Test/Sandbox/重复 Completion 对象，不能简单删除边界断言。

### 10.2 PlayMode

至少验证：

- SleepingForest 正常加载，单 Player 与单 Main Camera 保持；
- 未完成 Trial 进入终点不会完成，退出后可返回关卡；
- 通过真实 A → B 流程完成 Trial 后，进入 End Volume 完成 Slice；
- Sprint003C AdvantageSpan 已按既有链路激活，但 Main Route 仍可完成；
- Main Route 与 Advantage Route 均可汇入同一终点条件；
- 重复离开/进入 End Volume 不重复完成；
- 玩家在 End Volume 时收到 Completed Snapshot 可正确完成；
- Completion Trigger 附近掉落后 FallRecovery 正常，恢复后仍可完成；
- Completion 不改变 Player Transform、Movement、GroundDetector、Camera 或 Interaction 行为；
- Scene reload 后 Completion 重置且不存在 static 状态污染；
- Console Error = 0。

自动测试验证状态、连接和幂等，不尝试替代真实走图来证明路线“好玩”或反馈节奏合适。

### 10.3 完整回归

实施验收必须在当前 `108 EditMode / 58 PlayMode / Console Error 0` 基线上运行完整回归。测试数量以 Unity Test Runner 实际结果为准，不在 Discovery 预设最终数量。

---

## 11. 人工验收方向

在正式 `SleepingForest` Scene 中至少执行：

1. 新进入场景，确认 Completion UI 默认隐藏、End Marker 为未完成状态；
2. 不碰 Beacon，沿 Main Route 直接到终点，确认不完成且提示原因清晰；
3. 从终点安全返回，按 A → B 完成 Trial；
4. 确认 Root Bridge Advantage 环境变化仍由 003C 正常触发；
5. 分别通过 Main Route 与 Advantage Route 抵达终点，确认两者使用同一完成规则；
6. 确认首次满足条件时出现明确完成反馈；
7. 离开并重复进入，确认不重复提交、不重复创建 UI、不刷屏；
8. 在 End Volume 边缘主动掉落，确认 FallRecovery 后仍可继续；
9. 确认完成反馈不阻挡 WASD、Interaction 或 Camera；
10. Reload Scene，确认本局 Completion、Trial 和反馈按场景重新初始化；
11. 检查单 Player、单 Main Camera、Console Error = 0；
12. 检查稳定帧没有 Completion 系统持续 GC.Alloc。

---

## 12. 风险分析

| 风险 | 级别 | 处理方向 |
|---|---|---|
| Trigger 被环境 Collider 或非本地 Player 误触发 | 高 | Preflight 锁定本地玩家识别规则；显式 Layer/组件过滤，禁止名称查找 |
| Completion 直接依赖 Puzzle 实现导致架构边界倒置 | 高 | Preflight 锁定内容编排程序集与单向 Snapshot/Event 边界，不让 Puzzle 依赖 Level/UI |
| 把 AdvantageSpan 作为硬条件造成配置故障软锁 | 高 | Trial Snapshot 是唯一逻辑前置；003C 后果通过验证暴露错误，不作为第二门槛 |
| 玩家提前到终点但不知道缺少什么 | 中 | 提供非阻塞、明确的未满足反馈，并保持返回路线畅通 |
| Trigger 靠近平台边缘导致下落中误完成或频繁 Enter/Exit | 中 | Volume 完全放在安全平台 Bounds 内；以一次性状态保护重复回调 |
| UI 引用方向造成 Level/UI 循环依赖 | 中 | View 单向读取 Completion Snapshot；运行时状态不得引用具体 View |
| Scene reload 后残留 static 状态 | 中 | 所有状态与事件为 Scene 实例级；Reload 测试覆盖 |
| 为未来关卡过早抽象通用 Objective/Quest Framework | 中 | 使用 Sleeping Forest 专用组合组件；第二个真实关卡出现前不提取框架 |
| 003A 测试仍禁止所有 Completion 对象 | 中 | 实施时精确更新允许清单，并保留单实例、非 Sandbox、Foundation 不变等保护断言 |
| 完成 UI 变成模态菜单并影响移动测试 | 低 | 本 Sprint 只做非模态只读反馈，不暂停、不接管输入 |

---

## 13. Sprint003D 明确禁止范围

Sprint003D Discovery 不建议引入，后续实施也不应擅自扩展为：

- 新交互类型、新 Beacon、新机关或新 Puzzle；
- Quest、Objective、Reward、Achievement 或通用 Level Framework；
- Inventory、Crafting、Ability、角色差异；
- Network、Host 实现、Save 或跨 Scene Persistence；
- 对话系统、完整本地化系统、完整 UI/Audio/Animation Framework；
- 自动重启、场景选择、结算奖励或 Build Profile 改造；
- Character、Interaction、Camera Foundation 修改；
- ForestSignalTrial 状态机或 RootBridgeAdvantageSpan 行为修改；
- Main Route 封锁，或强制玩家必须使用 Advantage Route。

未来接入网络时，Completion 条件判断、Revision 与完成提交必须由 Host/Authority 确认；当前 Sprint 只保留实例级请求/结果边界，不实现网络代码。

---

## 14. Sprint003D Preflight 必须关闭的事项

进入 Implementation Plan 前，Preflight 必须基于仓库实物确认：

1. `SliceEndGround` 与 End Landmark 的实际 Transform、Renderer Bounds 和 Collider Bounds；
2. Completion Volume 的安全位置、尺寸及 Player 识别方式；
3. Sleeping Forest 内容编排、Level 与 UI 的 asmdef 落位及单向依赖；
4. Trial 当前 Snapshot/Event 的订阅生命周期与 Scene 初始化顺序；
5. 完成反馈使用的现有 uGUI 组件、Canvas 归属和默认隐藏方式；
6. 003A Scene 结构测试中需要精确替换的过时“无 Completion Gameplay”断言；
7. Scene Authoring 是否需要显式、确认式、幂等的 Sprint003D 菜单入口；不得自动覆盖场景；
8. 早到终点提示的 fallback 文案与显示/隐藏生命周期；
9. 完整测试基线继续以 `108 EditMode / 58 PlayMode / Console Error 0` 为起点。

这些是 Preflight 的具体架构与配置决策，不要求重新设计 Sprint003D 的核心 Completion 规则。

---

## 15. 完成 Sprint003D 后的 Vertical Slice 状态

Sprint003D 通过后，《沉睡森林》将首次具备完整、可理解、可重复验证的本地 Vertical Slice：

```text
进入 → 探索 → A/B 信标试炼 → 环境变化
     → Main/Advantage 路线选择 → 终点 → 完成反馈
```

这个闭环验证观察、环境交互、简单策略、可恢复错误与明确目标完成，同时没有提前引入多人网络、角色能力、资源系统或通用任务框架。

---

# READY FOR SPRINT003D PREFLIGHT

推荐方向已经明确：Sprint003D 采用“Forest Signal Trial Completed + 玩家进入统一 Slice End”的最小完成条件；多路线保留为空间选择，完成状态由场景局部内容编排组件单次提交，并通过非模态 Canvas 与 End Marker 提供明确反馈。下一步只需在 Preflight 中锁定程序集、Trigger Bounds、玩家过滤和 Scene Authoring 细节。
