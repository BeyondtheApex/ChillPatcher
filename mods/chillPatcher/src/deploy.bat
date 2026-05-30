@echo off
REM ChillPatcher - Deploy to Game Script
REM Deploys the release build to the game directory

setlocal

echo ========================================
echo ChillPatcher Deploy Script
echo ========================================

cd /d %~dp0

REM 默认游戏目录
set GameDir=F:\SteamLibrary\steamapps\common\wallpaper_engine\projects\myprojects\chill_with_you

REM 允许通过参数指定游戏目录
if not "%1"=="" set GameDir=%~1

set PluginDir=%GameDir%\BepInEx\plugins\ChillPatcher

REM 检查 release 目录是否存在
if not exist "release\ChillPatcher" (
    echo ERROR: Release directory not found!
    echo Please run build_release.bat first.
    exit /b 1
)

REM 检查游戏目录是否存在
if not exist "%GameDir%\BepInEx" (
    echo ERROR: Game directory not found or BepInEx not installed!
    echo Expected: %GameDir%\BepInEx
    exit /b 1
)

echo.
echo Deploying to: %PluginDir%
echo.

REM 清理旧文件（保留用户数据目录）
if exist "%PluginDir%" (
    echo Cleaning old installation...
    REM 备份用户数据
    if exist "%PluginDir%\cameras" (
        echo Preserving cameras folder...
        move /y "%PluginDir%\cameras" "%TEMP%\ChillPatcher_cameras_backup" >nul 2>&1
    )
    rmdir /s /q "%PluginDir%"
)

REM 复制新文件
echo Copying files...
xcopy /s /i /q /y "release\ChillPatcher" "%PluginDir%"

echo Writing steam_appid.txt...
> "%GameDir%\steam_appid.txt" echo 3548580

REM 恢复用户数据
if exist "%TEMP%\ChillPatcher_cameras_backup" (
    echo Restoring cameras folder...
    move /y "%TEMP%\ChillPatcher_cameras_backup" "%PluginDir%\cameras" >nul 2>&1
)

if %errorlevel% neq 0 (
    echo ERROR: Failed to copy files!
    exit /b 1
)

echo.
echo ========================================
echo Deploy Complete!
echo ========================================
echo.
echo Deployed to: %PluginDir%
echo.
echo ========================================

endlocal
exit /b 0
