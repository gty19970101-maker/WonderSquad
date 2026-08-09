# Sprint003C — Root Bridge Advantage Review Checklist

## 当前状态

`SPRINT003C VISIBLE GEOMETRY CLEANUP — VERIFIED`

## 范围

- [x] 只实现 Trial Completed 驱动的 Root Bridge Advantage Span 环境后果。
- [x] 未实现 Completion、End Trigger、Completion UI、Build 入口、Network、Save、Inventory、Ability 或通用 Puzzle Framework。
- [x] 未修改 Character Foundation、Interaction Foundation、ForestSignalTrial 核心状态机、Beacon A/B、FallRecovery 或 003D Completion；仅按本轮批准调整 Main Route 的桥区空间布局。

## 架构与状态

- [x] Consequence 只读取 `ForestSignalTrialSnapshot`、`Revision` 和 Trial 实例事件。
- [x] 未读取 Trial 私有字段，未写入 Trial/Beacon 状态。
- [x] `Initial`/`Activated` 是 SleepingForest 局部、有限状态，不是通用状态机。
- [x] 同 Revision/旧 Revision 不会重复应用环境变化。
- [x] Disable/Enable 后从 `CurrentSnapshot` 安全重建表现。
- [x] 没有 static Gameplay 状态、全局 EventBus、Service Locator、字符串查找或 Update 轮询。
- [x] `WonderSquad.Puzzle` 未新增 Interaction、Player、Camera、UI、Network 或 Editor 依赖。

## 环境与物理

- [x] `RootBridgeTemporaryCrossing` 不被新增 Runtime 逻辑持有为可写引用。
- [x] `RootBridgeTemporaryCrossing` 未被删除、禁用或替换；Renderer/Collider 同步调整为永久折线主路第一段。
- [x] `RootBridgeMainRouteOuterLeg` 与 `RootBridgeMainRouteReturn` 构成连续外侧绕行，始终启用。
- [x] Span Renderer 和承载 Collider 同步开关。
- [x] Span 是独立单层地面；Authoring 包含与 Main Bridge 的 Bounds 校验。
- [x] Authoring 不会自动执行，并具有确认与保存/取消流程。
- [x] 两个失去职责的旧 Guardrail 已删除，测试要求 Scene 中不存在这两个对象。
- [x] 隔离 Unity 自动测试确认三段主路、Span、入口、Slice End 与 Root Landmark 无正体积重叠。
- [x] 自动量化确认 Main Route 约 `46.24m`、Advantage Route `27.00m`，缩短约 `41.6%`。
- [x] Advantage Route 跳过完整外侧长段与两次主路转向，外侧平行段保持至少 `11.5m` 横向距离。
- [x] 正式 Unity 人工确认无 Z-fighting、双层地面、CharacterController 绊脚或 GroundDetector 抖动。
- [x] 正式 Unity 人工确认主桥永久可走、Advantage Route Activated 后可走且捷径感知明确。
- [x] 正式 Unity 人工确认 Player 位于桥区完成 Trial 时无卡死、掉落或位移异常。
- [x] 正式 Unity 人工确认 FallRecovery 在 Initial/Activated 均正常。

## 视觉与性能

- [x] 使用 `MaterialPropertyBlock`，不访问 `Renderer.material`，不写 `sharedMaterial`。
- [x] 专项 EditMode 测试覆盖两个 Span Visual 共享材质时的状态隔离。
- [x] 有 Marker 作为不依赖颜色的路径反馈。
- [x] 不使用 Animator、Tween、VFX、音效或 Shader 重构。
- [x] 静态审计确认无 `Update` 轮询或每帧查找，`MaterialPropertyBlock` 被实例复用，不存在稳定帧持续分配路径。

## 测试与回归

- [x] 新增 EditMode 与 PlayMode 专项测试。
- [x] 测试覆盖 Initial、Completed、IncorrectOrder 恢复、Revision 幂等、材质隔离、Scene 初态和 Foundation 回归。
- [x] Unity `6000.3.21f1` 编译无错误。
- [x] 隔离 Unity Sprint003C EditMode：`9 / 9 Passed`，`0 Failed`，`0 Skipped`。
- [x] 隔离 Unity Sprint003C PlayMode：`5 / 5 Passed`，`0 Failed`，`0 Skipped`。
- [x] 隔离 Unity 完整 EditMode：`122 / 122 Passed`，`0 Failed`，`0 Skipped`。
- [x] 隔离 Unity 完整 PlayMode：`63 / 63 Passed`，`0 Failed`，`0 Skipped`。
- [x] 正式 Unity Console Error = `0`。

## 文档

- [x] 实施报告已创建。
- [x] CHANGELOG 已记录空间分离修复和待正式 Unity 验证状态。
- [x] 正式 Unity 专项与完整 EditMode/PlayMode 均全部 Passed、`0 Failed`、`0 Skipped`；仓库无本次 XML，精确数量标记为 `TEST COUNT REQUIRES MANUAL RECORD`。
- [x] `Sprint003C_Gate_Review.md` 已生成。

## Verification 修复审计

- [x] `CS0104` 仅通过显式 `UnityEngine.Object` 修复测试命名歧义。
- [x] Span / Visual 初始状态由生命周期显式同步，不依赖 enum 默认值代表“已应用”。
- [x] 最终 EditMode 失败定位为 Fixture inactive 时 Configure 未订阅实例事件。
- [x] 测试夹具改为激活后 Configure，未直接调用 Span 激活绕过 Consumer。
- [x] 未修改 ForestSignalTrial、Beacon、Revision 或 Interaction Foundation 以迎合测试。

## Advantage Route Spatial Separation Fix（已被人工复验否决）

- [x] 原重合职责问题已定位：旧 Span 与永久桥连接同一入口/出口，虽无 Bounds 正体积重叠但无路线收益。
- [x] 第一轮曾设置主桥 `(5.75, -0.5, 35)` / `(7, 1, 16)` 与 Span `(0, -0.5, 35)` / `(2.5, 1, 16)`。
- [x] 正式人工复验确认该布局仍为长距离并排，`15.8%` 收益不具可读性；该布局已被替换。
- [x] 禁用状态 BoxCollider 使用配置 Bounds 审计，不以零 Bounds 掩盖重叠。
- [x] Completion Controller、SliceEndTrigger、Completion UI 和 Requirement Feedback 未修改。

## Advantage Route Readability Fix

- [x] Authoring 幂等设置永久主路三段：`(8.5,-0.5,23)/(9,1,6)`、`(16,-0.5,34)/(6,1,22)`、`(11.5,-0.5,45)/(3,1,6)`。
- [x] Advantage Span 为 `(0,-0.5,35)/(3,1,16)`，Completed 后直连入口与 Slice End。
- [x] 每个主路转角及捷径端点连接宽度不少于 `1.5m`。
- [x] 旧 Guardrail、旧 Advantage Start/Exit Junction 不再生成。
- [x] Scene、Prefab、Authoring 与 003A/003C 空间测试已同步。
- [x] Completion Controller、SliceEndTrigger、Requirement/Completion Feedback 未修改。
- [x] 正式 SleepingForest Scene 人工走图与 Console 验证通过。

## Visible Geometry 精确清理

- [x] 通过正式 Scene 的完整层级、Transform 与组件数据确认左/右长条分别为 `SleepingForestRoot/Landmarks/RootBridgeLandmark_LeftRoot` 与 `.../RootBridgeLandmark_RightRoot`，不是旧 Guardrail。
- [x] 两对象仅为 003A 装饰地标，不承担 Ground、Main Route、Advantage Route、Boundary、Slice End 或 Completion 职责。
- [x] Scene 已完整删除两个 GameObject 及其 MeshFilter、MeshRenderer、BoxCollider，不保留隐形 Collider。
- [x] 003A Builder 已移除独立创建路径，Greybox Rebuild 不会恢复旧长条。
- [x] 003C Authoring 通过两个精确名称执行旧 Scene Migration，重复 Apply 不会恢复旧长条，也不会模糊删除其他对象。
- [x] 新增/调整测试，断言旧长条不存在，并审计中央 Advantage 走廊没有未批准的可见/可碰撞大型 Cube。
- [x] Main Route `46.24m`、Advantage Route `27.00m`、约 `41.6%` 收益和两路线汇入同一 Slice End 的结构未改。
- [x] Character、Interaction、Camera、FallRecovery、ForestSignalTrial、Beacon 与 Completion 实现未改。
- [x] Unity `6000.3.21f1` 专项 EditMode / PlayMode 与完整回归全部 Passed、`0 Failed`、`0 Skipped`；未复用上一轮测试计数，精确数量标记为 `TEST COUNT REQUIRES MANUAL RECORD`。
- [x] 正式 Scene 人工确认绿色捷径两侧干净、无空气墙、Movement/GroundDetector/Camera 正常且 Console Error = `0`。

## 最终状态

# SPRINT003C VISIBLE GEOMETRY CLEANUP — VERIFIED
