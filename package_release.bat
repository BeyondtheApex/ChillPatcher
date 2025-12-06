@echo off
REM ChillPatcher - Package Release Script
REM Creates a ZIP file for distribution

setlocal EnableDelayedExpansion

echo ========================================
echo ChillPatcher Package Script
echo ========================================

cd /d %~dp0

REM 检查 release 目录是否存在
if not exist "release\ChillPatcher" (
    echo ERROR: Release directory not found!
    echo Please run build_release.bat first.
    exit /b 1
)

REM 获取版本号（从 MyPluginInfo.cs 读取）
for /f "tokens=2 delims==" %%a in ('findstr /C:"PLUGIN_VERSION" MyPluginInfo.cs') do (
    set "LINE=%%a"
)
REM 清理字符串
set VERSION=%LINE: =%
set VERSION=%VERSION:;=%
set VERSION=%VERSION:"=%

if "%VERSION%"=="" set VERSION=unknown

echo.
echo Version: %VERSION%
echo.

REM 创建发布包目录
set PackageDir=%~dp0packages
if not exist "%PackageDir%" mkdir "%PackageDir%"

set ZipFile=%PackageDir%\ChillPatcher_%VERSION%.zip

REM 删除旧的 zip 文件
if exist "%ZipFile%" del /f "%ZipFile%"

REM 使用 PowerShell 创建 ZIP
echo Creating ZIP package...
powershell -NoProfile -Command "Compress-Archive -Path 'release\ChillPatcher' -DestinationPath '%ZipFile%' -Force"

if %errorlevel% neq 0 (
    echo ERROR: Failed to create ZIP package!
    exit /b 1
)

echo.
echo ========================================
echo Package Created!
echo ========================================
echo.
echo Package: %ZipFile%
echo.

REM 显示包大小
for %%F in ("%ZipFile%") do (
    echo Size: %%~zF bytes
)

echo.
echo ========================================

endlocal
exit /b 0
