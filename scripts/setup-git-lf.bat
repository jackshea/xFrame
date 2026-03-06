@echo off
setlocal

git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Not inside a Git repository.
    exit /b 1
)

git config --local core.autocrlf false
if errorlevel 1 goto :config_error

git config --local core.eol lf
if errorlevel 1 goto :config_error

git config --local core.safecrlf true
if errorlevel 1 goto :config_error

echo [OK] Local Git line-ending settings applied:
echo   core.autocrlf =
git config --local --get core.autocrlf
echo   core.eol =
git config --local --get core.eol
echo   core.safecrlf =
git config --local --get core.safecrlf

echo.
echo Optional: run "git add --renormalize ." to normalize tracked files to LF.
exit /b 0

:config_error
echo [ERROR] Failed to write local Git configuration.
exit /b 1
