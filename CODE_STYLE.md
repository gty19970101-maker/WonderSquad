# Wonder Squad C# 编码规范

## 1. 基本原则

- 可读性、可测试性和边界清晰优先于技巧性。
- 一个类型只承担一个明确职责。
- Gameplay 规则不依赖 UI、Editor、具体网络 SDK 或场景对象名称。
- 可调参数进入 ScriptableObject、配置资产或明确常量，不散落在业务代码中。
- 公共契约保持最小；默认使用 `private`，确有跨程序集需求才扩大可见性。
- 新增代码必须在 Unity `6000.3.21f1` 编译，并通过相关测试。

## 2. 命名规范

| 对象 | 规则 | 示例 |
|---|---|---|
| Namespace | PascalCase，按 `WonderSquad.<Domain>[.<Feature>]` | `WonderSquad.Player.Movement` |
| Class | PascalCase，使用名词或职责名 | `PlayerController` |
| Interface | `I` + PascalCase | `IPlayerQuery` |
| Enum | PascalCase，使用单数名词 | `PlayerMovementState` |
| Enum 成员 | PascalCase | `Grounded` |
| ScriptableObject | PascalCase，以 `Definition`、`Settings` 或 `Catalog` 结尾 | `MovementSettings` |
| MonoBehaviour | PascalCase，以职责或 Unity 角色命名 | `CameraFollow` |
| 方法 | PascalCase，以动词开头 | `ApplyMovement` |
| Property | PascalCase | `IsGrounded` |
| Event | PascalCase，使用已发生语义 | `PlayerSpawned` |
| Event 处理方法 | `On` + 事件名 | `OnPlayerSpawned` |
| private 字段 | camelCase | `movementSettings` |
| 参数与局部变量 | camelCase | `targetPosition` |
| 常量 | PascalCase | `ContentSchemaVersion` |
| 文件夹 | PascalCase，按领域和职责命名 | `Runtime/Player/Movement` |
| 测试 | `<Type>Tests`，方法表达条件和结果 | `Move_WhenGrounded_ChangesPosition` |

文件名必须与主要类型名一致。缩写按普通单词处理，例如 `PlayerId`，不写 `PlayerID`。

## 3. Namespace

- 根命名空间固定为 `WonderSquad`。
- Namespace 与 Assembly Definition 的领域一致。
- 不使用无含义的 `Common`、`Helpers`、`Managers`、`Misc`。
- 通用契约放入 Core/Contracts；仅一个领域使用的类型保留在该领域。
- 不通过 Namespace 掩盖错误的程序集依赖。

## 4. Class 与 Interface

- Class 默认 `sealed`；确有继承设计和测试理由时才开放。
- 优先组合，不为“未来可能扩展”建立空抽象层。
- Interface 用于跨程序集端口、可替换基础设施或必要测试边界。
- 不为只有一个局部调用者的简单类机械创建 Interface。
- 公共 Interface 不暴露具体 UI、Editor、Photon 或场景组件类型。
- 构造函数或显式初始化方法必须使依赖清晰；禁止隐式访问全局万能 Manager。

## 5. Enum

- 使用单数 PascalCase 类型名。
- 第一个成员应显式赋值；需要安全默认时使用 `Unknown = 0` 或明确的默认状态。
- 网络或持久化 Enum 的数值不得随意重排。
- 不使用字符串替代稳定、有限的状态集合。

## 6. ScriptableObject

- 只保存静态定义、版本化配置和可调参数。
- 不保存局内权威运行时状态、玩家位置、机关进度或网络会话状态。
- 资产必须具有稳定 ID 或明确用途，并由校验器覆盖。
- 字段使用 `[SerializeField] private`，对外提供只读 Property。
- `OnValidate` 只能维护资产内部合法性，不能执行跨资产业务事务。
- 不把 ScriptableObject 扩展成任意脚本语言或万能配置容器。

## 7. MonoBehaviour

- 只负责 Unity 生命周期、场景引用和表现适配。
- 复杂规则应下沉到可测试的普通 C# 类型。
- `Awake` 建立自身引用，`OnEnable` 订阅，`OnDisable` 取消订阅。
- 禁止在 `Update`、`FixedUpdate` 或 `LateUpdate` 中反复执行全场景查找。
- 需要物理模拟时明确选择 CharacterController、Rigidbody 或其他方案，不混用冲突的运动所有权。
- Inspector 引用缺失时必须给出明确错误并安全停止，不允许持续抛异常。

## 8. 变量与序列化

```csharp
[SerializeField]
private MovementSettings movementSettings;

public MovementSettings MovementSettings => movementSettings;
```

- private 字段使用 camelCase，不使用 `m_` 前缀。
- 不公开可变字段。
- 对集合优先暴露只读接口。
- 布尔变量使用 `is`、`has`、`can`、`should` 等前缀。
- 单位必须体现在名称或 Tooltip 中，例如 `durationSeconds`。
- Unity Object 引用使用 Unity 的空值语义检查。

## 9. 方法

- 方法名以动词开头，并只完成一个层级的工作。
- 参数顺序保持稳定，避免大量布尔参数。
- 超过三个互相关联的可调参数时，优先使用配置对象或明确值对象。
- 命令方法表达动作，查询方法不得产生隐藏副作用。
- 不在属性 Getter 中执行场景查找、文件 IO 或网络请求。

## 10. Event

- Event 名称描述已经发生的事实，例如 `InventoryChanged`。
- 请求或命令不伪装成 Event，应使用 `Request`、`Command` 或端口方法。
- 发布者不假设订阅者存在。
- MonoBehaviour 必须成对订阅和取消订阅。
- 跨网络只传输稳定语义和必要数据，不传输本地化文本。

## 11. Folder 与 Assembly Definition

- 资源统一位于 `Assets/WonderSquad`。
- Runtime、Editor、Tests 必须隔离。
- Editor API 不得出现在 Runtime 程序集。
- 测试代码不得进入正常 Player Build。
- 每个领域程序集只引用技术架构依赖矩阵允许的程序集。
- UI 不得成为 Gameplay 领域的依赖。
- Fusion 类型只允许出现在未来的 Network/Fusion 适配层。

## 12. 注释规范

- 注释解释“为什么”和约束，不复述代码。
- 公共契约、非直观状态转换和权威规则使用 XML Documentation。
- Inspector 字段优先使用清晰命名和 `[Tooltip]`。
- TODO 必须包含原因和后续任务，例如：

```csharp
// TODO(Sprint007): Replace the offline authority adapter with Fusion host authority.
```

- 禁止保留大段注释掉的旧代码；历史由 Git 保存。
- 禁止使用注释掩盖错误职责或临时架构穿透。

## 13. Region 使用规范

- 默认不使用 `#region`。
- 仅允许用于生成代码、很长的 Editor Inspector 分区或平台桥接代码。
- 禁止嵌套 Region。
- 如果需要 Region 才能理解一个普通业务类，应拆分类。

## 14. 禁止 Magic Number

- 速度、距离、时长、冷却、阈值、资源成本、摄像机参数等必须进入 Settings、Definition 或命名常量。
- 数学恒等值和显而易见的集合索引可以直接使用。
- 单次出现不代表可以硬编码；判断标准是“设计或调试时是否可能调整”。

## 15. 禁止 God Class

出现以下信号必须拆分：

- 同时管理三个以上领域。
- 同时处理输入、规则、保存、UI 和网络。
- 大量无关序列化字段。
- 修改一个功能经常破坏不相关功能。
- 类型无法在隔离测试中实例化。

Bootstrap 只组合基础设施，不实现 Gameplay 规则；Level 只编排，不实现具体能力或机关内部规则。

## 16. 禁止循环依赖

- 程序集依赖必须是有向无环图。
- 领域协作通过 Core/Contracts 中的命令、查询、事件或端口。
- 禁止用静态单例、反射或场景查找规避程序集边界。
- 新增 `.asmdef` 引用必须在 Review 中说明。

## 17. 禁止跨层访问

允许方向：

```text
Presentation
→ Gameplay Domains
→ Orchestration and Content
→ Infrastructure contracts
```

禁止：

- Gameplay 直接操作 UI Widget。
- Content 引用运行时网络对象。
- Puzzle 直接写 Player 或 Ability 内部状态。
- Player 直接决定机关、库存、失败恢复或关卡完成结果。
- Editor 类型进入 Runtime。
- Network 复制另一套 Gameplay 规则。

需要跨层协作时，先定义最小端口或只读状态，再由外层适配。
