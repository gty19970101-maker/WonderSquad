# Wonder Squad Project Governance Report

## 1. 结果

Wonder Squad 项目治理文档库已经建立。所有规范以以下基线为准：

- `Design/WonderSquad_Design_Document_v1.1.md`
- `Design/Technical_Architecture_v1.1.md`
- `Design/MVP_Development_Plan_v1.1.md`

本次只创建规范和流程文档，没有生成或修改 Unity 业务代码。

## 2. 新增规范

| 文件 | 职责 |
|---|---|
| `CONTRIBUTING.md` | 规定项目目标、开发流程、Sprint、文档、Unity、禁止事项和 Review 门禁 |
| `CHANGELOG.md` | 使用 Keep a Changelog 记录未发布变化和 `v0.1-P0` |
| `CODE_STYLE.md` | 规定 C#、Unity 类型、命名、注释、目录和架构边界 |
| `BRANCH_STRATEGY.md` | 规定 main、develop、feature、bugfix、hotfix、release 的生命周期 |
| `COMMIT_RULE.md` | 规定 Conventional Commits 和 Wonder Squad 示例 |
| `REVIEW_CHECKLIST.md` | 提供每个 Sprint 合并前的强制检查清单 |
| `Docs/00_Project/Development_Workflow.md` | 串联设计、实现、测试、Review、Git、合并和发布 |
| `Project_Governance_Report.md` | 汇总治理成果和 Sprint001 前置准备 |

## 3. 后续开发必须遵守的规则

1. 使用 Unity `6000.3.21f1`，未经验证不得升级。
2. 每个 Sprint 只实现批准范围，必须写明非目标。
3. 代码、测试、Unity 资源、实施报告和 CHANGELOG 同步交付。
4. Runtime、Editor、Tests 必须隔离。
5. Assembly Definition 依赖必须符合技术架构且保持无环。
6. Gameplay 不依赖 UI、Editor、具体网络 SDK 或场景对象名称。
7. 可调参数和静态内容使用 Settings/Definition，不使用 Magic Number。
8. 禁止 God Class、万能 Manager、跨层直接访问和隐式全局状态。
9. ScriptableObject 不保存局内可变权威状态。
10. 不在未批准 Sprint 中提前接入 Interaction、Inventory、Puzzle、Ability、Network 或其他后续系统。
11. 所有合并必须有 Unity 编译和测试证据。
12. 所有 Commit 使用 Conventional Commits。
13. 功能开发不直接提交到 `main`。
14. 单人开发也执行相同的自审和合并门禁。

## 4. 当前 Git 状态观察

检查时：

- 当前分支：`feature/sprint001-character-controller`
- 本地存在：`main`
- 远程存在：`origin/main`
- 尚未发现 `develop`
- 工作区在创建治理文档前为干净状态

按本治理规范，正式 Sprint 开发的标准基线应为 `develop`。当前已经提前建立 Sprint001 feature 分支，因此需要在开始业务实现前明确基线处理方式。

## 5. 开始 Sprint001 前仍需完成的准备工作

### 阻塞准备

1. 从最新 `main` 创建并推送 `develop`。
2. 决定当前 `feature/sprint001-character-controller`：
   - 如果它直接基于最新 `main` 且尚无业务改动，可在创建 `develop` 后将其基线对齐到 `develop`。
   - 如果已经包含业务改动，先 Review 差异，再安全迁移；不得丢失提交。
3. 将本次治理文档作为独立 docs Commit 提交，建议：

   ```text
   docs(project): establish project governance
   ```

4. 确认远程仓库保护规则：
   - `main` 禁止直接推送。
   - 合并要求 Review Checklist 和必要测试。

### Sprint001 Definition of Ready

1. 冻结 Sprint001 目标为 Character Controller，不开始整个 P1。
2. 明确只实现：
   - 自动出生
   - CharacterController
   - New Input System
   - Walking、Running 预留、Rotation、Gravity、Ground Check
   - 可配置第三人称跟随 Camera
   - Player Prefab、MovementSettings、EditMode 和 PlayMode 测试
3. 明确不实现：
   - Interaction
   - Inventory
   - Puzzle
   - Ability
   - Network
   - UI
4. 确认 Player Assembly 只依赖 Core/Contracts 和所需 Unity 官方程序集。
5. 确认所有移动与摄像机可调参数进入 `MovementSettings`。
6. 确认 PlayerSandbox、键鼠、手柄和测试验收步骤。
7. 创建 Sprint001 实施报告文件或先建立报告骨架。

完成以上准备后，才开始 Sprint001 业务实现。
