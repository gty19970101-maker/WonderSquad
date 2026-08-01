# Sprint001D Controlled Third-Person Camera 实施报告

## 1. 实施结论

- Sprint：Sprint001D — Controlled Third-Person Camera
- 分支：`feature/sprint001d-camera-follow`
- Unity：`6000.3.21f1`
- Cinemachine：`3.1.7`
- 自动化状态：**PASSED**
- 人工 PlayerSandbox 验收：**PASSED**
- 最终状态：**PASSED**

本阶段已实现稳定、可配置、无输入依赖的固定斜俯视第三人称跟随相机。实现没有加入自由环绕、鼠标/手柄视角、相机输入、瞄准、动态 FOV、相机抖动、Cutscene、分屏、Spectator 或 Network Camera。

Unity 自动化回归与 PlayerSandbox 人工验收均已完成，Sprint001D 已满足收尾条件。

## 2. 相机链路与边界

```text
PlayerSpawner
→ PlayerSpawned 事件 / SpawnedPlayer 只读引用
→ CameraTargetBinder
→ PlayerCameraTarget
→ CinemachineCamera + CinemachineFollow
→ CinemachineBrain
→ Main Camera
```

边界说明：

- Player Prefab 只包含表现层锚点 `CameraTarget`，不包含 Main Camera 或 Camera Rig。
- `PlayerCameraTarget` 不读取输入、不执行位移、不引用 Cinemachine。
- `CameraTargetBinder` 只负责配置和显式本地玩家目标绑定，不自行计算跟随运动。
- 实际跟随、世界坐标偏移和阻尼由 Cinemachine Follow 执行。
- `PlayerMovement` 未修改，也不引用 `WonderSquad.Camera`。
- Camera 不读取 `PlayerInputReader`、Keyboard、Gamepad 或 InputAction。
- Camera 不写入 Player 根 Transform、PlayerMovement 或 CharacterController。
- Camera 状态只在本地处理，不进入网络权威。

## 3. 实现内容

### 3.1 Cinemachine 包

`Client/Packages/manifest.json` 新增：

```json
"com.unity.cinemachine": "3.1.7"
```

Unity Package Manager 已在 `packages-lock.json` 中解析为：

- `com.unity.cinemachine`：`3.1.7`，depth `0`
- `com.unity.splines`：`2.9.0`，Cinemachine 传递依赖
- `com.unity.settings-manager`：`2.1.1`，Splines 传递依赖

Input System、URP、Test Framework 以及其他现有直接包版本均未改变。

### 3.2 WonderSquad.Camera 程序集

新增独立表现层程序集：

`Client/Assets/WonderSquad/Runtime/Camera/WonderSquad.Camera.asmdef`

引用：

- `WonderSquad.Core`
- `WonderSquad.Player`
- `Unity.Cinemachine`

命名空间采用 `WonderSquad.Presentation.Camera`，避免与 `UnityEngine.Camera` 产生遮蔽。

Player、Interaction、Inventory、Ability、Puzzle、Network、UI 等程序集均未反向引用 `WonderSquad.Camera`，程序集依赖保持无环。

### 3.3 PlayerCameraTarget

Player Prefab 新增独立子对象：

```text
Player
├── VisualRoot
└── CameraTarget
    └── PlayerCameraTarget
```

职责：

- 提供稳定的只读跟随 Transform。
- 与未来替换的 VisualRoot/美术模型解耦。
- 不承载移动、输入、相机控制或网络逻辑。
- 不包含 Main Camera。

### 3.4 CameraFollowSettings

新增 `CameraFollowSettings` ScriptableObject 与默认资产。

| 参数 | 默认值 | 用途 |
|---|---:|---|
| Target Local Offset | `(0, 1.5, 0)` | Player 根节点上的观察锚点高度 |
| Follow Offset | `(0, 6.5, -9)` | 世界坐标固定斜俯视偏移 |
| Position Damping | `(0.35, 0.5, 0.35)` | Cinemachine 每轴位置阻尼 |
| Fixed Euler Angles | `(32, 0, 0)` | 固定世界坐标镜头方向 |
| Field Of View | `60` | 透视相机视野 |

配置会校验有限数值、阻尼范围、俯角、零 Roll 与 FOV 范围。运行时状态不会写回资产。

### 3.5 CameraTargetBinder

`CameraTargetBinder` 处理以下顺序：

- 相机先初始化、PlayerSpawner 后在 Start 生成玩家。
- Binder 启用时玩家已经存在。
- PlayerSpawner 稍后显式生成玩家。
- 当前玩家被销毁。
- PlayerSpawner 重新生成玩家。

绑定方式：

- 通过序列化的明确 `PlayerSpawner` 引用连接 Sandbox 本地生成源。
- 订阅 `PlayerSpawner.PlayerSpawned`，不使用静态全局 Player。
- 启用时读取现有 `SpawnedPlayer`，支持玩家先于 Binder 存在。
- 只检查缓存对象有效性，不通过每帧场景查找玩家。
- Player 销毁后清空 Cinemachine Follow target。
- 重新生成后绑定新实例的 `PlayerCameraTarget`。

Binder 提供显式 `TryBind(GameObject)` 边界，未来本地玩家选择来源可以替换，而无需让 Camera 变成 Network Spawn 系统。

### 3.6 Cinemachine Rig

新增 `PF_PlayerCameraRig.prefab`：

```text
PF_PlayerCameraRig
├── Main Camera
│   ├── Camera
│   ├── AudioListener
│   └── CinemachineBrain
└── Controlled Camera
    ├── CinemachineCamera
    ├── CinemachineFollow
    └── CameraTargetBinder
```

关键配置：

- Cinemachine Follow 使用 `WorldSpace` Binding Mode。
- 镜头只跟随目标位置，不根据 Player yaw 旋转。
- Controlled Camera 使用固定世界旋转。
- 场景中只有一个 Main Camera、AudioListener、CinemachineBrain 与 CinemachineCamera。
- Rig 属于 PlayerSandbox 场景表现层，不是 Player Prefab 子对象。

未新增自定义 `CameraFollow` 算法；跟随行为复用 Cinemachine。

### 3.7 PlayerSandbox

PlayerSandbox 已：

- 用 `PF_PlayerCameraRig` 实例替换 P0 固定相机。
- 保留场景契约名 `CameraMount`，保证 P0 Smoke Test 兼容。
- 将 Binder 显式连接到现有 `PlayerSpawner`。
- 新增 `CameraValidationRoot`。

验证区占位对象：

- `OccluderWall`
- `RaisedPlatform`
- `NarrowGateLeft`
- `NarrowGateRight`
- `Canopy`

占位对象用于人工观察跟随、构图与遮挡风险。Sprint001D 未加入复杂自研射线相机或遮挡材质淡化；基础碰撞/遮挡修正列为后续增强项。

## 4. 新增文件

### Runtime

- `Client/Assets/WonderSquad/Runtime/Camera/WonderSquad.Camera.asmdef`
- `Client/Assets/WonderSquad/Runtime/Camera/CameraFollowSettings.cs`
- `Client/Assets/WonderSquad/Runtime/Camera/CameraTargetBinder.cs`
- `Client/Assets/WonderSquad/Runtime/Player/Camera/PlayerCameraTarget.cs`
- 上述目录和文件对应 `.meta`

### Assets

- `Client/Assets/WonderSquad/Prefabs/Player/PF_PlayerCameraRig.prefab`
- `Client/Assets/WonderSquad/ScriptableObjects/Configuration/CameraFollowSettings.asset`
- 上述资产对应 `.meta`

### Tests

- `Client/Assets/WonderSquad/Tests/EditMode/CameraFollowEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/CameraFollowPlayModeTests.cs`
- 测试文件对应 `.meta`

### Documentation

- `Docs/01_Project/Sprint001D_Implementation_Report.md`
- `Docs/01_Project/Sprint001D_Review_Checklist.md`

## 5. 修改文件

- `Client/Packages/manifest.json`
- `Client/Packages/packages-lock.json`
- `Client/Assets/WonderSquad/Runtime/Player/Spawning/PlayerSpawner.cs`
- `Client/Assets/WonderSquad/Prefabs/Player/Player.prefab`
- `Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`
- `Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef`
- `Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef`
- `Docs/01_Project/Sprint001D_Preflight_Report.md`
- `CHANGELOG.md`

`PlayerSpawner` 只新增通用 `PlayerSpawned` 事件，原有单实例生成、销毁后重新生成和 Prefab/SpawnPoint 行为未改变。

## 6. 自动化测试

### 6.1 Sprint001D EditMode

```text
5 Passed
0 Failed
0 Skipped
```

覆盖：

- 默认 CameraFollowSettings 有效。
- Offset、Damping、角度和 FOV 范围有效。
- Player Prefab 包含独立有效 Camera Target，且不包含 Camera。
- Camera 程序集依赖方向正确。
- 实际加载的 Cinemachine 与 manifest 均为 `3.1.7`。

### 6.2 Sprint001D PlayMode

```text
6 Passed
0 Failed
0 Skipped
```

覆盖：

- 相机先初始化、玩家后生成时成功绑定。
- 玩家已存在时 Binder 重新启用可立即绑定。
- 玩家移动时相机跟随，停止后稳定。
- Player yaw 改变不会带动相机环绕。
- Player 销毁不会产生空引用，重新生成后重新绑定。
- 场景只有一个有效 Main Camera、Brain 和 CinemachineCamera。

### 6.3 完整回归

```text
EditMode
37 Passed
0 Failed
0 Skipped

PlayMode
19 Passed
0 Failed
0 Skipped
```

完整回归包括 Bootstrap、P0 Scene、PlayerSpawner、PlayerInputReader、PlayerMovement 和 Camera。

首次完整 PlayMode 回归发现 PlayerSandbox 缺少 P0 测试要求的 `CameraMount` 名称。处理方式是保留 Camera Rig Prefab，同时把场景实例命名恢复为 `CameraMount`；没有修改测试来绕过问题。修复后完整 PlayMode `19/19` 通过。

## 7. 范围审计

已确认未实现或引入：

- Camera 输入、自由环绕、鼠标/手柄视角
- Camera-relative Movement
- Jump
- Animation
- Interaction
- Inventory
- Ability
- Puzzle
- Network Camera 或 Photon
- Target Lock、Aim、Dynamic FOV、Shake、Cutscene
- Split Screen、Spectator
- Rigidbody Player Movement
- 静态全局 Player 引用
- Runtime `FindObjectOfType`、`FindFirstObjectByType` 或字符串玩家查找

相机程序集只新增允许的表现层依赖；Player 与 Gameplay 域没有反向依赖。

## 8. Unity 人工验收

### 8.1 实测结果

验收环境：Unity `6000.3.21f1`

| 验收项 | 结果 |
|---|---|
| WASD 移动时 Camera 平滑跟随 | 通过 |
| 固定斜俯视方向 | 通过 |
| Player 旋转不触发 Camera 自由环绕 | 通过 |
| 玩家停止后 Camera 稳定 | 通过 |
| 墙体测试 | 通过 |
| 门洞测试 | 通过 |
| 高低差测试 | 通过 |
| Canopy 测试 | 通过 |
| Main Camera 数量 | 通过 |
| Console Error | `0` |

人工结果与自动化测试一致，未发现 Camera 接入造成的 Player Spawn、输入或移动回归。

### 8.2 后续回归步骤

1. 使用 Unity `6000.3.21f1` 打开项目。
2. 等待 Package Manager 和脚本编译完成，确认 Cinemachine 显示为 `3.1.7`。
3. 打开 `Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`。
4. 确认 Hierarchy 中只有一个 `CameraMount` 实例，其下包含 Main Camera 与 Controlled Camera。
5. 点击 Play，确认一个 Player 正常生成。
6. 使用 WASD 移动，确认相机平滑跟随。
7. 快速切换 W/A/S/D，确认无明显抖动或方向突变。
8. 观察 Player 转向，确认画面整体方向保持固定，不绕 Player 自由旋转。
9. 松开输入，确认相机平滑停止并保持稳定。
10. 经过 OccluderWall、RaisedPlatform、NarrowGate 和 Canopy 区域，记录构图或穿墙问题。
11. 在运行时删除 Player(Clone)，确认 Console 无 NullReferenceException。
12. 在 PlayerSpawner Inspector 或调试入口重新执行生成，确认 Camera 绑定新 Player。
13. 确认场景只有一个启用的 Main Camera 和 AudioListener。
14. 重复 Sprint001C 的移动、停止、斜向、转向和重力检查，确认移动手感未改变。
15. 确认 Console Error 为 0。

## 9. 当前限制

- 只支持一个本地玩家 Camera Target。
- Camera 不提供任何玩家输入或自由环绕。
- 移动仍使用世界坐标，不是相机相对坐标。
- 没有复杂相机碰撞、遮挡物透明化或镜头区域系统。
- 没有网络本地玩家选择适配器；当前绑定源仅为 Sandbox `PlayerSpawner`。
- 当前默认参数已通过 PlayerSandbox 人工手感验收；正式角色模型和正式关卡美术接入后仍需按表现效果复调。

## 10. 推荐 Commit

```text
feat(camera): implement controlled third-person follow
```

## 11. 最终状态

**PASSED**

编译、EditMode、PlayMode 与 Unity PlayerSandbox 人工验收均已通过。Sprint001D 已完成收尾；本报告不启动 Sprint001E 或 Interaction。
