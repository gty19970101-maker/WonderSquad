# P0ProjectSetupTests 测试修复报告

## 1. 修复范围

本次只修改：

```text
Client/Assets/WonderSquad/Tests/EditMode/P0ProjectSetupTests.cs
```

没有修改：

- `Client/Assets/WonderSquad/Editor/P0ProjectSetup.cs`
- `Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity`
- Player、Camera、Input 或其他 Sprint001 业务代码
- Interaction、Inventory、Ability、Puzzle、Network

---

## 2. 原失败原因

Unity EditMode Test Runner 首次实测时，四项测试中两项失败，错误为：

```text
Overwriting the same path as another open scene is not allowed.
```

根因是测试场景生命周期没有完全闭合：

1. 哨兵场景使用 `NewSceneMode.Single` 创建并保存后，仍然作为已打开 Scene 保留在 SceneManager 中。
2. TearDown 只通过 AssetDatabase 删除场景资源，没有先释放 Unity 内存中的已打开 Scene 引用。
3. 下一项测试再次向相同路径保存场景时，Unity 判断该路径仍被一个打开的 Scene 占用并拒绝覆盖。
4. 所有测试共用一个固定场景路径，进一步放大了测试之间的状态污染。

生产代码的“场景存在则跳过、场景缺失才创建”逻辑不是此次失败原因。

---

## 3. 修改内容

### 3.1 独立临时目录

测试使用专用根目录：

```text
Assets/WonderSquad/Tests/EditMode/P0ProjectSetupTestTemp/
```

每项测试在该目录下创建一个新的 GUID 子目录，并分别保存：

```text
IsolationScene.unity
TestScene.unity
```

不同测试不再共享场景文件路径。

### 3.2 Scene 隔离

每项测试开始前：

1. 使用一个新的空 Single Scene 释放上一项测试可能遗留的 Scene 引用。
2. 删除上一项测试的专用临时目录。
3. 创建当前测试的唯一目录。
4. 创建并保存当前测试专属的 `IsolationScene.unity`。

这样生产代码使用 Additive Scene 创建测试场景时，不会受到 Unity Test Runner 默认未保存 Untitled Scene 的限制。

### 3.3 正确释放测试场景

哨兵场景改为：

- 使用 `NewSceneMode.Additive` 创建。
- 将哨兵对象明确移动到测试 Scene。
- 保存后立即调用 `EditorSceneManager.CloseScene`。

TearDown 会：

1. 用新的空 Single Scene 释放全部测试 Scene 引用。
2. 验证当前只剩一个未保存的隔离 Scene。
3. 通过 AssetDatabase 删除测试专用临时根目录。
4. 刷新 AssetDatabase。

测试完成后没有保留临时 `.unity`、目录或 `.meta` 资源。

### 3.4 Unity NUnit 兼容调整

初次批处理编译发现当前 Unity 内置 NUnit 不提供 `NonParallelizable` 属性，因此移除了该属性。

测试隔离不依赖该属性：

- 每项测试使用唯一 GUID 路径。
- SetUp/TearDown 完整重置 Scene 生命周期。
- Unity EditMode Test Runner 对当前测试夹具顺序执行。

---

## 4. Unity 实测结果

运行环境：

```text
Unity Editor: 6000.3.21f1
Test Platform: EditMode
Test Filter: WonderSquad.Tests.EditMode.P0ProjectSetupTests
```

最终结果：

```text
Total: 4
Passed: 4
Failed: 0
Skipped: 0
Duration: 1.8897446 seconds
```

| 测试 | 结果 |
|---|---|
| `AutomaticSetup_UsesCreateMissingOnlyMode` | 通过 |
| `EnsureP0SceneExists_WhenMissing_CreatesScene` | 通过 |
| `EnsureP0SceneExists_WhenPresent_DoesNotOverwriteScene` | 通过 |
| `EnsureP0SceneExists_WhenRepeated_DoesNotChangeExistingScene` | 通过 |

Unity Test Runner 结果文件：

```text
Client/TestResults/P0ProjectSetupTests_Rerun.xml
```

关键验证证据：

- 缺失场景成功创建。
- 已存在场景输出 skip 日志且内容不变。
- 重复执行先创建、后跳过。
- 自动入口保持 `CreateMissingOnly`。
- 测试程序集编译成功。

---

## 5. 清理与范围检查

- 临时目录 `P0ProjectSetupTestTemp` 已由测试 TearDown 删除。
- `PlayerSandbox.unity` 未被修改。
- `P0ProjectSetup.cs` 本次没有修改。
- 未新增 Player、Camera、Input、Interaction、Inventory、Ability、Puzzle 或 Network 代码。
- `git diff --check` 无空白错误。

---

## 6. GO 结论

此前 `Sprint001_Preflight_Report.md` 的唯一剩余条件是：

1. Unity 无编译错误。
2. `P0ProjectSetupTests` 四项全部通过。
3. `PlayerSandbox.unity` 未被自动修改。

本次 Unity 实测已满足以上条件。

最终结论：

**GO：可以开始 Sprint001。**

本次工作到此停止，没有开始 Sprint001 业务实现。
