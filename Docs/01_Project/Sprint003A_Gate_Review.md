# Sprint003A — Sleeping Forest Greybox Gate Review

## Gate 结论

`GO`

Sprint003A 专项测试、完整回归与正式 Scene 人工验收均已通过，可以将 Sprint003A 标记为 `PASSED`。本结论只关闭 Sleeping Forest Greybox Gate，不授权自动开始 Sprint003B。

## 审查基线

- Unity：`6000.3.21f1`
- 前置稳定里程碑：`v0.7-interaction-foundation`
- 正式 Scene：`Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity`
- Sprint003A 范围：正式 Greybox、局部 Fall Recovery、受控 Builder、专项测试与文档

## 自动化证据

测试使用 Unity `6000.3.21f1`，在包含当前 `Assets`、`Packages` 和 `ProjectSettings` 的隔离临时工程中运行，避免与已打开的正式工程争用。测试完成后临时执行环境不构成项目产物。

| 测试范围 | Passed | Failed | Skipped | 结论 |
| --- | ---: | ---: | ---: | --- |
| Sprint003A EditMode | 8 | 0 | 0 | 通过 |
| Sprint003A PlayMode | 3 | 0 | 0 | 通过 |
| 完整 EditMode | 82 | 0 | 0 | 通过 |
| 完整 PlayMode | 50 | 0 | 0 | 通过 |

关键专项测试：

- `SleepingForestScene_AuditedGeometryHasNoPositiveVolumeOverlap`
- `SleepingForestScene_HasValidFoundationConfiguration`
- `SleepingForestScene_HasValidLocalFallRecoveryConfiguration`
- `SleepingForest_PlayerSpawnsOnceAndCameraBinds`
- `SleepingForest_FallenPlayerRecoversAndCanMove`
- `SleepingForest_HasNoActiveInteractionOrCompletionGameplay`

## Gate 检查

### 单 Player

`通过`

- Scene 中只有一个 `PlayerSpawner`。
- Spawn 后只有一个 `PlayerMovement` 实例。
- 额外等待一帧后仍保持单实例。
- 人工 Play Mode 未发现重复 Player。

### 单 Main Camera

`通过`

- PlayMode 测试确认场景只有一个 Camera，且带 `MainCamera` Tag。
- `CameraTargetBinder` 成功绑定当前 Player。
- 人工验收确认 Camera Follow 正常。

### Fall Recovery

`通过`

- 场景只有一个有效 `SleepingForestFallRecovery`。
- Recovery 显式绑定 PlayerSpawner 与 RecoveryPoint。
- 玩家跌破阈值后只恢复一次。
- CharacterController 和 PlayerMovement 在恢复后保持启用，输入移动与 Camera 绑定继续有效。
- 人工跌落与恢复回归通过。

### Scene 几何

`通过`

- 27 个关键 Renderer/BoxCollider 对象的严格正体积交叠测试通过。
- 已记录并结构性消除旧布局 20 组 Cube 体积相交。
- 人工检查无 Z-fighting、明显 CharacterController 卡顿或 GroundDetector 抖动。
- Spawn → Observation → Beacon → Root Bridge → Slice End 完整走通。

### Foundation 边界

`通过`

- 未修改 PlayerMovement、GroundDetector、PlayerSpawner、PlayerInputReader 或 Camera Foundation。
- 未修改 InteractionDetector、Prompt、Executor 或已通过 Gate 的 Interaction 契约。
- `WonderSquad.SleepingForest.Greybox` 只依赖 `WonderSquad.Player` 的公开生成边界。
- Player、Camera 和 Interaction 程序集没有反向引用 SleepingForest Greybox。
- 完整 EditMode/PlayMode 回归全部通过。

### MVP 与 Sprint 范围

`通过`

- 没有 Beacon Gameplay、Root Bridge Gameplay、Puzzle、Ability、Network、Save 或 Inventory。
- 没有通用 Respawn、Checkpoint、Level Completion 或 Puzzle Framework。
- `SleepingForestFallRecovery` 明确为场景局部、后续可删除的 Greybox 安全组件。

## 路线边界特别记录

当前 Main Route 与 Advantage Route Reserved 在 Greybox 阶段都可以物理抵达终点。这是允许且已验收的空间结构。

Sprint003A 不通过以下方式改变它：

- 门或临时封堵；
- Beacon 激活条件；
- Root Bridge 状态；
- 角色能力检查；
- Puzzle 或完成条件。

路线差异、策略价值和环境后果属于 Sprint003B/003C 的独立设计与 Gate。

## 已知非阻塞项

- Fall Recovery 尚不是正式欢乐失败/救援系统。
- SleepingForest 尚未承担 Sprint003D 的 Build Profile 与正式 Completion 接入。
- 当前只验证本地单 Player；Host Authority 仍保持文档边界，未实现 Network。
- Greybox 使用 Placeholder 材料、灯光和几何，不代表最终美术质量。

## 最终决定

没有发现需要在进入后续 Gameplay Object 阶段前重构的 Sprint003A Gate 阻塞项。

**最终结论：`GO`**
