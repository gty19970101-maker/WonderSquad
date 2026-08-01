# Wonder Squad P0 项目初始化实施报告

## 1. 实施结论

P0 Unity 工程骨架已创建在：

```text
Client/
```

本次开始前，`Client/` 为空，不存在 `Assets/`、`Packages/` 或 `ProjectSettings/`，因此确认仓库原先没有 Unity 项目。

当前骨架包含项目版本、Unity 官方包、目录、程序集、命名空间、Bootstrap、基础日志、项目配置入口、输入动作结构、两个最小场景和基础测试。没有实现 P1 或后续玩法。

本机未检测到 Unity Editor 或 Unity Hub，因此已完成文件级与结构级检查，但无法在本机执行 Unity 导入、真实程序集编译、Test Runner 或开发构建。P0 状态为：

> 工程骨架实现完成；Unity Editor 首次导入与最终编译验证待执行。

---

## 2. 版本与依赖

### Unity版本

- 锁定版本：Unity `6000.3.21f1`
- Changeset：`c02631ffc030`
- 系列：Unity 6.3 LTS
- 项目方向：Universal 3D / URP

版本依据：

- [Unity 6000.3.21f1 官方发布说明](https://unity.com/releases/editor/whats-new/6000.3.21f1)
- [Unity 6.3 LTS 支持说明](https://unity.com/releases/unity-6/support)

### 已声明的Unity官方包

| 包 | 版本 | 用途 |
|---|---:|---|
| `com.unity.inputsystem` | `1.20.0` | 输入动作结构 |
| `com.unity.render-pipelines.universal` | `17.3.0` | Universal 3D / URP |
| `com.unity.test-framework` | `1.6.0` | EditMode 与 PlayMode 测试 |
| `com.unity.modules.*` | `1.0.0` | Unity内置基础模块 |

参考：

- [Input System 1.20.0 官方文档](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html)
- [URP 17.3 官方文档](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/urp-introduction.html)
- [Test Framework 1.6.0 官方文档](https://docs.unity3d.com/Packages/com.unity.test-framework@1.6/manual/index.html)

未引入：

- Photon Fusion
- Unity Netcode
- 语音 SDK
- Cinemachine
- Addressables
- 第三方日志、DI、测试或美术插件

Photon Fusion 按现有计划延期到原型 P7。

---

## 3. 创建内容

### 3.1 项目根与设置

| 文件 | 说明 |
|---|---|
| `Client/.gitignore` | 忽略 Library、Temp、Logs、构建产物和 IDE 文件 |
| `Client/README.md` | Unity打开、验证及 P0 范围说明 |
| `Client/Packages/manifest.json` | 锁定直接 Unity 官方包版本 |
| `Client/ProjectSettings/ProjectVersion.txt` | 锁定 Unity 6000.3.21f1 |
| `Client/ProjectSettings/ProjectSettings.asset` | 产品名、线性色彩空间、新输入系统入口 |
| `Client/ProjectSettings/EditorSettings.asset` | Force Text 与 Visible Meta Files |
| `Client/ProjectSettings/EditorBuildSettings.asset` | 注册 Bootstrap 与 P0 测试场景 |

### 3.2 基础目录

已建立：

```text
Client/Assets/WonderSquad/
├── Runtime/
├── Editor/
├── Tests/
├── Scenes/
├── Prefabs/
├── ScriptableObjects/
├── UI/
├── Art/
└── Audio/
```

进一步划分：

- `Art/Characters`
- `Art/Environment`
- `Art/Items`
- `Art/Animations`
- `Art/VFX`
- `Art/UI`
- `Audio/Music`
- `Audio/SFX`
- `Prefabs/Bootstrap`
- `Prefabs/Tests`
- `ScriptableObjects/Configuration`
- `ScriptableObjects/Content`
- `UI/Fonts`
- `UI/Icons`
- `UI/Layouts`
- `Tests/EditMode`
- `Tests/PlayMode`
- `Tests/ContentValidation`
- `Tests/Network`

没有创建 `Managers`、`Misc`、语音或 Fusion 目录。

### 3.3 程序集定义

共创建 20 个 Assembly Definition：

#### 已含P0代码

- `WonderSquad.Core`
- `WonderSquad.Content`
- `WonderSquad.Bootstrap`
- `WonderSquad.Editor`
- `WonderSquad.Tests.EditMode`
- `WonderSquad.Tests.PlayMode`
- `WonderSquad.Tests.ContentValidation`

#### 仅预留边界、没有业务代码

- `WonderSquad.Player`
- `WonderSquad.Interaction`
- `WonderSquad.Inventory`
- `WonderSquad.Item`
- `WonderSquad.Crafting`
- `WonderSquad.Puzzle`
- `WonderSquad.Ability`
- `WonderSquad.Status`
- `WonderSquad.Communication`
- `WonderSquad.Level`
- `WonderSquad.Network`
- `WonderSquad.UI`
- `WonderSquad.Save`

命名空间规划记录于：

```text
Client/Assets/WonderSquad/Runtime/NAMESPACE.md
```

静态检查结果：

- 程序集名称无重复。
- 所有声明的项目程序集引用均存在。
- 未发现程序集循环依赖。
- Gameplay 预留程序集只引用 Core 和必要的只读 Content。
- 没有 Fusion 类型或程序集引用。

### 3.4 基础日志

创建：

```text
Runtime/Core/Logging/
├── IProjectLogger.cs
├── ProjectLog.cs
└── ProjectLogLevel.cs

Runtime/Bootstrap/Logging/
└── UnityProjectLogger.cs
```

规则：

- `IProjectLogger` 是不依赖具体 Unity 输出方式的基础接口。
- `ProjectLog` 提供安全的空实现和统一入口。
- `UnityProjectLogger` 是 Bootstrap 外层的 Unity Console 适配器。
- 当前没有日志文件、远程日志、分析后台或业务事件记录。

### 3.5 项目常量与配置入口

创建：

```text
Runtime/Core/Configuration/ProjectConstants.cs
Runtime/Bootstrap/Configuration/ProjectConfiguration.cs
```

只包含：

- 产品名和日志标签。
- Bootstrap、测试场景和输入资产路径。
- 内容结构版本。
- Development/Test/Release 环境枚举。
- 是否启用开发诊断。

没有放入关卡、角色、能力、配方、物品或其他业务数据。

### 3.6 数据驱动基础边界

创建：

```text
Runtime/Content/Definitions/
├── IContentDescriptor.cs
└── ContentDefinition.cs

Runtime/Content/Validation/
├── ContentValidationIssue.cs
└── ContentValidator.cs
```

当前只校验：

- 稳定 ID 是否为空。
- 稳定 ID 是否使用小写、点分隔格式。
- ID 是否重复。
- SchemaVersion 是否为正数。

校验器只验证配置结构，不承诺证明关卡或机关可通关。

### 3.7 Bootstrap

创建：

```text
Runtime/Bootstrap/
├── BootstrapEntry.cs
└── BootstrapLifetime.cs
```

行为：

- 使用 `RuntimeInitializeOnLoadMethod` 在首个场景前初始化。
- 配置 Unity Console 日志适配器。
- 创建一个跨场景保留的 `[WonderSquad.Bootstrap]` 根对象。
- 处理重复初始化。
- 明确记录“未注册玩法系统”。

Bootstrap 没有加载玩家、背包、机关、能力、关卡或网络服务。

### 3.8 输入结构

输入资产：

```text
Client/Assets/WonderSquad/Settings/Input/WonderSquad.inputactions
```

动作：

- Move
- Look
- Jump
- Interact
- Ability
- Marker
- QuickIntent
- Emote
- Rescue
- Pause

控制方案：

- Keyboard & Mouse
- Gamepad

同时创建：

```text
Runtime/Core/Input/InputActionNames.cs
```

当前只有动作定义和稳定语义名，没有输入适配器或任何玩法响应。

### 3.9 场景

创建：

```text
Client/Assets/WonderSquad/Scenes/Bootstrap/Bootstrap.unity
Client/Assets/WonderSquad/Scenes/Tests/P0_TestSandbox.unity
```

两个场景都是最小空场景：

- Bootstrap 场景只包含识别根节点。
- P0 Test Sandbox 只包含测试根节点。
- Bootstrap 入口由运行时初始化特性创建，不依赖手动场景脚本引用。
- 没有角色、摄像机玩法、资源、机关或联机对象。

两个场景已经写入 `EditorBuildSettings.asset`，场景 GUID 与 Build Settings 引用一致。

### 3.10 编辑器验证入口

创建：

```text
Client/Assets/WonderSquad/Editor/P0ProjectValidator.cs
```

Unity 菜单：

```text
Wonder Squad > P0 > Validate Project Skeleton
```

检查：

- Bootstrap 场景存在。
- P0测试场景存在。
- Input Actions 资产存在。
- 两个场景均启用在 Build Settings 中。

### 3.11 基础测试

创建四个测试文件：

```text
Tests/EditMode/ProjectConstantsTests.cs
Tests/EditMode/ProjectLogTests.cs
Tests/ContentValidation/ContentValidatorTests.cs
Tests/PlayMode/BootstrapSmokeTests.cs
```

覆盖：

- 项目路径和输入动作语义。
- 日志适配切换和空日志安全行为。
- 合法、空、重复和非法格式的稳定 ID。
- PlayMode 下 Bootstrap 是否初始化并创建唯一根对象。

### 3.12 文件路径索引

- Unity 工程根目录：`Client/`
- 项目代码与资源根目录：`Client/Assets/WonderSquad/`
- Unity 包声明：`Client/Packages/manifest.json`
- Unity 项目设置：`Client/ProjectSettings/`
- Bootstrap 入口：`Client/Assets/WonderSquad/Runtime/Bootstrap/`
- 输入动作资产：`Client/Assets/WonderSquad/Settings/Input/WonderSquad.inputactions`
- P0 场景：`Client/Assets/WonderSquad/Scenes/`
- P0 测试：`Client/Assets/WonderSquad/Tests/`

---

## 4. 已执行的非Unity验证

由于本机无 Unity Editor，本次执行了以下静态检查：

| 检查 | 结果 |
|---|---|
| JSON、asmdef、inputactions 可解析 | 22个文件，0错误 |
| Assembly Definition 数量 | 20 |
| 缺失程序集引用 | 0 |
| 程序集循环依赖 | 0 |
| 必要基础目录 | 9/9存在 |
| 输入动作 | 10/10存在 |
| Build Settings与场景GUID | 一致 |
| P1+运行时代码特征扫描 | 0命中 |
| Photon/Fusion代码引用 | 0 |

C# 文件数量：

- Runtime：13
- Editor：1
- Tests：4

这些检查不能替代 Unity 的真实程序集编译与 Test Runner。

---

## 5. 如何在 Unity 中打开和验证

### 5.1 安装并打开

1. 在 Unity Hub 安装 Unity `6000.3.21f1`。
2. 确保安装 Windows Build Support。
3. 在 Hub 选择 **Add project from disk**。
4. 选择：

   ```text
   H:\gty\game\WonderSquad\Client
   ```

5. 等待 Package Manager 完成官方包解析。
6. 若弹出 Input System 后端切换提示，选择启用新 Input System 并重启 Editor。

### 5.2 首次导入检查

确认 Console 没有编译错误，然后：

1. 打开 `Assets/WonderSquad/Scenes/Bootstrap/Bootstrap.unity`。
2. 进入 Play Mode。
3. Console 应显示：

   ```text
   [WonderSquad] P0 Bootstrap initialized. Gameplay systems are not registered.
   ```

4. Hierarchy 或运行时对象中应存在：

   ```text
   [WonderSquad.Bootstrap]
   ```

5. 停止 Play Mode。

### 5.3 执行项目验证

运行：

```text
Wonder Squad > P0 > Validate Project Skeleton
```

预期输出：

```text
[WonderSquad] P0 project skeleton validation passed.
```

### 5.4 执行测试

打开：

```text
Window > General > Test Runner
```

执行：

- EditMode 全部测试。
- ContentValidation 全部测试。
- PlayMode 全部测试。

预期：

- 所有测试通过。
- 无重复 Bootstrap 根对象。
- 无程序集引用或 Input Actions 导入错误。

### 5.5 URP首次导入确认

`manifest.json` 已声明 URP 17.3.0，但本机没有 Unity Editor，无法生成和序列化项目专用的 URP Pipeline Asset。

首次打开后应在 P0 范围内完成：

1. 创建 Universal Render Pipeline Asset 和 Universal Renderer Data。
2. 放入：

   ```text
   Assets/WonderSquad/Settings/URP/
   ```

3. 在 Project Settings > Graphics 指定默认 Render Pipeline。
4. 在 Project Settings > Quality 为开发质量档指定同一 Pipeline。
5. 重新运行场景、测试和一次 Windows Development Build。

在上述步骤完成前，不应开始 P1。

### 5.6 首次导入后提交

Unity 会生成或更新：

- `Packages/packages-lock.json`
- 尚未生成的 `.meta`
- Editor管理的 ProjectSettings 序列化字段
- URP Pipeline 和 Renderer 资产

验证通过后应将这些文件一并提交，避免团队成员得到不同包解析结果或 GUID。

---

## 6. 当前限制

1. 本机没有 Unity Editor/Hub，无法执行真实导入和编译。
2. `Packages/packages-lock.json` 尚未由 Unity Package Manager 生成。
3. 未生成项目专用 URP Pipeline Asset 和 Renderer Data。
4. 尚未执行 Windows Development Build。
5. 尚未生成全部 Unity `.meta`；首次导入后必须提交。
6. Bootstrap 场景和测试场景故意为空，不包含相机、角色或玩法对象。
7. `ProjectConfiguration` 只有类型入口，尚未创建具体配置资产。
8. Network 程序集只是无 SDK 的边界占位；没有房间、同步或 Photon 代码。
9. 没有玩家移动、交互、背包、资源、道具、制作、机关、能力、状态、交流或关卡业务。

---

## 7. 下一步建议

下一步仍然只处理 P0 验证，不开始 P1：

1. 安装 Unity 6000.3.21f1 并首次打开 `Client/`。
2. 让 Package Manager 生成锁文件。
3. 创建并绑定 URP Pipeline Asset。
4. 运行 P0 项目验证菜单。
5. 运行全部基础测试。
6. 执行一次空场景 Windows Development Build。
7. 提交 Unity 生成的 `.meta`、包锁和最终 ProjectSettings。
8. 只有 P0 Console、测试和开发构建全部通过后，再由用户明确授权开始 P1。

---

## 8. P0 范围确认

本次没有：

- 创建玩家控制器。
- 实现移动或摄像机。
- 实现交互检测。
- 实现背包、资源或物品。
- 实现制作或机关。
- 实现角色能力。
- 实现欢乐失败和救援。
- 实现标记、快捷指令或表情。
- 接入联机或语音。
- 创建《沉睡森林》业务场景。

工作已在 P0 项目初始化范围内停止。
