# Sprint001-A Player Spawn 实施报告

## 1. Sprint 目标

Sprint001-A 实现 PlayerSandbox 使用的最小本地玩家生成闭环：

1. 基础 Player Prefab。
2. Player Spawn Point 组件。
3. 本地 Sandbox PlayerSpawner。
4. PlayerSandbox 启动后生成一个 Player。

本 Sprint 不包含 Movement，也不建立未来 Network Spawn 系统。

---

## 2. 实施结果

状态：

**PASSED**

Unity 版本：

```text
6000.3.21f1
```

分支：

```text
feature/sprint001-character-controller-v2
```

---

## 3. 新增内容

### 3.1 Player Prefab

文件：

```text
Client/Assets/WonderSquad/Prefabs/Player/Player.prefab
```

结构：

```text
Player
└── VisualRoot
    └── VisualPlaceholder
```

规则：

- `Player` 根节点保留给后续经过批准的 Player 组件。
- `VisualRoot` 隔离视觉表现与未来 Gameplay 根组件。
- `VisualPlaceholder` 使用 Unity 内置 Capsule Mesh 和默认 URP 材质。
- Placeholder 不包含 Collider，不取得未来移动或物理所有权。
- Prefab 不包含 PlayerController、Input、Camera、Animator、Interaction、Inventory、Ability、Puzzle 或 Network 组件。

### 3.2 PlayerSpawnPoint

文件：

```text
Client/Assets/WonderSquad/Runtime/Player/Spawning/PlayerSpawnPoint.cs
```

职责：

- 标记本地 Sandbox 的 Player 生成位置和旋转。
- 只公开 Transform 派生的只读 `Position` 与 `Rotation`。
- 不负责实例化、移动、网络权威或关卡逻辑。

命名空间：

```text
WonderSquad.Player.Spawning
```

程序集：

```text
WonderSquad.Player
```

### 3.3 PlayerSpawner

文件：

```text
Client/Assets/WonderSquad/Runtime/Player/Spawning/PlayerSpawner.cs
```

职责：

- 保存本地 Player Prefab 与 PlayerSpawnPoint 引用。
- PlayerSandbox 启动时尝试生成一个本地 Player。
- 已有存活 Player 时拒绝重复生成，并返回现有实例。
- Player 被销毁且 Unity 引用失效后，允许再次调用 `TrySpawn` 生成替代实例。
- 缺少 Prefab 或 SpawnPoint 时输出明确错误并安全返回。

明确边界：

- 不引用或查找 PlayerController。
- 不引用 Network、Fusion 或其他联网类型。
- 不处理输入、移动、摄像机、动画或 Gameplay 系统。
- 它是 PlayerSandbox 测试环境中的本地生成器，不是未来的网络生成权威。
- 未来接入 Network Spawn 时应建立独立 Network 适配器，不扩展本组件承担网络职责。

---

## 4. PlayerSandbox 配置

修改文件：

```text
Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity
```

现有对象：

```text
SpawnPoint
```

追加组件：

- `PlayerSpawnPoint`
- `PlayerSpawner`

配置：

- `PlayerSpawner.playerPrefab` 指向 `Player.prefab`。
- `PlayerSpawner.spawnPoint` 指向同一对象上的 `PlayerSpawnPoint`。
- `shouldSpawnOnStart` 为启用状态；旧键 `spawnOnStart` 通过 `FormerlySerializedAs` 保持序列化兼容。

原始结构保护：

- 没有新增、删除或重命名 PlayerSandbox 层级对象。
- 没有改变原 SpawnPoint 的位置、旋转或父节点。
- 没有改变 Camera、Ground、Boundary、DebugCanvas 或 P0 根节点。
- 没有修改 `P0ProjectSetup.cs`。

---

## 5. 程序集调整

Runtime Player 程序集没有新增依赖，仍只引用：

```text
WonderSquad.Core
```

测试程序集调整：

- `WonderSquad.Tests.EditMode` 增加 `WonderSquad.Player` 引用。
- `WonderSquad.Tests.PlayMode` 增加 `WonderSquad.Player` 引用。

Editor 程序集最终没有增加 Player 引用，也没有留下资产生成工具。

依赖方向：

```text
Tests
└── WonderSquad.Player
    └── WonderSquad.Core
```

没有循环依赖或跨层访问。

---

## 6. 测试

### 6.1 Sprint001-A EditMode

文件：

```text
Client/Assets/WonderSquad/Tests/EditMode/PlayerSpawnConfigurationTests.cs
```

结果：

```text
Total: 2
Passed: 2
Failed: 0
```

覆盖：

| 测试 | 结果 |
|---|---|
| Player Prefab 是有效 Prefab 资产 | 通过 |
| PlayerSandbox 的 Prefab、Spawner、SpawnPoint 引用有效且唯一 | 通过 |

结果文件：

```text
Client/TestResults/Sprint001A_EditMode.xml
```

### 6.2 Sprint001-A PlayMode

文件：

```text
Client/Assets/WonderSquad/Tests/PlayMode/PlayerSpawnerPlayModeTests.cs
```

结果：

```text
Total: 3
Passed: 3
Failed: 0
```

覆盖：

| 测试 | 结果 |
|---|---|
| PlayerSandbox 运行后生成一个 Player | 通过 |
| 已有 Player 时重复调用不会生成第二个实例 | 通过 |
| 删除 Player 后可以重新生成 | 通过 |

结果文件：

```text
Client/TestResults/Sprint001A_PlayMode_Final.xml
```

### 6.3 完整回归

| 测试平台 | 总数 | 通过 | 失败 |
|---|---:|---:|---:|
| EditMode | 19 | 19 | 0 |
| PlayMode | 5 | 5 | 0 |

结果文件：

```text
Client/TestResults/Sprint001A_AllEditMode.xml
Client/TestResults/Sprint001A_AllPlayMode.xml
```

完整回归包含已有 P0 Bootstrap、场景、配置、日志、输入与 P0ProjectSetup 测试。

### 6.4 Unity 人工实测

验证环境：

```text
Unity 6000.3.21f1
Scene: PlayerSandbox
```

用户完成的 Unity 实测结果：

| 验证项 | 结果 |
|---|---|
| PlayerSandbox 可以正常运行 | 通过 |
| 玩家胶囊体成功生成 | 通过 |
| 出生位置正确 | 通过 |
| Console Error | 0 |
| EditMode Tests | 全部通过 |
| PlayMode Tests | 全部通过 |
| 重复生成玩家 | 未发现 |
| NullReferenceException | 未发现 |

该人工验证补足了自动测试之外的场景可运行性、可见表现、Console 状态和实际出生位置验证。

---

## 7. 文件清单

### 新增

```text
Client/Assets/WonderSquad/Prefabs/Player.meta
Client/Assets/WonderSquad/Prefabs/Player/Player.prefab
Client/Assets/WonderSquad/Prefabs/Player/Player.prefab.meta
Client/Assets/WonderSquad/Runtime/Player/Spawning.meta
Client/Assets/WonderSquad/Runtime/Player/Spawning/PlayerSpawnPoint.cs
Client/Assets/WonderSquad/Runtime/Player/Spawning/PlayerSpawnPoint.cs.meta
Client/Assets/WonderSquad/Runtime/Player/Spawning/PlayerSpawner.cs
Client/Assets/WonderSquad/Runtime/Player/Spawning/PlayerSpawner.cs.meta
Client/Assets/WonderSquad/Tests/EditMode/PlayerSpawnConfigurationTests.cs
Client/Assets/WonderSquad/Tests/EditMode/PlayerSpawnConfigurationTests.cs.meta
Client/Assets/WonderSquad/Tests/PlayMode/PlayerSpawnerPlayModeTests.cs
Client/Assets/WonderSquad/Tests/PlayMode/PlayerSpawnerPlayModeTests.cs.meta
Client/TestResults/Sprint001A_EditMode.xml
Client/TestResults/Sprint001A_PlayMode_Final.xml
Client/TestResults/Sprint001A_AllEditMode.xml
Client/TestResults/Sprint001A_AllPlayMode.xml
Docs/01_Project/Sprint001A_Report.md
```

### 修改

```text
Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity
Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef
Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef
CHANGELOG.md
```

---

## 8. 未实现范围

以下内容明确未实现：

- Player Movement
- CharacterController
- Input System 运行时处理
- Camera Follow
- Animation
- Interaction
- Inventory
- Ability
- Puzzle
- Network Spawn
- Photon Fusion

---

## 9. 当前限制与后续边界

- 当前 Player 只具有视觉占位结构，不能移动。
- 当前 Spawner 只跟踪自己生成的一个本地实例，不承担跨 Spawner 或跨网络的全局唯一性。
- 当前删除后重生需要外部显式调用 `TrySpawn`；没有增加自动轮询或恢复系统。
- PlayerSandbox 使用固定 P0 Camera，不属于 Sprint001-A 的摄像机实现。
- 后续 Movement Sprint 可在 Player 根节点添加 CharacterController 和移动组件，但不得让 PlayerSpawner 依赖 PlayerController。

---

## 10. 验收结论

- Player Prefab 基础结构：通过。
- Player Spawn Point：通过。
- 本地 PlayerSpawner：通过。
- PlayerSandbox 自动生成一个 Player：通过。
- 防止重复生成：通过。
- 删除后允许重新生成：通过。
- 不依赖 PlayerController：通过。
- 未引入禁止系统：通过。
- Unity 编译和完整测试：通过。
- Unity PlayerSandbox 人工实测：通过。
- Console Error 为 0：通过。
- 未发现 NullReferenceException：通过。

**Sprint001-A Player Spawn：PASSED。**

### Sprint001-A Hotfix 收尾

- 已将序列化布尔字段由 `spawnOnStart` 安全迁移为 `shouldSpawnOnStart`。
- 保留原默认值 `true` 和原自动生成逻辑。
- 使用 `[FormerlySerializedAs("spawnOnStart")]` 兼容尚未重新保存的 Unity 资产。
- PlayerSandbox 中的序列化启用值保持为 `1`，对象层级、Prefab 引用和 SpawnPoint 引用未改变。
- 新增迁移属性 EditMode 验证，并补充自动生成启用、禁用及重复初始化 PlayMode 覆盖。
- Sprint001 Gate 的 G-01 已关闭，最终结论为 `GO`。
- 详细证据见 `Docs/01_Project/Sprint001A_Hotfix_Report.md`。

---

## 11. 范围审计

审计边界：

```text
Commit: 690e01c feat(player): implement sprint001A player spawn
```

审计结果：

| 范围 | 结果 | 说明 |
|---|---|---|
| Player Prefab | 合规 | 只有根节点、VisualRoot 和胶囊占位表现 |
| PlayerSpawnPoint | 合规 | 只提供出生位置与旋转 |
| PlayerSpawner | 合规 | 只负责一个本地 Sandbox Player 的生成与引用 |
| PlayerSandbox | 合规 | 只给原有 SpawnPoint 增加两个组件，层级与原始对象保持不变 |
| Runtime 依赖 | 合规 | Player 程序集仍只引用 Core |
| 测试 | 合规 | 只验证 Prefab、配置、生成、去重和删除后重生 |
| Movement | 未引入 | 没有 PlayerController 或 CharacterController |
| Input | 未引入 | 没有 PlayerInputHandler 或运行时输入读取 |
| Camera | 未引入 | 没有 CameraFollow 或 Player Camera Rig |
| Animation | 未引入 | 没有 Animator 或动画状态 |
| 其他 Gameplay | 未引入 | 没有 Interaction、Inventory、Ability 或 Puzzle |
| Network | 未引入 | 没有 Fusion、NetworkObject 或网络生成逻辑 |

结论：

**Sprint001-A 修改严格限制在 Player Spawn 范围内。**

---

## 12. Sprint001-B 实施前检查清单

Sprint001-B 开始前必须逐项确认。

### 12.1 Git 与工作区

- [ ] 当前分支与计划的 Sprint001-B 分支名称一致。
- [ ] Sprint001-A 的 `PASSED` 报告和 CHANGELOG 已提交。
- [ ] 工作区无未说明修改或临时 Unity 资源。
- [ ] 当前分支已包含最新、经过验证的 `develop`。
- [ ] 明确 Sprint001-B 的提交与 Review 边界。

### 12.2 Unity 基线

- [ ] 使用 Unity `6000.3.21f1`。
- [ ] 项目打开后无编译错误。
- [ ] Console 没有未说明 Error。
- [ ] Sprint001-A EditMode、PlayMode 与 P0 回归仍通过。
- [ ] PlayerSandbox 仍能生成且只生成一个 Player。

### 12.3 Sprint001-B 范围冻结

- [ ] 在实施前写明 Sprint001-B 的唯一目标。
- [ ] 明确 Sprint001-B 是否仅包含 CharacterController 与 Movement 基础。
- [ ] 明确 Walking、Rotation、Gravity、Ground Check 中哪些属于本阶段验收。
- [ ] Running 若只做预留，必须明确“预留”的具体形式，不提前加入未批准输入行为。
- [ ] 明确 Input、Camera、Animation、Interaction、Inventory、Ability、Puzzle 和 Network 是否全部继续排除。
- [ ] 如果范围尚未冻结，结论必须为 NO-GO，不得开始代码。

### 12.4 Player Prefab 与职责边界

- [ ] CharacterController 如获批准，只添加在 Player 根节点。
- [ ] 不让 PlayerSpawner 依赖 PlayerController。
- [ ] 不改变 PlayerSpawner 的本地 Sandbox 职责。
- [ ] 保留 `VisualRoot`，Movement 不直接控制视觉子对象层级。
- [ ] 不在 Player Prefab 中提前加入 Input、Camera、Animator 或 Network 组件。
- [ ] Player 删除后重新生成仍能得到完整且有效的 Prefab 实例。

### 12.5 配置与代码规范

- [ ] 所有可调移动参数进入获批准的 `MovementSettings` ScriptableObject。
- [ ] 不在 PlayerController、GroundDetector 或场景中散落 Magic Number。
- [ ] Runtime Player 继续只依赖架构允许的程序集。
- [ ] 不新增 Editor、UI、Interaction 或 Network 的 Runtime 反向引用。
- [ ] 类型与 Namespace 符合 `CODE_STYLE.md`。
- [ ] PlayerController 保持单一职责，不承担生成、输入、摄像机或网络逻辑。

### 12.6 测试计划

- [ ] EditMode 验证 MovementSettings 的合法范围和默认配置。
- [ ] PlayMode 验证 Player Prefab 包含且只包含获批准的移动组件。
- [ ] PlayMode 验证地面上的稳定状态。
- [ ] PlayMode 验证重力、旋转、Ground Check 等批准行为。
- [ ] 验证重复生成与删除后重生测试没有退化。
- [ ] 运行完整 EditMode 与 PlayMode 回归。
- [ ] 在 PlayerSandbox 手工验证 Console Error 为 0。

### 12.7 Definition of Ready

只有在以下条件全部满足后，Sprint001-B 才是 GO：

1. 范围和非目标已书面冻结。
2. Git 与 Unity 基线正常。
3. Sprint001-A 保持 PASSED。
4. Prefab、程序集和测试方案无架构冲突。
5. 没有要求 PlayerSpawner 承担 Movement 或 Network 职责。

当前仅提供检查清单，未对 Sprint001-B 作实施授权，也未开始 Sprint001-B 代码。
