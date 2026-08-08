# Sprint003B — Forest Signal Trial Review Checklist

## 当前状态

`PASSED / GO`

## 范围与架构

- [x] 只实现 Forest Beacon A/B 与 ForestSignalTrial。
- [x] 正确顺序固定为 A → B。
- [x] B 先执行产生 IncorrectOrder，不激活 B，不形成软锁。
- [x] 错序后仍可通过 A → B 完成。
- [x] Activated 不可返回 Dormant。
- [x] Trial Completed 只提交一次。
- [x] ForestSignalTrial 不读取 Player/Input，不操作 Prompt、Camera、路线或 Root Bridge。
- [x] 未修改 Character Foundation。
- [x] 未修改 Interaction Foundation。
- [x] 未引入 Puzzle Framework、通用基类、全局 EventBus 或静态 Manager。

## Definition 与运行时状态

- [x] Definition 只保存稳定 ID、schema、A/B Definition。
- [x] Runtime State 只存在 Scene Trial 实例。
- [x] Snapshot 为只读值类型。
- [x] Snapshot 包含 A、B、IncorrectOrder、Completed 与 Revision。
- [x] Runtime 不写入 ScriptableObject。
- [x] Scene 重载从初始状态与 Revision 0 开始。
- [x] 未实现跨 Scene/Save 持久化。

## 来源、重复请求与事件

- [x] Trial 校验实际 source 组件引用，而非只信任 TargetId。
- [x] 校验 source Scene、Slot 与 TargetId。
- [x] 未注册/伪造 source 不推进状态。
- [x] 相同 RequestId 仍由现有 Request Port 去重。
- [x] Activated/Completed 重复请求不递增 Revision。
- [x] 事件为实例级且参数为只读 Snapshot。
- [x] 没有 static event 或全局事件总线。

## 视觉与 Prefab

- [x] Beacon 使用组件组合，无 BaseBeacon/BaseInteractable。
- [x] 视觉使用 MaterialPropertyBlock。
- [x] 未访问 Renderer.material。
- [x] 未写入 sharedMaterial 状态。
- [x] 颜色外另有 Dormant/Activated/Incorrect Marker。
- [x] A/B 各自拥有 Renderer、Marker 与 PropertyBlock。
- [x] 没有 Animator、Tween、Particle 或复杂 Shader。
- [ ] 当前 Unity 已执行显式 Sprint003B authoring 菜单。
- [ ] `PF_ForestBeacon` 已在 Unity Inspector 验证组件完整。
- [ ] SleepingForest 中仅有两个正式 Beacon Target 与一个 Trial。

## 程序集

- [x] Puzzle 仍只引用 Core/Content。
- [x] Interaction 不引用 Puzzle。
- [x] Player/Camera 不引用 Puzzle。
- [x] Editor 依赖仅用于显式 authoring。
- [x] 测试程序集引用已补充。
- [x] 无循环程序集依赖。

## Sprint003A 边界

- [x] 旧“无 InteractionTarget”断言已精确调整。
- [x] 仍禁止 InteractionExecutionProbe 与标准诊断 Probe。
- [x] 仍禁止 Sandbox/P0 Fixture。
- [x] 仍禁止 Root Bridge Gameplay 与 Completion Gameplay。
- [x] 单 Player、单 Camera、FallRecovery 与几何断言保留。
- [x] Main/Advantage Route 的 003A 通行结构未在代码中修改。

## 静态验证

- [x] Content Runtime 静态编译：0 Warning / 0 Error。
- [x] Puzzle Runtime 静态编译：0 Warning / 0 Error。
- [x] Editor Authoring 静态编译：0 Warning / 0 Error。
- [x] 新增/修改测试源码静态编译：0 Warning / 0 Error。
- [x] 无新增 Foundation 文件修改。
- [x] 无新增 Package/Input/ProjectSettings 修改。

## Unity 自动化验证

- [x] Sprint003B EditMode 全部通过：`18/18`。
- [x] 完整 EditMode 全部通过：`100/100`。
- [x] Sprint003B PlayMode 全部通过：`4/4`。
- [x] 完整 PlayMode 全部通过：`54/54`。
- [x] Console Error = 0。
- [x] 稳定状态无持续 Beacon GC.Alloc。

## 人工验收

- [x] A → B 正确完成。
- [x] B → A 产生 IncorrectOrder，B 不激活。
- [x] 错序后 A → B 可恢复并完成。
- [x] 已激活 A/B 重复交互不推进。
- [x] 长按不重复执行。
- [x] A/B 视觉互不污染。
- [x] Prompt 正常。
- [x] Movement 正常。
- [x] Camera 正常。
- [x] FallRecovery 正常。
- [x] Main/Advantage Route 仍可通行。
- [x] Root Bridge、路线、Slice End 未发生 Gameplay 变化。
- [x] Console Error = 0。

## 合并与 Gate

- [x] Unity 生成资产及 `.meta` 已纳入当前 Sprint003B 变更范围。
- [x] `git diff --check` 通过。
- [x] Unity 实测结果已回填 Implementation Report。
- [x] 所有阻塞项关闭后已生成 Sprint003B Gate Review。
- [x] 未开始 Sprint003C。

最终 Unity 验证和人工验收均已完成，Sprint003B 标记为 `PASSED / GO`。

## 2026-08-09 定向修复跟进

- [x] 已记录 Unity 失败根因，未改变 Trial 预期状态语义。
- [x] 初始化不再因异步加载尚未完成而拒绝同 Scene 的有效 Beacon。
- [x] Authoring 会在启用 Root 前持久化并校验正式 Scene 引用。
- [x] 正式 Scene 测试会校验序列化 Definition/Trial/Beacon/Visual 引用和 Slot。
- [x] EditMode fixture 仅在 Trial 有效后配置 Visual。
- [x] Puzzle、Editor、EditMode Tests、PlayMode Tests 静态编译通过。
- [x] Unity Authoring 菜单已执行，正式 Scene 序列化引用已验证。
- [x] Unity EditMode / PlayMode 测试已重跑。
- [x] Unity 确认 Console Error = 0。
- [x] 可进入 Sprint003B Gate Review。

## 最终 Unity 验证回填

- [x] Sprint003B EditMode：`18 Passed / 0 Failed / 0 Skipped`。
- [x] Sprint003B PlayMode：`4 Passed / 0 Failed / 0 Skipped`。
- [x] 完整 EditMode：`100 Passed / 0 Failed / 0 Skipped`。
- [x] 完整 PlayMode：`54 Passed / 0 Failed / 0 Skipped`。
- [x] A → B Completed、B → A → B IncorrectOrder 恢复、重复交互和长按保护已人工验证。
- [x] A/B MaterialPropertyBlock、非颜色 Marker 与 Runtime State 已人工验证为相互独立。
- [x] Movement、GroundDetector、Camera Follow、FallRecovery 与 003A 两条路线回归正常。
- [x] Root Bridge、路线意义、Slice End Trigger 和 Completion UI 未被提前实现。
