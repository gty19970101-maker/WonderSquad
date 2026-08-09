# Sprint003D Implementation Plan — Sleeping Forest Slice Completion

## 1. Plan 状态

- Sprint：Sprint003D — Sleeping Forest Slice Completion
- Unity 基线：`6000.3.21f1`
- 分支基线：`feature/sprint003-gameplay-vertical-slice`
- 前置 Commit：`e1c15e4 feat(level): complete Sprint003C root bridge consequence`
- Preflight：原结论 `CONDITIONAL GO`，两个条件均已关闭
- 当前 Plan 结论：`READY FOR IMPLEMENTATION`
- 本轮边界：只生成 Implementation Plan；不修改 Runtime、Editor、Tests 或 Unity 资产

---

## 2. Preflight 条件关闭证据

| 条件 | 状态 | 证据 |
|---|---|---|
| Sprint003C 最终收尾与 Gate | `CLOSED` | `Sprint003C_Gate_Review.md` 为 `GO`；Sprint003C 为 `PASSED / GO`；专项 EditMode `8/8`、专项 PlayMode `4/4`、完整 EditMode `108/108`、完整 PlayMode `58/58`，Console Error `0` |
| Sprint003C 稳定 Git 基线 | `CLOSED` | HEAD 与远端 `origin/feature/sprint003-gameplay-vertical-slice` 均指向 `e1c15e4c31c34ef723a8ff7f4770c484e7dd79fc`；Gate Closure 审计时 working tree clean |

Sprint003D Implementation 必须从该 Commit 之后独立审计，不得把新的 003C 修复或范围扩展混入 003D。

---

## 3. Sprint 目标与唯一交付

将已通过的 Sleeping Forest 玩法链闭合为第一个完整本地 Vertical Slice：

```text
Spawn
  ↓
Explore
  ↓
Beacon A → Beacon B
  ↓
ForestSignalTrial Completed
  ↓
Root Bridge Environment Consequence
  ↓
Main Route / Advantage Route
  ↓
Slice End
  ↓
Sleeping Forest Slice Completed
  ↓
明确完成反馈
```

本 Sprint 只新增：

- Scene-local Slice End Trigger；
- Scene-local Completion State；
- Trial + Player Presence 双条件判断；
- 条件不足反馈；
- 完成反馈；
- 安全、幂等的 Scene Authoring；
- EditMode、PlayMode 与人工验收覆盖。

本 Sprint 不新增玩法机关、交互类型、输入或跨场景流程。

---

## 4. 正式 Completion 条件

唯一条件锁定为：

```text
ForestSignalTrial.CurrentSnapshot.IsCompleted == true
AND
SleepingForestSliceEndTrigger.IsPlayerPresent == true
```

判定矩阵：

| Trial Completed | Player at End | Completion |
|---:|---:|---|
| false | false | NotCompleted |
| false | true | NotCompleted；显示 ConditionsUnmet |
| true | false | NotCompleted |
| true | true | Completed |

明确规则：

- 仅 Beacon Trial Completed 不完成整个 Slice；
- 仅进入 Slice End 不完成整个 Slice；
- `RootBridgeAdvantageSpan Activated` 不是 Completion Gate；
- Main Route 与 Advantage Route 均汇入同一个 Trigger；
- 不记录或要求玩家实际使用哪条路线；
- 不要求 E 键，不新增 Input Action。

---

## 5. Completion 状态机

### 5.1 状态

新增内容专用枚举 `SleepingForestCompletionState`：

```text
NotCompleted
      ↓ TryComplete 首次成功
Completed
```

禁止添加 `Failed`、`Rewarding`、`Transitioning`、`Saved` 或通用 Objective 状态。

### 5.2 转换规则

- Scene 实例初始化为 `NotCompleted`；
- 仅当双条件同时满足时转换为 `Completed`；
- Completed 不可逆；
- 重复 Presence、重复 Trial Snapshot、相同 Revision、Disable/Enable 均不重复完成；
- Scene reload 创建新实例并回到 NotCompleted；
- Completion 不 Reload Scene、不传送 Player、不控制路线或输入。

### 5.3 Completion Revision

003D 不复制或创建第二套 Trial Revision。Completion 自身只保留最小 Revision：

- 初始 `CompletionRevision = 0`；
- 唯一一次 NotCompleted → Completed 后变为 `1`；
- 后续永不递增；
- Snapshot 同时记录完成时消费的 `TrialRevisionAtCompletion`，仅用于诊断和测试；
- Trial Revision 的推进规则仍完全属于 003B。

---

## 6. Trial / Trigger 双条件数据流

```text
ForestSignalTrial.CurrentSnapshot ──────┐
ForestSignalTrial.TrialStateChanged ────┤
                                        ├─> SleepingForestCompletionController
SliceEndTrigger.PlayerPresenceChanged ──┤             │
SliceEndTrigger.IsPlayerPresent ─────────┘             │
                                                      │
                                      Completion Snapshot / Instance Events
                                                      │
                                                      v
                              Completion Presenter → View + Marker
```

Completion Controller 是唯一条件判断者：

- Trigger 不读取 Trial、不拥有 Completion；
- Trial 不读取 Trigger、不拥有关卡 Completion；
- Presenter/View 不判断条件、不修改状态；
- Root Bridge Consequence 与 Completion 是两条独立 Trial Consumer 链。

---

## 7. Trigger 内 Trial 完成策略

正式选择：**立即完成**。

当 Player 已位于 Slice End，随后 Trial 从 InProgress/IncorrectOrder 恢复链进入 Completed 时：

```text
PlayerPresence == true
        +
OnTrialStateChanged(Completed Snapshot)
        ↓
EvaluateCompletion()
        ↓
NotCompleted → Completed
```

不要求 Player：

```text
离开 Trigger → 再进入 Trigger
```

理由：

- 避免隐性 Trigger 重入技巧；
- 避免未来多人中队友远端完成 Trial 时产生软锁；
- 事件驱动、易测试、无 Update 轮询；
- 双条件依然同时成立，没有降低完成标准。

当前地图中单玩家通常无法同时站在终点并亲自操作远端 Beacon；该组合必须由自动化测试通过真实 Trial 执行入口模拟。人工验收只记录地图实际可验证部分。

---

## 8. ForestSignalTrial Snapshot / Revision 生命周期

### 8.1 订阅顺序

`SleepingForestCompletionController.OnEnable` 固定执行：

```text
Validate serialized references
        ↓
Subscribe TrialStateChanged
        ↓
Subscribe PlayerPresenceChanged
        ↓
Read Trial.CurrentSnapshot
        ↓
Read Trigger.IsPlayerPresent
        ↓
EvaluateCompletion and Feedback
```

先订阅、再读取当前 Snapshot，避免在初始化边界漏掉状态变化。不能只等待未来事件。

### 8.2 Trial Revision 处理

Controller 只缓存“最近观察到的 Trial Snapshot/Revision”，不拥有其推进权：

- 第一个 Snapshot 无论 Revision 值是多少都必须接受；
- 严格旧于最近观察 Revision 的 Snapshot 忽略；
- 相同 Revision 不重复改变 Completion，但 Presence 变化仍能使用已缓存 Snapshot 重新判断；
- 更新 Revision 替换缓存并重新判断；
- IncorrectOrder Revision 可以被记录，但不完成；
- 未来更高的 Completed Revision 必须正常接受，不能被 IncorrectOrder 消耗；
- Controller 启用时若 `CurrentSnapshot` 已 Completed，必须立即同步，而不是等待下一次 Trial 事件。

### 8.3 生命周期防护

沿用 003C 已验证经验：

- enum 默认值不代表已初始化；
- 使用显式 `hasInitialized` / `hasObservedTrialSnapshot`；
- OnDisable 取消订阅，但不回退已完成状态；
- 重新启用后重新订阅并读取当前 Snapshot；
- 测试 Fixture 必须先完成引用配置再激活，或激活后显式 Configure 并确保订阅，与 Runtime 生命周期一致。

---

## 9. Component 职责

### 9.1 `SleepingForestSliceEndTrigger`

职责：

- 持有正式 `BoxCollider` Trigger 引用；
- 持有 Scene `PlayerSpawner` 显式引用；
- 验证进入/离开的 Collider 是否属于 `PlayerSpawner.SpawnedPlayer`；
- 暴露只读 `IsPlayerPresent`；
- 发布实例事件 `PlayerPresenceChanged`；
- 相同 Presence 不重复发布。

不负责：

- 读取 Trial；
- 判断或保存 Completion；
- UI；
- Interaction；
- 移动、传送、FallRecovery；
- 网络权威。

### 9.2 `SleepingForestCompletionController`

职责：

- 只读消费 `ForestSignalTrialSnapshot`、Revision 与实例事件；
- 只读消费 Trigger Presence；
- 执行双条件判断；
- 持有 Scene-local NotCompleted/Completed；
- 保证不可逆与幂等；
- 暴露只读 Snapshot；
- 发布实例级 `CompletionChanged` 与 `FeedbackStateChanged`。

不负责：

- Beacon/Trial 状态推进；
- Root Bridge 状态；
- Player 控制；
- View Widget；
- Save、Network、Reward 或 Scene Transition。

### 9.3 `SleepingForestCompletionSnapshot`

readonly value，包含：

- `State`；
- `IsCompleted`；
- `CompletionRevision`；
- `TrialRevisionAtCompletion`。

不包含 MonoBehaviour、Renderer、Collider、Canvas 或其他可写 Unity Object 引用。

### 9.4 `SleepingForestCompletionPresenter`

职责：

- 订阅 Controller 的实例事件；
- OnEnable 读取 Controller 当前 Snapshot 与 FeedbackState；
- 构建只读 ViewData；
- 只在 ViewData 变化时 Render；
- 同时驱动屏幕 View 与 World Marker View。

不读取 Player、Trial、Trigger、Bridge 或 Interaction。

### 9.5 `SleepingForestCompletionView`

职责：

- 显示/隐藏一个 Scene-local uGUI ContentRoot；
- 写入一个 Message Text；
- 相同数据不重复刷新；
- OnDisable/Scene reload 安全隐藏。

不查找 Controller，不判断条件，不发 Gameplay 命令。

### 9.6 `SleepingForestCompletionMarkerView`

职责：

- 控制无 Collider 的完成 Marker 显隐；
- 初始隐藏，Completed 后显示；
- 相同状态不重复设置。

不修改现有 Landmark Material 或 Collider。

---

## 10. Player 身份验证

最小可靠方案锁定为 Scene 实例关系：

```text
Serialized PlayerSpawner
        ↓
PlayerSpawner.SpawnedPlayer
        ↓
other.gameObject == SpawnedPlayer
OR
other.transform.IsChildOf(SpawnedPlayer.transform)
```

规则：

- 只认可正式 PlayerSpawner 当前生成的本地 Player；
- 当前 Player Prefab 根 `CharacterController` 是主要 Trigger Collider；
- Beacon、环境 Collider、Marker、Interaction Trigger 均被忽略；
- 不通过 GameObject Name 或硬编码 Tag 识别；
- 不使用 `FindObjectOfType`、static Player 或每帧查找；
- 不新增 Entity Identity Framework；
- `LocalPlayerIdentity` 保持不变，不为 003D 添加新职责。

Trigger 只保存当前接受的 Player Collider/Presence，不缓存全局 Player。若相同 Collider 重复 Enter/Exit，Presence 事件保持幂等。

---

## 11. Scene Hierarchy

正式组合锁定为一个独立 Scene 根，不嵌入 003B Trial 或 003C Consequence 根：

```text
SleepingForestCompletion
├── SleepingForestCompletionController
├── SleepingForestSliceEndTrigger
│   ├── BoxCollider (isTrigger = true)
│   └── SleepingForestSliceEndTrigger component
├── SleepingForestCompletedMarker
│   ├── Renderer only, no Collider
│   └── SleepingForestCompletionMarkerView
└── SleepingForestCompletionCanvas (PF_SleepingForestCompletionFeedback instance)
    ├── SleepingForestCompletionPresenter
    ├── SleepingForestCompletionView
    └── ContentRoot
        ├── Background
        └── MessageText
```

Scene 根上的 `SleepingForestCompletionController` 通过 Inspector 显式引用 Trial 与 Trigger。Presenter 显式引用 Controller、View 和 MarkerView。

独立根的原因：

- 003B/003C Authoring 重复 Apply 不会删除 003D；
- 003D Authoring 不需要改写 Trial 或 Consequence 根；
- Scene 唯一性测试容易定位；
- 不形成 Generic Level Controller。

---

## 12. Slice End Greybox 迁移

现有对象全部保留：

- `SliceEndGround`：继续作为唯一地面承载面；
- `SliceEndLandmark_Tower`：保留视觉和现有物理 Landmark；
- `SliceEndLandmark_Arch`：保留终点视觉结构；
- `EndAreaLight`：保持不变；
- Main/Advantage 接入口：保持不变。

不把 Tower、Arch 或 Ground 直接升级为 Trigger，避免混合承载、视觉与完成职责。新增独立无 Renderer `SleepingForestSliceEndTrigger`。

推荐 Trigger 配置：

| 配置 | 值 |
|---|---|
| Collider | `BoxCollider` |
| isTrigger | `true` |
| World Position | `(0, 1.5, 54.75)` |
| Size | `(16, 3, 2.5)` |
| Bounds | X `[-8, 8]`、Y `[0, 3]`、Z `[53.5, 56]` |

当前 `SliceEndGround` Bounds 为 X `[-10,10]`、Y `[-1,0]`、Z `[43,57]`。Trigger 位于安全承载面内，与 Ground 只在 Y = 0 边界接触，不形成正体积双层 Collider；其 Z 范围位于 Tower Collider 后侧，并与 Arch 分离。

若 Unity Editor 实测 Collider Bounds 与静态推导不同，只调整 Trigger Position/Size，不移动或缩放 003A/003C 对象。

---

## 13. Physics 安全规则

- Trigger `isTrigger = true`，不阻挡 CharacterController；
- Trigger 不带 Renderer，不产生 Z-fighting；
- Trigger 不承担 Ground，不与 `SliceEndGround` 形成正体积重叠；
- Completed Marker 无 Collider，不形成第二层地面；
- Trigger 与 Tower/Arch Collider 不形成正体积重叠；
- 不修改 PlayerMovement、GroundDetector 或 CharacterController 参数；
- 不修改 Camera；
- 不修改 `SleepingForestFallRecovery`、RecoveryPoint 或 fallYThreshold；
- 玩家在 End 附近掉落时由既有 FallRecovery 恢复，之后可重新进入 End；
- Completed 后掉落不回退 Completion State。

Editor 测试验证 Bounds 和配置，人工走图验证卡顿、接地抖动与边缘跌落体验。

---

## 14. Requirement Feedback

### 14.1 状态

新增 `SleepingForestCompletionFeedbackState`：

- `Hidden`
- `ConditionsUnmet`
- `Completed`

它只控制表现，不是第二套 Gameplay 状态机。

### 14.2 生命周期

```text
Scene enter
  → Hidden

Player enters End + Trial not completed
  → ConditionsUnmet

Player exits before Trial completion
  → Hidden

Player remains inside + Trial becomes completed
  → Completed

Trial completed first + Player enters End
  → Completed
```

条件不足文案锁定为 fallback：

```text
Forest signal incomplete
```

并保留稳定文本 key：

```text
sleeping_forest.completion.requirement_incomplete
```

本 Sprint 不接入完整 Localization Service。

Requirement Feedback 自动出现，不要求 E 键；离开 Trigger 即隐藏。它不使用 `InteractionPromptPresenter`、不显示 Binding、不创建新的 Prompt Framework。

---

## 15. Completion Feedback

完成文案锁定为 fallback：

```text
Sleeping Forest Complete
```

稳定文本 key：

```text
sleeping_forest.completion.complete
```

规则：

- Scene 初始隐藏；
- Completed 后显示并在当前 Scene 实例内保持；
- Completed Marker 同时显示；
- 重复 Trigger/Snapshot 不重新创建 Canvas、不重复 Render；
- 不暂停游戏、不阻挡 WASD/Interaction；
- Canvas Graphic 的 `raycastTarget = false`；
- Scene reload 后新实例重新隐藏；
- 不实现计时消失、动画、Tween、音效、奖励或下一关按钮。

“只显示一次”定义为：状态和 ViewData 只转换一次，而不是销毁 UI 后再次实例化。

---

## 16. UI Prefab

新增 `PF_SleepingForestCompletionFeedback`：

- 使用现有 uGUI `2.0.0`；
- Screen Space Overlay Canvas；
- 一个默认 inactive 的 ContentRoot；
- 一个占位背景 Image；
- 一个 `UnityEngine.UI.Text`；
- 不含 EventSystem、InputModule、Button、Animator 或 AudioSource；
- Presenter/View 引用在 Prefab 内部序列化；
- Scene-specific Controller 与 MarkerView 引用由 Authoring 在 Prefab 实例上持久化。

不修改现有 `PF_InteractionPrompt` 或 `InteractionPromptCanvas`。

---

## 17. 003C 独立边界

003D 不修改：

- `SleepingForestRootBridgeConsequence`；
- `RootBridgeAdvantageSpan`；
- `RootBridgeAdvantageVisual`；
- `RootBridgeTemporaryCrossing`；
- 003C Revision 过滤逻辑；
- Main/Advantage Route 几何。

003D 与 003C 分别只读消费 Trial：

```text
Trial Event ──> 003C Consequence ──> Bridge Environment
          └──> 003D Completion + End Presence ──> Slice Completed
```

PlayMode 与人工验收必须确认 A → B 后 003C 环境后果仍正常，但 Bridge Activated 不参与 Completion 条件表达式。

---

## 18. Scene Authoring 方案

### 18.1 菜单入口

```text
Wonder Squad > Sprint003D > Apply Sleeping Forest Completion
```

### 18.2 安全流程

1. Play Mode / Play Mode transition 中拒绝执行；
2. 显示明确 Apply/Cancel 对话框；
3. 调用 Unity 官方 modified scenes 保存确认；用户取消立即停止且无资产修改；
4. 创建或更新 UI Prefab；
5. 以 Single 模式打开固定 SleepingForest Scene Asset Path；
6. 查找唯一 `SleepingForestCompletion` 根；发现重复根/Controller/Trigger 时报告错误并停止；
7. 新建根时保持 inactive；更新既有根时先安全 inactive；
8. 创建/复用 Controller、Trigger、Marker、Canvas Prefab 实例；
9. 使用 `SerializedObject`/`SerializedProperty` 写入所有 Scene 引用；
10. 每组写入后调用 `ApplyModifiedProperties`；
11. 验证 PlayerSpawner、Trial、Trigger Collider、Controller、Presenter、View、Marker 引用完整；
12. 完成所有序列化配置后再激活根；
13. `MarkSceneDirty`、`SaveScene`、`SaveAssets`；
14. 仅在保存成功后报告完成。

### 18.3 003B 生命周期缺陷防护

不得在 active 根上先 AddComponent、触发 OnEnable，再补序列化引用。固定顺序为：

```text
inactive root
  → create/reuse components
  → assign SerializedProperties
  → ApplyModifiedProperties
  → validate references
  → activate root
```

### 18.4 幂等规则

- 重复 Apply 不产生第二个 Controller、Trigger、Canvas 或 Marker；
- 更新既有对象而不是删除整个 Scene 内容；
- 不触碰 003B/003C 根；
- 不运行 003A Greybox Rebuild；
- 不自动执行；
- 不手工编辑 Scene YAML；
- 不修改 Sandbox/Test Scene、Build Profile 或 ProjectSettings。

---

## 19. 文件清单

### 19.1 新增 Runtime — `WonderSquad.SleepingForest`

- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/WonderSquad.SleepingForest.asmdef`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestCompletionState.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestCompletionFeedbackState.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestCompletionSnapshot.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestSliceEndTrigger.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestCompletionController.cs`

### 19.2 新增 UI — `WonderSquad.UI`

- `Client/Assets/WonderSquad/Runtime/UI/SleepingForest/SleepingForestCompletionViewData.cs`
- `Client/Assets/WonderSquad/Runtime/UI/SleepingForest/SleepingForestCompletionPresenter.cs`
- `Client/Assets/WonderSquad/Runtime/UI/SleepingForest/SleepingForestCompletionView.cs`
- `Client/Assets/WonderSquad/Runtime/UI/SleepingForest/SleepingForestCompletionMarkerView.cs`

### 19.3 新增 Editor/Prefab/Tests

- `Client/Assets/WonderSquad/Editor/SleepingForestCompletionAuthoring.cs`
- `Client/Assets/WonderSquad/Prefabs/UI/PF_SleepingForestCompletionFeedback.prefab`
- `Client/Assets/WonderSquad/Tests/EditMode/SleepingForestCompletionEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/SleepingForestCompletionPlayModeTests.cs`

Unity `.meta` 文件随新增目录和资产一并生成/保留。

### 19.4 修改文件

- `Client/Assets/WonderSquad/Runtime/UI/WonderSquad.UI.asmdef`
- `Client/Assets/WonderSquad/Editor/WonderSquad.Editor.asmdef`
- `Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef`
- `Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef`
- `Client/Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity`
- `SleepingForestGreyboxEditModeTests.cs` 中过时的 Completion 禁止断言
- `ForestBeaconConfigurationEditModeTests.cs` 中过时的 Completion 禁止断言
- `ForestSignalTrialPlayModeTests.cs` 中过时的 Completion 禁止断言
- `CHANGELOG.md`
- `Docs/01_Project/Sprint003D_Implementation_Report.md`
- `Docs/01_Project/Sprint003D_Review_Checklist.md`

不新增 ScriptableObject、Input Action、Package 或 ProjectSettings 文件。

---

## 20. asmdef 关系

### 20.1 新内容程序集

`WonderSquad.SleepingForest` 依赖：

- `WonderSquad.Core`
- `WonderSquad.Player`
- `WonderSquad.Puzzle`

它不依赖：

- `WonderSquad.UI`
- `WonderSquad.Interaction`
- `WonderSquad.Camera`
- `WonderSquad.Editor`
- `WonderSquad.Network`

### 20.2 Presentation

`WonderSquad.UI` 增加单向引用：

- `WonderSquad.SleepingForest`

Gameplay/Content 不反向引用 UI。

### 20.3 Editor 与 Tests

- `WonderSquad.Editor` 增加 `WonderSquad.SleepingForest`、`WonderSquad.Player`、`WonderSquad.UI`；
- EditMode/PlayMode Tests 增加 `WonderSquad.SleepingForest`；
- Runtime 不引用 `UnityEditor`；
- PlayMode Tests 继续用 Editor Scene Asset Path API 加载未进入 Build Profile 的 SleepingForest。

### 20.4 为什么需要新程序集

- Puzzle 不应拥有整关完成；
- Greybox 是临时 003A 边界；
- 通用 `WonderSquad.Level` 当前只依赖 Core/Content，不应为单个关卡直接引用 Player/Puzzle 实现；
- `WonderSquad.SleepingForest` 是具体关卡的组合根，不是 `WonderSquad.LevelFramework`；
- Player/Puzzle 不反向引用它，因此依赖图无环。

---

## 21. 过时回归断言调整

不得删除现有场景边界测试。只精确调整 003D 后过时的“无 Completion Gameplay”断言：

- 要求且只允许一个 `SleepingForestCompletionController`；
- 要求且只允许一个 `SleepingForestSliceEndTrigger`；
- 继续禁止名为通用 `CompletionGameplay` 的未批准根；
- 继续禁止 Sandbox Probe、`InteractionExecutionProbe`、InteractionTestTarget 与 P0 对象；
- Scene `InteractionTarget` 仍只能是 Beacon A/B 两个；Slice End 不实现 IInteractable；
- 继续验证一个 Trial、一个 Root Bridge Consequence、一个 AdvantageSpan；
- 继续验证 Main Route 与 `RootBridgeTemporaryCrossing` 未改变；
- Foundation 依赖和核心契约断言保持。

测试名称如已表达“尚无 Completion”，必须同步重命名以描述新的批准边界，不能让名称与断言语义冲突。

---

## 22. Soft-lock 分析

| 情况 | 处理 | 软锁结论 |
|---|---|---|
| Trial 未完成直接进入 End | NotCompleted + ConditionsUnmet | 可离开并返回 Beacon |
| 离开 End，完成 A → B，再返回 | Enter 时读取已 Completed Snapshot 并完成 | 无需 Reload |
| Player 在 End 内时 Trial Completed | Trial Event 立即重新判断并完成 | 无需 Trigger 重入 |
| IncorrectOrder | 不完成；Main Route 不变 | A → B 后仍可继续 |
| 重复进入 End | Completed 不变，事件/UI 不重复 | 幂等 |
| 相同 Trial Revision 重放 | 不重复完成 | 幂等 |
| End 附近掉落 | Trigger Presence 清除，FallRecovery 恢复 | 可重新进入 |
| Completed 后掉落 | Completion 保持 Completed | 不回退 |
| Main Route 到 End | 同一 Trigger、同一条件 | 可完成 |
| Advantage Route 到 End | 同一 Trigger、同一条件 | 可完成 |
| Scene reload | 新 Scene 实例回到 NotCompleted | 无 static 污染 |

---

## 23. EditMode 测试计划

至少覆盖以下行为，不预设最终测试数量：

1. 初始 Completion 为 NotCompleted、Revision 0；
2. Trial 未完成 + PlayerAtEnd 不完成；
3. Trial Completed + Player 不在 End 不完成；
4. Trial Completed + PlayerAtEnd 完成；
5. Player 先在 End，Trial 后 Completed 立即完成；
6. Trial 先 Completed，Player 后进入 End 完成；
7. Completed 只发生一次，事件只发布一次；
8. 重复 Enter/Exit 幂等；
9. 相同/旧 Trial Revision 幂等；
10. 首次读取已 Completed Snapshot 不漏状态；
11. IncorrectOrder 不完成；
12. IncorrectOrder 后真实 A → B Completed 可以完成；
13. Trigger 只认可 `PlayerSpawner.SpawnedPlayer`；
14. 非 Player、Beacon、环境 Collider 不触发；
15. Trigger BoxCollider、isTrigger、Bounds 和 Scene 引用有效；
16. Trigger 与 Ground/Tower/Arch 无正体积 Collider 重叠；
17. Completion Snapshot 只读且 Trial/Completion Revision 含义正确；
18. Completion/Presence/Feedback Event 非 static；
19. View 初始隐藏；
20. ConditionsUnmet 显示且 Exit 隐藏；
21. Completed 后 View/Marker 显示；
22. 相同 ViewData 不重复刷新；
23. UI Graphic 不拦截 Raycast；
24. Scene 序列化引用完整；
25. Scene 只有一个 Controller、Trigger、Canvas、Marker；
26. Authoring 重复 Apply 不产生第二套对象；
27. Main/Advantage 共享同一条件，Controller 不引用路线或 Span；
28. Foundation 契约、源文件与 asmdef 方向保持。

测试 Fixture 必须模拟真实生命周期：先完成序列化/Configure，再激活并订阅；不得直接调用内部 Complete 或绕过 Trial 状态机。

---

## 24. PlayMode 测试计划

至少覆盖以下行为，不预设最终专项数量：

1. SleepingForest 通过 Editor PlayMode Scene Asset Path 成功加载；
2. Player 正常 Spawn 且只有一个；
3. Main Camera 正常且只有一个；
4. 未完成 Trial 到 End 不完成；
5. Requirement Feedback 自动出现；
6. Player 可离开 End，反馈隐藏且 Movement 不受影响；
7. 通过现有 Interaction Execution 完成 A → B；
8. 返回 End 后 Slice Completed；
9. Completion Feedback 与 Marker 显示；
10. Player 已在 End 时通过真实 Trial 入口完成 A → B，立即 Completion；
11. Main Route 可进入同一 End Trigger 并完成；
12. Advantage Route 可进入同一 End Trigger 并完成；
13. B → IncorrectOrder → A → B → End 可完成；
14. RootBridgeAdvantageSpan 正常激活回归；
15. Completion Event/Revision 只提交一次；
16. 重复进入 End 幂等，不重复创建 UI；
17. FallRecovery 后仍可完成；
18. Completed 后 FallRecovery 不回退状态；
19. PlayerMovement 正常；
20. GroundDetector 正常；
21. Camera Follow 正常；
22. Detection、Prompt、Execution 正常，End 不显示 Interaction Prompt；
23. Scene reload 后 Completion 与 Feedback 重置；
24. Console Error = `0`。

场景测试结束必须卸载/清理 Scene，避免污染后续 PlayMode Tests。

---

## 25. 完整回归基线

Implementation 前基线：

| 范围 | Passed | Failed | Skipped |
|---|---:|---:|---:|
| 完整 EditMode | 108 | 0 | 0 |
| 完整 PlayMode | 58 | 0 | 0 |
| Console Error | 0 | — | — |

Implementation 后必须运行：

- Sprint003D 专项 EditMode；
- Sprint003D 专项 PlayMode；
- 完整 EditMode；
- 完整 PlayMode；
- 正式 SleepingForest 人工验收。

最终数量以 Unity Test Runner 实际发现数量为准，不在 Plan 中预设。

---

## 26. 人工验收计划

### 流程 A：Main Route

```text
A → B → Main Route → Slice End → Completed
```

### 流程 B：Advantage Route

```text
A → B → Advantage Route → Slice End → Completed
```

### 流程 C：提前到终点

```text
不完成 Trial → Slice End → ConditionsUnmet
→ 离开 → A → B → 返回 End → Completed
```

### 流程 D：Trigger 内 Trial 完成

```text
先进入 Slice End 并停留
→ Trial Completed
→ 不要求重入 → 立即 Completion
```

当前地图若无法由同一个本地玩家同时站在 End 并操作远端 Beacon，则人工验收记录该空间限制，并以使用真实 Trial 执行入口的自动化测试作为该状态组合的正式证据。

### 流程 E：IncorrectOrder

```text
B → IncorrectOrder → A → B → End → Completed
```

### 流程 F：FallRecovery

```text
故意 Fall → Recovery → 继续 Trial/路线 → Completed
```

Completed 后再触发一次 FallRecovery，确认状态不回退。

### 流程 G：重复触发

```text
Completed → 离开 End → 反复进入 → 不重复完成
```

每个流程同时确认：

- Movement、GroundDetector、Camera、Interaction 正常；
- Main Route 永久可用；
- Root Bridge Consequence 正常；
- Trigger 不阻挡 CharacterController；
- End 区域无 Z-fighting、卡顿或接地抖动；
- UI 不阻挡输入；
- 单 Player、单 Main Camera；
- Console Error = `0`；
- 稳定帧无 Completion 持续 GC.Alloc。

---

## 27. Sprint003D 明确禁止边界

禁止新增：

- Reward、Quest、Mission、Achievement；
- Save/Persistence；
- Network/Host Authority 实现；
- NPC、Dialogue；
- 新 Puzzle、Beacon、Door、Lever；
- Ability、Inventory、Crafting；
- Level Select、下一关、Scene Transition；
- Audio、Animation、Tween、VFX Framework；
- 通用 Level、Objective 或 Completion Framework；
- 新 Input Action 或 Completion Input Reader；
- UI Framework 重构。

禁止修改：

- Character Foundation；
- Interaction Foundation；
- Camera Foundation；
- `ForestSignalTrial` 核心状态机与 Beacon A/B；
- `SleepingForestRootBridgeConsequence`、AdvantageSpan 与 TemporaryCrossing 核心逻辑；
- PlayerMovement、GroundDetector、FallRecovery；
- Main/Advantage Route 几何；
- Package、ProjectSettings、Build Profile。

---

## 28. 实施顺序

1. 新建 `WonderSquad.SleepingForest` asmdef 和只读 state/snapshot；
2. 实现 SliceEndTrigger 的 Player Presence；
3. 实现 CompletionController 的双条件、Revision 与生命周期；
4. 完成纯状态与 Trigger EditMode 测试；
5. 实现 ViewData、Presenter、View、MarkerView；
6. 创建 UI Prefab；
7. 实现安全、幂等 Authoring；
8. 显式 Apply 到 SleepingForest；
9. 精确调整 003A/B 过时边界测试；
10. 完成 Scene/Bounds/EditMode 测试；
11. 完成 PlayMode 集成测试；
12. 运行专项与完整回归；
13. 执行正式 Scene 人工验收；
14. 更新 Implementation Report、Review Checklist 与 CHANGELOG；
15. 等待 Unity 实测通过后再进行 Sprint003D Gate Review。

每一步只在前一步编译和局部测试通过后继续，不并行修改 Foundation。

---

## 29. Definition of Done

- [ ] 双条件是唯一 Completion Gate；
- [ ] 仅 Trial 或仅 End Presence 均不完成；
- [ ] Player 在 End 内时 Trial Completed 会立即完成；
- [ ] Completion State Scene-local、非 static、不可逆且幂等；
- [ ] Trial Snapshot/Revision/Instance Event 只读边界保持；
- [ ] IncorrectOrder 不完成，A → B 后可恢复；
- [ ] Trigger 只识别正式 `PlayerSpawner.SpawnedPlayer`；
- [ ] Trigger 不阻挡 CharacterController，不产生双层 Ground 或 Z-fighting；
- [ ] ConditionsUnmet 反馈自动显示并在离开时隐藏；
- [ ] Completed View/Marker 明确显示且不重复创建；
- [ ] Main 与 Advantage Route 均可完成；
- [ ] 003C 环境后果正常且不是 Completion Gate；
- [ ] FallRecovery、Movement、GroundDetector、Camera、Interaction 回归通过；
- [ ] Authoring 手动、确认式、幂等，序列化引用完整；
- [ ] Scene 只有一套正式 Completion 组合；
- [ ] Foundation、Input、Package、ProjectSettings 未修改；
- [ ] Sprint003D 专项 EditMode/PlayMode 通过；
- [ ] 完整 EditMode/PlayMode 回归通过；
- [ ] 正式 SleepingForest 人工验收通过；
- [ ] Console Error = `0`；
- [ ] Implementation Report、Review Checklist、CHANGELOG 完成。

---

## 30. 最终结论

# READY FOR IMPLEMENTATION

Sprint003D 的 Completion 条件、状态机、Trigger、Player 身份、Trial Revision 生命周期、UI 反馈、Scene Authoring、程序集依赖、物理安全、测试与人工验收方案均已锁定。下一步可以按本 Plan 实施，但本轮不得开始 Runtime、Editor、Tests 或 Unity 资产修改。
