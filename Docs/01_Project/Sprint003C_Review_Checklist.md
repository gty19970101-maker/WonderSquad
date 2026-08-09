# Sprint003C — Root Bridge Advantage Review Checklist

## 当前状态

`PASSED / GO`

## 范围

- [x] 只实现 Trial Completed 驱动的 Root Bridge Advantage Span 环境后果。
- [x] 未实现 Completion、End Trigger、Completion UI、Build 入口、Network、Save、Inventory、Ability 或通用 Puzzle Framework。
- [x] 未修改 Character Foundation、Interaction Foundation、ForestSignalTrial 核心状态机、Beacon A/B、FallRecovery 或 Main Route。

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
- [x] 主桥不会被删除、移动、禁用、替换或改变承载 Collider。
- [x] Span Renderer 和承载 Collider 同步开关。
- [x] Span 是独立单层地面；Authoring 包含与 Main Bridge 的 Bounds 校验。
- [x] Authoring 不会自动执行，并具有确认与保存/取消流程。
- [x] Unity 实测确认所有新 Span/Junction 与既有几何无正体积重叠或 Z-fighting。
- [x] Unity 实测确认 Advantage Route 有明确的空间/路径收益，且不是唯一通路。
- [x] Unity 实测确认 Player 位于 Main Bridge/Junction 时 Trial 完成不会卡死、掉落或抖动。
- [x] Unity 实测确认 FallRecovery 在 Initial/Activated 均正常。

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
- [x] Sprint003C EditMode：`8 / 8 Passed`，`0 Failed`，`0 Skipped`。
- [x] Sprint003C PlayMode：`4 / 4 Passed`，`0 Failed`，`0 Skipped`。
- [x] 完整 EditMode：`108 / 108 Passed`，`0 Failed`，`0 Skipped`。
- [x] 完整 PlayMode：`58 / 58 Passed`，`0 Failed`，`0 Skipped`。
- [x] Console Error = `0`。

## 文档

- [x] 实施报告已创建。
- [x] CHANGELOG 已记录 Sprint003C 最终 Unity 验证和 `PASSED / GO` 状态。
- [x] Unity 实测结果与人工验收已回填。
- [x] `Sprint003C_Gate_Review.md` 已生成。

## Verification 修复审计

- [x] `CS0104` 仅通过显式 `UnityEngine.Object` 修复测试命名歧义。
- [x] Span / Visual 初始状态由生命周期显式同步，不依赖 enum 默认值代表“已应用”。
- [x] 最终 EditMode 失败定位为 Fixture inactive 时 Configure 未订阅实例事件。
- [x] 测试夹具改为激活后 Configure，未直接调用 Span 激活绕过 Consumer。
- [x] 未修改 ForestSignalTrial、Beacon、Revision 或 Interaction Foundation 以迎合测试。

## 最终状态

# PASSED / GO
