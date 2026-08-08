# Sprint003A — Sleeping Forest Greybox Review Checklist

## 最终状态

`PASSED / GO`

## 范围与架构

- [x] 仅修改 Sprint003A Greybox、局部恢复组件、Builder、专项测试和文档。
- [x] 未修改 Character Foundation 或 Interaction Foundation。
- [x] 未修改 PlayerMovement、GroundDetector、Camera Foundation 或 Input Actions。
- [x] 未实现 Beacon、Root Bridge、Puzzle、Ability 或 Network Gameplay。
- [x] Fall Recovery 位于独立、可删除的 Greybox 内容程序集。
- [x] Player 程序集不反向引用 Greybox 内容程序集。
- [x] 不使用静态 Player、Service Locator 或每帧对象搜索。

## Greybox 路线与几何

- [x] Spawn、Entrance、Observation 只在平台边缘连接。
- [x] Main Route 通过 Beacon/Junction 平台连续连接 Root Bridge 与 Slice End。
- [x] 路段终止于平台边缘，不使用长 Cube 穿插形成转角。
- [x] 所有可走表面顶部为 `Y=0`，不使用统一 Y 偏移掩盖重叠。
- [x] Renderer Bounds 与 BoxCollider Bounds 无已知正体积交叠。
- [x] 人工检查无 Z-fighting、明显卡顿或 GroundDetector 抖动。
- [x] Root Bridge 具有无需 Gameplay 的基础可走通方案。
- [x] Main Route 与 Advantage Route Reserved 均可物理抵达终点。
- [x] 003A 未人为封堵 Advantage Route，也未赋予两条路线玩法差异。

## Fall Recovery

- [x] RecoveryPoint 为显式场景引用。
- [x] Fall 阈值为显式序列化配置。
- [x] 通过 PlayerSpawner 事件绑定当前本地 Player。
- [x] 恢复时不修改移动计算。
- [x] 恢复后位置高于阈值，不会连续传送。
- [x] 恢复后 CharacterController、PlayerMovement、接地与 Camera 正常。
- [x] 无持续 GC 分配设计。

## Builder

- [x] Create Missing 不覆盖已有 Scene。
- [x] Rebuild 只在保存确认和覆盖确认后执行。
- [x] 无自动创建或自动强制重建入口。
- [x] Builder 已同步最终 Junction、Route、Recovery 与 Advantage 几何。
- [x] 未手工编辑 Scene YAML。

## 自动验证

- [x] Sprint003A EditMode：`8 Passed / 0 Failed / 0 Skipped`。
- [x] Sprint003A PlayMode：`3 Passed / 0 Failed / 0 Skipped`。
- [x] 完整 EditMode：`82 Passed / 0 Failed / 0 Skipped`。
- [x] 完整 PlayMode：`50 Passed / 0 Failed / 0 Skipped`。
- [x] 单 Player、单 Main Camera、Camera Binder 与 FallRecovery 自动验证通过。

## 人工验收

- [x] SleepingForest 成功进入 Play Mode。
- [x] Spawn → Observation → Beacon → Root Bridge → Slice End 可走通。
- [x] 跌出地图后恢复，恢复后继续移动与接地。
- [x] Camera Follow 正常。
- [x] 无可见 Z-fighting。
- [x] 无明显 CharacterController 卡顿或 GroundDetector 抖动。
- [x] Console Error `0`。

## Gate 结论

所有 Sprint003A 专项测试、完整回归和人工验收均已通过。Sprint003A 标记为 `PASSED`，Gate 结论为 `GO`。
