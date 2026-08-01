# Wonder Squad 文档一致性检查报告

## 1. 检查范围

本报告检查以下 17 份文档：

- `Design/WonderSquad_Design_Document_v1.1.md`
- `Design/MVP_Development_Plan_v1.1.md`
- `Design/Technical_Architecture_v1.1.md`
- `Docs/02_Gameplay/` 下全部 10 份文档
- `Docs/03_Level/Level_Framework.md`
- `Docs/04_Technical/` 下全部 2 份文档
- `Docs/05_Data/Data_Driven_Design.md`

初次检查仅生成报告；随后按本报告完成了文档层修复。整个过程没有创建 Unity 代码。

---

## 修复状态（2026-08-01）

本报告最初记录的 13 项问题已完成文档层修复。以下状态以修复后的当前文档为准；后续问题清单保留为历史审计依据。

| 问题 | 状态 | 修复摘要 |
|---|---|---|
| DOC-CONS-001 | 已修复 | 区分编译依赖、端口和事件，统一为 Core/Contracts + Content 的单向结构 |
| DOC-CONS-002 | 已修复 | Level Recovery Orchestrator 成为复合恢复唯一编排者，并定义事务顺序 |
| DOC-CONS-003 | 已修复 | 允许重复角色，明确四角色 2～4 人共 65 个无序组合的结构校验 |
| DOC-CONS-004 | 已修复 | 新增 P0/P1/P2，语音和实际随机变体延期到 MVP 之后 |
| DOC-CONS-005 | 已修复 | 能力限制为 Anchor、Socket、EnvironmentState 和 RevealTarget |
| DOC-CONS-006 | 已修复 | 冻结四个初始角色，每个角色一个标志性 MVP 能力 |
| DOC-CONS-007 | 已修复 | 明确第一阶段不集成语音 SDK |
| DOC-CONS-008 | 已修复 | 明确轻量小地图职责、坐标映射和 LevelDefinition 数据 |
| DOC-CONS-009 | 已修复 | MVP 使用固定默认变体，实际随机变体延期 |
| DOC-CONS-010 | 已修复 | 校验器只负责结构规则，实际可通关性由流程测试和人工游玩验证 |
| DOC-CONS-011 | 已修复 | 参与者静音限定当前会话，不跨 PlayerId 持久化 |
| DOC-CONS-012 | 已修复 | 新增《沉睡森林》物品表示矩阵 |
| DOC-CONS-013 | 已修复 | 新增 `Docs/01_Project/Project_Glossary.md` |

修复后未发现仍处于“阻塞/高/中/低”状态的已确认问题。尚未确定的具体数值、动画和内容参数继续保留在各系统文档的“待确认事项”中。

---

## 2. 初次检查结论（历史）

核心方向整体一致：

- 玩家人数统一为 2～4 人。
- 单局时长统一为 10～20 分钟，推荐约 15 分钟。
- 地图结构统一为“一关一张独立小地图”，MVP 只做《沉睡森林》。
- 角色能力参与解谜，且不得形成单一角色硬锁。
- 普通失败统一采用搞笑状态、救援或局部恢复，不立即重开整关。
- 非语音交流是 MVP 必做项，语音不能成为通关条件。
- 联机统一采用 Photon Fusion 2 Host Mode，主机决定共享结果。
- 玩家关卡编辑器、发布和审核均不属于 MVP。
- 数据驱动只预留未来编辑器边界，不应演变成通用脚本平台。

未发现会使项目完全无法启动的设计矛盾，但存在需要在实现前处理的高风险问题：

1. 模块依赖图包含多组明确循环，且与架构文档的“防循环规则”矛盾。
2. 角色合法组合和通关验证范围没有可执行定义。
3. MVP 同时包含较多核心系统，优先级和退出条件仍不够严格。
4. 角色能力的动态物理实现与当前网络架构之间缺少明确约束。

### 初次问题数量

| 严重级别 | 数量 |
|---|---:|
| 阻塞 | 0 |
| 高 | 5 |
| 中 | 7 |
| 低 | 1 |

---

## 3. 十二项检查结果摘要

| 检查项 | 结论 |
|---|---|
| 游戏人数 | 一致：2～4 人 |
| 单局时长 | 一致：10～20 分钟 |
| 地图结构 | 一致：独立小地图，MVP 仅《沉睡森林》 |
| 角色能力规则 | 方向一致，但实现角色数量和能力粒度未确定 |
| 缺少角色的替代解法 | 原则一致，但“合法阵容”和测试覆盖定义不足 |
| 欢乐失败后继续 | 原则一致，但恢复编排职责存在重叠 |
| 语音与非语音 | 核心原则一致；语音是否进入 MVP 未冻结 |
| MVP 范围 | 主体一致；关卡变体、语音和完整角色数量仍摇摆 |
| 联机权威 | 无直接冲突：共享结果均由主机决定 |
| 超出 MVP 的系统 | 未直接实现编辑器或后端；部分预留和验证任务存在过度设计风险 |
| 循环依赖 | 存在多组高风险循环 |
| 技术支持能力 | 基础架构可支持固定插槽式解谜；自由动态建造/绳索/环境物理尚未被充分支持 |

---

## 4. 初次问题清单（均已修复）

## DOC-CONS-001：模块依赖图存在多组循环

- **严重级别：高**
- **涉及文件：**
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/02_Gameplay/Player_System.md`
  - `Docs/02_Gameplay/Character_Ability_System.md`
  - `Docs/02_Gameplay/Interaction_System.md`
  - `Docs/02_Gameplay/Inventory_System.md`
  - `Docs/02_Gameplay/Item_System.md`
  - `Docs/02_Gameplay/Crafting_System.md`
  - `Docs/02_Gameplay/Puzzle_System.md`
  - `Docs/02_Gameplay/Status_And_Failure_System.md`
  - `Docs/02_Gameplay/Communication_System.md`
  - `Docs/03_Level/Level_Framework.md`
- **问题描述：**
  - 技术架构要求防止循环依赖，但其模块表和系统文档列出了多组双向依赖：
    - Player ↔ Ability
    - Player ↔ Interaction
    - Item ↔ Inventory
    - Item ↔ Crafting
    - Crafting ↔ Puzzle
    - Puzzle ↔ Status/Rescue
    - Status/Rescue ↔ Level
    - Item ↔ Level
    - Communication ↔ Status/Rescue
    - Game Loop ↔ Level
  - 当前“与其他系统的依赖”混合记录了编译依赖、运行时调用、事件订阅和业务协作，无法直接指导 Assembly Definition 或代码引用方向。
- **推荐修改方案：**
  - 将每份文档的依赖拆成：
    1. 编译时依赖
    2. 只读查询接口
    3. 命令接口
    4. 发布事件
    5. 事件消费者
  - 建立单向依赖层：
    - Core/Content Abstractions
    - Gameplay Domains
    - Orchestration
    - Infrastructure
    - Presentation
  - Player 只依赖输入和状态接口，不依赖 Ability 实现。
  - Puzzle 调用 `IStatusCommand`，Status 不反向依赖 Puzzle。
  - Level 订阅状态和机关事件，Status 与 Puzzle 不直接调用 Level 实现。
  - Inventory 依赖 Content 中的 ItemDefinition，不依赖运行时 Item 模块。

## DOC-CONS-002：失败恢复、机关重置和全队脱困的唯一编排者不明确

- **严重级别：高**
- **涉及文件：**
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/02_Gameplay/Player_System.md`
  - `Docs/02_Gameplay/Item_System.md`
  - `Docs/02_Gameplay/Puzzle_System.md`
  - `Docs/02_Gameplay/Status_And_Failure_System.md`
  - `Docs/03_Level/Level_Framework.md`
- **问题描述：**
  - Player 负责请求跌落恢复。
  - Item 负责关键物品恢复。
  - Puzzle 负责失败输出和局部重置。
  - Status 负责全队无法互救时的安全恢复。
  - Level 同时负责检查点、恢复点、关键道具恢复点和全队脱困。
  - 各文档没有定义一次复合失败中由谁确定操作顺序。例如“玩家受困并持有关键道具，同时机关正在重置”可能同时触发多个恢复过程，造成重复传送、重复物品生成或旧状态覆盖。
- **推荐修改方案：**
  - 指定 `Level Recovery Orchestrator` 为复合恢复流程的唯一编排者。
  - Player、Item、Puzzle、Status 只报告异常或执行单域命令。
  - 定义恢复顺序：
    1. 冻结新交互
    2. 取消救援和持有关系
    3. 恢复关键物品
    4. 重置机关
    5. 清理临时状态
    6. 移动玩家
    7. 增加 Revision 并恢复交互
  - 所有恢复请求使用同一 RecoveryTransactionId，保证幂等。

## DOC-CONS-003：“合法角色组合”没有可执行定义

- **严重级别：高**
- **涉及文件：**
  - `Design/WonderSquad_Design_Document_v1.1.md`
  - `Design/MVP_Development_Plan_v1.1.md`
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/02_Gameplay/Character_Ability_System.md`
  - `Docs/02_Gameplay/Puzzle_System.md`
  - `Docs/03_Level/Level_Framework.md`
  - `Docs/05_Data/Data_Driven_Design.md`
- **问题描述：**
  - 总设计要求任何 2～4 人合法角色组合原则上都能通关。
  - 系统文档尚未确定是否允许重复角色，也未定义“合法”的限制。
  - MVP 测试计划主要覆盖“缺少某个角色”的代表阵容，不能证明所有允许组合可通关。
  - 若允许重复角色，则“两名相同角色”“三名相同角色”“四名相同角色”是否都必须通关仍不明确。
- **推荐修改方案：**
  - 在设计文档中冻结以下之一：
    - 允许任意重复角色，则所有 2～4 人组合都必须有主线通路；或
    - 限制重复角色，并明确定义房间选择规则。
  - 建立能力标签覆盖矩阵，而不是只按角色名称测试。
  - MVP 至少验证：
    - 最少能力覆盖阵容
    - 单一角色重复阵容（若允许）
    - 缺少每个能力方向的阵容
    - 2 人最不利组合
  - 将“原则上可通关”转换为明确的发布验收规则。

## DOC-CONS-004：MVP 核心范围过宽，缺少严格的 P0/P1 退出条件

- **严重级别：高**
- **涉及文件：**
  - `Design/MVP_Development_Plan_v1.1.md`
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/02_Gameplay/` 下全部文档
  - `Docs/03_Level/Level_Framework.md`
  - `Docs/05_Data/Data_Driven_Design.md`
- **问题描述：**
  - 当前 MVP 同时包含：
    - 2～4 人实时联机
    - 多角色能力
    - 场景和小地图标记
    - 动作轮盘
    - 资源、团队库存、道具、制作
    - 三个多解法机关
    - 临时状态、救援、全队脱困
    - 数据驱动触发器、条件、动作和校验
    - 关卡内变体
    - 条件性语音
  - 对个人或 2～3 人团队，这些系统同时进入首个可验证版本的成本明显偏高。
  - 计划虽将语音设为条件性，但没有对角色数量、关卡变体、小地图和数据校验深度建立同等级退出条件。
- **推荐修改方案：**
  - 建立明确优先级：
    - P0：双人联机、一个能力差异、标记、一个多解法机关、一个状态与救援。
    - P1：扩展至四人、四能力方向、三个机关、小地图标记。
    - P2：关卡变体、语音和更完整内容校验。
  - 每个 P1/P2 项写明进入条件和延期条件。
  - 先完成“双人无麦窄切片”，验证好玩后再扩展完整《沉睡森林》。

## DOC-CONS-005：动态角色能力缺少与网络物理兼容的 MVP 约束

- **严重级别：高**
- **涉及文件：**
  - `Design/WonderSquad_Design_Document_v1.1.md`
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/02_Gameplay/Character_Ability_System.md`
  - `Docs/02_Gameplay/Item_System.md`
  - `Docs/02_Gameplay/Puzzle_System.md`
  - `Docs/04_Technical/Network_Authority_Rules.md`
- **问题描述：**
  - 总设计包含绳索、临时结构、牵引特殊物体和改变环境等能力方向。
  - 系统文档尚未确定这些能力使用自由动态物理，还是预定义交互点和插槽。
  - 当前网络架构可以稳定支持有限状态和插槽式结果，但没有为自由绳索、任意建造、连续环境变形或多人网络物理定义同步和回滚方案。
  - 如果直接按自由物理能力实现，成本和风险明显高于当前 MVP 架构估计。
- **推荐修改方案：**
  - MVP 明确限制为数据驱动的预定义能力机会：
    - 绳索固定到 Author-defined Anchor
    - 结构放入预定义 Socket
    - 魔法切换有限 Environment State
    - 观察者揭示预定义 Route/Clue
  - 网络只同步能力请求、目标 ID 和最终有限状态。
  - 自由建造、连续牵引和复杂绳索物理列为非 MVP。

## DOC-CONS-006：MVP 实现多少角色和能力仍未冻结

- **严重级别：中**
- **涉及文件：**
  - `Design/WonderSquad_Design_Document_v1.1.md`
  - `Design/MVP_Development_Plan_v1.1.md`
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/02_Gameplay/Character_Ability_System.md`
- **问题描述：**
  - 总设计列出探险家、工匠、魔法师和观察者四个初始角色。
  - 技术与系统文档允许“四种方向的最小集合或代表性子集”。
  - MVP 完成标准只要求“每种已实现能力”有用途，因此无法判断缺少哪项内容仍算 MVP 完成。
- **推荐修改方案：**
  - 明确 MVP 角色清单及每个角色唯一的 P0 能力。
  - 如果先做代表性子集，应在计划中写明角色数量和延期角色。
  - 《沉睡森林》内容验收与角色清单使用同一版本化矩阵。

## DOC-CONS-007：语音是产品需求，但是否进入 MVP 尚未冻结

- **严重级别：中**
- **涉及文件：**
  - `Design/WonderSquad_Design_Document_v1.1.md`
  - `Design/MVP_Development_Plan_v1.1.md`
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/02_Gameplay/Communication_System.md`
  - `Docs/04_Technical/Save_Data_Boundary.md`
- **问题描述：**
  - 所有文档一致认为非语音交流是必做，语音不是通关条件。
  - 但基础语音在 MVP 中仍是条件性选项，尚未做最终决定。
  - 该决策影响 SDK、平台权限、隐私、静音、设备选择、测试和发布审核，不能长期保持未决。
- **推荐修改方案：**
  - 在项目初始化里程碑设立明确的 Go/No-Go 日期。
  - 若 No-Go：
    - MVP 验收明确不包含语音。
    - 保留 `IVoiceChatService` 边界，但不建立具体包依赖和不可用 UI。
  - 若 Go：
    - 明确 SDK、费用上限、首发平台、权限流程和最小举报/静音能力。

## DOC-CONS-008：小地图标记是设计要求，但技术架构没有明确 MiniMap 职责

- **严重级别：中**
- **涉及文件：**
  - `Design/WonderSquad_Design_Document_v1.1.md`
  - `Design/MVP_Development_Plan_v1.1.md`
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/02_Gameplay/Communication_System.md`
  - `Docs/03_Level/Level_Framework.md`
- **问题描述：**
  - 总设计要求标记必须出现在场景和小地图。
  - Communication 文档已把世界与小地图表现列入 MVP。
  - MVP 计划和技术架构没有定义小地图数据源、坐标映射、显示范围、UI 所有权或场景标记与小地图标记的复用方式。
  - 当前 UI 模块可以承载该功能，但缺少可实现规则。
- **推荐修改方案：**
  - 新增轻量 MiniMap 设计小节，而不必新增独立核心模块。
  - 定义：
    - 固定俯视图或简化拓扑图
    - 世界坐标到小地图坐标转换
    - 标记类型、裁剪和层级
    - 关卡边界数据来源
    - 不可见区域是否隐藏
  - 若小地图成本超出 MVP，应先修改总设计要求，不能由系统文档自行降级。

## DOC-CONS-009：关卡内变体在设计中是可选，在计划中接近强制

- **严重级别：中**
- **涉及文件：**
  - `Design/WonderSquad_Design_Document_v1.1.md`
  - `Design/MVP_Development_Plan_v1.1.md`
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/03_Level/Level_Framework.md`
  - `Docs/05_Data/Data_Driven_Design.md`
- **问题描述：**
  - 总设计对 MVP 使用“可加入少量关卡内变化”。
  - MVP 计划和 Level Framework 使用“至少一种低风险变体”或把变体放入完整关卡任务。
  - 完成定义又使用“若使用关卡变体”，形成可选与必做并存。
- **推荐修改方案：**
  - 统一为以下之一：
    - P0 不要求变体，只要求数据结构可支持；或
    - P0 明确实现一种资源点候选变体。
  - 对小团队建议选择第一种，把实际变体放到 P1。

## DOC-CONS-010：数据校验器的“可通关性”责任可能过度设计

- **严重级别：中**
- **涉及文件：**
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/02_Gameplay/Puzzle_System.md`
  - `Docs/03_Level/Level_Framework.md`
  - `Docs/05_Data/Data_Driven_Design.md`
- **问题描述：**
  - 文档要求验证器发现无通用替代解、角色硬锁、非法组合，并保障变体可通关。
  - 对包含多人时序、物理、资源消耗和多条路径的机关，自动证明可通关成本很高，甚至不可行。
  - Data Driven 文档末尾承认 MVP 只能做结构检查和测试矩阵，但其他文档的验收措辞更强。
- **推荐修改方案：**
  - 明确区分：
    - 自动结构校验：ID、引用、白名单、至少存在一条声明的通用路径、资源预算非负。
    - 人工/自动化游玩验证：实际可通关、时长、物理可行性。
  - 校验器不承诺证明关卡可通关，只证明配置结构满足最低规则。

## DOC-CONS-011：单人静音的保存键缺少稳定身份

- **严重级别：中**
- **涉及文件：**
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/02_Gameplay/Communication_System.md`
  - `Docs/04_Technical/Save_Data_Boundary.md`
- **问题描述：**
  - 文档允许保存“单人静音”设置。
  - 当前 MVP 没有正式账号体系，PlayerId 是会话级标识，显示名也不是可信身份。
  - 因此无法可靠地跨会话持久保存某个具体玩家的静音状态。
- **推荐修改方案：**
  - MVP 将单人静音限定为当前房间或当前应用会话数据，不写入长期本地存档。
  - 只持久保存全局语音开关、语音音量和输入设备。
  - 未来有稳定账号 ID 后再保存跨会话玩家静音列表。

## DOC-CONS-012：资源、库存物品和场景道具的边界尚未形成完整分类表

- **严重级别：中**
- **涉及文件：**
  - `Design/MVP_Development_Plan_v1.1.md`
  - `Docs/02_Gameplay/Inventory_System.md`
  - `Docs/02_Gameplay/Item_System.md`
  - `Docs/02_Gameplay/Crafting_System.md`
  - `Docs/02_Gameplay/Puzzle_System.md`
- **问题描述：**
  - 文档原则上区分团队共享资源和大型场景道具，但具体材料未分类。
  - 尚未确定木材、水晶、零件和传送门部件哪些直接入团队库存，哪些必须搬运到插槽。
  - 该选择会显著影响合作体验、网络同步、制作输出和关键物品恢复。
- **推荐修改方案：**
  - 为《沉睡森林》建立 Item Representation Matrix：
    - 定义 ID
    - Resource/Tool/PuzzlePart/Quest 分类
    - 抽象数量或场景实例
    - 是否可携带
    - 是否关键
    - 生成与恢复规则
    - 制作输出形态
  - MVP 中同一物品不要在抽象库存和场景实例之间无规则切换。

## DOC-CONS-013：同一概念存在多套名称，缺少统一术语表

- **严重级别：低**
- **涉及文件：**
  - `Design/WonderSquad_Design_Document_v1.1.md`
  - `Design/MVP_Development_Plan_v1.1.md`
  - `Design/Technical_Architecture_v1.1.md`
  - `Docs/02_Gameplay/` 下多份文档
  - `Docs/03_Level/Level_Framework.md`
  - `Docs/05_Data/Data_Driven_Design.md`
- **问题描述：**
  - 以下术语混用但关系未正式定义：
    - 机关 / 谜题 / Puzzle
    - 道具 / 物品 / Item
    - 搞笑状态 / 临时状态 / 失败状态 / Status Effect
    - 关卡内变化 / 有限随机 / 关卡变体 / LevelVariant
    - 检查点 / 恢复点 / 关键道具恢复点
    - 主机 / 服务器 / 权威端 / State Authority
  - 这些术语大多可以共存，但会影响类名、数据名、测试用例和跨团队沟通。
- **推荐修改方案：**
  - 新增项目术语表并规定：
    - 中文正式名
    - 英文/代码名
    - 定义
    - 不推荐的同义词
  - 建议明确：
    - Puzzle = 机关运行系统；PuzzleDefinition = 单个机关配置
    - Item = 所有物品定义；Resource/Tool/PuzzlePart 为分类
    - Status = 权威临时状态；Failure 是产生状态或恢复的事件
    - LevelVariant = 固定关卡内的白名单变化
    - Checkpoint = 玩家恢复位置；RecoveryPoint = 物品或系统恢复位置
    - MVP 的权威端统一称为 Host/主机

---

## 5. 无冲突项

以下事项已在全部相关文档中保持一致，无需调整：

### 人数、时长与地图

- 2～4 人。
- 每关 10～20 分钟。
- MVP 只做《沉睡森林》。
- 一关一张独立小地图。
- 不做连续开放大世界。

### 核心体验

- 不开语音也能通关。
- 语音不能成为机关条件。
- 角色能力改变效率或路径。
- 缺少特定角色仍应存在通用替代。
- 至少一个主要问题有两种以上解法。
- 普通失败不立即重开整关。
- 救援和局部恢复优先。

### 联机与保存

- Photon Fusion 2 Host Mode。
- 客户端提交意图，主机决定共享结果。
- 资源、库存、道具、制作、机关、状态、救援和关卡均由主机确认。
- 摄像机、UI、语音音量和本地过滤只在本地处理。
- MVP 不保存局内权威世界状态。
- MVP 不需要自建后端或独立服务器。

### 非 MVP

- 商城、氪金、排行榜和公会。
- 复杂基地、大量角色和复杂战斗。
- 随机生成大地图。
- 玩家关卡编辑器 UI。
- UGC 发布、审核、评分和浏览。
- 任意玩家脚本。

---

## 6. 建议修订顺序

1. 先解决 DOC-CONS-001，建立无环代码依赖图。
2. 再解决 DOC-CONS-002，确定恢复流程唯一编排者。
3. 冻结 DOC-CONS-003 和 DOC-CONS-006 的角色组合与能力清单。
4. 根据团队资源处理 DOC-CONS-004、DOC-CONS-007 和 DOC-CONS-009，冻结 MVP P0/P1。
5. 用预定义插槽约束处理 DOC-CONS-005。
6. 补齐小地图、物品分类和静音数据边界。
7. 最后建立术语表并统一所有文档用词。

完成前四步后，再开始大规模 Unity 业务实现，可以显著降低返工风险。
