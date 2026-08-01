# Wonder Squad Sprint Merge Review Checklist

每个 Sprint 合并前复制本清单到 Pull Request，并附验证证据。

## 1. 范围

- [ ] Sprint 目标、范围、非目标和完成标准已明确。
- [ ] 实现只包含本 Sprint 批准内容。
- [ ] 没有顺带开始下一 Sprint 或非 MVP 系统。
- [ ] 新增第三方包、SDK 或服务已经得到明确批准。

## 2. 编译与 Unity

- [ ] 使用 Unity `6000.3.21f1` 打开项目。
- [ ] 代码可以正常编译。
- [ ] Console 没有未说明的 Error。
- [ ] Scene、Prefab 和 ScriptableObject 没有 Missing Script 或 Missing Reference。
- [ ] `.meta` 与资源移动完整提交。
- [ ] Build Settings/Build Profiles 没有意外变化。
- [ ] 相关场景可以独立进入 Play Mode。

## 3. 测试

- [ ] 相关 EditMode 测试通过。
- [ ] 相关 PlayMode 测试通过。
- [ ] 需要时完成手工键鼠和手柄验证。
- [ ] 缺陷修复包含能复现问题的回归测试。
- [ ] 新增行为包含正常路径、边界和失败路径测试。
- [ ] 测试结果和未覆盖风险已记录。

## 4. 代码质量

- [ ] 符合 `CODE_STYLE.md`。
- [ ] 命名表达职责，Namespace 与目录一致。
- [ ] 关键公共契约和非直观规则增加了必要注释。
- [ ] 没有无价值注释或大段注释掉的代码。
- [ ] 没有 Magic Number；可调参数进入 Settings、Definition 或命名常量。
- [ ] 没有 God Class、万能 Manager 或 `Misc` 容器。
- [ ] MonoBehaviour 生命周期订阅和释放成对。
- [ ] Update/FixedUpdate/LateUpdate 中没有不必要的全场景查找或分配。

## 5. 架构

- [ ] 实现符合 `Technical_Architecture_v1.1.md`。
- [ ] 模块职责符合相关系统设计文档。
- [ ] 没有新增 Assembly Definition 循环依赖。
- [ ] 没有越权程序集引用。
- [ ] Runtime、Editor、Tests 正确隔离。
- [ ] Gameplay 不依赖 UI、Editor、具体网络 SDK 或场景对象名称。
- [ ] Content 不持有局内可变权威状态。
- [ ] 跨领域访问通过 Core/Contracts 的端口、命令、查询或事件。
- [ ] 网络权威或本地保存边界没有被隐式改变。

## 6. 游戏设计与 MVP

- [ ] 实现符合 `WonderSquad_Design_Document_v1.1.md`。
- [ ] 实现符合 `MVP_Development_Plan_v1.1.md`。
- [ ] 不强制语音，不让重要信息只依赖颜色或声音。
- [ ] 没有引入特定角色硬锁。
- [ ] 欢乐失败不会被实现为立即重开整关。
- [ ] 没有加入商城、氪金、排行榜、公会、开放大世界、复杂基地、复杂战斗或玩家编辑器 UI。

## 7. 文档

- [ ] 相关设计文档与当前实现一致。
- [ ] 相关游戏设计文档已更新，或确认无需修改。
- [ ] 相关技术架构/系统设计已更新，或确认无需修改。
- [ ] Sprint 实施报告已创建或更新。
- [ ] `CHANGELOG.md` 的 `[Unreleased]` 已更新。
- [ ] 文件路径、Unity 配置和验证步骤准确。
- [ ] 规划内容与已实现内容明确区分。

## 8. Git 与 Review

- [ ] 分支名称符合 `BRANCH_STRATEGY.md`。
- [ ] Commit 符合 `COMMIT_RULE.md`。
- [ ] 分支已同步目标分支并解决冲突。
- [ ] Reviewer 的阻塞意见已解决。
- [ ] Pull Request 描述包含范围、测试、风险、文档和截图/日志等必要证据。

## 9. 合并结论

- [ ] 通过：允许合并。
- [ ] 有条件通过：列出必须在合并前关闭的条件，不立即合并。
- [ ] 不通过：返回修改。

未完成的阻塞项：

```text
无 / 在此填写
```

验证环境与证据：

```text
Unity版本：
测试：
场景：
构建：
Reviewer：
```
