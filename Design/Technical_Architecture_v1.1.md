# Wonder Squad MVP 技术架构 v1.1

## 1. 文档目的

本文档基于：

- 《Wonder Squad 游戏开发总设计文档 v1.1》
- 《Wonder Squad MVP 开发计划 v1.1》
- 《Wonder Squad v1.1 设计变更影响分析》
- 原《Wonder Squad MVP 技术架构》

本文档定义 MVP 的技术选型、模块边界、依赖方向、网络权威、数据归属、数据驱动内容模型及关键接口草案，不包含完整业务实现代码。

正式术语与依赖定义以 `Docs/01_Project/Project_Glossary.md` 为准。

核心目标：

> 以最小但可扩展的技术架构，支持 2～4 人在《沉睡森林》中使用角色能力、非语音或语音交流、环境解谜、不同解法、欢乐失败与队友救援完成一局 10～20 分钟的合作冒险。

---

## 2. v1.1 架构原则

- 主机集中权威：客户端提交意图，主机决定共享结果。
- 非语音交流优先：语音是可选便利能力，不是关卡依赖。
- 能力而非角色类名：关卡通过能力标签表达机会，不硬编码必选角色。
- 多条解法汇聚：不同能力、资源和路线最终写入统一谜题目标状态。
- 失败是持续状态：普通失败使用临时状态、救援和局部恢复，不重载整关。
- 一关一场景边界：不建设开放世界流式加载。
- 内容数据驱动：静态定义、权威运行时状态和客户端表现分离。
- 为未来编辑器预留数据边界，不提前制作编辑器平台。
- 有限类型优于任意脚本：触发器、条件和动作采用白名单类型。
- 小团队优先：只抽象《沉睡森林》实际使用的能力。
- 预定义能力机会：绳索锚点、结构插槽、环境状态和揭示点均由关卡数据定义，不做自由网络物理建造。
- 复合恢复唯一编排：Level Recovery Orchestrator 负责跨 Player、Item、Puzzle 和 Status 的恢复顺序。

---

## 3. 技术选型摘要

| 项目 | v1.1 MVP 建议 |
|---|---|
| Unity | Unity `6000.3.21f1`（Unity 6.3 LTS），项目唯一正式版本基线 |
| 项目模板 | Universal 3D |
| 画面形态 | 3D 场景与物理、受控摄像机的 2.5D 卡通表现 |
| 渲染管线 | URP |
| 输入 | Unity Input System |
| 联机 SDK | Photon Fusion 2 |
| 网络拓扑 | Host Mode |
| 状态权威 | 主机集中权威 |
| 玩家数量 | 2～4 人 |
| 关卡结构 | 一关一张独立小地图 |
| 首关 | 《沉睡森林》 |
| 语音 | 第一阶段不集成 SDK，只保留适配接口 |
| 非语音交流 | MVP 必做：标记、快捷意图、动作与求救 |
| 内容定义 | ScriptableObject 或等价版本化数据资产 |
| 自建后端 | MVP 不需要 |
| 独立服务器 | MVP 不需要 |
| 本地保存 | 设置、隐私、输入、辅助功能和少量教学标记 |
| 玩家编辑器 | MVP 不实现 |
| MVP 角色 | 探险家、工匠、魔法师、观察者；各一个标志性能力，允许重复选择 |
| 关卡变体 | 只保留数据边界，MVP 使用固定默认变体 |

版本管理规则：

- Unity Editor 精确锁定为 `6000.3.21f1`；`ProjectVersion.txt`、治理文档与验证报告必须保持一致。
- Fusion 和其他关键包写入清单并锁定。
- MVP 期间不主动升级大版本。
- 补丁升级先在独立分支完成构建、联机和完整关卡回归。
- 语音 SDK 如果接入，也必须通过适配层隔离和锁定版本。

---

## 4. Unity 模板与 2.5D 实现

继续使用 Universal 3D 模板：

- 角色、地形、道具和机关使用 3D 对象。
- 移动、跳跃、搬运和环境机关使用 3D 物理。
- 摄像机采用受控俯视斜角、等距感或有限旋转。
- UI、图标、标记和部分特效使用 2D 资源。
- 不混用 2D 与 3D 物理系统。

v1.1 新增表现要求：

- 角色能力具备稳定的图形、颜色、动作和音效语言。
- 场景标记在 3D 世界与必要的小地图层同时表现。
- 临时状态与求救必须对队友可读。
- 重要信息不能只依赖颜色、声音或语音。

---

## 5. 联机与语音选型

## 5.1 联机结论

继续采用：

- Photon Fusion 2
- Host Mode
- 一名玩家作为主机兼本地玩家
- 其他玩家作为客户端
- 主机持有共享世界 State Authority

选择理由保持不变：

- 适合 2～4 人短局实时合作。
- 适合移动、搬运道具、同步机关和状态。
- Host Mode 提供集中权威和输入预测模型。
- 不需要维护独立服务器。

### 5.2 Fusion 与 Unity Netcode 取舍

| 对比项 | Photon Fusion 2 | Unity Netcode for GameObjects |
|---|---|---|
| 主机权威 | Host Mode 直接适配 | 可实现 Host |
| 实时移动 | 预测、回滚与重模拟模型清晰 | 基础同步可用，复杂预测通常需更多工作 |
| 房间连接 | Photon Cloud 房间能力直接可用 | 常需组合 Unity Sessions/Lobby/Relay |
| 可搬运道具与机关 | 集中状态权威适合本项目 | 可实现，但需要团队建立更多约束 |
| SDK 依赖 | 第三方云与 SDK 绑定较高 | Unity 生态绑定更自然 |
| 小团队 MVP 速度 | 团队无既有框架时优先 | 团队已有 NGO 经验时合理 |

v1.1 的交流、能力、状态和数据驱动需求不改变网络方案结论。

### 5.3 语音方案

第一阶段 MVP 不集成语音 SDK：

- `Communication` 完整实现游戏内交流。
- 只定义 `IVoiceChatService` 适配接口，不安装具体包。
- UI 不显示不可用的语音按钮。
- Save 不保存参与者静音列表。
- 关卡验收以全队不开麦为准。

MVP 验证通过后，语音仍须通过独立适配器接入，且不得进入谜题条件、状态存档或 Fusion 玩法数据。

---

## 6. 总体分层

```text
Presentation
UI · Camera · Animation · Audio · Marker Views
                         ↓
Gameplay Domains
Player · Ability · Interaction · Communication · Inventory
Item · Crafting · Puzzle · Status/Rescue
                         ↓
Orchestration and Content
Level · Trigger Runtime · Content Definitions · Validation
                         ↓
Infrastructure
Network/Fusion · Voice Adapter · Save · Unity Runtime
```

依赖规则：

- 表现层只读取状态并提交意图。
- 玩法域不依赖具体 UI、场景对象名称或 Photon 类型。
- Content 保存静态定义，不持有会话运行时状态。
- Network 复制意图和权威结果，不复制另一套玩法规则。
- Save 只保存允许的本地数据。
- Voice Adapter 不被 Puzzle 或 Level 依赖。
- Level 编排目标，不实现具体能力、制作和机关规则。

---

## 7. 客户端模块

## 7.1 Player

### 职责

- 读取本地移动、跳跃、交互、能力、标记和救援输入。
- 驱动角色移动预测和本地表现。
- 保存 PlayerId、RoleId、交互点、持有点和摄像机目标。
- 应用权威临时状态产生的移动修饰。
- 发布角色动作状态。

### 不负责

- 不决定能力效果、资源结果、救援成功或谜题完成。
- 不直接修改关卡状态。

### 依赖

- 编译时只依赖 Core/Contracts。
- 通过命令、查询和事件与 Ability、Interaction、Communication、Status、Level 和 Network Adapter 协作，不引用其实现。

---

## 7.2 Ability

### 职责

- 定义角色与能力。
- 管理能力输入、冷却或持续时间等轻量规则。
- 提供能力标签查询。
- 向环境对象提交能力使用意图。
- 在主机上校验角色、距离、目标、当前状态和使用条件。
- 输出标准化环境效果或触发输入。

### 设计约束

- 关卡通过 `AbilityTag` 判断能力，不直接判断 `Explorer` 等具体类名。
- 能力不得形成唯一通关条件。
- 不建设复杂技能树、伤害、等级和装备加成。
- 只实现《沉睡森林》实际使用的能力类型。
- 探险家只使用预定义 Anchor，工匠只使用预定义 Socket，魔法师只切换有限 EnvironmentState，观察者只揭示预定义 RevealTarget。
- 自由绳索、任意建造、连续环境变形与复杂网络物理属于非 MVP。

### 依赖

- 编译时依赖 Core/Contracts 与 Content。
- 通过 IPlayerQuery、IAbilityTargetPort、AbilityCommand 和 AbilityApplied 协作。
- Puzzle、Item、Crafting、Level 和 Network Adapter 只引用公开契约，不形成反向实现依赖。

---

## 7.3 Interaction

### 职责

- 发现附近可交互对象。
- 计算距离、方向、遮挡和本地提示。
- 统一表达使用、采集、拾取、放置、制作、激活与救援意图。
- 支持即时、持续、同步窗口和多人槽位交互。

### 不负责

- 不执行具体玩法结果。
- 不把本地可用提示当作权威成功。

### 依赖

- 编译时只依赖 Core/Contracts 中的 IInteractable、InteractionContext 与目标端口。
- Player、Ability、UI 和 Network Adapter 通过公开契约协作。

---

## 7.4 Communication

### 职责

- 管理场景标记、快捷意图、动作和求救表达。
- 管理标记类型、目标、世界位置、发送者、生命周期和优先级。
- 对标记数量、频率、位置与权限做主机校验。
- 向 UI 和世界表现提供统一事件。
- 提供本地隐藏、静音和过滤。
- 为 MVP 后续的语音适配器保留接口，不在第一阶段创建具体会话。

### 设计约束

- 快捷消息使用稳定键值，本地解析文字和图标。
- 标记同步的是语义，不同步本地化后的字符串。
- 角色庆祝等纯表现动作可以用短暂事件。
- 求救等影响玩法的信息需要持续权威状态或可重建快照。
- 语音内容不能成为任何关卡条件。

### 依赖

- 编译时只依赖 Core/Contracts。
- 通过 IPlayerQuery、MarkerCommand、HelpRequested、SavePort 和只读 ViewModel 协作。
- 语音适配器只保留接口，第一阶段无具体实现依赖。

---

## 7.5 Inventory

### 职责

- 表示团队共享资源和必要的轻量玩家持有记录。
- 提供查询、堆叠、增加、预留和原子消耗。
- 发布变更供 UI 使用。

### v1.1 调整

- 支持 Ability 产生的材料成本修饰。
- 修饰结果必须由主机计算。
- 角色能力不能直接绕过库存权威。

### 依赖

- 编译时依赖 Core/Contracts 与 Content。
- 通过 CostModifier、InventoryQuery、InventoryCommand 和 InventoryChanged 协作。
- 不依赖 Item、Ability、Crafting、Puzzle、UI 或 Network 实现。

---

## 7.6 Item

### 职责

- 定义资源、工具、机关部件和关键物品。
- 管理世界实例、持有、放置、使用、消耗和恢复。
- 管理关键道具离图、持有者离开和异常销毁。
- 向机关提交标准物品标签和实例 ID。

### v1.1 调整

- 支持能力可交互标签。
- 支持数据定义的恢复点。
- 支持通用替代工具。

### 依赖

- 编译时依赖 Core/Contracts 与 Content。
- 通过 IPlayerQuery、ItemCommand、ItemStateChanged、ItemSpawnPort 和 ItemRecoveryPort 协作。
- 不依赖 Player、Ability、Inventory、Crafting、Puzzle、Level 或 Network 实现。

---

## 7.7 Crafting

### 职责

- 管理配方、材料需求、工作点和输出。
- 在主机上校验并原子消耗。
- 生成权威制作结果。
- 支持团队共同提交材料。

### v1.1 调整

- 支持基于 AbilityTag 的有限配方修饰。
- 为无对应角色提供通用配方。
- 记录采用的配方或解法标识。

### 依赖

- 编译时依赖 Core/Contracts 与 Content。
- 通过 InventoryPort、ItemSpawnPort、CostModifier、CraftCommand 和 CraftResult 协作。
- 不依赖 Inventory、Item、Ability、UI 或 Network 实现。

---

## 7.8 Puzzle

### 职责

- 运行数据定义的机关状态机。
- 接受交互、能力、物品、区域、计时和人数条件输入。
- 管理多条 SolutionPath。
- 将不同解法汇聚到统一目标状态。
- 产生完成、失败、状态施加、支线路径开启或局部重置动作。
- 提供状态调试和快照。

### v1.1 调整

- 不硬编码固定角色。
- 不硬编码必须四人。
- 支持 2 人基础解法和 3～4 人并行机会。
- 同步窗口必须允许非语音视觉协调。
- 失败输出可调用 Status/Rescue。
- 配置由 Content Validation 校验。

### 依赖

- 编译时依赖 Core/Contracts 与 Content。
- 消费标准领域事件，通过 InventoryQuery、ItemPort 和 StatusPort 执行动作。
- 向 Level 发布 PuzzleSolved 与 PuzzleRecoveryRequired。
- 不依赖 Interaction、Ability、Item、Inventory、Status、Level 或 Network 实现。

---

## 7.9 Status/Rescue

### 职责

- 管理临时状态的施加、持续、修饰、解除和过期。
- 管理求救、救援目标、救援进度和结果。
- 支持被弹飞、困住、黏滑、笨拙等有限状态。
- 提供超时、检查点或全队脱困兜底。
- 只发布单人状态和“疑似全队受困”事件，不直接执行跨系统恢复。
- 发布状态供 Player、Communication、UI 和动画读取。

### 设计约束

- 状态持续时间短且可理解。
- 不允许表现代码直接冻结权威输入。
- 救援由主机校验距离、目标状态和参与者。
- 缺少特定能力时存在通用救援。
- 普通状态不写入长期存档。

### 依赖

- 编译时依赖 Core/Contracts 与 Content。
- 通过 IPlayerQuery、RescueCommand、StatusCommand 和能力标签输入协作。
- 发布 StatusChanged、HelpRequested 与 PartyIncapacitated。
- 不依赖 Player、Interaction、Ability、Level、Communication、UI 或 Network 实现。

---

## 7.10 Level

### 职责

- 管理独立关卡生命周期。
- 加载并实例化 LevelDefinition。
- 管理进入、等待、进行、完成、结算和返回关卡选择。
- 管理出生点、检查点、恢复点和全队脱困。
- 通过 Level Recovery Orchestrator 统一编排复合恢复事务。
- 汇总谜题完成状态。
- 加载并记录固定默认关卡变体；随机选择延期到 MVP 之后。
- 记录人数、阵容、种子、解法、失败、救援和耗时。

### 不负责

- 不实现具体能力、配方、道具或机关规则。
- 不加载连续开放世界分区。

### 依赖

- 编译时只依赖 Core/Contracts 与 Content/Validation。
- 通过只读接口获取 Player/Ability 状态。
- 订阅 Puzzle、Item、Status 的公开事件。
- 通过恢复、物品、状态和玩家端口发送命令，不引用这些模块的实现程序集。

### Recovery Orchestrator

复合恢复使用唯一 `RecoveryTransactionId`，顺序固定为：

1. 暂停相关目标的新交互。
2. 取消持续交互、救援和持有关系。
3. 恢复或重新放置关键物品。
4. 局部重置相关机关并增加 Revision。
5. 清除需要解除的临时状态。
6. 将玩家移动到权威检查点。
7. 恢复目标交互并发布事务完成事件。

Player、Item、Puzzle 与 Status 只能报告异常或执行各自的单域命令，不能自行启动跨系统恢复。

---

## 7.11 Content/Validation

### 职责

- 加载角色、能力、物品、配方、机关、关卡和变体静态定义。
- 管理内容格式版本和稳定 ID。
- 校验缺失引用、重复 ID、非法组合和不受支持类型。
- 结构校验主线是否声明至少一条通用替代解法。
- 校验变体候选和依赖。
- 为未来玩家编辑器提供可序列化边界。
- 对四角色可重复选择的 2～4 人共 65 个无序组合检查是否声明主线通用路径；真实游玩只覆盖代表性最不利矩阵。

### 设计约束

- MVP 只支持白名单类型。
- 不执行任意脚本或反射调用任意方法。
- 不实现在线发布、审核和下载。
- 静态定义不可被客户端在会话中当作权威状态修改。
- 校验器只证明 ID、引用、白名单、资源预算和声明路径等结构规则，不证明物理或时序上必然可通关；实际可通关性由自动化流程测试和人工游玩验证。

### 依赖

- 编译时依赖 Unity 资产加载能力与 Core/Contracts。
- Ability、Item、Crafting、Puzzle 和 Level 只读消费定义，不构成 Content 的反向依赖。

---

## 7.12 Network

### 职责

- 封装 Photon Fusion。
- 管理房间、玩家、角色选择和场景会话。
- 收集并传递玩家输入。
- 同步玩家、能力、标记、资源、道具、制作、机关、状态、救援和关卡。
- 同步固定默认变体；为后续主机种子选择保留字段。
- 校验请求、处理幂等、迟到请求和玩家离开。
- 提供网络时间、Tick、角色和对象映射。

### 不负责

- 不复制玩法规则。
- 不传输语音音频。
- 不保存长期账号数据。

### 依赖

- 编译时依赖 Photon Fusion SDK、Core/Contracts、Content 稳定 ID 和各领域公开网络契约。
- 不依赖 Gameplay 领域的内部实现类型。

---

## 7.13 UI

### 职责

- 房间、角色选择、加载和错误反馈。
- 交互提示、团队库存、配方、目标和结算。
- 能力提示、冷却或可用状态。
- 动作轮盘、场景标记和求救状态。
- 轻量小地图：使用 LevelDefinition 提供的固定边界，将权威世界标记映射到小地图坐标。
- 临时状态和救援反馈。
- 语音开关、静音和设备状态（若接入）。
- 设置、暂停、退出和无障碍入口。

### 设计约束

- 不直接修改玩法状态。
- 不把谜题答案完整显示给玩家。
- 重要信息使用图形、文字、形状和声音组合。
- 支持键鼠和手柄。
- 小地图只显示固定关卡轮廓、队友、目标和有效标记，不实现战争迷雾、导航网格或动态地图生成。

### 依赖

- 编译时依赖 Core/Contracts、各模块只读 ViewModel、命令接口、SavePort 与 Localization。
- Gameplay 模块不引用 UI。

---

## 7.14 Save

### 职责

- 保存画面、声音、输入和辅助功能。
- 为未来语音保存全局音量与设备设置预留字段；第一阶段不保存具体参与者静音。
- 保存标记过滤、隐私和动作轮盘布局。
- 保存是否看过基础教学。
- 管理格式版本和损坏恢复。

### 不保存

- 语音内容
- 局内角色权威状态
- 团队资源
- 道具实例
- 机关状态
- 临时状态
- 救援进度
- 关卡变体和完成状态

### 依赖

- 编译时依赖 Core/Contracts 与本地文件系统。
- UI、Communication 和 Player 输入设置通过 SavePort 使用，不构成 Save 的反向依赖。

---

## 8. 模块依赖总览

下表只表示**编译时依赖**。事件消费者和运行时协作不计入反向依赖。

| 模块 | 允许的编译时依赖 | 通过端口或事件协作 |
|---|---|---|
| Core/Contracts | 无 | 定义 ID、命令、查询和事件 |
| Content/Validation | Core/Contracts、Unity 资产 | 向各领域提供只读定义 |
| Player | Core/Contracts | 发布 PlayerState；消费 MovementModifier |
| Ability | Core/Contracts、Content | 接收 AbilityCommand；发布 AbilityApplied |
| Interaction | Core/Contracts | 发布 InteractionCommand |
| Communication | Core/Contracts | 接收 MarkerCommand；消费 HelpRequested |
| Inventory | Core/Contracts、Content | 接收 InventoryCommand；发布 InventoryChanged |
| Item | Core/Contracts、Content | 接收 ItemCommand；发布 ItemStateChanged |
| Crafting | Core/Contracts、Content | 通过 InventoryPort 与 ItemSpawnPort 执行事务 |
| Puzzle | Core/Contracts、Content | 消费标准输入；通过 StatusPort 和 ItemPort 执行动作 |
| Status/Rescue | Core/Contracts、Content | 发布 StatusChanged、HelpRequested、PartyIncapacitated |
| Level | Core/Contracts、Content | 订阅领域事件并编排关卡与恢复事务 |
| Network/Fusion | Core/Contracts、各领域公开网络契约、Fusion | 将网络输入映射为命令并复制快照 |
| Save | Core/Contracts、本地文件 | 提供本地设置端口 |
| UI | Core/Contracts、各领域只读 ViewModel、Save 端口 | 提交命令，不被玩法模块引用 |

防循环规则：

- Puzzle 不直接引用具体角色实现，只查询能力标签。
- Ability 不直接完成 Puzzle，只提交标准输入。
- Status 不依赖 Level，只发布状态、求救或全队受困事件。
- Level 不写 Puzzle 内部字段。
- Communication 不改变 Puzzle 条件。
- Content 不引用运行时网络对象。
- UI 永远不是玩法模块的依赖。
- Fusion 类型只出现在 Network/Fusion 和必要的网络表现适配器中。
- 领域之间若需要协作，只引用 Core/Contracts 中的端口或事件，不引用对方实现程序集。

---

## 9. 网络权威

## 9.1 必须由主机决定

### 会话与关卡

- 是否允许加入。
- 玩家标识、角色选择确认和生成点。
- 关卡开始、阶段、完成、结算和返回。
- 固定默认关卡变体；随机种子字段保留但第一阶段不选择随机内容。
- 检查点和全队脱困。

### 玩家与能力

- 玩家位置、速度和恢复位置的最终状态。
- 能力是否合法、目标是否有效。
- 能力持续时间、冷却和环境结果。
- 角色当前临时状态对移动的权威修饰。

### 交流

- 场景标记是否在有效位置。
- 标记类型、数量、频率和生命周期。
- 求救状态是否存在。

纯庆祝动作可作为短暂表现事件，但客户端不能伪造影响玩法的求救或操作完成状态。

### 资源、库存、道具和制作

- 采集成功、先后顺序和掉落。
- 库存增加、预留和原子消耗。
- 能力造成的材料修饰。
- 道具生成、持有、放置、使用、恢复和销毁。
- 配方有效性和制作输出。

### 机关、失败与救援

- 机关输入合法性。
- 解法条件是否满足。
- 状态机转换和完成。
- 失败输出和局部重置。
- 临时状态施加、持续、叠加、解除和过期。
- 救援参与者、进度和结果。
- 关键道具恢复。

## 9.2 只在本地决定

- 输入设备读取与键位。
- 摄像机、震动和取景。
- UI 页面、轮盘布局和焦点。
- 标记显示过滤。
- 当前会话的单人静音，以及本地语音输入设备和音量。
- 画质、语言、字幕、色觉和文字大小。
- 不影响玩法的粒子、音效和装饰动画。
- 本地预测和插值表现。
- 是否展示已看过的教学提示。

## 9.3 请求规则

- 所有共享状态请求包含 RequestId、PlayerId 和目标 ID。
- 主机校验距离、角色、能力、状态、资源和目标版本。
- 请求处理必须幂等。
- 持续状态使用可重建的网络属性或快照。
- 短暂意图使用 RPC 或输入流。
- 不依赖 RPC 顺序表达机关状态机。
- 不同步本地化字符串，传输稳定语义 ID。
- 不按帧同步不变的标记、能力定义或内容数据。

---

## 10. 数据驱动内容架构

## 10.1 三类数据

### 静态内容定义

由项目构建携带：

- RoleDefinition
- AbilityDefinition
- ItemDefinition
- RecipeDefinition
- PuzzleDefinition
- StatusDefinition
- LevelDefinition
- LevelVariantDefinition

### 权威运行时状态

由主机持有并同步：

- 当前玩家与角色
- 能力运行状态
- 团队库存
- 道具实例
- 机关阶段
- 临时状态
- 标记
- 随机种子与变体
- 关卡阶段

### 客户端表现状态

仅本地：

- UI 动画
- 标记屏幕布局
- 粒子和音效
- 摄像机
- 本地过滤和无障碍表现

## 10.2 Trigger/Condition/Action

触发器系统使用有限类型：

```text
Trigger
  OnInteract
  OnAbilityApplied
  OnItemPlaced
  OnAreaEntered
  OnTimerElapsed
  OnPlayerCountReached

Condition
  HasAbilityTag
  HasItemTag
  InventoryContains
  PlayerCountAtLeast
  PuzzleStateEquals
  StatusPresent

Action
  SetPuzzleState
  ConsumeInventory
  SpawnItem
  OpenRoute
  ApplyStatus
  ClearStatus
  ActivateCheckpoint
  CompleteObjective
```

约束：

- 所有类型显式注册。
- 数据不能指定任意 C# 类型或方法名执行。
- 条件只读取允许的查询接口。
- 动作只通过主机权威服务执行。
- 新类型必须有验证器、测试和版本说明。

## 10.3 多解法

每个主要问题使用 `SolutionPathDefinition` 描述：

- 能力标签条件
- 资源与道具条件
- 最小参与人数
- 交互步骤或时间窗
- 预计额外时间
- 预计资源成本
- 完成后目标状态
- 失败输出
- 恢复方案

关卡验证器至少检查：

- 主线目标是否有两人基础解。
- 是否存在不依赖某个单一角色的通用路径。
- 每个已实现角色是否存在能力机会。
- 所有路径是否汇聚到合法目标。

## 10.4 关卡内变体

`LevelVariantDefinition` 只允许选择：

- 资源候选点
- 机关白名单组合
- 支线路线开关
- 任务条件集合

规则：

- 主机生成种子。
- 主机从候选集合选择。
- 客户端接收最终选择。
- 测试日志记录种子和结果。
- 不随机重建地图拓扑。
- 不允许未经验证的任意组合。

## 10.5 未来玩家编辑器边界

MVP 只提供：

- 可序列化定义
- 稳定 ID
- 格式版本
- 白名单组件
- 内容校验
- 运行时加载边界

MVP 不提供：

- 编辑器 UI
- 在线内容包下载
- 发布与审核
- 评分与收藏
- 用户任意脚本
- 远程资源执行

未来玩家内容必须视为不可信输入，发布前需要大小、依赖、类型、版本、引用和可通关验证。

---

## 11. 本地保存与后端

## 11.1 本地保存

建议保存：

- 音量、画质、分辨率和帧率。
- 输入绑定与手柄设置。
- 摄像机设置。
- 字幕、色觉、文字大小和语言。
- 语音设备、语音音量、按键说话或开麦方式。
- 当前房间内的单人静音、标记过滤和隐私设置；参与者静音不跨会话持久化。
- 基础教学标记。
- 本地存档格式版本。
- 开发测试的可选匿名日志。

不得把本地保存当作权威：

- 角色选择确认
- 角色能力运行状态
- 团队库存
- 道具
- 机关
- 临时状态
- 救援
- 关卡变体
- 关卡完成

## 11.2 第一版后端结论

第一版仍不需要自建后端或独立服务器。

使用：

- Photon Cloud 房间与连接。
- 玩家主机运行权威模拟。
- 本地文件保存设置。
- 第一阶段不使用语音服务。

语音服务是基础设施，不等于自建业务后端。

## 11.3 未来何时需要后端

- 玩家关卡上传、下载、版本和审核。
- 账号、好友和跨设备存档。
- 公共匹配、举报和封禁。
- 可信奖励或经济系统。
- 精选关卡、评分和收藏。
- 专用服务器和正式运营。

---

## 12. 建议项目目录

```text
Assets/
└── WonderSquad/
    ├── Art/
    │   ├── Characters/
    │   ├── Environment/
    │   ├── Items/
    │   ├── Animations/
    │   ├── VFX/
    │   └── UI/
    ├── Audio/
    │   ├── Music/
    │   ├── SFX/
    │   └── Voice/
    ├── Content/
    │   ├── Roles/
    │   ├── Abilities/
    │   ├── Items/
    │   ├── Recipes/
    │   ├── Statuses/
    │   ├── Puzzles/
    │   └── Levels/
    │       └── SleepingForest/
    ├── Prefabs/
    │   ├── Player/
    │   ├── Items/
    │   ├── Interaction/
    │   ├── Communication/
    │   ├── Puzzles/
    │   ├── Status/
    │   ├── Network/
    │   ├── UI/
    │   │   └── MiniMap/
    │   └── Environment/
    ├── Scenes/
    │   ├── Bootstrap/
    │   ├── Menu/
    │   ├── Levels/
    │   │   └── SleepingForest/
    │   └── Tests/
    ├── Scripts/
    │   ├── Core/
    │   │   ├── Identifiers/
    │   │   ├── Events/
    │   │   ├── StateMachine/
    │   │   └── Versioning/
    │   ├── Content/
    │   │   ├── Definitions/
    │   │   ├── Loading/
    │   │   └── Validation/
    │   ├── Player/
    │   ├── Ability/
    │   ├── Interaction/
    │   ├── Communication/
    │   │   ├── Markers/
    │   │   ├── QuickIntent/
    │   │   └── Voice/
    │   ├── Inventory/
    │   ├── Item/
    │   ├── Crafting/
    │   ├── Puzzle/
    │   │   ├── Runtime/
    │   │   ├── Triggers/
    │   │   ├── Conditions/
    │   │   └── Actions/
    │   ├── Status/
    │   ├── Level/
    │   ├── Network/
    │   │   ├── Abstractions/
    │   │   ├── Fusion/
    │   │   └── Replication/
    │   ├── UI/
    │   └── Save/
    ├── Settings/
    │   ├── Input/
    │   ├── URP/
    │   └── Physics/
    └── Tests/
        ├── EditMode/
        ├── PlayMode/
        ├── Network/
        └── ContentValidation/
Packages/
ProjectSettings/
Design/
```

目录规则：

- 项目资源统一放在 `Assets/WonderSquad`。
- 第三方 Fusion、语音和美术包与业务目录分离。
- 核心模块使用 Assembly Definition 固化边界。
- `Content` 只保存静态定义。
- 关卡专属资产放在 `SleepingForest` 内容目录。
- Network/Fusion 封装具体 SDK。
- 不建立无边界的 `Managers` 或 `Misc`。

---

## 13. 关键接口与数据结构草案

以下为边界示例，不是完整实现。

## 13.1 标识

```csharp
public readonly record struct PlayerId(ulong Value);
public readonly record struct RoleId(string Value);
public readonly record struct AbilityId(string Value);
public readonly record struct AbilityTag(string Value);
public readonly record struct ItemDefinitionId(string Value);
public readonly record struct ItemInstanceId(ulong Value);
public readonly record struct RecipeId(string Value);
public readonly record struct PuzzleId(string Value);
public readonly record struct SolutionPathId(string Value);
public readonly record struct StatusId(string Value);
public readonly record struct MarkerId(ulong Value);
public readonly record struct LevelId(string Value);
public readonly record struct TriggerId(string Value);
public readonly record struct RequestId(uint Value);
```

## 13.2 角色与能力

```csharp
public sealed class RoleDefinition
{
    public RoleId Id;
    public IReadOnlyList<AbilityId> Abilities;
}

public sealed class AbilityDefinition
{
    public AbilityId Id;
    public IReadOnlyList<AbilityTag> Tags;
    public float CooldownSeconds;
}

public readonly record struct AbilityRequest(
    RequestId RequestId,
    PlayerId PlayerId,
    AbilityId AbilityId,
    ulong TargetId);

public readonly record struct AbilityResult(
    bool Accepted,
    AbilityFailureReason FailureReason);

public interface IAbilityAuthority
{
    AbilityResult TryActivate(in AbilityRequest request);
}

public interface IAbilityQuery
{
    bool PlayerHasTag(PlayerId playerId, AbilityTag tag);
}
```

## 13.3 交流

```csharp
public enum MarkerKind
{
    Location,
    Resource,
    Puzzle,
    Route,
    Danger,
    Help
}

public enum QuickIntentKind
{
    ComeHere,
    Wait,
    IWillDoIt,
    YouDoIt,
    NeedHelp,
    NeedResources,
    Done
}

public readonly record struct MarkerRequest(
    RequestId RequestId,
    PlayerId SenderId,
    MarkerKind Kind,
    Vector3 WorldPosition,
    ulong? TargetId);

public readonly record struct MarkerSnapshot(
    MarkerId MarkerId,
    PlayerId SenderId,
    MarkerKind Kind,
    Vector3 WorldPosition,
    double ExpireAtNetworkTime);

public interface ICommunicationAuthority
{
    bool TryCreateMarker(in MarkerRequest request);
    bool TryRemoveMarker(PlayerId senderId, MarkerId markerId);
}

public interface IVoiceChatService
{
    Task JoinSessionAsync(string sessionId, CancellationToken token);
    Task LeaveSessionAsync(CancellationToken token);
    void SetLocalMuted(bool muted);
    void SetParticipantMuted(PlayerId playerId, bool muted);
}
```

## 13.4 临时状态与救援

```csharp
public sealed class StatusDefinition
{
    public StatusId Id;
    public float MaxDurationSeconds;
    public bool CanBeRescued;
    public IReadOnlyList<AbilityTag> FastRescueTags;
}

public readonly record struct StatusSnapshot(
    PlayerId TargetId,
    StatusId StatusId,
    double AppliedAt,
    double ExpiresAt,
    uint Revision);

public readonly record struct RescueRequest(
    RequestId RequestId,
    PlayerId RescuerId,
    PlayerId TargetId,
    StatusId StatusId);

public interface IStatusAuthority
{
    bool TryApply(PlayerId targetId, StatusId statusId);
    bool TryClear(PlayerId targetId, StatusId statusId);
}

public interface IRescueAuthority
{
    bool TryStart(in RescueRequest request);
    bool TryComplete(in RescueRequest request);
}
```

## 13.5 机关数据

```csharp
public enum PuzzlePhase
{
    Inactive,
    Available,
    InProgress,
    Solved,
    Resetting
}

public sealed class SolutionPathDefinition
{
    public SolutionPathId Id;
    public IReadOnlyList<AbilityTag> OptionalAdvantageTags;
    public IReadOnlyList<ItemRequirement> ItemRequirements;
    public int MinimumPlayers;
    public string CompletionActionId;
    public string FailureActionId;
}

public sealed class PuzzleDefinition
{
    public PuzzleId Id;
    public IReadOnlyList<TriggerDefinition> Triggers;
    public IReadOnlyList<SolutionPathDefinition> Solutions;
    public string ResetActionId;
}

public readonly record struct PuzzleSnapshot(
    PuzzleId PuzzleId,
    PuzzlePhase Phase,
    SolutionPathId? SolvedBy,
    int ProgressStep,
    uint Revision);

public interface IPuzzleAuthority
{
    bool TryApply(in PuzzleInput input);
    bool TryReset(PuzzleId puzzleId);
    PuzzleSnapshot GetSnapshot(PuzzleId puzzleId);
}
```

## 13.6 触发器

```csharp
public enum TriggerKind
{
    Interact,
    AbilityApplied,
    ItemPlaced,
    AreaEntered,
    TimerElapsed,
    PlayerCountReached
}

public sealed class TriggerDefinition
{
    public TriggerId Id;
    public TriggerKind Kind;
    public IReadOnlyList<ConditionDefinition> Conditions;
    public IReadOnlyList<ActionDefinition> Actions;
}

public interface ITriggerRuntime
{
    void Submit(in TriggerEvent triggerEvent);
}

public interface IContentValidator
{
    ValidationReport ValidateLevel(LevelDefinition level);
}
```

`ConditionDefinition` 与 `ActionDefinition` 必须是有限、显式注册、可序列化的类型集合。

## 13.7 关卡与变体

```csharp
public sealed class LevelDefinition
{
    public int SchemaVersion;
    public LevelId Id;
    public int MinimumPlayers;
    public int MaximumPlayers;
    public int TargetDurationMinutes;
    public MiniMapDefinition MiniMap;
    public IReadOnlyList<PuzzleId> MainPuzzles;
    public IReadOnlyList<string> CheckpointIds;
    public IReadOnlyList<string> RecoveryPointIds;
    public LevelVariantDefinition Variants;
}

public sealed class MiniMapDefinition
{
    public Vector2 WorldMinXZ;
    public Vector2 WorldMaxXZ;
    public string BackgroundAssetId;
}

public sealed class LevelVariantDefinition
{
    public IReadOnlyList<ResourceVariantSet> ResourceSets;
    public IReadOnlyList<RouteVariantSet> RouteSets;
    public IReadOnlyList<ObjectiveVariantSet> ObjectiveSets;
}

public readonly record struct LevelSessionState(
    LevelPhase Phase,
    int ConnectedPlayerCount,
    uint RandomSeed,
    string VariantId,
    double StartNetworkTime,
    int SolvedPuzzleCount);

public interface ILevelAuthority
{
    bool CanStart();
    void StartLevel(uint randomSeed);
    void NotifyPuzzleSolved(PuzzleId puzzleId, SolutionPathId solutionId);
    void RecoverAllPlayers();
    void CompleteLevel();
}
```

## 13.8 网络会话

```csharp
public interface INetworkSession
{
    SessionStatus Status { get; }
    Task CreateAsync(string displayName, CancellationToken token);
    Task JoinAsync(SessionJoinRequest request, CancellationToken token);
    Task LeaveAsync(CancellationToken token);
}

public interface IGameplayRequestSender
{
    void SendInteraction(in InteractionContext request);
    void SendAbility(in AbilityRequest request);
    void SendMarker(in MarkerRequest request);
    void SendCraft(in CraftRequest request);
    void SendPuzzleInput(in PuzzleInput request);
    void SendRescue(in RescueRequest request);
}
```

## 13.9 本地设置

```csharp
public sealed class LocalSettingsData
{
    public int SchemaVersion;
    public float MasterVolume;
    public float MusicVolume;
    public float SfxVolume;
    public float VoiceVolume;
    public bool LocalVoiceMuted;
    public float CameraSensitivity;
    public bool CameraShakeEnabled;
    public bool HideNonFriendMarkers;
    public string LanguageCode;
    public bool BasicTutorialSeen;
}
```

---

## 14. 场景与生命周期

场景：

- `Bootstrap`
- `Menu`
- `SleepingForest`
- `Result` 可独立或作为 UI 流程
- `Test_*`

流程：

```text
Bootstrap
→ 读取本地设置与内容目录
→ 验证必要内容
→ Menu
→ 创建或加入 Fusion 房间
→ 选择角色
→ 第一阶段跳过语音房间；后续版本可通过适配器加入
→ 主机加载 SleepingForest
→ 主机选择种子和变体
→ 生成 2～4 名玩家
→ 开始关卡
→ 完成三个机关
→ 主机宣布完成
→ Result
→ 返回关卡选择或退出房间
```

生命周期规则：

- 关卡对象随关卡卸载。
- 标记、临时状态和救援进度随会话释放。
- 退出房间同时退出语音房间。
- 主机退出时 MVP 可结束本局，不强制迁移。
- 不依赖开放世界常驻状态。

---

## 15. 异常与恢复

| 异常 | 权威处理 |
|---|---|
| 玩家掉出地图 | 主机应用趣味反馈并传送到最近检查点 |
| 玩家被临时状态困住 | 队友救援、超时解除或安全恢复 |
| 全队无法互救 | 主机触发全队脱困到检查点 |
| 关键道具离图 | 主机移到配置的恢复点 |
| 持有者离开 | 主机解除持有并安全放置 |
| 机关非法状态 | 主机局部重置 |
| 重复请求 | RequestId 去重并返回已有结果 |
| 客户端断线 | 显示重连；超时返回菜单 |
| 主机退出 | 结束本局并明确提示 |
| 标记刷屏 | 主机频率限制；客户端可过滤 |
| 麦克风权限拒绝 | 保持非语音完整可玩，不阻挡进入 |
| 内容配置无效 | 开发构建阻止启动并报告；发布构建使用安全回退 |
| 变体无法加载 | 回退默认变体并记录种子与错误 |
| 本地设置损坏 | 恢复默认并保留诊断信息 |

---

## 16. 测试架构

### 16.1 Edit Mode

- ID 唯一性。
- 内容版本和引用。
- 配方原子消耗。
- 能力标签查询。
- Puzzle 合法状态转换。
- Trigger/Condition/Action 白名单。
- 主线通用替代解法存在性。
- 标记频率限制。
- 状态持续、解除和救援规则。
- 固定默认变体解析结果一致。

### 16.2 Play Mode

- 移动、能力、标记和动作轮盘。
- 资源、制作和通用替代工具。
- 三个机关的优势、替代和恢复解法。
- 临时状态、队友救援和全队脱困。
- 检查点与关键道具恢复。
- 关卡开始、完成、结算和返回。

### 16.3 联机

- Host + 1、2、3 客户端。
- 不同角色选择和重复角色。
- 能力同时作用于同一目标。
- 标记创建、过期、过滤和刷屏。
- 资源、道具和制作争用。
- 同时机关输入。
- 状态施加与救援竞争。
- 玩家持有关键道具或救援中离开。
- 固定默认变体一致。
- 延迟、抖动和丢包。
- 全程不开麦完成 10～20 分钟关卡。

### 16.4 体验测试

- 缺少每种角色的代表阵容。
- 两人基础解和四人并行解。
- 只用游戏内交流。
- 不使用语音完成全流程。
- 不同辅助设置。
- 失败是否有趣、清楚且可恢复。

---

## 17. MVP 不采用的架构

- 微服务。
- 自建账号和认证。
- 独立专用服务器。
- ECS/DOTS 核心玩法。
- 开放世界流式加载。
- 随机生成大地图。
- 通用技能树、战斗和装备框架。
- 玩家关卡编辑器 UI。
- UGC 上传、审核、评分和浏览后端。
- 任意玩家脚本执行。
- 完整多人世界快照存档。
- 语音识别驱动谜题。

---

## 18. 技术检查点

### 检查点一：初始化

- Unity `6000.3.21f1`（Unity 6.3 LTS）、URP 和 Fusion 版本锁定。
- 双实例连接。
- 内容 ID 与验证器工作。
- 已确认语音 SDK 延期到 MVP 之后。

### 检查点二：单机窄切片

- 一种能力、一个标记、一个资源、一个道具、一个机关、一个状态和一次救援。
- 所有内容来自数据定义。

### 检查点三：双人无麦切片

- 两人只用标记和快捷意图完成。
- 主机决定能力、机关、状态和救援结果。
- 重复请求不产生重复结果。

### 检查点四：三机关灰盒

- 每个机关有两人基础解。
- 至少一个有两种以上解法。
- 缺少特定角色仍可完成。
- 普通失败可局部恢复。

### 检查点五：完整 MVP

- 2～4 人完成《沉睡森林》。
- 10～20 分钟。
- 无麦通关。
- 角色能力、欢乐失败、救援和多解法均被验证。
- 未扩展到编辑器、后端或开放世界。

---

## 19. 最终决策

Wonder Squad v1.1 MVP 采用：

> Unity `6000.3.21f1`（Unity 6.3 LTS）+ Universal 3D/URP + 2.5D 表现 + Photon Fusion 2 Host Mode + 主机集中权威 + 数据驱动独立关卡 + 游戏内非语音交流 + 角色能力标签 + 临时状态与救援 + 本地轻量设置 + 无自建后端。

语音在 MVP 验证后可通过独立适配器接入，但不得成为玩法依赖。玩家关卡编辑器不进入 MVP；当前只建立未来可使用的稳定、版本化、可校验内容边界。

---

## 20. v1.0 到 v1.1 版本变更记录

### 新增模块

- `Ability`
- `Communication`
- `Status/Rescue`
- `Content/Validation`
- Trigger/Condition/Action 运行边界
- 可选 Voice Adapter

### 扩展模块

- Player：增加角色、能力、标记和状态输入。
- Interaction：增加能力、救援和多人同步窗口。
- Inventory：增加主机权威能力修饰。
- Item：增加能力标签和数据恢复点。
- Crafting：增加角色优势与通用替代配方。
- Puzzle：增加多条解法、失败输出、数据触发器和双人基础解。
- Level：增加独立关卡闭环、变体、阵容与全队脱困。
- Network：增加能力、标记、状态、救援和种子同步。
- UI：增加动作轮盘、标记、能力、状态、救援和语音设置。
- Save：增加语音、隐私、过滤和无障碍本地数据。

### 权威变化

- 新增由主机决定的能力结果。
- 新增由主机校验的场景标记和求救状态。
- 新增由主机决定的临时状态、救援和全队脱困。
- 预留由主机生成随机种子和选择关卡变体的后续能力；MVP 使用固定默认变体。

### 数据架构变化

- 增加 Role、Ability、Status、Level、Variant 和 SolutionPath 定义。
- 增加稳定 ID、内容版本和校验器。
- 增加有限 Trigger/Condition/Action 类型。
- 明确静态定义、权威运行时状态与本地表现状态三层分离。
- 为未来玩家编辑器保留安全数据边界，但不实现编辑器平台。

### 保持不变

- Unity `6000.3.21f1`（Unity 6.3 LTS）正式版本基线。
- Universal 3D + URP 的 2.5D 方向。
- Photon Fusion 2 Host Mode。
- 主机集中权威原则。
- 2～4 人。
- 《沉睡森林》一张 MVP 地图。
- 单关 10～20 分钟。
- MVP 不需要自建后端或独立服务器。

### 删除或替换

- 删除随机生成大地图架构方向，替换为关卡内白名单变体。
- 将“未来不建设内容平台”的绝对约束替换为“只建设最小数据边界，不建设玩家编辑器和发布平台”。
- 明确语音不得进入谜题条件、长期存档或玩法状态。
