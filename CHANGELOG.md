# Changelog

本文件记录 Wonder Squad 的重要变更。

格式遵循 [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)，版本命名在 MVP 阶段采用项目里程碑标识。

## [Unreleased]

### Added

- 项目治理文档与开发工作流。
- Sprint001-A 本地 Player Spawn：基础 Player Prefab、PlayerSpawnPoint 与 Sandbox PlayerSpawner。
- PlayerSandbox 启动时生成单个本地 Player，并支持销毁后重新生成。
- Player Spawn 的 EditMode 配置测试、PlayMode 行为测试与 P0 回归验证。
- Sprint001-B Player Input Reader：复用 New Input System 的 Gameplay/Move Action，支持 WASD、方向键、输入限幅和启停生命周期。
- PlayerSandbox 的输入变化调试日志，以及输入配置、限幅、生命周期、重复订阅和 Player 静止状态测试。
- Sprint001-C Character Controller Movement：数据化水平移动、角色朝向、地面状态和重力。
- Player Prefab 的 CharacterController、GroundDetector、PlayerMovement 与 MovementSettings 配置，以及对应 EditMode、PlayMode 测试。
- Sprint001-D Controlled Third-Person Camera：基于 Cinemachine `3.1.7` 的固定斜俯视跟随、独立 Camera Target、运行时本地玩家绑定和数据化相机参数。
- PlayerSandbox 的 Camera Rig、CameraValidationRoot，以及玩家生成、销毁、重新生成、跟随稳定性、固定朝向和单相机约束测试。

### Changed

- Sprint001-A Player Spawn 已通过 Unity `6000.3.21f1` 人工验收：PlayerSandbox、胶囊生成、出生位置、单实例和全部测试均正常，Console Error 为 0。
- Sprint001-C Character Controller Movement 已通过 Unity `6000.3.21f1` 最终验收：WASD、停止、转向、斜向限速、重力、接地、禁用与重新启用均正常，Console Error 为 0；EditMode 32/32、PlayMode 13/13 通过。
- Sprint001-D Controlled Third-Person Camera 已通过 Unity `6000.3.21f1` 最终验收：EditMode 37/37、PlayMode 19/19 通过；WASD 跟随、固定斜俯视、停止稳定性、墙体、门洞、高低差、Canopy 与单 Main Camera 人工检查均正常，Console Error 为 0，最终状态为 `PASSED`。

### Fixed

- 使用 `FormerlySerializedAs("spawnOnStart")` 将 `PlayerSpawner` 的序列化布尔字段安全迁移为 `shouldSpawnOnStart`，保留原默认值、运行逻辑及 Unity 资产中的序列化值，并关闭 Sprint001 Gate 命名门禁项。

## [v0.1-P0] - 2026-08-01

### Added

- 完成 P0 项目初始化和 Unity 工程骨架。
- 锁定 Unity `6000.3.21f1`、Universal 3D / URP、Input System 与测试框架。
- 建立 Runtime、Editor、Tests、Scenes、Prefabs、ScriptableObjects、UI、Art 和 Audio 目录。
- 建立模块 Assembly Definition、命名空间边界、Bootstrap、日志接口和项目配置入口。
- 建立输入动作资产、三个独立 Sandbox、基础 EditMode 与 PlayMode 测试。
- 建立 v1.1 游戏设计、MVP 开发计划、技术架构和系统设计文档库。
- 完成《沉睡森林》关卡设计、原型任务分解和 P0 审查报告。
- 建立 Git 仓库并连接远程仓库。

### Fixed

- 修复 P0 场景黑屏所需的 Camera、灯光、占位环境和 URP 绑定。
- 修复程序集边界、Bootstrap 生命周期、输入配置和内容引用校验问题。

[Unreleased]: https://github.com/gty19970101-maker/WonderSquad/compare/v0.1-P0...HEAD
[v0.1-P0]: https://github.com/gty19970101-maker/WonderSquad/releases/tag/v0.1-P0
