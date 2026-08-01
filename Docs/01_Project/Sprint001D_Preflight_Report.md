# Sprint001D Preflight Report

## 1. 报告信息

- Sprint：Sprint001D
- 目标：Third Person Camera
- 检查日期：2026-08-01
- Unity 基线：`6000.3.21f1`
- 实际检查分支：`develop`
- 工作区状态：检查开始时干净
- 本次范围：只做实施前静态检查，不修改 Unity 代码、Prefab、Scene、Package 或配置
- 最终结论：**CONDITIONAL GO**

## 2. 结论摘要

Sprint001A～C 已提供稳定的本地玩家生成、设备无关输入、CharacterController 移动、朝向、接地与重力基础。Sprint001C 的 Unity 实测结果为 EditMode `32 Passed / 0 Failed`、PlayMode `13 Passed / 0 Failed`，人工验收也已通过，因此 Character Foundation 已满足相机接入前提。

当前不能直接开始实现，原因不是 Character Foundation 缺陷，而是以下两项实施前条件尚未完成：

1. 当前实际分支为 `develop`。按照 `CONTRIBUTING.md` 与 `BRANCH_STRATEGY.md`，实现应从最新 `develop` 创建独立 feature 分支，不应直接写入 `develop`。
2. 项目当前没有安装 Cinemachine，而治理规范禁止引入未确认的第三方插件或 SDK。虽然 Cinemachine 是 Unity 官方包，仍需在实现前确认采用它，并锁定包版本。

解除上述条件后可以进入 Sprint001D，无需先重构 Player Spawn、PlayerInputReader 或 PlayerMovement。

## 3. 检查结果

### 3.1 Character Foundation 是否满足 Camera 接入条件

**结果：满足。**

现有基础：

- `PlayerSpawner` 在 Sandbox 中只生成一个本地 Player，并提供只读 `SpawnedPlayer` 引用。
- `Player.prefab` 已包含稳定根对象、`VisualRoot`、占位模型、`PlayerInputReader`、`CharacterController`、`GroundDetector` 和 `PlayerMovement`。
- `PlayerMovement` 只从 `PlayerInputReader.MoveInput` 读取设备无关输入。
- `PlayerMovement` 不引用 Camera、Cinemachine 或 Camera Transform。
- Player 使用 `CharacterController` 作为唯一移动核心，Prefab 中没有 Rigidbody。
- 水平移动、旋转、重力、接地、禁用/恢复与生成回归均已通过 Unity 实测。

现有移动采用世界坐标方向。Sprint001D 必须保持该行为，不得顺带改成 Camera-relative Movement；若未来需要相机相对输入，应作为独立 Sprint 重新设计和验证。

### 3.2 是否推荐使用 Cinemachine

**结果：推荐，但须先确认并锁定版本。**

推荐理由：

- Cinemachine 是 Unity 官方相机包，适合小团队减少跟随阻尼、取景、镜头恢复和遮挡碰撞方面的自研成本。
- Unity 官方文档确认 Cinemachine `3.1.7` 是 Unity `6000.0` 的 released package；本项目为 Unity `6000.3.21f1`，属于同一 Unity 6 产品线。
- Cinemachine 负责本地表现，不改变玩家移动或网络权威边界。
- 后续关卡相机、临时镜头和过场镜头可以继续复用同一套 Brain/Camera 管线。

官方资料：

- [Unity 6 Cinemachine package information](https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.cinemachine.html)
- [Cinemachine 3.1 manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html)

建议采用：

- 包：`com.unity.cinemachine`
- 建议锁定版本：`3.1.7`
- 镜头类型：受控斜俯视第三人称跟随
- Sprint001D 不加入自由环绕、肩后瞄准、相机输入、镜头震动或复杂遮挡淡化

不建议直接使用“始终跟随角色朝向旋转”的默认肩后式配置。当前 Player 会朝移动方向转身，如果相机也随角色朝向持续绕转，会让世界坐标移动与屏幕方向快速失配，并可能造成眩晕。相机应跟随 Camera Target 的位置，但保持由配置决定的受控角度。

若项目负责人不批准新增 Cinemachine，则需在实现前把方案改为最小自研 Follow Camera，并重新确认遮挡处理的 MVP 范围；不可在实施中临时切换方案。

### 3.3 是否已有 Camera 相关脚本

**结果：没有。**

全仓库 Runtime、Prefab 与 Package 检查未发现：

- Camera Follow 脚本
- Camera Controller/Binder 脚本
- Cinemachine 组件或包引用
- Camera Settings 数据资产
- Camera 自动测试

唯一相关代码是 `PlayerMovement` 注释明确声明其不负责 Camera 行为，不属于实现。

### 3.4 是否已有 Camera Rig

**结果：只有 P0 固定验证相机层级，不是可用 Camera Rig。**

`PlayerSandbox.unity` 当前包含：

```text
SandboxRoot
└── CameraMount
    └── Main Camera
```

`CameraMount` 使用固定位置与固定俯角；`Main Camera` 只有 Unity Camera 与 AudioListener。它没有：

- 玩家目标绑定
- 跟随行为
- 阻尼
- 距离或角度配置
- 遮挡恢复
- Cinemachine Brain/Camera

因此它可以作为 P0 验证相机保留到 Sprint001D 实施时迁移，但不能认定为已存在的 Third Person Camera Rig。

### 3.5 是否存在 Camera 与 PlayerMovement 耦合

**结果：当前不存在。**

静态检查确认：

- `PlayerMovement` 不引用 `Camera`、`Camera.main`、Cinemachine 或相机程序集。
- `PlayerMovement` 不读取 Camera Transform。
- `PlayerInputReader` 不负责移动 Transform，也不包含 Camera 输入。
- `PlayerSpawner` 不参与移动计算。

Sprint001D 必须保持依赖方向：

```text
Camera Presentation
        ↓ 只读跟随
Player Camera Target

PlayerInputReader
        ↓
PlayerMovement
        ↓
CharacterController
```

禁止形成以下反向依赖：

```text
PlayerMovement → Camera
PlayerInputReader → Camera Rig
PlayerSpawner → Cinemachine
```

### 3.6 Camera 应属于哪个程序集

**结果：应新增独立表现层程序集 `WonderSquad.Camera`。**

建议位置：

`Client/Assets/WonderSquad/Runtime/Camera/WonderSquad.Camera.asmdef`

允许的依赖：

- `WonderSquad.Core`
- `WonderSquad.Player`，仅用于读取最小 Player Camera Target 边界
- `Unity.Cinemachine`，仅在批准并安装 Cinemachine 后

禁止依赖：

- `WonderSquad.Interaction`
- `WonderSquad.Inventory`
- `WonderSquad.Ability`
- `WonderSquad.Puzzle`
- `WonderSquad.Network`
- `WonderSquad.UI`
- Editor 程序集

依赖方向只能是 Camera 表现层读取 Player，不允许 `WonderSquad.Player` 反向引用 `WonderSquad.Camera`。这样符合 `Technical_Architecture_v1.1.md` 的 `Presentation → Gameplay Domains` 分层，并避免循环依赖。

### 3.7 Camera 是否应依赖 Input

**结果：Sprint001D 不应依赖 Input。**

当前设计是受控 2.5D 第三人称跟随，不需要玩家自由旋转镜头。因此：

- Camera 不读取 `PlayerInputReader.MoveInput`。
- Camera 不读取 `Keyboard.current`、Gamepad 或现有 Move Action。
- Camera 不修改 Input Action 生命周期。
- Player 输入程序集不引用 Camera。

若后续确认需要有限旋转或手柄右摇杆，应新增 Camera 专用 Action 和 Camera Input Reader，并由 Camera 表现层独立消费；不能复用 MoveInput，也不能让 PlayerMovement 感知具体输入设备。该功能不属于当前 Sprint001D 最小范围。

### 3.8 是否需要 Camera Target

**结果：需要。**

`Player.prefab` 当前只有 `Player`、`VisualRoot` 和 `VisualPlaceholder`，没有稳定 Camera Target。直接跟随 Player 根节点会把 collider 原点、旋转和镜头构图绑定在一起；直接跟随占位模型会把相机依赖到未来会替换的美术结构。

建议在 Player Prefab 增加：

```text
Player
├── CameraTarget
└── VisualRoot
    └── VisualPlaceholder
```

规则：

- Camera Target 是 Player 的只读跟随锚点，不执行相机逻辑。
- 高度和局部偏移可配置，不硬编码在 Camera Follow 脚本中。
- Camera 跟随位置，但 Sprint001D 不应被 Player yaw 强制带动镜头环绕。
- Target 不保存 Camera、Cinemachine 或本地输入引用。
- Target 不参与网络同步；未来每个客户端只将本地玩家目标绑定到自己的相机。
- 不使用 `Camera.main`、全局静态单例或每帧场景查找绑定目标。

由于 Player 是运行时生成的，场景中的 Camera Rig 不能预先序列化引用未来 Player 实例。Sprint001D 需要一个明确的本地目标绑定步骤。允许 Camera 表现层读取 `PlayerSpawner.SpawnedPlayer` 或由 Sandbox 组合层显式注入 Target，但不得让 PlayerSpawner 引用 Camera/Cinemachine，也不得把 Sandbox 本地生成器演化为网络生成系统。

### 3.9 Camera Sandbox 如何验证

**结果：优先扩展现有 `PlayerSandbox`，不新增重复业务 Sandbox。**

`Prototype_Task_Breakdown.md` 的 P1-003 已明确要求在 PlayerSandbox 增加遮挡墙、高低差、树冠和狭窄门洞测试段。继续使用该场景可以同时回归 Player Spawn、Input 和 Movement，避免维护两套玩家基础场景。

建议在 `PlayerSandbox` 下建立独立、可开关的 `CameraValidationRoot`，使用占位几何体配置：

- 直线和转角移动段
- 紧贴墙面段
- 单面遮挡墙
- 树冠或顶部遮挡
- 狭窄门洞
- 高低差与落下区
- 越界/重生后重新绑定验证点

自动化验证建议：

1. 场景中只有一个启用的 Main Camera、AudioListener 和 Camera Brain。
2. Player 运行时生成后，Camera Rig 只绑定该本地 Player 的 Camera Target。
3. Player 移动时相机持续跟随，目标不会丢失。
4. Camera 不修改 Player Transform、CharacterController 或 MoveInput。
5. 目标静止后相机在配置容差内稳定，不持续漂移或抖动。
6. Camera 组件禁用与重新启用后能安全恢复目标。
7. 遮挡区不会让相机长期留在墙体内部；恢复路径无明显跳变。
8. PlayerSpawner、PlayerInputReader 和 PlayerMovement 原有测试继续通过。

人工验证步骤：

1. 使用 Unity `6000.3.21f1` 打开 `PlayerSandbox`。
2. Play 后确认一个 Player 正常生成，Main Camera 正确绑定本地 Camera Target。
3. 使用 WASD 完成直线、斜向、急转和反向移动。
4. 经过墙面、树冠、门洞和高低差测试段。
5. 从高处落下并等待接地，检查镜头无明显抖动。
6. 快速转向时检查镜头不随角色 yaw 高频环绕。
7. 删除并重新生成本地 Player，验证旧 Target 不残留且新 Target 可重新绑定。
8. 运行至少 10 分钟，检查目标不丢失、相机不长期穿模、玩家控制方向保持可理解。
9. 确认 Console Error 为 0。

本 Sprint 允许占位几何体；不要求正式美术、相机音效、镜头震动或遮挡物淡化。

### 3.10 预计新增和修改文件清单

以下是实施阶段的建议清单，本次 Preflight 不创建或修改这些文件。

#### 包与程序集

- 修改 `Client/Packages/manifest.json`：批准后加入 `com.unity.cinemachine` `3.1.7`
- 修改 `Client/Packages/packages-lock.json`：由 Unity Package Manager 锁定解析结果
- 新增 `Client/Assets/WonderSquad/Runtime/Camera/WonderSquad.Camera.asmdef`
- 修改 `Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef`
- 修改 `Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef`
- 新增上述目录、文件对应的 Unity `.meta`

#### Runtime 与配置

- 新增 `Client/Assets/WonderSquad/Runtime/Player/Camera/PlayerCameraTarget.cs`
- 新增 `Client/Assets/WonderSquad/Runtime/Camera/CameraSettings.cs`
- 新增 `Client/Assets/WonderSquad/Runtime/Camera/LocalPlayerCameraBinder.cs`
- 新增 `Client/Assets/WonderSquad/ScriptableObjects/Configuration/CameraSettings.asset`
- 新增上述目录、文件与资产对应的 Unity `.meta`

说明：

- `PlayerCameraTarget` 只标识 Player 的跟随锚点，不持有 Camera 逻辑。
- `LocalPlayerCameraBinder` 只负责本地目标绑定，不负责移动、输入或网络生成。
- 如 Cinemachine 组件和序列化配置已完全承担跟随行为，不应再创建重复包装用的 `CameraFollow.cs`。

#### Prefab 与 Scene

- 修改 `Client/Assets/WonderSquad/Prefabs/Player/Player.prefab`：增加 `CameraTarget`
- 新增 `Client/Assets/WonderSquad/Prefabs/Player/PF_PlayerCameraRig.prefab`
- 修改 `Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`：接入 Rig，并增加 `CameraValidationRoot`

不修改 Player Prefab 的 CharacterController、Movement、Input 或现有视觉层级职责；不把 Camera Rig 作为 Player Prefab 子对象，以免未来每个网络 Player 都生成一套本地相机。

#### Tests

- 新增 `Client/Assets/WonderSquad/Tests/EditMode/CameraSettingsEditModeTests.cs`
- 新增 `Client/Assets/WonderSquad/Tests/EditMode/PlayerCameraTargetEditModeTests.cs`
- 新增 `Client/Assets/WonderSquad/Tests/PlayMode/PlayerCameraRigPlayModeTests.cs`
- 新增测试文件对应的 Unity `.meta`

#### 文档

- 新增 `Docs/01_Project/Sprint001D_Implementation_Report.md`
- 新增 `Docs/01_Project/Sprint001D_Review_Checklist.md`
- 修改 `CHANGELOG.md` 的 `Unreleased`

具体文件名可在实现时按现有命名规范微调，但不得改变模块与依赖边界。

## 4. 风险与控制措施

| 风险 | 级别 | 控制措施 |
|---|---|---|
| 在 `develop` 直接实现 | 阻塞 | 从最新 `develop` 创建 Sprint001D feature 分支 |
| 未确认即加入 Cinemachine | 阻塞 | 先批准 Unity 官方包并锁定 `3.1.7` |
| 相机跟随 Player yaw 自动环绕 | 高 | 采用受控斜俯视角；只跟随目标位置 |
| 世界坐标移动与屏幕方向不一致 | 高 | Sprint001D 不修改移动；以固定角度验证可读性 |
| 运行时生成后找不到 Target | 高 | 采用显式本地绑定，不使用每帧场景查找 |
| Camera Rig 放入 Player Prefab | 高 | Rig 保持本地 Scene/Presentation 对象 |
| 遮挡淡化范围膨胀 | 中 | MVP 只做基本碰撞/恢复；不做材质淡化系统 |
| Camera 程序集反向污染 Player | 高 | Camera 可依赖 Player，Player 不依赖 Camera |
| 未来网络玩家生成多个相机 | 高 | 相机只绑定本地玩家；Camera 状态不网络同步 |

## 5. 开始实现前必须完成的条件

1. 从最新 `develop` 创建独立分支，建议：
   `feature/sprint001d-third-person-camera`
2. 明确批准采用 Unity 官方 Cinemachine。
3. 在 Unity Package Manager 中确认并锁定 `com.unity.cinemachine` `3.1.7`，完成一次编译检查后再写 Camera Runtime 代码。
4. 将本 Sprint 镜头行为冻结为：
   - 受控斜俯视第三人称跟随
   - 跟随本地 Player Camera Target 的位置
   - 不随 Player yaw 自由环绕
   - 无 Camera 输入
   - 基础阻尼与遮挡恢复
   - 不做遮挡淡化、震动、瞄准或过场

## 6. 最终结论

**CONDITIONAL GO**

Character Foundation 已满足接入条件，不需要先重构。完成“建立独立 feature 分支”和“批准并锁定 Cinemachine 版本”两项条件后，可以开始 Sprint001D；在此之前不得修改 Unity Camera 业务代码、Player Prefab 或 PlayerSandbox。
