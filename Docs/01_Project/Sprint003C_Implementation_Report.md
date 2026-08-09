# Sprint003C — Root Bridge Advantage / Environment Consequence Implementation Report

## 1. 当前状态

`SPRINT003C VISIBLE GEOMETRY CLEANUP — VERIFIED`

- Unity 目标版本：`6000.3.21f1`
- 实现基线：Sprint003A `PASSED / GO`、Sprint003B `PASSED / GO`
- 最终验收环境：Unity `6000.3.21f1`
- 第一轮空间分离解决了 Bounds 重叠，但正式 Unity 人工复验确认两条路线仍长距离并排、旧 Guardrail 失去职责，玩法可读性不合格。折线主路、中央直连捷径及后续 Visible Geometry Cleanup 均已完成正式 Unity 复验。

## 2. 已实现内容

### 只读 Snapshot 消费

`SleepingForestRootBridgeConsequence` 只持有 Inspector 显式配置的 `ForestSignalTrial`、`RootBridgeAdvantageSpan` 与 `RootBridgeAdvantageVisual`。

- 在 `OnEnable` 订阅 Trial **实例**的 `TrialStateChanged`，并读取 `CurrentSnapshot` 完成首次同步。
- 只依据 Snapshot 的 `IsCompleted` 与 `Revision` 决定 `Initial`/`Activated`。
- 相同或旧 Revision 不会重复写视觉、切换 Collider 或创建对象。
- Disable/Enable 后重新读取当前 Snapshot；不写 Trial、不读取 Trial 私有字段、不使用 static 状态、全局事件或 `Update` 轮询。

### 独立 Span 与视觉

- `RootBridgeAdvantageSpan` 只管理独立 `GroundSurface` 的 Renderer 与承载 Collider。
- `RootBridgeAdvantageVisual` 只管理 Marker 和每 Renderer 的 `MaterialPropertyBlock`；没有访问 `Renderer.material`，不写 `sharedMaterial`。
- `Initial`：Span Renderer/Collider 关闭、Dormant Marker 显示。
- `Activated`：Span Renderer/Collider 同时开启、Activated Direction Marker 显示。
- 视觉反馈含颜色覆盖及非颜色的方向性 Marker；没有 Animator、Tween、VFX、音效或 Shader 改造。

### 主桥和 Foundation 边界

`RootBridgeTemporaryCrossing` 仍未被 Runtime 组件引用为可写对象。显式 003C Authoring 将它作为永久主路的东向第一段，新增外侧北向段和折返终点段；Renderer 与 Collider 同步变换并始终启用。两个失去职责的旧 Guardrail 已删除。Player、Camera、Input、Movement、GroundDetector、FallRecovery、Beacon、ForestSignalTrial、Interaction Foundation 及 003D Completion 均未修改。

## 3. 新增文件

Runtime（`WonderSquad.Puzzle`）：

- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/SleepingForestRootBridgeState.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/SleepingForestRootBridgeConsequence.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/RootBridgeAdvantageSpan.cs`
- `Client/Assets/WonderSquad/Runtime/Puzzle/SleepingForest/RootBridgeAdvantageVisual.cs`

Editor（`WonderSquad.Editor`）：

- `Client/Assets/WonderSquad/Editor/SleepingForestRootBridgeConsequenceAuthoring.cs`

测试：

- `Client/Assets/WonderSquad/Tests/EditMode/SleepingForestRootBridgeConsequenceEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/SleepingForestRootBridgeConsequencePlayModeTests.cs`

文档：

- 本报告
- `Docs/01_Project/Sprint003C_Review_Checklist.md`

## 4. Prefab、Scene 与几何 Authoring

编译后，在 Unity 菜单执行：

```text
Wonder Squad > Sprint003C > Apply Root Bridge Consequence
```

命令会先显示确认对话框并走 Unity 的保存确认流程；取消时不执行修改。它只创建/更新：

- `Assets/WonderSquad/Prefabs/SleepingForest/PF_RootBridgeAdvantageSpan.prefab`
- SleepingForest 中的 `ForestSignalRouteConsequence` 根、两段永久主路补充几何和一个直连 Span Prefab 实例；旧 Start/Exit Junction 与旧 Guardrail 均不再保留。

它不会运行 003A Greybox Builder，不会修改 PlayerSandbox、测试场景、Build Profile、ProjectSettings、Recovery、Foundation 或 003D Completion 对象。它会幂等设置三段永久折线主路、删除旧 Guardrail，并重建 003C 局部后果根。

几何采用单层、边缘相接策略：

| 对象 | Position | Scale | 连接规则 |
| --- | --- | --- | --- |
| `RootBridgeTemporaryCrossing` | `(8.5, -0.5, 23)` | `(9, 1, 6)` | 永久主路第一段：由入口向东，形成第一次明确转向 |
| `RootBridgeMainRouteOuterLeg` | `(16, -0.5, 34)` | `(6, 1, 22)` | 永久主路外侧长段：向北绕过中间空区 |
| `RootBridgeMainRouteReturn` | `(11.5, -0.5, 45)` | `(3, 1, 6)` | 永久主路折返段：由外侧汇入 Slice End |
| `RootBridgeAdvantageSpan/GroundSurface` | `(0, -0.5, 35)` | `(3, 1, 16)` | Trial Completed 后直接跨越入口与 Slice End 之间的中间空区 |

Authoring 会同时校验 Renderer 与禁用状态 Collider 的配置 Bounds、所有转角连接宽度、无正体积重叠、旧 Guardrail 不存在、外侧平行段与捷径至少相隔 `10m`，且捷径长度不超过主路的 `65%`。按各段中心线计算：Main Route 约 `46.24m`，Advantage Route 为 `27.00m`，缩短约 `41.6%`。捷径跳过永久主路的东向段、外侧北向长段及两次转向。

## 5. 自动化测试

已新增专项测试覆盖：

- Initial 状态的 Renderer、Collider、Marker；
- A → B Completed 后的 Span 激活；
- IncorrectOrder 不激活，随后 A → B 可恢复；
- 同一 Revision 的启停/重新订阅幂等；
- `MaterialPropertyBlock` 不污染共享材质；
- 两个独立 Span Visual 在共享材质时仍保持各自的 PropertyBlock/Marker 状态；
- Span 与 Main Bridge 无正体积重叠；
- 正式场景初始组合；
- 正式场景 A → B 后的 Span 激活、主桥不变、Player/Camera/FallRecovery 回归。

空间分离缺陷发现前的原 Sprint003C Unity `6000.3.21f1` 验收基线：

| 测试 | 最终结果 |
| --- | --- |
| Sprint003C EditMode | `8 / 8 Passed`，`0 Failed`，`0 Skipped` |
| Sprint003C PlayMode | `4 / 4 Passed`，`0 Failed`，`0 Skipped` |
| 完整 EditMode 回归 | `108 / 108 Passed`，`0 Failed`，`0 Skipped` |
| 完整 PlayMode 回归 | `58 / 58 Passed`，`0 Failed`，`0 Skipped` |
| Console Error | `0` |

## 6. Unity Verification 问题与修复记录

### 6.1 EditMode Test `CS0104` Object 命名歧义

`SleepingForestRootBridgeConsequenceEditModeTests.cs` 同时使用 `System` 与 `UnityEngine`，测试中的 `Object` 调用产生 C# 命名歧义。

修复方式：Unity 对象相关调用显式使用 `UnityEngine.Object`。该修复只消除测试程序集编译错误，没有修改 Runtime、断言逻辑或测试行为。

### 6.2 Span / Visual 初始状态同步

`SleepingForestRootBridgeState.Initial` 的枚举值为 `0`。若生命周期仅依赖默认字段值判断“状态是否变化”，首次同步可能把默认值误认为已应用的状态，从而跳过 Renderer、Collider 或 Marker 的显式初始化。

修复方式：生命周期初始化显式应用 Initial 状态，不以枚举默认值代替“状态已同步”标记。该修复确保 Scene 初始、Configure 和重新启用时的表现与逻辑状态一致。

### 6.3 最终主要 EditMode 失败：测试夹具生命周期顺序

最终五项 EditMode 失败的共同根因位于 `SleepingForestRootBridgeConsequenceEditModeTests` 测试夹具：

```text
Consequence.Configure 时 Fixture 根对象 inactive
    ↓
isActiveAndEnabled == false
    ↓
Configure 不建立 Trial 实例事件订阅
    ↓
后续 Completed Revision 没有到达 Consumer
```

最终修复顺序：

```text
创建 Consequence
    ↓
激活 Fixture
    ↓
Configure
    ↓
建立实例事件订阅并读取 Initial Snapshot
    ↓
正常接收后续 Revision 事件
```

该问题属于 **003C EditMode 测试夹具生命周期**，不是 `ForestSignalTrial`、Revision、Beacon A/B 或 Interaction Foundation 缺陷。没有为测试通过而直接调用 Span 激活，也没有改写 003B 状态机。

## 7. 原人工验收结果与后续缺陷

正式 `SleepingForest` Scene 人工验收全部通过：

- ForestSignalTrial Completed 后 `RootBridgeAdvantageSpan` 正确激活；
- Main Route 永久保持可通行；
- 当时仅确认 Advantage Span 可激活和可走；Sprint003D 人工验收进一步发现它与主桥近似平行、长度无优势，因此“形成捷径收益”的旧结论已撤销并由本轮空间分离修复取代；
- IncorrectOrder 不激活捷径，之后 A → B 可恢复并激活；
- 重复相同 Revision 不重复产生环境后果；
- `RootBridgeTemporaryCrossing` 在原验收时未移动；本轮仅将其与护栏整体安全东移，未禁用、替换或修改承载 Collider；
- 玩家位于桥附近激活环境后果时无卡死、掉落或位移异常；
- Renderer / Collider 状态同步正常；
- 无新的 Z-fighting；
- `MaterialPropertyBlock` 视觉隔离正常，无 shared Material 污染；
- FallRecovery、Movement、GroundDetector、Camera 正常；
- Beacon Detection、Prompt、Execution 与 Interaction 回归正常；
- Console Error = `0`。

## 8. 已执行的 Unity 验证步骤

1. 用 Unity `6000.3.21f1` 打开项目，等待编译完成，确认 Console 无 Error。
2. 执行 `Wonder Squad > Sprint003C > Apply Root Bridge Consequence`，确认保存对话框。
3. 在 SleepingForest Hierarchy 检查只有一个 `ForestSignalRouteConsequence`、一个 `RootBridgeAdvantageSpan`；主桥为 `(5.75, -0.5, 35)`、Scale 保持 `(7, 1, 16)`，两侧护栏与主桥边缘对齐。
4. 运行 `SleepingForestRootBridgeConsequenceEditModeTests`，再运行全部 EditMode 测试。
5. 运行 `SleepingForestRootBridgeConsequencePlayModeTests`，再运行全部 PlayMode 测试。
6. 在 SleepingForest Play Mode 进行 A → B：观察 Span 与方向 Marker 激活；走 Main Route 验证仍可通行；再走 Advantage Route 验证它是独立的空间选择。
7. 在 Main Bridge/Junction 附近完成 A → B，确认无位移、掉落、卡死、GroundDetector 抖动或 CharacterController 异常。
8. 验证 B → A → B、重复 Completed、FallRecovery、Movement、Camera、Prompt 和 Console Error `0`。

以上步骤已在 Unity `6000.3.21f1` 正式项目完成。Main Route、Advantage Route、Trial 恢复链路、桥区安全性及关联 Foundation 回归均通过，Console Error 为 `0`。

## 9. 已知限制与 Sprint003D 边界

- 003C 不触发 Slice End、Level Completed、Completion UI 或 Build Profile 接入。
- 003C 不实现网络、存档、角色能力、Inventory、通用 Puzzle/Bridge 框架或失败系统。
- Advantage Route 当前只表达《沉睡森林》本地 Vertical Slice 的可选捷径收益；角色能力、网络权威和跨场景状态不属于 003C。
- 当前环境后果仅是 Trial Snapshot 的本地派生表现；未来网络应同步 Trial 的权威 Snapshot/Revision，而不是同步 Visual 私有状态。

## 10. Advantage Route Spatial Separation Fix（已被正式人工复验否决）

### 原问题

原布局为：主桥 `(0, -0.5, 35)` / `(7, 1, 16)`；Advantage Span `(-5, -0.5, 34.5)` / `(2, 1, 15)`，再由西侧 Start/Exit Junction 连接同一入口和终点。虽然 Renderer/Collider 没有正体积重叠，但两条路线长度与职责近似，绿色 Span 只是平行覆盖式表现，没有可感知捷径收益。

### 修复方式

- 永久主桥及护栏整体安全东移；桥面 Scale、Renderer、Collider enabled 状态均保持不变。
- Advantage Span 改为 `x = 0` 的单层直连桥，宽 `2.5m`、长 `16m`；Initial 时仍不可见且不承载，Completed 后一次性激活。
- 删除旧 003C Start/Exit Junction；入口 `RootBridgeEntranceJunction` 和 `SliceEndGround` 直接承接两条路线。
- 主桥中段 Bounds：X `[2.25, 9.25]`、Z `[27, 43]`；捷径中段 Bounds：X `[-1.25, 1.25]`、Z `[27, 43]`，横向净间隔 `1m`，无正体积重叠、共面覆盖或双层 Ground Collider。
- 主路约 `32.06m`；捷径 `27.00m`，长度收益约 `15.8%`。

### 自动验证

隔离 Unity `6000.3.21f1` 实际结果：

| 测试 | 结果 |
| --- | --- |
| Sprint003C EditMode | `9 / 9 Passed`，`0 Failed`，`0 Skipped` |
| Sprint003C PlayMode | `5 / 5 Passed`，`0 Failed`，`0 Skipped` |
| 完整 EditMode | `122 / 122 Passed`，`0 Failed`，`0 Skipped` |
| 完整 PlayMode | `63 / 63 Passed`，`0 Failed`，`0 Skipped` |

该轮自动化覆盖永久主桥存在、Span Initial/Activated、几何独立、最小连接宽度、至少 `15%` 路径收益、Revision 幂等以及 Foundation/003D 回归。但正式 Unity 人工复验确认 `32.06m` 对 `27.00m` 的并排路线缺少可读性，且随桥移动的两个 Guardrail 成为无意义长条，因此该布局不再是当前实现。

## 11. Advantage Route Readability Fix

### 删除的无意义 Geometry

- `Boundary_RootBridgeWestGuardrail`：第一轮移动后位于两条路线之间，侵入主要视觉/行走走廊，删除。
- `Boundary_RootBridgeEastGuardrail`：第一轮移动后成为远离有效边缘的悬空长条，删除。
- 旧 `RootBridgeAdvantageStartJunction` / `RootBridgeAdvantageExitJunction` 已在上一轮移除，本轮继续不生成。

### 新路线结构

永久 Main Route 由三个边缘相接的单层 Collider 组成：

```text
入口 ──向东── RootBridgeTemporaryCrossing
                    │
                    └──向北── RootBridgeMainRouteOuterLeg
                                      │
                                      └──向西── RootBridgeMainRouteReturn ── Slice End

入口 ───────── Completed 后出现的 RootBridgeAdvantageSpan ───────── Slice End
```

- Main Route 中心线约 `46.24m`，更宽、永久存在，包含两次明确转向和一段外侧绕行。
- Advantage Route 中心线 `27.00m`，宽 `3m`，无需精准平衡操作；完成 Trial 后直接跨过整个中间空区。
- 捷径缩短约 `41.6%`，跳过 `RootBridgeTemporaryCrossing` 东向段、`RootBridgeMainRouteOuterLeg` 外侧长段及折返转向。
- 外侧北向段与绿色 Span 横向净距离为 `11.5m`；它们不再构成贴邻并排双桥。
- 两条路线最终都汇入同一个 `SliceEndGround`，Completion 条件保持不变。

### Renderer / Collider 审计

- 所有永久主路 Renderer 与 BoxCollider 同位置、同 Scale 且始终启用。
- Advantage Span Initial 时 Renderer/Collider 关闭，Completed 后同步启用。
- 主路各段、捷径、入口和 Slice End 之间无正体积 Ground 重叠；失去职责的旧 Root Landmark 长条已移除。
- 每个路线连接面有效宽度不少于 `1.5m`；无双层 Collider 或共面覆盖。
- 测试明确要求旧 Guardrail Scene 对象不存在，避免 Authoring 回退。

### 隔离 Unity 自动验证

Unity `6000.3.21f1`：

| 测试 | 结果 |
| --- | --- |
| Sprint003C EditMode | `9 / 9 Passed`，`0 Failed`，`0 Skipped` |
| Sprint003C PlayMode | `5 / 5 Passed`，`0 Failed`，`0 Skipped` |
| 完整 EditMode | `122 / 122 Passed`，`0 Failed`，`0 Skipped` |
| 完整 PlayMode | `63 / 63 Passed`，`0 Failed`，`0 Skipped` |

正式 Unity 已确认：主路转角无绊脚，绿色捷径视觉上立即可读，无 Z-fighting 或双层 Ground，GroundDetector 无抖动，FallRecovery、Camera 与 Completion 正常，Console Error 为 `0`。

## 12. Visible Geometry 精确审计与清理

### 实际可见对象

正式 `SleepingForest.unity` 的序列化层级、Transform、MeshFilter、MeshRenderer 与 BoxCollider 审计确认，截图中的两条棕色长条不是上一轮已删除的 `Boundary_RootBridgeWestGuardrail` / `Boundary_RootBridgeEastGuardrail`，而是两个独立的 003A Greybox 地标 Cube：

| 侧别 | 完整 Hierarchy Path | Position / Rotation / Scale | Renderer Bounds | Collider Bounds | 创建来源 |
| --- | --- | --- | --- | --- | --- |
| 左 | `SleepingForestRoot/Landmarks/RootBridgeLandmark_LeftRoot` | `(-5,1.5,35)` / `(0,0,0)` / `(2,3,12)` | Center `(-5,1.5,35)`，Size `(2,3,12)`，Min `(-6,0,29)`，Max `(-4,3,41)` | 与 Renderer 相同 | 003A `SleepingForestGreyboxSceneBuilder.CreateRootBridgeLandmark`，不是 Prefab |
| 右 | `SleepingForestRoot/Landmarks/RootBridgeLandmark_RightRoot` | `(5,1.5,35)` / `(0,0,0)` / `(2,3,12)` | Center `(5,1.5,35)`，Size `(2,3,12)`，Min `(4,0,29)`，Max `(6,3,41)` | 与 Renderer 相同 | 003A `SleepingForestGreyboxSceneBuilder.CreateRootBridgeLandmark`，不是 Prefab |

同一区域所有带 MeshFilter、MeshRenderer 与 BoxCollider 的主要 Scene Cube 审计如下；Renderer 与 Collider 使用同一 Transform/单位 Cube，因此表中 World Bounds 对两者一致：

| Hierarchy Path / 对象 | World Bounds Min → Max | Gameplay 职责 |
| --- | --- | --- |
| `SleepingForestRoot/Environment/MainRoute/JunctionPlatforms/RootBridgeEntranceJunction` | `(-4,-1,19) → (4,0,27)` | 两路线共同入口 |
| `SleepingForestRoot/Environment/MainRoute/RootBridgeTemporaryCrossing` | `(4,-1,20) → (13,0,26)` | 永久 Main Route 第一段 |
| `ForestSignalRouteConsequence/RootBridgeMainRouteOuterLeg` | `(13,-1,23) → (19,0,45)` | 永久 Main Route 外侧长段 |
| `ForestSignalRouteConsequence/RootBridgeMainRouteReturn` | `(10,-1,42) → (13,0,48)` | 永久 Main Route 折返段 |
| `ForestSignalRouteConsequence/RootBridgeAdvantageSpan/GroundSurface` | `(-1.5,-1,27) → (1.5,0,43)` | Completed 后启用的中央捷径 |
| `SleepingForestRoot/Environment/Terrain/SliceEndGround` | `(-10,-1,43) → (10,0,57)` | 两路线共同终点地面 |
| `SleepingForestRoot/Landmarks/SliceEndLandmark_Arch` | `(-5,4.25,45.75) → (5,5.75,47.25)` | 高位终点地标，不承载玩家 |
| `SleepingForestRoot/Environment/Boundaries/Boundary_North` | `(-32,0,57.5) → (32,2,58.5)` | 地图北边界，不位于捷径两侧 |

相关区域的其余承载 Cube 均有明确职责：入口 Junction、三段永久 Main Route、Initial 时关闭的中央 Advantage Ground、Slice End Ground；`Boundary_North` 位于 Slice End 北边界，不是截图长条。两个 Root Landmark 的底面只在 `Y=0` 接触地面高度，但其 X/Z 位置在新路线布局中没有桥面承载，因此表现为绿色捷径两侧悬空实体。

### 为什么上一轮删除后仍存在

上一轮仅精确删除了两个 `Boundary_RootBridge*Guardrail`。这两个 Root Landmark 使用不同名称、由 003A Builder 的另一条创建路径生成，而且旧 003C Authoring 还把它们作为必需引用读取并加入 Bounds 校验，因此 Apply 会保留它们。它们不是 Prefab Child，也不是 Guardrail 的残留序列化副本。

### Gameplay 职责与清理

两个对象只承担旧版“Root 装饰地标”，不承担 Ground、Main Route、Advantage Route、Guardrail、Boundary、Slice End 或 Completion 职责。最终方案是完整删除两个 GameObject，因此对应 MeshFilter、MeshRenderer 与 BoxCollider 同时移除，不留下隐形 Collider。

- 003A Builder 不再调用或保留 `CreateRootBridgeLandmark`，未来 Rebuild 不会重新生成。
- 003C Authoring 使用两个精确常量迁移旧 Scene，只删除 `RootBridgeLandmark_LeftRoot` 与 `RootBridgeLandmark_RightRoot`；没有模糊匹配或批量名称清理。
- 003C Authoring 的幂等校验要求两个旧对象不存在，同时保持 Main Route `46.24m`、Advantage Route `27.00m`、约 `41.6%` 收益与现有连接方式不变。
- 正式 Scene 已同步移除这两个完整序列化对象及其 `Landmarks` 父级引用；Completion、Slice End Trigger、UI 与所有 Foundation 未改。

### 测试边界与当前结果

- 003A 测试不再把旧 Root Landmark 视为必需 Greybox 几何，并明确断言二者不存在。
- 003C EditMode 新增中央 Advantage 走廊审计：除 Span 自身外，X `[-7,7]`、Z `[27,43]` 范围内不得存在同时带 MeshFilter、MeshRenderer、BoxCollider 的未批准大型 Cube。
- 003C Scene/PlayMode 测试同时断言旧 Root Landmark 和旧 Guardrail 均不存在；原路线长度、连续性、几何无正体积重叠与 Main/Advantage 可用性标准保持不变。
- 静态检查已确认 Builder 不再创建旧对象、Scene 无旧对象或组件 fileID 残留、Authoring 只保留精确迁移常量，目标 C# 文件 `git diff --check` 通过。
- Unity `6000.3.21f1` 正式 Test Runner 已完成 Sprint003C/003D 专项 EditMode、专项 PlayMode、完整 EditMode 与完整 PlayMode 回归，结果均为全部 Passed、`0 Failed`、`0 Skipped`。仓库未保存本次正式 Test Runner XML，因此精确数量记录为 `TEST COUNT REQUIRES MANUAL RECORD`，不复用上一轮计数。

## 13. 最终结论

# SPRINT003C VISIBLE GEOMETRY CLEANUP — VERIFIED

两个可见长条已按真实 Scene 对象精确清理，Builder、003C Authoring、正式 Scene 与回归测试均已同步。重复 Apply 不恢复废弃对象，未发现隐形 Collider、Z-fighting、接地抖动或持续 GC 分配。Root Bridge Consequence 的 Snapshot/Revision/实例事件逻辑未改；003D Completion、Slice End Trigger 与 Completion UI 未改。该修复已由正式 Unity 验收关闭，并纳入 Sprint003D Gate Review。
