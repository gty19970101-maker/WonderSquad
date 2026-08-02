# Sprint002B Interaction Prompt Review Checklist

## 1. Review 信息

- 日期：2026-08-02
- Unity：`6000.3.21f1`
- 分支：`feature/sprint002b-interaction-prompt`
- 范围：只读 Interaction Prompt
- 当前状态：**PASSED**

标记说明：

- `[x]`：已通过代码、资产或配置审查。
- `[ ]`：尚未通过或仍需确认。
- `[N/A]`：明确不属于 Sprint002B。

## 2. 编译与包

- [x] Unity 已生成更新后的 `WonderSquad.Interaction.dll`。
- [x] Unity 已生成新的 `WonderSquad.UI.dll`。
- [x] Unity 已生成更新后的 EditMode/PlayMode Tests 程序集。
- [x] `manifest.json` 将 `com.unity.ugui` 精确锁定为 `2.0.0`。
- [x] `packages-lock.json` 解析 `com.unity.ugui` 为 `2.0.0`、depth `0`。
- [x] 未修改其他 Package 版本。
- [x] Unity Console Error 为 `0`。

## 3. 架构与职责

- [x] 数据流保持 Detector → Presenter → View。
- [x] `InteractionDetector` 不引用 UI。
- [x] Detector 只发布去重的当前目标变化，不执行交互。
- [x] Presenter 只转换和同步只读显示状态。
- [x] Presenter 不修改 Player、Detector 或目标。
- [x] View 不引用 `IInteractable`。
- [x] View 不查找目标。
- [x] View 只负责显示、隐藏和去重刷新。
- [x] `IInteractable` 未修改，仍为 Sprint002A 最小只读契约。
- [x] Prompt Definition/Source 不包含具体机关类型。
- [x] 没有全局事件总线或静态 Player/Target 引用。
- [x] 没有程序集循环依赖。

## 4. Prompt 数据

- [x] 包含稳定目标标识。
- [x] 包含 Prompt ID、动作文本 Key 与 MVP 回退文本。
- [x] 包含当前设备类型和 Binding 显示文本。
- [x] 包含可见状态。
- [x] `InteractionPromptData` 为只读值类型。
- [x] 空字符串与隐藏状态有安全处理。
- [x] 相同数据按值去重。
- [x] 目标切换产生不同 Prompt 数据。
- [x] ScriptableObject 只保存静态定义，不保存运行时状态。

## 5. Input Binding 显示

- [x] 复用现有 `Gameplay/Interact`。
- [x] 键鼠 Binding 来自 `Keyboard&Mouse` Group。
- [x] 手柄 Binding 来自 `Gamepad` Group。
- [x] 没有修改 Input Actions 资产。
- [x] 没有订阅 `Interact.performed` 执行业务。
- [x] 没有启用或禁用 `Interact` Action。
- [x] 没有使用 `Keyboard.current` 或 `Gamepad.current` 读取业务按键。
- [x] Binding 初始化后缓存，不在 Update 中解析。
- [x] 缺失 Binding 使用 `Unbound` 安全回退。
- [x] 键鼠设备事件能在 PlayerSandbox 更新显示。
- [x] 手柄设备事件能在 PlayerSandbox 更新显示或明确回退。

## 6. 生命周期

- [x] 无目标时输出隐藏状态。
- [x] 目标变化事件仅在引用实际变化时触发。
- [x] 目标离开、遮挡或 Detector 禁用时清除当前目标。
- [x] 目标销毁时使用 Unity Object 存活语义安全处理。
- [x] Presenter 成对订阅/取消订阅 PlayerSpawner、Detector、View 和 Input System。
- [x] Presenter 禁用时隐藏 View 并解除旧绑定。
- [x] View 禁用时清空显示状态。
- [x] Player 重新生成时重新绑定 Detector。
- [x] 目标进入、离开、遮挡和恢复的 PlayMode 行为通过。
- [x] 目标切换和销毁的 PlayMode 行为通过。
- [x] Detector、Presenter、View 禁用/启用回归通过。
- [x] 完整 PlayMode 回归覆盖 PlayerSpawner 生命周期，Presenter 重绑定边界未产生错误。

## 7. UI 与可用性

- [x] 使用 uGUI `Canvas`、`Image` 和 legacy `Text`，未引入 TextMeshPro。
- [x] 包含背景、Binding 文本和动作文本。
- [x] 默认隐藏。
- [x] 使用屏幕底部居中锚点和参考分辨率。
- [x] 不包含 `GraphicRaycaster`。
- [x] 所有 Graphic 的 `raycastTarget` 关闭。
- [x] 使用占位样式，没有复杂动画或音效。
- [x] `CanvasScaler` 与基础锚点配置有效；多分辨率像素级美术 QA 不属于本 Sprint Gate。
- [x] Prompt UI 不阻挡 WASD、Camera 或其他输入。

## 8. 性能

- [x] Runtime Prompt 代码不使用 LINQ。
- [x] Presenter 和 View 没有 Update 循环。
- [x] 稳定目标不重复发布事件。
- [x] 相同 Prompt 数据不重复设置 Text。
- [x] 不在稳定帧解析 Binding。
- [x] 不在稳定帧查找 Player、目标或组件。
- [x] 不在稳定帧创建集合。
- [x] PlayMode 的预热后 `GC.GetAllocatedBytesForCurrentThread` 测试通过。
- [x] Profiler 中稳定帧没有 Prompt 持续 `GC.Alloc`。

## 9. 自动化测试

### EditMode

- [x] 已创建 11 项 Sprint002B EditMode Tests。
- [x] 11 项 Sprint002B EditMode Tests 全部通过。
- [x] 完整 EditMode `55 Passed / 0 Failed`。

### PlayMode

- [x] 已创建 11 项 Sprint002B PlayMode Tests。
- [x] 11 项 Sprint002B PlayMode Tests 全部通过。
- [x] 完整 PlayMode `37 Passed / 0 Failed`。
- [x] Character Foundation 回归通过。
- [x] Sprint002A Detection 回归通过。
- [x] Console Error 为 `0`。

Unity 真实验证环境为 `6000.3.21f1`。专项与完整测试均为 `0 Failed`。

## 10. 范围审计

- [x] 未实现 `InteractionExecutor`。
- [x] 未调用或增加 `Interact()`。
- [x] 未实现按键执行交互。
- [x] 未实现 Door、Lever、Chest。
- [x] 未实现 Puzzle、Inventory、Ability、Network。
- [x] 未实现完整本地化。
- [x] 未实现复杂 UI 动画、音效或触控 UI。
- [x] 未修改 Player Spawn、Input、Movement 或 Camera 生产逻辑。
- [x] 未修改 Interaction Detection 的选择、距离、Layer 或遮挡规则。
- [x] 未开始 Sprint002C。

## 11. Unity 人工验收

- [x] Play 后 Prompt 默认隐藏。
- [x] 靠近目标显示正确 Binding 和动作文本。
- [x] 离开范围后隐藏。
- [x] 遮挡时隐藏，遮挡清除后恢复。
- [x] 两个目标之间切换内容稳定。
- [x] 按 `E` 和手柄 Interact 不改变任何目标状态。
- [x] WASD、Player Spawn、Movement 与 Camera 正常。
- [x] UI 不阻挡输入。
- [x] Console Error 为 `0`。
- [x] Profiler 稳定帧无 Prompt 持续 `GC.Alloc`。

## 12. 最终状态

# PASSED

代码、资产、程序集、专项测试、完整回归、人工验收与性能检查均已完成。Sprint002B 状态为 `PASSED`；是否进入 Sprint002C 仍须以 Gate Review 结论为准。
