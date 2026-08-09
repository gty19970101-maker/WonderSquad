# Sprint003C — Forest Signal Environment Consequence Implementation Plan

## 1. 文档状态

- 计划状态：`READY FOR IMPLEMENTATION`
- Unity 基线：`6000.3.21f1`
- 前置 Sprint：Sprint003A `PASSED / GO`；Sprint003B `PASSED / GO`
- 已验证基线：Sprint003B 专项 EditMode `18/18`、PlayMode `4/4`；完整 EditMode `100/100`、PlayMode `54/54`；Console Error `0`
- 正式场景：`Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity`
- 本文只锁定 Sprint003C 的实施方案。本轮不修改代码、Scene、Prefab、ScriptableObject、Input Actions、Packages 或 ProjectSettings。

## 2. Sprint 目标与完成边界

让已完成的 `ForestSignalTrial`（正确顺序 `Beacon A → Beacon B`）产生一个可见、可走、非必要且无软锁风险的正式关卡环境后果：激活 `RootBridgeAdvantageSpan`。

```text
Beacon A → Beacon B
        ↓
ForestSignalTrial 发布只读 Completed Snapshot（新 Revision）
        ↓
SleepingForestRootBridgeConsequence 消费实例事件
        ↓
RootBridgeAdvantageSpan 由 Initial 切换为 Activated
        ↓
玩家仍可走 Main Route，或选择有明确效率收益的 Advantage Route
```

本 Sprint 的结束条件是“试炼完成后环境后果正确生效，玩家可安全选择路线并继续前往 Slice End 区域”。它**不是**《沉睡森林》切片完成：终点触发、完成状态、完成 UI、Build 入口仍属于 Sprint003D。

## 3. 不可变约束

1. `ForestSignalTrial`、Beacon A/B、`IInteractable`、`IExecutableInteraction`、Detector、Prompt、Executor、Player、Camera、Movement、GroundDetector 与 FallRecovery 的生产实现均不修改。
2. 003C 只消费 Trial 的 `CurrentSnapshot`、`Revision` 和实例级 `TrialStateChanged` 事件；不得读取私有字段，亦不得向 Trial 或 Beacon 写入任何状态。
3. `RootBridgeTemporaryCrossing` 必须保持原位置、原 Renderer、原承载 Collider 和始终可通行状态。不得禁用、移动、缩放、删除或以任何方式替换它。
4. 新的捷径只能通过启用一个初始不承载玩家、且与 Main Route 无正体积重叠的独立 Span 形成；不得在玩家脚下动态改造地面。
5. 采用组合式 SleepingForest 内容组件；不得新增通用 Bridge/Puzzle/Environment 基类、通用状态机、全局 Manager、static 状态、全局 EventBus 或 Service Locator。
6. 不实现 Root Bridge 以外的路线封锁、Puzzle Framework、Inventory、Ability、Network、Save、Quest、Audio、Animation、Tween、VFX、Completion 或 UI。

## 4. 架构与单向数据流

```text
WonderSquad.Content                         WonderSquad.Puzzle
ForestSignalTrialDefinition ───────► ForestSignalTrial（既有真值）
                                               │
                                  CurrentSnapshot / TrialStateChanged(snapshot)
                                               │ 仅实例级、只读
                                               ▼
                          SleepingForestRootBridgeConsequence（新增）
                             ├── RootBridgeAdvantageSpan（新增局部组合）
                             │     ├── state: Initial / Activated
                             │     ├── visual: Marker / MaterialPropertyBlock
                             │     └── collider: 仅独立捷径地面
                             └── Dormant / Activated route markers
                                               │
                                               ▼
                         SleepingForest Scene 的静态表现与独立捷径
```

`ForestSignalTrial` 不认识桥或 Scene 表现。`SleepingForestRootBridgeConsequence` 不知道输入、交互、Prompt 或 Beacon 的内部状态机；它只将 Snapshot 映射为本地环境状态。Scene/Prefab 负责把来自不同程序集的组件通过 Inspector 显式组合起来，不产生程序集反向引用。

## 5. Root Bridge 状态模型与 Revision 规则

### 5.1 局部状态

新增内容专用枚举 `SleepingForestRootBridgeState`：

| 状态 | 输入 Snapshot | Main Route | Advantage Span | 表现 |
| --- | --- | --- | --- | --- |
| `Initial = 0` | `IsCompleted == false` | `RootBridgeTemporaryCrossing` 始终存在 | Renderer / 承载 Collider 均禁用 | Dormant Marker 显示，Activated Marker 隐藏 |
| `Activated` | `IsCompleted == true` | 完全不变 | Renderer / 承载 Collider 同时启用 | Dormant Marker 隐藏，方向 Marker 与非颜色形状反馈显示 |

`IncorrectOrder` 不是桥状态。错序时桥保持 `Initial`；B → A → B 正常完成后，Completed Snapshot 覆盖为 `Activated`。003C 不增加计时、惩罚、失败或恢复框架。

### 5.2 Snapshot 消费与幂等

`SleepingForestRootBridgeConsequence` 持有序列化的 `ForestSignalTrial` 引用，并遵守以下算法：

1. `OnEnable` 订阅该实例的 `TrialStateChanged`，立即读取 `CurrentSnapshot`，同步一次当前场景状态。
2. 事件到达时，先比较 `snapshot.Revision` 与 `lastAppliedRevision`。
3. 首次同步可无条件应用；随后只有较新的 Revision 才可产生状态切换或视觉写入。
4. 相同 Revision、较旧 Revision、空/无效 Snapshot 都不创建物体、不改变 Collider、不重复写 PropertyBlock。
5. `OnDisable` 解除订阅；重新启用时重新读取 `CurrentSnapshot`，但不重置 Trial、不会把已完成状态回退为 Initial。

`lastAppliedRevision` 只是表现消费者的局部去重缓存，不是新的玩法真值，不持久化、不静态共享。运行时环境状态由同一 Scene 内的 Trial Snapshot 可完全重建，因此不增加 Environment Snapshot 或新的 ScriptableObject。

## 6. RootBridgeAdvantageSpan 组合设计

### 6.1 新增组件职责

| 组件 | 程序集 | 职责 | 禁止职责 |
| --- | --- | --- | --- |
| `SleepingForestRootBridgeConsequence` | `WonderSquad.Puzzle` | 订阅显式 Trial、将只读 Snapshot/Revision 映射为桥状态、协调局部组件 | 不修改 Trial；不读取输入；不控制 Main Route；不查找全场景对象 |
| `RootBridgeAdvantageSpan` | `WonderSquad.Puzzle` | 显式持有 Span Renderer、承载 Collider、Root GameObject；以幂等方式应用 `Initial`/`Activated` | 不订阅 Trial；不决定状态；不移动玩家；不建立通用桥抽象 |
| `RootBridgeAdvantageVisual` | `WonderSquad.Puzzle` | 使用每 Renderer 自己的 `MaterialPropertyBlock` 和 Marker 映射局部视觉 | 不改 Collider；不改 Trial；不访问 `renderer.material` 或写 `sharedMaterial` |
| `RootBridgeRouteMarker`（仅在职责确需拆分时） | `WonderSquad.Puzzle` | 管理 Dormant/Activated 的非阻挡标记显隐 | 不含计时、动画、输入或全局事件 |

若 `RootBridgeAdvantageVisual` 与 `RootBridgeRouteMarker` 最终只服务一个单一物件，可合并为一个小型 Visual 组件；不得为了预期扩展拆成框架。

### 6.2 Prefab 结构

新建内容专用的 `PF_RootBridgeAdvantageSpan`，其可复用含义限于 SleepingForest 的该一段捷径，而非通用桥 Prefab：

```text
PF_RootBridgeAdvantageSpan                 初始 inactive（或 Root active、承载子物体 inactive）
├─ RootBridgeAdvantageSpan                 显式 Renderer / BoxCollider 引用
├─ RootBridgeAdvantageVisual               显式 Renderer / Marker 引用
├─ GroundSurface                           MeshRenderer + MeshFilter + BoxCollider（唯一承载面）
├─ DirectionMarker                         无 Collider；Activated 时显示
├─ RootArchMarker                          无 Collider；Activated 时显示，非颜色反馈
└─ DormantRootMarker                       无 Collider；Initial 时显示
```

实施时应让根对象保持 active，以避免禁用组件生命周期造成意外订阅顺序；仅 `GroundSurface` 的 Renderer 与承载 Collider、以及各状态 Marker 按状态切换。无论采用何种具体层级，必须满足：

- `GroundSurface` 是唯一允许承载 Player 的新 Collider；
- Initial 下它的 Renderer 与 Collider 都禁用；Activated 下二者都启用；
- Marker、装饰和指引都没有阻挡 Player 的 Collider；
- 状态切换不 Instantiate/Destroy 对象；
- 每个 Renderer 各自拥有并复用自己的 `MaterialPropertyBlock`，不创建材质实例，不写共享材质资产。

### 6.3 视觉方案

Activated 视觉必须包含两类清晰反馈：

- **颜色覆盖**：以 `MaterialPropertyBlock` 对 Span 与 Activated Marker 施加森林信号的可配置颜色；
- **非颜色反馈**：可见的根系拱/叶片方向 Marker，用轮廓和朝向表达“新路径已出现”。

Dormant 状态仅保留不阻挡玩家的根系封闭/未响应标记。不得使用 Animator、Tween、粒子、音效、复杂 Shader 或持续 Update 轮询。稳定帧不得重复 `SetPropertyBlock`、创建字符串/集合或执行 LINQ。

## 7. Scene Authoring 与几何锁定

### 7.1 既有物件不得改变

下列 003A Greybox 对象继续保持原结构、位置和 Collider 职责：

- `RootBridgeTemporaryCrossing`：Main Route 的唯一既有跨越地面；
- `RootBridgeEntranceJunction`：Main Route 接口；
- `SliceEndGround`：目标区域地面；
- `RecoveryRoute` 与 `SleepingForestFallRecovery`：原有掉落恢复边界；
- `AdvantageRouteReserved`：保持其 Greybox 预留身份，003C 不重建、不封堵、不把它变成完成条件。

### 7.2 新增 Junction 与 Span 的固定连接策略

为避免 003A 已修复的 Z-fighting、双 Collider 与 CharacterController 卡顿，003C 新增两个**独立、边缘相接**的局部 Junction，而非让长路段插入既有平台：

```text
RootBridgeEntranceJunction（既有）
        └─边缘相接─► RootBridgeAdvantageStartJunction（新增）
                               └─► PF_RootBridgeAdvantageSpan（新增、Initial 不承载）
                                         └─► RootBridgeAdvantageExitJunction（新增）
                                                   └─边缘相接─► SliceEndGround（既有）
```

Authoring 锁定规则如下；数值是生成 Scene 后必须审计的目标锚点，而非可随意调参：

- Start Junction 仅接触 `RootBridgeEntranceJunction` 的西侧边界，不与它产生正体积交叠；
- Span 从 Start Junction 的北侧边缘出发，向 Slice End 的西侧安全入口延伸；不得穿入 `RootBridgeTemporaryCrossing` 的 `x = -3.5..3.5` 承载体积；
- Exit Junction 仅接触 `SliceEndGround` 的西侧边界，不把 Span 地面插入 `SliceEndGround` 内部；
- 顶面高度与既有地面齐平。若需要连接过渡，只允许一个连续斜坡或单个 Junction，不允许用统一 Y 偏移或两层共面 Cube 伪装拼接；
- Ground Surface 的净宽度必须不少于当前 Player CharacterController 的直径加安全余量；实施后以当前 Controller 实测转向/直行稳定性为准；
- Path 长度从 `RootBridgeEntranceJunction` 的路线选择点到 `SliceEndGround` 的优势出口，必须比 Main Route 的对应行走距离短至少 **15%**，并具有指向出口的 Marker；否则该捷径只是另一条普通路，不能通过 Sprint003C 验收。

实施必须记录每个新/调整路段的 Position、Rotation、Scale、Renderer Bounds、Collider Bounds 与相接边界。任何与现有路面、Main Bridge、Slice End Ground 发生正体积重叠或共面大面积覆盖的组合，都必须在提交前消除；不得用 Camera、URP、Render Queue、Depth Bias 或全局 Y 偏移掩盖。

### 7.3 Scene 对象装配

在 SleepingForest 的既有环境根下新增 `ForestSignalRouteConsequence`（避免使用已被 003A 边界测试保留的 `RootBridgeGameplay` 泛称）：

```text
Environment
└─ ForestSignalRouteConsequence
   ├─ SleepingForestRootBridgeConsequence
   │  ├─ ForestSignalTrial（Scene 显式引用）
   │  ├─ RootBridgeAdvantageSpan（Scene/Prefab 显式引用）
   │  └─ RootBridgeAdvantageVisual（Scene/Prefab 显式引用）
   ├─ RootBridgeAdvantageStartJunction
   ├─ PF_RootBridgeAdvantageSpan instance
   └─ RootBridgeAdvantageExitJunction
```

`ForestSignalRouteConsequence` 不需、也不得持有 Player、Camera、Detector、Prompt、Executor、Beacon 或 FallRecovery 引用。Trial、Span、Visual 和必要 Marker 的引用必须通过 Inspector 明确配置；禁止按 GameObject 名称查找。

Scene Authoring 只可通过显式、确认式、幂等的 Sprint003C 菜单命令或人工 Inspector 装配完成。禁止自动入口，禁止执行 Sprint003A Greybox Rebuild，禁止手工编辑 Scene YAML。重复执行 Authoring 时必须校验并更新唯一的既有 003C 对象，不得复制 Span、Junction 或 Consequence。

## 8. 物理安全与软锁审计

| 风险情境 | 设计控制 | 必测结果 |
| --- | --- | --- |
| 玩家站在 `RootBridgeTemporaryCrossing` 上完成 A → B | Main Bridge 的任何 Renderer/Collider 都不在 003C 的可写引用列表中 | 玩家位置不突变、不被夹住、不掉落，仍可走主路 |
| 玩家站在新增 Span 未来出现的位置附近 | 新 Span 初始不存在承载 Collider，Activated 时只新增一个与 Main 无正体积重叠的独立地面 | 无双层地面、无 GroundDetector 抖动，仍可退回 Main Route |
| 玩家已通过 Main Route | Main Route 永不关闭 | 可以继续到 Slice End 区域 |
| 玩家选择 Advantage Route | Marker 明确导向，入口/出口均边缘相接 | 快捷路线可走且有至少 15% 行走距离收益 |
| B → A → B | IncorrectOrder 不触碰桥和 Collider | 完成后正常 Activated，无永久不利状态 |
| FallRecovery 在 Initial/Activated 触发 | 003C 不写 RecoveryPoint 或 Recovery 组件 | 恢复后仍可经 Main Route 继续 |
| 同一 Completed Revision 重放或消费者 Disable/Enable | Revision 去重 + Snapshot 重建 | 不重复创建/切换物体，不产生 Collider 变化或异常 |
| 重新进入 Scene | Trial 重新建构 Revision 0；消费者同步 Snapshot | Initial 环境正确恢复，无跨 Scene 状态 |

不得对 `RootBridgeTemporaryCrossing` 执行 `SetActive`、禁用 Collider、销毁、变换或材质状态切换。这样即使发生内容组件配置错误，也不会破坏 003A 的保底通路和 FallRecovery。

## 9. 程序集关系

不新增生产 asmdef。维持既有有向依赖：

```text
WonderSquad.Core
├─ WonderSquad.Content
│  └─ WonderSquad.Puzzle        ← 新增的 SleepingForest consequence / span 组件
└─ WonderSquad.Interaction      ← 不引用 Puzzle

WonderSquad.Player / WonderSquad.Camera / WonderSquad.UI
└─ 不引用 WonderSquad.Puzzle
```

`WonderSquad.Puzzle` 继续只引用 `WonderSquad.Core` 和 `WonderSquad.Content`。它不增加对 Interaction、Player、Camera、UI、Network 或 UnityEditor 的引用。`WonderSquad.Editor` 可引用 Puzzle 仅用于显式 Authoring 和内容校验；Runtime 绝不引用 UnityEditor。

## 10. 完整文件计划

所有 Unity 资产由 Unity 生成并提交对应 `.meta`；不得手写 YAML。确切目录可随已有 SleepingForest 文件组织微调，但主要类型必须与文件名一致。

### 10.1 计划新增

- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/SleepingForestRootBridgeState.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/SleepingForestRootBridgeConsequence.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/RootBridgeAdvantageSpan.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/RootBridgeAdvantageVisual.cs`
- `Client/Assets/WonderSquad/Editor/SleepingForestRootBridgeConsequenceAuthoring.cs`（仅显式、确认式、幂等 Authoring/校验；若人工装配足够则不创建）
- `Client/Assets/WonderSquad/Prefabs/Gameplay/SleepingForest/PF_RootBridgeAdvantageSpan.prefab`
- `Client/Assets/WonderSquad/Tests/EditMode/SleepingForestRootBridgeConsequenceEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/SleepingForestRootBridgeConsequencePlayModeTests.cs`
- `Docs/01_Project/Sprint003C_Implementation_Report.md`
- `Docs/01_Project/Sprint003C_Review_Checklist.md`

### 10.2 计划修改

- `Client/Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity`：仅增加 003C 专用 Consequence、Junction、Span Prefab 与显式引用；不修改 Main/Recovery/Foundation 对象。
- 现有 SleepingForest Scene 校验/PlayMode 测试：仅将“无 003B InteractionTarget”一类过时断言更新为允许已批准 Beacon 和 003C 环境对象，同时继续验证单 Player、单 Main Camera、Main Route、FallRecovery 与 Foundation 边界。
- `CHANGELOG.md`：实施完成并通过 Unity 验收后记录 Sprint003C。

不修改 `ForestSignalTrial`、Beacon Definition、Interaction 生产代码、Player、Input、Camera、FallRecovery、Builder、Packages、ProjectSettings 或任何 ScriptableObject Definition。

## 11. 自动化测试计划

测试数量以实现后的 Unity Test Runner 实际结果为准，不在计划中预设计数。

### 11.1 EditMode

1. Consequence 配置校验：Trial、Span、Visual、所有 Renderer/Collider/Marker 引用非空，且都属于同一 Scene。
2. Initial Snapshot：Main Bridge 仍是原对象且未被 Consequence 持有为可写状态；Span Renderer/Collider 均关闭；Dormant Marker 状态正确。
3. Completed Snapshot：仅 Span Renderer/Collider 同时启用；Activated Marker 和非颜色反馈正确；Main Bridge 的 active、Transform、Collider enabled 状态不变。
4. 非 Completed（包括 IncorrectOrder）Snapshot 保持 Initial；Completed 后正确覆盖为 Activated。
5. 相同 Revision 重放幂等：没有额外对象/Collider、没有重复视觉写入或状态转换。
6. 更高 Revision 的 Initial/Completed 映射确定；Disable/Enable 后由 `CurrentSnapshot` 重建状态。
7. 每 Renderer 的 `MaterialPropertyBlock` 隔离；不访问 `renderer.material`，不修改 shared Material。
8. Span、Junction、Main Bridge、SliceEndGround 的 Renderer Bounds 和 Collider Bounds 无正体积重叠；连接面只在规定边缘相接。
9. 静态扫描/程序集校验：Puzzle 依赖方向不变；Foundation 与 003B 核心契约/Definition 未修改。

### 11.2 PlayMode

1. 直接加载 SleepingForest 时为 Initial：单 Player、单 Main Camera、Main Route 和 FallRecovery 正常，Span 不可承载。
2. 通过既有 A → B 完成 Trial 后，事件驱动激活 Span；不使用测试直接写 Trial 私有状态。
3. 玩家在 Main Bridge/Junction 附近完成 Trial 时位置、接地和移动稳定，Main Route 始终能继续走。
4. Activated 后 Advantage Span 可通行，且以预定义测试锚点验证其到出口的路线长度/导向收益符合至少 15% 的设计阈值。
5. B → A → B 后最终仍会 Activated；错序前后桥与 Main Route 皆不发生永久变化。
6. 同一 Completed Revision 重放、重复启用消费者及重新订阅不产生多个 Span/Collider，不改变玩家位置。
7. Initial 与 Activated 下分别触发 FallRecovery，恢复后玩家仍能移动、接地并进入 Main Route。
8. 003B Beacon 的 Detection、Prompt、Execution、长按去重、A/B 独立视觉回归通过。
9. Character Foundation 与 Sprint002A–D、Sprint003A–B 完整回归通过，Console Error 为 `0`。

PlayMode 场景加载继续采用测试边界允许的 Editor PlayMode Scene Asset Path 方案；不将 SleepingForest 加入正式 Build Profile，也不提前实现 Sprint003D Build 入口。

## 12. PlayerSandbox 与正式场景人工验收

系统验证仍可在 PlayerSandbox 进行，但 003C 的人工验收必须在正式 `SleepingForest.unity`。

1. 进入场景，确认 Main Route、单 Player、单 Main Camera、Movement、GroundDetector、FallRecovery 正常；Span 初始不可见/不可承载。
2. 依次激活 A → B，确认 Trial Completed 后根系/方向 Marker 和 Span 出现，且不开语音也能通过形状/方向理解新路线。
3. 站在 `RootBridgeTemporaryCrossing` 与入口 Junction 附近完成 A → B；确认没有角色夹住、掉落、地面抖动或 Main Bridge 改变。
4. 不走捷径，沿 Main Route 到达 Slice End 区域；确认通路不因 Trial 或事件而关闭，且没有 Completion UI（003D 范围）。
5. 重载场景后执行 A → B，走 Advantage Span；确认入口/出口无台阶感、无 Z-fighting，路线明显更短、更容易辨识，并能随时退回 Main Route。
6. 执行 B → A → B；确认错序不改变桥、不锁死路线，最终仍激活 Span。
7. 在 Initial 与 Activated 各触发一次 FallRecovery；确认恢复后均可继续使用 Main Route。
8. 重复 Completed 状态或 Disable/Enable Consequence；确认环境只应用一次、不产生重复 Span/Collider/视觉污染。
9. 检查 Console Error `0`；Profiler 中稳定状态不存在本组件持续 GC.Alloc。

## 13. 为 Sprint003D 预留的边界

003C 只提供可由后续读取的、现有 Trial 完成事实和已激活的环境结果；不创建新的关卡流程框架。Sprint003D 可在不修改本组件内部逻辑的前提下：

- 以独立的 Slice End Trigger 读取 `ForestSignalTrial.CurrentSnapshot.IsCompleted`；
- 在到达终点时决定 `Level Completed` 与 Completion UI；
- 将 003C 的 Main/Advantage 路线作为完成前可选择的环境路径。

003D 不应读取 `SleepingForestRootBridgeConsequence` 的私有状态，也不应把终点、UI 或 Build Profile 引用反向塞入它。若未来需要网络，Host Authority 只同步 Trial 的权威 Snapshot/Revision；客户端的桥表现仍是该只读状态的派生结果。本 Sprint 不实现任何网络代码。

## 14. 禁止范围

- Root Bridge Main Route 的移除、封锁、替换或承载 Collider 修改；
- Slice Completion、End Trigger、Completion UI、下一关、Build Profile 入口；
- Puzzle Framework、通用 Bridge/Environment/GameObject 基类、通用状态机；
- Door、Lever、Chest、Inventory、Crafting、Ability、Quest、Save、Network；
- Character Foundation、Interaction Foundation、ForestSignalTrial/Beacon 核心状态机、Input、Camera、Movement、GroundDetector、FallRecovery；
- ScriptableObject Definition 运行时写入；
- Animator、Tween、音效、VFX、复杂 Shader；
- `Renderer.material`、`sharedMaterial` 写入、持续 GC 分配、每帧查找/轮询；
- 全局 EventBus、静态 Gameplay 状态、Service Locator、自动 Scene Builder 入口。

## 15. Definition of Done

- [ ] 003C 仅通过 Trial 的只读 Snapshot、Revision 与实例事件驱动环境后果。
- [ ] Main Route / `RootBridgeTemporaryCrossing` 完全未被本 Sprint 修改，始终可通行。
- [ ] `RootBridgeAdvantageSpan` 初始不承载、Completed 后安全启用，且与既有地面没有正体积/共面重叠。
- [ ] Advantage Route 有至少 15% 的可观察步行距离/导向优势，但不是通关必需。
- [ ] 初始、Completed、错序恢复、同 Revision 重放、Disable/Enable、Scene 重入行为确定且幂等。
- [ ] 视觉使用 `MaterialPropertyBlock` 与非颜色 Marker；无共享材质污染、材质实例泄漏或稳定帧持续 GC。
- [ ] 软锁审计覆盖 Main Bridge 站位、Span 接口、错序、FallRecovery、重复事件和场景重入。
- [ ] EditMode、PlayMode、完整回归与正式场景人工验收全部通过，Console Error `0`。
- [ ] Implementation Report、Review Checklist、CHANGELOG 和 Gate Review 已在实施/验收阶段更新。

## 16. 最终结论

# READY FOR IMPLEMENTATION

Sprint003C 已锁定为“保留安全主路、只读驱动独立优势 Span”的最小环境后果。实施前不得扩展为完成流程、通用 Puzzle 框架或任何 Foundation 改动。
