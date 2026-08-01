# Puzzle System

## 系统目标

提供可组合、可同步、可重置的环境机关框架，使玩家通过角色能力、资源、道具、地形和多人协作采用不同方案完成同一目标。

机关必须数据驱动，并为未来玩家关卡编辑器保留有限、安全的组件边界。

## 系统边界

负责：

- PuzzleDefinition 与运行时状态。
- Trigger、Condition、Action 的有限组合。
- 多条 SolutionPath。
- 机关输入、状态转换、进度、完成和局部重置。
- 角色优势、通用替代和恢复解法。
- 失败输出与 Status 系统连接。
- 机关配置合法性与调试状态。

不负责：

- 玩家输入采集。
- 物品、库存和能力的内部规则。
- 整个关卡完成与结算。
- 任意代码式关卡脚本。
- 编辑器 UI 和玩家内容发布。

## 核心流程

```text
加载 PuzzleDefinition
→ 注册触发器和初始状态
→ 接收标准化 TriggerEvent
→ 主机读取当前状态
→ 评估白名单 Condition
→ 选择满足的 SolutionPath
→ 执行白名单 Action
→ 更新 PuzzleSnapshot
→ 同步状态和反馈
→ 达到完成条件时通知 Level
```

失败流程：

```text
条件不满足或操作失败
→ 执行 FailureAction
→ 施加搞笑状态/开启支线/弹飞/局部重置
→ 保留可恢复路径
```

### 三层解法

每个核心机关至少定义：

1. 角色优势解法：更快、更省资源或更安全。
2. 通用替代解法：更多资源、步骤或协作。
3. 恢复解法：失败、丢失或异常后可继续。

## 主要状态

- `Inactive`
- `Available`
- `InProgress`
- `TemporarilyBlocked`
- `Solved`
- `Resetting`

状态必须有限、可观察、可序列化。不得用任意组件布尔值组合隐式表达关键状态。

### MVP 白名单触发器

- 交互
- 能力作用
- 物品放置
- 区域进入
- 计时结束
- 参与人数满足

### MVP 白名单动作

- 改变机关状态
- 生成或消耗物品
- 开启路线
- 施加或解除状态
- 激活检查点
- 完成目标
- 局部重置

## 与其他系统的依赖

编译时依赖：

- `Core/Contracts`：TriggerEvent、查询端口、动作端口、PuzzleSnapshot 和领域事件。
- `Content`：PuzzleDefinition、Trigger、Condition、Action 与 SolutionPath。

通过端口或事件协作：

- 消费 InteractionCommand、AbilityApplied、ItemPlaced 和 CraftSucceeded 等标准事件。
- 使用 InventoryQuery、ItemCommandPort 与 StatusCommandPort，不引用领域实现。
- 发布 PuzzleStateChanged、PuzzleSolved 和 PuzzleRecoveryRequired。
- Level 订阅完成与恢复事件；Puzzle 不依赖 Level。
- Communication 和 UI 只消费快照，不参与条件判定。
- Network Adapter 复制权威状态。

## 联机同步要求

- 机关状态只由主机转换。
- 客户端发送输入事件，不直接设状态。
- 需要迟加入者看到的状态使用快照或持续网络属性。
- 短暂特效使用事件，但不能作为唯一状态来源。
- 同步窗口使用网络时间。
- 每个输入包含来源、玩家、目标和 RequestId。
- 多种解法最终写入一个权威 `Solved` 状态，并记录 SolutionPathId。
- 局部重置增加 Revision，客户端忽略旧表现事件。

## MVP范围

- 三个《沉睡森林》机关：
  - 沉睡藤门
  - 回声石阵
  - 古代传送门
- 2 人基础解。
- 3～4 人并行或效率机会。
- 至少一个主要障碍有两种以上解法。
- 非语音可理解的状态与时间窗。
- 搞笑失败输出和局部重置。
- 有限 Trigger/Condition/Action。
- 数据校验和调试状态。

## 非MVP范围

- 通用可视化机关编辑器。
- 任意脚本和复杂逻辑图。
- 无限嵌套条件。
- 自动生成任意机关。
- 玩家发布和内容审核。
- 大量机关组件库。
- 战斗 Boss 机关。

## 验收标准

- 三个机关从数据定义加载并稳定完成。
- 机关状态对所有客户端一致。
- 两人不开麦能完成每个主线机关。
- 缺少任一特定角色仍有合理路径。
- 至少一个机关通过两种 SolutionPath 完成过。
- 失败不会永久卡关或重载整图。
- 配置缺失、重复 ID、无通用路径和非法动作能被校验器发现。
- 校验器只验证声明结构；物理可行性、多人时序和实际时长必须通过流程测试与人工游玩。
- Puzzle 不直接引用具体角色类名、UI 或关卡脚本。
- Puzzle 不直接依赖 Crafting、Status、Item、Inventory、Communication 或 Level 的实现程序集。

## 风险与待确认事项

### 风险

- 数据组件演变成难以调试的脚本语言。
- 多解法组合导致状态爆炸。
- 非语音同步时间窗过短。
- 局部重置与迟到网络事件冲突。
- 角色优势解法无意中成为唯一可行解。

### 待确认

- 三个机关的最终步骤与解法预算。
- 回声石阵的视觉节奏窗口。
- 沉睡藤门的通用替代资源数量。
- 古代传送门是否要求并行搬运。
- MVP 是否需要一个机关内变体，或只做资源/路线变体。
