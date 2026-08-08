# Sprint002D EditMode Test Fix Report

## 1. 结论

- Unity：`6000.3.21f1`
- 修复日期：2026-08-08
- 当前状态：**PASSED**

已修复 Interaction Probe 在 EditMode 中创建材质实例的生产代码路径，并补充多实例隔离测试。修复后已在 Unity Test Runner 与 PlayerSandbox 通过最终验证。

## 2. 原始失败

用户实测结果：

- 完整 EditMode：73 Tests，69 Passed，4 Failed。
- 失败组：InteractionProbeEditModeTests，7 Tests 中 4 Failed。
- 其他现有 EditMode 测试均通过。

Unity Console 报错为：调用 `Renderer.material` 会在 EditMode 实例化材质并泄漏到场景。

四个失败与 `InteractionProbeEditModeTests` 中四个创建 Probe fixture 的测试一一对应：默认状态、第一次执行、第二次执行和无效 Context。每个 fixture 都经过 `InteractionProbeVisualState.Configure`，该方法随即调用 `ApplyVisualState`，原实现访问了 `placeholderRenderer.material.color`。因此 Unity 报错使测试失败；状态、Result、RequestId 和契约断言本身不是已观察到的失败原因。

## 3. 修改文件

- `Client/Assets/WonderSquad/Runtime/Interaction/Diagnostics/InteractionProbeVisualState.cs`
- `Client/Assets/WonderSquad/Tests/EditMode/InteractionProbeEditModeTests.cs`
- `Docs/01_Project/Sprint002D_Implementation_Report.md`
- `Docs/01_Project/Sprint002D_Review_Checklist.md`
- `CHANGELOG.md`
- `Docs/01_Project/Sprint002D_EditMode_TestFix_Report.md`

未修改 Sprint002A–C 核心契约、InteractionDetector、InteractionExecutor、Prompt Runtime、Player、Input Actions、程序集定义或 Unity 资产。

## 4. 修复方式

`InteractionProbeVisualState` 现在为每个组件实例持有一个懒创建的 `MaterialPropertyBlock`，并在状态改变时：

1. 读取该 Renderer 当前 PropertyBlock。
2. 写入 `_BaseColor` 与 `_Color` 的显示颜色覆盖。
3. 把 PropertyBlock 重新设置回同一个 Renderer。

不再访问 `Renderer.material`，也不需要写入 `Renderer.sharedMaterial`。

这保留了 Runtime 的 Inactive / Active 独立颜色切换：`MaterialPropertyBlock` 附着于单个 Renderer，不会实例化材质，也不会修改共享 Material Asset。每个 Probe 的 Visual State 持有自己的 Block；两个实例各自覆盖其 Renderer 的属性，状态互不影响。

PropertyBlock 只在首次应用时分配一次，后续状态切换复用同一对象；它不在 Update 中运行，也不产生稳定帧持续 GC。

## 5. 测试调整

- 保留原有默认状态、单次/双次执行、Result、RequestId、无效 Context、Prefab 和架构边界测试。
- 新增 `ProbeVisualState_TwoInstancesKeepIndependentPropertyBlocks`：一个 Probe 激活后，断言另一个 Probe 仍为 Inactive，并分别读取两个 Renderer 的 `_BaseColor` PropertyBlock 值。
- 现有源码边界测试新增断言，禁止 Probe Runtime 出现 `.material` 访问。

Sprint002D EditMode 测试数量从 7 增至 8。

## 6. 静态检查

- 使用 Unity `6000.3.21f1` 自带 .NET Roslyn 与项目现有 Bee response 参数，在系统临时输出目录成功编译 `WonderSquad.Interaction`、`WonderSquad.Tests.EditMode` 与 `WonderSquad.Tests.PlayMode`；未覆盖 Unity `Library` 输出。
- 新视觉组件不包含 `.material` 或 `.sharedMaterial` 访问。
- 没有新增 Animator、Tween、Audio、VFX、UI、Network、全局 Manager、全局 EventBus 或通用抽象。
- 没有修改共享 Material Asset。
- 没有修改 Prefab、Scene 或任何 Sprint002A–C 生产逻辑。

## 7. Unity 修复后验证

1. Unity `6000.3.21f1` 编译正常，Console Error = `0`。
2. `WonderSquad.Tests.EditMode.InteractionProbeEditModeTests`：`8 Passed / 0 Failed`，未再出现 `Instantiating material`。
3. 完整 EditMode：`74 Passed / 0 Failed`。
4. Sprint002D PlayMode：`8 Passed / 0 Failed`；完整 PlayMode：`47 Passed / 0 Failed`。
5. PlayerSandbox 人工验收通过：两个 `PF_InteractionProbe` 可分别切换颜色和逻辑状态，任一实例状态变化不影响另一个实例；未发现 shared Material 污染、material instance leak 或稳定帧持续 `GC.Alloc`。

## 8. 最终结论

修复已完成并通过 Unity 最终验收。`MaterialPropertyBlock` 为每个 Renderer 保存独立属性覆盖，不会实例化材质或写入共享 Material Asset；Sprint002D 状态为 **PASSED**。当前不开始 Sprint003。
