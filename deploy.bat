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

REM 清理旧文件
if exist "%PluginDir%" (
    echo Cleaning old installation...
    rmdir /s /q "%PluginDir%"
)

REM 复制新文件
echo Copying files...
xcopy /s /i /q /y "release\ChillPatcher" "%PluginDir%"

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
