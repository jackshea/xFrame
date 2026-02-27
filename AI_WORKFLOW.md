# xFrame AI Workflow

本文件给 AI 编码代理提供统一入口：快速定位、生成代码、最小验证、全量验证。

## 1. 固定输入

- Unity 版本：`2021.3.51f1`
- 项目根目录：仓库根目录
- Unity 可执行路径：
  - 推荐通过环境变量 `UNITY_EDITOR_PATH` 注入
  - 或在命令参数里显式传入

## 2. 统一验证命令（Python 优先）

### 2.1 Python（推荐，跨平台）

```bash
python scripts/ai/validate.py single --platform EditMode --filter "xFrame.Tests.DITests"
python scripts/ai/validate.py suite --platform EditMode
python scripts/ai/validate.py full
```

常用参数：
- `--unity "<path>"`：显式指定 Unity Editor
- `--project-path "<path>"`：指定项目路径
- `--dry-run`：只打印命令，不执行

Unity 路径解析顺序：
1) `--unity`
2) 环境变量 `UNITY_EDITOR_PATH`

### 2.4 WSL 与 Windows Unity 交互

在 WSL 中，`validate.py` 会自动处理路径转换：
- `--unity` 支持 Windows 路径（如 `C:\Program Files\Unity\Editor\Unity.exe`）
- 也支持 WSL 挂载路径（如 `/mnt/c/Program Files/Unity/Editor/Unity.exe`）
- 传给 Unity 的 `-projectPath/-testResults/-logFile` 会自动转成 Windows 路径

示例（WSL 内执行）：

```bash
python scripts/ai/validate.py suite --platform EditMode --unity "C:\\Program Files\\Unity 2021.3.51f1\\Editor\\Unity.exe"
```

### 2.2 Bash 包装器 (Linux/macOS/Git-Bash)

```bash
./scripts/ai/validate.sh single --platform EditMode --filter "xFrame.Tests.DITests"
./scripts/ai/validate.sh suite --platform EditMode
./scripts/ai/validate.sh full
```

常用参数：
- `--unity "<path>"`：显式指定 Unity Editor
- `--project-path "<path>"`：指定项目路径
- `--dry-run`：只打印命令，不执行

> `validate.sh` 仅做 Python 转发，核心逻辑在 `validate.py`。

### 2.3 PowerShell 包装器 (Windows)

```powershell
./scripts/ai/validate.ps1 -Command single -Platform EditMode -Filter "xFrame.Tests.DITests"
./scripts/ai/validate.ps1 -Command suite -Platform PlayMode
./scripts/ai/validate.ps1 -Command full
```

常用参数：
- `-Unity "<path>"`
- `-ProjectPath "<path>"`
- `-DryRun`

> `validate.ps1` 仅做 Python 转发，核心逻辑在 `validate.py`。

## 3. AI 代码生成与自验证流程

1. 先修改受影响模块（`Runtime` / `Editor` / `Tests`），保持最小改动。
2. 如果改了框架行为，先补或更新对应测试（优先 EditMode）。
3. 先跑最小验证：`single` 或 `suite --platform EditMode`。
4. 再跑回归验证：`full`（EditMode + PlayMode）。
5. 失败时先看 `Temp/AIValidation/logs/*.log`，修复后重复 3-4。

## 4. 产物约定

- 测试结果 XML：`Temp/AIValidation/*.xml`
- 执行日志：`Temp/AIValidation/logs/*.log`

这样 AI 可以稳定解析结果文件，减少“跑了但没法判断是否通过”的情况。

## 5. Definition of Done (AI 任务完成标准)

- 相关测试通过（至少受影响模块的最小验证）
- 无新增编译错误（Unity Console / batch log）
- 变更说明包含：改动点 + 验证命令 + 结果
