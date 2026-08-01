# Wonder Squad Git 分支规范

## 1. 长期分支

### `main`

- 始终保持可发布、可回退。
- 只接收经过验证的 release、hotfix 或阶段里程碑。
- 禁止直接提交功能开发。
- 合并到 `main` 后创建版本 Tag，并更新 CHANGELOG。

### `develop`

- Sprint 与功能集成分支。
- 所有常规 feature、bugfix 先合并到 `develop`。
- 必须保持可编译，主测试集必须通过。
- 如果仓库尚无 `develop`，应从最新 `main` 创建并推送后再开始正式 Sprint 实现。

## 2. 短期分支

| 类型 | 来源 | 目标 | 示例 |
|---|---|---|---|
| `feature/` | `develop` | `develop` | `feature/sprint001-character-controller` |
| `bugfix/` | `develop` | `develop` | `bugfix/player-ground-check` |
| `release/` | `develop` | `main`，并回合 `develop` | `release/v0.2` |
| `hotfix/` | `main` | `main`，并回合 `develop` | `hotfix/bootstrap-crash` |
| `docs/` | `develop` | `develop` | `docs/project-governance` |

后续 Sprint 示例：

- `feature/sprint001-character-controller`
- `feature/sprint002-interaction`
- `feature/sprint003-pickup`

分支名使用小写 kebab-case，不使用空格、中文、开发者姓名或模糊名称。

## 3. 什么时候 Merge

仅当以下条件全部满足时合并：

- Sprint 范围和非目标没有被破坏。
- Unity 使用锁定版本无编译错误。
- 相关 EditMode、PlayMode 和必要手工测试通过。
- Console 无未说明 Error。
- `REVIEW_CHECKLIST.md` 完成。
- 相关设计、架构、系统、实施报告和 CHANGELOG 已更新。
- Assembly Definition 无新增循环或越权依赖。
- Prefab、Scene、ScriptableObject 与 `.meta` 完整。
- Pull Request 的阻塞意见已解决。

feature 与 bugfix 推荐 Squash Merge 到 `develop`，使一个 Sprint 或修复形成一个清晰提交。release 与 hotfix 推荐保留明确合并记录，并将结果同步回 `develop`。

## 4. 什么时候 Delete

- feature、bugfix、docs 分支合并且远程 CI/验证完成后删除本地与远程分支。
- release 分支在 `main` 打 Tag、回合 `develop` 并验证后删除。
- hotfix 分支在 `main` 与 `develop` 都包含修复后删除。
- 未合并的实验分支只能在确认没有需要保留的提交后删除。
- 不长期复用已合并的 Sprint 分支；下一 Sprint 创建新分支。

## 5. 同步与冲突

- 开始工作前同步目标分支。
- 合并前更新分支并解决冲突。
- Unity Scene、Prefab、ProjectSettings 冲突不得盲目选择一侧；必须在 Unity 中重新打开验证。
- 解决冲突后重新编译和运行受影响测试。
- 不使用破坏性历史重写覆盖他人已推送工作。

## 6. 当前 Sprint 约束

Sprint001 必须只包含 Character Controller 批准范围。Interaction、Inventory、Puzzle、Ability、Network 和 UI 不得进入该分支。
