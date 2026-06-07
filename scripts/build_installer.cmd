@echo off
REM =============================================================================
REM OmniMixPlayer 安装程序构建 (批处理入口)
REM 用法: build_installer.cmd
REM =============================================================================

cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0build_installer.ps1" %*
if %ERRORLEVEL% NEQ 0 (
    pause
    exit /b %ERRORLEVEL%
)
