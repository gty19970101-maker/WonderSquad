# Sprint003B — Forest Beacon Interaction State Implementation Plan

## 1. 文档状态

- 计划状态：`READY FOR IMPLEMENTATION`
- Unity 基线：`6000.3.21f1`
- 输入基线：Sprint003A `PASSED / GO`
- 自动化回归基线：EditMode `82/82`，PlayMode `50/50`
- 正式场景：`Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity`
- 本文仅锁定实施方案；本轮不修改代码、Prefab、Scene、ScriptableObject、Input Actions、Packages 或 ProjectSettings。

## 2. Sprint 目标

在已通过验收的 SleepingForest Greybox 中，将 Beacon A 与 Beacon B 接入现有 Detection → Prompt → Execution 链路，并建立一个关卡实例级、可观察且可恢复的“森林信号试炼”状态闭环：

```text
玩家发现 Beacon
→ 现有 Prompt 展示“校准信标”
→ 现有 Executor 重新验证并提交请求
→ ForestBeaconInteraction 将合法请求交给 ForestSignalTrial
→ Trial 按固定 A → B 顺序更新运行时状态、Snapshot 与 Revision
→ 两个 Beacon 分别刷新自己的视觉和可交互状态
```

本 Sprint 只验证 Beacon 交互状态及反馈。Trial 完成不打开 Root Bridge、不封锁或开放路线、不触发 Slice Completion。

## 3. 不可变实施决策

1. 正确顺序固定为 `A → B`，不由运行时动态重排。
2. Beacon A/B 共享一个 Scene 内 `ForestSignalTrial`，但各自拥有独立 Target、Prompt Source、执行适配器、Renderer、Marker 与 `MaterialPropertyBlock`。
3. `ForestSignalTrial` 是唯一逻辑真值持有者；Beacon 组件不得保存可分叉的 Trial 副本。
4. 静态 Definition 只读；运行时不得修改 ScriptableObject。
5. Trial 状态事件只允许实例级 C# event；禁止 static event、全局 EventBus、Service Locator 或静态 Manager。
6. 复用现有 `IInteractable`、`IExecutableInteraction`、`InteractionTarget`、Prompt、Detector、Executor 与 Request Port，不修改 Sprint002A–D 的生产契约或实现。
7. 视觉实例隔离采用 `MaterialPropertyBlock`；禁止访问 `Renderer.material`，禁止写入 `sharedMaterial`。
8. 当前生命周期仅限本次 SleepingForest Scene 实例；不做跨 Scene、存档或网络持久化。

## 4. 架构与数据流

```text
                         静态只读内容
                  ForestSignalTrialDefinition
                     ├─ Beacon A Definition
                     └─ Beacon B Definition
                               │
                               ▼
InteractionDetector ──只读──> InteractionTarget
        │                         ▲
        ▼                         │ 可用性映射
InteractionPromptPresenter        │
        │                         │
        ▼                         │
InteractionPromptView             │

Interaction Input
        │
        ▼
InteractionExecutor
        │ 重新验证距离 / Layer / 遮挡 / 存活 / TargetId
        ▼
LocalInteractionRequestPort
        │ 去重后的 InteractionRequest
        ▼
ForestBeaconInteraction（A 或 B）
        │ 传递“实际 Scene 组件引用 + Context”，不只传任意 TargetId
        ▼
ForestSignalTrial（Scene 唯一运行时真值）
        ├─ 更新 Beacon Runtime State
        ├─ 生成只读 Snapshot
        ├─ Revision 递增
        └─ 发布实例级 TrialStateChanged
                 │
                 ├─> ForestBeaconInteraction A/B：映射可检测/可执行状态
                 └─> ForestBeaconVisual A/B：映射实例视觉
```

依赖只能向稳定契约与内容数据方向流动。Detector、Prompt、Executor 不认识 Forest Beacon；Player、Camera、FallRecovery 不引用 Puzzle。

## 5. 核心类型职责

### 5.1 `ForestBeaconDefinition`

位置：`WonderSquad.Content`。

它是 `ForestSignalTrialDefinition` 内的可序列化只读配置项，不是独立运行时状态，也不单独创建 ScriptableObject 资产。每项只包含：

- Beacon Slot：`BeaconA` 或 `BeaconB`；
- 稳定 `InteractionTargetId`；
- 必要的静态校验信息。

它不得包含 Scene Object、Renderer、运行时状态、Revision、最近请求、环境后果或网络字段。

### 5.2 `ForestSignalTrialDefinition`

位置：`WonderSquad.Content`，资产类型为 ScriptableObject。

职责：

- 保存 schema version 与稳定 Trial ID；
- 保存 Beacon A/B 两项 `ForestBeaconDefinition`；
- 固定表达 A 为第一步、B 为第二步；
- 验证 Trial ID、Slot 和 TargetId 均有效，且 A/B Slot 与 TargetId 不重复。

禁止职责：

- 不保存 Dormant/Activated、IncorrectOrder、Completed 或 Revision；
- 不写回运行时结果；
- 不持有 Scene 组件引用；
- 不包含 Root Bridge、路线、Completion、Ability、Network 或 Save 配置。

### 5.3 `ForestBeaconRuntimeState`

位置：`WonderSquad.Puzzle`，只读值类型。

每个实例状态至少包含：

- `Slot`；
- `TargetId`；
- `LogicalState`：`Dormant` 或 `Activated`；
- 派生只读属性 `IsActivated`。

该结构由 Trial 创建并包含在 Snapshot 中。Beacon Interaction 与 Visual 只读取，不自行改写。

### 5.4 `ForestSignalTrialSnapshot`

位置：`WonderSquad.Puzzle`，不可变只读值类型。

必须包含：

- `Phase`：`AwaitingFirstBeacon`、`AwaitingSecondBeacon` 或 `Completed`；
- Beacon A 的完整 `ForestBeaconRuntimeState`；
- Beacon B 的完整 `ForestBeaconRuntimeState`；
- `LastAttemptedTargetId`；
- `LastAttemptedSlot`；
- `LastOutcome`：`None`、`FirstBeaconActivated`、`IncorrectOrder` 或 `TrialCompleted`；
- `Revision`：`uint`；
- 派生只读属性 `HasIncorrectOrder` 与 `IsCompleted`。

Snapshot 不包含 GameObject、Transform、Collider、Material、Presenter、本地化字符串或网络类型。003C 只能通过该 Snapshot 判断 A、B、IncorrectOrder、Trial Completed 与 Revision，无需读取 Trial 私有字段。

### 5.5 `ForestSignalTrial`

位置：`WonderSquad.Puzzle`，Scene 内唯一实例。

职责：

- 从只读 Definition 初始化本 Scene 实例的状态；
- 显式持有 Scene 中 Beacon A 与 B 的 `ForestBeaconInteraction` 引用；
- 校验配置的组件、Scene 归属、Slot 和 TargetId；
- 处理来自已配置 Beacon 实例的执行尝试；
- 以原子顺序提交状态、生成 Snapshot、递增 Revision、发送实例事件；
- 暴露只读 `CurrentSnapshot`；
- 暴露实例级 `TrialStateChanged(snapshot)`。

禁止职责：

- 不查找 Beacon、Player、Detector、Prompt 或环境对象；
- 不控制 Root Bridge、路线、终点、UI 或全局 Lighting；
- 不保存到 ScriptableObject、静态字段或 `DontDestroyOnLoad`；
- 不执行网络权威逻辑。

### 5.6 `ForestBeaconInteraction`

位置：`WonderSquad.Puzzle`，实现现有 `IExecutableInteraction`。

职责：

- 显式引用同对象上的、实现 `IInteractable` 的 `MonoBehaviour`；
- 显式引用所属 `ForestSignalTrial`；
- 保证其 `TargetId` 与目标组件一致；
- 将 `Execute(context)` 适配为 Trial 的实例请求；
- 根据 Snapshot 将本 Beacon 映射为可执行或不可执行；
- Activated 后禁用本实例的检测 Target，使 Prompt 从现有链路自然隐藏。

它不拥有 Trial 真值、不直接控制 Visual、不读取输入、不修改 Prompt，也不依赖具体 `InteractionTarget` 类型。`WonderSquad.Puzzle` 因此不需要引用 `WonderSquad.Interaction`。

### 5.7 `ForestBeaconVisual`

位置：`WonderSquad.Puzzle`。

职责：

- 订阅所属 Trial 的实例事件，并读取自身 Slot 对应的 Snapshot 状态；
- 将状态映射为 `Dormant`、`Activated` 或派生的 `Incorrect` 表现；
- 每个 Renderer 使用自己的 `MaterialPropertyBlock`；
- 使用颜色覆盖加一种非颜色反馈；
- 记录最后应用的 Revision，稳定状态不重复写 Renderer 或切换对象。

非颜色反馈锁定为子 Marker 显隐：

- Dormant：`DormantMarker` 可见；
- Activated：`ActivatedRing` 可见；
- Incorrect：`IncorrectMarker`（叉形或倾斜标记）可见。

不使用 Animator、Tween、粒子、音效、动态材质实例或复杂 Shader。

## 6. Scene 来源真实性与配置校验

仅检查 TargetId 不足以证明请求来自 Scene 中实际配置的 Beacon。因此 Trial 的内部协调入口必须同时接收：

- 发起请求的 `ForestBeaconInteraction` 组件实例；
- 现有 `InteractionContext`。

Trial 在任何状态推进前按以下顺序校验：

1. Definition 与 A/B 配置有效；
2. source 不为 null，且与 Trial Inspector 中配置的 Beacon A 或 Beacon B 引用完全相同；
3. source 与 Trial 属于同一个已加载 Scene；
4. source 的 Slot 与 Definition 对应 Slot 一致；
5. source 的 `TargetId` 与 Definition 对应 TargetId 一致；
6. source 的执行状态在当前 Snapshot 中可用；
7. Context 与请求目标仍有效；通用距离、Layer、遮挡和存活校验仍由现有 Executor 负责。

Trial 不提供“只传 TargetId 即可推进”的公共写入口。任意伪造 TargetId、未配置的第三个 Beacon、跨 Scene 组件或配置不匹配都返回 `TargetInvalid`，不改变 Snapshot、不递增 Revision、不发送事件。

若 Scene 初始配置无效，Trial 进入不可执行的安全状态，两个 Beacon 均不得推进，并记录一次清晰错误；不得以名称查找或自动猜测替代引用。

## 7. 状态机与确定性行为

### 7.1 初始状态

```text
Phase: AwaitingFirstBeacon
Beacon A: Dormant
Beacon B: Dormant
LastAttemptedTargetId: Invalid/Empty
LastAttemptedSlot: Unknown
LastOutcome: None
Revision: 0
```

### 7.2 转换表

| 当前状态 | 请求 | 通用 Result | 新状态 | Revision | 视觉与可交互结果 |
| --- | --- | --- | --- | --- | --- |
| AwaitingFirst；A/B Dormant | A | `Success` | A Activated；B Dormant；AwaitingSecond；Outcome=FirstBeaconActivated | `+1` | A Activated 并退出检测；B Dormant |
| AwaitingFirst；A/B Dormant | B | `Success` | A/B 仍 Dormant；仍 AwaitingFirst；Outcome=IncorrectOrder | `+1` | B 显示 Incorrect，但保持可检测/可执行 |
| IncorrectOrder 后仍 AwaitingFirst | A | `Success` | A Activated；B Dormant；AwaitingSecond；Outcome=FirstBeaconActivated | `+1` | A Activated；B 清除 Incorrect，恢复 Dormant |
| AwaitingSecond；A Activated/B Dormant | B | `Success` | A/B Activated；Completed；Outcome=TrialCompleted | `+1` | A/B Activated，均退出检测 |
| B 已显示 Incorrect，玩家再次形成新的合法 B 按压 | B | `Success` | 逻辑状态不变；LastOutcome 仍 IncorrectOrder，记录本次有效尝试 | `+1` | B 保持 Incorrect；事件按新 Revision 发送一次 |
| 任意 Beacon 已 Activated | 对该 Beacon 的新/陈旧请求 | 正常 Detector/Executor 链为 `TargetInvalid`；隔离地绕过 Executor 直接到 Request Port 为 `Busy` | 不变 | 不变 | 不刷新，不回到 Dormant |
| Completed | 任一 Beacon | 同上 | 不变 | 不变 | 不变 |
| 任意 | 未配置 source、伪造 TargetId、跨 Scene source、无效 Context | `TargetInvalid` 或现有契约对应失败码 | 不变 | 不变 | 不刷新 |
| 任意 | 相同 PlayerId + RequestId 重放 | Request Port 返回缓存 Result | 不变 | 不变 | 不刷新 |

`IncorrectOrder` 是 Trial 领域结果，不新增或修改 Core 的 `InteractionResultCode`。返回 `Success` 表示这次合法交互已被内容层接受并产生确定的 Trial 结果，不表示 Trial 已完成。

### 7.3 错序恢复规则

B 在 A 之前交互时：

1. B 不激活；A/B 仍为 Dormant；
2. Trial 记录 `IncorrectOrder` 并递增 Revision；
3. 只有 B 显示 Incorrect，路线与环境不变；
4. B 仍可交互，不产生软锁；
5. 玩家随后操作 A，Trial 正常推进到 AwaitingSecond，同时清除 B 的 Incorrect 表现；
6. 玩家再操作 B，Trial 正常 Completed。

错误不要求重启 Scene、等待冷却或使用额外资源。

### 7.4 已激活重复交互规则

- Activated 是单向状态，本 Sprint 不存在 Activated → Dormant 转换。
- Activated 后其 `InteractionTarget` 被禁用，因此正常玩家链路不再选择它，也不显示 Prompt。
- Executor 手中的陈旧目标请求在重新验证时返回 `TargetInvalid`。
- 仅在隔离测试绕过 Executor、直接调用 Request Port 时，由 `IsExecutionAvailable == false` 返回 `Busy`。
- 无论哪种路径，Trial 不推进、Revision 不增加、事件不发送、视觉不重复应用。

## 8. Revision 与事件规则

### 8.1 Revision

Revision 初始为 `0`。只有 Trial 接受一个新的领域尝试并提交新的“尝试结果快照”时递增一次：

- A 正确首次激活：递增；
- B 错序：递增；
- 错序后 A 正确激活：递增；
- B 正确完成 Trial：递增；
- B 在尚未激活时由玩家形成另一笔新的错序请求：递增。

以下情况绝不递增：

- 初始化与消费者首次同步；
- 相同 RequestId 重放；
- Executor 在距离、Layer、遮挡、存活或 TargetId 验证阶段拒绝；
- source 不是 Scene 中配置的 A/B；
- Activated/Completed 状态的陈旧或绕过请求；
- 组件 Disable/Enable；
- Visual 重绘；
- Scene 中其他环境变化。

支持的单局生命周期内 Revision 必须单调递增且不得回绕；到达 `uint.MaxValue` 时必须安全拒绝新的状态提交并记录错误，不允许回到 `0`。

### 8.2 实例级事件

`ForestSignalTrial` 提供：

```text
CurrentSnapshot
TrialStateChanged(ForestSignalTrialSnapshot snapshot)
```

事件规则：

- 事件是 Trial 实例字段，禁止 static；
- 每次 Revision 成功递增后，在完整 Snapshot 提交后同步发送一次；
- 被拒绝或重复的请求不发送；
- 初始化 Revision 0 不依赖事件广播；
- 消费者启用时采用“订阅当前 Trial → 立即读取并应用 CurrentSnapshot”的方式同步初态；
- 消费者禁用/销毁时解除订阅；重新启用只重读快照，不重置 Trial；
- 消费者用 Revision 与首次应用标志避免相同 Snapshot 重复刷新。

该边界同时作为 003C 的唯一预留接口，不新增全局事件系统。

## 9. Scene 生命周期与持久化边界

1. 每次进入或重新加载 SleepingForest，Scene 创建新的 `ForestSignalTrial` 实例。
2. Trial 在初始化阶段从 Definition 建立初始 Snapshot：A/B Dormant、AwaitingFirst、Revision 0。
3. 同一 Scene 会话内单纯 Disable/Enable Trial 或消费者不得重置进度；消费者从 `CurrentSnapshot` 恢复显示。
4. 卸载 Scene 即销毁本次 Trial 状态；重新进入从 Revision 0 开始。
5. 本 Sprint 不使用 PlayerPrefs、文件、Save 模块、ScriptableObject 写回、静态缓存或 `DontDestroyOnLoad`。
6. 003C 只能消费当前 Scene 实例状态；跨 Scene、断线恢复和存档留待明确后续设计。

## 10. Beacon A/B 组合与配置

### 10.1 稳定身份

- Beacon A TargetId：`sleeping_forest.beacon.a`
- Beacon B TargetId：`sleeping_forest.beacon.b`
- Prompt ID：`sleeping_forest.beacon.activate`
- 本地化预留 Key：`interaction.sleeping_forest.beacon.activate`
- MVP 回退文本：`校准信标`

A/B 可以共享只读 Prompt Definition 和静态材质资产，但不能共享运行时状态、Renderer PropertyBlock 或可变组件实例。

### 10.2 `PF_ForestBeacon` 结构

```text
PF_ForestBeacon                         Interactable Layer
├─ InteractionTarget                   现有；Scene 实例覆盖唯一 TargetId
├─ InteractionPromptSource             现有；引用共享只读 Prompt Definition
├─ ForestBeaconInteraction             新增；显式 Target + Trial + Slot 引用
├─ ForestBeaconVisual                  新增；显式 Renderer + Marker + Trial + Slot 引用
├─ Trigger Collider                     isTrigger = true
├─ DetectionAnchor                     显式检测锚点
└─ Visual
   ├─ BeaconBody                       单一主 Renderer
   ├─ DormantMarker
   ├─ ActivatedRing
   └─ IncorrectMarker
```

Prefab 资产负责结构完整性；最终 TargetId、Slot 和 Trial 引用由 SleepingForest 中两个实例显式配置。Scene 验证必须拒绝空 ID、模板 ID、重复 ID、错误 Slot 或未引用 Trial 的实例。

### 10.3 Scene 配置

- Scene 新增一个 `ForestSignalTrial` 对象；
- Trial 引用 `ForestSignalTrialDefinition.asset`、Beacon A interaction 和 Beacon B interaction；
- 使用两个 `PF_ForestBeacon` 实例替换或承接 `BeaconA_Reserved` / `BeaconB_Reserved` 的功能位置；
- 复用 003A 预留 Detection Anchor 的位置；
- 每处仅保留一个主视觉表面，避免与 Reserved Mesh 共面重叠；
- A/B Collider 均为 Interactable Layer 的 Trigger，不阻挡 Player；
- 不为 Trial 配置 Root Bridge、路线、终点或 Completion 引用。

003B 接入后不得运行 `Rebuild Sleeping Forest Greybox`。若未来必须重建几何，应停止并以独立 Scene Authoring 变更重新集成 Beacon；本 Sprint 不修改 Builder，也不恢复自动重建入口。

## 11. 与现有 Interaction Foundation 的连接

| 现有部分 | 003B 使用方式 | 是否修改 |
| --- | --- | --- |
| `IInteractable` | 由现有 `InteractionTarget` 提供检测身份 | 否 |
| `IExecutableInteraction` | 由 `ForestBeaconInteraction` 实现 | 否 |
| `InteractionDetector` / Validator | 按现有范围、Layer、遮挡规则检测 | 否 |
| Prompt Source / Presenter / View | 读取共享只读 Prompt Definition 并显示 | 否 |
| `InteractionExecutor` | 在调用执行契约前完成现有重新验证 | 否 |
| `LocalInteractionRequestPort` | 保留 RequestId 去重和本地请求边界 | 否 |
| Player Interaction Input | 保留单次按下触发与长按去重 | 否 |

`ForestBeaconInteraction` 只通过 Core 契约接入。不得让 Prompt、Detector、Input 或 Visual 绕过 Executor/Request Port 直接推进 Trial。

## 12. 程序集关系

不新增生产 asmdef，保持：

```text
WonderSquad.Core
   ├── WonderSquad.Interaction
   └── WonderSquad.Content
           └── WonderSquad.Puzzle

WonderSquad.Puzzle references: WonderSquad.Core, WonderSquad.Content
WonderSquad.Interaction references: WonderSquad.Core
```

约束：

- Puzzle 不引用 Interaction、Player、UI、Camera 或 Network；
- Interaction 不引用 Puzzle 或 Beacon；
- Player/Camera 不引用 Puzzle；
- Unity Scene/Prefab 可以组合不同程序集的 MonoBehaviour，这不构成代码反向依赖；
- 测试 asmdef 仅在测试需要直接引用新类型时增加 Content/Puzzle 引用。

## 13. 完整文件计划

所有 Unity 资产均包含由 Unity 生成的对应 `.meta` 文件；实施时不得手写 Scene YAML。

### 13.1 计划新增

内容定义：

- `Client/Assets/WonderSquad/Runtime/Content/SleepingForest/ForestBeaconSlot.cs`
- `Client/Assets/WonderSquad/Runtime/Content/SleepingForest/ForestBeaconDefinition.cs`
- `Client/Assets/WonderSquad/Runtime/Content/SleepingForest/ForestSignalTrialDefinition.cs`

运行时状态与组件：

- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestBeaconLogicalState.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestSignalTrialPhase.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestSignalTrialOutcome.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestBeaconRuntimeState.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestSignalTrialSnapshot.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestSignalTrial.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestBeaconInteraction.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/ForestBeaconVisual.cs`

资产：

- `Client/Assets/WonderSquad/ScriptableObjects/Content/SleepingForest/ForestSignalTrialDefinition.asset`
- `Client/Assets/WonderSquad/ScriptableObjects/Content/Interaction/InteractionPrompt_ForestBeacon.asset`
- `Client/Assets/WonderSquad/Prefabs/SleepingForest/PF_ForestBeacon.prefab`

测试：

- `Client/Assets/WonderSquad/Tests/EditMode/ForestSignalTrialEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/EditMode/ForestBeaconConfigurationEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/ForestSignalTrialPlayModeTests.cs`

交付文档：

- `Docs/01_Project/Sprint003B_Implementation_Report.md`
- `Docs/01_Project/Sprint003B_Review_Checklist.md`

### 13.2 计划修改

- `Client/Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity`
- `Client/Assets/WonderSquad/Tests/EditMode/SleepingForestGreyboxEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/SleepingForestGreyboxPlayModeTests.cs`
- EditMode/PlayMode 测试 asmdef：仅在缺少时增加 `WonderSquad.Content` 与 `WonderSquad.Puzzle` 引用
- `CHANGELOG.md`
- `Docs/01_Project/Sprint003B_Preflight_Report.md`：实施后只回填状态与实际验证结果

### 13.3 明确不修改

- `WonderSquad.Core` 的 Interaction 契约；
- `WonderSquad.Interaction` 的 Detector、Validator、Prompt、Executor 与 Request Port；
- Character、Input、Movement、GroundDetector、Camera、FallRecovery；
- SleepingForest Greybox Builder；
- Input Actions、Packages、ProjectSettings、Build Profile；
- Root Bridge、路线 Collider 和 Slice End 逻辑。

## 14. 过时 Sprint003A 测试的精确更新

以下断言在 003B 获批内容接入后过时：

- EditMode `SleepingForestScene_HasNoSandboxOrFutureGameplayFixtures` 中“Scene 不得包含任何 InteractionTarget”；
- PlayMode `SleepingForest_HasNoActiveInteractionOrCompletionGameplay` 中同类断言。

更新原则：

1. 改为要求 Scene 恰好存在两个正式 Forest Beacon Target；
2. 两个 TargetId 必须分别等于 `sleeping_forest.beacon.a` 与 `sleeping_forest.beacon.b`，且唯一；
3. 每个 Target 必须与正确 Slot、Prompt Source、ForestBeaconInteraction、ForestBeaconVisual 和 Trigger Collider 配套；
4. Scene 仍不得包含 `InteractionExecutionProbe`、`InteractionProbeBehaviour`、`PF_InteractionProbe` 诊断行为或 Sandbox Target；
5. Scene 仍不得包含 Root Bridge Gameplay、路线切换、Completion Gameplay、Ability、Inventory、Network；
6. 保留 003A 的单 Player、单 Main Camera、FallRecovery、Geometry、P0/Debug Fixture 排除与 Foundation 边界断言；
7. 不通过删除测试、放宽为“至少一个”或忽略类型来掩盖配置错误。

测试名称可重命名为“无 Sandbox/后续环境及 Completion Gameplay”，使含义与批准的两个 Beacon 一致。

## 15. EditMode 测试计划

至少覆盖：

1. Definition 的 schema、Trial ID、A/B Slot、TargetId 有效且唯一；
2. 运行时不会修改 Definition 资产数据；
3. 初始 Snapshot 为 A/B Dormant、AwaitingFirst、Outcome None、Revision 0；
4. A 首次执行后只激活 A，进入 AwaitingSecond，Revision 恰好 +1；
5. B 首先执行产生 IncorrectOrder，A/B 均不激活，Revision 恰好 +1；
6. B 错序后执行 A，再执行 B，可以正常 Completed；
7. 已激活 Beacon 不回到 Dormant，重复/陈旧请求不递增 Revision；
8. 每笔新的合法错序 B 请求只递增一次；相同 RequestId 重放不递增；
9. 未配置 source、第三个 source、伪造 TargetId、跨 Scene source 均不能推进；
10. Snapshot 全部公开数据只读，足以表达 A、B、IncorrectOrder、Completed 与 Revision；
11. 事件只属于 Trial 实例；状态提交后每个新 Revision 仅触发一次；无 static event；
12. Disable/Enable 消费者不重置 Trial，重新读取 Snapshot 后一致；
13. 两个 Visual 使用独立 PropertyBlock，A/B 切换互不污染 shared Material；
14. 视觉至少同时验证颜色覆盖与 Marker 显隐；
15. 相同 Revision 不重复刷新 Visual；
16. `PF_ForestBeacon` 必需组件、Layer、Trigger、Anchor 与显式引用完整；
17. Scene 中 A/B TargetId、Slot 与 Trial 配置匹配；
18. Interaction 核心契约签名未变化；Puzzle/Content/Interaction asmdef 依赖方向正确；
19. 更新后的 Sprint003A 边界测试仍能阻止 Probe、Root Bridge、Completion 等越界对象进入正式 Scene。

## 16. PlayMode 测试计划

使用 Editor PlayMode 场景加载 API 按 Asset Path 加载 SleepingForest，不要求提前加入 Build Profile。测试结束正确卸载并清理 Scene。

至少覆盖：

1. Scene 加载后只有一个有效 Trial、两个唯一 Beacon、一个 Player 与一个 Main Camera；
2. 靠近 A/B 时现有 Detector 与 Prompt 正常，显示共享 Beacon Prompt；
3. A → B 正确完成，Snapshot、Revision、Target 可用性与 Visual 一致；
4. B → A 首步产生 IncorrectOrder，B 不激活，Trial 不完成；
5. 错序后 A → B 可以正常完成，无 Scene 重启与软锁；
6. 已激活 Beacon 从检测链退出，Prompt 隐藏，重复请求不重复推进；
7. 长按交互键只执行一次，松开再按才形成新请求；
8. A/B 颜色与 Marker 独立，且不修改 shared Material；
9. 遮挡、超距、目标销毁、组件禁用和无效目标不会推进 Trial或产生异常；
10. Trial Completed 后 Root Bridge、Main/Advantage Route、Slice End 与 Completion 仍保持 003A 状态；
11. Player Movement、Camera Follow、FallRecovery、单 Player/单 Camera 正常；
12. Sprint002A–D 与 Sprint003A 全部回归继续通过；
13. Console Error = 0，稳定状态无 Beacon 系统持续 `GC.Alloc`。

## 17. 完整回归策略

实施完成后按顺序执行：

1. Sprint003B 专项 EditMode；
2. 完整 EditMode；
3. Sprint003B 专项 PlayMode；
4. 完整 PlayMode；
5. SleepingForest 人工验收。

验收门槛：

- 所有新增专项测试 0 Failed、0 Skipped；
- 完整回归 0 Failed；
- 原基线 82 EditMode / 50 PlayMode 的所有既有用例继续通过；
- 新总数以 Unity Test Runner 实际结果记录，不在实施前虚构；
- Console Error = 0；
- 未取得 Unity 实测前，状态只能是 `IMPLEMENTED — UNITY VERIFICATION REQUIRED`。

## 18. 人工验收方案

在正式 SleepingForest Scene 中分别重新进入 Play Mode 执行独立路线，确保每次 Trial 从 Revision 0 初始化。

### 18.1 正确顺序

1. 进入 Scene，确认 A/B 均 Dormant；
2. 靠近 A，Prompt 正常；按一次交互键；
3. 确认 A Activated、A Prompt 消失、B 仍 Dormant；
4. 靠近 B 并执行；
5. 确认 A/B 均 Activated、Trial Completed；
6. 确认 Root Bridge、路线和 Slice End 没有状态变化。

### 18.2 错序与恢复

1. 重新进入 Scene；
2. 先执行 B，确认 B 显示 Incorrect，A/B 逻辑均未激活；
3. 确认 B 仍可交互，Player 未被锁死；
4. 执行 A，确认 A Activated、B 清除 Incorrect 并回到 Dormant；
5. 再执行 B，确认 Trial 正常 Completed。

### 18.3 重复、隔离与回归

1. 对已激活 Beacon 再按交互键，确认不重复推进、不回到 Dormant；
2. 长按交互键，确认一次按压只产生一次尝试；
3. 验证 A/B 颜色与 Marker 状态互不影响，无 shared Material 污染；
4. 验证 Prompt、WASD Movement、Camera Follow 与 FallRecovery 正常；
5. 验证 Main Route 与 Advantage Route Reserved 仍可物理到达，不新增封锁；
6. 确认 Console Error = 0；
7. Profiler 检查稳定状态无 Beacon 系统持续 GC.Alloc。

## 19. 实施顺序

1. 在 Content 中建立 Slot、Beacon Definition 与 Trial Definition，并完成纯数据校验测试；
2. 在 Puzzle 中建立只读 Runtime State、Snapshot、Phase、Outcome 与 Revision 规则；
3. 实现纯 Trial 状态转换与 source 身份校验，先以 EditMode 锁定全部表格行为；
4. 实现 ForestBeaconInteraction 的 Core 契约适配和可用性映射；
5. 实现基于 MaterialPropertyBlock 与 Marker 的 ForestBeaconVisual；
6. 创建 Definition、Prompt 与 `PF_ForestBeacon` 资产；
7. 在 SleepingForest Scene 显式接入唯一 Trial 与两个 Beacon；
8. 精确更新两项过时的 Sprint003A 测试断言；
9. 增加 Scene/Prefab EditMode 与全链路 PlayMode 测试；
10. 执行专项测试、完整回归与人工验收；
11. 最后更新 Implementation Report、Review Checklist 与 CHANGELOG。

每一步失败时只修复对应层；不得为方便而修改 Foundation。

## 20. 风险与控制措施

| 风险 | 级别 | 控制措施 |
| --- | --- | --- |
| 任意 TargetId 可绕过 Scene 配置推进 Trial | 阻塞 | Trial 同时校验 source 组件引用、Scene、Slot 与 TargetId，不开放纯 ID 写入口 |
| A/B 各自持有状态造成分叉 | 阻塞 | 唯一 Trial 持有真值；Interaction/Visual 只消费 Snapshot |
| IncorrectOrder 被误当成 Core 执行失败 | 高 | Core Result 保持 Success；领域 Outcome 写入 Snapshot |
| Activated Beacon 被再次执行或切回 Dormant | 高 | 禁用检测目标；Executor/Request Port 两层明确失败语义；状态机无逆向边 |
| Revision 因刷新、启停或重放错误递增 | 高 | 只在新领域尝试提交时递增；逐类自动测试锁定 |
| ScriptableObject 被当成运行时存储 | 高 | Definition 不含可变字段；EditMode 验证运行前后资产值一致 |
| A/B 共享材质导致串色或泄漏 | 高 | 独立 MaterialPropertyBlock；禁止 material/sharedMaterial 写入；双实例测试 |
| 003B 意外控制路线或完成 | 阻塞 | Trial 不持有环境引用；PlayMode 显式验证 003A 环境状态不变 |
| 旧 003A 测试被过度放宽 | 高 | 只批准两个指定 Target，并保留 Probe/Foundation/环境边界断言 |
| Greybox Rebuild 清除 003B 内容 | 高 | 接入后禁止运行 Builder；几何重建必须单独审查和重新集成 |

## 21. 明确禁止范围

Sprint003B 不得实现或修改：

- Root Bridge Gameplay；
- 路线开启、封锁、Collider 切换或捷径后果；
- Slice Completion、关卡管理或 Build 入口；
- Puzzle Framework、通用状态机、通用 Gameplay Object 基类；
- Inventory、Crafting、Ability、Quest；
- Network、Host Authority 实现；
- Save/Persistence Framework 或跨 Scene 状态；
- Character Foundation、Player、Input、Movement、GroundDetector；
- Camera Foundation、FallRecovery；
- Interaction Foundation 的契约或生产实现；
- Animator、Tween、音效、粒子、复杂 Shader；
- 全局事件总线、Service Locator、静态 Gameplay Manager。

## 22. Definition of Done

- [ ] Definition、Runtime State、Snapshot 与 Revision 职责分离且无运行时 SO 写入。
- [ ] Scene 中只有一个 ForestSignalTrial，且只接受实际配置的 Beacon A/B source。
- [ ] Beacon A/B TargetId、Prompt、执行组件、视觉组件和 PropertyBlock 相互独立。
- [ ] A → B 能正确 Completed。
- [ ] B → A 产生 IncorrectOrder，且随后 A → B 可恢复并 Completed。
- [ ] 已激活 Beacon 不可重复推进且永不回到 Dormant。
- [ ] Snapshot 足够 003C 只读判断 A、B、IncorrectOrder、Completed 与 Revision。
- [ ] Revision 与实例事件严格遵守本文确定规则。
- [ ] 使用 MaterialPropertyBlock，且至少有 Marker 这一种非颜色反馈。
- [ ] Detector、Prompt、Executor 与 Character Foundation 未修改。
- [ ] 两项过时 Sprint003A 断言已精确更新，其他边界保护保留。
- [ ] Root Bridge、路线和 Slice Completion 未接入 Trial。
- [ ] Sprint003B 专项 EditMode/PlayMode 全部通过。
- [ ] 完整 EditMode/PlayMode 回归 0 Failed。
- [ ] 正式 SleepingForest 人工验收全部通过，Console Error = 0。
- [ ] Implementation Report、Review Checklist 与 CHANGELOG 已更新。

## 23. 最终结论

# READY FOR IMPLEMENTATION

Preflight 已为 GO，且本计划已经锁定状态所有权、来源校验、错序恢复、重复交互、Revision、事件、视觉隔离、Scene 生命周期和测试边界。实施阶段可以按本文顺序推进，无需修改 Character Foundation 或 Interaction Foundation。
