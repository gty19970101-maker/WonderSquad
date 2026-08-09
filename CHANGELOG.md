# Changelog

本文件记录 Wonder Squad 的重要变更。

格式遵循 [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)，版本命名在 MVP 阶段采用项目里程碑标识。

## [Unreleased]

### Added

- 项目治理文档与开发工作流。
- Sprint001-A 本地 Player Spawn：基础 Player Prefab、PlayerSpawnPoint 与 Sandbox PlayerSpawner。
- PlayerSandbox 启动时生成单个本地 Player，并支持销毁后重新生成。
- Player Spawn 的 EditMode 配置测试、PlayMode 行为测试与 P0 回归验证。
- Sprint001-B Player Input Reader：复用 New Input System 的 Gameplay/Move Action，支持 WASD、方向键、输入限幅和启停生命周期。
- PlayerSandbox 的输入变化调试日志，以及输入配置、限幅、生命周期、重复订阅和 Player 静止状态测试。
- Sprint001-C Character Controller Movement：数据化水平移动、角色朝向、地面状态和重力。
- Player Prefab 的 CharacterController、GroundDetector、PlayerMovement 与 MovementSettings 配置，以及对应 EditMode、PlayMode 测试。
- Sprint001-D Controlled Third-Person Camera：基于 Cinemachine `3.1.7` 的固定斜俯视跟随、独立 Camera Target、运行时本地玩家绑定和数据化相机参数。
- PlayerSandbox 的 Camera Rig、CameraValidationRoot，以及玩家生成、销毁、重新生成、跟随稳定性、固定朝向和单相机约束测试。
- Sprint002A Interaction Detection：Core 只读 `IInteractable` 契约、稳定目标 ID、非分配范围查询、距离/Layer/射线遮挡验证及确定性目标选择。
- 数据化 `InteractionSettings`、`Interactable` Layer、Player 检测锚点、PlayerSandbox 临时测试目标，以及对应 EditMode/PlayMode 测试。
- Sprint002B Interaction Prompt：不可变 Prompt 数据、静态 Prompt Definition/Source、目标变化 Presenter、只显示的 uGUI View，以及键鼠/手柄 Binding 显示缓存。
- PlayerSandbox 的只读 Prompt 验证 UI、双目标切换环境，以及覆盖显示、隐藏、遮挡、生命周期、设备切换、只读行为和稳定帧分配的 EditMode/PlayMode 测试。
- Sprint002C Interaction Execution：独立 `IExecutableInteraction` 契约、稳定 PlayerId/RequestId、不可变 Context/Request/Result、独立 Interact 输入读取、本地请求端口、执行前重新验证与 RequestId 去重。
- PlayerSandbox ExecutionProbe，以及覆盖单次按压、长按抑制、释放重置、距离、Layer、遮挡、销毁、组件生命周期和 Prompt 只读边界的 EditMode/PlayMode 测试。
- Sprint002D Standard Interaction Probe：新增组合式 `PF_InteractionProbe` 参考模板，由 InteractionTarget、Prompt Source、双态执行行为和最小视觉状态组件构成；不引入通用交互基类、正式机关或网络逻辑。
- PlayerSandbox 中新增标准 Probe 参考实例，以及覆盖双态切换、请求结果、Prefab 完整性、Prompt、长按抑制、遮挡、销毁和 Player/Camera 回归的专项 EditMode/PlayMode 测试。
- Sprint003A《沉睡森林》正式 Greybox：独立 Gameplay Scene、Spawn/Observation/Beacon/Main Route/Root Bridge/Recovery/End 空间、可确认重建的 Editor Builder 与专项测试。
- Sprint003A 场景局部 `SleepingForestFallRecovery`：通过显式 PlayerSpawner 和 RecoveryPoint 引用，在玩家跌出灰盒后恢复至安全地面；不建立通用 Respawn 或 Checkpoint Framework。
- Sprint003B Forest Signal Trial：新增只读 Beacon Definition、Scene 实例级 Trial、A→B 顺序状态、IncorrectOrder 恢复、确定性 Revision、只读 Snapshot 与实例事件。
- Sprint003B Forest Beacon 组合内容：复用现有 Detection、Prompt 与 Execution 契约，新增独立 Beacon 执行适配、MaterialPropertyBlock 视觉和显式 Unity authoring 命令；不修改 Interaction/Character Foundation。
- Sprint003B EditMode/PlayMode 测试：覆盖正确顺序、错序恢复、重复请求、Scene 来源验证、双实例视觉隔离、Prompt/Input 链路和 Sprint003A 边界回归。
- Sprint003C Root Bridge Advantage / Environment Consequence：新增只读 Trial Snapshot 消费组件、独立 `RootBridgeAdvantageSpan` 与实例级 MaterialPropertyBlock 视觉；永久主桥保持安全且始终可通行。
- Sprint003D Sleeping Forest Slice Completion：新增场景局部双条件 Completion Controller、正式 Player Slice End Trigger、readonly Snapshot/Revision 边界，以及不阻挡输入的 Requirement/Completion uGUI 反馈与无 Collider 世界 Marker。
- Sprint003D 显式幂等 Authoring 与专项测试：生成唯一 Completion 组合和 `PF_SleepingForestCompletionFeedback`，覆盖 Trial-first、Player-first、IncorrectOrder 恢复、重复触发、Player 身份、UI 生命周期和 Foundation 回归。

### Changed

- Sprint003D 已通过 Unity `6000.3.21f1` 正式验收：Sprint003C/003D 专项及完整 EditMode/PlayMode 全部 Passed、`0 Failed`、`0 Skipped`，Console Error `0`；仓库未保存本次 Test Runner XML，精确数量标记为 `TEST COUNT REQUIRES MANUAL RECORD`，不复用旧计数。A—G Completion 流程、Main/Advantage Route、IncorrectOrder、FallRecovery 与反馈闭环均通过，最终状态为 `PASSED / GO — VERTICAL SLICE COMPLETE`。

- Sprint003C 原 Unity 验收基线为专项 EditMode `8/8`、专项 PlayMode `4/4`、完整 EditMode `108/108`、完整 PlayMode `58/58`；Sprint003D 人工走图随后发现旧 Advantage Span 与永久主桥职责近似重合，该 `PASSED / GO` 已由定向空间修复状态取代。
- Sprint003C 第一轮空间分离虽通过自动测试，但正式 Unity 复验确认 `32.06m` 对 `27.00m` 的两条路线仍长距离并排，旧 Guardrail 也成为无意义长条；该布局已被路线可读性修复取代。
- Sprint003C Advantage Route 可读性修复曾在隔离 Unity `6000.3.21f1` 通过专项 EditMode `9/9`、专项 PlayMode `5/5`、完整 EditMode `122/122`、完整 PlayMode `63/63`。永久 Main Route 改为约 `46.24m` 的三段折线绕行，Advantage Route 保持 `27.00m` 直连并跳过完整外侧长段和两次转向；这些计数是最终可见 Geometry 清理前的历史记录。
- Sprint003C 可见 Geometry 精确清理定位并删除了 `SleepingForestRoot/Landmarks/RootBridgeLandmark_LeftRoot` 与 `RootBridgeLandmark_RightRoot`；二者是 003A Builder 独立创建、上一轮 003C Authoring 明确保留的装饰 Cube，并非已删除 Guardrail。Builder、Authoring、正式 Scene 和走廊回归测试已同步；其待验证状态随后由正式 Unity 验收关闭。
- Sprint003C Visible Geometry Cleanup 已通过正式 Unity 人工复验：废弃 Root Landmark 不再存在，重复 Apply 不恢复对象，无空气墙、隐形 Collider、Z-fighting 或接地抖动；`46.24m` Main Route 与 `27.00m` Advantage Route 均正常，结论为 `SPRINT003C VISIBLE GEOMETRY CLEANUP — VERIFIED`。
- Sprint003B Forest Signal Trial 已通过 Unity `6000.3.21f1` 最终验收：专项 EditMode `18/18`、专项 PlayMode `4/4`、完整 EditMode `100/100`、完整 PlayMode `54/54` 均通过，Console Error 为 `0`。A → B、IncorrectOrder 恢复、重复保护、双 Beacon 视觉隔离与 Sprint003A/002 回归均正常，最终状态为 `PASSED / GO`。

- Sprint001-A Player Spawn 已通过 Unity `6000.3.21f1` 人工验收：PlayerSandbox、胶囊生成、出生位置、单实例和全部测试均正常，Console Error 为 0。
- Sprint001-C Character Controller Movement 已通过 Unity `6000.3.21f1` 最终验收：WASD、停止、转向、斜向限速、重力、接地、禁用与重新启用均正常，Console Error 为 0；EditMode 32/32、PlayMode 13/13 通过。
- Sprint001-D Controlled Third-Person Camera 已通过 Unity `6000.3.21f1` 最终验收：EditMode 37/37、PlayMode 19/19 通过；WASD 跟随、固定斜俯视、停止稳定性、墙体、门洞、高低差、Canopy 与单 Main Camera 人工检查均正常，Console Error 为 0，最终状态为 `PASSED`。
- Sprint002A Interaction Detection 已通过 Unity `6000.3.21f1` 最终验收：EditMode 44/44、PlayMode 26/26 通过；范围检测、清除、遮挡恢复、Layer、Trigger 与 Character Foundation 人工回归均正常，Console Error 为 0，最终状态为 `PASSED`。
- 将已解析的 `com.unity.ugui` `2.0.0` 提升为 manifest 直接依赖，未修改其他 Package 版本。
- Sprint002B Interaction Prompt 已通过 Unity `6000.3.21f1` 最终验收：专项 EditMode 11/11、专项 PlayMode 11/11、完整 EditMode 55/55、完整 PlayMode 37/37 均通过；Prompt 生命周期、遮挡恢复、多目标切换、键鼠/手柄 Binding、只读交互、WASD/Camera 回归与稳定帧 GC 人工检查均正常，Console Error 为 0，最终状态为 `PASSED`。
- Sprint002C Interaction Execution 已通过 Unity `6000.3.21f1` 最终验收：专项 EditMode 11/11、专项 PlayMode 7/7、完整 EditMode 66/66、完整 PlayMode 44/44 均通过；ExecutionProbe、单次按压、长按抑制、释放重置、无目标、超距、遮挡、目标销毁、Prompt/Execution 一致性及 Movement/Camera/Prompt 回归均正常，Console Error 为 0，最终状态为 `PASSED`。
- Sprint002D Standard Interaction Probe 已通过 Unity `6000.3.21f1` 最终验收：专项 EditMode 8/8、完整 EditMode 74/74、专项 PlayMode 8/8、完整 PlayMode 47/47 均通过；双 Probe 独立检测、Prompt、执行、双态切换、长按抑制、范围/遮挡拒绝与 Character Foundation 回归均正常，Console Error 为 0，最终状态为 `PASSED`。
- Sprint003A Sleeping Forest Greybox 已通过 Unity `6000.3.21f1` 最终验收：专项 EditMode 8/8、专项 PlayMode 3/3、完整 EditMode 82/82、完整 PlayMode 50/50 均通过；正式场景走通、跌落恢复、单 Player、单 Main Camera、Camera Follow、接地和几何人工检查正常，Console Error 为 0，最终状态为 `PASSED / GO`。
- Sprint003A 保留 Main Route 与 Advantage Route Reserved 均可物理抵达终点的 Greybox 结构；路线差异、Beacon 状态和 Gameplay 阻挡留待 Sprint003B/003C。
- Sprint003A 的“正式 Scene 不含 InteractionTarget”边界测试调整为仅允许两个稳定 ID 的 Forest Beacon，同时继续禁止 Test Probe、Sandbox、Root Bridge Gameplay 与 Completion Gameplay。

### Fixed

- 修复 Sprint003C 路线可读性：删除悬空/堵路的两条旧 Guardrail，将永久主路重排为东向、外侧北向、折返 Slice End 的三段安全路径；绿色 Span 在 Completed 后直连入口与终点。Authoring、Prefab、Scene、003A/003C Bounds 和路线连续性测试已同步，未修改 Completion 或 Foundation。
- 修复 Sprint003C 绿色捷径两侧仍可见的两条棕色长条：精确删除无 Gameplay 职责的 `RootBridgeLandmark_LeftRoot` / `RightRoot` 及其 BoxCollider，移除 003A Builder 创建源，并在 003C Authoring 中加入精确、幂等的旧 Scene 清理；不改变 Main/Advantage 路线、Completion 或 Foundation。

- 修复 Sprint003C EditMode Test 中 `System.Object` 与 `UnityEngine.Object` 的 `CS0104` 命名歧义；Unity API 调用显式限定为 `UnityEngine.Object`，未改变测试逻辑。
- 修复 Root Bridge Span/Visual 首次状态同步可能被 enum 默认值跳过的问题；生命周期显式应用 Initial 状态，保持 Renderer、Collider 与 Marker 一致。
- 修复 Sprint003C EditMode Fixture 在 inactive 根对象上 Configure 导致未订阅 Trial 实例事件的问题；调整为激活 Fixture 后 Configure 并读取 Initial Snapshot。该问题仅属于测试夹具生命周期，不是 ForestSignalTrial、Revision、Beacon 或 Interaction Foundation 缺陷。
- Sprint003B targeted verification fix: formal Forest Signal Trial authoring now persists and validates Trial/Beacon references before enabling the hierarchy; early scene initialization no longer rejects otherwise-valid same-scene references. Unity Test Runner rerun is still required.

- 修复《沉睡森林》Main Route 的 Beacon B 至 Root Bridge 横向断口、4 米窄路和抬高信标底座造成的不可通行问题；主路线改为 6 米宽、由 Beacon/Junction 平台承接的边缘终止路段，临时根桥扩宽并增加局部护栏。
- 修复 Sleeping Forest Greybox 中 Main/Recovery/Advantage/Root Bridge 的 20 组实际 Cube 体积相交；移除统一 Y 偏移方案，Renderer 与 BoxCollider 正体积交叠回归测试通过，人工复查无 Z-fighting、卡顿或接地抖动。
- 修复 SleepingForest Greybox 玩家离开路面后无限下落的问题；低于 `Y=-8` 时由场景局部恢复组件送回显式 `RecoveryPoint`，Character、Interaction、Camera 与 Input Foundation 保持不变。
- Sprint002D Interaction Probe 视觉状态改用每 Renderer 的 `MaterialPropertyBlock` 覆盖颜色，避免 EditMode 调用 `Renderer.material` 生成泄漏材质、修改共享 Material 或让多个 Probe 共用视觉状态。
- 使用 `FormerlySerializedAs("spawnOnStart")` 将 `PlayerSpawner` 的序列化布尔字段安全迁移为 `shouldSpawnOnStart`，保留原默认值、运行逻辑及 Unity 资产中的序列化值，并关闭 Sprint001 Gate 命名门禁项。
- 修复 `OccludedTarget_IsNotDetectedAndRecoversWhenClear` 与 PlayerSandbox 既有 `P0_VisibleMarker` Collider 重叠造成的测试隔离问题；仅调整测试射线路径，未修改 Interaction Detection 生产逻辑。

## [v0.1-P0] - 2026-08-01

### Added

- 完成 P0 项目初始化和 Unity 工程骨架。
- 锁定 Unity `6000.3.21f1`、Universal 3D / URP、Input System 与测试框架。
- 建立 Runtime、Editor、Tests、Scenes、Prefabs、ScriptableObjects、UI、Art 和 Audio 目录。
- 建立模块 Assembly Definition、命名空间边界、Bootstrap、日志接口和项目配置入口。
- 建立输入动作资产、三个独立 Sandbox、基础 EditMode 与 PlayMode 测试。
- 建立 v1.1 游戏设计、MVP 开发计划、技术架构和系统设计文档库。
- 完成《沉睡森林》关卡设计、原型任务分解和 P0 审查报告。
- 建立 Git 仓库并连接远程仓库。

### Fixed

- 修复 P0 场景黑屏所需的 Camera、灯光、占位环境和 URP 绑定。
- 修复程序集边界、Bootstrap 生命周期、输入配置和内容引用校验问题。

[Unreleased]: https://github.com/gty19970101-maker/WonderSquad/compare/v0.1-P0...HEAD
[v0.1-P0]: https://github.com/gty19970101-maker/WonderSquad/releases/tag/v0.1-P0
