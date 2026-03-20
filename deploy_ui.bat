@echo off
REM UI 热部署脚本 - 复制 UI 源码到游戏目录（支持热重载）
REM 排除 node_modules 和 @outputs 目录

setlocal

set SRC=%~dp0ui\window-manager
set DEST=F:\SteamLibrary\steamapps\common\wallpaper_engine\projects\myprojects\chill_with_you\BepInEx\plugins\ChillPatcher\ui\window-manager

if not exist "%SRC%" (
    echo ERROR: Source directory not found: %SRC%
    exit /b 1
)

echo Deploying UI to game directory...
echo   From: %SRC%
echo   To:   %DEST%

robocopy "%SRC%" "%DEST%" /MIR /XD node_modules @outputs /NFL /NDL /NJH /NJS /NP

if %errorlevel% leq 7 (
    echo UI deployed successfully.
) else (
    echo WARNING: robocopy returned error code %errorlevel%
)

endlocal
