# Git LF Line Ending Setup

本仓库统一使用 `LF`（`\n`）作为文本文件换行符。

## 包含内容

- `.gitattributes`：强制 Git 以 `LF` 处理文本文件
- `.editorconfig`：约束编辑器默认使用 `LF`
- `scripts/setup-git-lf.sh`：Linux / WSL 设置脚本
- `scripts/setup-git-lf.bat`：Windows 设置脚本

## 建议执行顺序

1. Windows 用户执行：`scripts\setup-git-lf.bat`
2. Linux / WSL 用户执行：`bash scripts/setup-git-lf.sh`
3. 如需把已跟踪文件统一到 `LF`，再执行：`git add --renormalize .`

## 脚本写入的 Git 本地配置

- `core.autocrlf=false`
- `core.eol=lf`
- `core.safecrlf=true`

## 说明

- 这些设置只负责约束后续 Git 行为和编辑器默认行为。
- 是否批量转换现有文件，由使用者自行决定。
- 如果工作区里已有未提交改动，建议先确认再执行 `git add --renormalize .`。
