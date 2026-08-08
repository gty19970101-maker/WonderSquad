# Sprint003A — Z-Fighting 精确几何审计

## 状态

`PASSED`

本次只修正《沉睡森林》Greybox Builder 的几何拼接，并补充 EditMode 结构测试。没有修改现有 Scene YAML、Character Foundation、Interaction Foundation、Player、GroundDetector、Camera、URP、Shader、Render Queue 或 Depth Bias，也没有开始 Sprint003B。

## 审计方法

- 审计对象来自当前 `SleepingForest.unity` 的旧生成结果以及对应 Builder 参数。
- 默认 Cube 的 `Renderer.bounds` 与 `BoxCollider.bounds` 都按世界空间 AABB 检查。
- 对旋转 Route Cube，AABB 只用于记录候选交叠范围；另外使用 XZ 平面的 OBB 分离轴检查，确认下表均为实际体积相交，而不只是 AABB 假阳性。
- 交叠判定要求 X、Y、Z 三轴交叠长度都大于 `0.0001m`。只在一个端面相接不视为体积交叠。
- 改后布局对 27 个路面、平台、支撑、护栏和桥根对象进行了同样的严格正体积检查，结果为 `0` 对。

## 根因

上一轮将长 Route Cube 统一降低到 `Y=-0.03`，只改变了顶部高度，没有消除 Cube 体积互相插入。旧布局仍存在三类结构问题：

1. 旋转长路段伸入 Observation、Beacon、Bridge 和 Recovery 平台内部。
2. 相邻长路段直接交叉，转弯或分叉没有独立 Junction Platform。
3. Advantage Support、桥根与护栏占用了其他 Cube 的体积范围。

因此本轮删除统一 Y 偏移策略。所有可走地面继续使用顶部 `Y=0`，通过“平台负责交汇、路段止于边缘”的结构性拼接消除重叠。

## 旧布局实际交叠清单

下表位置和 Scale 均为修复前值；旋转路段的 Scale 是本地 Scale，Bounds 交叠范围为世界空间 AABB。单位均为米。

| # | Object A：原 Position / Scale | Object B：原 Position / Scale | 原 Bounds 交叠范围 | 修改后的 Position / Scale 与连接方式 |
| --- | --- | --- | --- | --- |
| 1 | `ObservationAreaGround` `(0,-0.5,-15.5)` / `(26,1,15)` | `MainRoute_ToBeaconA` `(-7,-0.53,-5.5)` / `(7,1,19.105)`，Yaw `-47.121°` | X `[-13,2.38]`；Y `[-1,-0.03]`；Z `[-14.56,-8]` | Observation 不变；Route 改为 `(-10,-0.5,-6)` / `(6,1,4)`。Route 南端只在 Observation 北边 `Z=-8` 端面相接。 |
| 2 | `ObservationAreaGround` `(0,-0.5,-15.5)` / `(26,1,15)` | `RecoveryRoute_OuterLoop` `(14,-0.53,-7.5)` / `(6,1,9.434)`，Yaw `58.003°` | X `[8.41,13]`；Y `[-1,-0.03]`；Z `[-12.54,-8]` | 新增 `RecoveryRouteEntryJunction` `(9,-0.5,-4)` / `(8,1,8)`，只在 Observation 的 `Z=-8` 边界相接；OuterLoop 改为 `(16,-0.5,-4)` / `(6,1,6)`，连接 Junction 东边。 |
| 3 | `MainRoute_ToBeaconA` `(-7,-0.53,-5.5)` / `(7,1,19.105)` | `BeaconA_Reserved_Base` `(-14,-0.5,1)` / `(9,1,9)` | X `[-16.38,-9.5]`；Y `[-1,-0.03]`；Z `[-3.5,3.56]` | Beacon A 改为 `(-12,-0.5,0)` / `(8,1,8)`；Route 终止于其南边 `Z=-4`，不再伸入平台。 |
| 4 | `MainRoute_ToBeaconA` `(-7,-0.53,-5.5)` / `(7,1,19.105)` | `MainRoute_BeaconAToBeaconB` `(-0.5,-0.53,7)` / `(7,1,29.547)`，Yaw `66.038°` | X `[-15.42,2.38]`；Y `[-1.03,-0.03]`；Z `[-2.2,3.56]` | 删除长对角 `MainRoute_BeaconAToBeaconB`。Beacon A 平台承接南北两段；北段为 `(-12,-0.5,6)` / `(6,1,4)`。 |
| 5 | `MainRoute_ToBeaconA` `(-7,-0.53,-5.5)` / `(7,1,19.105)` | `AdvantageRouteSupport` `(-20,0.5,-2)` / `(10,2,30)` | X `[-16.38,-15]`；Y `[-0.5,-0.03]`；Z `[-14.56,3.56]` | Support 改为 `(-23,0.5,0)` / `(10,2,30)`；Main Route 世界 X 范围为 `[-13,-7]`，两者保留 5m 间距。 |
| 6 | `BeaconA_Reserved_Base` `(-14,-0.5,1)` / `(9,1,9)` | `MainRoute_BeaconAToBeaconB` `(-0.5,-0.53,7)` / `(7,1,29.547)` | X `[-15.42,-9.5]`；Y `[-1,-0.03]`；Z `[-2.2,5.5]` | Beacon A 改为 `(-12,-0.5,0)` / `(8,1,8)`；新的北段从平台北边 `Z=4` 开始。 |
| 7 | `BeaconA_Reserved_Base` `(-14,-0.5,1)` / `(9,1,9)` | `AdvantageRouteSupport` `(-20,0.5,-2)` / `(10,2,30)` | X `[-18.5,-15]`；Y `[-0.5,0]`；Z `[-3.5,5.5]` | Beacon A 改为 `(-12,-0.5,0)` / `(8,1,8)`，Support 改为 `(-23,0.5,0)` / `(10,2,30)`；X 方向保留 2m 间距。 |
| 8 | `MainRoute_BeaconAToBeaconB` `(-0.5,-0.53,7)` / `(7,1,29.547)` | `BeaconB_Reserved_Base` `(13,-0.5,13)` / `(9,1,9)` | X `[8.5,14.42]`；Y `[-1,-0.03]`；Z `[8.5,16.2]` | Beacon B 改为 `(0,-0.5,12)` / `(8,1,8)`；新增 `MainRouteJunction_Northwest` `(-12,-0.5,12)` / `(8,1,8)`，水平 Route `(-6,-0.5,12)` / 世界 Scale `(4,1,6)` 只接 Beacon B 西边 `X=-4`。 |
| 9 | `MainRoute_BeaconAToBeaconB` `(-0.5,-0.53,7)` / `(7,1,29.547)` | `MainRoute_BeaconBToBridge` `(6.5,-0.53,20)` / `(7,1,19.105)`，Yaw `-42.879°` | X `[-2.56,14.42]`；Y `[-1.03,-0.03]`；Z `[10.62,16.2]` | 旧对角段被 Junction + 直线段替换；Beacon B 平台分别承接西侧来路和北侧去桥 Route，两个 Route 不直接相交。 |
| 10 | `MainRoute_BeaconAToBeaconB` `(-0.5,-0.53,7)` / `(7,1,29.547)` | `RecoveryRoute_ReturnLoop` `(15.5,-0.53,4)` / `(6,1,18.682)`，Yaw `-15.524°` | X `[10.11,14.42]`；Y `[-1.03,-0.03]`；Z `[-2.2,13.8]` | 两条斜线均移除。Main Route 从西边接 Beacon B；Recovery Return 改为 `(11.5,-0.5,12)` / 世界 Scale `(15,1,6)`，只接 Beacon B 东边 `X=4`。 |
| 11 | `MainRoute_BeaconAToBeaconB` `(-0.5,-0.53,7)` / `(7,1,29.547)` | `AdvantageRouteSupport` `(-20,0.5,-2)` / `(10,2,30)` | X `[-15.42,-15]`；Y `[-0.5,-0.03]`；Z `[-2.2,13]` | 旧长对角段移除；新主路限制在 Beacon/Junction 之间，Support 移至 X `[-28,-18]`，不再相交。 |
| 12 | `BeaconB_Reserved_Base` `(13,-0.5,13)` / `(9,1,9)` | `MainRoute_BeaconBToBridge` `(6.5,-0.53,20)` / `(7,1,19.105)` | X `[8.5,15.56]`；Y `[-1,-0.03]`；Z `[10.62,17.5]` | Beacon B 改为 `(0,-0.5,12)` / `(8,1,8)`；去桥 Route 改为 `(0,-0.5,17.5)` / `(6,1,3)`，从平台北边 `Z=16` 开始。 |
| 13 | `BeaconB_Reserved_Base` `(13,-0.5,13)` / `(9,1,9)` | `RecoveryRoute_ReturnLoop` `(15.5,-0.53,4)` / `(6,1,18.682)` | X `[10.11,17.5]`；Y `[-1,-0.03]`；Z `[8.5,13.8]` | Beacon B 改为 `(0,-0.5,12)` / `(8,1,8)`；Recovery Return 改为 `(11.5,-0.5,12)` / 世界 Scale `(15,1,6)`，终止在平台东边 `X=4`。 |
| 14 | `MainRoute_BeaconBToBridge` `(6.5,-0.53,20)` / `(7,1,19.105)` | `RootBridgeTemporaryCrossing` `(0,-0.5,34.5)` / `(7,1,17)` | X `[-2.56,3.5]`；Y `[-1,-0.03]`；Z `[26,29.38]` | 去桥 Route 终止于 `Z=19`；新增 `RootBridgeEntranceJunction` `(0,-0.5,23)` / `(8,1,8)`，其北边 `Z=27` 与改后桥面 `(0,-0.5,35)` / `(7,1,16)` 相接。 |
| 15 | `MainRoute_BeaconBToBridge` `(6.5,-0.53,20)` / `(7,1,19.105)` | `RecoveryRoute_ReturnLoop` `(15.5,-0.53,4)` / `(6,1,18.682)` | X `[10.11,15.56]`；Y `[-1.03,-0.03]`；Z `[10.62,13.8]` | 两条斜线移除；新去桥 Route 位于 X `[-3,3]`、Z `[16,19]`，新 Recovery Return 位于 X `[4,19]`、Z `[9,15]`。 |
| 16 | `RecoveryRoute_OuterLoop` `(14,-0.53,-7.5)` / `(6,1,9.434)` | `RecoveryLeafPad` `(18,-0.5,-5)` / `(9,1,9)` | X `[13.5,19.59]`；Y `[-1,-0.03]`；Z `[-9.5,-2.46]` | LeafPad 改为 `(23,-0.5,-4)` / `(8,1,8)`；OuterLoop 改为 `(16,-0.5,-4)` / `(6,1,6)`，终止于 LeafPad 西边 `X=19`。 |
| 17 | `RecoveryRoute_OuterLoop` `(14,-0.53,-7.5)` / `(6,1,9.434)` | `RecoveryRoute_ReturnLoop` `(15.5,-0.53,4)` / `(6,1,18.682)` | X `[10.11,19.59]`；Y `[-1.03,-0.03]`；Z `[-5.8,-2.46]` | 两段不再直接交叉；`RecoveryLeafPad` 负责西侧进入，新的 `RecoveryRoute_ReturnNorth` `(23,-0.5,4)` / `(6,1,8)` 从其北边离开。 |
| 18 | `RecoveryLeafPad` `(18,-0.5,-5)` / `(9,1,9)` | `RecoveryRoute_ReturnLoop` `(15.5,-0.53,4)` / `(6,1,18.682)` | X `[13.5,20.89]`；Y `[-1,-0.03]`；Z `[-5.8,-0.5]` | LeafPad 改为 `(23,-0.5,-4)` / `(8,1,8)`；ReturnNorth 从 `Z=0` 开始，只在 LeafPad 北边端面相接。 |
| 19 | `Boundary_RootBridgeWestGuardrail` `(-3.75,0.75,34.5)` / `(0.5,1.5,17)` | `RootBridgeLandmark_LeftRoot` `(-4.75,1.5,34.5)` / `(2,3,12)` | X `[-4,-3.75]`；Y `[0,1.5]`；Z `[28.5,40.5]` | Guardrail 改为 `(-3.75,0.75,35)` / `(0.5,1.5,16)`；LeftRoot 改为 `(-5,1.5,35)` / `(2,3,12)`。两者只在 `X=-4` 端面相接。 |
| 20 | `Boundary_RootBridgeEastGuardrail` `(3.75,0.75,34.5)` / `(0.5,1.5,17)` | `RootBridgeLandmark_RightRoot` `(4.75,1.5,34.5)` / `(2,3,12)` | X `[3.75,4]`；Y `[0,1.5]`；Z `[28.5,40.5]` | Guardrail 改为 `(3.75,0.75,35)` / `(0.5,1.5,16)`；RightRoot 改为 `(5,1.5,35)` / `(2,3,12)`。两者只在 `X=4` 端面相接。 |

## 改后拼接规则

### 高度

- 所有可走路面、平台和临时桥：中心 `Y=-0.5`、厚度 `1`、顶部 `Y=0`。
- 不再使用 `-0.03m` 统一 Route 偏移。
- Advantage Support 顶部与 HighPlatform 底部只在 `Y=1.5` 端面相接。
- 垂直 Blocker、桥根和护栏只允许端面接触相邻结构，不允许正体积插入。

### Main Route

```text
Observation north edge
→ MainRoute_ToBeaconA
→ Beacon A platform
→ MainRoute_BeaconAToNorthwestJunction
→ MainRouteJunction_Northwest
→ MainRoute_NorthwestJunctionToBeaconB
→ Beacon B platform
→ MainRoute_BeaconBToBridge
→ RootBridgeEntranceJunction
→ RootBridgeTemporaryCrossing
→ SliceEndGround
```

每条 Route 只连接两个边缘。Beacon A、Beacon B、Northwest Junction 和 Root Bridge Entrance Junction 是唯一交汇表面。

### Recovery Route

```text
Observation north edge
→ RecoveryRouteEntryJunction
→ RecoveryRoute_OuterLoop
→ RecoveryLeafPad
→ RecoveryRoute_ReturnNorth
→ RecoveryRouteReturnJunction
→ RecoveryRoute_ReturnLoop
→ Beacon B east edge
```

Recovery 的转弯由三个独立平台承担，三条直线路段不再互相穿插。

### Advantage Route Reserved

- `AdvantageRouteEntryPlatform`：`(-15.5,-0.5,-11)` / `(5,1,6)`，只接 Observation 西边。
- `AdvantageRouteSupport`：`(-23,0.5,0)` / `(10,2,30)`。
- `AdvantageRouteHighPlatform`：`(-23,2,0)` / `(8,1,28)`，只与 Support 在底面接触。
- `AdvantageRouteVisibleBlocker`：`(-17.75,1.5,-11)` / `(0.5,3,6)`，只与 EntryPlatform 顶面及 Support 东侧端面接触。

## Renderer 与 Collider 复核结果

Builder 改后，对以下 27 个对象的 Renderer Bounds 和 BoxCollider Bounds 做了两两严格正体积检查：

- Spawn、Entrance、Observation
- 4 条 Main Route、2 个 Main Junction、2 个 Beacon Base、Root Bridge、Slice End
- 3 条 Recovery Route、3 个 Recovery Junction/Pad
- Advantage Entry、Support、HighPlatform、VisibleBlocker
- 2 个 Root Bridge Guardrail、2 个 Root Landmark

静态参数审计结果：

```text
STRICT_POSITIVE_VOLUME_OVERLAPS=0
```

另外新增 EditMode 测试 `SleepingForestScene_AuditedGeometryHasNoPositiveVolumeOverlap`。该测试在重建后的正式 Scene 中同时检查 Renderer 和 BoxCollider，不会把合法的端面相接误报为体积重叠。

## Builder 与测试修改

- `Client/Assets/WonderSquad/Editor/SleepingForestGreyboxSceneBuilder.cs`
  - 删除统一 Route Y 偏移。
  - Main Route 与 Recovery Route 改为 Junction + 边缘终止的轴向路段。
  - 新增 Main/Recovery `JunctionPlatforms` 层级。
  - 调整 Beacon、Root Bridge、Advantage Support、Blocker、Guardrail 和 Root Landmark 几何。
- `Client/Assets/WonderSquad/Tests/EditMode/SleepingForestGreyboxEditModeTests.cs`
  - 更新新对象名称、尺寸和顶部高度断言。
  - 新增 27 对象 Renderer/Collider 正体积重叠回归检查。

使用 Unity 6000.3.21f1 当前 Bee 响应文件执行静态编译：

- `WonderSquad.Editor`：通过，Exit Code `0`。
- `WonderSquad.Tests.EditMode`：通过，Exit Code `0`。

最终 Unity Test Runner 已验证：Sprint003A EditMode `8/8`、完整 EditMode `82/82`、Sprint003A PlayMode `3/3`、完整 PlayMode `50/50`，均为 `0 Failed / 0 Skipped`。其中 Bounds 专项测试已在重建后的正式 Scene 通过。

## 需要重新人工检查的区域

显式执行受确认保护的 Sprint003A Rebuild 后，按以下顺序检查：

1. Spawn → Entrance → Observation 三个平台接边。
2. Observation 北边到 Beacon A 南边。
3. Beacon A 北边、Northwest Junction 两侧、Beacon B 西边。
4. Beacon B 北边、Root Bridge Entrance Junction、桥面和 Slice End。
5. Observation 与 Recovery Entry 的接入口。
6. Recovery Entry → LeafPad → Return Junction → Beacon B 东边。
7. Observation 西边、Advantage Entry、Visible Blocker、Support 与 HighPlatform 接缝。
8. Root Bridge 两侧 Guardrail 与 Root Landmark 接缝。

最终人工复查已覆盖上述区域：移动 Camera 时无颜色闪烁，CharacterController 无明显卡顿，GroundDetector 无抖动。Console Error 为 `0`，本审计状态为 `PASSED`。
