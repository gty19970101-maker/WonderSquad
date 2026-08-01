# Item System

## 系统目标

管理资源、工具、机关部件和关键道具的静态定义与场景实例，让玩家可以拾取、携带、放置和使用物品，并保证关键物品在失败、离图或玩家离开后可恢复。

## 系统边界

负责：

- ItemDefinition 与 ItemInstance 分离。
- 场景实例生成、持有、放置、使用、消耗和回收。
- 物品标签、交互标签、物理配置和恢复策略。
- 玩家持有关系和放置槽关系。
- 关键道具恢复点。

不负责：

- 团队资源数量。
- 配方材料判定。
- 机关完成判定。
- 任意建造系统。
- 长期装备与收藏存档。

## 核心流程

### 生成

```text
主机读取 ItemDefinition
→ 分配 ItemInstanceId
→ 在合法生成点创建网络实例
→ 同步初始状态
```

### 拾取与放置

```text
玩家请求拾取
→ 主机校验距离、可用状态和持有槽
→ 设为 Held 并绑定 HolderId
→ 玩家请求放置
→ 主机校验插槽与标签
→ 设为 Placed 或 Available
```

### 恢复

```text
检测离图/持有者离开/非法状态
→ 发布 ItemRecoveryRequired
→ Level Recovery Orchestrator 创建恢复事务
→ 通过 IItemRecoveryPort 命令主机标记 Recovering
→ 解除旧关系
→ 移到配置恢复点
→ 返回 Available
```

关键物品不得由 Item 模块自行重新生成；所有恢复必须复用原 ItemInstanceId 或由同一恢复事务明确替换，避免复制。

## 主要状态

- `Available`
- `Held`
- `Placed`
- `InUse`
- `Consumed`
- `Recovering`
- `Destroyed`

关键物品通常不能直接进入 `Destroyed`，除非关卡目标已完成或有明确替代物。

### 《沉睡森林》物品表示矩阵

| 稳定 ID | 分类 | 运行时表示 | 是否关键 | 恢复规则 |
|---|---|---|---|---|
| `resource.wood` | Resource | 拾取后进入团队库存数量 | 否 | 不单独恢复 |
| `resource.stone` | Resource | 拾取后进入团队库存数量 | 否 | 不单独恢复 |
| `resource.crystal` | Resource | 拾取后进入团队库存数量 | 否 | 不单独恢复 |
| `resource.parts` | Resource | 拾取后进入团队库存数量 | 否 | 不单独恢复 |
| `tool.vine_cutter` | Tool | 主机生成的可携带场景实例 | 否 | 离图时回到工具恢复点 |
| `tool.bridge_kit` | Tool | 主机生成并放入预定义 Socket | 否 | 未消耗时回到工具恢复点 |
| `puzzle.portal_core` | PuzzlePart | 可携带场景实例 | 是 | 回到配置的关键物品恢复点 |
| `puzzle.portal_lens` | PuzzlePart | 可携带场景实例 | 是 | 回到配置的关键物品恢复点 |
| `puzzle.portal_stabilizer` | PuzzlePart | 可携带场景实例 | 是 | 回到配置的关键物品恢复点 |

同一实例在同一时刻只能采用一种表示。Resource 入库后销毁掉落实例；Tool 与 PuzzlePart 不进入抽象库存。

## 与其他系统的依赖

编译时依赖：

- `Core/Contracts`：ItemInstanceId、持有/放置/恢复命令和事件。
- `Content`：ItemDefinition、标签和 RecoveryPointId。

通过端口或事件协作：

- 使用 `IPlayerQuery` 获取持有点，不引用 Player 实现。
- Inventory 通过 `IResourceDepositPort` 接收资源结果。
- Crafting 通过 `IItemSpawnPort` 请求输出实例。
- Puzzle 通过 ItemTag 和 ItemInstanceId 消费放置结果。
- Level Recovery Orchestrator 通过 `IItemRecoveryPort` 执行恢复。
- Network Adapter 复制实例状态。

Item 不依赖 Player、Inventory、Crafting、Puzzle 或 Level 的实现程序集。

## 联机同步要求

- 主机生成和销毁场景物品。
- 主机决定拾取者、持有者、插槽和最终物理状态。
- 客户端可以预测持有表现，但接受主机校正。
- 同一实例只能被一名玩家持有或一个插槽占用。
- 关键物品恢复必须由主机触发。
- 同步必要位置、旋转、状态、HolderId 和 SocketId。
- 休眠静态物品不需要持续高频同步。

## MVP范围

- 资源掉落物。
- 可携带工具。
- 三个机关需要的部件。
- 拾取、携带、放下、插槽放置和使用。
- 关键道具恢复。
- 数据驱动物品标签与物理参数。
- 《沉睡森林》物品表示矩阵。

## 非MVP范围

- 装备系统。
- 武器和复杂战斗道具。
- 耐久、品质和随机属性。
- 任意自由建造。
- 玩家间交易。
- 长期收藏和云存档。
- 大规模网络物理物体。

## 验收标准

- 物品定义与场景实例有不同稳定标识。
- 两名玩家争用同一物品时只有一个成功。
- 持有、放置和插槽状态对所有客户端一致。
- 关键物品离图或持有者离开后可恢复。
- 恢复不会复制物品或重复推进机关。
- 能力和机关通过标签查询物品，不依赖 Prefab 名称。
- 静态物品在无变化时不产生不必要同步。
- 所有《沉睡森林》物品符合表示矩阵，不在库存数量和场景实例之间无规则切换。

## 风险与待确认事项

### 风险

- 网络物理抖动。
- 物品恢复与旧请求竞争产生复制。
- 过多动态物品影响性能和关卡可读性。
- 物理放置允许绕过预期机关条件。

### 待确认

- 玩家能否投掷物品；MVP 建议只做轻量放下或受控抛出。
- 大型物品的移动速度和是否需要多人搬运；MVP 关键部件默认单人可搬。
- 各工具与机关部件的预定义 Socket 清单；MVP 不允许自由建造放置。
- 可回收工具的资源返还比例。
