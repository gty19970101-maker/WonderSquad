# Sprint003A — Sleeping Forest Greybox Implementation Report

## 最终状态

`PASSED`

- Unity：`6000.3.21f1`
- Sprint：Sprint003A — Sleeping Forest Greybox
- 正式场景：`Client/Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity`
- Gate：`GO`

## 实施结果

Sprint003A 已建立并验证《沉睡森林》正式 Greybox：玩家能够生成，经 Observation、Beacon 区域和 Root Bridge 到达 Slice End；跌出承载面后可恢复并继续移动。场景保持为纯 Greybox，不包含 Beacon Gameplay、Root Bridge Gameplay、Puzzle、角色能力或 Network。

## 场景结构

```text
SleepingForestRoot
├── PlayerSetup
│   ├── PlayerSpawnPoint
│   ├── PlayerSpawner
│   ├── RecoveryPoint
│   └── SleepingForestFallRecovery
├── Presentation
│   ├── PF_PlayerCameraRig
│   └── InteractionPromptCanvas
├── Environment
│   ├── Terrain
│   ├── MainRoute
│   │   └── JunctionPlatforms
│   ├── AdvantageRouteReserved
│   ├── RecoveryRoute
│   │   └── JunctionPlatforms
│   └── Boundaries
├── Landmarks
├── Gameplay
└── Lighting
```

## 路线与几何

Main Route 采用平台承接、路段止于边缘的结构：

```text
Spawn → Entrance → Observation
→ Beacon A → Northwest Junction → Beacon B
→ Root Bridge Entrance Junction → Root Bridge → Slice End
```

- Main Route 宽度为 `6m`。
- Beacon 与 Junction 平台为 `8 × 1 × 8m`。
- Temporary Root Bridge 为 `7 × 1 × 16m`。
- 所有可走表面顶部为 `Y=0`，不使用统一 Y 偏移。
- Renderer 与 BoxCollider 的严格正体积交叠审计结果为 `0`。
- 人工检查未发现 Z-fighting、CharacterController 卡顿或 GroundDetector 抖动。

Main Route 与 `AdvantageRouteReserved` 在当前 Greybox 阶段都存在可物理抵达终点的路径。这是有意保留的空间结构；Sprint003A 不增加门、阻挡、能力检查、Beacon 状态或 Puzzle 逻辑。路线差异与玩法意义留给 Sprint003B/003C。

## Fall Recovery

`SleepingForestFallRecovery` 是场景局部、可删除的 Greybox 安全组件：

- 通过显式 `PlayerSpawner` 事件绑定当前本地 Player。
- 通过显式 `RecoveryPoint` 提供安全恢复位置。
- 玩家低于 `fallYThreshold = -8` 时，临时停用 CharacterController、设置安全 Pose，再恢复原启用状态。
- 不每帧搜索 Player，不使用静态全局 Player。
- 不修改 PlayerMovement、GroundDetector、PlayerSpawner、Camera 或 Input。
- 不建立通用死亡、Respawn、Checkpoint、Save 或 Network 框架。

## Builder 安全规则

- `Create Missing Sleeping Forest Greybox`：Scene 存在时跳过。
- `Rebuild Sleeping Forest Greybox (Development Only)`：执行保存确认与覆盖确认后才重建。
- 没有 Unity 启动、脚本编译、Domain Reload 或 Play Mode 自动重建入口。
- Builder 已同步最终 Junction、Route、Recovery、Advantage 和 Root Bridge 几何。
- 本次最终收尾未修改 Scene YAML。

## Unity 自动化结果

测试在 Unity `6000.3.21f1` 中使用与当前工程相同的 `Assets`、`Packages` 和 `ProjectSettings` 临时副本运行，以避免与已打开的正式工程争用。

| 范围 | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Sprint003A EditMode | 8 | 0 | 0 |
| Sprint003A PlayMode | 3 | 0 | 0 |
| 完整 EditMode | 82 | 0 | 0 |
| 完整 PlayMode | 50 | 0 | 0 |

专项 PlayMode 验证：

- 单个 PlayerSpawner 和单个 PlayerMovement。
- 单个带 `MainCamera` Tag 的 Camera。
- CameraTargetBinder 成功绑定。
- FallRecovery 绑定 Player、恢复一次、恢复后仍可移动。
- Scene 不包含提前实现的 Interaction Target 或 Completion Gameplay。

## 人工验收结果

- [x] SleepingForest Greybox 成功生成并进入 Play Mode。
- [x] Spawn → Observation → Beacon 区域 → Root Bridge → Slice End 完整走通。
- [x] Main Route 与 Advantage Route Reserved 都可以物理抵达终点。
- [x] Fall Recovery 正常，恢复后移动与接地正常。
- [x] Camera Follow 正常。
- [x] 几何无可见闪烁。
- [x] 路面和平台交界无明显卡顿或 GroundDetector 抖动。
- [x] Console Error `0`。

## 范围审计

- Character Foundation：未修改。
- Interaction Foundation：未修改。
- PlayerMovement、GroundDetector、Camera Foundation：未修改。
- 未实现 Beacon、Root Bridge、Puzzle、Ability 或 Network Gameplay。
- 仅新增 SleepingForest Greybox 内容适配、Builder、Scene、材料、测试和文档。

## 已知限制

- Fall Recovery 不是未来正式失败/救援方案。
- 当前只验证本地单 Player，不实现 Network Authority。
- 003A 不赋予 Main/Advantage Route 不同玩法意义。
- Build Profile、正式 Completion、Beacon Gameplay 和环境后果仍属于后续 Sprint。

Sprint003A 已满足 Definition of Done，最终状态为 `PASSED / GO`。
