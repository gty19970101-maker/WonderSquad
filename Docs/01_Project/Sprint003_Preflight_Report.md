# Sprint003 — Sleeping Forest Vertical Slice Preflight Report

## 1. 结论

- 审查日期：2026-08-08
- 正式 Unity 基线：`6000.3.21f1`
- 当前稳定里程碑：`v0.7-interaction-foundation`
- 验证基线：EditMode `74/74 Passed`；PlayMode `47/47 Passed`；Console Error `0`
- Sprint 定位：《沉睡森林》的第一个正式 Gameplay Vertical Slice
- 实施前状态：**GO**

Sprint003 可以开始正式的灰盒内容实施规划。现有 Character Foundation 与 Interaction Foundation 足以支持森林信标试炼，**不需要修改** `InteractionDetector`、`InteractionPromptPresenter`、`InteractionExecutor`、`PlayerInputReader`、`PlayerMovement` 或 Camera。

本 Sprint 的工作对象是《沉睡森林》中的第一段可玩内容，而不是 PlayerSandbox、InteractionSandbox 或技术 Demo。PlayerSandbox 继续只承担既有系统验证职责。

# GO

## 2. 当前稳定基线与仓库审计

### 2.1 已验证的可复用能力

| 基础 | 可复用结论 |
| --- | --- |
| Character Foundation | `PlayerSpawner` 可在包含有效 Player Prefab 与 `PlayerSpawnPoint` 的本地场景自动生成一个玩家；CharacterController 移动、重力、接地与 Cinemachine 受控斜俯视 Camera 已通过回归。 |
| Interaction Foundation | Player Prefab 已带 Detector、Prompt 所需输入/身份边界、Local Request Port 与 Executor；有效目标可以经过 Detection → Prompt → Execution 触发 `IExecutableInteraction`。 |
| Prompt | 场景中的 `InteractionPromptPresenter` 通过显式 `PlayerSpawner`、View 与 Input Actions 引用绑定本地玩家；目标失效时能清除展示。 |
| 运行时启动 | `BootstrapEntry` 在 `BeforeSceneLoad` 运行；正式关卡直接作为首场景启动时，Bootstrap 仍会初始化。 |
| 渲染 | URP 已在 Graphics/Quality 使用现有 URP Asset；固定方向光、简单 Mesh 和颜色变化足够支持灰盒验证。 |

### 2.2 当前场景与构建配置

当前 `Client/Assets/WonderSquad/Scenes/` 只包含：

- `Bootstrap/Bootstrap.unity`
- `Tests/PlayerSandbox.unity`
- `Tests/GameplaySandbox.unity`
- `Tests/RecoverySandbox.unity`

`EditorBuildSettings.asset` 也只列出以上 Bootstrap/Test 场景；目前没有正式《沉睡森林》场景，也没有关卡加载器。这不是 Sprint003A 的阻塞项，但意味着在创建正式关卡时必须完成以下场景级配置：

1. 新建正式关卡场景并在 Editor 中直接 Play 验证。
2. 将正式场景加入专用 Vertical Slice Build Profile，或将其作为该 Profile 的首场景；不能保留“先进入 Bootstrap 场景、但没有加载正式关卡”的构建流程。
3. 不删除或改作 PlayerSandbox/Test 场景；它们仍用于回归。

## 3. 《沉睡森林》Vertical Slice 目标

采用 Discovery 已确定的**森林信标试炼**，不重新设计测试关卡。

### 3.1 3～5 分钟的核心流程

```text
进入《沉睡森林》灰盒区域
→ 远望终点树台、被阻断路线与两处森林信标
→ 通过环境图形、形状和空间关系理解信标顺序
→ 选择“先观察的稳妥公共路线”或“直接尝试的较快但更容易失误路线”
→ 与两处信标完成有限、可读的交互
→ 环境出现可通行的根桥与连续路标光
→ 若顺序错误，进入安全叶垫支线并短距离返回
→ 通过开启的路线抵达终点树台
→ 获得明确的本地完成反馈
```

这条流程不能退化为“走过去 → 按 E → 完成”：至少需要一个观察点、一个顺序/路线选择、两次状态关联的交互、一个可见的世界变化和一个可恢复错误后果。

### 3.2 最小体验验收命题

- 玩家知道终点在何处，且理解“信标会改变路线”。
- 玩家能从环境而非长文本、语音或音效读到正确顺序的线索。
- 玩家会在稳妥观察与更快直接尝试之间作出简单选择。
- 玩家能把“我做的交互”与“根桥/路标出现”建立因果关系。
- 玩家错误后继续探索与修正，而不是重启场景或陷入软锁。
- 玩家进入终点时明确知道本段已完成。

## 4. 正式场景策略

### 4.1 推荐场景位置与命名

创建建议（Sprint003A 实施时，而非本 Preflight）：

```text
Client/Assets/WonderSquad/Scenes/Levels/SleepingForest/SleepingForest.unity
```

该命名将正式内容与 `Scenes/Tests/` 明确分离，且与“一关一张独立小地图”的设计一致。它是《沉睡森林》的正式灰盒起点，不是复制 PlayerSandbox 后改名的测试场景。

### 4.2 正式场景最小组成

| 场景区域 | 最小内容 | 复用/约束 |
| --- | --- | --- |
| 启动与生成 | 一个 `PlayerSpawner`、一个 `PlayerSpawnPoint`、现有 `Player.prefab` | 使用显式 Prefab/SpawnPoint 引用；仍是单机本地生成器，不是 Network Spawn。 |
| 相机 | 一个 `PF_PlayerCameraRig` 实例 | 显式绑定同一 PlayerSpawner；只保留一个 Main Camera。 |
| Prompt | 一个现有 `PF_InteractionPrompt` 与 `InteractionPromptPresenter` 组合 | Presenter 显式引用同一 PlayerSpawner 和已批准 Input Actions。 |
| 世界灰盒 | 地面、边界、观察点、长路线、短尝试路线、两处信标、根桥、终点树台、叶垫支线 | 使用 Cube、Plane、简单 Collider、现有 Layer 与简单材质。 |
| 光照 | 一个方向光与少量静态点/区域光（如需） | 使用已配置 URP；不引入后处理、复杂 Shader 或新渲染配置。 |

`BootstrapEntry` 无需额外场景对象即可初始化。所有 Player、Camera 与 Prompt 的引用必须在 Inspector 明确配置，不使用按名称查找、`FindObjectOfType` 或静态全局 Player。

### 4.3 Build Profile 风险

当前构建从 `Bootstrap.unity` 开始，且没有关卡加载逻辑。正式场景加入后，必须在 Sprint003D 前选择并验证一个最小构建入口：

- 为 Vertical Slice 使用首场景为 `SleepingForest.unity` 的专用 Build Profile；或
- 以经过单独审查的最小场景选择/加载流程进入该关卡。

前者成本更低，符合当前“只验证玩法”的目标。此项不是开始 003A 的阻塞项，但它是宣布“可独立构建游玩”之前的必做配置。

## 5. Forest Beacon 职责与组件建议

### 5.1 组件组合，而非通用基类

森林信标应由内容专用组件组成。名称以职责为准，建议如下：

| 组件 | 职责 | 不负责 |
| --- | --- | --- |
| `InteractionTarget`（现有） | 稳定 TargetId、检测锚点、可检测性与优先级 | 执行、视觉、试炼状态。 |
| `InteractionPromptSource`（现有） | 提供稳定 Prompt Definition 与本地显示语义 | 选择目标、执行、改变状态。 |
| `ForestBeaconInteraction`（建议） | 实现 `IExecutableInteraction`；把一次已验证请求提交给同场景的森林信标试炼状态 | 目标检测、UI、输入读取、网络。 |
| `ForestBeaconVisual`（建议） | 表现该信标的 Dormant / Activated / Incorrect 视觉状态 | 决定顺序、推进完成、移动玩家。 |
| `ForestSignalTrial`（建议） | 仅编排本试炼的两个信标、正确顺序、路线结果与局部恢复引用 | 成为通用 Puzzle Manager、扫描场景或管理其他关卡。 |
| `ForestRouteReveal`（建议） | 根据试炼的显式状态切换根桥可通行 Collider、Mesh 与路标光对象 | 修改 Player、读取 Input、执行信标交互。 |
| `SleepingForestSliceCompletion`（建议） | 在路线已开启且玩家进入终点区域时记录/展示本地完成 | 完整 Level 生命周期、结算、Save 或关卡切换。 |

`ForestBeaconState` 不建议作为独立的通用 MonoBehaviour。每个信标的局部显示状态由 `ForestBeaconInteraction` 的有限私有状态和 `ForestBeaconVisual` 表现；全局顺序与完成状态只属于 `ForestSignalTrial`。

禁止创建：`GenericPuzzleObject`、`BasePuzzle`、`BaseInteractable`、`GameplayObject`、`UniversalStateMachine`、全局 EventBus、Service Locator 或全局 Manager。

### 5.2 交互与状态流程

```text
InteractionDetector
→ InteractionPromptPresenter / View
→ InteractionExecutor
→ LocalInteractionRequestPort
→ ForestBeaconInteraction (IExecutableInteraction)
→ ForestSignalTrial
→ ForestBeaconVisual + ForestRouteReveal
→ SleepingForestSliceCompletion
```

`ForestBeaconInteraction` 只在 Executor 已完成距离、Layer、遮挡、目标存活和 RequestId 去重后接收请求。它必须返回既有结构化 `InteractionResult`；Prompt、Detector、Visual 和场景 Trigger 都不能绕过执行链直接改变信标或路线状态。

### 5.3 数据边界

最小静态内容可在实施后作为 `ForestSignalTrialDefinition`（名称待 Implementation Plan 确认）放入 `WonderSquad.Content`：稳定试炼 ID、两处信标 ID、合法顺序、公共路径存在标记、失败恢复锚点 ID、终点 ID 和格式版本。

它不是通用 `PuzzleDefinition` 替代品，也不是玩家编辑器格式。运行时的“当前已激活信标、尝试顺序、路线是否开启、完成是否确认”必须和静态定义分离。

## 6. 环境后果方案

### 6.1 方案比较

| 方案 | 可读性 | 实现成本 | 是否真正改变通行/决策 | 结论 |
| --- | --- | --- | --- | --- |
| 仅黑暗区域变亮 | 中 | 低 | 弱；可能只是装饰 | 不单独采用。 |
| 植物/路标亮起 | 高 | 低 | 中；适合指出方向 | 作为辅助反馈。 |
| 封锁路径变为可通过 | 高 | 低至中 | 强；玩家能直接体验世界改变 | 采用。 |
| 仅出现环境 Marker | 中 | 低 | 弱；容易等同 UI 提示 | 不单独采用。 |
| 下一目标被揭示 | 中高 | 低 | 中；适合作为完成后的补充 | 可与路标组合。 |

### 6.2 推荐：根桥显现 + 路标亮起

正确激活两处信标后，**根桥由不可通行变为可通行**，同时一串植物/路标以静态颜色或简单对象显隐形成通往终点树台的可见方向。

实现应只切换预先放置对象的 Renderer、Collider 与可见 Mesh，或使用两套已配置的静态灰盒对象；不使用 Animator、Tween、复杂 Shader Framework、Audio System 或逐帧特效。根桥是本试炼的内容对象，不应抽象为通用 Door。

正确结果不能只以颜色区分：桥体几何、可通行性、路标形状和终点视线必须共同表达状态。

## 7. 最小策略路线

### 7.1 当前版本的选择

第一版不实现完整 Ability System，也不伪造角色能力。最小策略是：

- **稳妥公共路线**：先去观察台读取完整符号顺序，再通过更长但安全的道路访问第二信标；任何本地玩家都能完成。
- **快速尝试路线**：直接前往近处信标并根据不完整线索尝试顺序；节省观察时间，但错误会触发短暂叶垫支线。

这是一种真实的时间/风险/信息取舍，而不是两条外观不同、结果相同的走廊。

### 7.2 未来角色能力接入边界

未来角色能力只改变获得优势的方式，不改变公共通关路径：

| 未来能力方向 | 对信标试炼的优势 | 不可做的事 |
| --- | --- | --- |
| 观察者 AbilityTag | 从远处揭示第二信标或正确顺序，减少观察台往返 | 不能移除公共线索或成为唯一解。 |
| 探险家 AbilityTag | 经预定义高处 Anchor 抵达更短路线 | 不能改为自由攀爬或让公共路线失效。 |
| 魔法师 AbilityTag | 切换预定义的环境状态以临时显示安全踏脚点 | 不能引入连续环境变形。 |
| 工匠 AbilityTag | 使用预定义 Socket 形成低成本稳妥路线 | 不在本 Sprint 引入资源、制作或任意建造。 |

未来接入使用 AbilityTag、预定义目标/Anchor/Socket 和有限环境状态；不需要改写 Interaction Foundation。Sprint003 只保留场地与内容设计上的入口，不在此时增设未使用的 Ability Framework 或数据字段。

## 8. 欢乐恢复方案

### 8.1 错误情境

玩家在未观察完整线索的情况下，以错误顺序激活信标。错误必须有清晰的本地可视反馈：错误信标熄灭/变为错误形态，安全根桥仍未出现，附近叶丛引导玩家进入一个短叶垫下坡支线。

### 8.2 恢复路径

```text
错误顺序
→ 错误信标与叶垫支线出现明显反馈
→ 玩家沿固定安全下坡到叶垫
→ 通过 10～30 秒可见回路回到观察台或首个信标
→ 本试炼仅重置本次顺序，不重载场景、不销毁信标、不清除完成能力
→ 玩家读取线索后重新尝试
```

灰盒阶段优先让坡道与回路自身构成恢复，不向 `PlayerMovement` 写入击飞、传送、冻结或自定义物理。若 CharacterController 对坡道反馈不稳定，改为平坦的短绕行支线；不为了“搞笑失败”修改 Character Foundation。

恢复成本目标为 10～30 秒，首次错误不超过约 30 秒回到可继续位置。任何失败状态都不得永久封闭公共路径、迫使场景重启或造成不可逆软锁。

## 9. 完成条件

完成条件限定为：

1. `ForestSignalTrial` 已确认两处信标以合法顺序激活，且根桥路线已开启；
2. 本地玩家进入终点树台的 Completion Trigger；
3. `SleepingForestSliceCompletion` 以场景内可见文本/形状/静态灯光组合确认“森林信标试炼完成”。

它不加载下一关、不建立完整 Level State Machine、不写 Save、不生成奖励、不进入结算画面。若玩家在根桥未开启时进入终点触发范围，必须不完成，并保留可理解的环境提示。

## 10. Multiplayer 与 Host Authority 边界

当前仅验证本地单玩家，不引入 Network 代码、Photon Fusion 类型或房间逻辑。

未来进入 2～4 人时，下列状态必须迁移为 Host / StateAuthority 决定：

| 状态或请求 | 未来权威规则 |
| --- | --- |
| 信标交互请求 | Host 重新校验 PlayerId、RequestId、TargetId、距离、遮挡、目标状态与顺序。 |
| 信标局部状态与正确顺序 | Host 持有唯一 AuthoritativeState；客户端只展示快照/结果。 |
| 根桥可通行状态与 Revision | Host 改变并同步；迟到请求不能覆盖更新状态。 |
| 错误输出与局部复位 | Host 确认，并以有限状态/恢复结果同步。 |
| 完成 Trigger | Host 校验路线已开启和玩家进入；完成只产生一次。 |

对 2 人，信标的分工应允许一人观察/标记、一人操作；对 3～4 人只增加并行观察或容错，不增加必需参与人数。5～10 人不属于 MVP；若未来探索该规模，仍不能把本机关的必需站位人数扩大到 5～10，且需要独立的网络与关卡密度审查。

## 11. 组件、程序集与资产建议

### 11.1 程序集建议

| 内容 | 推荐程序集 | 理由 |
| --- | --- | --- |
| `ForestBeaconInteraction`、`ForestSignalTrial`、`ForestRouteReveal`、`SleepingForestSliceCompletion` | `WonderSquad.Puzzle` | 它们是一个有限、可重置、具有完成条件的环境挑战；该程序集现有依赖只有 Core/Content。 |
| `ForestBeaconVisual` | `WonderSquad.Puzzle` | 仅为同一内容对象的表现适配，不引用 UI、Player 或 Network。 |
| 试炼静态 Definition/验证 | `WonderSquad.Content` | 静态定义、稳定 ID 与内容合法性归属于 Content；Puzzle 只读消费。 |
| Prompt / Detector / Executor / Player / Camera | 现有程序集，不修改 | 保持已通过的单向依赖。 |

`WonderSquad.Puzzle` 不需要编译时引用 `WonderSquad.Interaction`：信标执行组件可通过一个显式 Inspector 引用的 `MonoBehaviour` 缓存 `IInteractable` 契约，或由内容定义持有稳定 TargetId；不得让 Puzzle 反向引用 Detector、Prompt 或 Executor 实现。Interaction 也绝不引用具体森林信标类型。

### 11.2 实施后可新增的资产（本 Preflight 不创建）

```text
Runtime/Puzzle/SleepingForest/
  ForestBeaconInteraction.cs
  ForestBeaconVisual.cs
  ForestSignalTrial.cs
  ForestRouteReveal.cs
  SleepingForestSliceCompletion.cs

ScriptableObjects/Content/SleepingForest/
  ForestSignalTrialDefinition.asset

Prefabs/SleepingForest/
  PF_ForestBeacon.prefab
  PF_ForestRouteRootBridge.prefab        (仅当实际复用超过一个场景对象时)

Scenes/Levels/SleepingForest/
  SleepingForest.unity
```

`PF_ForestRouteRootBridge` 不是预先要求的抽象。若场景中只使用一次根桥，应先保留为场景对象，避免为了潜在复用增加 Prefab/组件层。

### 11.3 资产策略

允许且优先使用：Plane、Cube、Sphere、简单树根/石柱占位、简单材质、静态颜色、简单灯光、Collider 和静态 Mesh 启停。

不导入商店资产，不制作大量森林美术，不加入复杂 Shader、Animator、粒子、Tween、音效或后处理。正式场景身份来自清晰的空间、目标和交互因果，而非美术完成度。

## 12. 自动化测试策略

### 12.1 EditMode

新增测试应覆盖内容逻辑而非 Foundation 重测：

- 合法信标顺序使试炼进入 RouteOpen 状态。
- 错误顺序只产生本次局部恢复状态，不破坏公共路径或永久完成。
- 重复 RequestId/已完成状态不会重复推进或重复完成。
- 路线开启与关闭的可见/Collider 配置一致。
- 终点在 RouteOpen 前不完成；RouteOpen 后进入 Trigger 才完成。
- 每个信标具有有效稳定 ID、Prompt Source、Trigger Collider、Interactable Layer 和执行组件。
- 试炼声明公共路径与可恢复失败出口。
- Puzzle/Content 的程序集方向不依赖 Player、UI、Interaction 实现或 Network。

### 12.2 PlayMode

在正式 `SleepingForest.unity` 或专用临时加载副本中验证：

```text
Player Spawn
→ Movement / Camera
→ Detection
→ Prompt
→ Execution
→ ForestBeaconInteraction
→ ForestSignalTrial
→ ForestRouteReveal
→ Completion Trigger
```

最少覆盖：

- 玩家能生成，Camera 和 Prompt 正常绑定。
- 近处信标被检测、显示正确 Prompt、执行一次只推进一次。
- 两次正确交互开启根桥和路标，Player 可物理通过。
- 错误顺序不完成，触发可走出的安全支线/局部复位。
- 清除错误后正确顺序仍能完成，不需重载场景。
- 终点未开启时拒绝完成，开启后只完成一次。
- PlayerMovement、Camera、Interaction 检测/提示/执行没有回归；Console Error = `0`。
- 完整回归不低于当前基线：EditMode `74/74`、PlayMode `47/47`；新增测试应使总数增加而非替换既有覆盖。

### 12.3 人工验收（必须在正式场景）

1. 在 `SleepingForest.unity` 直接 Play，不使用 PlayerSandbox。
2. 确认玩家、Main Camera 与 Prompt 各只有一套有效实例。
3. 在不读开发说明的情况下，观察到终点、两处信标与环境线索。
4. 走公共观察路线，完成正确交互顺序，确认根桥和路标出现。
5. 走快速尝试路线并故意做错一次，确认可理解的失败、10～30 秒回路和无场景重启。
6. 回到主线后再次完成，进入终点树台并确认完成反馈。
7. 验证错误和正确结果不只依赖颜色；WASD、Camera、Prompt 与交互按键仍正常。
8. Console Error = `0`；Profiler 中稳定状态无新逻辑持续 GC.Alloc。

## 13. Sprint003 子 Sprint 拆分

### Sprint003A — Sleeping Forest Greybox

范围：创建正式 `SleepingForest.unity` 灰盒空间、局部 Player/Camera/Prompt 场景配置、出生点、观察台、公共路线、快速尝试路线、叶垫回路和终点区域。

验收：PlayerSpawner、Movement、Camera 和 Prompt 在正式场景正常；无森林信标玩法逻辑也能从出生走到各区域；PlayerSandbox 不被修改。

### Sprint003B — Forest Beacon Gameplay

范围：通过组件组合实现两处 `ForestBeaconInteraction`、局部有限顺序状态、Prompt Definition、信标视觉与结构化结果。

验收：既有 Detection → Prompt → Execution 可驱动信标；正确/错误交互不修改 Interaction Foundation；无通用基类或通用 Puzzle Framework。

### Sprint003C — Environment Consequence and Route Choice

范围：根桥/路标环境结果、公共观察路线与快速尝试路线、错误顺序的叶垫回路和局部恢复。

验收：正确结果实际改变可通行性；错误不软锁、不重载；两条选择在时间/风险/信息上有可感知差异。

### Sprint003D — Playable Slice Integration and Gate

范围：终点完成、正式场景专项测试、人工盲测、Build Profile 首场景配置与全量回归。

验收：3～5 分钟闭环可重复完成；完成状态明确；完整测试基线无回归；决定是否为后续双人无麦切片做新的 Network/Communication Preflight。

此拆分避免同一 Sprint 同时修改场景、内容状态、路线表现、完成逻辑、多人网络和角色能力系统。

## 14. 禁止范围

- 修改 InteractionDetector、InteractionPromptPresenter、InteractionExecutor、PlayerInputReader、PlayerMovement、Camera 或已通过的核心契约。
- 网络、Fusion、房间、同步、Host Authority 运行时代码、多人代理或假队友。
- Inventory、Resource、Item、Crafting、携带、放置、制作、Save、后端。
- Door、Lever、Pressure Plate 的通用系统；通用 Puzzle Framework、通用状态机、EventBus、Service Locator、全局 Manager。
- 四角色完整 Ability System、自由攀爬、任意建造、复杂物理、战斗、AI。
- 正式《沉睡森林》之外的开放世界、关卡变体、玩家编辑器、语音 SDK。
- 大量美术、商店资产、复杂 Shader、动画、音效和粒子。

## 15. 风险与阻塞项

| 项目 | 级别 | 处理方式 | 是否阻塞 003A |
| --- | --- | --- | --- |
| 正式场景尚不存在 | 中 | 003A 创建独立 Levels/SleepingForest 场景，不复制测试夹具。 | 否 |
| Build Settings 没有正式关卡入口 | 中 | 003D 前配置专用 Vertical Slice Build Profile 或确定最小加载入口。 | 否；阻塞独立 Build 声明 |
| 当前无 Ability Runtime | 中 | Sprint003 用路线/信息策略验证；只预留 AbilityTag 内容入口，不实现完整能力。 | 否 |
| 欢乐失败的坡道/回路可读性不足 | 中 | 使用平坦可见回路作保底；盲测恢复时间。 | 否 |
| 内容脚本错误依赖 Interaction 实现 | 高 | Puzzle 只依赖 Core/Content，并经 Core 契约/显式引用协作；在 003B 做 asmdef 审计。 | 否，前提是遵守设计 |
| 把单机结果误称为合作验证 | 高 | 003D Gate 明确只报告本地环境闭环；双人无麦另开 Preflight。 | 否 |
| `PlayerSpawner` 的语义仍是本地生成器 | 低 | 只在本地正式场景使用；未来网络替换 Spawn Authority，不改变其用途。 | 否 |

## 16. Definition of Ready

开始 Sprint003A 前必须确认：

- [x] 使用既定“森林信标试炼”，不另起测试关卡。
- [x] 目标是 3～5 分钟本地环境闭环，不宣称已验证多人合作。
- [x] 正式场景位置、灰盒边界、公共路线、快速尝试路线、叶垫回路和终点区域已在本报告锁定。
- [x] 环境后果锁定为“根桥实际可通行 + 路标可见”，不采用只有颜色或 UI 的伪后果。
- [x] 错误恢复锁定为局部、可见、10～30 秒回路，不重启场景。
- [x] 组件组合、程序集方向与禁止抽象已锁定；Interaction/Player/Camera Foundation 不改。
- [x] 角色能力与多人仅保留明确扩展边界，不在 Sprint003 实现。
- [x] 自动化、人工验收与回归门槛已定义。

唯一后续配置注意项是正式场景创建后，在 003D 前为独立 Build 选择有效的首场景入口；它不阻塞 003A 灰盒实施。

## 17. 最终状态

# GO

可以开始 **Sprint003A — Sleeping Forest Greybox** 的实施前计划或实现准备。不得跳过 003A 直接实现通用机关、网络、库存或完整 Ability System。
