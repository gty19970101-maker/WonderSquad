# Sprint002B Interaction Prompt Implementation Plan

## 1. 计划状态

- Sprint：Sprint002B — Interaction Prompt
- Unity：`6000.3.21f1`
- 当前分支：`feature/sprint002b-interaction-prompt`
- 当前基线：`972dc44` / `v0.4-interaction-detection`
- Sprint002A：`PASSED`，Gate Review 为 `GO`
- 分支状态：从当前 `develop` 基线创建
- 工作区状态：计划生成前干净
- 实施状态：**READY FOR IMPLEMENTATION**

本文件只定义实施规则、文件边界和验收方案，不包含 Unity 业务实现代码。

## 2. 目标

当 `InteractionDetector` 发现有效目标时，在 PlayerSandbox 展示本地 Interaction Prompt；目标消失、切换或失效时正确更新或隐藏。

唯一主链：

```text
InteractionDetector
        ↓ 只读当前目标变化
InteractionPromptPresenter
        ↓ 只读 View State
InteractionPromptView
```

Prompt 只表达“当前本地检测到一个可提示目标”，不代表目标可以被权威执行，更不代表交互已经成功。

## 3. 实现范围

### 3.1 本 Sprint 只实现

- 只读 Prompt Data Model。
- 目标侧 Prompt Definition 与 Source。
- Detector 当前目标变化通知。
- Interaction Prompt Presenter。
- Interaction Prompt View。
- 无目标、进入、离开、切换、遮挡和失效生命周期。
- 现有键鼠与手柄 Interact Binding 的只读显示。
- 本地化 Key 与开发回退文本。
- PlayerSandbox Prompt 验证环境。
- EditMode、PlayMode 与 Character Foundation/Sprint002A 回归测试。

### 3.2 明确禁止

- `InteractionExecutor`
- `Interact()`
- 任何目标状态修改
- Door
- Lever
- Chest
- Inventory
- Puzzle
- Ability
- Network、RPC、Photon Fusion
- UI 触发 Gameplay Command
- 全局事件总线或静态 Player/Target 引用
- 复杂动画
- 完整本地化系统
- 移动端触控输入

## 4. 核心职责

### 4.1 InteractionDetector

继续负责：

- 检测与验证候选目标。
- 多目标去重和确定性选择。
- 提供当前只读 `IInteractable`。

本 Sprint 只允许增加：

- 实例级当前目标变化通知。
- 相同目标不重复通知。
- 清空、禁用或目标失效时通知空目标。

不得增加：

- Prompt 文本、Canvas、TMP 或 Input System 依赖。
- Prompt Definition 解析。
- UI 可见性控制。
- 交互输入或执行。

### 4.2 InteractionPromptPresenter

负责：

- 绑定 PlayerSandbox 当前本地 Player 的 Detector。
- 订阅 Detector 当前目标变化。
- 将目标与 Prompt Source 转换为只读 Prompt View State。
- 缓存当前 TargetId、PromptId 和绑定显示信息。
- 控制 View 显示、切换和隐藏。
- Player 销毁或重新生成时解除旧绑定并连接新 Detector。

不得负责：

- 重新执行 Physics Query。
- 重新排序多个候选目标。
- 读取具体 Door、Lever、Puzzle 等类型。
- 启用或消费 Interact Action。
- 修改 Player、Detector 或目标状态。
- 发出 Gameplay Command。

Presenter 是唯一编排点。PlayerSpawner 只作为 Sandbox 本地玩家来源，不进入 Prompt 数据模型，也不承担未来 Network Player 选择。

### 4.3 InteractionPromptView

负责：

- 显示或隐藏 Prompt 根节点。
- 显示动作回退文本。
- 分别显示键鼠和手柄 Binding Hint。
- 仅在 View State 变化时写入 TMP 文本。

不得负责：

- 读取 Detector。
- 读取 Input Action。
- 解析 Prompt Source。
- 识别具体交互对象。
- 播放复杂动画。
- 执行交互。

View 使用屏幕空间 UI。当前只需要清晰、稳定、可验证的占位样式，不建设完整 HUD 视觉系统。

## 5. Prompt Data Model

### 5.1 InteractionPromptDefinition

使用 ScriptableObject 保存静态内容：

| 字段 | 规则 |
|---|---|
| Prompt ID | 稳定、非空、与 GameObject 名称无关 |
| Localization Key | 稳定、非空；供未来本地化系统查询 |
| Fallback Text | MVP 与测试使用的开发回退文本 |

约束：

- Definition 不保存当前目标、可见性或其他运行时状态。
- Definition 不包含回调、命令或执行方法。
- 不包含设备绑定文本；设备提示由 UI 本地生成。
- 非法 Definition 不显示 Prompt。

### 5.2 InteractionPromptData

使用不可变只读值表达：

- `InteractionTargetId`
- Prompt ID
- Localization Key
- Fallback Text
- 配置是否合法

不包含：

- `GameObject` 名称
- 具体机关类型
- 输入状态
- UI Widget 引用
- Action、Delegate 或 Command
- 网络或权威状态

### 5.3 IInteractionPromptSource

目标侧只读端口：

- 只提供有效 Prompt Data。
- 不提供 `Interact()`。
- 不改变目标状态。

### 5.4 InteractionPromptSource

目标 Prefab 上的最小 Unity 适配组件：

- 引用一个 `InteractionPromptDefinition`。
- 使用同对象的稳定 `InteractionTargetId` 生成只读 Prompt Data。
- Definition 缺失或非法时返回无效状态。

它与 `IInteractable` 分离，不修改 Sprint002A 已通过 Gate 的检测契约。

## 6. Presenter 绑定与生命周期

### 6.1 初始化

```text
Presenter OnEnable
→ View 进入 Hidden
→ 订阅 PlayerSpawner.PlayerSpawned
→ 若 Player 已存在，立即绑定其 InteractionDetector
→ 缓存键鼠与手柄 Binding Display String
→ 读取 Detector 当前目标
→ 生成首个 View State
```

### 6.2 Detector 绑定

- 只绑定当前明确的 Sandbox 本地 Player。
- Player 上没有且只能没有重复 Detector。
- 切换 Detector 前先取消旧订阅。
- 同一 Detector 不重复订阅。
- 不使用 `FindObjectOfType`、Tag、对象名称或静态 Player 引用。

### 6.3 禁用与销毁

```text
Presenter OnDisable
→ 取消 PlayerSpawner 订阅
→ 取消 Detector 订阅
→ 清空 TargetId 与 PromptId 缓存
→ View 隐藏
```

Detector 禁用时必须先清空当前目标并发出一次空目标通知。目标销毁时 Presenter 不保留 Unity 伪空引用。

### 6.4 Player 重生

- 旧 Player Detector 禁用或销毁后 Prompt 隐藏。
- PlayerSpawner 生成新 Player 时 Presenter 绑定新 Detector。
- 新 Player 已有当前目标时立即刷新。
- 不创建第二个 Presenter、View 或 Canvas。

## 7. 目标状态规则

### 7.1 无目标

- Prompt 隐藏。
- 清空当前 TargetId、PromptId 和显示数据。
- 不保留上一目标文字。

### 7.2 获得目标

- 只在 Detector 目标变化时解析 Prompt Source。
- 有效 Source/Definition：显示。
- 无 Source、Definition 缺失或数据非法：隐藏并安全退出。
- 非法配置诊断最多记录一次，不逐帧刷日志。

### 7.3 目标切换

- Target A → Target B 时，在同一次 Presenter 更新中替换数据。
- 不先短暂显示空提示再显示 B，除非 Detector 实际发布了空目标。
- 切换后不得残留 A 的文字、TargetId 或 PromptId。

### 7.4 目标丢失

以下情况统一进入 Hidden：

- 离开检测范围。
- 目标被墙体遮挡。
- Layer 或可检测状态失效。
- 目标禁用或销毁。
- Detector 禁用。
- Player 销毁。

### 7.5 多目标更新

- Presenter 不接收候选数组。
- Presenter 不运行排序或距离计算。
- Detector 继续使用 Sprint002A 的优先级、距离、TargetId 顺序选择唯一 CurrentTarget。
- 多目标优先级改变导致 CurrentTarget 改变时，Presenter 只消费最终结果。
- 相同 CurrentTarget 连续帧不触发 View 更新。

## 8. 键鼠与手柄显示

### 8.1 数据来源

复用 `WonderSquad.inputactions` 中：

- Action Map：`Gameplay`
- Action：`Interact`
- 键鼠 Binding：`<Keyboard>/e`
- 手柄 Binding：`<Gamepad>/buttonWest`

### 8.2 显示规则

- 使用 Input System Binding 元数据生成 Display String。
- 不硬编码 `"E"`、`"X"`、`"Square"` 或平台按钮名称。
- View 使用独立文本槽展示动作文字、键鼠提示与手柄提示，避免运行时拼接字符串。
- Sprint002B 可以同时展示键鼠和手柄提示。
- 不实现“最后使用设备”全局跟踪服务。
- 不添加 Touch Control Scheme 或移动端按钮。

### 8.3 输入边界

允许：

- 读取 Action 名称、Binding Group 和 Binding Display String。

禁止：

- 调用 Action `Enable()`。
- 订阅 `performed`、`canceled` 或 `started` 来执行行为。
- 调用 `Interact()` 或 Executor。
- 使用 `Keyboard.current` 或 `Gamepad.current`。
- 修改 `WonderSquad.inputactions`。

按下已绑定按键不得改变 Prompt 之外的任何状态。

## 9. 本地化预留

- `InteractionPromptDefinition` 从第一版保存稳定 Localization Key。
- Sprint002B 使用 Fallback Text 显示。
- Presenter 中保留单一文本解析位置，不把本地化判断散落到 View 或 Detector。
- 不安装 Unity Localization 包。
- 不创建语言表、语言切换 UI 或字体回退系统。
- 不同步本地化后的字符串。

未来接入本地化时，只替换 UI 文本解析策略；Detector、Prompt Source 和 `IInteractable` 不需要改变。

## 10. 零 GC 策略

### 10.1 变化驱动

- Detector 只在目标引用变化时通知。
- Presenter 不在 Update 中反复解析目标。
- View 只在状态变化时写入文本或切换可见性。

### 10.2 缓存

- Prompt ID、Localization Key 和 Fallback Text 来自静态 Definition。
- 键鼠与手柄 Binding Display String 在 Presenter 初始化时生成并缓存。
- 当前 TargetId、PromptId 和可见性缓存用于阻止重复 View 更新。

### 10.3 禁止分配路径

稳定帧中禁止：

- 字符串拼接或插值。
- `ToString()` 生成 UI 字符串。
- LINQ。
- `ToList()`、`ToArray()` 或新集合。
- 反复 `GetBindingDisplayString()`。
- 重复组件查找。
- 重复事件订阅。
- 重复 TMP 文本赋值。

目标切换时允许一次必要的组件解析和 View 更新；稳定显示或稳定隐藏状态必须通过预热后的托管分配测试。

## 11. 程序集依赖

### 11.1 Runtime

```text
WonderSquad.Core
      ▲             ▲
      │             │
WonderSquad.Player  WonderSquad.Interaction
      ▲             ▲
      └──── WonderSquad.UI
```

允许引用：

| 程序集 | 引用 |
|---|---|
| `WonderSquad.Core` | 无 |
| `WonderSquad.Player` | `WonderSquad.Core`、`Unity.InputSystem` |
| `WonderSquad.Interaction` | `WonderSquad.Core` |
| `WonderSquad.UI` | `WonderSquad.Core`、`WonderSquad.Player`、`WonderSquad.Interaction`、`Unity.InputSystem`、`Unity.TextMeshPro` |

禁止：

- Player 引用 Interaction 或 UI。
- Interaction 引用 Player、UI、Input System 或 TMP。
- UI 被 Gameplay Runtime 反向引用。
- Runtime 引用 Tests 或 Editor。

### 11.2 Unity UI 包

- 将当前已解析的 `com.unity.ugui` 精确版本 `2.0.0` 写为 manifest 直接依赖。
- `packages-lock.json` 只允许该包依赖深度变为直接依赖。
- 不升级或修改 Cinemachine、Input System、URP、Test Framework 或其他无关包。
- Unity 导入后确认 `Unity.TextMeshPro` 程序集可被 `WonderSquad.UI` 引用。

## 12. 实施顺序

### B-01：锁定 UI 依赖和程序集边界

目标：

- 将 `com.unity.ugui` `2.0.0` 提升为直接依赖。
- 更新 UI 与测试 asmdef。
- 先建立并验证单向依赖。

完成标准：

- Unity 编译无错误。
- 无无关包版本变化。
- Player 与 Interaction asmdef 未增加反向引用。

### B-02：建立 Prompt Data Model

目标：

- 创建 Definition、Data、Source Contract 和 Unity Source。

完成标准：

- Definition 与 Data 只读有效。
- 不修改 `IInteractable`。
- 不包含输入、UI 或执行方法。

### B-03：增加 Detector 目标变化通知

目标：

- 让 UI 以变化驱动方式消费 CurrentTarget。

完成标准：

- 首次目标、切换和清空各通知一次。
- 相同目标不重复通知。
- Detector 的选择、遮挡和零分配行为不变。

### B-04：实现 Presenter

目标：

- 绑定 Sandbox Player、Detector、Prompt Source 与 View。

完成标准：

- 目标进入、离开、切换和失效产生正确 View State。
- 生命周期订阅成对。
- 不执行 Physics、Input 或 Gameplay Command。

### B-05：实现 View 与 Binding Display

目标：

- 显示 Fallback Text、键鼠提示与手柄提示。

完成标准：

- View 不读取 Gameplay。
- Binding 来自现有 Input Actions。
- Action 不被启用或消费。
- 无复杂动画。

### B-06：组装资产与 PlayerSandbox

目标：

- 创建默认 Definition、Prompt Prefab 和测试目标配置。
- 在 PlayerSandbox 建立可重复人工验证环境。

完成标准：

- 只有一个 Prompt View。
- 无目标时默认隐藏。
- 不增加具体机关或执行组件。
- UI 锚点、缩放和常见 Game View 尺寸下可见。

### B-07：完成自动化测试与回归

目标：

- 运行新增 EditMode、PlayMode 和现有完整回归。

完成标准：

- 新增测试全部通过。
- Sprint001 Character Foundation 与 Sprint002A 全部回归。
- Console Error 为 `0`。

### B-08：Unity 人工验收与文档收尾

目标：

- 执行第 16 节步骤。
- 更新 Implementation Report、Review Checklist 和 CHANGELOG。

完成标准：

- 所有人工步骤通过后才可标记 `PASSED`。
- 人工验收前状态保持 `CONDITIONAL GO` 或 `UNITY VERIFICATION REQUIRED`。

## 13. 新增文件

### Runtime / Interaction

- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/InteractionPromptData.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/InteractionPromptDefinition.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/IInteractionPromptSource.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/InteractionPromptSource.cs`
- 对应目录与 `.meta`

### Runtime / UI

- `Client/Assets/WonderSquad/Runtime/UI/Interaction/InteractionPromptPresenter.cs`
- `Client/Assets/WonderSquad/Runtime/UI/Interaction/InteractionPromptView.cs`
- `Client/Assets/WonderSquad/Runtime/UI/Interaction/InteractionBindingDisplayProvider.cs`
- 对应目录与 `.meta`

### Assets

- `Client/Assets/WonderSquad/ScriptableObjects/Content/Interaction/InteractionPrompt_Default.asset`
- `Client/Assets/WonderSquad/Prefabs/UI/PF_InteractionPrompt.prefab`
- 对应目录与 `.meta`

### Tests

- `Client/Assets/WonderSquad/Tests/EditMode/InteractionPromptEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionPromptPlayModeTests.cs`
- 对应 `.meta`

### Documents

- `Docs/01_Project/Sprint002B_Implementation_Report.md`
- `Docs/01_Project/Sprint002B_Review_Checklist.md`

## 14. 修改文件

- `Client/Assets/WonderSquad/Runtime/Interaction/Detection/InteractionDetector.cs`
- `Client/Assets/WonderSquad/Runtime/UI/WonderSquad.UI.asmdef`
- `Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef`
- `Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef`
- `Client/Assets/WonderSquad/Prefabs/Interaction/PF_Interactable_Test.prefab`
- `Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`
- `Client/Packages/manifest.json`
- `Client/Packages/packages-lock.json`
- `CHANGELOG.md`

以下文件不得修改：

- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/IInteractable.cs`
- `Client/Assets/WonderSquad/Runtime/Core/Contracts/Interaction/InteractionTargetId.cs`
- `Client/Assets/WonderSquad/Runtime/Player/`
- `Client/Assets/WonderSquad/Runtime/Camera/`
- `Client/Assets/WonderSquad/Settings/Input/WonderSquad.inputactions`
- Puzzle、Inventory、Ability、Network Runtime

## 15. 自动化测试方案

### 15.1 EditMode

1. `IInteractable` 仍不包含 Prompt 字段或执行方法。
2. Prompt Definition 默认与资产配置有效。
3. 空 Prompt ID、空 Localization Key 或空 Fallback Text 被拒绝。
4. Prompt Data 是不可变只读值。
5. Prompt Source 缺少 Definition 时无效。
6. Presenter 初始状态为 Hidden。
7. 空目标隐藏 View。
8. 有效目标生成正确 View State。
9. 同一目标重复通知不重复刷新 View。
10. Target A → Target B 正确替换 TargetId、PromptId 与文字。
11. 无效 Source/Definition 安全隐藏。
12. 键鼠 Binding Display 可以从现有 Interact Action 解析。
13. 手柄 Binding Display 可以从现有 Interact Action 解析。
14. Binding 解析不启用 Action、不订阅执行回调。
15. Player、Interaction、UI asmdef 依赖方向正确。
16. `com.unity.ugui` 精确为 `2.0.0`，无无关包变化。

### 15.2 PlayMode

1. Player 生成前 Prompt 隐藏。
2. Player 生成后 Presenter 绑定唯一 Detector。
3. 无目标时 Prompt 隐藏。
4. 目标进入范围后显示 Fallback Text、键鼠和手柄提示。
5. 离开范围后隐藏并清除旧数据。
6. 目标 A/B 切换后只显示当前目标。
7. 多目标排序仍与 Detector 的优先级、距离、TargetId 规则一致。
8. 墙体遮挡后隐藏，清除遮挡后恢复。
9. 目标禁用或销毁后隐藏且无异常。
10. Detector 禁用后隐藏，重新启用后可恢复。
11. Player 销毁后隐藏，重新生成后重新绑定。
12. 相同目标稳定存在时 View 不重复刷新。
13. 稳定显示与稳定隐藏状态在预热后不产生托管分配。
14. 按下键盘 Interact Binding 不修改目标状态。
15. 按下手柄 Interact Binding 不修改目标状态。
16. 场景只有一个 Prompt View，不重复创建 Canvas。
17. Player Spawn、Input、Movement、Camera 与 Interaction Detection 回归全部通过。

### 15.3 静态范围扫描

Interaction/UI 新增 Runtime 代码不得包含：

- `Interact(`
- `InteractionExecutor`
- Door、Lever、Chest
- Inventory、Puzzle、Network 类型引用
- `Keyboard.current`
- `Gamepad.current`
- `FindObjectOfType` 或对象名查找
- 每帧 LINQ 或字符串格式化

扫描结果需要记录到 Implementation Report。

## 16. 人工验收方案

使用 Unity `6000.3.21f1`：

1. 打开 `PlayerSandbox`。
2. 进入 Play Mode，确认 Player 正常生成。
3. 未接近任何目标时 Prompt 不显示。
4. 靠近 `InteractionTestTarget`，Prompt 显示动作文字。
5. 确认键鼠和手柄提示均来自现有 Input Actions。
6. 离开范围，Prompt 隐藏且不残留旧文字。
7. 在两个临时目标之间移动，确认目标切换稳定。
8. 在多目标重叠范围改变距离，确认 Presenter 只显示 Detector 选中的目标。
9. 使用墙体遮挡目标，Prompt 隐藏；清除遮挡后恢复。
10. 运行时禁用或删除目标，Prompt 安全隐藏。
11. 运行时禁用 Detector，Prompt 隐藏；重新启用后可恢复。
12. 删除 Player 后 Prompt 隐藏；重新生成 Player 后 Presenter 重新绑定。
13. 按 `E` 和手柄西侧按钮，确认没有交互执行、动画或目标状态变化。
14. 确认 WASD、Player Spawn、Camera 与 Sprint002A Detection 正常。
15. 确认场景没有 Door、Lever、Chest、Inventory、Puzzle 或 Network 实现。
16. 确认 Prompt 没有复杂动画或逐帧日志。
17. Console Error 为 `0`。

## 17. 完成标准

只有同时满足以下条件，Sprint002B 才可进入最终人工验收：

- Unity 编译无错误。
- 所有新增 EditMode 与 PlayMode 测试通过。
- Character Foundation 与 Sprint002A 完整回归通过。
- Prompt 正确处理无目标、进入、离开、切换、遮挡、失效和 Player 重生。
- 键鼠与手柄提示来自 Input Actions Binding 元数据。
- 稳定帧零托管分配验证通过。
- InteractionDetector 不执行交互。
- `IInteractable`、Player、Camera 与 Input Actions 未修改。
- 没有引入禁止系统、循环依赖或无关包变化。
- Implementation Report、Review Checklist 与 CHANGELOG 已更新。

Unity 人工验收全部通过后，Sprint002B 才能标记为 `PASSED`。完成本计划后停止，不开始 InteractionExecutor、Door、Lever、Inventory、Puzzle 或 Network。
