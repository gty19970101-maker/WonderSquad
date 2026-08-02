# Sprint002A Implementation Plan

## 1. 计划信息

- Sprint：Sprint002A
- 名称：Interaction Detection
- 计划日期：2026-08-02
- 当前分支：`feature/sprint002a-interaction-detection`
- 正式基线：`v0.3-character-foundation-final`
- 基线提交：`9af2dba`
- Unity：`6000.3.21f1`
- 当前工作区：干净
- Character Foundation：`GO`
- 本文状态：实施计划，不代表代码已经完成

基线核对结果：

- 当前 HEAD 与 `v0.3-character-foundation-final` 指向同一提交。
- 当前 `develop` 与正式基线指向同一提交。
- Sprint002A 使用独立 feature 分支。
- `WonderSquad.Interaction` 目前只有 Assembly Definition，没有 Interaction Runtime 业务实现。
- 上一版 Preflight 的 Git 条件已经关闭。

## 2. Sprint目标

实现玩家发现附近可交互对象的检测层。

Sprint002A 完成后，系统应能够：

1. 从玩家提供的空间参考点进行有限范围查询。
2. 从物理查询结果中识别实现 `IInteractable` 的目标。
3. 过滤无效、超距、被遮挡或配置不允许的目标。
4. 返回当前最合适的只读检测结果。
5. 在没有有效目标时返回空结果。
6. 不执行交互、不改变目标状态。
7. 不改变 Player 的位置、旋转、输入、Camera 或 CharacterController 行为。

本阶段只建立 Detection，不形成完整“按键→执行→结果反馈”闭环。

## 3. 架构确认

### 3.1 WonderSquad.Core

`WonderSquad.Core` 包含跨领域的 Interaction Contract。

计划位置：

```text
Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/
```

计划命名空间：

```text
WonderSquad.Core.Contracts.Interaction
```

Core 只定义：

- `IInteractable`
- 最小目标标识和值对象
- Detection 查询和结果需要的最小只读数据

Core 不包含：

- Physics 查询。
- MonoBehaviour 生命周期。
- 目标搜索。
- UI。
- 交互执行。
- Puzzle、Inventory 或 Network 规则。

### 3.2 WonderSquad.Interaction

`WonderSquad.Interaction` 包含 Interaction Detection 实现。

计划位置：

```text
Client/Assets/WonderSquad/Runtime/Interaction/Detection/
```

计划命名空间：

```text
WonderSquad.Interaction.Detection
```

该程序集当前已经存在，并保持只引用：

```text
WonderSquad.Core
```

本 Sprint 不新增其他 Runtime 程序集引用。

### 3.3 Player

Player 只能提供：

- Detection Origin 的位置。
- Detection Origin 的朝向。
- 已有本地输入能力。

Sprint002A 不消费 Interact 输入。Player 仅通过 Prefab 组合提供空间参考点；Player 代码不理解 `IInteractable`、Detector、Validator 或任何具体机关。

不修改：

- `PlayerSpawner`
- `PlayerInputReader`
- `PlayerMovement`
- `GroundDetector`
- `PlayerCameraTarget`
- `CameraTargetBinder`

### 3.4 依赖关系

```text
WonderSquad.Player ─────────→ WonderSquad.Core

WonderSquad.Interaction ────→ WonderSquad.Core

Player Prefab
├── Player 组件
└── InteractionDetector 组件
```

Prefab 组合多个程序集的组件，不形成 Player 与 Interaction 的 C# 编译时依赖。

禁止出现：

```text
WonderSquad.Player → WonderSquad.Interaction
WonderSquad.Interaction → WonderSquad.Player
WonderSquad.Interaction → WonderSquad.Puzzle
WonderSquad.Interaction → WonderSquad.Inventory
WonderSquad.Interaction → WonderSquad.Network
```

## 4. 检测方案

### 4.1 确定方案

采用：

```text
非分配范围查询
→ IInteractable 映射与去重
→ InteractionValidator
→ 射线遮挡检测
→ 稳定选择有效目标
```

Unity 实现方向：

- 使用 `Physics.OverlapSphereNonAlloc` 收集范围内 Collider。
- Collider 缓冲区在初始化时按配置创建并复用。
- 使用明确的 Interactable LayerMask 限制查询对象。
- 使用 Raycast 或 Linecast 检查 Detection Origin 与目标锚点之间的遮挡。
- 使用独立 Occlusion LayerMask。
- 同一目标有多个 Collider 时只保留一个候选。
- 不使用 LINQ 或每帧创建 List、数组、闭包和临时查询对象。

### 4.2 选择原因

选择非分配范围查询的原因：

1. 不需要给 Player 增加大范围 Trigger Collider。
2. 不需要添加 Rigidbody，不干扰现有 CharacterController 移动所有权。
3. 每次刷新都从当前 Physics Scene 重建候选，不容易残留已禁用或已销毁目标。
4. 预分配 Collider 缓冲区可避免检测循环持续产生托管垃圾。
5. LayerMask 可以把检测成本限制在明确目标集合。
6. 测试可以主动触发一次检测刷新，结果更容易隔离和复现。

增加射线遮挡检测的原因：

1. 仅按半径会错误发现墙后、地板下或封闭区域内的目标。
2. 遮挡检查符合现有 Interaction System 的距离、方向和可达性规则。
3. 射线结果只影响本地候选，不会被误认为权威执行成功。
4. 未来主机执行层可以使用同一组空间规则重新校验请求。

### 4.3 不采用的默认方案

本阶段不以 Trigger 回调作为主检测方案。

原因：

- Trigger 回调依赖额外的 Collider/Physics Body 组合配置。
- 容易引入 Enter/Exit 顺序和目标禁用后的缓存清理问题。
- 多 Collider 目标会产生重复回调。
- 为 Player 增加 Rigidbody 或额外物理所有权会增加 Character Foundation 回归风险。

本阶段也不使用：

- 每帧 `FindObjectOfType` 或 `FindFirstObjectByType`。
- Tag 或对象名称查找。
- `Physics.OverlapSphere` 的分配版本。
- 全局 Interaction Manager。
- 静态玩家引用。

## 5. 核心职责

### 5.1 InteractionDetector

负责：

- 持有显式 Detection Origin。
- 持有 `InteractionDetectionSettings`。
- 创建并复用 Collider 候选缓冲区。
- 执行非分配范围查询。
- 将 Collider 映射为 `IInteractable`。
- 对同一目标的多个 Collider 去重。
- 收集距离、方向和遮挡证据。
- 调用 `InteractionValidator`。
- 从有效候选中选择稳定的当前目标。
- 对外提供只读当前检测结果。
- Disabled 时清空当前结果。

不负责：

- 读取键盘或手柄。
- 监听 Interact Input Action。
- 调用目标业务方法。
- 修改目标状态。
- 修改 Player Transform。
- 移动 CharacterController。
- 显示 UI。
- 发送 RPC 或 Network Command。

### 5.2 InteractionValidator

负责：

- 验证 Detector 与 Settings 配置是否有效。
- 验证候选对象和 `IInteractable` 引用是否有效。
- 验证目标是否启用且允许被本地检测。
- 验证距离。
- 验证面对方向。
- 验证 Layer Mask。
- 根据 Detector 提供的射线结果判断是否被遮挡。
- 返回明确、只读的验证结果。

建议实现为不依赖 MonoBehaviour 生命周期的普通 `sealed` C# 类型，使规则可以在 EditMode 中独立测试。

Detector 负责采集 Physics 证据；Validator 负责根据证据作出规则判断。Validator 不改变目标、Player 或 Scene。

### 5.3 InteractionExecutor

**不在 Sprint002A 实现。**

未来职责：

- 接收 Interact 输入形成的请求。
- 构造或提交 `InteractionCommand`。
- 调用离线权威端口或 Network Adapter。
- 处理 Accepted、Rejected、Cancelled 与重复 RequestId。
- 将权威结果交给具体目标领域。

本 Sprint 不创建：

```text
InteractionExecutor.cs
```

Detector 不应为了未来 Executor 增加直接执行入口。

### 5.4 IInteractable

`IInteractable` 只定义跨领域契约。

最小契约职责：

- 提供稳定目标标识。
- 提供只读检测锚点。
- 提供只读本地可检测状态。
- 提供候选优先级或稳定排序信息。

不得定义：

- 无条件改变状态的 `Interact()`。
- 门、拉杆、宝箱、资源等具体类型。
- UI 文本或 Widget。
- Player 实现类型。
- Photon、RPC 或 Network Object。
- Inventory、Puzzle 或 Ability 具体对象。

`IInteractable` 的具体签名应保持最小，只添加本 Sprint 检测流程实际需要的成员。

### 5.5 InteractionTarget

计划使用一个最小通用 `InteractionTarget` MonoBehaviour 作为 Unity Collider 与 `IInteractable` Contract 之间的适配组件。

负责：

- 实现 `IInteractable`。
- 保存测试所需的稳定 TargetId。
- 提供显式检测锚点。
- 提供只读可检测状态和优先级。

不负责：

- 执行任何玩法结果。
- 表示门、拉杆或宝箱。
- 修改其他组件。

它用于验证 Detection 基础设施，不成为通用万能业务目标。

### 5.6 InteractionDetectionSettings

所有可调参数进入 `InteractionDetectionSettings` ScriptableObject：

- Detection Radius。
- Facing Angle 或 Minimum Facing Dot。
- Interactable LayerMask。
- Occlusion LayerMask。
- Query Trigger Interaction 策略。
- Candidate Buffer Capacity。
- Focus 切换滞后参数，只有稳定选择确有需要时加入。

禁止把距离、角度、缓冲容量或 Mask 数值散落在 Detector 或 Validator 中。

ScriptableObject 只保存静态配置，不保存当前候选、Focused Target 或任何局内可变状态。

## 6. 检测流程

```text
InteractionDetector 启用
→ 校验 Origin 与 Settings
→ 创建固定容量 Collider 缓冲区
→ 非分配范围查询
→ 跳过 null、禁用或不符合 Layer 的 Collider
→ 查找 Collider 所属 IInteractable
→ 按 TargetId 去重
→ 采集距离和方向
→ 执行遮挡射线
→ InteractionValidator 确认可用性
→ 按优先级、方向、距离和稳定 TargetId 排序
→ 更新只读 Current Target
```

无有效目标：

```text
查询结果为空或全部无效
→ Current Target = None
```

组件禁用：

```text
InteractionDetector.OnDisable
→ 清空候选计数
→ 清空 Current Target
→ 不保留静态状态
```

目标被禁用或销毁：

```text
下次 Detection Refresh
→ Unity Object 有效性检查失败
→ 移除候选
→ Current Target 安全清空或切换
```

## 7. 目标选择规则

候选选择必须确定且稳定，建议依次比较：

1. 是否通过 Validator。
2. 配置优先级。
3. 面对方向得分。
4. 距离。
5. 稳定 TargetId。

规则约束：

- 同一输入和场景状态应得到相同目标。
- 不依赖 `Physics` 返回 Collider 的数组顺序。
- 不依赖 GameObject 名称或 Hierarchy 顺序。
- 同一目标的多个 Collider 不增加目标权重。
- 目标发生轻微距离变化时不应频繁闪烁切换。
- Buffer 满时输出明确诊断并保持安全结果，不能越界或产生随机选择。

## 8. 本Sprint范围

### 8.1 包含

- `IInteractable` 最小只读 Contract。
- `InteractionDetector`。
- `InteractionValidator`。
- `InteractionTarget` 测试适配组件。
- `InteractionDetectionSettings`。
- 非分配范围查询。
- 距离、方向、Layer Mask 和遮挡验证。
- 稳定候选选择。
- Player Prefab 的 Detection Origin 与 Detector 组合。
- GameplaySandbox 检测验证环境。
- EditMode 与 PlayMode 测试。
- Character Foundation 回归验证。

### 8.2 明确禁止

本 Sprint 不实现：

- 按 `E` 交互。
- 手柄交互按键消费。
- UI 提示。
- 高亮、图标或正式交互表现。
- `InteractionExecutor`。
- Door。
- Lever。
- Chest。
- Button。
- 资源采集。
- Puzzle。
- Inventory。
- Item 业务。
- Ability 业务。
- Crafting。
- Rescue。
- Network。
- Photon Fusion。
- RPC。
- 多人持续交互。
- 交互冷却。
- 全局事件总线。

不得修改 `WonderSquad.inputactions`；Interact Action 虽然已经存在，但本 Sprint 不读取它。

## 9. 计划新增文件

以下文件为计划清单，当前尚未创建。

### 9.1 Core Contracts

```text
Client/Assets/WonderSquad/Runtime/Core/
└── Contracts/
    └── Interaction/
        ├── IInteractable.cs
        └── InteractionTargetId.cs
```

若实现发现 `InteractionTargetId` 不能带来当前测试需要的稳定语义，可以使用一个最小稳定值对象；不得直接以 GameObject 名称作为 TargetId。

### 9.2 Interaction Runtime

```text
Client/Assets/WonderSquad/Runtime/Interaction/
└── Detection/
    ├── InteractionDetector.cs
    ├── InteractionValidator.cs
    ├── InteractionTarget.cs
    └── InteractionDetectionSettings.cs
```

不新增 `InteractionExecutor.cs`。

### 9.3 配置与 Prefab

```text
Client/Assets/WonderSquad/ScriptableObjects/Configuration/
└── InteractionDetectionSettings.asset

Client/Assets/WonderSquad/Prefabs/Interaction/
└── PF_Interactable_Test.prefab
```

所有 Unity 文件和目录必须包含对应 `.meta`。

### 9.4 Tests

```text
Client/Assets/WonderSquad/Tests/EditMode/
└── InteractionDetectionEditModeTests.cs

Client/Assets/WonderSquad/Tests/PlayMode/
└── InteractionDetectionPlayModeTests.cs
```

测试辅助类型优先保留在 Tests；只有 GameplaySandbox 的通用检测目标确实需要 Runtime 组件时，才使用 `InteractionTarget`。

### 9.5 Sprint文档

实现阶段完成后计划生成：

```text
Docs/01_Project/Sprint002A_Implementation_Report.md
Docs/01_Project/Sprint002A_Review_Checklist.md
```

并更新：

```text
CHANGELOG.md
```

## 10. 计划修改文件

### 10.1 Unity资产

```text
Client/Assets/WonderSquad/Prefabs/Player/Player.prefab
Client/Assets/WonderSquad/Scenes/Tests/GameplaySandbox.unity
```

Player Prefab 只增加：

- `InteractionOrigin`。
- `InteractionDetector`。
- `InteractionDetectionSettings` 引用。

GameplaySandbox 只增加：

- 近距可检测目标。
- 远距目标。
- 遮挡目标。
- 空目标验证区。
- 必要的占位 Occluder。
- 可选的只读调试 Gizmo；不得加入正式 UI。

### 10.2 Project Settings

计划在 `Client/ProjectSettings/TagManager.asset` 的首个可用用户层加入：

```text
Interactable
```

要求：

- 通过 Unity Layer 配置写入。
- 不硬编码 Layer 数字。
- 不移动或覆盖现有 Layer。
- Player 保持原有 Layer 和碰撞行为。

若实施前确认无需新增层，也必须保留显式 LayerMask 配置，并在 Implementation Report 记录最终决定。

### 10.3 Test Assembly

```text
Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef
Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef
```

只增加：

```text
WonderSquad.Interaction
```

`WonderSquad.Core` 已存在，无需重复调整。Runtime asmdef 不增加 Player、Camera 或其他领域引用。

## 11. 不允许修改的文件

除非在实现中发现真实阻塞并先停止报告，否则不修改：

```text
Client/Assets/WonderSquad/Runtime/Player/Spawning/PlayerSpawner.cs
Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInputReader.cs
Client/Assets/WonderSquad/Runtime/Player/Movement/PlayerMovement.cs
Client/Assets/WonderSquad/Runtime/Player/Movement/GroundDetector.cs
Client/Assets/WonderSquad/Runtime/Player/Camera/PlayerCameraTarget.cs
Client/Assets/WonderSquad/Runtime/Camera/
Client/Assets/WonderSquad/Settings/Input/WonderSquad.inputactions
Client/Packages/manifest.json
Client/Packages/packages-lock.json
```

不得修改以下程序集行为：

- Puzzle
- Inventory
- Item
- Ability
- Crafting
- Status
- Communication
- Network
- UI

## 12. 程序集依赖关系

### 12.1 Runtime

| 程序集 | 允许依赖 | Sprint002A变化 |
|---|---|---|
| `WonderSquad.Core` | 无 WonderSquad Runtime 依赖 | 增加 Interaction Contract 文件，不改 asmdef |
| `WonderSquad.Interaction` | `WonderSquad.Core` | 保持不变 |
| `WonderSquad.Player` | `WonderSquad.Core`、`Unity.InputSystem` | 保持不变 |
| `WonderSquad.Camera` | Core、Player、Cinemachine | 保持不变 |

### 12.2 Tests

| 程序集 | 计划新增依赖 |
|---|---|
| `WonderSquad.Tests.EditMode` | `WonderSquad.Interaction` |
| `WonderSquad.Tests.PlayMode` | `WonderSquad.Interaction` |

Tests 可以引用 Runtime；Runtime 不得引用 Tests。

### 12.3 依赖审计标准

完成时必须确认：

- Player asmdef 不包含 Interaction。
- Interaction asmdef 不包含 Player。
- Interaction asmdef 不包含 Network 或 Photon。
- 没有循环引用。
- Editor API 未进入 Runtime。
- 测试代码未进入 Player Build。

## 13. EditMode测试设计

计划文件：

```text
InteractionDetectionEditModeTests.cs
```

### 13.1 Detector配置

1. `InteractionDetector` 缺少 Detection Origin 时配置无效。
2. 缺少 `InteractionDetectionSettings` 时配置无效。
3. 配置完整时返回有效状态。
4. Candidate Buffer Capacity 与实际缓冲区初始化一致。
5. Detector 禁用时不保留当前目标。

### 13.2 Layer Mask

1. Interactable LayerMask 为空时配置无效。
2. 指定 Interactable Layer 时目标允许进入候选。
3. 不在 Interactable Layer 的 Collider 被忽略。
4. Occlusion LayerMask 可独立于 Interactable Layer。
5. Layer 使用配置值，不硬编码用户 Layer 数字。

### 13.3 Detection参数

1. Detection Radius 必须为有限正数。
2. Detection Radius 为 `0`、负数、NaN 或 Infinity 时无效。
3. Facing 参数必须处于合法范围。
4. Candidate Buffer Capacity 必须大于 `0`。
5. 可调参数全部来自 Settings。

### 13.4 Validator规则

1. 范围内且无遮挡目标有效。
2. 超出范围目标无效。
3. 背向且不符合 Facing 条件的目标无效。
4. 遮挡目标无效。
5. null、禁用或不可检测目标无效。
6. 验证过程不改变目标或 Player 状态。

### 13.5 稳定排序

1. 两个目标结果不依赖 Physics 返回顺序。
2. 优先级更高目标胜出。
3. 优先级相同时按方向和距离选择。
4. 完全相同时按稳定 TargetId 决定。
5. 同一 TargetId 的多个 Collider 只产生一个候选。

### 13.6 架构

1. `WonderSquad.Interaction` 只引用 `WonderSquad.Core`。
2. `WonderSquad.Player` 未引用 `WonderSquad.Interaction`。
3. `IInteractable` 位于 Core Contract 命名空间。
4. 不存在 `InteractionExecutor` Runtime 实现。

## 14. PlayMode测试设计

计划文件：

```text
InteractionDetectionPlayModeTests.cs
```

### 14.1 附近发现目标

步骤：

1. 创建 Player Detection Origin。
2. 在半径内放置通用 Interactable Target。
3. 等待 Physics 同步并刷新 Detection。

完成标准：

- Detector 返回该目标。
- 目标状态没有变化。
- Player Transform 没有因检测发生变化。

### 14.2 无目标返回空

覆盖：

- 场景没有 Interactable。
- 目标在范围外。
- 目标 Layer 不匹配。
- 目标被禁用或销毁。

完成标准：

- Current Target 为空。
- 不产生 NullReferenceException。

### 14.3 遮挡目标不可发现

步骤：

1. 将目标放在 Detection Radius 内。
2. 在 Origin 与目标锚点之间放置属于 Occlusion Mask 的墙体。
3. 刷新 Detection。
4. 移除墙体后再次刷新。

完成标准：

- 有墙时目标无效。
- 移除墙体后目标可以被发现。
- 墙体本身不会成为 Interaction Target。

### 14.4 不影响PlayerMovement

步骤：

1. 使用正式 Player Prefab。
2. 记录无输入时的位置和旋转。
3. 运行多个 Detection Refresh。
4. 使用既有 Move Input 驱动玩家经过检测区。

完成标准：

- Detector 不直接修改 Player Transform。
- CharacterController 仍是唯一移动核心。
- 无输入时不产生水平漂移。
- 有输入时移动、转向、重力和 Camera 行为与 Character Foundation 一致。

### 14.5 不产生GC异常

测试目标：

- Detector 初始化可以分配固定缓冲区。
- 完成预热后，重复检测路径不创建新的 Collider 数组、List、LINQ Enumerator、闭包或目标包装对象。
- 通过 Unity Profiler Recorder 或等价的受控分配测试观察 Detector 自身刷新路径。
- 测试框架和 Physics 内部不可归因噪声必须与 Detector 代码分配区分记录，避免建立不稳定的 Editor 全帧零分配断言。

完成标准：

- Detector 的稳定刷新路径没有可归因的托管分配。
- Buffer 饱和不会数组越界或触发异常。
- Console 不出现 GC 相关错误、内存增长警告或异常。

### 14.6 生命周期

1. Detector Disable 后 Current Target 立即清空。
2. Disable 期间不更新目标。
3. 重新 Enable 后恢复检测。
4. 重复 Enable/Disable 不创建重复状态或订阅。
5. 目标销毁后安全清理引用。

### 14.7 Character Foundation回归

继续运行：

- Bootstrap Tests。
- PlayerSpawner Tests。
- PlayerInputReader Tests。
- PlayerMovement Tests。
- Camera Tests。

基线：

```text
EditMode: 37 Passed / 0 Failed
PlayMode: 19 Passed / 0 Failed
```

Sprint002A 完成后不得出现既有测试回退。

## 15. GameplaySandbox配置计划

使用：

```text
Client/Assets/WonderSquad/Scenes/Tests/GameplaySandbox.unity
```

计划区域：

```text
InteractionValidationRoot
├── NearTarget
├── FarTarget
├── OccludedTarget
├── OccluderWall
├── OverlappingTargetA
├── OverlappingTargetB
└── EmptyDetectionArea
```

配置要求：

- 使用临时模型。
- 不创建正式 UI。
- 可使用 Gizmo 或 Inspector 只读字段观察 Current Target。
- 不需要按 `E`。
- 目标不能执行动画或改变状态。
- 不加入 Door、Lever、Chest、Puzzle 或 Inventory。
- 保留 P0 Scene 基础契约和 Character Foundation 行为。

## 16. 推荐实施顺序

### S002A-01：建立Core Contract

- 创建最小 `IInteractable` 和 TargetId。
- 确认 Core 不依赖 Interaction。
- 先写 Contract 与 asmdef 依赖测试。

### S002A-02：建立Detection Settings

- 创建数据化范围、方向、LayerMask 与缓冲容量。
- 完成配置校验 EditMode Tests。

### S002A-03：实现InteractionValidator

- 先实现无副作用验证规则。
- 完成距离、方向、遮挡证据和无效目标测试。

### S002A-04：实现通用InteractionTarget

- 只提供 Contract 所需数据。
- 不加入执行能力。

### S002A-05：实现InteractionDetector

- 建立预分配缓冲区。
- 完成范围查询、去重、验证和稳定选择。
- 完成 Disable/Enable 生命周期。

### S002A-06：组合Player Prefab

- 新增 Detection Origin 和 Detector。
- 不修改 Player 代码或 CharacterController。

### S002A-07：配置GameplaySandbox

- 创建近距、远距、遮挡、重叠和空目标区域。
- 只使用占位资源。

### S002A-08：完成PlayMode与GC验证

- 执行检测行为、生命周期、Movement 不受影响和分配检查。

### S002A-09：完整回归与人工验收

- 完整 EditMode。
- 完整 PlayMode。
- GameplaySandbox 人工检查。
- Console Error = 0。

### S002A-10：文档和Gate准备

- 生成 Implementation Report。
- 生成 Review Checklist。
- 更新 CHANGELOG。
- 执行范围和程序集依赖审计。

## 17. 完成标准

Sprint002A 只有满足以下条件才可标记完成：

1. 玩家附近的有效通用目标可以被发现。
2. 无目标时返回空结果。
3. 墙后目标不能成为有效检测结果。
4. LayerMask 和 Detection 参数全部来自 Settings。
5. 同一目标的多个 Collider 不产生重复候选。
6. 候选选择不依赖 Physics 返回顺序。
7. Detector 和 Validator 不改变目标状态。
8. Detector 不读取 Interact Input。
9. Detector 不影响 PlayerMovement、CharacterController 或 Camera。
10. 稳定刷新路径不产生 Detector 可归因的托管分配。
11. `WonderSquad.Interaction` 仍只依赖 `WonderSquad.Core`。
12. Player 程序集不依赖 Interaction。
13. 不存在 `InteractionExecutor` 实现。
14. 未实现 UI、机关、Puzzle、Inventory 或 Network。
15. 新增 EditMode 和 PlayMode 测试通过。
16. Character Foundation 回归测试通过。
17. GameplaySandbox 可以独立运行，Console Error 为 0。
18. Prefab、Scene、Settings 与 `.meta` 完整。

## 18. 风险与应对

| 风险 | 级别 | 应对 |
|---|---|---|
| Detector 被扩展成执行器 | 高 | 不创建 Executor，不在 IInteractable 中放状态变更方法 |
| Player 与 Interaction 形成双向依赖 | 阻塞 | 两个程序集只共同依赖 Core |
| Trigger/Rigidbody 影响 CharacterController | 高 | 使用非分配范围查询，不给 Player 增加 Rigidbody |
| Collider Buffer 满导致漏目标 | 中 | 数据化容量、饱和诊断和密集候选测试 |
| Physics 返回顺序不稳定 | 高 | 明确排序和稳定 TargetId 平局规则 |
| 相邻目标频繁切换 | 中 | 稳定评分，必要时加入数据化切换滞后 |
| 自身 Collider 阻挡射线 | 中 | LayerMask、射线起点和目标归属规则专项测试 |
| 多 Collider 目标重复 | 中 | 按 TargetId 去重 |
| Editor 全帧GC测试不稳定 | 中 | 测量 Detector 自身刷新路径并记录测试噪声边界 |
| 顺带加入UI或具体机关 | 高 | 严格执行第8.2节禁止范围 |

## 19. 实施开始门禁

当前实施准备状态：

| 条件 | 状态 |
|---|---|
| 正式基线包含完整 Character Foundation | 已满足 |
| `develop` 指向正式基线 | 已满足 |
| Sprint002A 独立分支 | 已满足 |
| 工作区干净 | 已满足 |
| Interaction asmdef 存在且依赖正确 | 已满足 |
| Detection 与 Execution 边界明确 | 已满足 |
| 文件与测试计划明确 | 已满足 |

计划结论：

# READY FOR IMPLEMENTATION

该结论只表示 Sprint002A 的实施计划已明确。本次没有开始编写代码；后续必须由单独实施指令启动。
