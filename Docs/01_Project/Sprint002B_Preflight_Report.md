# Sprint002B Interaction Prompt Preflight Report

## 1. Preflight 结论

- 检查日期：2026-08-02
- Unity 基线：`6000.3.21f1`
- 当前分支：`feature/sprint002a-interaction-detection`
- 当前提交：`f6e017f`
- 工作区：检查开始时干净
- Sprint002A：`PASSED`，Gate Review 为 `GO`
- Sprint002B 目标：检测到有效目标后展示本地交互提示，不执行交互
- 最终结论：**CONDITIONAL GO**

现有 Interaction Detection、Input Actions、UI 程序集和 PlayerSandbox 足以承载 Sprint002B，不需要重构 Sprint002A。开始实现前必须先把 Sprint002A 合并到最新 `develop`，再创建 Sprint002B 专用分支；同时确认并直接锁定本阶段实际使用的 Unity UI 文本依赖。关闭第 12 节条件后可以实施。

本次只完成静态预检和设计决策，没有修改 Unity 代码、Prefab、Scene、Input Actions 或包配置。

## 2. 当前基础检查

### 2.1 已具备

- `WonderSquad.Interaction` 已提供只读 `InteractionDetector.CurrentTarget`。
- `IInteractable` 保持最小只读契约，不含 `Interact()`。
- Detector 已正确处理无目标、目标离开、目标销毁、Layer、距离和遮挡。
- `WonderSquad.UI.asmdef` 已存在，目前只引用 `WonderSquad.Core`，尚无 Runtime 代码。
- PlayerSandbox 已有空的屏幕空间 `DebugCanvas`，但当前没有 Prompt View。
- `WonderSquad.inputactions` 已存在 `Gameplay/Interact`：
  - 键鼠：`<Keyboard>/e`
  - 手柄：`<Gamepad>/buttonWest`
- `InputActionNames.Interact` 已存在，无需新增 Action 名称。
- `com.unity.ugui` `2.0.0` 已在 `packages-lock.json` 中解析，但当前是传递依赖。
- 项目未安装 Unity Localization 包，也没有现有本地化 Runtime。
- 全仓库没有重复的 Interaction Prompt、Prompt Presenter 或 Prompt Definition 实现。

### 2.2 当前差距

- Detector 没有目标变化通知，UI 若直接轮询会产生不必要的重复刷新。
- 目标没有独立的 Prompt 语义来源。
- UI 没有 Prompt View、Presenter 或本地玩家绑定组件。
- 当前没有完整 Localization 服务。
- Input System 中没有 Touch 控制方案或移动端 Interact 绑定。
- PlayerSandbox 的 `DebugCanvas` 为空，且 RectTransform 当前缩放为零；不能在不验证布局的情况下直接视为可用 Prompt Canvas。

## 3. 十二项设计检查

### 3.1 Prompt UI 应属于哪个程序集

**结论：属于现有 `WonderSquad.UI` 程序集。**

理由：

- Prompt 是本地表现状态，不是 Interaction 权威状态。
- UI 负责布局、文字、图标、设备提示和可见性。
- Interaction 只提供目标与只读 Prompt 语义，不操作 Canvas、TMP 或其他 Widget。
- 不新建额外的 Prompt 专用程序集，避免为一个窄切片过度拆分。

建议命名空间：

- `WonderSquad.Interaction.Prompt`：只读 Prompt 数据、Definition 和目标侧 Source。
- `WonderSquad.UI.Interaction`：View、Presenter、Binder 和按键显示解析。

### 3.2 Interaction 与 UI 的依赖方向

**结论：只允许 `UI → Interaction`。**

实施后的允许方向：

```text
WonderSquad.Player      → WonderSquad.Core
WonderSquad.Interaction → WonderSquad.Core
WonderSquad.UI          → WonderSquad.Core
WonderSquad.UI          → WonderSquad.Interaction
WonderSquad.UI          → WonderSquad.Player
WonderSquad.UI          → Unity.InputSystem
WonderSquad.UI          → Unity UI Text Assembly
```

`WonderSquad.UI → WonderSquad.Player` 仅用于 PlayerSandbox 的明确本地玩家绑定：Presenter/Binder 订阅 `PlayerSpawner.PlayerSpawned`，再读取该 Player 上的 Detector。UI 不修改 Player 状态。

禁止：

- `WonderSquad.Interaction → WonderSquad.UI`
- `WonderSquad.Player → WonderSquad.UI`
- `WonderSquad.Player → WonderSquad.Interaction`
- 用静态单例、反射、对象名称或全局事件总线绕过依赖边界

### 3.3 是否需要只读 Prompt 数据模型

**结论：需要。**

建议使用不可变的 `InteractionPromptData`，只表达：

- 稳定 Prompt ID。
- 本地化文本 Key。
- MVP 开发回退文本。
- 可选的静态图标语义 ID。

目标 ID 继续使用 `InteractionTargetId`。按键显示字符串不进入 Interaction 数据模型，由 UI 本地解析并缓存。

该数据：

- 不包含按钮回调。
- 不包含 `Interact()`。
- 不包含具体机关类型。
- 不保存运行时可变权威状态。
- 不进行网络同步。

### 3.4 IInteractable 是否暴露提示文本

**结论：不修改 `IInteractable`，使用独立 Source/Definition。**

原因：

- `IInteractable` 已通过 Sprint002A Gate，职责仅是目标检测。
- 显示文本属于表现语义，不应污染所有交互目标的检测契约。
- 直接暴露本地化后的文本会把语言和 UI 细节带入 Gameplay Contract。
- 未来同一目标可能按角色、设备或可用性显示不同提示，不能把单一字符串固定在检测接口。

建议：

- `InteractionPromptDefinition`：ScriptableObject 静态定义，含稳定 ID、文本 Key 和开发回退文本。
- `IInteractionPromptSource`：只读公开 Prompt 数据。
- `InteractionPromptSource`：挂在交互目标附近的最小适配组件，引用 Definition。

UI 只查询 `IInteractionPromptSource`，不识别 Door、Lever、Chest、Puzzle 或其他具体类型。

### 3.5 如何避免 UI 依赖具体机关

**结论：以 `IInteractable + IInteractionPromptSource` 组合提供数据。**

Presenter 从 Detector 获得当前 `IInteractable`，只在目标变化时解析同对象层级上的只读 Prompt Source。找不到有效 Source 时隐藏 Prompt，不回退为基于 GameObject 名称生成的文字。

禁止：

- `if target is Door`
- 按 Tag 或对象名选择文字
- UI 直接读取机关内部字段
- 每类机关各写一套 Prompt View

未来具体目标只需通过 Prefab 组合提供 Prompt Definition，不需要 UI 引用其程序集。

### 3.6 无目标、目标切换和目标失效

**结论：使用实例级目标变化通知和明确的隐藏状态。**

建议为 Detector 增加只读实例事件 `CurrentTargetChanged`，仅当目标引用实际变化时触发。该事件不是全局事件总线，不执行任何交互。

状态规则：

- 初始化或无目标：View 隐藏且清空缓存。
- 首次获得有效目标：解析一次 Prompt Source，显示一次。
- 同一目标连续帧：不重复设置文本或重建模型。
- 切换目标：替换 TargetId、Prompt 数据和 View 内容。
- 目标离开、遮挡、禁用、销毁或 Detector 禁用：发布空目标，立即隐藏。
- 目标缺少或持有非法 Definition：安全隐藏并记录一次明确诊断，不持续刷日志。
- Player 销毁或重新生成：Binder 解除旧订阅、隐藏 View，并绑定新 Player 的 Detector。

### 3.7 是否需要支持本地化

**结论：数据必须从本阶段开始支持本地化 Key，但不实现完整本地化系统。**

Sprint002B：

- Definition 保存稳定文本 Key。
- 保存一个开发/测试回退文本。
- UI 通过最小文本解析边界取得显示文本；没有 Localization 服务时使用回退文本。
- 测试以 Key 和回退行为为准，不绑定某一种正式语言。

本阶段不安装或实现：

- Unity Localization 完整包与表格工作流
- 语言切换页面
- 多语言字体回退体系
- 网络传输本地化字符串

未来 Localization 只替换 UI 文本解析器，不修改 Detector 或 `IInteractable`。

### 3.8 键鼠、手柄与手机触控提示

**结论：Sprint002B 必须支持键鼠与手柄提示；手机触控不属于当前项目基线。**

- 键鼠读取现有 `Gameplay/Interact` 的 `E` Binding 显示信息。
- 手柄读取现有 `buttonWest` Binding 显示信息。
- 不硬编码 `"E"`、`"X"` 或平台按钮名称。
- MVP 可同时展示已配置的键鼠与手柄提示，避免为动态设备切换提前建设全局设备服务。
- 若实现当前设备切换，只能是 UI 本地表现，不得修改 PlayerInputReader 或 Interaction 状态。
- 当前没有 Touch Control Scheme；Sprint002B 不增加手机输入、触控按钮或移动端布局。
- View 保留独立的 Binding Hint 槽位即可，不提前实现移动端适配层。

### 3.9 是否允许读取交互键位

**结论：允许读取绑定元数据，禁止消费交互输入。**

UI 可以通过序列化 `InputActionReference` 读取：

- Action 名称。
- Binding Group。
- Binding Display String。

本阶段禁止：

- 订阅 `Interact.performed` 或 `Interact.canceled` 来触发玩法。
- 调用 `Interact`、Executor、命令端口或目标方法。
- 通过 `Keyboard.current` 或 `Gamepad.current` 读取交互按键。
- 为显示 Prompt 改写或启用另一套 Input Action。

按下 `E` 或手柄按钮时，Prompt 可以保持显示，但不得产生任何目标状态变化。

### 3.10 如何避免每帧重复分配字符串

**结论：以变化驱动更新和字符串缓存为强制门禁。**

- Detector 只在目标变化时通知。
- Definition 中的 Key 和回退文本为静态引用。
- Binding Display String 只在 View 初始化、配置变更或明确设备显示策略变化时解析并缓存。
- View 只在可见性、TargetId、Prompt ID 或 Binding Hint 变化时写入文本。
- 不在 `Update` 中使用字符串插值、拼接、`ToString()`、LINQ 或反复调用 `GetBindingDisplayString()`。
- 不在目标不变时重复调用 TMP 文本赋值。
- PlayMode 增加预热后的稳定 Prompt 循环托管分配验证。

### 3.11 如何防止 Player、Interaction、UI 循环依赖

**结论：固定以下有向无环图，并以 asmdef 测试锁定。**

```text
Core
▲  ▲  ▲
│  │  │
Player  Interaction
   ▲       ▲
   └── UI ─┘
```

实际含义是 Player 和 Interaction 分别只向 Core 依赖；UI 位于外层，可以读取两者的公开状态。Player 与 Interaction 不引用 UI，Interaction 也不引用 Player。

测试必须解析三个 asmdef 并断言：

- Player 不含 Interaction/UI。
- Interaction 不含 Player/UI。
- UI 可以含 Player/Interaction/Core。
- Runtime 不引用 Tests 或 Editor。

### 3.12 Prompt 状态测试

**结论：可以基于现有 PlayerSandbox 和 Interaction Detection 测试设施覆盖。**

必须验证：

- 无目标时隐藏。
- 目标进入范围后显示。
- 目标离开范围后隐藏。
- 两个目标之间切换时内容正确替换，无旧内容残留。
- 墙体遮挡目标后隐藏。
- 遮挡移除后恢复。
- 目标禁用、销毁或 Definition 失效时隐藏且无异常。
- 同一目标稳定存在时不重复刷新 View。
- Player 销毁和重新生成后重新绑定。
- 按下 Interact Binding 不执行任何行为。
- Prompt 存在时 Player Spawn、Input、Movement 和 Camera 不回归。

## 4. 建议职责划分

### Interaction

- `InteractionDetector`：保持候选检测职责；仅增加目标变化的只读实例通知。
- `InteractionPromptDefinition`：静态 Prompt 语义和开发回退文本。
- `InteractionPromptData`：跨 Interaction/UI 的不可变只读数据。
- `IInteractionPromptSource`：目标侧只读 Prompt 端口。
- `InteractionPromptSource`：Unity Prefab 组合适配器。

Interaction 不引用 Canvas、TMP、Input System、Player 或 UI。

### UI

- `InteractionPromptView`：只负责显示/隐藏文字、按键提示和可选图标槽。
- `InteractionPromptPresenter`：把当前目标的 Prompt 数据转换为 View 状态并去重更新。
- `InteractionPromptBinder`：通过显式 PlayerSpawner 引用绑定当前 Sandbox 本地玩家 Detector，处理销毁与重新生成。
- `InteractionBindingDisplayProvider`：只读解析并缓存现有 Interact Binding 的显示信息。

UI 不修改 Detector、Player 或目标状态。

## 5. 预计新增文件

文件名可在 Implementation Plan 中做不改变职责的微调。

### Runtime / Interaction

- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/InteractionPromptData.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/InteractionPromptDefinition.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/IInteractionPromptSource.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/InteractionPromptSource.cs`
- 上述目录、脚本对应的 `.meta`

### Runtime / UI

- `Client/Assets/WonderSquad/Runtime/UI/Interaction/InteractionPromptView.cs`
- `Client/Assets/WonderSquad/Runtime/UI/Interaction/InteractionPromptPresenter.cs`
- `Client/Assets/WonderSquad/Runtime/UI/Interaction/InteractionPromptBinder.cs`
- `Client/Assets/WonderSquad/Runtime/UI/Interaction/InteractionBindingDisplayProvider.cs`
- 上述目录、脚本对应的 `.meta`

### Assets

- `Client/Assets/WonderSquad/ScriptableObjects/Content/Interaction/InteractionPrompt_Default.asset`
- `Client/Assets/WonderSquad/Prefabs/UI/PF_InteractionPrompt.prefab`
- 新目录、资产及 Prefab 对应的 `.meta`

### Tests

- `Client/Assets/WonderSquad/Tests/EditMode/InteractionPromptEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionPromptPlayModeTests.cs`
- 对应 `.meta`

### Sprint 文档

- `Docs/01_Project/Sprint002B_Implementation_Plan.md`
- `Docs/01_Project/Sprint002B_Implementation_Report.md`
- `Docs/01_Project/Sprint002B_Review_Checklist.md`

## 6. 预计修改文件

- `Client/Assets/WonderSquad/Runtime/Interaction/Detection/InteractionDetector.cs`
  - 仅增加去重的当前目标变化通知。
- `Client/Assets/WonderSquad/Runtime/UI/WonderSquad.UI.asmdef`
  - 增加允许的 Interaction、Player、Input System 和 UI 文本依赖。
- `Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef`
- `Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef`
- `Client/Assets/WonderSquad/Prefabs/Interaction/PF_Interactable_Test.prefab`
  - 只增加 Prompt Source/Definition 组合。
- `Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`
  - 增加独立 Prompt 验证 UI 和必要的第二测试目标；不得加入具体机关。
- `Client/Packages/manifest.json`
  - 若使用当前已解析的 uGUI/TMP，直接锁定 `com.unity.ugui` `2.0.0`，不升级版本。
- `Client/Packages/packages-lock.json`
  - 只接受 `com.unity.ugui` 依赖深度变化，不接受无关包版本变化。
- `CHANGELOG.md`

以下文件预计不修改：

- `IInteractable.cs`
- `InteractionTargetId.cs`
- PlayerSpawner、PlayerInputReader、PlayerMovement、GroundDetector
- Camera Runtime
- `WonderSquad.inputactions`
- Puzzle、Inventory、Ability、Network

## 7. 程序集依赖

### Runtime

| 程序集 | 允许引用 |
|---|---|
| `WonderSquad.Core` | 无 |
| `WonderSquad.Player` | `WonderSquad.Core`、`Unity.InputSystem` |
| `WonderSquad.Interaction` | `WonderSquad.Core` |
| `WonderSquad.UI` | `WonderSquad.Core`、`WonderSquad.Interaction`、`WonderSquad.Player`、`Unity.InputSystem`、选定的 Unity UI 文本程序集 |

### Tests

- EditMode Tests 增加 `WonderSquad.UI` 和所需 UI 文本程序集引用。
- PlayMode Tests 增加 `WonderSquad.UI` 和所需 UI 文本程序集引用。
- Tests 继续可以引用 Interaction、Player 与 Input System。
- Runtime 不得反向引用 Tests。

## 8. 自动化测试方案

### 8.1 EditMode

1. `IInteractable` 仍为只读且没有 Prompt 文本或执行方法。
2. Prompt Definition 的稳定 ID、文本 Key 和回退文本配置有效。
3. Prompt Data 为只读值，不包含回调或可变目标状态。
4. 缺少 Definition、空 ID 或空文本 Key 时配置无效。
5. Presenter 的初始状态为隐藏。
6. 相同目标重复输入不会重复刷新 View。
7. 目标切换正确替换 Prompt 数据。
8. 空目标或非法 Source 正确产生隐藏状态。
9. 现有 Interact Action 的键鼠与手柄 Binding 可以解析显示信息。
10. 解析绑定不启用或消费 Interact Action。
11. Player、Interaction、UI asmdef 依赖方向符合第 7 节。
12. `com.unity.ugui` 版本与选定 UI 文本程序集配置准确。

### 8.2 PlayMode

1. Player 生成前 Prompt 隐藏。
2. Player 生成后 Binder 绑定唯一 Detector。
3. 无目标时 Prompt 隐藏。
4. 目标进入范围后 Prompt 显示正确回退文本与绑定提示。
5. 离开范围后 Prompt 隐藏且清空旧内容。
6. 从目标 A 切换到目标 B 时内容正确替换。
7. 墙体遮挡时隐藏，遮挡清除后恢复。
8. 目标禁用或销毁时隐藏且无 `NullReferenceException`。
9. Player 销毁后隐藏，重新生成后可以重新绑定。
10. 同一目标稳定存在时不重复创建对象、订阅或分配字符串。
11. 按下 `E` 或模拟手柄 Interact 不修改目标状态、不执行交互。
12. 场景只存在一个 Prompt View，不重复创建 Canvas。
13. Player Spawn、Input、Movement、Camera 和 Sprint002A Detection 全量回归通过。

## 9. 人工验收方案

在 Unity `6000.3.21f1` 中：

1. 打开 `PlayerSandbox` 并进入 Play Mode。
2. 玩家生成后、未接近目标时 Prompt 不显示。
3. 靠近 `InteractionTestTarget` 时出现交互提示。
4. 提示同时具备动作文字和键位信息，不只依赖颜色或声音。
5. 离开目标范围后 Prompt 立即隐藏。
6. 在两个临时测试目标之间移动，确认内容切换且不闪回旧目标。
7. 让墙体遮挡目标，确认 Prompt 隐藏；移除遮挡后恢复。
8. 运行时禁用或删除目标，确认 Prompt 安全隐藏。
9. 验证键盘与手柄 Binding 显示来自 Input Actions，而不是硬编码文字。
10. 按 `E` 和手柄西侧按钮，确认没有目标状态变化、动画或交互执行。
11. 删除并重新生成 Player，确认 Prompt Binder 安全恢复。
12. 确认 WASD、Camera 和现有检测行为没有回归。
13. 确认没有 Door、Lever、Chest、Puzzle、Inventory 或 Network 对象。
14. Console Error 为 `0`，且没有每帧重复日志。

## 10. 范围禁令

Sprint002B 禁止：

- 调用 `Interact` 或增加无条件执行方法。
- 创建 InteractionExecutor。
- 修改目标、库存、机关、谜题或网络状态。
- Door、Lever、Chest 或其他具体玩法对象。
- Puzzle、Inventory、Ability、Network。
- 全局事件总线、静态 Player/Target 引用或 Singleton。
- 复杂 UI 动画、动态设备服务或完整本地化系统。
- 移动端触控输入与触控 HUD。
- 修改 Player Movement、Camera 或 Input Reader。

本地 Prompt 不同步，不需要主机确认；未来真正的交互请求仍必须由权威端重新校验。

## 11. 风险与控制

| 风险 | 级别 | 控制 |
|---|---|---|
| UI 反向进入 Gameplay | 高 | 固定 `UI → Interaction/Player`，以 asmdef 测试锁定 |
| 把提示文字塞入 `IInteractable` | 高 | 使用独立 Prompt Source/Definition |
| Prompt 与真实可执行结果混淆 | 高 | 明确本地提示不是权威成功，本阶段无 Executor |
| 每帧字符串分配 | 中 | 目标变化驱动、缓存 Definition 与 Binding 文本 |
| 目标切换时闪烁或显示旧数据 | 中 | TargetId/PromptId 去重，空目标立即清理 |
| Input Hint 意外消费按键 | 高 | 只读 Binding 元数据，不订阅执行输入 |
| 依赖传递 UI 包 | 中 | 将实际使用的 uGUI 版本直接锁定并审计 lockfile |
| DebugCanvas 配置不可见 | 中 | 使用独立 Prompt Prefab并在 Unity 中验证缩放与锚点 |
| 未来本地化迁移返工 | 中 | 当前即保存稳定文本 Key，Fallback 仅用于 MVP |
| 提前扩展移动端 | 低 | Touch 明确列为非本阶段范围 |

## 12. 开始实现前必须关闭的条件

### 阻塞条件

1. Sprint002A 当前提交 `f6e017f` 尚未合并到 `develop`；当前分支比 `develop` 超前 1 个提交。
2. 必须先完成 Sprint002A 的 Review/Merge，再从最新 `develop` 创建并切换到 `feature/sprint002b-interaction-prompt`。
3. 分支切换后工作区必须干净，且改动范围只属于 Sprint002B。

### 实施前确认

4. 在 Unity `6000.3.21f1` 中确认 `com.unity.ugui` `2.0.0` 提供的选定文本组件可被 `WonderSquad.UI` asmdef 正常引用。
5. 若使用当前已解析的 uGUI/TMP，必须把 `com.unity.ugui` `2.0.0` 提升为 manifest 直接依赖；不得升级或修改其他包。
6. 实施计划必须锁定以下已确定规则：
   - 不修改 `IInteractable`。
   - UI 只读依赖 Interaction。
   - Prompt 使用稳定 Key 与开发 Fallback。
   - 键鼠和手柄提示来自现有 Binding 元数据。
   - 不读取按键执行状态，不实现移动端。
   - 目标变化通知是实例事件，不是全局事件总线。

## 13. 最终状态

# CONDITIONAL GO

架构与现有基础满足 Sprint002B。关闭第 12 节分支、工作区和 UI 文本依赖条件后，可以开始 Interaction Prompt 实现；当前不得开始写 Sprint002B 代码。
