# Sprint001 Character Controller 实施前检查报告

## 1. 复检结论

**CONDITIONAL GO：两个设计与实现阻塞项已经消除；新增 Editor 测试须在当前已打开的 Unity Editor 中完成运行验证后，方可开始 Sprint001。**

本报告于阻塞修复后重新执行 Sprint001 Preflight。当前没有发现需要继续修改 Unity 版本、P0 场景、程序集或 Sprint 范围的设计阻塞。

唯一剩余条件：

- 当前项目已被一个 Unity Editor 实例打开，第二个批处理实例不能同时取得项目锁，因此新增的 `P0ProjectSetupTests` 尚未获得 Unity Test Runner 的实际结果。
- 在 Unity `6000.3.21f1` 完成无编译错误和四项新增 EditMode 测试通过后，本结论自动转为 **GO**。
- 若出现编译错误或任一测试失败，结论恢复为 **NO-GO**，必须仅在 P0 工具范围内修复。

不得在满足上述条件前开始 Sprint001 业务实现。

---

## 2. 检查基线

已重新检查并遵循：

- `CONTRIBUTING.md`
- `CODE_STYLE.md`
- `BRANCH_STRATEGY.md`
- `COMMIT_RULE.md`
- `REVIEW_CHECKLIST.md`
- `Docs/00_Project/Development_Workflow.md`
- `Design/WonderSquad_Design_Document_v1.1.md`
- `Design/MVP_Development_Plan_v1.1.md`
- `Design/Technical_Architecture_v1.1.md`
- `Docs/01_Project/Prototype_Task_Breakdown.md`
- `Docs/02_Gameplay/Player_System.md`
- `Docs/02_Gameplay/Interaction_System.md`
- `Docs/04_Technical/Network_Authority_Rules.md`

`Docs/01_Project/P1_Sprint_Plan.md` 仍不存在。Sprint001 范围继续以用户批准的 Sprint001 Character Controller 说明、原型任务分解和本报告为准。

---

## 3. Git 复检

| 检查项 | 结果 | 说明 |
|---|---|---|
| 当前分支 | 通过 | `feature/sprint001-character-controller-v2` |
| develop 基线 | 通过 | 修复前 HEAD 与已获取的 `origin/develop` 均为 `15a3bcdf2825d2b592edecd22b625d25c7070066`，Ahead/Behind 为 `0/0` |
| 修复前工作区 | 通过 | 首次 Preflight 时工作区干净 |
| 当前工作区 | 预期变更 | 仅包含本次版本治理、P0 工具、Editor 测试和报告变更；不是意外脏文件 |
| 分支 upstream | 非阻塞 | 当前 feature 分支尚未配置 upstream，首次推送时设置 |

---

## 4. Unity 版本基线复检

### 4.1 最终基线

项目唯一正式 Unity Editor 版本为：

```text
6000.3.21f1
```

对应系列为 Unity 6.3 LTS。

选择依据：

- `Client/ProjectSettings/ProjectVersion.txt` 已是 `6000.3.21f1 (c02631ffc030)`。
- 当前 URP `17.3.0`、Input System `1.20.0` 及序列化资源来自该工程基线。
- P0 已由用户在 `6000.3.21f1` 完成真实编译、EditMode、PlayMode、Bootstrap、Input System 和 URP 验证。
- 没有执行跨大版本降级，也没有仅修改 `ProjectVersion.txt`。

用户请求中的“【在这里填写最终确认版本，例如 2022.3.62f1】”是未替换的占位文本，不构成有效的版本确认。若未来确需迁移到其他版本，必须建立独立迁移任务并重新执行完整 P0 验收。

### 4.2 已统一的位置

| 位置 | 处理结果 |
|---|---|
| `Client/ProjectSettings/ProjectVersion.txt` | 已一致，无修改 |
| 根 `README.md` | 新增正式版本声明与迁移规则 |
| `Client/README.md` | 已一致，无修改 |
| `CONTRIBUTING.md` | 已一致，无修改 |
| `CODE_STYLE.md` | 已一致，无修改 |
| `REVIEW_CHECKLIST.md` | 已一致，无修改 |
| `Docs/00_Project/Development_Workflow.md` | 增加精确版本及功能 Sprint 禁止顺带迁移规则 |
| `Design/Technical_Architecture_v1.1.md` | 将“最新稳定补丁”收敛为 `6000.3.21f1` |
| `Design/MVP_Development_Plan_v1.1.md` | 将模糊 Unity LTS 补丁收敛为 `6000.3.21f1` |
| `Docs/01_Project/Prototype_Task_Breakdown.md` | 将“最新稳定补丁”收敛为 `6000.3.21f1` |
| P0 实施与审查报告 | 已一致，无修改 |
| 本报告 | 删除旧版本二选一，将正式基线固定为 `6000.3.21f1` |
| 历史技术架构与影响分析 | 同步精确版本，避免历史文件继续给出浮动建议 |

### 4.3 冲突与处理结果

| 发现项 | 类型 | 处理 |
|---|---|---|
| 旧 Preflight 引用 `2022.3.62f1` | 明确冲突 | 已移除为候选基线；记录为未替换占位示例 |
| 技术架构写“最新稳定补丁” | 模糊且可漂移 | 已替换为 `6000.3.21f1` |
| 原型任务写“Unity 6.3 LTS 最新稳定补丁” | 模糊且可漂移 | 已替换为 `6000.3.21f1` |
| MVP 计划只写“Unity LTS 补丁” | 不够精确 | 已替换为 `6000.3.21f1` |

版本阻塞项结论：**已解决。**

---

## 5. P0ProjectSetup 场景保护复检

### 5.1 全部触发入口

| 入口 | 行为 | 是否可覆盖现有场景 |
|---|---|---|
| `[InitializeOnLoadMethod]` 自动入口 | 仅在非 BatchMode、Session 未处理、非 Play Mode 且 URP 缺失时调度安全设置 | 否 |
| `Wonder Squad/P0/Apply Project Setup (Safe)` | 显示确认后配置 URP、构建列表，并只创建缺失场景 | 否 |
| `ApplyFinalProjectSetup()` 公共安全入口 | 只使用 `CreateMissingOnly` | 否 |
| `Wonder Squad/P0/Rebuild All P0 Scenes...` | 显示明确的破坏性确认对话框后重建四个场景 | 是，仅限显式确认 |

不存在其他调用 `NewSceneSetup.EmptyScene` 的自动路径。

### 5.2 生命周期安全

| 场景 | 结果 |
|---|---|
| Unity 启动 | 自动模式固定为 `CreateMissingOnly`，现有场景被跳过 |
| 脚本重新编译 | Domain Reload 后同样进入安全模式，不能重建现有场景 |
| Domain Reload | SessionState 防止同一会话重复调度；即使再次执行也保持幂等 |
| 进入 Play Mode | 自动回调在 `isPlayingOrWillChangePlaymode` 时返回 |
| 自动项目初始化 | 只创建缺失场景 |
| 手动安全设置 | 只创建缺失场景 |
| 手动强制重建 | 独立菜单、明确说明会丢失改动、必须用户确认 |

### 5.3 幂等规则

`EnsureP0SceneExists(scenePath, rootName)`：

1. 校验路径与根节点名称。
2. 使用 AssetDatabase 与文件存在性检查判断场景是否已经存在。
3. 场景存在时返回 `false`，不打开、不保存、不修改，并输出包含场景路径的 skip 日志。
4. 场景不存在时创建、保存并返回 `true`。
5. 重复执行对已存在场景无副作用。

`PlayerSandbox.unity` 只有在用户选择 `Rebuild All P0 Scenes...` 并确认后才可能被覆盖。

场景覆盖阻塞项结论：**代码层已解决。**

---

## 6. Editor 测试

新增：

```text
Client/Assets/WonderSquad/Tests/EditMode/P0ProjectSetupTests.cs
```

覆盖：

| 测试 | 验证内容 |
|---|---|
| `EnsureP0SceneExists_WhenMissing_CreatesScene` | 场景不存在时成功创建 |
| `EnsureP0SceneExists_WhenPresent_DoesNotOverwriteScene` | 已存在场景内容保持逐字节一致 |
| `EnsureP0SceneExists_WhenRepeated_DoesNotChangeExistingScene` | 重复执行不改变场景 |
| `AutomaticSetup_UsesCreateMissingOnlyMode` | 自动入口模式固定为只创建缺失场景 |

静态检查结果：

- 测试位于既有 `WonderSquad.Tests.EditMode` Editor-only 测试程序集。
- 该程序集已经引用 `WonderSquad.Editor` 和 NUnit TestAssemblies。
- 未增加 Runtime 或 Gameplay 程序集依赖。
- `git diff --check` 无空白错误。

运行状态：

- 使用已安装的 `6000.3.21f1` 发起批处理测试。
- 因同一项目当前已被 Unity Editor 打开，批处理实例在取得项目后返回码 1，未生成测试结果 XML。
- 没有关闭用户正在运行的 Editor，也没有冒险复制或降级工程。
- 因此四项新增测试标记为：**已静态验证但未运行验证**。

---

## 7. 其余 Sprint001 Preflight 复检

| 检查项 | 结果 | 说明 |
|---|---|---|
| New Input System | 通过 | `1.20.0`、`activeInputHandler: 1`、Gameplay Map、Move/Look、键鼠与手柄配置仍在 |
| URP | 通过 | `17.3.0`；Graphics 与所有 Quality 档位仍引用 P0 URP Asset |
| Player 程序集 | 通过 | `WonderSquad.Player` 只引用 Core；Sprint001 时可增加官方 `Unity.InputSystem` 引用 |
| Editor/Runtime 隔离 | 通过 | P0 修复与测试仅在 Editor 程序集 |
| 重复 Player 实现 | 通过 | 未发现 PlayerController、CharacterController 或 Player Prefab |
| 重复 Camera 实现 | 通过 | 只有 P0 固定验证 Camera，无 CameraFollow |
| 重复 Input 实现 | 通过 | 只有共享 Input Action Asset 和常量，无 PlayerInputHandler |
| 验证场景 | 通过 | `PlayerSandbox.unity` 仍是最合适的 Sprint001 验证场景 |
| 范围隔离 | 通过 | 本次没有引入 Interaction、Inventory、Ability、Puzzle、Network、Player 移动、CameraFollow 或输入处理 |

---

## 8. Sprint001 计划文件清单

以下只是重新确认后的计划，当前尚未创建或实现：

### 计划新增

```text
Client/Assets/WonderSquad/Runtime/Player/Camera/CameraFollow.cs
Client/Assets/WonderSquad/Runtime/Player/Input/PlayerInputHandler.cs
Client/Assets/WonderSquad/Runtime/Player/Movement/GroundDetector.cs
Client/Assets/WonderSquad/Runtime/Player/Movement/MovementSettings.cs
Client/Assets/WonderSquad/Runtime/Player/Movement/PlayerController.cs
Client/Assets/WonderSquad/Runtime/Player/Spawning/OfflinePlayerSpawner.cs
Client/Assets/WonderSquad/Prefabs/Player/Player.prefab
Client/Assets/WonderSquad/ScriptableObjects/Configuration/MovementSettings.asset
Client/Assets/WonderSquad/Tests/EditMode/MovementSettingsTests.cs
Client/Assets/WonderSquad/Tests/PlayMode/PlayerControllerPlayModeTests.cs
Docs/01_Project/Sprint001_Implementation_Report.md
```

### 计划修改

```text
Client/Assets/WonderSquad/Runtime/Player/WonderSquad.Player.asmdef
Client/Assets/WonderSquad/Tests/EditMode/WonderSquad.Tests.EditMode.asmdef
Client/Assets/WonderSquad/Tests/PlayMode/WonderSquad.Tests.PlayMode.asmdef
Client/Assets/WonderSquad/Scenes/Tests/PlayerSandbox.unity
CHANGELOG.md
```

Sprint001 不再需要修改 `P0ProjectSetup.cs`；其安全边界已在本次阻塞修复中完成。

明确禁止引入：

```text
Interaction
Inventory
Ability
Puzzle
Network
```

---

## 9. 进入 Sprint001 的条件

在当前 Unity Editor 中完成：

1. 等待脚本导入和编译结束，确认 Console 无编译错误。
2. 运行 EditMode 测试：
   - `WonderSquad.Tests.EditMode.P0ProjectSetupTests`
3. 确认四项测试全部通过。
4. 确认 `PlayerSandbox.unity` 的版本控制内容未发生自动变化。

全部满足后：

**GO：可以开始 Sprint001。**

当前最终结论：

**CONDITIONAL GO。**
