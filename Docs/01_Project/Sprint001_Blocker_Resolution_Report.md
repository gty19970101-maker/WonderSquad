# Sprint001 阻塞项解决报告

## 1. 处理范围

本次只处理 Sprint001 Preflight 的两个阻塞项：

1. 统一 Unity 正式版本基线。
2. 消除 `P0ProjectSetup.cs` 自动或误操作覆盖 `PlayerSandbox.unity` 的风险。

没有实现 Player 移动、CameraFollow、PlayerInputHandler、Interaction、Inventory、Ability、Puzzle、Network 或其他 Sprint001 业务逻辑。

---

## 2. 最终结果

| 阻塞项 | 处理结果 | 验证状态 |
|---|---|---|
| Unity 版本冲突 | 已统一为 `6000.3.21f1` | 项目文件与文档已静态复检；该版本此前已通过 P0 Unity 实测 |
| P0 工具覆盖 PlayerSandbox | 已将自动与安全入口改为只创建缺失场景；强制重建改为独立确认菜单 | 代码与测试已静态验证；新增测试等待当前 Unity Editor 内运行 |

重新执行 Preflight 后的结论：

**CONDITIONAL GO**

条件是新增的四项 EditMode 测试在 Unity `6000.3.21f1` 中全部通过且 Console 无编译错误。满足后转为 **GO**；失败则为 **NO-GO**。

---

## 3. Unity 版本基线统一

### 3.1 决策

项目唯一正式 Unity Editor 版本基线：

```text
Unity 6000.3.21f1
Revision c02631ffc030
Series Unity 6.3 LTS
```

### 3.2 决策依据

- 工程的 `ProjectVersion.txt` 已锁定 `6000.3.21f1`。
- 当前项目资源、URP `17.3.0` 和 Input System `1.20.0` 来自 Unity 6 工程。
- P0 已由用户在 `6000.3.21f1` 完成真实编译、测试与资源配置验证。
- 请求中的目标版本字段仍是占位文本，只包含 `2022.3.62f1` 示例，没有形成有效的降级授权。
- 将 Unity 6 项目直接改写为 Unity 2022 会产生 Scene、Prefab、ProjectSettings、URP 和 Package 降级风险。

因此本次没有修改 `ProjectVersion.txt`，也没有执行跨大版本降级。

### 3.3 发现的冲突

| 文件或范围 | 原内容 | 问题 | 处理 |
|---|---|---|---|
| 旧 `Sprint001_Preflight_Report.md` | 将 `2022.3.62f1` 作为待选要求 | 与实际工程及 P0 基线冲突 | 明确其为未替换占位示例，正式基线固定为 `6000.3.21f1` |
| `Technical_Architecture_v1.1.md` | “统一锁定最新稳定补丁” | 补丁版本会随时间漂移 | 改为精确的 `6000.3.21f1` |
| `MVP_Development_Plan_v1.1.md` | “锁定 Unity LTS 补丁” | 未写明精确补丁 | 改为 `6000.3.21f1` |
| `Prototype_Task_Breakdown.md` | “Unity 6.3 LTS 最新稳定补丁” | 补丁版本会随时间漂移 | 改为 `6000.3.21f1` |
| 历史技术架构与影响分析 | Unity 6.3 LTS 或最新补丁建议 | 可能被误读为可自行选择 | 同步为 `6000.3.21f1` |
| 根目录 | 缺少 `README.md` | 无仓库级版本入口 | 新增 README，并声明唯一基线与迁移规则 |

### 3.4 检查过且无需修改

- `Client/ProjectSettings/ProjectVersion.txt`
- `Client/README.md`
- `CONTRIBUTING.md`
- `CODE_STYLE.md`
- `REVIEW_CHECKLIST.md`
- `CHANGELOG.md`
- `Project_Governance_Report.md`
- `Docs/01_Project/P0_Implementation_Report.md`
- `Docs/01_Project/P0_Review_Report.md`

这些文件已经明确使用 `6000.3.21f1`。

### 3.5 修改的版本文档

- `README.md`
- `Docs/00_Project/Development_Workflow.md`
- `Design/Technical_Architecture_v1.1.md`
- `Design/MVP_Development_Plan_v1.1.md`
- `Docs/01_Project/Prototype_Task_Breakdown.md`
- `Design/Technical_Architecture.md`
- `Design/Design_Change_Impact_v1.1.md`
- `Docs/01_Project/Sprint001_Preflight_Report.md`

---

## 4. P0ProjectSetup 安全修复

修改文件：

```text
Client/Assets/WonderSquad/Editor/P0ProjectSetup.cs
```

### 4.1 修复前风险

原有自动入口和菜单入口最终都会调用同一套场景重建逻辑。该逻辑通过 `NewSceneSetup.EmptyScene` 重建以下场景：

- Bootstrap
- PlayerSandbox
- GameplaySandbox
- RecoverySandbox

这会使 Sprint001 对 `PlayerSandbox.unity` 的后续合法修改在误运行菜单或重新初始化时丢失。

### 4.2 修复后的入口

#### 自动入口

`[InitializeOnLoadMethod]` 仍使用 SessionState 防重复调度，但场景模式被固定为：

```text
CreateMissingOnly
```

自动入口：

- BatchMode 不执行自动初始化。
- 进入或即将进入 Play Mode 时不执行。
- 场景存在时只记录 skip 日志。
- 场景缺失时才创建。
- 脚本编译与 Domain Reload 不存在强制重建路径。

#### 安全菜单

菜单：

```text
Wonder Squad/P0/Apply Project Setup (Safe)
```

显示确认说明后：

- 配置 P0 URP 与 Build Settings。
- 只创建缺失的 P0 场景。
- 不改变任何已存在场景。

#### 强制重建菜单

菜单：

```text
Wonder Squad/P0/Rebuild All P0 Scenes...
```

这是唯一允许覆盖现有 P0 场景的入口。执行前必须通过确认对话框，且对话框明确说明会覆盖 `PlayerSandbox` 并丢失已有改动。

### 4.3 幂等实现

新增公共 Editor 工具方法：

```text
EnsureP0SceneExists(scenePath, rootName)
```

行为：

- 参数无效时立即抛出明确异常。
- 场景存在时不加载、不保存、不修改，输出清晰 skip 日志并返回 `false`。
- 场景缺失时创建并返回 `true`。
- 创建过程使用 Additive 临时场景，保存后关闭，不替换用户当前打开的场景。
- 保存或关闭失败时抛出异常。
- 重复执行不会改变现有场景。

---

## 5. 新增 Editor 测试

新增文件：

```text
Client/Assets/WonderSquad/Tests/EditMode/P0ProjectSetupTests.cs
Client/Assets/WonderSquad/Tests/EditMode/P0ProjectSetupTests.cs.meta
```

测试覆盖：

1. 场景不存在时可以创建。
2. 场景存在时不会覆盖，场景文件内容保持一致。
3. 重复执行不会改变现有场景。
4. 自动入口固定使用 `CreateMissingOnly`，不会选择强制重建模式。

测试使用临时测试场景，并在每项测试前后清理。没有修改正式 P0 场景。

---

## 6. 验证记录

### 6.1 已完成

- 全仓库 Unity 版本声明检索。
- P0 工具全部调用入口检索。
- `NewSceneSetup.EmptyScene` 到安全/强制路径的调用关系检查。
- Editor、Runtime、Tests 程序集边界检查。
- `git diff --check`：通过。
- Sprint 范围检查：未发现 Gameplay 业务实现。
- Unity 可执行文件确认：`H:\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe`。

### 6.2 尚未完成

新增 EditMode 测试尚未在 Unity Test Runner 中实际完成。

原因：

- 项目当前已被用户的 Unity Editor 实例打开。
- 对同一路径启动第二个 Unity BatchMode 实例时未生成 Test Result XML，并以 Unity 返回码 1 结束。
- 为避免关闭用户 Editor、丢失未保存状态或破坏项目锁，没有强制终止现有 Unity 进程。

这不是代码失败证据，但在获得运行结果前不能标记为完整 GO。

### 6.3 Unity 内验收步骤

1. 回到当前打开的 Unity `6000.3.21f1`。
2. 等待资源刷新和脚本编译完成。
3. 确认 Console 没有编译错误。
4. 打开 Test Runner，选择 EditMode。
5. 运行 `WonderSquad.Tests.EditMode.P0ProjectSetupTests`。
6. 确认四项测试全部通过。
7. 检查版本控制，确认 `PlayerSandbox.unity` 没有被自动修改。

---

## 7. 最终文件清单

### 新增

- `README.md`
- `Client/Assets/WonderSquad/Tests/EditMode/P0ProjectSetupTests.cs`
- `Client/Assets/WonderSquad/Tests/EditMode/P0ProjectSetupTests.cs.meta`
- `Docs/01_Project/Sprint001_Blocker_Resolution_Report.md`

### 修改

- `Client/Assets/WonderSquad/Editor/P0ProjectSetup.cs`
- `Docs/00_Project/Development_Workflow.md`
- `Design/Technical_Architecture_v1.1.md`
- `Design/MVP_Development_Plan_v1.1.md`
- `Docs/01_Project/Prototype_Task_Breakdown.md`
- `Design/Technical_Architecture.md`
- `Design/Design_Change_Impact_v1.1.md`
- `Docs/01_Project/Sprint001_Preflight_Report.md`

### 明确未修改

- `Client/ProjectSettings/ProjectVersion.txt`
- 正式 P0 场景
- Player Runtime
- Input Action Asset
- Interaction、Inventory、Ability、Puzzle、Network 模块

---

## 8. 最终结论

版本冲突已消除，P0 场景覆盖路径已在代码层消除。

由于新增 Editor 测试尚未取得 Unity 实测结果，当前结论为：

**CONDITIONAL GO**

测试全部通过且 Console 无编译错误后转为：

**GO**

在此之前停止，不开始 Sprint001。
