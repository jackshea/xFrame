# AGENTS 指南（xFrame）
目标：统一协作规则，保证改动可验证、可回归。

## 基本约定
- 默认中文沟通，换行符使用 LF。
- Unity 版本：`2021.3.51f1`（`ProjectSettings/ProjectVersion.txt`）。
- 优先复用仓库现有模式，避免引入新规范冲突。

## 目录边界
- 框架层：`Assets/xFrame/Runtime/`、`Assets/xFrame/Editor/`、`Assets/xFrame/Tests/`、`Assets/xFrame.Examples/`。
- 业务层：`Assets/Game/`。
- 配置层：`Packages/`、`ProjectSettings/`。
- 要求：框架层与业务层解耦，避免跨层依赖。

## 代码规范
- C#，4 空格缩进，Allman 大括号，最小改动。
- 命名：类型/方法/属性 `PascalCase`，接口 `I*`，私有字段 `_camelCase`。
- `using` 顺序：System -> Unity/第三方 -> 项目命名空间；移除未使用项。
- 注释：类和方法使用中文 XML 注释，写清职责、设计意图与关键生命周期。
- 异常与日志：禁止空 `catch`；记录上下文；优先 `IXLogger` / `IXLogManager`。
- DI 与模块化：新服务优先 VContainer 注册；Runtime 能力优先接口暴露；Editor 代码放 `Editor` 目录。

## 测试与验证
- 测试框架：NUnit + Unity Test Framework。
- EditMode 测纯逻辑，PlayMode 测运行时行为；测试文件命名 `*Tests.cs`。
- 修复缺陷必须补回归测试，测试方法推荐 `Action_ShouldExpected`。
- 建议顺序：受影响单测（`-testFilter`）-> 对应测试集 -> 全量测试（时间允许）。
- 单元测试通过 `unity-rpc` skill 执行。

## 提交要求
- 提交前缀：`feat:`、`fix:`、`test:`、`opt:`、`format:`（可带 scope）。
- 提交信息统一使用中文，准确概括本次变更内容。
- 提交信息尽可能详细，不要只有标题；除非改动确实足够简单。
- 单次提交聚焦单一逻辑变更。
- PR 至少包含：变更摘要、影响模块、测试命令与结果、迁移说明（如有）。

## 执行清单
- 改动前：确认改动层，参考同目录实现，确定最小验证命令。
- 提交前：无编译错误，相关测试通过，无无关改动，文档/注释与行为一致。
