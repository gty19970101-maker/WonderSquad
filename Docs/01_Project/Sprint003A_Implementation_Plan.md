# Sprint003A — Sleeping Forest Greybox Implementation Plan

## 1. Sprint 目标

- Unity 基线：`6000.3.21f1`
- 前置里程碑：`v0.7-interaction-foundation`
- 前置验证：EditMode `74/74 Passed`；PlayMode `47/47 Passed`；Console Error `0`
- 场景身份：正式 Gameplay Scene，《沉睡森林》的首个灰盒 Vertical Slice
- 本计划状态：**READY FOR IMPLEMENTATION**

Sprint003A 的唯一目标是建立 `SleepingForest` 正式场景的**空间、路线、视线、出生、探索节奏和未来玩法预留**。它不实现森林信标、根桥状态、完成条件或任何正式 Gameplay 规则。

完成后，玩家应能在正式场景中正常出生、移动和使用 Camera，沿可读灰盒路线探索，看见信标/根桥/终点 Landmark，理解公共主路线、未来优势路线与恢复回路的空间关系，并走到当前灰盒终点。

## 2. 范围与非目标

### 本 Sprint 实现

- 独立于 Test 场景的《沉睡森林》正式灰盒 Scene。
- PlayerSpawner、Player Prefab、Camera Rig、Prompt 场景组合的显式接入。
- 出生区、入口、观察区、信标区、公共主路线、优势路线预留、根桥区预留、恢复回路和当前灰盒终点。
- 用占位几何、简单材质、静态颜色、静态灯光、高度差和 Landmark 组织空间阅读。
- 正式场景专项 EditMode/PlayMode 配置测试与人工走图验收。

### 明确不实现

- `ForestBeaconInteraction`、`ForestBeaconVisual`、`ForestSignalTrial`、`ForestRouteReveal`、`SleepingForestSliceCompletion`。
- `InteractionTarget`、`InteractionPromptSource` 或 `IExecutableInteraction` 在森林 Landmark 上的正式配置。
- Root Bridge 可变状态、完成 Trigger、错误交互、自动恢复或任何影响 Player 的失败逻辑。
- Ability、Network、Inventory、Item、Crafting、Puzzle Framework、Save、AI、Combat、Animation、Audio、VFX、正式美术或正式 Lighting Pass。
- 对 `InteractionDetector`、`InteractionExecutor`、Prompt、`PlayerInputReader`、`PlayerMovement`、Camera Foundation 或已通过核心契约的修改。

## 3. 正式场景与目录策略

### 3.1 推荐路径

创建以下正式内容路径：

```text
Client/Assets/WonderSquad/Scenes/Gameplay/SleepingForest/
  SleepingForest.unity
```

理由：

- 当前项目已有 `Scenes/Bootstrap` 与 `Scenes/Tests`，`Scenes/Gameplay` 可清楚表示正式可玩内容。
- `SleepingForest` 是一张独立小地图，保留单关后续拆分为子场景或内容目录的空间。
- 它不会与 `PlayerSandbox`、`GameplaySandbox` 或 `RecoverySandbox` 混淆。

`PlayerSandbox` 继续只用于 Character/Interaction Foundation 的系统验证；不得复制其 Occluder、Test Probe、DebugCanvas、`P0_` Marker 或其他测试夹具到正式场景。

### 3.2 Build 配置

**003A 不修改 `EditorBuildSettings.asset` 或创建 Build Profile。**

本 Sprint 的正式场景通过 Unity Editor 直接打开并 Play 验证。当前 Build Settings 从 `Bootstrap.unity` 开始且没有关卡加载流程，故不能把“已创建场景”误记为“独立 Build 可玩”。

Sprint003D 再为 Vertical Slice 明确选择以下其一：

1. 以 `SleepingForest.unity` 为首场景的专用 Build Profile；或
2. 经独立审查的最小场景进入流程。

`BootstrapEntry` 采用 `BeforeSceneLoad` 初始化，故直接启动 `SleepingForest.unity` 时仍会初始化；003A 不需要在 Scene 中手工添加 Bootstrap Root。

## 4. 场景根结构与 Foundation 接入

建议 Scene Hierarchy：

```text
SleepingForestRoot
├── PlayerSetup
│   ├── PlayerSpawnPoint
│   └── PlayerSpawner                 (existing Player.prefab reference)
├── Presentation
│   ├── PF_PlayerCameraRig             (one scene instance)
│   └── InteractionPromptCanvas        (existing Prompt View + Presenter)
├── Environment
│   ├── ForestEntrance
│   ├── ObservationArea
│   ├── BeaconArea
│   ├── MainRoute
│   ├── AdvantageRouteReserved
│   ├── RootBridgeAreaReserved
│   ├── RecoveryRoute
│   └── SliceEndArea
└── Lighting
    └── DirectionalLight
```

`SleepingForestRoot` 只用于 Scene 组织，不承担运行时 Manager、规则或全局查找职责。

### 4.1 Player Foundation 配置

| 需求 | 场景配置 | 验收边界 |
| --- | --- | --- |
| 本地玩家 | 现有 `PlayerSpawner` 显式引用现有 `Player.prefab` 与本场景 `PlayerSpawnPoint` | Play 后只生成一个 Player；Spawner 仍只是本地生成器，不是网络 Authority。 |
| 输入与移动 | 不新增或替换 Player Component | Player Prefab 既有 `PlayerInputReader`、`PlayerMovement`、`GroundDetector` 与 CharacterController 继续工作。 |
| 相机 | 单个 `PF_PlayerCameraRig` Scene 实例，Binder 显式引用同一 PlayerSpawner 和现有 CameraFollowSettings | 仅一个 Main Camera；玩家生成后正常绑定，场景不把相机设为 Player 子物体。 |
| Prompt | 现有 Prompt View/Presenter 显式引用同一 PlayerSpawner 与已批准 Input Actions | 003A 没有正式目标时 Prompt 默认隐藏；不复制或改写 Prompt Runtime。 |

不得通过名称、Tag、静态全局 Player 或每帧场景查找建立这些引用。

## 5. 灰盒空间布局

### 5.1 俯视关系图

```text
                                  [Advantage Route Reserved]
                                  高处短路；003A 可见但阻断
                                             |
 [Spawn Area] → [Forest Entrance] → [Observation Area]
                                      /        |        \
                          公共线索回路        |         远望：End Landmark
                                    /          | \
                         [Main Route] → [Beacon A] → [Beacon B]
                                                |             \
                                                |              [Recovery Route]
                                                |               叶垫下坡 → 回到 Observation / Main Route
                                                |
                                  [Root Bridge Area Reserved]
                                   当前：静态临时灰盒通路
                                                |
                                         [Slice End Area]
```

### 5.2 区域表

| 区域 | 空间职责 | 首次可见内容 | 003A 可做 / 不可做 |
| --- | --- | --- | --- |
| Player Spawn Area | 建立安全出生与 Camera 初始构图 | 森林入口、远方高处终点的局部轮廓 | 可正常生成/移动；不可播放教程或触发完成。 |
| Forest Entrance | 从开阔出生区收束为明确前进方向 | 高信标轮廓、入口色块、Observation 方向 | 可用几何/灯光引导；不可加入交互。 |
| Observation Area | 首次同时看见 Beacon Landmark、根桥区和 End Landmark | 两处 Beacon、根桥缺口、终点树台/拱门 | 可放纯视觉符号和远景；不可写正确顺序规则。 |
| Beacon Area | 预留两处未来交互点及其视线/Collider 空间 | Beacon A、Beacon B 与各自 360°交互平台 | 只放灰盒 Landmark 与空 Anchor；不可挂正式 Interaction 组件。 |
| Main Route | 所有未来角色均可使用的公共通路 | Beacon、根桥方向、回退视线 | 003A 始终可走通。 |
| Advantage Route Reserved | 预留未来 AbilityTag 获得的高处短路/信息优势 | 高处平台、封闭但可见入口 | 当前以清晰物理/视觉阻断表示“未开放”，不能成为通关必经路。 |
| Root Bridge Area Reserved | 预留未来“信标影响世界”的可读地点 | 桥根轮廓、缺口、终点方向 | 使用静态中性临时跨越方式让 003A 可走通；不切换状态。 |
| Recovery Route | 为 003C 错误后果预留安全短回路 | 叶垫、下坡或低处回环道路 | 003A 玩家可主动走入并回到主线；没有失败触发。 |
| Slice End Area | 当前走图终点与未来 Completion 区域 | 高大终点 Landmark、回望根桥/Beacon | 只提供空间终点；不触发完成、不显示结算。 |

### 5.3 首次路线节奏

目标是第一次探索约 3～5 分钟，而不是 3～5 分钟连续直线跑步。

| 段落 | 目标时间 | 设计目的 |
| --- | --- | --- |
| Spawn → Entrance | 15～25 秒 | 熟悉构图和前进方向。 |
| Entrance → Observation | 25～40 秒 | 让玩家第一次看到终点、Beacon 与根桥关系。 |
| Observation → Beacon Area | 45～75 秒 | 阅读 Landmark、选择公共路线或观察被阻断的优势路线。 |
| Beacon Area → Root Bridge | 30～45 秒 | 预演未来信标到环境后果的空间距离。 |
| Root Bridge → End | 20～35 秒 | 形成“到达”的收束感。 |
| 可选 Recovery Loop | 15～30 秒 | 验证回路、可见回归点和无软锁。 |

单纯按 4 units/s 直跑的路径长度会短于上述体验时长；探索时长来自 Landmark 观察、路线选择和回望。不要用数百单位的空白跑图凑时长。

## 6. Landmark 与灰盒表现规范

### 6.1 必需 Landmark

| Landmark | 推荐灰盒形态 | 视觉语言 | 位置/视线要求 |
| --- | --- | --- |
| Beacon Landmark | 高 5～7 units 的 Cylinder/Obelisk + 宽底座 | 与环境形状不同的竖向轮廓；静态颜色仅作辅助 | Observation Area 首次可同时看见至少一个完整 Beacon 和另一个方向线索。 |
| Root Bridge Landmark | 3.5 units 宽的弧形根桥轮廓/缺口两端石根 | 明显跨越缺口的横向形状，不能只用颜色 | Observation Area、Beacon Area 与 End 回望中至少两个点可看见。 |
| End Landmark | 高 7～10 units 的树台、拱门或石环 | 最大的竖向地标、独特轮廓、简单静态光色 | Spawn/Observation 至少一个位置能看到局部轮廓；从根桥区域应完整可见。 |

### 6.2 灰盒资源策略

| 资源 | 归属 | 本 Sprint 用途 |
| --- | --- | --- |
| `SleepingForest.unity` | 正式 Gameplay | 正式场景；不是 Placeholder。 |
| 地面、边界、坡道、平台、桥根、叶垫、Landmark Mesh | Greybox Placeholder | 用内置 Primitive 或简单 Mesh 表达空间与通行关系。 |
| 简单材质与颜色 | Greybox Placeholder | 区分入口、Beacon、根桥、回路和终点；必须同时以形状/位置表达。 |
| Directional Light 与少量静态 Light | Greybox Placeholder | 区分安全入口、Beacon 区与路线方向。 |
| `Player.prefab`、Camera Rig、Prompt Prefab、Settings | 既有正式系统资产 | 复用，不复制、不改写。 |

不导入商店资产，不创建复杂 Shader、粒子、动画、音效、昼夜、雾或后处理调优。

## 7. 尺度、CharacterController 与 Camera 约束

现有 Player CharacterController：高度 `1.8`、半径 `0.45`、Slope Limit `45°`、Step Offset `0.3`。现有 Walking Speed 为 `4 units/s`；Camera 使用世界空间 Follow Offset `(0, 6.5, -9)`、固定 Pitch `32°`、FOV `60°`。

### 7.1 建议灰盒尺寸

| 项目 | 建议 | 原因 |
| --- | --- | --- |
| 主路线/桥面最小净宽 | `3.0` units；Root Bridge `3.5` units | 给直径 `0.9` 的 CharacterController 留出转向、相机可读性和未来双人并行余量。 |
| 观察区/信标交互平台 | 直径或最小边长 `6～8` units | 后续两个玩家可站位，当前也不会让 Prompt/Camera 过密。 |
| 普通转角外侧空间 | `4` units 以上 | 固定斜俯视镜头下避免墙体紧贴 Player 后方。 |
| 优势路线入口 | 至少 `2.5` units 宽，但以几何阻断而非不可见墙表示未开放 | 玩家能看见其意义，又不会误以为碰撞错误。 |
| 坡道 | 目标 `≤ 20°`，优先 `≤ 15°`；高度变化 1.0～1.5 units 使用 5～8 units 长坡道 | 虽然 Slope Limit 是 45°，灰盒不应在极限角度测试 Camera/Controller。 |
| 台阶 | 单级 `≤ 0.25` units | 低于 0.3 Step Offset，减少接地抖动风险。 |
| 低顶/吊顶 | 不设置在路线与 Camera 追踪体积内 | 当前 Camera 不是为狭窄顶棚迷宫设计；优先开放天空。 |
| 总体灰盒占地 | 约 `90×70` 至 `120×80` units | 足够容纳视线、路线选择与回路，不成为大地图。 |

### 7.2 Camera First 布局规则

- 先在 PlayerSandbox 保持正常的固定相机设置，再调场景，不改 Camera Foundation 来迁就坏布局。
- Beacon 位置不得只在 Player 正前方很近处；Observation Area 应能在固定斜俯视构图中看到其顶部/轮廓。
- 路线不要使用连续 2 units 宽的高墙走廊、急转的盲角、低天花或狭窄竖井。
- 高低差必须从远处可读；路线边缘需有宽缓坡或实体护边，不能要求跳跃。
- 在 Root Bridge、优势路线入口、Recovery 下坡与 End Area 分别执行 Camera 墙体/高低差/视线回归。

## 8. Interaction Foundation 预留

003A 不配置正式森林信标交互，但每处 Beacon 必须预留：

- 一个命名明确的空 `Transform` Anchor，供 003B 配置未来 Detection Position。
- 半径至少约 `3` units 的无障碍交互平台与无遮挡视线走廊。
- 未来 Trigger Collider 的空间；当前不添加 `InteractionTarget`、`InteractionPromptSource`、`IExecutableInteraction`、Probe 或测试 Collider。
- 未来 Prompt 出现时不被 Beacon Mesh、墙体或 Camera 关键构图遮挡的位置。

`InteractionExecutionProbe` 仍是诊断夹具，绝不能作为正式森林信标或复制进 `SleepingForest.unity`。

## 9. 自动化测试计划

### 9.1 EditMode（003A 专项）

测试只验证正式场景结构和 Foundation 接入，不覆盖 003B Gameplay：

- `SleepingForest.unity` 存在于 `Scenes/Gameplay/SleepingForest/` 的正式路径。
- 场景根有唯一 `PlayerSpawner` 与唯一 `PlayerSpawnPoint`，且 Spawn 配置有效。
- 场景包含唯一 Main Camera / Camera Rig，且其 Binder 引用同一个 PlayerSpawner。
- 场景包含有效 Prompt Presenter/View 组合，且其 Spawner 引用正确。
- 场景有 Entrance、Observation、Beacon、Main、Advantage、Root Bridge、Recovery、End 的明确区域标记。
- 两处 Beacon 预留 Anchor 存在，但场景不含 `InteractionExecutionProbe`、`PF_InteractionProbe`、`P0_` 对象、`DebugCanvas`、Test Probe 或测试墙。
- 场景中没有 `ForestBeaconInteraction`、RootBridge 状态组件或 Completion Trigger；003A 不提前实现 003B/003D。
- Build 配置准备状态被明确记录：场景尚未加入现有 Build Settings，此为 003A 预期，而非失败。

### 9.2 PlayMode（003A 专项）

加载 `SleepingForest.unity`，只验证：

```text
Scene Load
→ PlayerSpawner
→ one Player instance
→ Camera Target Binder
→ PlayerMovement
→ Prompt stays hidden with no formal target
```

最少测试：

- 场景加载后玩家只生成一次；重复帧或绑定过程不产生第二玩家。
- Player 具备可用 CharacterController、PlayerMovement 与 GroundDetector，且能在主路线静止/移动。
- 场景只有一个有效 Main Camera，Camera Binder 成功跟随本地 Player。
- Beacon Placeholder 不被检测为可交互目标，不产生 Prompt，也不接受执行。
- 从 Spawn 沿公共路线、Recovery Loop 和临时根桥通路均不会穿地、卡住或抛异常。
- 现有 Character/Interaction Foundation 完整回归仍通过，Console Error = `0`。

## 10. 人工验收计划

在正式 `SleepingForest.unity` 中执行，不得替代为 PlayerSandbox：

1. 打开 `SleepingForest.unity`，确认它位于 `Scenes/Gameplay/SleepingForest/`。
2. Play，确认玩家在 Spawn Area 生成，且只生成一个。
3. 确认一个 Main Camera 正常绑定；WASD 沿完整路线移动无异常。
4. 穿过 Forest Entrance，从 Observation Area 看见至少一个 Beacon、Root Bridge 区域和 End Landmark 的预期视线。
5. 走 Main Route 到 Beacon Area，确认两处未来信标位置有足够交互平台和无遮挡空间，但不显示 Prompt、不执行交互。
6. 观察 Advantage Route Reserved：入口可见、当前清楚阻断、不会误导为唯一通路。
7. 主动走 Recovery Route，确认它能在 10～30 秒内回到 Observation/Main Route，不存在死路或必须重启的情况。
8. 通过静态临时根桥通路，确认 Root Bridge Area 的宽度、Camera 视野与高度差安全。
9. 到达 Slice End Area，确认其空间和 Landmark 明确，但不显示完成状态、不触发切关。
10. 全程检查狭窄墙体、下坡、Root Bridge、优势路线入口与 End Area 的 Camera；以关卡布局修复可见性问题。
11. 第一次探索（含观察与 Recovery Loop）约 3～5 分钟；Console Error = `0`。

## 11. 实施文件与资产清单

### 11.1 本 Sprint 预期新增/修改

| 文件/资产 | 操作 | 类型 | 用途 |
| --- | --- | --- | --- |
| `Client/Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity` | 新增 | 正式 Gameplay Scene | 003A 灰盒空间与现有 Foundation 的场景组合。 |
| 对应 `.meta` 及目录 `.meta` | 新增 | Unity 元数据 | 保持 GUID/版本控制完整。 |
| `Client/Assets/WonderSquad/Tests/EditMode/SleepingForestGreyboxEditModeTests.cs` | 新增 | 测试 | 验证 Scene 路径、结构、无测试夹具与配置。 |
| `Client/Assets/WonderSquad/Tests/PlayMode/SleepingForestGreyboxPlayModeTests.cs` | 新增 | 测试 | 验证正式场景生成、Camera、移动和无交互占位。 |
| 对应测试 `.meta` | 新增 | Unity 元数据 | 版本控制完整。 |
| `Docs/01_Project/Sprint003A_Implementation_Report.md` | 新增 | 文档 | 实施范围、实际文件、Unity 结果、限制。 |
| `Docs/01_Project/Sprint003A_Review_Checklist.md` | 新增 | 文档 | Merge 前范围、架构、测试与资产审计。 |
| `CHANGELOG.md` | 修改 | 文档 | 在 Unreleased 记录正式灰盒场景。 |

### 11.2 可能新增的灰盒 Prefab / Material

默认优先在场景中直接使用 Primitive，避免只为单次灰盒创建资产。只有相同 Landmark 在同一场景反复出现时，才允许新增：

```text
Prefabs/SleepingForest/Greybox/PF_SF_BeaconLandmark_Greybox.prefab
Prefabs/SleepingForest/Greybox/PF_SF_RouteMarker_Greybox.prefab
Art/Materials/Greybox/M_SF_Greybox_*.mat
```

这些均是 **Greybox Placeholder**，不承载 Interaction、执行、状态、完成或网络逻辑。003A 不创建 `ForestBeacon` 正式 Prefab，也不创建 Root Bridge 状态 Prefab。

## 12. Definition of Done

Sprint003A 只有同时满足以下条件才可标记完成：

- [ ] `SleepingForest.unity` 正式场景存在，且与 `Scenes/Tests/` 明确分离。
- [ ] 现有 Bootstrap、PlayerSpawner、Player Prefab、PlayerInputReader、PlayerMovement、Camera Rig 和 Prompt 通过显式引用正常工作。
- [ ] Play 后仅有一个 Player 和一个有效 Main Camera；Camera 正确绑定。
- [ ] Spawn、Entrance、Observation、Beacon、Main、Advantage、Root Bridge、Recovery、End 九类区域均可读。
- [ ] Main / Advantage / Recovery 的空间关系成立；公共路线始终可走，Recovery 无软锁。
- [ ] Beacon、Root Bridge 与 End Landmark 的位置、视线和未来交互/完成空间明确。
- [ ] Beacon Placeholder 没有正式 Interaction 或 Gameplay 逻辑；Root Bridge 不切换状态；End 不触发完成。
- [ ] 灰盒首次探索约 3～5 分钟；关键 Camera 点、宽度、坡度和高度差通过人工走图。
- [ ] 003A 专项 EditMode / PlayMode 测试、既有完整回归和 Console 检查均通过。
- [ ] 未修改 Character / Interaction Foundation、Input Actions、Build Settings、Package、ScriptableObject Settings 或 PlayerSandbox。

## 13. 实施后停止点

003A 完成后必须停止并进行 Gate/人工 Unity 验收。森林信标的真实执行逻辑、根桥环境状态和完成触发属于 **Sprint003B 及以后**，不得提前实现。

## 14. 最终状态

# READY FOR IMPLEMENTATION
