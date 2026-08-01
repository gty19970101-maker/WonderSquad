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

**完成**

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
- `spawnOnStart` 为启用状态。

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

**Sprint001-A Player Spawn 已完成。**

本次工作到此停止，不开始 Movement。
