# Sprint003D — Sleeping Forest Slice Completion Review Checklist

## 当前状态

`PASSED / GO — VERTICAL SLICE COMPLETE`

## 范围

- [x] 只实现 Slice End、Completion State、Requirement/Completion Feedback 与场景闭环。
- [x] 未实现 Reward、Quest、Save、Network、Achievement、下一关、Scene Transition、Inventory、Ability 或新 Puzzle。
- [x] 未修改 Character、Interaction、Camera Foundation。
- [x] 未修改 ForestSignalTrial、Beacon A/B 或 RootBridgeAdvantageConsequence 核心逻辑。

## Completion 条件与状态

- [x] 只有 Trial Completed + PlayerAtEnd 同时成立才完成。
- [x] 仅完成 A → B 不完成整个 Slice。
- [x] 仅进入 End 不完成整个 Slice。
- [x] RootBridgeAdvantageSpan Activated 不是 Completion Gate。
- [x] Main Route 与 Advantage Route 使用同一 Slice End Trigger。
- [x] NotCompleted → Completed 不可逆且只发生一次。
- [x] Completion Revision 与 Trial Revision 语义分离。
- [x] Scene reload 后重新从 NotCompleted 开始，无跨 Scene 持久化。

## Trial Snapshot / Revision

- [x] 只读消费 `ForestSignalTrialSnapshot`、Revision、实例事件与 `CurrentSnapshot`。
- [x] 先订阅再立即读取 CurrentSnapshot。
- [x] IncorrectOrder Revision 不阻塞未来 Completed Revision。
- [x] 相同/旧 Revision 幂等。
- [x] Player 已在 End 时 Trial Completed 会立即完成。
- [x] 无 Update 轮询、全局查找或 static Gameplay 状态。

## Trigger 与 Player 身份

- [x] Trigger 显式引用 `PlayerSpawner` 与 isTrigger BoxCollider。
- [x] 只接受 `PlayerSpawner.SpawnedPlayer` 根或子 Collider。
- [x] 环境、Beacon、Bridge Collider 无法触发 Completion。
- [x] Trigger 不拥有 Completion、不读取 Trial、不显示 UI、不移动 Player。
- [x] Trigger 不作为 Ground Collider，不阻挡 CharacterController。

## UI 与 Feedback

- [x] Requirement Feedback 使用独立场景 UI，不冒充 InteractionPrompt。
- [x] Completion View 只显示只读 ViewData。
- [x] UI Graphic/Text `raycastTarget = false`。
- [x] 相同数据不重复刷新或创建 Canvas。
- [x] Completed Marker 无 Collider，不修改既有 Landmark。
- [x] Stable text key 与 fallback 已锁定。
- [x] 未引入 UI Framework、动画、Tween、音效或完整本地化系统。

## Scene Authoring 与资产

- [x] Authoring 仅通过显式确认菜单执行，不自动运行。
- [x] Authoring 走 Unity 保存/取消流程。
- [x] Root inactive 时完成 SerializedObject 引用并 Apply。
- [x] 最后统一启用，避免 OnEnable 早于配置。
- [x] 重复 Apply 复用唯一 Completion 组合；重复对象会阻塞并报错。
- [x] 隔离 Unity Authoring 实际执行成功，Scene/Prefab 已生成并回填。
- [x] Scene 只有一个 Controller、Trigger、Presenter/View/Marker。
- [x] SliceEnd Tower、Arch、Ground 保留。
- [x] Prefab 与所有新增 Unity 文件均包含 `.meta`。

## Physics 与 Soft-lock

- [x] Trigger/Marker 不新增承载面或双层 Ground。
- [x] 没有 Renderer 共面叠加或新的 Z-fighting 路径。
- [x] 未修改 Main Route、Advantage Route 或 RootBridgeTemporaryCrossing。
- [x] 未完成 Trial 提前到 End 后可以离开并恢复流程。
- [x] IncorrectOrder 后仍可 A → B → End 完成。
- [x] Completion 不锁定、重置、传送或销毁 Player。
- [x] 正式 Editor 确认 Trigger 边缘无卡顿、GroundDetector 无抖动。
- [x] 正式 Editor 确认 FallRecovery 后可继续 Completion。

## 架构

- [x] `WonderSquad.SleepingForest` 只引用 Core、Player、Puzzle。
- [x] UI 单向依赖 SleepingForest；Gameplay 不依赖 UI。
- [x] Player、Puzzle、Interaction、Camera 无反向引用。
- [x] Runtime 不引用 UnityEditor。
- [x] 无循环依赖、Global Manager、Quest/Level Framework。
- [x] 无 `renderer.material`、sharedMaterial 写入或持续帧分配路径。

## 自动测试

- [x] Sprint003D EditMode 覆盖双条件、顺序、IncorrectOrder、幂等、身份、UI、Scene 配置与程序集边界。
- [x] Sprint003D PlayMode 覆盖提前到达、离开、Trial-first、Player-first、IncorrectOrder、UI 和 Foundation 回归。
- [x] Unity `6000.3.21f1` 隔离编译无错误。
- [x] 隔离 Sprint003D EditMode：`13/13 Passed`。
- [x] 隔离 Sprint003D PlayMode：`4/4 Passed`。
- [x] 隔离完整 EditMode：`121/121 Passed`。
- [x] 隔离完整 PlayMode：`62/62 Passed`。
- [x] 隔离完整 PlayMode 日志无未处理异常。
- [x] 当前正式 Editor 的 Sprint003C/003D 专项 EditMode/PlayMode 全部 Passed，`0 Failed`，`0 Skipped`。
- [x] 当前正式 Editor 的完整 EditMode/PlayMode 全部 Passed，`0 Failed`，`0 Skipped`。
- [x] 当前正式 Editor Console Error = `0`。
- [x] 仓库未保存本次 Test Runner XML，最终精确数量明确标记为 `TEST COUNT REQUIRES MANUAL RECORD`，未复用旧计数。

## 人工验收

- [x] A → B → Main Route → End → Completion。
- [x] A → B → Advantage Route → End → Completion。
- [x] 未完成 Trial → End → Requirement → 离开 → A → B → 返回完成。
- [x] Player 位于 End 内时 Trial Completed → 立即完成。
- [x] B → IncorrectOrder → A → B → End → 完成。
- [x] Fall → Recovery → 继续 → 完成。
- [x] 完成后离开/重进不重复触发。
- [x] Movement、GroundDetector、Camera、Prompt、Bridge 均正常。
- [x] 稳定帧未发现 Completion 持续 GC.Alloc。

## Sprint003C 内容修复闭环

- [x] Main Route 为约 `46.24m` 的永久三段路线；Advantage Route 为 `27.00m` 直连捷径，收益明确且不长距离并排。
- [x] `RootBridgeLandmark_LeftRoot` / `RightRoot` 已从正式 Scene、003A Builder 与 003C Authoring 生命周期完整清理。
- [x] 重复执行 Sprint003C Apply 不恢复废弃 Geometry。
- [x] 无空气墙、隐形 BoxCollider、Z-fighting、双层 Ground Collider 或接地抖动。
- [x] Completion 条件未加入 Bridge Activated；两条路线共享 Trial Completed + PlayerAtEnd 双条件。

## 文档

- [x] Preflight 保留历史并记录 `GO — CONDITIONS CLOSED`。
- [x] Implementation Plan 为 `READY FOR IMPLEMENTATION`。
- [x] Implementation Report 已创建。
- [x] CHANGELOG 已更新。
- [x] 人工验收步骤与已知限制已记录。

## 最终状态

# SPRINT003D — PASSED / GO

# SLEEPING FOREST GAMEPLAY VERTICAL SLICE — COMPLETE

Sprint003D Gate Review 已生成。停止于当前 Vertical Slice，不开始 Sprint004。
