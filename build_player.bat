@echo off
REM OmniMixPlayer - Complete Build Script
REM Builds C# backend + modules + Flutter GUI → playerbuild/
REM Usage: build_player.bat [skip-flutter] [full]
REM   (no args)     - Build C# projects + Flutter
REM   skip-flutter  - Build C# only, skip Flutter
REM   full          - Also clean + restore NuGet packages

setlocal EnableDelayedExpansion

set SKIP_FLUTTER=0
set FULL_BUILD=0
for %%a in (%*) do (
    if /i "%%a"=="skip-flutter" set SKIP_FLUTTER=1
    if /i "%%a"=="full" set FULL_BUILD=1
)

echo ========================================
echo OmniMixPlayer Build Script
echo ========================================
if %FULL_BUILD% equ 1 echo   [full mode: clean + restore]
if %SKIP_FLUTTER% equ 1 echo   [skipping Flutter]
echo.

REM ═══════════════════════════════════════════
REM  Paths
REM ═══════════════════════════════════════════
cd /d "%~dp0OmniMixPlayer"

set Configuration=Release
set PlayerBuildDir=%~dp0playerbuild
set BackendBuildDir=%~dp0OmniMixPlayer\bin\Backend
set SdkBuildDir=%~dp0OmniMixPlayer\bin\SDK
set ModulesBuildDir=%~dp0OmniMixPlayer\bin\Modules
set FlutterDir=%~dp0OmniMixPlayer\gui_flutter
set FlutterBuildDir=%FlutterDir%\build\windows\x64\runner\Release

REM Clean old build
echo [0] Cleaning playerbuild...
if exist "%PlayerBuildDir%" rmdir /s /q "%PlayerBuildDir%"
mkdir "%PlayerBuildDir%"
mkdir "%PlayerBuildDir%\modules"
mkdir "%PlayerBuildDir%\native\x64"

REM ═══════════════════════════════════════════
REM  Step 1: Build SDK
REM ═══════════════════════════════════════════
echo.
echo [1/8] Building OmniMixPlayer.SDK...
if %FULL_BUILD% equ 1 (
    dotnet restore OmniMixPlayer.SDK\OmniMixPlayer.SDK.csproj
)
dotnet build OmniMixPlayer.SDK\OmniMixPlayer.SDK.csproj -c %Configuration% --no-restore
if %errorlevel% neq 0 (
    echo ERROR: SDK build failed!
    exit /b 1
)

REM ═══════════════════════════════════════════
REM  Step 2: Build Backend
REM ═══════════════════════════════════════════
echo.
echo [2/8] Building OmniMixPlayer.Backend...
if %FULL_BUILD% equ 1 (
    dotnet restore OmniMixPlayer.Backend\OmniMixPlayer.Backend.csproj
)
dotnet build OmniMixPlayer.Backend\OmniMixPlayer.Backend.csproj -c %Configuration% --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Backend build failed!
    exit /b 1
)

REM ═══════════════════════════════════════════
REM  Step 3-7: Build Modules
REM ═══════════════════════════════════════════
REM Module ID mapping: source dir → moduleId (used as folder name)
REM The folder name MUST match moduleId for DependencyLoader native resolution

call :BuildModule 3 "LocalFolder" "com.chillpatcher.localfolder"
if %errorlevel% neq 0 exit /b 1

call :BuildModule 4 "Netease" "com.chillpatcher.netease"
if %errorlevel% neq 0 exit /b 1

call :BuildModule 5 "Bilibili" "com.chillpatcher.bilibili"
if %errorlevel% neq 0 exit /b 1

call :BuildModule 6 "QQMusic" "com.chillpatcher.qqmusic"
if %errorlevel% neq 0 exit /b 1

call :BuildModule 7 "Spotify" "com.chillpatcher.spotify"
if %errorlevel% neq 0 exit /b 1

REM ═══════════════════════════════════════════
REM  Step 8: Copy everything to playerbuild
REM ═══════════════════════════════════════════
echo.
echo [8/8] Assembling playerbuild...

REM --- Backend ---
echo   - Backend...
robocopy "%BackendBuildDir%" "%PlayerBuildDir%" /E /NFL /NDL /NJH /NJS /NP /XD native modules config >nul 2>&1
REM Copy backend's native DLLs
if exist "%BackendBuildDir%\Native\x64\*.dll" (
    copy /y "%BackendBuildDir%\Native\x64\*.dll" "%PlayerBuildDir%\native\x64\" >nul
    echo     Native decoders copied
)

REM --- Modules (rename dirs to moduleId) ---
echo   - Modules...

call :CopyModule "LocalFolder" "com.chillpatcher.localfolder"
call :CopyModule "Netease" "com.chillpatcher.netease"
call :CopyModule "Bilibili" "com.chillpatcher.bilibili"
call :CopyModule "QQMusic" "com.chillpatcher.qqmusic"
call :CopyModule "Spotify" "com.chillpatcher.spotify"

REM --- Flutter GUI ---
if %SKIP_FLUTTER% equ 1 (
    echo   - Flutter GUI: SKIPPED
    goto :Done
)

echo   - Flutter GUI...
echo     Building Flutter...
pushd "%FlutterDir%"
call flutter build windows --release
popd
if %errorlevel% neq 0 (
    echo ERROR: Flutter build failed!
    exit /b 1
)

if exist "%FlutterBuildDir%" (
    robocopy "%FlutterBuildDir%" "%PlayerBuildDir%" /E /NFL /NDL /NJH /NJS /NP >nul 2>&1
    echo     Flutter GUI copied
) else (
    echo     WARNING: Flutter build output not found at %FlutterBuildDir%
)

:Done
echo.
echo ========================================
echo Build Complete!
echo ========================================
echo.
echo Output Directory: %PlayerBuildDir%
echo.
echo Directory Structure:
echo   playerbuild\
echo   +-- OmniMixPlayer.Backend.exe
echo   +-- OmniMixPlayer.Backend.dll
echo   +-- OmniMixPlayer.SDK.dll
echo   +-- OmniMixPlayer.SDK.xml
echo   +-- OmniMixPlayer.Gui.exe  (Flutter)
echo   +-- native\
echo   ^|   +-- x64\
echo   ^|       +-- ChillAudioDecoder.dll
echo   ^|       +-- ChillFlacDecoder.dll
echo   +-- modules\
echo   ^|   +-- com.chillpatcher.localfolder\
echo   ^|   ^|   +-- OmniMixPlayer.Module.LocalFolder.dll
echo   ^|   ^|   +-- TagLibSharp.dll
echo   ^|   ^|   +-- Newtonsoft.Json.dll
echo   ^|   ^|   +-- System.Data.SQLite.dll
echo   ^|   ^|   +-- native\x64\SQLite.Interop.dll
echo   ^|   +-- com.chillpatcher.netease\
echo   ^|   ^|   +-- OmniMixPlayer.Module.Netease.dll
echo   ^|   ^|   +-- Newtonsoft.Json.dll
echo   ^|   ^|   +-- QRCoder.dll
echo   ^|   ^|   +-- TagLibSharp.dll
echo   ^|   ^|   +-- native\x64\ChillNetease.dll
echo   ^|   +-- com.chillpatcher.bilibili\
echo   ^|   ^|   +-- OmniMixPlayer.Module.Bilibili.dll
echo   ^|   ^|   +-- Newtonsoft.Json.dll
echo   ^|   +-- com.chillpatcher.qqmusic\
echo   ^|   ^|   +-- OmniMixPlayer.Module.QQMusic.dll
echo   ^|   ^|   +-- Newtonsoft.Json.dll
echo   ^|   ^|   +-- QRCoder.dll
echo   ^|   ^|   +-- native\x64\ChillQQMusic.dll
echo   ^|   +-- com.chillpatcher.spotify\
echo   ^|       +-- OmniMixPlayer.Module.Spotify.dll
echo   ^|       +-- Newtonsoft.Json.dll
echo   +-- data\  (Flutter assets)
echo   +-- *.dll  (Flutter engine + backend deps)
echo.
echo To run (process mode - GUI auto-spawns backend):
echo   playerbuild\omnimix_gui.exe
echo.
echo To run (service mode):
echo   sc.exe create OmniMixPlayerBackend binPath= "%PlayerBuildDir%OmniMixPlayer.Backend.exe" start=demand
echo   playerbuild\omnimix_gui.exe
echo.
echo To test backend alone:
echo   playerbuild\OmniMixPlayer.Backend.exe
echo.
echo ========================================

endlocal
exit /b 0

REM ═══════════════════════════════════════════
REM  Subroutines
REM ═══════════════════════════════════════════

:BuildModule
REM %1 = step number, %2 = source folder name, %3 = moduleId
echo.
echo [%1/8] Building module: %~2...
set "ModuleProj=modules\%~2\ChillPatcher.Module.%~2.csproj"
if %FULL_BUILD% equ 1 (
    dotnet restore "%ModuleProj%"
)
dotnet build "%ModuleProj%" -c %Configuration% --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Module %~2 build failed!
    exit /b 1
)
exit /b 0

:CopyModule
REM %1 = source folder name, %2 = moduleId (destination folder name)
set "SrcDir=%ModulesBuildDir%\%~1"
set "DstDir=%PlayerBuildDir%\modules\%~2"

if not exist "%SrcDir%" (
    echo     WARNING: Module output not found: %SrcDir%
    exit /b 0
)

echo     %~1 -^> modules/%~2/
mkdir "%DstDir%"

REM Copy all DLLs (SDK.dll is excluded by Private=False in csproj)
copy /y "%SrcDir%\*.dll" "%DstDir%\" >nul 2>&1
copy /y "%SrcDir%\*.json" "%DstDir%\" >nul 2>&1
copy /y "%SrcDir%\*.png" "%DstDir%\" >nul 2>&1

REM Copy module-specific native DLLs
if exist "%SrcDir%\native\x64\*.dll" (
    mkdir "%DstDir%\native\x64" 2>nul
    copy /y "%SrcDir%\native\x64\*.dll" "%DstDir%\native\x64\" >nul
    echo       + native DLLs
)

REM Copy module source native DLLs (pre-built, not from build output)
set "ModuleSrcDir=%~dp0OmniMixPlayer\modules\%~1"
if exist "%ModuleSrcDir%\native\x64\*.dll" (
    mkdir "%DstDir%\native\x64" 2>nul
    copy /y "%ModuleSrcDir%\native\x64\*.dll" "%DstDir%\native\x64\" >nul
)

exit /b 0
