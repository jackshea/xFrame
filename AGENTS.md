# AGENTS 指南（xFrame）
本文件面向在本仓库执行任务的 agent（编码、测试、重构、文档）。
目标：降低试错成本，统一风格，保证变更可验证、可回归。

## 1. 项目基础信息
- Unity 版本：`2021.3.51f1`（`ProjectSettings/ProjectVersion.txt`）。
- 核心目录：`Assets/xFrame/`；业务目录：`Assets/Game/`。
- 测试框架：`com.unity.test-framework`（`Packages/manifest.json`）。
- 主要依赖：VContainer、UniTask、Addressables。
- 原则：优先沿用仓库既有模式，不引入冲突规范。

## 2. 目录与职责边界
- `Assets/xFrame/Runtime/`：框架运行时代码（EventBus/DI/Logging/Scheduler/UI 等）。
- `Assets/xFrame/Editor/`：编辑器扩展代码。
- `Assets/xFrame/Tests/EditMode/`：EditMode 测试。
- `Assets/xFrame/Tests/PlayMode/`：PlayMode 测试。
- `Assets/xFrame.Examples/`：示例代码。
- `Assets/Game/`：游戏业务层。
- `Packages/`、`ProjectSettings/`：包与项目配置。
- 约束：保持框架层与业务层分离，避免跨层耦合。

## 3. 构建/检查/测试命令
说明：仓库无独立 lint 脚本与 CI workflow，主要依赖 Unity CLI + 测试校验。

### 3.1 打开项目
- Unity Hub 打开项目，版本固定 `2021.3.51f1`。
- Windows 编辑器路径示例：`D:\Program Files\Unity 2021.3.51f1\Editor\Unity.exe`。

### 3.2 全量 EditMode
```bash
"<UnityEditor>" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Temp/EditMode-results.xml -logFile Temp/EditMode.log -quit
```

### 3.3 全量 PlayMode
```bash
"<UnityEditor>" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Temp/PlayMode-results.xml -logFile Temp/PlayMode.log -quit
```

### 3.4 单个测试（重点）
Unity Test Framework 支持 `-testFilter`（完整测试名、Fixture、分号列表、正则）。
本仓库暂无现成 `-testFilter` 脚本，可直接使用下列模板。

单个测试方法：
```bash
"<UnityEditor>" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter "xFrame.Tests.DITests.Singleton_ShouldResolveSameInstance" -testResults Temp/SingleTest-results.xml -logFile Temp/SingleTest.log -quit
```

单个测试类（Fixture）：
```bash
"<UnityEditor>" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter "xFrame.Tests.DITests" -testResults Temp/Fixture-results.xml -logFile Temp/Fixture.log -quit
```

多个测试（分号分隔）：
```bash
"<UnityEditor>" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter "xFrame.Tests.DITests;xFrame.Tests.SchedulerServiceTests" -testResults Temp/Multi-results.xml -quit
```

### 3.5 构建与静态检查
- Player 构建：Unity Editor -> `File > Build Settings`。
- 无独立 lint，改为以下门禁：
  1) Unity Console 无新增错误。
  2) 相关 EditMode/PlayMode 测试通过。
  3) 关键脚本在 Editor 中可编译。

### 3.6 AI 统一验证脚本（推荐）
- Python（推荐，跨平台）：`python scripts/ai/validate.py`
  - 单目标：`python scripts/ai/validate.py single --platform EditMode --filter "xFrame.Tests.DITests"`
  - 单套件：`python scripts/ai/validate.py suite --platform EditMode`
  - 全量：`python scripts/ai/validate.py full`
- Bash：`./scripts/ai/validate.sh`
  - 单目标：`./scripts/ai/validate.sh single --platform EditMode --filter "xFrame.Tests.DITests"`
  - 单套件：`./scripts/ai/validate.sh suite --platform EditMode`
  - 全量：`./scripts/ai/validate.sh full`
- PowerShell：`./scripts/ai/validate.ps1`
  - 单目标：`./scripts/ai/validate.ps1 -Command single -Platform EditMode -Filter "xFrame.Tests.DITests"`
  - 单套件：`./scripts/ai/validate.ps1 -Command suite -Platform PlayMode`
  - 全量：`./scripts/ai/validate.ps1 -Command full`
- Unity 路径优先通过 `UNITY_EDITOR_PATH` 注入，或命令参数显式指定。

## 4. 代码风格（基于现有实现）

### 4.1 基础格式
- 语言：C#。
- 缩进：4 空格。
- 大括号：Allman（另起一行）。
- 变更原则：最小改动，不做无关格式化。

### 4.2 命名约定
- 类型/方法/属性：`PascalCase`。
- 接口：`I` 前缀（如 `ISchedulerService`、`IXLogManager`）。
- 私有字段：`_camelCase`。
- 命名空间：按目录分层（`xFrame.Runtime.<Module>`、`xFrame.Editor.<Module>`、`xFrame.Tests`）。
- 历史命名（如 `xFrameApplication`）保持兼容，不强行重命名。

### 4.3 using 与文件组织
- `using` 顺序：System -> Unity/第三方 -> 项目命名空间。
- 移除未使用 `using`。
- 新增文件时，路径/命名空间/类名保持一致。

### 4.4 注释与文档
- 注释优先中文，解释意图，不重复代码字面含义。
- 公共 API 与非显然逻辑建议补充 XML 注释。
- 避免噪声注释。

### 4.5 错误处理与日志
- 禁止空 `catch`。
- 捕获异常时保留异常对象并记录上下文。
- 优先使用 `IXLogger` / `IXLogManager`，避免随意 `Debug.Log`。
- 可恢复流程：记录并降级；不可恢复流程：抛出或显式失败。

### 4.6 依赖注入与模块化
- 新服务优先通过 VContainer 注册。
- Runtime 模块优先通过接口暴露能力。
- 编辑器代码放 `Editor` 目录，必要时加 `#if UNITY_EDITOR`。

## 5. 测试规范
- 测试框架：NUnit + Unity Test Framework。
- EditMode：纯逻辑、无需场景。
- PlayMode：依赖帧循环或运行时行为。
- 测试文件：`*Tests.cs`。
- 测试方法：行为命名，推荐 `Action_ShouldExpected`。
- 修复缺陷时必须补充或更新回归测试。

建议执行顺序：
1) 先跑受影响单测（`-testFilter`）。
2) 再跑对应测试集（EditMode/PlayMode）。
3) 最后跑全量（时间允许时）。

## 6. Agent 执行建议
- 先查同模块实现，再改代码。
- 采用小步修改与小步验证。
- 每次修改至少做一轮编译/测试验证。
- 尽量避免跨模块大改；必须跨模块时先列影响清单。
- 不提交临时目录：`Library/`、`Temp/`、`Logs/`、`obj/`。

## 7. 提交与 PR 规范
- 提交前缀：`feat:`、`fix:`、`test:`、`opt:`、`format:`。
- 推荐 scope：`feat(Scheduler): ...`、`test(DI): ...`。
- 单次提交聚焦一个逻辑变更。
- PR 至少包含：变更摘要、影响模块、测试依据（命令+结果）、迁移说明（如有）。

## 8. Cursor/Copilot 规则同步
已检查：`.cursor/rules/`、`.cursorrules`、`.github/copilot-instructions.md`。
当前仓库未发现上述规则文件。
若后续新增，需把关键约束同步到本 `AGENTS.md`。

## 9. 快速执行清单
开始任务前：
1) 确认改动层（Runtime / Editor / Tests / Game）。
2) 选 1-2 个同目录实现作为风格锚点。
3) 明确最小验证命令（优先单测）。

提交结果前：
1) 关键路径无编译错误。
2) 相关测试通过（至少单测 + 对应测试集）。
3) 无无关文件改动与临时文件。
4) 文档/注释与代码行为一致。
