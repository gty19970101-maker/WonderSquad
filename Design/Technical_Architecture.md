# Wonder Squad MVP 技术架构

## 1. 文档目的

本文档基于以下文件：

- 《Wonder Squad 游戏开发总设计文档 v1.0》
- 《Wonder Squad MVP 开发计划》

本文档只定义 MVP 的技术边界、模块职责、依赖方向、网络权威、数据归属以及关键接口草案，不包含完整业务实现代码。

MVP 的技术架构只服务于一个目标：

> 用一张《沉睡森林》地图和 2～4 人联机，可靠验证“多人合作、环境解谜、创造解决方案”是否好玩。

架构原则：

- 优先完成可测试的完整玩法闭环。
- 保持简单，避免为未来可能出现的需求提前建设复杂平台。
- 核心玩法逻辑与具体网络 SDK 隔离。
- 联机状态从设计之初采用集中权威，避免客户端各自决定关键结果。
- 配置数据与运行时状态分离。
- 关键系统可在单机测试环境中运行，但不能绕过正式的权威规则。

---

## 2. 技术选型摘要

| 项目 | MVP 建议 |
|---|---|
| Unity 版本 | Unity 6.3 LTS，锁定团队统一的最新补丁版本 |
| 项目模板 | Universal 3D 模板 |
| 画面形态 | 2.5D：3D 场景与物理、受控视角、卡通化表现 |
| 渲染管线 | URP |
| 输入 | Unity Input System |
| 联机 SDK | Photon Fusion 2 |
| 联机拓扑 | Host Mode：一名玩家作为主机兼客户端 |
| 玩家数量 | 2～4 人 |
| 独立游戏服务器 | MVP 不需要 |
| 自建后端 | MVP 不需要 |
| 持久化 | 本地设置与少量本地进度；局内共享状态不落本地 |
| 目标平台 | 首版优先 Windows PC，其他平台在验证后决定 |

所有版本号都应写入项目清单并锁定。MVP 开发中途不主动升级 Unity 主版本、网络 SDK 大版本或渲染管线。

---

## 3. Unity 版本建议

### 3.1 推荐版本

推荐使用 **Unity 6.3 LTS**，并在项目初始化当天选择 Unity Hub 中可用的最新稳定补丁版本，例如 `6000.3.xf1`。

选择理由：

- Unity 6.3 是当前 LTS 版本，适合进入固定生产周期的项目。
- 官方支持期持续到 2027 年 12 月，覆盖 MVP 的合理开发周期。
- Unity 6 系列拥有当前的 Input System、URP 和多人开发工具支持。
- 个人或 2～3 人团队更需要稳定版本，避免追逐预览版功能。

### 3.2 版本管理规则

- `ProjectVersion.txt` 是团队唯一版本依据。
- 所有人安装完全相同的编辑器版本和必要平台模块。
- 禁止使用 Alpha、Beta 或 Preview 编辑器作为主开发环境。
- 补丁升级只在出现明确阻断问题或安全问题时进行。
- 升级前建立可运行版本标记，并在独立分支完成打开、构建、联机和关卡回归测试。

---

## 4. 项目模板与画面形态

### 4.1 结论

使用 **Universal 3D 模板**创建项目，在 3D 世界中实现 **2.5D 卡通玩法表现**。

这里的 2.5D 指：

- 场景、角色、道具和机关使用 3D 模型。
- 移动、碰撞、跳跃、搬运和环境机关使用 3D 物理空间。
- 摄像机采用受控的俯视斜角、等距感或有限旋转，而非完全自由的第一/第三人称视角。
- UI、图标、提示和部分特效可使用 2D 资源。

### 4.2 不选择纯 2D 模板的原因

- 设计包含跳跃、攀爬、跨越河流、搬运、建造与环境空间解谜。
- 《沉睡森林》需要利用高度、地形和空间关系形成多种解法。
- 纯 2D 物理会限制关卡表达，并增加模拟立体空间的额外成本。

### 4.3 不定义为完全自由 3D 的原因

- 受控视角更接近设计文档中的 2.5D 卡通方向。
- 可降低镜头、导航、迷路和联机可读性问题。
- 小团队可以更集中地设计谜题视线、地标和路径。

### 4.4 渲染与物理建议

- 使用 URP，优先性能稳定和跨平台能力。
- 使用 3D Collider 与 Rigidbody；不混用 2D 物理系统。
- 核心玩法层不要依赖特定美术材质或后处理效果。
- 原型阶段使用灰盒和占位模型，关卡验证后再进行正式表现替换。
- 对可搬运道具限制数量、速度和复杂碰撞，避免物理同步成本失控。

---

## 5. 联机方案选择

## 5.1 推荐结论

MVP 推荐：

- **Photon Fusion 2**
- **Host Mode**
- 一名玩家同时充当主机和本地玩家
- 其他 1～3 名玩家作为客户端加入
- 主机持有共享世界的 State Authority
- 客户端提交输入或交互请求，不直接决定关键世界结果

Host Mode 足以支持 2～4 人、10～20 分钟的合作关卡，并且不需要支付和维护独立游戏服务器。

### 5.2 Photon Fusion 与 Unity Netcode 的取舍

| 对比项 | Photon Fusion 2 | Unity Netcode for GameObjects |
|---|---|---|
| 与 Unity 工作流 | 第三方 SDK，Unity 集成成熟 | Unity 官方 GameObject 联机方案，编辑器与 UGS 生态结合自然 |
| 主机权威 | Host Mode 直接支持 | Host 模式可实现 |
| 移动体验 | Host/Server 模式提供输入预测、回滚和重模拟模型 | 基础同步清晰，但复杂预测通常需要更多设计和实现工作 |
| 房间与连接 | Photon Cloud 房间体系直接可用，Host 模式支持 NAT 穿透与必要时的 Relay | 通常需要组合 Sessions/Lobby/Relay 等 Unity 服务或自行处理连接层 |
| 网络编程模型 | 概念较多，但权威、输入和网络 Tick 边界明确 | API 与 MonoBehaviour/GameObject 习惯接近，上手直接 |
| 可搬运物体与机关 | 集中状态权威适合本项目；仍需谨慎控制网络物理 | 可实现，但团队需要自行制定更多同步和预测约束 |
| 服务依赖 | 依赖 Photon 账号、AppId、云服务限额和定价 | 依赖 Unity 包；使用 Relay/Lobby 时依赖 UGS 限额和定价 |
| 厂商锁定 | 较高 | 较低，但使用 UGS 后仍有服务层绑定 |
| 独立服务器迁移 | Host Mode 代码可按 Fusion 的 Client/Server模型演进，但仍需验证具体实现 | 可演进到 Dedicated Server，但同样需要部署和适配 |
| 小团队 MVP 速度 | 较优，尤其适合需要预测的实时合作玩法 | 如果团队已有 NGO 经验，也可以成为合理选择 |

### 5.3 为什么 MVP 首选 Photon Fusion

本项目不是回合制或纯房间状态游戏，而是包含：

- 实时角色移动与跳跃
- 玩家共同搬运或争用道具
- 资源采集和制作
- 多人同时触发机关
- 物理与环境状态变化

Fusion Host Mode 的输入权威、状态权威、预测和重模拟模型更贴合这类实时合作体验。对 2～3 人团队而言，使用它可以减少自行搭建移动预测和连接服务的工作。

### 5.4 何时改选 Unity Netcode

满足以下任一条件时，可在项目初始化阶段改选 Netcode for GameObjects：

- 团队已经有成熟的 NGO、Unity Relay 和 Sessions/Lobby 经验。
- 希望尽可能减少第三方 SDK 依赖。
- 玩法原型确认不需要复杂移动预测或网络物理。
- 团队已有可复用的 NGO 房间、同步、测试和部署基础设施。

一旦完成第一条双人联网玩法闭环，就不再更换网络方案，除非出现无法规避的技术阻断。

### 5.5 不推荐 Shared Mode 作为默认方案

Fusion Shared Mode 对初学者更易接入，但状态权威分散在不同客户端。Wonder Squad 的关键玩法涉及共享资源、共同制作、争用道具和机关状态，集中权威更容易保证结果唯一、重连一致与防止重复操作。因此 MVP 默认采用 Host Mode。

### 5.6 网络抽象边界

核心玩法模块不应直接散布 Photon API。建议：

- `Network` 模块负责 Fusion 生命周期、生成、RPC、网络属性和对象映射。
- 玩法模块通过请求接口、权威服务和状态事件协作。
- 配置数据类不继承网络 SDK 类型。
- UI 不直接访问 `NetworkRunner` 或网络对象。
- 单机测试使用本地权威适配器，执行与主机相同的规则，而不是另一套简化逻辑。

---

## 6. 总体架构

建议采用“表现层、玩法域、关卡编排、基础设施”四层结构。

```text
UI / Presentation
        ↓
Player · Interaction · Inventory · Item · Crafting · Puzzle
        ↓
Level / Session Orchestration
        ↓
Network · Save · Unity/Photon Infrastructure
```

依赖规则：

- UI 可以读取玩法状态并发送用户意图，不直接修改玩法状态。
- 玩法模块之间通过小型接口、标识符和事件协作。
- Level 负责流程编排，不承载采集、制作或机关的底层规则。
- Network 传输意图和权威结果，不重新定义玩法规则。
- Save 只负责允许持久化的数据，不保存局内权威世界快照。
- ScriptableObject 主要用于静态定义；运行时状态使用独立状态对象。
- 禁止通过全局查找或场景对象名称建立核心依赖。

---

## 7. 客户端模块划分

## 7.1 Player

### 职责

- 读取本地玩家输入。
- 驱动移动、转向、跳跃和必要的移动状态。
- 管理角色表现状态，例如移动、持有、被弹飞和恢复。
- 提供角色位置、交互点、持有点和玩家标识。
- 在本地玩家上驱动摄像机目标。
- 将输入提交给 Network，由主机验证并模拟权威结果。

### 不负责

- 不直接决定资源是否采集成功。
- 不直接修改背包内容。
- 不直接判定机关完成。
- 不直接生成网络道具。

### 依赖

- 依赖 Interaction 发起交互。
- 依赖 Inventory 查询持有状态。
- 依赖 Network 提交输入并接收权威状态。
- 被 UI、Level 和 Puzzle 读取必要状态。

---

## 7.2 Interaction

### 职责

- 发现附近可交互目标。
- 根据距离、朝向、遮挡和状态筛选目标。
- 提供交互提示和可用性原因。
- 把玩家的交互意图转换为统一请求。
- 支持即时交互、持续交互、携带、放置和共同交互。
- 统一处理交互开始、取消、完成与反馈。

### 不负责

- 不实现具体采集、制作或机关规则。
- 不直接改变目标状态。

### 依赖

- 依赖 Player 的交互位置与玩家标识。
- 调用 Item、Crafting、Puzzle 或 Level 暴露的可交互接口。
- 通过 Network 发送需要权威判定的交互请求。
- 向 UI 提供提示状态。

---

## 7.3 Inventory

### 职责

- 表示玩家或团队当前拥有的资源与轻量道具记录。
- 提供容量、堆叠、查询、预留、增加和消耗规则。
- 支持制作材料的原子性检查与扣除。
- 发布库存变更事件供 UI 使用。

### MVP 建议

- 普通制作资源优先使用团队共享库存，降低争抢和协作摩擦。
- 大型关键道具使用场景实体和“持有关系”，不塞入抽象背包。
- 不实现装备槽、重量、耐久、稀有度和复杂物品网格。

### 依赖

- 依赖 Item 的物品标识与静态定义。
- 被 Crafting 查询和消耗。
- 通过 Network 接收权威变更。
- 被 UI 只读展示。
- 不依赖 Puzzle 的具体实现。

---

## 7.4 Item

### 职责

- 定义资源、普通物品、可制作道具和关键关卡道具。
- 管理场景物品的生成、拾取、持有、放置、使用和回收状态。
- 管理物品静态定义与运行时实例的分离。
- 提供关键物品掉出地图、持有者离开后的恢复策略。
- 向 Interaction 暴露可交互能力。

### 不负责

- 不自行决定制作结果。
- 不自行决定谜题是否完成。
- 不包含 UI 展示逻辑。

### 依赖

- 可依赖 Player 的持有挂点接口。
- 依赖 Network 同步实例、位置、持有者和状态。
- 被 Inventory、Crafting、Puzzle 和 Level 使用。

---

## 7.5 Crafting

### 职责

- 管理配方定义、材料需求和制作输出。
- 校验玩家或团队是否满足制作条件。
- 在主机上原子性消耗材料并创建制作结果。
- 处理共同提交材料、制作点限制和失败原因。
- 发布制作开始、成功和失败事件。

### MVP 限制

- 只实现服务于《沉睡森林》的少量固定配方。
- 不实现技能等级、品质随机、制作时间队列和复杂工作台升级。

### 依赖

- 依赖 Inventory 查询、预留和扣除材料。
- 依赖 Item 创建制作结果。
- 通过 Interaction 接收请求。
- 依赖 Network 执行权威请求与同步结果。
- 向 UI 提供配方和失败原因。

---

## 7.6 Puzzle

### 职责

- 定义机关组件、状态机、输入条件、成功条件和重置规则。
- 组合按钮、压力板、插槽、能量连接、搬运物和合作触发器。
- 接受玩家交互、物品放置、区域进入等标准化输入。
- 判定机关状态转换和完成结果。
- 支持多种解法映射到相同的谜题目标。
- 提供防卡死、超时重置和调试状态查看。

### 不负责

- 不直接控制整关开始、结束和结算。
- 不直接扣除抽象资源；需要时通过 Inventory 或 Item 接口请求。

### 依赖

- 依赖 Interaction、Item，必要时依赖 Inventory。
- 将完成事件发送给 Level。
- 依赖 Network 同步权威状态。
- 向 UI 提供目标或反馈数据，但不直接绘制 UI。

---

## 7.7 Level

### 职责

- 管理《沉睡森林》的关卡生命周期。
- 管理玩家进入、准备、开局、进行中、完成和退出状态。
- 编排教学、资源区域、古代遗迹、三个机关和传送门目标。
- 管理检查点、跌落恢复点和关键道具恢复点。
- 汇总谜题完成情况并判定关卡完成。
- 记录局内时长与用于测试的匿名事件。
- 控制场景加载和关卡级对象注册。

### 不负责

- 不包含具体移动、库存、配方或机关内部规则。
- 不保存长期账号数据。

### 依赖

- 读取 Player 会话状态。
- 监听 Puzzle 完成事件。
- 通过 Item 管理关键关卡物品恢复。
- 依赖 Network 同步关卡阶段和胜利状态。
- 向 UI 提供当前目标、时间和结算信息。

---

## 7.8 Network

### 职责

- 封装 Photon Fusion。
- 管理创建房间、加入房间、离开和关机流程。
- 管理玩家生成、销毁、网络标识和主机权威。
- 收集本地输入并提交给主机。
- 传输交互、采集、制作、放置和机关操作请求。
- 复制玩家、道具、资源、机关和关卡的权威状态。
- 处理重复请求、迟到请求、玩家离开和关键物品释放。
- 提供网络时间、Tick 和连接状态。
- 为其他模块提供 SDK 无关的会话与权威接口。

### 不负责

- 不在 RPC 中复制完整玩法规则。
- 不持有 UI 页面逻辑。
- 不承担长期账号、排行或商业化功能。

### 依赖

- 依赖 Photon Fusion SDK 及其连接、房间和传输能力。
- 连接所有需要共享状态的玩法模块。
- 被 UI 读取连接状态，但 UI 不能直接调用具体 SDK。

---

## 7.9 UI

### 职责

- 启动、创建房间、加入房间、准备和加载反馈。
- 显示交互提示、库存摘要、配方、当前目标和结果。
- 显示连接中、断线、主机离开和错误信息。
- 提供设置、暂停和退出入口。
- 保持提示简洁，不替玩家直接解答谜题。

### 不负责

- 不直接修改库存、机关、关卡和网络状态。
- 不包含玩法判定逻辑。

### 依赖

- 以只读 ViewModel 或查询接口读取各模块状态。
- 通过命令接口提交用户意图。
- 使用 Save 读取和保存本地设置。

---

## 7.10 Save

### 职责

- 保存本地设置、键位、音量、画面和辅助功能选项。
- 保存少量非竞争性本地标记，例如是否看过操作提示。
- 对存档版本进行标记，并提供默认值和损坏恢复。
- 在开发构建中保存可选的本地测试日志。

### MVP 限制

- 不实现云存档。
- 不保存局内共享世界状态。
- 不保存权威库存、机关完成状态或多人关卡进度。
- 不实现账号系统。

### 依赖

- 可被 UI、Player 输入设置和本地表现系统使用。
- 不依赖 Network 才能读取设置。
- 不被 Puzzle、Crafting 用作权威数据源。

---

## 8. 模块依赖关系

### 8.1 依赖矩阵

| 模块 | 主要依赖 | 主要被依赖 |
|---|---|---|
| Player | Interaction、Inventory、Network | Interaction、Item、Level、UI |
| Interaction | Player、Network、可交互接口 | Player、Item、Crafting、Puzzle、UI |
| Inventory | Item、Network | Crafting、Puzzle、UI |
| Item | Player 持有接口、Network | Interaction、Inventory、Crafting、Puzzle、Level |
| Crafting | Inventory、Item、Network | Interaction、UI、Level |
| Puzzle | Interaction、Item、可选 Inventory、Network | Level、UI |
| Level | Player、Item、Puzzle、Network | UI |
| Network | Fusion、各模块的网络适配器 | 所有共享状态模块 |
| UI | 各模块只读接口、Save | 无核心玩法模块 |
| Save | 本地文件系统 | UI、本地设置消费者 |

### 8.2 防止循环依赖

- `Item` 不直接引用某个具体 `Puzzle`；通过通用输入接口传递物品标识。
- `Puzzle` 完成后发布事件，由 `Level` 订阅。
- `Level` 不直接改写 `Puzzle` 内部字段，只调用公开的重置或激活接口。
- `Inventory` 不直接生成场景物体；由 `Crafting` 协调 `Item` 服务。
- `Network` 使用适配器调用玩法服务，不让玩法程序集直接到处引用 Fusion 类型。
- `UI` 只订阅状态与调用命令，不反向成为玩法模块依赖。

建议用 Assembly Definition 固化上述边界。

---

## 9. 网络权威规则

## 9.1 基本原则

客户端表达“意图”，主机决定“结果”。

客户端可以立即表现低风险的本地反馈，例如按钮按下动画、交互进度预览或预测移动；但最终共享状态必须以主机结果为准。

### 9.2 必须由主机或服务器决定的逻辑

#### 玩家与会话

- 玩家是否允许加入房间。
- 玩家生成点和有效玩家标识。
- 开局条件、关卡阶段、关卡结束和返回流程。
- 玩家复活位置及防止越界的最终校正。
- 主机退出后的会话终止策略；MVP 不强制实现主机迁移。

#### 移动与物理

- 玩家位置与速度的最终权威状态。
- 跳跃、击飞、跌落和恢复是否合法。
- 可影响谜题结果的碰撞与区域进入结果。
- 网络物理道具的最终位置、速度和睡眠状态。

本地玩家可以进行输入预测，但不能自行宣布到达目标区或触发成功。

#### 资源和库存

- 资源节点是否仍可采集。
- 哪个请求先成功。
- 资源数量增加、扣除与团队库存结果。
- 掉落物生成和销毁。
- 防止重复采集和重复拾取。

#### 道具

- 道具实例的生成、所有者、持有者和销毁。
- 玩家是否能拾取、放置或使用道具。
- 两名玩家争用同一物体时的唯一结果。
- 关键道具掉出地图或持有者离开后的恢复。

#### 制作

- 配方是否有效。
- 材料是否充足。
- 材料预留和扣除。
- 制作成功、失败和输出物生成。
- 同时制作请求的顺序与幂等处理。

#### 机关和关卡

- 机关输入是否合法。
- 机关状态机转换、重置和完成状态。
- 同时触发条件是否满足。
- 多种解法是否达到同一个目标条件。
- 三个机关的完成汇总。
- 传送门修复和最终通关。

### 9.3 可以只在本地决定的逻辑

- 本地摄像机位置、阻尼、震动与取景。
- 本地输入设备读取和键位映射。
- UI 页面开关、焦点、提示排版。
- 音量、画质、语言和辅助功能。
- 不影响玩法结果的粒子、屏幕特效和装饰动画。
- 本地预测与插值表现；最终状态仍接受主机校正。
- 是否展示已看过的教学提示。

### 9.4 RPC 与状态同步使用原则

- RPC 用于短暂意图或事件，例如“请求交互”。
- Networked Property 用于需要迟加入者看到的持续状态，例如机关阶段。
- 每个会改变共享状态的请求带有玩家标识、目标标识和请求序号。
- 主机进行距离、状态、权限和资源校验。
- 请求处理必须幂等，重复包不能重复发放资源或制作物品。
- 不依赖 RPC 到达顺序表达复杂状态机。
- 不按帧同步无需变化的数据。

---

## 10. 数据保存策略

## 10.1 只在本地保存的数据

建议保存：

- 主音量、音乐音量、音效音量。
- 分辨率、全屏、画质和帧率选项。
- 键盘、鼠标或手柄绑定。
- 镜头灵敏度、反转设置和震动开关。
- 字幕、色觉、文字大小等辅助设置。
- 语言。
- 是否看过基础操作提示。
- 最近使用的房间显示名或玩家昵称；不得视为可信身份。
- 本地存档格式版本。
- 开发阶段可选的匿名测试日志。

这些数据使用 JSON 或等价的轻量格式即可。敏感信息和服务密钥不得写入本地明文存档。

### 10.2 不允许以本地存档作为权威的数据

- 当前房间成员。
- 局内玩家位置。
- 团队资源和库存。
- 场景道具实例。
- 制作结果。
- 机关状态。
- 关卡完成状态。
- 可影响其他玩家结果的奖励。

MVP 中这些数据只存在于当前网络会话的主机权威状态中，会话结束后释放。

### 10.3 可选的本地测试记录

为验证 MVP，可在玩家同意的开发测试中记录：

- 单局开始与结束时间。
- 玩家数量。
- 每个机关开始、重置和完成时间。
- 使用的解法标识。
- 卡死恢复与断线事件。

记录只用于测试分析，不应包含聊天内容、真实身份或不必要的个人信息。

---

## 11. 第一版是否需要独立后端

### 11.1 结论

**第一版不需要自建独立后端，也不需要独立游戏服务器。**

MVP 使用：

- Photon Cloud 处理连接与房间服务。
- 一名玩家的 Unity 客户端作为 Fusion Host，运行权威游戏模拟。
- 本地文件保存个人设置。

Photon Cloud 属于第三方联机基础设施，但不是本项目需要开发和维护的业务后端。

### 11.2 暂不建设的后端能力

- 账号注册与登录。
- 用户数据库。
- 云存档。
- 排行榜。
- 商城和支付。
- 公会。
- 匹配评级。
- 复杂数据统计后台。
- 专用服务器编排。

### 11.3 何时再引入后端

只有验证核心玩法好玩，并出现以下明确需求后再设计：

- 跨设备存档或长期基地。
- 正式账号、好友和邀请体系。
- 可信奖励与经济系统。
- 公共匹配、举报和封禁。
- 服务端数据分析。
- 主机作弊成为真实问题。
- 需要主机迁移或持续世界。
- 正式运营要求高可用专用服务器。

后端引入时，应保持游戏会话服务、账号服务、存档服务和分析服务边界清晰，不把它们塞入 Unity 客户端模块。

---

## 12. 建议项目目录结构

```text
Assets/
└── WonderSquad/
    ├── Art/
    │   ├── Characters/
    │   ├── Environment/
    │   ├── Items/
    │   ├── Materials/
    │   ├── Animations/
    │   ├── VFX/
    │   └── UI/
    ├── Audio/
    │   ├── Music/
    │   └── SFX/
    ├── Config/
    │   ├── Player/
    │   ├── Items/
    │   ├── Recipes/
    │   ├── Puzzles/
    │   └── Levels/
    ├── Prefabs/
    │   ├── Player/
    │   ├── Items/
    │   ├── Interaction/
    │   ├── Puzzles/
    │   ├── Network/
    │   ├── UI/
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
    │   │   └── Utilities/
    │   ├── Player/
    │   │   ├── Runtime/
    │   │   ├── Presentation/
    │   │   └── Tests/
    │   ├── Interaction/
    │   ├── Inventory/
    │   ├── Item/
    │   ├── Crafting/
    │   ├── Puzzle/
    │   ├── Level/
    │   ├── Network/
    │   │   ├── Abstractions/
    │   │   ├── Fusion/
    │   │   ├── Replication/
    │   │   └── Tests/
    │   ├── UI/
    │   └── Save/
    ├── Settings/
    │   ├── Input/
    │   ├── URP/
    │   └── Physics/
    └── Tests/
        ├── EditMode/
        ├── PlayMode/
        └── Network/
Packages/
ProjectSettings/
Design/
```

### 12.1 目录规则

- 所有本项目资源放在 `Assets/WonderSquad`，第三方资源单独放置，不混入业务目录。
- 每个核心模块建立 Assembly Definition。
- `Config` 保存 ScriptableObject 静态定义，不保存运行时状态。
- 关卡专属内容放在 `Scenes/Levels/SleepingForest` 对应目录，不污染通用模块。
- 网络适配实现放在 `Network/Fusion`，玩法模块只引用 `Network/Abstractions`。
- 测试与模块尽量就近放置，跨模块集成测试集中到顶层 `Tests`。
- 不建立 `Managers` 或 `Misc` 作为无边界收纳目录。

---

## 13. 关键标识与数据结构草案

以下内容是接口形状和数据边界示例，不是完整实现。

### 13.1 强类型标识

```csharp
public readonly record struct PlayerId(ulong Value);
public readonly record struct ItemDefinitionId(string Value);
public readonly record struct ItemInstanceId(ulong Value);
public readonly record struct RecipeId(string Value);
public readonly record struct PuzzleId(string Value);
public readonly record struct InteractableId(ulong Value);
public readonly record struct RequestId(uint Value);
```

目的：

- 避免把玩家、物品、配方和机关标识混用。
- 网络层负责把业务标识映射为 Fusion 网络对象或玩家引用。
- 配置标识在构建后保持稳定，不使用场景对象名称作为标识。

### 13.2 交互

```csharp
public enum InteractionKind
{
    Use,
    Collect,
    PickUp,
    Drop,
    Place,
    Craft,
    Activate
}

public readonly record struct InteractionContext(
    PlayerId PlayerId,
    InteractableId TargetId,
    InteractionKind Kind,
    RequestId RequestId);

public readonly record struct InteractionResult(
    bool Accepted,
    InteractionFailureReason FailureReason);

public interface IInteractable
{
    InteractionAvailability Query(in InteractionContext context);
}

public interface IInteractionAuthority
{
    InteractionResult TryExecute(in InteractionContext context);
}
```

约束：

- `Query` 可用于本地提示，但不代表最终成功。
- `TryExecute` 只在主机权威上下文执行。
- 具体对象不能因为客户端播放了交互动画就提前改变共享状态。

### 13.3 物品与库存

```csharp
public enum ItemCategory
{
    Resource,
    Tool,
    PuzzlePart,
    Quest
}

public sealed class ItemDefinition
{
    public ItemDefinitionId Id;
    public ItemCategory Category;
    public int MaxStack;
    public bool IsCarryableWorldObject;
}

public readonly record struct ItemStack(
    ItemDefinitionId ItemId,
    int Quantity);

public readonly record struct InventoryChange(
    ItemDefinitionId ItemId,
    int PreviousQuantity,
    int CurrentQuantity);

public interface IReadOnlyInventory
{
    int GetQuantity(ItemDefinitionId itemId);
    bool Contains(ItemDefinitionId itemId, int quantity);
}

public interface IInventoryAuthority : IReadOnlyInventory
{
    bool TryAdd(ItemStack stack);
    bool TryConsume(IReadOnlyList<ItemStack> costs);
}
```

约束：

- 运行时库存修改只发生在主机。
- `TryConsume` 必须一次完成全部材料校验和扣除，不能扣一半后失败。
- 世界中的大型物体使用 `ItemInstanceId`，不只用数量表示。

### 13.4 制作

```csharp
public sealed class RecipeDefinition
{
    public RecipeId Id;
    public IReadOnlyList<ItemStack> Ingredients;
    public ItemStack Output;
    public string RequiredStationTag;
}

public readonly record struct CraftRequest(
    RequestId RequestId,
    PlayerId PlayerId,
    RecipeId RecipeId,
    InteractableId StationId);

public readonly record struct CraftResult(
    bool Succeeded,
    CraftFailureReason FailureReason,
    ItemInstanceId? SpawnedItemId);

public interface ICraftingAuthority
{
    CraftResult TryCraft(in CraftRequest request);
}
```

约束：

- 制作请求在主机上校验距离、工作点状态和材料。
- 请求按 `RequestId` 去重。
- 制作输出由主机生成并同步。

### 13.5 场景道具

```csharp
public enum WorldItemState
{
    Available,
    Held,
    Placed,
    Consumed,
    Recovering
}

public readonly record struct WorldItemSnapshot(
    ItemInstanceId InstanceId,
    ItemDefinitionId DefinitionId,
    WorldItemState State,
    PlayerId? HolderId);

public interface IWorldItemAuthority
{
    bool TryPickUp(PlayerId playerId, ItemInstanceId itemId);
    bool TryDrop(PlayerId playerId, ItemInstanceId itemId);
    bool TryPlace(
        PlayerId playerId,
        ItemInstanceId itemId,
        InteractableId socketId);
}
```

### 13.6 机关

```csharp
public enum PuzzlePhase
{
    Inactive,
    Available,
    InProgress,
    Solved,
    Resetting
}

public readonly record struct PuzzleInput(
    PuzzleId PuzzleId,
    PlayerId PlayerId,
    PuzzleInputKind Kind,
    InteractableId SourceId,
    ItemInstanceId? ItemId);

public readonly record struct PuzzleSnapshot(
    PuzzleId PuzzleId,
    PuzzlePhase Phase,
    int ProgressStep,
    uint Revision);

public interface IPuzzleAuthority
{
    bool TryApply(in PuzzleInput input);
    bool TryReset(PuzzleId puzzleId);
    PuzzleSnapshot GetSnapshot(PuzzleId puzzleId);
}

public interface IPuzzleEvents
{
    event Action<PuzzleSnapshot> StateChanged;
    event Action<PuzzleId, PuzzleSolutionId> Solved;
}
```

约束：

- `PuzzleSolutionId` 记录玩家采用的解法，用于验证“创造解决方案”。
- 机关只同步有限状态，不同步任意组件内部字段。
- `Revision` 用于忽略过期表现更新和辅助调试。

### 13.7 关卡会话

```csharp
public enum LevelPhase
{
    Loading,
    WaitingForPlayers,
    Playing,
    Completed,
    Ending
}

public readonly record struct LevelSessionState(
    LevelPhase Phase,
    int ConnectedPlayerCount,
    double StartNetworkTime,
    int SolvedPuzzleCount);

public interface ILevelAuthority
{
    bool CanStart();
    void StartLevel();
    void NotifyPuzzleSolved(PuzzleId puzzleId);
    void CompleteLevel();
}

public interface IReadOnlyLevelSession
{
    LevelSessionState Current { get; }
}
```

### 13.8 网络会话抽象

```csharp
public enum SessionRole
{
    None,
    Host,
    Client
}

public readonly record struct SessionJoinRequest(
    string RoomCode,
    string DisplayName);

public readonly record struct SessionStatus(
    bool IsConnected,
    SessionRole Role,
    int PlayerCount,
    string RoomCode);

public interface INetworkSession
{
    SessionStatus Status { get; }
    Task CreateAsync(string displayName, CancellationToken token);
    Task JoinAsync(SessionJoinRequest request, CancellationToken token);
    Task LeaveAsync(CancellationToken token);
}

public interface IAuthorityContext
{
    bool IsStateAuthority { get; }
    double NetworkTime { get; }
}

public interface IGameplayRequestSender
{
    void SendInteraction(in InteractionContext request);
    void SendCraft(in CraftRequest request);
    void SendPuzzleInput(in PuzzleInput request);
}
```

约束：

- 上层模块不知道 `NetworkRunner`、`PlayerRef` 或 Photon RPC 的具体形状。
- Fusion 适配器负责序列化、对象查找、请求去重和回传结果。

### 13.9 本地存档

```csharp
public sealed class LocalSettingsData
{
    public int SchemaVersion;
    public float MasterVolume;
    public float MusicVolume;
    public float SfxVolume;
    public float CameraSensitivity;
    public bool CameraShakeEnabled;
    public string LanguageCode;
    public bool BasicTutorialSeen;
}

public interface ILocalSaveService
{
    LocalSettingsData LoadSettings();
    void SaveSettings(LocalSettingsData settings);
    void ResetSettings();
}
```

约束：

- 存档读取失败时回退到默认值。
- 保存采用临时文件替换或等价的安全写入方式。
- 不在此结构中加入团队库存、机关或关卡权威数据。

---

## 14. 场景与启动流程

### 14.1 场景建议

- `Bootstrap`：初始化配置、Save、网络适配器和必要的全局服务。
- `Menu`：创建房间、加入房间、设置和错误反馈。
- `SleepingForest`：完整 MVP 关卡。
- `Test_*`：移动、采集、制作、机关和网络专项测试场景。

### 14.2 启动流程

```text
启动应用
→ 加载 Bootstrap
→ 读取本地设置
→ 进入 Menu
→ 创建或加入 Photon 房间
→ 主机加载并同步 SleepingForest
→ 生成 2～4 名玩家
→ 主机启动关卡
→ 完成三个机关并修复传送门
→ 主机宣布完成
→ 展示本局结果
→ 离开或重新开始
```

### 14.3 生命周期规则

- 全局服务数量保持最少。
- 关卡对象随关卡卸载，不常驻到菜单。
- 网络对象只由主机或 Fusion 正确的生成流程创建。
- 退出房间时取消未完成请求、释放本地引用并恢复 UI。
- 不依赖 Script Execution Order 修补不清晰的初始化关系。

---

## 15. 异常与恢复策略

MVP 至少定义以下行为：

| 异常 | MVP 处理 |
|---|---|
| 玩家掉出地图 | 主机传送到最近检查点，播放趣味反馈 |
| 关键道具掉出地图 | 主机回收到指定恢复点 |
| 持有关键道具的玩家离开 | 主机解除持有并在安全位置生成或放置 |
| 客户端短时断线 | 显示重连状态；超时后返回菜单 |
| 主机退出 | MVP 可结束本局并向客户端明确提示，不强制主机迁移 |
| 重复采集或制作请求 | 按 RequestId 和当前状态拒绝重复执行 |
| 机关进入非法组合状态 | 主机执行局部重置，不要求重开整关 |
| 本地设置损坏 | 备份或忽略损坏文件，恢复默认设置 |

恢复机制应服务于“欢乐失败、快速重试”，避免死亡惩罚和长时间回退。

---

## 16. 测试架构要求

### 16.1 单元与编辑器测试

- 配方材料校验和原子扣除。
- 库存容量与堆叠。
- 机关状态机合法转换。
- 请求去重。
- 存档版本与损坏恢复。

### 16.2 Play Mode 测试

- 移动与交互范围。
- 拾取、放置与关键道具恢复。
- 制作完整流程。
- 三个机关独立完成和重置。
- 关卡开始、完成和重新进入。

### 16.3 联机测试

- Host + 1、2、3 个客户端。
- 两名玩家同时采集同一资源。
- 两名玩家同时拾取同一道具。
- 制作请求重复或延迟到达。
- 同时触发机关。
- 玩家持有关键道具时离开。
- 延迟、抖动和丢包模拟。
- 完整 10～20 分钟通关回归。

所有核心状态应能在开发构建中查看其权威拥有者、网络标识和当前状态，降低小团队排错成本。

---

## 17. MVP 明确不采用的架构

- 不采用微服务。
- 不采用自建账号和认证服务。
- 不采用独立专用服务器。
- 不采用 ECS/DOTS 作为核心玩法基础。
- 不建设通用开放世界流式加载框架。
- 不建设复杂热更新系统。
- 不建设通用技能、战斗、装备或属性框架。
- 不建设商城、支付、排行榜和公会相关模块。
- 不为尚未确定的多地图和大量角色建立插件化内容平台。
- 不保存完整多人世界快照。

这些决定用于保护 MVP 的验证速度，不代表正式产品永远不能采用相关技术。

---

## 18. 技术决策检查点

### 检查点一：项目初始化结束

- Unity 6.3 LTS 具体补丁已锁定。
- Universal 3D + URP 已建立。
- Fusion 2 Host Mode 能建立最小双人连接。

### 检查点二：最小玩法闭环

- 一种资源、一个配方、一个道具和一个机关在单机权威模式下完成。
- 模块没有直接依赖具体 UI 或场景对象名称。

### 检查点三：双人联网闭环

- 相同玩法链在 Host + Client 下完成。
- 资源、库存、制作、道具和机关均由主机决定。
- 客户端不会通过重复请求产生重复结果。

### 检查点四：完整 MVP

- 2～4 人完成《沉睡森林》。
- 单局 10～20 分钟。
- 三个机关和至少一个多解法问题通过联机验证。
- 没有因为架构扩张加入延期清单中的系统。

---

## 19. 官方资料

- Unity 6 发布与 LTS 信息：<https://unity.com/releases/unity-6>
- Unity 6 支持策略：<https://unity.com/releases/unity-6/support>
- Netcode for GameObjects 包文档：<https://docs.unity3d.com/Manual/com.unity.netcode.gameobjects.html>
- Photon Fusion 网络拓扑：<https://doc.photonengine.com/fusion/current/manual/network-topologies>
- Photon Fusion 玩家输入与预测：<https://doc.photonengine.com/fusion/current/manual/input/player-input>
- Photon Fusion Host/Shared 示例说明：<https://doc.photonengine.com/fusion/current/game-samples/fusion-starter>

---

## 20. 最终决策

Wonder Squad MVP 采用：

> Unity 6.3 LTS + Universal 3D/URP + 2.5D 表现 + Photon Fusion 2 Host Mode + 主机集中权威 + 本地轻量设置存档 + 无自建后端。

该组合优先保障 2～4 人实时合作中的状态一致、快速迭代和小团队可执行性。后续只有在 MVP 证明核心玩法好玩之后，才评估独立服务器、正式账号、云存档和长期运营架构。
