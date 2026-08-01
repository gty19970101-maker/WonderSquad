# Sprint001D Review Checklist

## 1. 范围

- [x] 只实现受控斜俯视第三人称跟随。
- [x] 未实现自由环绕。
- [x] 未实现鼠标或手柄相机输入。
- [x] 未实现锁定目标、瞄准、动态 FOV 或相机抖动。
- [x] 未实现 Cutscene、分屏、Spectator 或 Network Camera。
- [x] 未实现 Jump、Animation 或后续玩法系统。
- [x] 未实现 Interaction、Inventory、Ability 或 Puzzle。

## 2. 分支、Unity 与 Package

- [x] 当前分支为 `feature/sprint001d-camera-follow`。
- [x] 未在 `develop` 或 `main` 实施。
- [x] Unity 基线为 `6000.3.21f1`。
- [x] manifest 精确指定 Cinemachine `3.1.7`。
- [x] packages-lock 解析 Cinemachine `3.1.7`、depth `0`。
- [x] 未发现旧 Cinemachine 版本冲突。
- [x] 未升级 Input System、URP、Test Framework 或其他现有直接包。
- [x] 已记录 Cinemachine 的 Splines 与 Settings Manager 传递依赖。

## 3. Prefab 与 Scene

- [x] Player Prefab 包含独立 `CameraTarget`。
- [x] Player Prefab 不包含 Main Camera。
- [x] Player Prefab 不包含 Camera Rig。
- [x] `PF_PlayerCameraRig.prefab` 属于场景表现层。
- [x] Rig 包含一个 Main Camera、AudioListener 和 CinemachineBrain。
- [x] Rig 包含一个 CinemachineCamera 和 CinemachineFollow。
- [x] PlayerSandbox 明确连接 PlayerSpawner 与 CameraTargetBinder。
- [x] PlayerSandbox 只有一个有效 Main Camera。
- [x] PlayerSandbox 保留 P0 `CameraMount` 场景契约。
- [x] PlayerSandbox 包含 `CameraValidationRoot` 占位验证环境。
- [x] 所有新增 Unity 资产与脚本都有 `.meta`。

## 4. 职责与耦合

- [x] PlayerCameraTarget 只提供跟随锚点。
- [x] PlayerCameraTarget 不执行移动或相机控制。
- [x] CameraTargetBinder 不实现 Cinemachine 已有的跟随算法。
- [x] Camera 不依赖 PlayerInputReader。
- [x] Camera 不读取 Keyboard、Gamepad 或 InputAction。
- [x] Camera 不向 PlayerMovement 写入状态。
- [x] PlayerMovement 不引用 Camera 程序集。
- [x] PlayerSpawner 不持有 Camera、Binder 或 Cinemachine 引用。
- [x] PlayerSpawner 只新增通用 PlayerSpawned 事件。
- [x] Player Prefab 转向不会驱动相机环绕。
- [x] Camera Rig 不是 Player 子对象。

## 5. 生命周期

- [x] 相机先初始化、玩家后生成可以绑定。
- [x] 玩家先存在、Binder 后启用可以绑定。
- [x] PlayerSpawner 稍后生成可以绑定。
- [x] Player 销毁后安全清空 Target。
- [x] Player 重新生成后绑定新 Target。
- [x] 没有静态全局 Player 引用。
- [x] 没有每帧 FindObjectOfType/FindFirstObjectByType。
- [x] 没有字符串名称 Runtime 玩家查找。
- [x] 事件在 Binder 禁用时取消订阅。
- [x] 不会重复创建 Camera 对象。

## 6. 参数与代码质量

- [x] Target Local Offset 位于 CameraFollowSettings。
- [x] Follow Offset 位于 CameraFollowSettings。
- [x] Position Damping 位于 CameraFollowSettings。
- [x] Fixed Euler Angles 位于 CameraFollowSettings。
- [x] Field Of View 位于 CameraFollowSettings。
- [x] 配置校验拒绝非有限值和越界阻尼。
- [x] Runtime 没有散落可调相机 Magic Number。
- [x] Namespace 为 `WonderSquad.Presentation.Camera`，不遮蔽 UnityEngine.Camera。
- [x] MonoBehaviour 使用 sealed。
- [x] Inspector 引用为 `[SerializeField] private` 并提供 Tooltip。
- [x] 未加入静态可变状态、God Class、Region 或万能 Manager。

## 7. 程序集与架构

- [x] 新增独立 `WonderSquad.Camera` 表现层程序集。
- [x] Camera 只引用 Core、Player、Unity.Cinemachine。
- [x] Player 不引用 WonderSquad.Camera。
- [x] Gameplay 领域不引用 WonderSquad.Camera。
- [x] 没有循环程序集依赖。
- [x] Tests 引用 Camera 与 Unity.Cinemachine，Runtime 不引用 Tests。
- [x] Camera 状态保持本地，不进入 Network Authority。
- [x] 公开 TryBind 边界允许未来替换本地玩家选择来源。

## 8. EditMode Tests

- [x] CameraFollowSettings 默认资产有效。
- [x] Follow Offset 与 Target Offset 有效。
- [x] Damping 范围有效。
- [x] PlayerCameraTarget 配置有效。
- [x] Player Prefab 不包含 Camera。
- [x] 程序集依赖方向正确。
- [x] Cinemachine 实际包与 manifest 均为 `3.1.7`。
- [x] 完整 EditMode 回归通过。

结果：

```text
Sprint001D related: 5 Passed, 0 Failed
Full EditMode: 37 Passed, 0 Failed, 0 Skipped
```

## 9. PlayMode Tests

- [x] 玩家生成后绑定 Camera Target。
- [x] 相机先初始化、玩家后生成仍可绑定。
- [x] 已存在玩家在 Binder 重新启用后可绑定。
- [x] 玩家移动时相机跟随。
- [x] 玩家停止后相机稳定。
- [x] 玩家转向不会让相机自由环绕。
- [x] 玩家销毁无 NullReferenceException。
- [x] 玩家重新生成后重新绑定。
- [x] 场景只有一个有效 Main Camera。
- [x] 不会重复创建 Cinemachine Camera。
- [x] PlayerSpawner、PlayerInputReader、PlayerMovement 与 Bootstrap 回归通过。

结果：

```text
Sprint001D related: 6 Passed, 0 Failed
Full PlayMode: 19 Passed, 0 Failed, 0 Skipped
```

## 10. 人工 Unity 验收

- [x] PlayerSandbox Play 后一个 Player 正常生成。
- [x] WASD 移动时相机平滑跟随。
- [x] 快速切换方向时跟随保持平滑。
- [x] Player 转向时相机方向保持固定。
- [x] 松开输入后相机平稳停止。
- [x] 固定斜俯视构图符合当前 Sandbox 验收要求。
- [x] 墙体、门洞、高低差与 Canopy 验证区通过。
- [x] 删除与重新生成 Player 的安全重绑定已由 PlayMode 自动化覆盖。
- [x] 场景只有一个有效 Main Camera。
- [x] Camera 接入未改变既有 WASD 移动行为。
- [x] Console Error 为 0。
- [x] 人工验收未发现资源引用或运行时缺失错误。

## 11. Review 关注项

- [x] 默认斜俯视构图适合当前 PlayerSandbox 的世界坐标移动。
- [x] 当前阻尼满足平滑且停止后稳定的验收要求。
- [x] Camera Target 高度适合当前胶囊占位模型；正式角色模型接入时允许数据化复调。
- [x] OccluderWall、Canopy 与 NarrowGate 未暴露 Sprint001D 阻塞问题。
- [x] CameraTargetBinder 对 PlayerSpawner 的依赖保持 Sandbox 本地适配边界。
- [x] 新增 Prefab、Scene、ScriptableObject 与对应 `.meta` 静态核对完整。

## 12. 最终状态

**PASSED**

自动化验证、架构检查和 Unity `6000.3.21f1` 人工验收均已通过。Sprint001D 已完成；本清单不启动 Sprint001E 或 Interaction。
