# Contributing to Wonder Squad

## 项目目标

Wonder Squad 是一款面向 2～4 人的合作幻想冒险解谜游戏。第一阶段 MVP 只验证以下核心体验是否有趣：

- 多人合作与分工。
- 不同角色能力参与环境解谜。
- 同一问题存在多种解决方案。
- 不开语音也能完成合作。
- 失败后通过欢乐状态、自动恢复或队友救援继续游戏。

MVP 只制作一张 10～20 分钟的独立小地图《沉睡森林》，不建设连续开放大世界、商城、氪金、排行榜、公会、复杂基地、复杂战斗或玩家关卡编辑器。

## 设计与技术基线

开发前必须阅读并遵守：

1. `Design/WonderSquad_Design_Document_v1.1.md`
2. `Design/Technical_Architecture_v1.1.md`
3. `Design/MVP_Development_Plan_v1.1.md`
4. `Docs/02_Gameplay/`、`Docs/03_Level/`、`Docs/04_Technical/`、`Docs/05_Data/` 中与改动相关的系统文档
5. 当前 Sprint 任务说明和验收标准

发生冲突时，先停止实现并更新设计或影响分析。不得用代码默默覆盖设计决策。

## 开发流程

所有改动遵循：

```text
确认设计与范围
→ 建立或更新技术/系统设计
→ 定义 Sprint 与验收步骤
→ 从 develop 创建短期分支
→ 实现最小改动
→ Unity 编译和测试
→ 更新文档与 CHANGELOG
→ 按 REVIEW_CHECKLIST.md 自审或互审
→ 提交 Pull Request
→ 合并
→ 删除短期分支
```

详细流程见 `Docs/00_Project/Development_Workflow.md`。

## Sprint 工作方式

- 一个 Sprint 只解决一个明确、可独立验收的目标。
- Sprint 开始前必须写清目标、范围、非目标、依赖、文件清单、测试计划和完成标准。
- 不因为“以后可能需要”提前实现未批准系统。
- Sprint 中发现跨系统需求时，先记录风险或创建后续任务，不擅自扩大当前范围。
- 每个 Sprint 必须生成或更新实施报告，记录实际文件、配置、测试结果、限制和遗留项。
- Sprint 未满足阻塞验收项时不得标记完成，也不得以“后续再修”作为合并依据。
- 单人开发同样必须执行分支、自审、测试和文档门禁。

## 文档更新要求

代码和文档属于同一个交付物。以下变化必须在同一 Sprint 中更新文档：

| 变化 | 必须更新 |
|---|---|
| 游戏规则、MVP 范围 | 设计文档、影响分析、CHANGELOG |
| 模块职责或依赖 | `Technical_Architecture_v1.1.md`、对应系统文档 |
| 数据结构、稳定 ID、触发器 | `Data_Driven_Design.md`、相关系统文档 |
| 关卡规则或对象 | `Level_Framework.md`、具体关卡设计 |
| 网络权威或本地保存边界 | `Network_Authority_Rules.md` 或 `Save_Data_Boundary.md` |
| Sprint 实现 | Sprint 实施报告、CHANGELOG |
| 开发流程或规范 | 本治理文档库及 `Project_Governance_Report.md` |

文档必须描述已实现状态和明确的未实现状态，不得把规划写成已经完成。

## Unity 版本要求

- 固定使用 Unity `6000.3.21f1`。
- 项目模板与渲染管线为 Universal 3D / URP。
- 使用 Unity Input System。
- 未经独立验证和 Review，不得升级 Unity、URP、Input System 或测试框架版本。
- 包版本变化必须提交 `Packages/manifest.json` 与 `Packages/packages-lock.json`。
- 必须提交 Unity 资产对应的 `.meta` 文件。
- 不提交 `Library/`、`Temp/`、`Logs/`、本地构建产物或 IDE 缓存。

## 禁止事项

- 禁止直接向 `main` 提交功能代码。
- 禁止绕过 Review Checklist 合并。
- 禁止在一个 Sprint 中顺带实现未批准的后续系统。
- 禁止引入未确认的第三方插件、SDK、后端或云服务。
- 禁止让 Gameplay 领域依赖 UI、Editor、具体 Photon 类型或场景对象名称。
- 禁止跨层直接修改其他领域内部状态。
- 禁止循环程序集依赖。
- 禁止 God Class、万能 Manager、`Misc` 等无边界容器。
- 禁止在代码中硬编码可调玩法参数、关卡流程或内容 ID。
- 禁止把 ScriptableObject 当作局内可变权威状态容器。
- 禁止强制语音或让关键玩法只依赖颜色、声音或语音。
- 禁止在 MVP 中加入商城、氪金、排行榜、公会、开放大世界、复杂基地、复杂战斗或玩家编辑器 UI。
- 禁止提交编译错误、失败测试、丢失引用或未说明的 Console Error。

## Review 流程

1. 开发者先完成 `REVIEW_CHECKLIST.md` 自审。
2. 提交 Pull Request，说明范围、依赖、风险、测试步骤、测试结果和文档变化。
3. Reviewer 先检查范围和架构，再检查实现细节。
4. 阻塞问题必须修复后重新验证。
5. 非阻塞问题必须明确记录为后续任务，不能静默忽略。
6. Unity 资源改动必须检查 Prefab、Scene、ScriptableObject、`.meta` 和 GUID。
7. 合并前必须确认目标分支最新，并解决冲突后重跑相关测试。
8. 单人项目可进行自审，但必须保留相同的检查表和验证证据。

满足 Review、测试和文档门禁后，才允许按 `BRANCH_STRATEGY.md` 合并。
