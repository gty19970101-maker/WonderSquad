# Sprint002B Interaction Prompt Implementation Report

## 1. 实施结论

- 实施日期：2026-08-02
- Unity 基线：`6000.3.21f1`
- 分支：`feature/sprint002b-interaction-prompt`
- 基线提交：`972dc44`（`v0.4-interaction-detection`）
- 实施范围：Interaction Prompt 只读展示层
- 当前状态：**PASSED**

Interaction Prompt 已按以下单向数据流实现：

```text
InteractionDetector
        │ 当前目标变化（只读实例事件）
        ▼
InteractionPromptPresenter
        │ 不可变 InteractionPromptData
        ▼
InteractionPromptView
```

本阶段没有实现 `InteractionExecutor`、`Interact()`、交互按键执行或任何具体机关。Unity `6000.3.21f1` 已完成真实验证：Sprint002B EditMode `11/11`、Sprint002B PlayMode `11/11`、完整 EditMode `55/55`、完整 PlayMode `37/37` 均通过，人工 PlayerSandbox 验收和稳定帧 Profiler 检查通过，Console Error 为 `0`。

## 2. 完成内容

### 2.1 只读 Prompt 数据

- `InteractionPromptData`
  - 只读值类型。
  - 包含稳定 `InteractionTargetId`、Prompt ID、动作文本 Key、MVP 回退文本、设备类型、Binding 显示文本和可见状态。
  - 支持值相等比较，使相同目标、设备和内容不会重复刷新 View。
  - 空值统一转换为安全空字符串；隐藏状态不能被误判为可显示状态。
- `InteractionPromptDefinition`
  - ScriptableObject 静态定义。
  - 保存稳定 Prompt ID、未来本地化 Key 和当前开发回退文本。
  - 不保存可见性、当前目标或其他局内运行状态。
- `IInteractionPromptSource` / `InteractionPromptSource`
  - 以独立只读组合组件把 Prompt Definition 连接到检测目标。
  - 未修改 Sprint002A 已通过 Gate 的 `IInteractable`。
  - UI 不识别 Door、Lever、Chest 或其他具体玩法类型。

### 2.2 Presenter

`InteractionPromptPresenter` 负责：

- 通过显式 `PlayerSpawner` 引用绑定当前本地 Sandbox Player。
- 订阅 Player 生成与当前 Detector 的目标变化实例事件。
- 在玩家已存在、玩家稍后生成、目标销毁、Detector 禁用和玩家重新生成时安全更新绑定。
- 仅在目标变化时解析 `IInteractionPromptSource`。
- 将目标语义、当前设备和已缓存 Binding 转换为不可变 Prompt 数据。
- 对键盘/鼠标和手柄设备事件进行本地显示切换。
- Presenter 或 View 禁用时清除脏显示状态，重新启用时根据当前 Detector 恢复。

Presenter 不读取 `Interact` Action 的执行状态，不启用该 Action，不向目标、Player 或 Detector 写入状态。

### 2.3 Binding 显示

`InteractionBindingDisplayProvider`：

- 复用现有 `WonderSquad.inputactions` 中的 `Gameplay/Interact`。
- 从 `Keyboard&Mouse` 和 `Gamepad` Binding Group 解析显示文本。
- 当前键鼠 Binding 为 `<Keyboard>/e`，手柄 Binding 为 `<Gamepad>/buttonWest`。
- 初始化时解析一次并缓存字符串，不在 `Update` 中重复解析。
- 不启用或消费 `Interact` Action。
- 缺少 Input Actions、Action 或有效 Binding 时返回稳定回退文本 `Unbound`，不抛异常。

设备切换使用 Input System 的设备事件，仅改变本地提示表现，不代表交互请求或权威结果。该事件是 Unity Input System 生命周期接口，不是项目全局事件总线。

### 2.4 View 与 UI

`InteractionPromptView`：

- 只接收 `InteractionPromptData`。
- 不引用 `IInteractable`，不查找目标，不执行交互。
- 相同数据不重复设置文本或激活状态。
- 隐藏时清空旧文本，避免目标切换后的脏内容。
- 禁用时立即恢复默认隐藏状态。

`PF_InteractionPrompt.prefab`：

- 使用 `com.unity.ugui` `2.0.0` 的 `Canvas`、`CanvasScaler`、`Image` 与 `Text`。
- 不使用 TextMeshPro。
- 包含背景容器、Binding 文本和动作文本。
- 默认隐藏，采用屏幕底部居中锚点和 `1920 × 1080` 参考分辨率。
- 没有 `GraphicRaycaster`，所有 Graphic 的 `raycastTarget` 均关闭，因此不阻挡 Player 输入或 UI 射线。
- 使用占位颜色、字体与布局，没有动画和音效。

### 2.5 目标生命周期

实现覆盖：

1. 无目标时隐藏。
2. 新目标进入时显示。
3. 目标离开范围时隐藏。
4. 目标被遮挡时隐藏。
5. 遮挡清除后恢复。
6. 当前目标切换时替换内容。
7. 目标销毁时安全隐藏。
8. Detector 禁用时通过空目标通知隐藏。
9. Presenter 或 View 禁用时清空显示。
10. 重新启用时读取当前 Detector 状态恢复显示。
11. Player 销毁或重新生成时解除旧订阅并绑定新实例。

为此只在 `InteractionDetector` 增加了去重的 `CurrentTargetChanged` 实例事件；Detector 仍不引用 UI、不读取 Input、不执行交互。

## 3. 新增文件

### Runtime / Interaction

- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/InteractionPromptData.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/InteractionPromptDefinition.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/IInteractionPromptSource.cs`
- `Client/Assets/WonderSquad/Runtime/Interaction/Prompt/InteractionPromptSource.cs`
- 上述目录、脚本对应 `.meta`

### Runtime / UI

- `Client/Assets/WonderSquad/Runtime/UI/Interaction/InteractionBindingDisplayProvider.cs`
- `Client/Assets/WonderSquad/Runtime/UI/Interaction/InteractionPromptPresenter.cs`
- `Client/Assets/WonderSquad/Runtime/UI/Interaction/InteractionPromptView.cs`
- 上述目录、脚本对应 `.meta`

### Assets

- `Client/Assets/WonderSquad/ScriptableObjects/Content/Interaction/InteractionPrompt_Default.asset`
- `Client/Assets/WonderSquad/ScriptableObjects/Content/Interaction/InteractionPrompt_Inspect.asset`
- `Client/Assets/WonderSquad/Prefabs/UI/PF_InteractionPrompt.prefab`
- 上述目录、资产对应 `.meta`

### Tests

- `Client/Assets/WonderSquad/Tests/EditMode/InteractionPromptEditModeTests.cs`
- `Client/Assets/WonderSquad/Tests/PlayMode/InteractionPromptPlayModeTests.cs`
- 对应 `.meta`

### Documents

- `Docs/01_Project/Sprint002B_Implementation_Plan.md`
- `Docs/01_Project/Sprint002B_Implementation_Report.md`
- `Docs/01_Project/Sprint002B_Review_Checklist.md`

## 4. 修改文件

- `Client/Assets/WonderSquad/Runtime/Interaction/Detection/InteractionDetector.cs`
  - 增加去重的只读当前目标变化事件。
- `Client/Assets/WonderSquad/Runtime/UI/WonderSquad.UI.asmdef`
  - 增加 Interaction、Player、Input System 与 uGUI 的单向外层依赖。
- `Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef`
- `Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef`
  - 增加 UI 与 uGUI 测试引用。
- `Client/Assets/WonderSquad/Prefabs/Interaction/PF_Interactable_Test.prefab`
  - 仅组合默认 Prompt Source/Definition。
- `Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`
  - 组合 Prompt UI Prefab和第二个只读测试目标。
- `Client/Packages/manifest.json`
- `Client/Packages/packages-lock.json`
  - 将已解析的 uGUI `2.0.0` 提升为直接依赖；没有改动其他包版本。
- `Docs/01_Project/Sprint002B_Preflight_Report.md`
- `CHANGELOG.md`

## 5. 程序集变化

```text
WonderSquad.Core
    ▲             ▲
    │             │
Player      Interaction
    ▲             ▲
    └──────┬──────┘
           │
          UI ──► Unity.InputSystem
           └──► UnityEngine.UI
```

允许依赖：

| Assembly | 直接依赖 |
|---|---|
| `WonderSquad.Core` | 无 |
| `WonderSquad.Player` | `WonderSquad.Core`、`Unity.InputSystem` |
| `WonderSquad.Interaction` | `WonderSquad.Core` |
| `WonderSquad.UI` | `WonderSquad.Core`、`WonderSquad.Interaction`、`WonderSquad.Player`、`Unity.InputSystem`、`UnityEngine.UI` |

未增加：

- Player → Interaction/UI
- Interaction → Player/UI
- Gameplay → UI
- Runtime → Tests/Editor
- 任何循环依赖

## 6. Package 变化

| Package | 修改前 | 修改后 | 说明 |
|---|---:|---:|---|
| `com.unity.ugui` | lockfile 中传递依赖 `2.0.0` | manifest 直接依赖 `2.0.0`，lockfile depth `0` | 精确锁定当前使用的 UI 包 |

没有升级、降级或修改其他 Package。

## 7. 自动化测试

### 7.1 已创建

- EditMode：11 项
  - PromptData 有效性、隐藏安全状态和只读字段。
  - Definition/Prefab 配置。
  - 非法配置拒绝。
  - Binding 安全回退与不启用 Action。
  - 相同数据去重与目标切换。
  - Presenter 启停状态。
  - View 不引用 `IInteractable`。
  - 程序集依赖无环。
  - uGUI 精确版本。
- PlayMode：11 项
  - 默认隐藏与进入范围显示。
  - 离开范围隐藏。
  - 遮挡隐藏与恢复。
  - 多目标确定性切换。
  - 目标销毁。
  - Detector 启停。
  - Presenter/View 启停。
  - 键鼠与手柄设备切换。
  - Prompt 不移动 Player。
  - 按 Interact 不改变目标状态。
  - 稳定 Prompt 不重复刷新且预热后循环无托管分配。

### 7.2 当前结果

| 验证项 | 结果 | 证据/限制 |
|---|---|---|
| C# 与 asmdef 编译 | 已通过 Unity 编译 | Unity `6000.3.21f1` 已生成更新后的 Interaction、UI、EditMode 和 PlayMode DLL |
| Sprint002B EditMode | 通过 | `11 Passed / 0 Failed` |
| Sprint002B PlayMode | 通过 | `11 Passed / 0 Failed` |
| 完整 EditMode | 通过 | `55 Passed / 0 Failed` |
| 完整 PlayMode | 通过 | `37 Passed / 0 Failed` |
| Console Error = 0 | 通过 | Unity 人工验收确认 |
| PlayerSandbox 人工验收 | 通过 | Prompt 生命周期、Binding、只读行为、输入与 Camera 回归均正常 |
| 稳定帧 GC | 通过 | Profiler 未发现 Prompt 系统持续 `GC.Alloc` |

实施阶段的独立 batch 尝试曾因本机 Licensing Client 初始化超时而未产生结果；该限制已由随后在正式 Unity 图形编辑器中完成的真实验证关闭，不影响最终验收结论。

## 8. 零分配与范围审计

静态扫描结果：

- Runtime Prompt 路径无 LINQ。
- 无 `Update` 中的字符串拼接、集合创建、组件查找或 Binding 解析。
- Binding 仅初始化时解析；设备类型不变时不重建数据。
- 目标未变化时 Detector 不触发事件，View 也不重复写入 Text。
- 无 `FindObjectOfType`、`FindFirstObjectByType`、`GameObject.Find`、Tag 或名称查找。
- 无 `Keyboard.current` 或 `Gamepad.current` 业务读取。
- 无 `InteractionExecutor`、`Interact()` 或目标状态写入。
- 测试代码中的 `FindObjectsByType` 仅用于断言场景组成，不进入 Player Build 或稳定帧生产路径。

未引入或修改：

- Door、Lever、Chest
- Puzzle、Inventory、Ability、Network
- Player Spawn、Player Input、Player Movement、Camera 生产逻辑
- Input Actions 资产
- 全局事件总线、静态 Player 引用
- 完整本地化、复杂动画、音效、触控 UI

## 9. Unity 人工验收结果

环境：Unity `6000.3.21f1`，`PlayerSandbox`。

- 无目标时 Prompt 默认隐藏：通过。
- 靠近目标后 Prompt 正确显示：通过。
- Binding 和动作文本显示正确：通过。
- 离开范围后 Prompt 隐藏：通过。
- 遮挡时隐藏、遮挡解除后恢复：通过。
- 多目标切换时内容稳定更新：通过。
- 键鼠 Binding 显示：通过。
- 手柄 Binding 显示或安全回退：通过。
- 按交互键不执行任何目标行为：通过。
- WASD 与 Camera 回归：通过。
- Prompt 不阻挡输入：通过。
- Console Error：`0`。
- 稳定帧未发现 Prompt 系统持续 `GC.Alloc`：通过。

## 10. 已知限制

- 本阶段使用英文开发回退文本；仅预留本地化 Key，未集成 Localization 系统。
- 手柄显示文本由 Input System 和当前平台决定，可能显示 `X`、`West Button` 或等价可读名称。
- 缺少有效绑定时显示 `Unbound`；本阶段没有配置修复 UI。
- 当前 Presenter 通过 Sandbox `PlayerSpawner` 选择唯一的本地玩家；未来联机必须由本地玩家选择/会话适配层替换该组合边界。
- 没有触控提示、复杂动画、音效、交互执行和网络同步。

## 11. 推荐 Commit

```text
feat(interaction): add read-only interaction prompt UI
```

## 12. 最终状态

# PASSED

Sprint002B 已通过 Unity 编译、专项测试、完整回归、PlayerSandbox 人工验收、Console 和稳定帧 GC 检查。可进入 Sprint002B Gate Review；本次不开始 Sprint002C。
