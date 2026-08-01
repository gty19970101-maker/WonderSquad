# Wonder Squad Development Workflow

## 1. 目的

本流程把游戏设计、技术设计、系统规则、Sprint 实现、Unity 验证和 Git 交付连接为一条可追踪链路，避免代码先于设计、Sprint 范围失控或文档与实现分离。

适用于个人开发和 2～3 人小团队。

项目唯一正式 Unity Editor 版本基线为 **Unity `6000.3.21f1`（Unity 6.3 LTS）**。Implementation、Unity Test、Code Review 和构建必须使用该精确版本；版本变更必须作为独立迁移任务执行影响分析与 P0 回归，不得在功能 Sprint 中顺带升级或降级。

## 2. 总流程

```text
Game Design
↓
Technical Design
↓
System Design
↓
Sprint
↓
Implementation
↓
Unity Test
↓
Code Review
↓
Git Commit
↓
Merge
↓
Release
```

任何一步发现上游假设错误，都返回对应上游步骤修正文档，不在下游临时硬编码规避。

## 3. 各阶段输入、输出与文档

| 阶段 | 主要工作 | 必须检查或更新的文档 | 进入下一步的门禁 |
|---|---|---|---|
| Game Design | 定义体验、玩家规则、MVP 范围和非目标 | `WonderSquad_Design_Document_v1.1.md`、关卡设计、CHANGELOG | 目标可验证，范围无冲突 |
| Technical Design | 决定 Unity、模块、数据、网络权威、保存和依赖 | `Technical_Architecture_v1.1.md`、影响分析、技术规则文档 | 架构能支持设计，依赖无环 |
| System Design | 把规则细化为状态、流程、接口、同步和验收 | `Docs/02_Gameplay/`、`Docs/03_Level/`、`Docs/04_Technical/`、`Docs/05_Data/` | 系统边界和测试条件明确 |
| Sprint | 选择最小可交付切片，明确非目标和依赖 | `Prototype_Task_Breakdown.md`、Sprint 说明或计划 | Definition of Ready 满足 |
| Implementation | 编写代码、资产、配置和自动化测试 | 代码、Prefab、Scene、ScriptableObject、Sprint 实施报告草稿 | 编译通过，未越界 |
| Unity Test | 执行 EditMode、PlayMode、手工场景和必要构建 | Sprint 实施报告中的验证记录、缺陷记录 | 阻塞测试全部通过 |
| Code Review | 检查范围、架构、质量、资产和文档 | `REVIEW_CHECKLIST.md`、相关设计文档 | 阻塞意见关闭 |
| Git Commit | 形成原子、可追踪提交 | `COMMIT_RULE.md`、CHANGELOG | Commit 合规，工作区无意外文件 |
| Merge | 合并到正确目标分支 | `BRANCH_STRATEGY.md`、PR 记录 | 目标分支验证通过 |
| Release | 冻结版本、构建、Tag 和发布记录 | CHANGELOG、版本报告、已知问题 | Release 验收通过并可回退 |

## 4. Game Design

需要回答：

- 该变化服务哪个核心体验？
- 是否仍满足 2～4 人、10～20 分钟、独立小地图？
- 是否支持不开语音？
- 是否形成角色硬锁？
- 失败后是否仍能继续游戏？
- 是否属于当前 MVP？

若改变这些答案，必须先更新总设计与影响分析。

## 5. Technical Design

需要回答：

- 哪个模块拥有状态与规则？
- 编译时依赖是否符合矩阵？
- 哪些结果未来由主机决定？
- 哪些数据仅本地保存？
- 是否使用数据定义，还是确有理由写成代码？
- 是否为未来编辑器过度设计？

新增 SDK、后端、跨模块引用、网络权威变化或数据版本变化必须经过独立 Review。

## 6. System Design

系统文档至少明确：

- 系统目标和边界。
- 核心流程与主要状态。
- 与其他系统的依赖。
- 联机同步要求。
- MVP 与非 MVP 范围。
- 验收标准。
- 风险和待确认事项。

代码不得实现系统文档明确列为非 MVP 的内容。

## 7. Sprint

### Definition of Ready

Sprint 开始前必须满足：

- 目标可以一句话说明。
- 包含明确的“做”和“不做”。
- 依赖任务已经完成。
- 计划修改的模块和场景已确定。
- 验收步骤可执行。
- 测试类型已确定。
- 分支已从正确基线创建。

### Sprint 执行规则

- 优先完成可运行的最小纵向切片。
- 每完成一个独立行为就补测试，不把测试集中拖到最后。
- 发现设计冲突时暂停实现并记录。
- 不跨 Sprint 预先实现 Interaction、Inventory、Puzzle、Network 等后续系统。

### Definition of Done

- 批准范围全部完成。
- Unity 编译无错误。
- 相关自动测试和手工测试通过。
- Console 无未说明 Error。
- 架构和程序集检查通过。
- 文档、实施报告和 CHANGELOG 已更新。
- Review Checklist 完成。
- 代码已提交并可安全合并。

## 8. Implementation

- 按 `CODE_STYLE.md` 编码。
- 所有可调玩法参数放入 Settings 或 Definition。
- Runtime、Editor、Tests 分离。
- Prefab 和 Scene 使用明确职责，避免把所有系统挂到一个对象。
- 先保留简单清晰的离线边界，再在批准的联机 Sprint 中接入 Fusion。
- 临时模型、UI 和音效必须明确标记为占位，不能混淆为完成资产。

实现过程中同步维护 Sprint 实施报告的文件清单和限制，避免结束时依赖记忆补写。

## 9. Unity Test

最低验证顺序：

1. Unity 完整编译。
2. EditMode 测试。
3. ContentValidation 测试。
4. 相关 PlayMode 测试。
5. 目标 Sandbox 手工验证。
6. 键鼠与手柄验证（涉及输入时）。
7. Development Build（涉及启动、场景、平台或资源配置时）。

测试失败必须记录复现步骤、预期、实际和修复结果。修复缺陷后增加回归测试。

## 10. Code Review

Reviewer 按以下顺序检查：

1. Sprint 范围与非目标。
2. 游戏设计和 MVP 边界。
3. 模块职责和程序集依赖。
4. 状态、数据和权威所有权。
5. 实现正确性与错误恢复。
6. 测试覆盖。
7. Unity 资源和 `.meta`。
8. 文档与 CHANGELOG。

详细清单见根目录 `REVIEW_CHECKLIST.md`。

## 11. Git Commit

- Commit 使用 `COMMIT_RULE.md`。
- 每个 Commit 原子且可解释。
- 不提交 Unity 生成缓存和本机构建。
- 资源与 `.meta` 同时提交。
- Sprint 完成前允许多个小 Commit；Merge 时按分支策略整理历史。

## 12. Merge

- feature、bugfix、docs 合并到 `develop`。
- release、hotfix 按 `BRANCH_STRATEGY.md` 进入 `main` 并同步回 `develop`。
- 合并前解决冲突并重跑相关验证。
- 合并后删除短期分支。
- 未通过门禁的 Sprint 不合并。

## 13. Release

Release 必须：

- 从经过验证的 `develop` 建立 release 分支。
- 冻结功能，只修复发布阻塞问题。
- 更新 CHANGELOG、版本号和发布报告。
- 完成目标平台构建与冒烟测试。
- 合并到 `main` 并创建 Tag。
- 将 release 修复同步回 `develop`。

MVP 发布还必须验证：2～4 人、10～20 分钟、无麦可通关、缺角阵容替代解法、欢乐失败恢复和《沉睡森林》完整流程。

## 14. 变更回流规则

| 发现的问题 | 返回阶段 |
|---|---|
| 体验目标错误或 MVP 范围变化 | Game Design |
| 技术选型或权威边界不支持设计 | Technical Design |
| 状态、流程或系统职责不清 | System Design |
| Sprint 太大或依赖未完成 | Sprint |
| 实现缺陷 | Implementation |
| 测试缺口 | Unity Test |
| 质量或架构问题 | Code Review |
| Commit/分支不合规 | Git Commit / Merge |

禁止用临时跨层访问、硬编码或关闭测试来绕过回流。
