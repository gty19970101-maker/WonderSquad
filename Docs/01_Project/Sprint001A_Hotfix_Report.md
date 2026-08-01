# Sprint001-A Hotfix Report

## 1. Hotfix 目标

关闭 Sprint001 Gate 唯一门禁项：修正 `PlayerSpawner` 序列化布尔字段的命名，同时完整保留已有 Unity 资产数据和 Player Spawn 行为。

本次严格限制在 Sprint001-A Player Spawn 范围，不开始 Sprint001-B。

## 2. 修改前后字段

修改前：

```csharp
[SerializeField]
[Tooltip("Creates the local player when the Sandbox starts.")]
private bool spawnOnStart = true;
```

修改后：

```csharp
[FormerlySerializedAs("spawnOnStart")]
[SerializeField]
[Tooltip("Creates the local player when the Sandbox starts.")]
private bool shouldSpawnOnStart = true;
```

- 字段类型未改变。
- 默认值仍为 `true`。
- `Start` 中的条件判断只同步使用新字段名，分支语义未改变。
- Attribute 顺序与项目当前序列化迁移风格一致：迁移属性、序列化属性、Inspector 说明、字段。

## 3. FormerlySerializedAs 迁移说明

`FormerlySerializedAs("spawnOnStart")` 告知 Unity：当旧 Prefab、Scene 或其他序列化资产仍保存旧键时，应把该值加载到 `shouldSpawnOnStart`。

因此：

- 旧值为 `true` 的资产仍读取为 `true`。
- 旧值为 `false` 的资产仍读取为 `false`。
- 不依赖脚本默认值替代已有资产值。
- PlayerSandbox 已使用新键保存为 `shouldSpawnOnStart: 1`，启用值保持不变。

结论：**序列化值可保留。**

## 4. 受影响文件

### Runtime

```text
Client/Assets/WonderSquad/Runtime/Player/Spawning/PlayerSpawner.cs
```

仅增加序列化迁移命名空间与属性，并更新字段及其条件引用。

### Scene

```text
Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity
```

仅把序列化键由 `spawnOnStart` 更新为 `shouldSpawnOnStart`；值仍为 `1`。未修改 Hierarchy、Transform、组件、Prefab 引用或 SpawnPoint 引用。

### Tests

```text
Client/Assets/WonderSquad/Tests/EditMode/PlayerSpawnConfigurationTests.cs
Client/Assets/WonderSquad/Tests/PlayMode/PlayerSpawnerPlayModeTests.cs
Client/TestResults/Sprint001A_Hotfix_EditMode.xml
```

### Documents

```text
CHANGELOG.md
Docs/01_Project/Sprint001A_Report.md
Docs/01_Project/Sprint001_Gate_Review.md
Docs/01_Project/Sprint001A_Hotfix_Report.md
```

Player Prefab 未修改。

## 5. 测试调整

### EditMode

新增迁移契约测试，验证：

- `shouldSpawnOnStart` 私有字段存在。
- 旧私有字段 `spawnOnStart` 不再存在。
- 新字段仍有 `[SerializeField]`。
- 新字段存在 `[FormerlySerializedAs]`。
- 迁移属性的旧字段名严格等于 `spawnOnStart`。

该测试读取 Attribute 元数据，不修改业务行为，也不让一般行为测试依赖私有字段值。

### PlayMode

行为测试覆盖：

- `shouldSpawnOnStart = true` 时，场景启动后生成一个 Player。
- `shouldSpawnOnStart = false` 时，不自动生成 Player。
- Start 完成后重复调用 `TrySpawn`，仍只有一个 Player。
- 删除已生成 Player 后，可再次生成。

禁用场景使用 Unity JSON 序列化设置测试实例，不修改生产 API，不要求公开仅供测试使用的 setter。

## 6. Unity 验证结果

验证版本：

```text
Unity 6000.3.21f1
```

| 验证项 | 结果 | 说明 |
|---|---|---|
| 脚本编译 | 通过 | Unity 执行相关 EditMode 测试前完成编译，无编译错误 |
| 相关 EditMode Tests | 3/3 通过 | 包含 Prefab、Scene 配置及迁移属性验证 |
| 相关 PlayMode Tests | 未取得新运行结果 | 两次批处理均在测试执行前被本机 Unity Licensing Client 重连循环阻塞；不是断言失败或编译失败 |
| 既有 Sprint001-A PlayMode 基线 | 3/3 通过 | Hotfix 前已经 Unity 实测通过 |
| PlayerSandbox 人工回归 | 待按下列步骤复核 | 不把未执行验证标记为通过 |

测试在与当前源码同步的隔离项目副本中使用相同 Unity 版本执行，以避免改动或锁定已打开的主工程。隔离副本只用于测试，不属于交付内容。

## 7. PlayerSandbox 人工回归步骤

1. 使用 Unity `6000.3.21f1` 打开 `PlayerSandbox`。
2. 在 SpawnPoint 的 PlayerSpawner Inspector 中确认 `Should Spawn On Start` 为启用。
3. 进入 Play Mode。
4. 确认原 SpawnPoint 位置只出现一个 Player 胶囊。
5. 再次触发公开的 `TrySpawn` 验证路径时，确认不产生第二个 Player。
6. 确认 Console Error 为 `0`，且没有 `NullReferenceException`。
7. 退出 Play Mode，确认 Scene 没有产生待保存的结构变化。

## 8. 全仓库依赖搜索

排除本次报告和历史 Gate 说明后：

- Runtime 中旧字符串只存在于 `[FormerlySerializedAs("spawnOnStart")]`。
- EditMode 中旧字符串只用于验证迁移契约。
- 不存在其他旧字段代码读取或赋值。
- 不存在 `SerializedProperty` 对旧字段名的依赖。
- 不存在 JSON、反射或测试通过旧字段名设置运行状态的依赖。
- PlayMode 的新字段字符串只用于隔离配置 `false` 行为测试。
- PlayerSandbox 使用新字段键。

所有旧名称剩余位置均为有意的迁移声明、迁移测试或历史变更记录。

## 9. 序列化值保留结论

**已保留。**

依据：

1. 新字段类型和默认值不变。
2. `FormerlySerializedAs` 旧键完全匹配。
3. EditMode 迁移元数据测试通过。
4. PlayerSandbox 新键的保存值仍为 `1`。
5. 未修改 Prefab 结构、Scene 结构或对象引用。

## 10. 范围审计

| 范围 | 结果 |
|---|---|
| Player Spawn 业务行为 | 未改变 |
| Player Prefab 结构 | 未修改 |
| PlayerSandbox 场景结构 | 未修改 |
| P0ProjectSetup | 未修改 |
| Input | 未引入 |
| Movement | 未引入 |
| Camera | 未引入 |
| Interaction | 未引入 |
| Inventory | 未引入 |
| Ability | 未引入 |
| Puzzle | 未引入 |
| Network / Fusion | 未引入 |
| 程序集依赖 | 未修改 |

## 11. Gate 最终结论

**GO**

原唯一门禁项已安全关闭。该结论不表示已开始 Sprint001-B；本次工作到此停止。
