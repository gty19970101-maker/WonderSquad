# Wonder Squad Commit 规范

## 1. 格式

采用 Conventional Commits：

```text
<type>(<scope>): <summary>
```

可选正文说明动机、实现约束和测试，可选 Footer 记录 Breaking Change 或关联任务。

建议使用英文小写 type 和 scope，summary 使用简洁祈使语气，不以句号结尾。

## 2. Type

| Type | 用途 |
|---|---|
| `feat` | 新功能或新的可玩能力 |
| `fix` | 缺陷修复 |
| `refactor` | 不改变外部行为的结构调整 |
| `docs` | 文档变更 |
| `test` | 测试新增或调整 |
| `build` | Unity 包、构建配置、程序集和依赖 |
| `ci` | 持续集成、自动化检查 |
| `style` | 不改变行为的格式调整 |
| `perf` | 性能优化 |

必要时可使用 `chore` 处理不属于以上类型的仓库维护，但不得用它掩盖功能改动。

## 3. Scope

推荐 Scope：

- `player`
- `interaction`
- `inventory`
- `item`
- `crafting`
- `puzzle`
- `ability`
- `status`
- `communication`
- `level`
- `network`
- `ui`
- `save`
- `core`
- `content`
- `docs`
- `project`

一个提交涉及多个无关 Scope 时，应拆分提交。

## 4. Wonder Squad 示例

```text
feat(player): add configurable character controller
test(player): cover grounded walking in play mode
fix(player): prevent duplicate offline spawn
refactor(core): isolate project logging contract
docs(level): clarify sleeping forest fallback routes
build(project): lock Unity input system package
ci(project): run edit mode tests on pull requests
style(player): apply C# naming conventions
perf(puzzle): cache trigger condition lookup
```

带正文：

```text
feat(player): add configurable character controller

Use CharacterController and the New Input System.
Keep movement parameters in MovementSettings.

Tests: EditMode and PlayerSandbox PlayMode passed.
```

Breaking Change：

```text
refactor(content)!: version stable content identifiers

BREAKING CHANGE: Existing prototype content assets must be migrated to schema v2.
```

## 5. 原子提交

- 一个提交只表达一个可解释的变化。
- 功能代码与直接相关测试可以同一提交。
- 大规模格式化不得与行为修改混合。
- 资源移动必须连同 `.meta` 一起提交。
- 不提交 `WIP`、`temp`、`fix stuff`、`update` 等模糊信息。
- 不提交 Unity Library、Temp、Logs、本地 Build 或 IDE 缓存。
- 合并前清理调试输出、临时资产和无用注释代码。

## 6. 与 Sprint 和 CHANGELOG 的关系

- Commit 描述技术变化。
- Sprint 实施报告描述交付范围、验证和限制。
- CHANGELOG 只记录玩家、开发流程或项目交付层面值得保留的重要变化。
- 尚未发布的 Sprint 变化写入 `[Unreleased]`，发布时移动到对应版本。
