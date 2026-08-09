# Sprint003D Preflight Report — Sleeping Forest Slice Completion

## 1. Preflight 结论

# CONDITIONAL GO

Sprint003D 的玩法条件、场景空间、只读状态输入和表现边界均可在不修改 Sprint001/002 Foundation、Forest Signal Trial 核心状态机及 Root Bridge Consequence 核心行为的前提下实现。当前没有玩法或 Unity 技术阻塞。

进入 Implementation 前仍需关闭两项项目治理条件：

1. 仓库中的 Sprint003C 文档尚未回填用户提供的最终 Unity 结果：`Sprint003C_Implementation_Report.md` 与 `Sprint003C_Review_Checklist.md` 仍标记 `IMPLEMENTED — UNITY VERIFICATION REQUIRED`，`CHANGELOG.md` 仍写“Unity 验证待执行”，且当前不存在 `Sprint003C_Gate_Review.md`；
2. 当前工作区保留 Sprint003C Scene、Runtime、Editor、Prefab、Tests 与文档的未提交改动。开始 003D Implementation 前必须先形成可追踪的 003C 稳定 Git 基线，避免无法区分 003C 与 003D 的变更范围。

上述条件只涉及稳定基线与治理记录，不要求修改 Sprint003C 玩法实现。本轮未运行 Unity Test Runner，测试基线采用用户提供的 Unity `6000.3.21f1` 实测结果。

---

## 2. 审计范围与稳定基线

### 2.1 已确认基线

| 模块 | 状态 |
|---|---|
| Sprint001 Character Foundation | `PASSED / GO` |
| Sprint002 Interaction Foundation | `PASSED / GO` |
| Sprint003A Sleeping Forest Greybox | `PASSED / GO` |
| Sprint003B Forest Signal Trial | `PASSED / GO` |
| Sprint003C Root Bridge Consequence | 用户确认 `PASSED / GO`，仓库收尾文档待同步 |

用户提供的最新 Unity 实测基线：

- Sprint003C EditMode：`8 Passed / 0 Failed / 0 Skipped`
- Sprint003C PlayMode：`4 Passed / 0 Failed / 0 Skipped`
- 完整 EditMode：`108 Passed / 0 Failed / 0 Skipped`
- 完整 PlayMode：`58 Passed / 0 Failed / 0 Skipped`
- Console Error：`0`

### 2.2 本轮只读审计内容

已检查：

- `Sprint003D_Discovery_Report.md`；
- `ForestSignalTrial`、`ForestSignalTrialSnapshot` 的公开读取与实例事件；
- `SleepingForestRootBridgeConsequence` 的 003C 派生链；
- `PlayerSpawner`、`LocalPlayerIdentity`、Player Prefab 的本地玩家身份边界；
- `SleepingForestFallRecovery` 的玩家绑定与恢复方式；
- `SliceEndGround`、终点 Landmark 及相关 Collider 的 Scene/Builder 配置；
- `WonderSquad.Player`、`WonderSquad.Puzzle`、`WonderSquad.Level`、`WonderSquad.SleepingForest.Greybox`、`WonderSquad.UI` 与 Tests 的 asmdef；
- Sprint003A/B/C 的场景边界与回归断言；
- 现有显式 Scene Authoring 菜单的安全模式。

本轮没有修改 Runtime、Editor、Tests 或 Unity 资产。

---

## 3. Discovery 结论复核

Discovery 方案可执行，正式 Completion 条件继续锁定为：

```text
ForestSignalTrial.CurrentSnapshot.IsCompleted
AND
CurrentLocalPlayerInsideSliceEndVolume
```

只有两个条件同时满足，`SleepingForest Slice Completion` 才允许从 `NotCompleted` 转为 `Completed`。

明确禁止：

- 仅完成 Beacon A → B 就自动完成整个 Slice；
- 未完成 Trial 时仅进入终点就完成；
- 把 `RootBridgeAdvantageSpan Activated` 增加为第三个强制条件；
- 要求玩家按 E 或新增 Completion Input Action；
- 要求玩家必须走 Advantage Route。

Main Route 与 Advantage Route 是两条空间路线，最终共享同一个 Trigger 和同一组逻辑条件。

---

## 4. 核心玩家流程

```text
Spawn
  ↓
Explore
  ↓
Beacon A → Beacon B
  ↓
ForestSignalTrial Completed
  ↓
Sprint003C Root Bridge Environment Consequence
  ↓
Main Route / Advantage Route
  ↓
Slice End Trigger
  ↓
SleepingForest Completion Completed
  ↓
一次性完成反馈
```

Completion 是 Trigger-based Completion，不是 Interaction-based Completion。Slice End 不实现 `IInteractable`、不显示 Interaction Prompt、不调用 `InteractionExecutor`，也不要求玩家按键确认。

---

## 5. Trigger 内 Trial 变为 Completed 的策略

选择方案 A：**玩家位于 Slice End Trigger 内时，Trial 一旦变为 Completed，立即完成 Slice。**

原因：

- 不依赖玩家理解“必须退出再进入”的隐藏技巧；
- 不会因为 Trigger 没有重新产生 `OnTriggerEnter` 而软锁；
- 当前单机中行为确定，未来多人时也自然支持队友在远端完成 Trial；
- 只需复用 Trial 实例事件，无 Update 轮询；
- 条件仍严格保持为“Player at Slice End + Trial Completed”，没有降低完成要求。

因此 Completion Controller 必须在两类事件上评估条件：

1. Slice End 的本地玩家 Presence 变化；
2. `ForestSignalTrial.TrialStateChanged`。

组件启用时还需读取一次 `CurrentSnapshot` 与 Volume 当前 Presence，以覆盖初始化先后顺序。

---

## 6. Slice End Trigger 设计

### 6.1 当前 Scene 空间审计

当前已序列化对象：

| 对象 | Position | Scale / Bounds 说明 |
|---|---:|---|
| `SliceEndGround` | `(0, -0.5, 50)` | Scale `(20, 1, 14)`；承载 Bounds 为 X `[-10, 10]`、Y `[-1, 0]`、Z `[43, 57]` |
| `SliceEndLandmark_Tower` | `(0, 5, 50)` | Scale `(5, 5, 5)`；现有 CapsuleCollider 约占 Z `[47.5, 52.5]`，实际 Bounds 需由 Editor 测试确认 |
| `SliceEndLandmark_Arch` | `(0, 5, 46.5)` | Scale `(10, 1.5, 1.5)`；Collider 位于玩家头顶区域 |
| `RootBridgeAdvantageExitJunction` | Scene 已存在 | Advantage Route 与 End Ground 的现有接入口，不得由 003D 改造 |

### 6.2 推荐 Volume 配置

正式对象建议命名 `SleepingForestSliceEndVolume`，初始配置锁定为：

- GameObject：独立、无 Renderer 的 Scene 子对象；
- Collider：`BoxCollider`；
- `isTrigger = true`；
- World Center：约 `(0, 1.5, 54.75)`；
- Size：约 `(16, 3, 2.5)`；
- 预期 Bounds：X `[-8, 8]`、Y `[0, 3]`、Z `[53.5, 56]`。

该位置具备以下性质：

- 完全位于 `SliceEndGround` 平面范围内，远端仍保留约 1 米地面余量；
- 与 Ground 仅在 Y = 0 边界接触，不形成正体积双层 Collider；
- 位于 Tower 现有 Collider 之后，预期保留约 1 米间隔；
- 不与 Arch Collider 形成正体积重叠；
- X 宽度允许玩家从 Tower 两侧进入，不形成狭窄夹缝；
- Main 与 Advantage 两条路线均进入同一 Volume；
- Trigger 不提供物理承载，不阻挡 CharacterController。

最终 Position/Size 必须在 Implementation 的 Editor Bounds 测试和 Unity Scene 视图中确认；若实测 Tower Collider Bounds 与当前静态推导不同，只允许调整 Volume 自身，不移动 003A/003C 几何。

### 6.3 Player 识别方式

采用显式 Scene 实例关系：

- Volume 序列化引用当前 `PlayerSpawner`；
- 通过 `PlayerSpawner.SpawnedPlayer` 获取当前唯一离线玩家实例；
- `OnTriggerEnter/Exit` 只接受该 Player 根对象的 Collider，或属于该根对象层级的 Collider；
- 不使用 GameObject Name、Tag、`FindObjectOfType`、static Player 或每帧搜索；
- `LocalPlayerIdentity` 继续作为 Player 既有身份组件，但 003D 不修改它，也不建立通用 Entity 系统；
- 当前 Player Prefab 只有一个根 `CharacterController` 作为主要 Trigger 参与者，测试仍需防止非玩家 Collider 误触发。

此处对 `PlayerSpawner` 的引用只服务当前本地 Vertical Slice。未来 Network Spawn 接入时，应替换“当前本地参与者来源”，而不是修改 Completion 条件或让 Trigger 成为网络权威。

### 6.4 Presence 生命周期

- Enter：若 Collider 属于当前 `SpawnedPlayer`，设置 `IsPlayerInside = true` 并发布实例级 Presence 变化；
- Exit：只有匹配当前已接受 Player Collider 时才清除 Presence；
- Disable：清理临时 Presence，并安全通知 Controller 隐藏 NotReady 提示；
- 重复 Enter/Exit：相同状态不重复发事件；
- Volume 不直接完成关卡、不显示 UI、不读取 Trial。

---

## 7. Completion State 设计

### 7.1 状态拥有者

建议正式组件：`SleepingForestSliceCompletion`。

它是 Scene 实例级、《沉睡森林》专用的内容编排组件，不是全局 Level Manager。其显式引用为：

- 一个 `ForestSignalTrial`；
- 一个 `SleepingForestSliceEndVolume`。

它不引用 PlayerMovement、Camera、Interaction、FallRecovery、RootBridgeAdvantageSpan 或具体 UI View。

### 7.2 最小状态与 Revision

```text
NotCompleted / Revision 0
          ↓ 首次同时满足两项条件
Completed    / Revision 1
```

规则：

- 只允许一次 `NotCompleted → Completed`；
- Completed 永不在当前 Scene 实例内回退；
- 重复 Trigger、重复 Trial Revision、组件 Disable/Enable 不增加 Completion Revision；
- Completion Revision 只在正式完成转换时增加，不因 ConditionUnmet 提示变化增加；
- Scene reload 创建新实例并恢复 `NotCompleted / Revision 0`；
- 不写入 ScriptableObject，不跨 Scene 保存。

### 7.3 只读 Snapshot

建议只读 `SleepingForestSliceCompletionSnapshot` 至少包含：

- Completion State；
- `IsCompleted`；
- Completion Revision；
- 提交完成时消费的 Trial Revision。

Snapshot 为 readonly value，不暴露 setter，不允许 Presentation 反向修改状态。

### 7.4 实例事件

允许两个局部事件边界：

- `CompletionChanged(Snapshot)`：仅首次完成时触发一次；
- `FeedbackStateChanged(FeedbackState)`：只在 `Hidden / ConditionsUnmet / Completed` 真正变化时触发。

二者均为 Scene 实例事件：

- 非 static；
- 无订阅者时安全；
- OnEnable 订阅、OnDisable 取消订阅；
- 不使用全局 Event Bus；
- 不在 Update 中发布或轮询。

---

## 8. Trial 只读消费方式

现有 003B 已提供完整公开边界：

- `ForestSignalTrial.CurrentSnapshot` 会确保 Scene Trial 初始化；
- `ForestSignalTrialSnapshot.IsCompleted` 表达正式试炼完成；
- `ForestSignalTrialSnapshot.Revision` 表达 Trial 状态版本；
- `ForestSignalTrial.TrialStateChanged` 是实例级事件。

003D 使用方式：

```text
OnEnable
  → Subscribe TrialStateChanged
  → Read CurrentSnapshot
  → Evaluate(CurrentSnapshot, Volume.IsPlayerInside)

OnTrialStateChanged(snapshot)
  → Evaluate(snapshot, Volume.IsPlayerInside)
```

禁止：

- 修改 `ForestSignalTrial` 或 Beacon A/B；
- 读取 Trial 私有字段；
- 重写 A → B / IncorrectOrder 状态机；
- 复制 Trial 状态；
- 把 Trial Definition 当作运行时状态；
- 使用 static 状态或 Global Event Bus。

IncorrectOrder Snapshot 不是 Completed，因此不会完成 Slice。玩家随后 A → B 产生新的 Completed Revision 后，003D 正常消费并继续完成。

---

## 9. 与 Sprint003C 的关系

正式 Completion 条件保持：

```text
Trial Completed + Player at Slice End
```

不变成：

```text
Trial Completed + Bridge Activated + Player at Slice End
```

`SleepingForestRootBridgeConsequence` 与 Completion Controller 分别订阅同一个 Trial 实例事件，形成两条独立的只读派生链：

```text
Trial Snapshot/Event
  ├─> RootBridgeConsequence → AdvantageSpan
  └─> SliceCompletion + End Presence → Completed
```

003D 不调用 `RootBridgeAdvantageSpan.ApplyState`，不读取其私有状态，也不改变 003C Revision。自动化与人工验收会确认 003C 环境后果仍正常存在；它是集成回归项，不是第二个 Completion Gate。

这样既避免重复表达同一因果，也避免 003C Scene 配置错误把玩家永久锁在未完成状态。

---

## 10. Completion Feedback 方案

### 10.1 选定方案

采用一个独立 Scene-local uGUI Feedback Prefab，加一个无 Collider 的完成 Marker：

- `PF_SleepingForestCompletionFeedback`：Screen Space Overlay Canvas；
- `SleepingForestCompletionPresenter`：只读消费 Completion Snapshot/FeedbackState；
- `SleepingForestCompletionView`：只负责 Panel 与 Text 显示；
- `SleepingForestCompletedMarker`：完成前隐藏，Completed 后显示；
- 不修改或复用 Interaction Prompt 的状态链；
- 不创建第二套交互 Prompt，不把 Slice End 伪装成 `IInteractable`。

### 10.2 View 状态

| 状态 | 显示 |
|---|---|
| `Hidden` | Panel 隐藏，Text 为空，Completed Marker 隐藏 |
| `ConditionsUnmet` | 显示最小提示，例如“森林信号尚未完成”；离开 End 后隐藏 |
| `Completed` | 显示明确完成文本，例如“沉睡森林试炼完成”；Completed Marker 显示 |

Completed 反馈在当前 Scene 实例内保持可见；“只显示一次”表示只发生一次状态提交与 Render 变化，不是反复创建/播放，也不要求计时后销毁。

### 10.3 uGUI 约束

- 复用已锁定的 `com.unity.ugui 2.0.0`，不修改 Package；
- 使用 `UnityEngine.UI.Text`，不引入 TextMeshPro；
- Canvas/Panel/Text 的 `raycastTarget` 关闭，不阻挡 WASD 或 Interaction；
- 不创建 EventSystem 或输入模块；
- 相同 FeedbackState 不重复设置 GameObject Active 或 Text；
- 不做 Tween、Animator、Audio、VFX；
- 不修改现有 `InteractionPromptCanvas` 或 Prompt Runtime。

### 10.4 Marker 约束

- Marker 是新建的无 Collider 表现对象，不改变 `SliceEndGround` 或 Landmark 承载结构；
- 推荐使用独立 Renderer/GameObject 显隐；若需要颜色覆盖，使用 `MaterialPropertyBlock`；
- 禁止 `renderer.material` 与 shared Material 修改；
- Marker Renderer 可以位于无形 Trigger 内，但不得新增 Collider 或与现有 Renderer 形成共面表面。

---

## 11. 条件不足反馈

玩家未完成 Trial 进入 Slice End 时：

- Completion State 保持 `NotCompleted`；
- FeedbackState 转为 `ConditionsUnmet`；
- 不传送、不重置、不封路、不 Reload、不冻结 Input；
- 玩家离开 Volume 后 FeedbackState 回到 `Hidden`；
- 完成 A → B 并返回后可正常 Completed；
- 若 Trial 在玩家仍位于 Volume 时变为 Completed，反馈直接从 `ConditionsUnmet` 切换为 `Completed`。

该反馈属于 Slice Completion Presentation，不调用 `InteractionPromptPresenter`，不显示 Binding，也不要求 E 键。

---

## 12. Scene 生命周期与初始化顺序

### 12.1 进入 Scene

- `ForestSignalTrial` 按 003B 规则初始化；
- Volume 初始没有本地玩家 Presence；
- Slice Completion 为 `NotCompleted / Revision 0`；
- Feedback 为 `Hidden`；
- Completed Marker 隐藏。

### 12.2 初始化时序保护

Completion Controller 的推荐顺序：

```text
Awake
  → 初始化自身 NotCompleted 状态（只执行一次）

OnEnable
  → 订阅 Trial 与 Volume 实例事件
  → 读取 Trial.CurrentSnapshot
  → 读取 Volume.IsPlayerInside
  → Evaluate

OnDisable
  → 取消订阅
  → 不回退已提交的 Completed 状态
```

Presenter/View 重新启用后必须从 Controller 当前 Snapshot/FeedbackState 恢复，不依赖错过的历史事件。

### 12.3 Scene Reload

Scene reload 销毁旧 Scene 实例与实例事件。新 Scene 创建新的 Completion Controller，恢复 `NotCompleted / Revision 0` 与隐藏反馈。Sprint003D 不实现跨 Scene Persistence。

---

## 13. 组件职责

| 组件 | 职责 | 明确不负责 |
|---|---|---|
| `SleepingForestSliceEndVolume` | 识别当前 `PlayerSpawner.SpawnedPlayer` 进入/离开正式 End Trigger；发布 Presence | Trial、Completion 状态、UI、移动、传送 |
| `SleepingForestSliceCompletion` | 组合 Trial Snapshot 与 Presence；单次提交 Completed；公开 Snapshot/实例事件 | Beacon、Bridge、UI、Interaction、存档 |
| `SleepingForestSliceCompletionSnapshot` | 只读表达 Completion State、Revision、Trial Revision | 写状态、Unity Object 引用 |
| `SleepingForestCompletionFeedbackState` | 表达 Hidden/ConditionsUnmet/Completed | 通用 UI 状态机 |
| `SleepingForestCompletionPresenter` | 订阅只读 Completion 边界；映射为 ViewData | 修改 Completion/Trial/Player |
| `SleepingForestCompletionView` | 无条件逻辑地显示/隐藏 Panel、Text、Marker | 查找 Player、读取 Trial、触发完成 |
| `SleepingForestCompletionAuthoring` | 显式、确认式、幂等地装配 Scene/Prefab 引用 | 自动执行、修改 Foundation、重建 Greybox |

不需要新增接口、基类、Service Locator、全局 Manager 或通用状态机。

---

## 14. 文件与程序集边界

### 14.1 现有程序集不适合直接承载状态拥有者的原因

- `WonderSquad.Puzzle`：拥有 Trial 与 003C 机关后果，但设计文档明确 Puzzle 不应拥有整关完成；
- `WonderSquad.SleepingForest.Greybox`：是 003A 临时 Greybox/FallRecovery 边界，不应承载正式 Slice Completion；
- `WonderSquad.Level`：当前只依赖 Core/Content。直接为一个具体关卡增加 Player/Puzzle 实现引用，会破坏通用 Level 的既有依赖约束；
- `WonderSquad.UI`：只能读取状态，不应拥有权威 Completion 状态。

### 14.2 推荐新增内容编排程序集

新增一个关卡内容专用而非通用框架程序集：

```text
WonderSquad.SleepingForest
  → WonderSquad.Core
  → WonderSquad.Player
  → WonderSquad.Puzzle
```

建议位置：

```text
Assets/WonderSquad/Runtime/Level/SleepingForest/
  WonderSquad.SleepingForest.asmdef
  Completion/
```

现有 `Greybox/` 子目录已有自己的 asmdef，因此不会被新父级程序集重复编译。

依赖方向：

```text
WonderSquad.UI
        ↓
WonderSquad.SleepingForest
     ↙             ↘
Player            Puzzle
     ↘             ↙
          Core
```

- Player、Puzzle、Interaction、Camera 均不得引用 `WonderSquad.SleepingForest`；
- `WonderSquad.SleepingForest` 不引用 UI、Interaction 或 Camera；
- `WonderSquad.UI` 增加对 `WonderSquad.SleepingForest` 的单向只读引用；
- 不形成循环依赖；
- 该程序集只容纳《沉睡森林》的场景编排，不演变为 Generic Level Framework。

### 14.3 Editor 与 Tests

- `WonderSquad.Editor` 在 Implementation 时增加对 `WonderSquad.SleepingForest`、`WonderSquad.Player` 与 `WonderSquad.UI` 的直接引用，仅供类型安全 Authoring；
- EditMode/PlayMode Tests 增加对 `WonderSquad.SleepingForest` 的引用；
- 生产 Runtime 不得引用 `UnityEditor`；
- PlayMode Scene 测试继续使用 `EditorSceneManager.LoadSceneInPlayMode/LoadSceneAsyncInPlayMode` 按资产路径加载，不要求把 SleepingForest 加入 Build Profile。

### 14.4 Implementation 预计新增文件

Runtime 内容编排：

- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/WonderSquad.SleepingForest.asmdef`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestSliceCompletionState.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestCompletionFeedbackState.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestSliceCompletionSnapshot.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestSliceEndVolume.cs`
- `Client/Assets/WonderSquad/Runtime/Level/SleepingForest/Completion/SleepingForestSliceCompletion.cs`

UI：

- `Client/Assets/WonderSquad/Runtime/UI/SleepingForest/SleepingForestCompletionViewData.cs`
- `Client/Assets/WonderSquad/Runtime/UI/SleepingForest/SleepingForestCompletionPresenter.cs`
- `Client/Assets/WonderSquad/Runtime/UI/SleepingForest/SleepingForestCompletionView.cs`

Editor/资产/测试：

- `Client/Assets/WonderSquad/Editor/SleepingForestCompletionAuthoring.cs`
- `Client/Assets/WonderSquad/Prefabs/UI/PF_SleepingForestCompletionFeedback.prefab`
- `Client/Assets/WonderSquad/Tests/EditMode/SleepingForestCompletionEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/SleepingForestCompletionPlayModeTests.cs`

Implementation 预计修改：

- `WonderSquad.UI.asmdef`
- `WonderSquad.Editor.asmdef`
- EditMode/PlayMode Test asmdef；
- `SleepingForest.unity`（只增加 003D 组合对象与序列化引用）；
- 003A/B 中过时的 Completion 禁止断言；
- `CHANGELOG.md` 与 003D 实施报告。

不需要新增 ScriptableObject、Input Action、Package 或 ProjectSettings。

---

## 15. Scene Authoring 策略

建议提供显式菜单：

```text
Wonder Squad > Sprint003D > Apply Sleeping Forest Completion
```

必须遵循：

- 仅手动执行，无 `InitializeOnLoad`、Asset Postprocessor 或 Play Mode 自动入口；
- Play Mode 中禁用；
- 修改前显示明确确认对话框；
- 先调用 Unity 官方的 modified scenes 保存确认流程；用户取消时立即退出且不改资产；
- 使用固定 Scene Asset Path 打开正式 SleepingForest，不操作 Sandbox/Test Scene；
- 采用 Single Scene authoring，避免 Additive + Untitled Scene 生命周期问题；
- 只创建/更新唯一 `SleepingForestCompletion` 组合根；
- 已存在且引用正确时保持对象身份，不重复创建 Controller、Volume、Canvas 或 Marker；
- 如果发现多个正式 Controller/Volume，应报错并停止，不静默删除未知对象；
- 所有 `PlayerSpawner`、Trial、Volume、Presenter、View 与 Marker 引用必须真实序列化；
- Mark Dirty、Save Scene、Save Assets 后再报告成功；
- 不运行 Sprint003A Rebuild，不修改 003B Beacon 或 003C Consequence；
- 不手工编辑 Scene YAML。

UI Prefab 的创建/更新也必须幂等。Scene 中实例化一个 Prefab 实例，不在每次运行时创建 Canvas。

---

## 16. Soft-lock Audit

| 场景 | 预期结果 | 结论 |
|---|---|---|
| 未完成 Trial 直接进入 Slice End | NotCompleted；显示条件不足；可自由离开 | 无软锁 |
| 离开 End，完成 A → B，再返回 | 第二次进入时正常 Completed | 无需 Reload |
| 玩家在 End 内时 Trial 变为 Completed | 由 Trial 事件立即完成 | 不依赖重新进入 Trigger |
| Completed 后重复进入 | State/Revision/Event 不变，UI 不重复创建 | 幂等 |
| Completed 后触发 FallRecovery | Completed 保持；Player 正常恢复 | Completion 不控制 Player |
| Trigger 附近掉落 | Presence 清除；FallRecovery 后可继续 | Trigger 不延伸至悬空区 |
| Main Route 到终点 | 使用同一 Volume 正常完成 | Main Route 永久合法 |
| Advantage Route 到终点 | 使用同一 Volume 正常完成 | 不形成第二套条件 |
| B → IncorrectOrder → A → B → End | IncorrectOrder 不完成；恢复后 Completed | 复用 003B 可恢复状态机 |
| Scene reload | 新实例回到 NotCompleted，反馈隐藏 | 无 static/Persistence 污染 |

不允许 Completion 执行玩家传送、输入控制、Collider 开关、场景 Reload 或路线封锁。

---

## 17. Sprint003A/B/C 回归影响

### 17.1 Foundation 冻结

不得修改：

- `PlayerSpawner`、`PlayerInputReader`、`PlayerMovement`、`GroundDetector`；
- Camera Foundation；
- `SleepingForestFallRecovery`；
- `InteractionDetector`、`InteractionValidator`、`InteractionExecutor`、`InteractionInputReader`、Interaction Prompt；
- `IInteractable`、`IExecutableInteraction`、`InteractionResult`、`InteractionResultCode`；
- `ForestSignalTrial` 核心状态机、Beacon A/B；
- `SleepingForestRootBridgeConsequence` 核心行为。

### 17.2 已发现的过时断言

以下测试包含 003D 后语义过时的 Completion 禁止断言：

- `SleepingForestGreyboxEditModeTests.SleepingForestScene_HasOnlyApprovedBeaconInteractionGameplay`：检查层级名称不含 `CompletionGameplay`；
- `ForestBeaconConfigurationEditModeTests.SleepingForest_ContainsNoDiagnosticProbeOrFutureConsequences`：检查层级名称不含 `CompletionGameplay`；
- `ForestSignalTrialPlayModeTests.AssertRoutesRemainUnchanged`：检查 `GameObject.Find("CompletionGameplay")` 为 null。

Implementation 不得删除整组边界测试。应精确调整为：

- 允许且要求一个已批准类型的 `SleepingForestSliceCompletion`；
- 允许且要求一个已批准类型的 `SleepingForestSliceEndVolume`；
- 继续禁止 Sandbox Probe、`InteractionExecutionProbe`、未批准的 `CompletionGameplay` 通用根、重复 Controller、重复 Volume；
- 继续验证仅两个合法 Beacon TargetId；
- 继续验证 Main Route、Temporary Crossing、Advantage Route 与 Foundation 未被改动；
- Completion Trigger 不增加 `InteractionTarget`，因此 Scene 中 InteractionTarget 仍应只有 Beacon A/B 两个。

### 17.3 003C 回归

- Completion 不引用或修改 `RootBridgeAdvantageSpan`；
- `RootBridgeTemporaryCrossing` 保持不变；
- AdvantageSpan 的 Renderer/Collider/Revision 测试继续通过；
- Scene 中 003C Consequence 和 Span 仍各只有一个；
- 003D Authoring 不得以删除/重建 003C 根的方式装配。

---

## 18. EditMode 测试计划

至少规划：

1. Completion 初始为 NotCompleted、Revision 0；
2. Trial 未完成时 Player Presence 进入不完成；
3. Trial 完成后 Presence 进入会完成；
4. Presence 已在 End 内时 Trial 变 Completed 会立即完成；
5. Completion 只发生一次；
6. 重复 Enter、重复 Snapshot、重复相同 Revision 保持幂等；
7. Snapshot 只读字段和值正确；
8. Completion Event 为实例级、非 static、无订阅者安全；
9. Scene reload/new fixture 初始化为 NotCompleted；
10. Main/Advantage 两条路线不进入状态条件表达式；
11. Volume 具有 BoxCollider、`isTrigger = true`、有效 PlayerSpawner 引用；
12. Volume Bounds 在 SliceEndGround 内，不与 Ground/Tower/Arch Collider 形成正体积重叠；
13. 非 Player Collider 不改变 Presence；
14. View 默认隐藏；
15. ConditionsUnmet 显示，Exit 后隐藏；
16. Completed View/Marker 显示且相同数据不重复刷新；
17. UI Graphic 不拦截 Raycast；
18. Scene 只有一个 Controller、Volume 与 Feedback View；
19. 没有 static/global Completion 状态；
20. asmdef 依赖方向无循环，Player/Puzzle/Interaction 不反向引用 SleepingForest；
21. Foundation 契约和组件源文件不需要修改；
22. 003A/B/C 精确边界测试继续保留其保护能力。

不预设最终测试数量，以 Implementation 完成后 Unity Test Runner 实际数量为准。

---

## 19. PlayMode 测试计划

至少规划：

1. 通过 Editor PlayMode Test API 成功加载 SleepingForest Asset Path；
2. Player 正常 Spawn 且只有一个；
3. Main Camera 正常且只有一个；
4. 未完成 Trial 进入 End 不完成；
5. ConditionsUnmet 反馈出现；
6. Player 可离开 End，提示隐藏且移动不受影响；
7. 真实 A → B 流程完成 Trial；
8. 返回 End 后 Completion 发生并显示反馈；
9. Player 在 Volume 内时模拟真实 Trial 完成事件可立即完成；
10. Completion Event/Revision 只提交一次；
11. 重复离开/进入不重复完成、不重复创建 UI；
12. Main Route 可到达同一 End Volume；
13. Advantage Route 可到达同一 End Volume；
14. B → IncorrectOrder → A → B 后仍可完成；
15. Root Bridge 环境后果保持正常；
16. Completion Trigger 附近掉落后 FallRecovery 正常；
17. Completed 后 FallRecovery 不清除 Completion；
18. Movement、GroundDetector、Camera 正常；
19. Detection、Prompt、Execution 回归正常，End 不出现 Interaction Prompt；
20. Scene reload 后 Completion 与 Feedback 重置；
21. Console Error = 0。

完整回归必须从用户确认的 `108 EditMode / 58 PlayMode / Console Error 0` 基线继续，最终数量按 Unity Test Runner 实测记录。

---

## 20. 人工验收计划

### 流程 A：正常 Main Route

```text
Spawn → A → B → Main Route → Slice End → Completion
```

确认 Root Bridge Consequence 正常、Main Route 仍永久可用、完成反馈明确。

### 流程 B：Advantage Route

```text
Spawn → A → B → Advantage Route → Slice End → Completion
```

确认 Advantage Route 与 Main Route 汇入同一 Trigger，不形成不同完成规则。

### 流程 C：提前到终点

```text
Spawn → 不完成 Trial → Slice End → ConditionsUnmet
      → 离开 → A → B → 返回 Slice End → Completion
```

确认不锁路、不传送、不 Reload、不要求按 E。

### 流程 D：IncorrectOrder 恢复

```text
B → IncorrectOrder → A → B → Slice End → Completion
```

确认错误不会污染 Completion 状态。

### 流程 E：重复触发

```text
Completed → 离开 End → 再进入 → 不重复提交/创建反馈
```

### 流程 F：FallRecovery

流程中主动掉落并 Recovery，继续 A/B/路线/终点流程；Completed 后再次掉落也不回退完成状态。

所有流程同时检查：

- Movement、GroundDetector、Camera、Interaction 正常；
- UI 不阻挡输入；
- 单 Player、单 Main Camera；
- Trigger 不产生卡顿、双层地面或 Z-fighting；
- Console Error = 0；
- 稳定帧没有 Completion 系统持续 GC.Alloc。

---

## 21. 风险与处理

| 风险 | 级别 | 处理 |
|---|---|---|
| 003C 实测已通过但仓库收尾文档仍为待验证 | 高 | Implementation 前同步 003C Report/Checklist/Changelog 并生成 Gate Review |
| 003C 改动尚未形成稳定 Git 基线 | 高 | Implementation 前提交或以项目既定方式固定 003C 变更，保证 003D 范围可审计 |
| 通用 Level 直接依赖 Puzzle/Player 实现 | 高 | 使用 `WonderSquad.SleepingForest` 内容编排程序集，不修改通用 Level 依赖 |
| Trigger 接受环境 Collider | 高 | 显式引用 PlayerSpawner，并与 SpawnedPlayer 根/子层级比较 |
| Trigger 与 Tower/Ground Collider 重叠 | 高 | 使用后半平台推荐 Bounds；Editor Bounds 测试和 Scene 人工检查双重验证 |
| 仅依赖 OnTriggerEnter 导致远端 Trial 完成后必须重入 | 高 | Controller 同时订阅 Trial 实例事件，选择立即完成 |
| Completion 依赖 003C 状态造成软锁 | 高 | 只以 Trial + Presence 为 Gate；003C 仅做集成回归 |
| View 与 Gameplay 相互引用形成循环 | 中 | UI → SleepingForest 单向读取；SleepingForest 不引用 UI |
| UI 污染 Interaction Prompt | 中 | 独立 View/Canvas，不引用 Prompt Data、Binding 或 IInteractable |
| 重复 Enter/事件反复提交 | 中 | 状态与 Feedback 均比较后发布，Completed 单向且 Revision 固定为 1 |
| Scene Authoring 复制对象或破坏已有场景 | 中 | 显式确认、幂等、唯一性检查、真实序列化引用；重复对象时中止 |
| Scene reload 保留旧 static 状态 | 中 | 所有状态与事件为 Scene 实例级，专门 PlayMode reload 测试 |

---

## 22. Sprint003D 禁止范围

严格禁止：

- 新 Puzzle、Beacon、Door、Lever 或 Interaction 类型；
- Ability、Inventory、Crafting；
- Network、Save/Persistence；
- Quest、Mission、Reward、Achievement；
- Dialogue、NPC、Level Select、下一关加载；
- Scene Transition Framework；
- Audio、Animation、Tween、VFX Framework；
- 通用 Level/Objective Framework；
- UI Framework 重构；
- 新 Input Action、Completion Input Reader 或 Press E to Finish；
- Character、Interaction、Camera Foundation 修改；
- Main Route 封锁或强制 Advantage Route；
- Package、ProjectSettings 或 Build Profile 修改。

---

## 23. Definition of Ready / Gate 条件

### 23.1 已满足

- Completion 唯一条件已锁定；
- Trigger 内 Trial 变 Completed 采用立即完成；
- Scene 空间与 Trigger 推荐 Bounds 已审计；
- Player 身份识别方式已锁定为显式 PlayerSpawner Scene 实例关系；
- Trial Snapshot/Event 只读消费方式已确认；
- 003C 明确不是第二 Gate；
- Completion State、Revision、事件与 Scene 生命周期已定义；
- uGUI Feedback、ConditionsUnmet 与 Marker 方案已选定；
- 内容编排程序集和依赖方向已明确，无循环；
- Authoring、回归测试与人工验收范围已明确；
- 不需要修改 Foundation、Input、Package 或 ProjectSettings。

### 23.2 Implementation 前必须关闭

1. 使用用户提供的最终测试结果完成 Sprint003C 文档收尾并生成 `Sprint003C_Gate_Review.md`；
2. 将当前 Sprint003C 变更固定为可追踪的稳定 Git 基线，使后续 003D diff 可独立审计。

关闭以上两项后，可进入 Sprint003D Implementation Plan。当前不得直接开始 Implementation。

---

# CONDITIONAL GO

Sprint003D 技术与玩法设计可实施；剩余条件仅为 Sprint003C 基线文档和版本治理收口。完成这两项后无需重新设计 Completion，可以直接进入 Sprint003D Implementation Plan。

---

## 24. Gate Closure Record

本节追加于原始 `CONDITIONAL GO` 之后，用于保留完整审计历史。原始结论、两个条件及当时的风险判断均不删除、不改写。

### 24.1 原始实施前条件

1. 使用最终 Unity 测试结果完成 Sprint003C 文档收尾并生成 `Sprint003C_Gate_Review.md`；
2. 将 Sprint003C 改动固定为可追踪的稳定 Git 基线，使 Sprint003D diff 可独立审计。

### 24.2 条件关闭证据

| 条件 | 状态 | 关闭证据 |
|---|---|---|
| Sprint003C 最终测试与 Gate Review | `CLOSED` | `Sprint003C_Gate_Review.md` 最终结论为 `GO`，Sprint003C 状态为 `PASSED / GO`；专项 EditMode `8/8`、专项 PlayMode `4/4`、完整 EditMode `108/108`、完整 PlayMode `58/58` 均通过，Console Error `0` |
| Sprint003C 可追踪 Git 基线 | `CLOSED` | Branch：`feature/sprint003-gameplay-vertical-slice`；Commit：`e1c15e4 feat(level): complete Sprint003C root bridge consequence`；HEAD 与 `origin/feature/sprint003-gameplay-vertical-slice` 均为 `e1c15e4c31c34ef723a8ff7f4770c484e7dd79fc`；关闭审计时 working tree clean |

### 24.3 Sprint003C Gate 结果

- Gate Review：`GO`
- 最终状态：`PASSED / GO`
- Foundation 边界：保持
- Sprint003D Completion：未提前实现

### 24.4 当前实施准入状态

# GO — CONDITIONS CLOSED

原始两个 `CONDITIONAL GO` 条件已全部关闭。Sprint003D 可以进入 Implementation Plan；本记录不授权跳过 Plan 或直接开始 Runtime/Scene Implementation。
