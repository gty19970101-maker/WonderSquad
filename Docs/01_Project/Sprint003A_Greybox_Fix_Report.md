# Sprint003A — Greybox Fix Report

## 状态

`PASSED`

本报告覆盖《沉睡森林》Sprint003A Greybox 的 Main Route 连通性、局部跌落恢复和几何拼接修复。未开始 Sprint003B。

## Main Route 修复

初版旋转长路段在转角处形成尖角、窄缝和不连续桥入口。随后采用统一 `-0.03m` Route 高度的尝试虽然降低了表面，却没有消除 Cube 体积相交，因此该策略已撤销。

当前路线使用顶部统一为 `Y=0` 的独立平台和边缘终止路段：

```text
Spawn
→ Entrance
→ Observation
→ MainRoute_ToBeaconA
→ Beacon A
→ MainRoute_BeaconAToNorthwestJunction
→ MainRouteJunction_Northwest
→ MainRoute_NorthwestJunctionToBeaconB
→ Beacon B
→ MainRoute_BeaconBToBridge
→ RootBridgeEntranceJunction
→ RootBridgeTemporaryCrossing
→ Slice End
```

- Main Route 宽度：`6m`。
- Beacon A/B Base：`8 × 1 × 8m`。
- Northwest 与 Root Bridge Entrance Junction：`8 × 1 × 8m`。
- Temporary Root Bridge：`7 × 1 × 16m`，Z 范围 `[27,43]`。
- Slice End Ground：Z 范围 `[43,57]`，与桥面只在端面相接。
- 不需要 Jump，不以 Advantage Route 作为通关前提。

## Recovery 与 Advantage 拼接

Recovery Route 使用三个独立承接平台：

```text
Observation
→ RecoveryRouteEntryJunction
→ RecoveryRoute_OuterLoop
→ RecoveryLeafPad
→ RecoveryRoute_ReturnNorth
→ RecoveryRouteReturnJunction
→ RecoveryRoute_ReturnLoop
→ Beacon B
```

各 Route 只到平台边缘，不在平台内部或转弯处互相穿插。

Advantage Route Reserved 调整到 Observation 西侧；Entry、Support、HighPlatform 与 VisibleBlocker 只允许端面接触。它仍是预留区域，不承担 003A 完成路线。

完整的逐对象原始 Bounds、20 组实际交叠以及改后 Position/Scale 记录见 `Sprint003A_ZFighting_Audit.md`。

## Fall Recovery 设计

`SleepingForestFallRecovery` 是场景局部、后续可删除的 Greybox 安全组件：

```text
PlayerSpawner.PlayerSpawned
→ 缓存本地 Player 与 CharacterController
→ LateUpdate 检查 Player Y
→ Y < -8
→ 临时禁用 CharacterController
→ 传送到显式 RecoveryPoint (0, 0.1, -46)
→ 恢复 CharacterController 原启用状态
```

- 不修改 PlayerMovement、GroundDetector 或 PlayerSpawner。
- 不在每帧查找 Player，不使用静态全局 Player。
- 不创建死亡、复活、Checkpoint、Health、Save 或 Network 框架。
- RecoveryPoint 位于安全地面并高于阈值，不会无限重复传送。

## Builder 同步

`SleepingForestGreyboxSceneBuilder` 已同步：

- Main/Recovery Route 的 Junction + 边缘终止结构。
- Spawn、Entrance、Observation、Bridge 与 End 的端面拼接。
- Beacon、Recovery、Advantage、Guardrail 与 Root Landmark 新尺寸。
- RecoveryPoint、Fall Recovery 组件和显式引用。
- 删除旧的统一 Route Y 偏移。

Builder 仍只提供明确菜单入口：

- `Create Missing Sleeping Forest Greybox`：Scene 存在时跳过。
- `Rebuild Sleeping Forest Greybox (Development Only)`：保存检查和覆盖确认通过后才执行。

没有自动创建入口，本轮没有运行 Builder，也没有手工修改 Scene YAML。

## 自动测试变化

EditMode：

- 更新新 Route、Junction、Beacon、Recovery 和 Bridge 结构断言。
- 验证所有可走表面顶部为 `Y=0`。
- 新增 27 个关键对象的 Renderer Bounds 与 BoxCollider Bounds 严格正体积重叠测试。
- 保留 Fall Recovery 配置与程序集隔离检查。

PlayMode：

- 保留正式 Scene 加载、单 Player Spawn、Camera 绑定、Fall Recovery 和恢复后移动检查。
- 测试通过 Editor PlayMode Scene API 从资产路径加载，不要求提前加入 Build Profile。

静态编译结果：

- `WonderSquad.Editor`：Exit Code `0`。
- `WonderSquad.Tests.EditMode`：Exit Code `0`。

最终 Unity Test Runner 结果：Sprint003A EditMode `8/8`、Sprint003A PlayMode `3/3`、完整 EditMode `82/82`、完整 PlayMode `50/50`，均为 `0 Failed / 0 Skipped`。

## 最终人工验收

- Spawn → Observation → Beacon → Root Bridge → Slice End 完整走通。
- Main Route 与 Advantage Route Reserved 均存在可物理抵达终点的路径；003A 未增加 Gameplay 阻挡。
- 跌落恢复、恢复后移动、接地和 Camera Follow 正常。
- 所有审计区域无可见闪烁。
- 路面/平台接缝无明显 CharacterController 卡顿或 GroundDetector 抖动。
- 单 Player、单 Main Camera，Console Error `0`。

Sprint003A Greybox 修复已通过最终验证，状态为 `PASSED`。
