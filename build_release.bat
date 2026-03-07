@echo off
REM ChillPatcher - Complete Build and Release Script
REM Builds all projects and creates release directory structure
REM Usage: build_release.bat [full]
REM   (no args)  - Quick build: compile only, reuse existing licenses/native
REM   full       - Full build: compile + native plugins + license collection

setlocal EnableDelayedExpansion

REM 检查 full 参数
set FULL_BUILD=0
if /i "%1"=="full" set FULL_BUILD=1

if %FULL_BUILD% equ 1 (
    echo ========================================
    echo ChillPatcher FULL Build Script
    echo ========================================
) else (
    echo ========================================
    echo ChillPatcher Quick Build Script
    echo   ^(use "build_release.bat full" for full build^)
    echo ========================================
)

REM 切换到项目根目录
cd /d %~dp0

REM 配置
set Configuration=Release

set ReleaseDir=%~dp0release
set PluginDir=%ReleaseDir%\ChillPatcher
set ModulesDir=%PluginDir%\Modules
set NativeDir=%PluginDir%\native
set LicenseDir=%PluginDir%\licenses
set NugetLicenseDir=%LicenseDir%\nuget
set NpmLicenseDir=%LicenseDir%\npm

REM 清理旧的发布目录
echo.
echo [0/8] Cleaning release directory...
if exist "%ReleaseDir%" rmdir /s /q "%ReleaseDir%"
mkdir "%PluginDir%"
mkdir "%ModulesDir%"
mkdir "%NativeDir%\x64"
mkdir "%PluginDir%\SDK"

REM ========== Step 1: Build SDK ==========
echo.
echo [1/8] Building ChillPatcher.SDK...
dotnet build ChillPatcher.SDK\ChillPatcher.SDK.csproj -c %Configuration% --no-restore
if %errorlevel% neq 0 (
    echo ERROR: SDK build failed!
    exit /b 1
)

REM ========== Step 2: Build Main Plugin ==========
echo.
echo [2/8] Building ChillPatcher (Main Plugin)...
dotnet build ChillPatcher.csproj -c %Configuration% --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Main plugin build failed!
    exit /b 1
)

REM ========== Step 3: Build Modules ==========
echo.
echo [3/8] Building ChillPatcher.Module.LocalFolder...
dotnet build ChillPatcher.Module.LocalFolder\ChillPatcher.Module.LocalFolder.csproj -c %Configuration% --no-restore
if %errorlevel% neq 0 (
    echo ERROR: LocalFolder module build failed!
    exit /b 1
)

echo.
echo [4/8] Building ChillPatcher.Module.Netease...
dotnet build ChillPatcher.Module.Netease\ChillPatcher.Module.Netease.csproj -c %Configuration% --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Netease module build failed!
    exit /b 1
)

echo.
echo [5/8] Building ChillPatcher.Module.Bilibili...
dotnet build ChillPatcher.Module.Bilibili\ChillPatcher.Module.Bilibili.csproj -c %Configuration% --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Bilibili module build failed!
    exit /b 1
)

echo.
echo [6/9] Building ChillPatcher.Module.QQMusic...
dotnet build ChillPatcher.Module.QQMusic\ChillPatcher.Module.QQMusic.csproj -c %Configuration% --no-restore
if %errorlevel% neq 0 (
    echo ERROR: QQMusic module build failed!
    exit /b 1
)

REM ========== Step 7: Build OneJS ==========
echo.
echo [7/9] Building ChillPatcher.OneJS...
dotnet build ChillPatcher.OneJS\ChillPatcher.OneJS.csproj -c %Configuration% --no-restore
if %errorlevel% neq 0 (
    echo ERROR: OneJS build failed!
    exit /b 1
)

REM ========== Step 7.5: Build OneJS UI (esbuild) ==========
echo.
echo [7.5/9] Building OneJS UI (Preact + esbuild)...
cd ui\default
if not exist "node_modules" (
    echo   - Installing npm dependencies...
    call npm install
    if %errorlevel% neq 0 (
        echo ERROR: npm install failed!
        cd ..\..
        exit /b 1
    )
)
echo   - Bundling UI with esbuild...
call npm run build
if %errorlevel% neq 0 (
    echo ERROR: esbuild build failed!
    cd ..\..
    exit /b 1
)
cd ..\..

REM ========== Step 7: Build Native Plugins (Only in full mode) ==========
if %FULL_BUILD% equ 1 (
echo.
echo [8/9] Building Native Plugins...

if exist "NativePlugins\AudioDecoder\build.bat" (
    echo   - Building Audio Decoder...
    cd NativePlugins\AudioDecoder
    call build.bat >nul 2>&1
    if %errorlevel% neq 0 (
        echo WARNING: Native audio decoder build failed, using existing if available
    )
    cd ..\..
)

if exist "NativePlugins\FlacDecoder\build.bat" (
    echo   - Building FLAC Decoder...
    cd NativePlugins\FlacDecoder
    call build.bat >nul 2>&1
    if %errorlevel% neq 0 (
        echo WARNING: Native FLAC decoder build failed, using existing if available
    )
    cd ..\..
)

if exist "NativePlugins\SmtcBridge\build.bat" (
    echo   - Building SMTC Bridge...
    cd NativePlugins\SmtcBridge
    call build.bat >nul 2>&1
    if %errorlevel% neq 0 (
        echo WARNING: Native SMTC bridge build failed, using existing if available
    )
    cd ..\..
)

if exist "netease_bridge\build.bat" (
    echo   - Building Netease Bridge...
    cd netease_bridge
    call build.bat --no-pause >nul 2>&1
    if %errorlevel% neq 0 (
        echo WARNING: Netease bridge build failed, using existing if available
    )
    cd ..
)

if exist "qqmusic_bridge\build.bat" (
    echo   - Building QQ Music Bridge...
    cd qqmusic_bridge
    call build.bat --no-pause >nul 2>&1
    if %errorlevel% neq 0 (
        echo WARNING: QQ Music bridge build failed, using existing if available
    )
    cd ..
)
) else (
echo.
echo [8/9] Native Plugins: SKIPPED ^(quick build^)
)

REM ========== Step 8: Copy files to release directory ==========
echo.
echo [9/9] Copying files to release directory...

REM Main Plugin
echo   - Main Plugin files...
copy /y "bin\ChillPatcher.dll" "%PluginDir%\" >nul

REM SDK (for developers)
echo   - SDK files...
copy /y "bin\SDK\ChillPatcher.SDK.dll" "%PluginDir%\SDK\" >nul

REM Dependencies (主插件依赖)
echo   - Dependencies...
if exist "bin\NAudio.Core.dll" copy /y "bin\NAudio.Core.dll" "%PluginDir%\" >nul
if exist "bin\NAudio.Wasapi.dll" copy /y "bin\NAudio.Wasapi.dll" "%PluginDir%\" >nul

REM OneJS Runtime (脚本引擎)
echo   - OneJS Runtime...
if exist "ChillPatcher.OneJS\bin\ChillPatcher.OneJS.dll" copy /y "ChillPatcher.OneJS\bin\ChillPatcher.OneJS.dll" "%PluginDir%\" >nul
if exist "ChillPatcher.OneJS\bin\ExCSS.Unity.dll" copy /y "ChillPatcher.OneJS\bin\ExCSS.Unity.dll" "%PluginDir%\" >nul
if exist "ChillPatcher.OneJS\bin\DotNet.Glob.dll" copy /y "ChillPatcher.OneJS\bin\DotNet.Glob.dll" "%PluginDir%\" >nul
if exist "ChillPatcher.OneJS\bin\NUglify.dll" copy /y "ChillPatcher.OneJS\bin\NUglify.dll" "%PluginDir%\" >nul
if exist "ChillPatcher.OneJS\bin\ICSharpCode.SharpZipLib.dll" copy /y "ChillPatcher.OneJS\bin\ICSharpCode.SharpZipLib.dll" "%PluginDir%\" >nul
if exist "ChillPatcher.OneJS\bin\IndexRange.dll" copy /y "ChillPatcher.OneJS\bin\IndexRange.dll" "%PluginDir%\" >nul
if exist "ChillPatcher.OneJS\bin\System.Memory.dll" copy /y "ChillPatcher.OneJS\bin\System.Memory.dll" "%PluginDir%\" >nul
if exist "ChillPatcher.OneJS\bin\System.Buffers.dll" copy /y "ChillPatcher.OneJS\bin\System.Buffers.dll" "%PluginDir%\" >nul
if exist "ChillPatcher.OneJS\bin\System.Runtime.CompilerServices.Unsafe.dll" copy /y "ChillPatcher.OneJS\bin\System.Runtime.CompilerServices.Unsafe.dll" "%PluginDir%\" >nul

REM OneJS 默认 UI 脚本 (递归复制完整 ui 源码，排除 node_modules，以便用户自定义)
echo   - OneJS UI source files...
set "UIDir=%PluginDir%\ui"
if not exist "%UIDir%" mkdir "%UIDir%"
robocopy "ui" "%UIDir%" /E /XD node_modules /NFL /NDL /NJH /NJS /NP >nul 2>&1

REM Native Plugins (只需 x64，放在 native/x64/)
echo   - Native plugins...
if exist "bin\native\x64\ChillAudioDecoder.dll" copy /y "bin\native\x64\ChillAudioDecoder.dll" "%NativeDir%\x64\" >nul
if exist "bin\native\x64\ChillFlacDecoder.dll" copy /y "bin\native\x64\ChillFlacDecoder.dll" "%NativeDir%\x64\" >nul
if exist "bin\native\x64\ChillSmtcBridge.dll" copy /y "bin\native\x64\ChillSmtcBridge.dll" "%NativeDir%\x64\" >nul
if exist "bin\native\x64\ChillNetease.dll" copy /y "bin\native\x64\ChillNetease.dll" "%NativeDir%\x64\" >nul
if exist "ChillPatcher.OneJS\native\x64\puerts.dll" copy /y "ChillPatcher.OneJS\native\x64\puerts.dll" "%NativeDir%\x64\" >nul

REM VC++ Runtime DLLs (from lib folder)
echo   - VC++ Runtime DLLs...
if exist "lib\vcruntime140.dll" copy /y "lib\vcruntime140.dll" "%NativeDir%\x64\" >nul
if exist "lib\vcruntime140_1.dll" copy /y "lib\vcruntime140_1.dll" "%NativeDir%\x64\" >nul
if exist "lib\msvcp140.dll" copy /y "lib\msvcp140.dll" "%NativeDir%\x64\" >nul
if exist "lib\concrt140.dll" copy /y "lib\concrt140.dll" "%NativeDir%\x64\" >nul

REM RIME library (from librime build)
if exist "rime\librime\build\bin\Release\rime.dll" (
    echo   - RIME library...
    copy /y "rime\librime\build\bin\Release\rime.dll" "%PluginDir%\" >nul
)

REM Modules
echo   - Modules...

REM LocalFolder 模块 (ID: com.chillpatcher.localfolder)
set "LocalFolderModuleDir=%ModulesDir%\com.chillpatcher.localfolder"
if not exist "%LocalFolderModuleDir%" mkdir "%LocalFolderModuleDir%"
if not exist "%LocalFolderModuleDir%\native" mkdir "%LocalFolderModuleDir%\native"
if not exist "%LocalFolderModuleDir%\native\x64" mkdir "%LocalFolderModuleDir%\native\x64"
copy /y "ChillPatcher.Module.LocalFolder\bin\ChillPatcher.Module.LocalFolder.dll" "%LocalFolderModuleDir%\" >nul
REM LocalFolder 模块的依赖
copy /y "ChillPatcher.Module.LocalFolder\bin\System.Data.SQLite.dll" "%LocalFolderModuleDir%\" >nul
copy /y "ChillPatcher.Module.LocalFolder\bin\Newtonsoft.Json.dll" "%LocalFolderModuleDir%\" >nul
copy /y "ChillPatcher.Module.LocalFolder\bin\TagLibSharp.dll" "%LocalFolderModuleDir%\" >nul
REM SQLite 原生库复制到模块的 native 目录
if exist "ChillPatcher.Module.LocalFolder\bin\native\x64\SQLite.Interop.dll" (
    copy /y "ChillPatcher.Module.LocalFolder\bin\native\x64\SQLite.Interop.dll" "%LocalFolderModuleDir%\native\x64\" >nul
)

REM Netease 模块 (ID: com.chillpatcher.netease)
echo   - Netease Module...
set "NeteaseModuleDir=%ModulesDir%\com.chillpatcher.netease"
if not exist "%NeteaseModuleDir%" mkdir "%NeteaseModuleDir%"
if not exist "%NeteaseModuleDir%\native" mkdir "%NeteaseModuleDir%\native"
if not exist "%NeteaseModuleDir%\native\x64" mkdir "%NeteaseModuleDir%\native\x64"
copy /y "ChillPatcher.Module.Netease\bin\ChillPatcher.Module.Netease.dll" "%NeteaseModuleDir%\" >nul
REM Netease 模块的依赖
copy /y "ChillPatcher.Module.Netease\bin\Newtonsoft.Json.dll" "%NeteaseModuleDir%\" >nul
if exist "ChillPatcher.Module.Netease\bin\QRCoder.dll" copy /y "ChillPatcher.Module.Netease\bin\QRCoder.dll" "%NeteaseModuleDir%\" >nul
REM Netease 模块原生库
if exist "bin\native\x64\ChillNetease.dll" (
    copy /y "bin\native\x64\ChillNetease.dll" "%NeteaseModuleDir%\native\x64\" >nul
)

REM Bilibili 模块 (ID: com.chillpatcher.bilibili)
echo   - Bilibili Module...
set "BilibiliModuleDir=%ModulesDir%\com.chillpatcher.bilibili"
if not exist "%BilibiliModuleDir%" mkdir "%BilibiliModuleDir%"
copy /y "ChillPatcher.Module.Bilibili\bin\ChillPatcher.Module.Bilibili.dll" "%BilibiliModuleDir%\" >nul
REM Bilibili 模块的依赖
copy /y "ChillPatcher.Module.Bilibili\bin\Newtonsoft.Json.dll" "%BilibiliModuleDir%\" >nul

REM QQ Music 模块 (ID: com.chillpatcher.qqmusic)
echo   - QQ Music Module...
set "QQMusicModuleDir=%ModulesDir%\com.chillpatcher.qqmusic"
if not exist "%QQMusicModuleDir%" mkdir "%QQMusicModuleDir%"
if not exist "%QQMusicModuleDir%\native" mkdir "%QQMusicModuleDir%\native"
if not exist "%QQMusicModuleDir%\native\x64" mkdir "%QQMusicModuleDir%\native\x64"
copy /y "ChillPatcher.Module.QQMusic\bin\ChillPatcher.Module.QQMusic.dll" "%QQMusicModuleDir%\" >nul
REM QQ Music 模块的依赖
copy /y "ChillPatcher.Module.QQMusic\bin\Newtonsoft.Json.dll" "%QQMusicModuleDir%\" >nul
if exist "ChillPatcher.Module.QQMusic\bin\QRCoder.dll" copy /y "ChillPatcher.Module.QQMusic\bin\QRCoder.dll" "%QQMusicModuleDir%\" >nul
REM QQ Music 模块原生库
if exist "qqmusic_bridge\ChillQQMusic.dll" (
    copy /y "qqmusic_bridge\ChillQQMusic.dll" "%QQMusicModuleDir%\native\x64\" >nul
)

REM RIME data directory (rime-data/shared 和 rime-data/user)
echo   - RIME data...
set RimeDataDir=%PluginDir%\rime-data
set RimeSharedDir=%RimeDataDir%\shared
set RimeUserDir=%RimeDataDir%\user
set OpenCCDir=%RimeSharedDir%\opencc

if not exist "%RimeSharedDir%" mkdir "%RimeSharedDir%"
if not exist "%RimeUserDir%" mkdir "%RimeUserDir%"
if not exist "%OpenCCDir%" mkdir "%OpenCCDir%"

REM Copy prelude (基础配置文件)
if exist "rime\rime-schemas\prelude\symbols.yaml" copy /y "rime\rime-schemas\prelude\symbols.yaml" "%RimeSharedDir%\" >nul
if exist "rime\rime-schemas\prelude\punctuation.yaml" copy /y "rime\rime-schemas\prelude\punctuation.yaml" "%RimeSharedDir%\" >nul
if exist "rime\rime-schemas\prelude\key_bindings.yaml" copy /y "rime\rime-schemas\prelude\key_bindings.yaml" "%RimeSharedDir%\" >nul

REM Copy custom default.yaml (使用我们的配置)
if exist "rime\RimeDefaultConfig\default.yaml" copy /y "rime\RimeDefaultConfig\default.yaml" "%RimeSharedDir%\" >nul
if exist "rime\RimeDefaultConfig\luna_pinyin.custom.yaml" copy /y "rime\RimeDefaultConfig\luna_pinyin.custom.yaml" "%RimeSharedDir%\" >nul

REM Copy essay (语言模型)
if exist "rime\rime-schemas\essay\essay.txt" copy /y "rime\rime-schemas\essay\essay.txt" "%RimeSharedDir%\" >nul

REM Copy luna_pinyin schemas
if exist "rime\rime-schemas\luna-pinyin\luna_pinyin.schema.yaml" copy /y "rime\rime-schemas\luna-pinyin\luna_pinyin.schema.yaml" "%RimeSharedDir%\" >nul
if exist "rime\rime-schemas\luna-pinyin\luna_pinyin.dict.yaml" copy /y "rime\rime-schemas\luna-pinyin\luna_pinyin.dict.yaml" "%RimeSharedDir%\" >nul
if exist "rime\rime-schemas\luna-pinyin\pinyin.yaml" copy /y "rime\rime-schemas\luna-pinyin\pinyin.yaml" "%RimeSharedDir%\" >nul

REM Copy stroke dependency
if exist "rime\rime-schemas\stroke\stroke.schema.yaml" copy /y "rime\rime-schemas\stroke\stroke.schema.yaml" "%RimeSharedDir%\" >nul
if exist "rime\rime-schemas\stroke\stroke.dict.yaml" copy /y "rime\rime-schemas\stroke\stroke.dict.yaml" "%RimeSharedDir%\" >nul

REM Copy double_pinyin schemas
if exist "rime\rime-schemas\double-pinyin\double_pinyin.schema.yaml" copy /y "rime\rime-schemas\double-pinyin\double_pinyin.schema.yaml" "%RimeSharedDir%\" >nul
if exist "rime\rime-schemas\double-pinyin\double_pinyin_abc.schema.yaml" copy /y "rime\rime-schemas\double-pinyin\double_pinyin_abc.schema.yaml" "%RimeSharedDir%\" >nul
if exist "rime\rime-schemas\double-pinyin\double_pinyin_flypy.schema.yaml" copy /y "rime\rime-schemas\double-pinyin\double_pinyin_flypy.schema.yaml" "%RimeSharedDir%\" >nul
if exist "rime\rime-schemas\double-pinyin\double_pinyin_mspy.schema.yaml" copy /y "rime\rime-schemas\double-pinyin\double_pinyin_mspy.schema.yaml" "%RimeSharedDir%\" >nul

REM Copy OpenCC data files (繁简转换必需)
if exist "rime\librime\share\opencc\*.json" copy /y "rime\librime\share\opencc\*.json" "%OpenCCDir%\" >nul 2>&1
if exist "rime\librime\share\opencc\*.ocd2" copy /y "rime\librime\share\opencc\*.ocd2" "%OpenCCDir%\" >nul 2>&1

REM Resources (if exists)
if exist "Resources" (
    echo   - Resources...
    if not exist "%PluginDir%\Resources" mkdir "%PluginDir%\Resources"
    xcopy /s /q /y "Resources\*" "%PluginDir%\Resources\" >nul 2>&1
)

REM License files (only in full mode; quick build reuses existing)
if %FULL_BUILD% equ 1 (
echo   - License files...
if not exist "%LicenseDir%" mkdir "%LicenseDir%"
if exist "LICENSE" copy /y "LICENSE" "%LicenseDir%\ChillPatcher-LICENSE.txt" >nul
if exist "rime\librime\LICENSE" copy /y "rime\librime\LICENSE" "%LicenseDir%\librime-LICENSE.txt" >nul
if exist "NativePlugins\dr_libs\LICENSE" copy /y "NativePlugins\dr_libs\LICENSE" "%LicenseDir%\dr_libs-LICENSE.txt" >nul
if exist "NativePlugins\fdk-aac\NOTICE" copy /y "NativePlugins\fdk-aac\NOTICE" "%LicenseDir%\fdk-aac-NOTICE.txt" >nul
if exist "NativePlugins\minimp4\LICENSE" copy /y "NativePlugins\minimp4\LICENSE" "%LicenseDir%\minimp4-LICENSE.txt" >nul
if exist "ChillPatcher.OneJS\LICENSE-OneJS.txt" copy /y "ChillPatcher.OneJS\LICENSE-OneJS.txt" "%LicenseDir%\OneJS-LICENSE.txt" >nul
if exist "ChillPatcher.OneJS\LICENSE-Puerts.txt" copy /y "ChillPatcher.OneJS\LICENSE-Puerts.txt" "%LicenseDir%\Puerts-LICENSE.txt" >nul
if exist "ChillPatcher.Module.Netease\LICENSE-go-musicfox.txt" copy /y "ChillPatcher.Module.Netease\LICENSE-go-musicfox.txt" "%LicenseDir%\go-musicfox-LICENSE.txt" >nul

REM NuGet package licenses (via dotnet-project-licenses)
echo   - NuGet package licenses...
if not exist "%NugetLicenseDir%" mkdir "%NugetLicenseDir%"
where dotnet-project-licenses >nul 2>&1
if %errorlevel% equ 0 (
    dotnet-project-licenses -i ChillPatcher.csproj -e -f "%NugetLicenseDir%" -u -c --packages-filter build\nuget-packages-filter.json -l Error >nul 2>&1
    dotnet-project-licenses -i ChillPatcher.Module.LocalFolder\ChillPatcher.Module.LocalFolder.csproj -e -f "%NugetLicenseDir%" -u -c --packages-filter build\nuget-packages-filter.json -l Error >nul 2>&1
    dotnet-project-licenses -i ChillPatcher.Module.Netease\ChillPatcher.Module.Netease.csproj -e -f "%NugetLicenseDir%" -u -c --packages-filter build\nuget-packages-filter.json -l Error >nul 2>&1
    dotnet-project-licenses -i ChillPatcher.Module.Bilibili\ChillPatcher.Module.Bilibili.csproj -e -f "%NugetLicenseDir%" -u -c --packages-filter build\nuget-packages-filter.json -l Error >nul 2>&1
    dotnet-project-licenses -i ChillPatcher.Module.QQMusic\ChillPatcher.Module.QQMusic.csproj -e -f "%NugetLicenseDir%" -u -c --packages-filter build\nuget-packages-filter.json -l Error >nul 2>&1
    dotnet-project-licenses -i ChillPatcher.OneJS\ChillPatcher.OneJS.csproj -e -f "%NugetLicenseDir%" -u -c --packages-filter build\nuget-packages-filter.json -l Error >nul 2>&1
    REM Remove JSON summary (only keep license text files)
    if exist "%NugetLicenseDir%\licenses.json" del /q "%NugetLicenseDir%\licenses.json"
    echo     NuGet licenses collected.
) else (
    echo     WARNING: dotnet-project-licenses not installed, skipping NuGet license collection.
    echo     Install with: dotnet tool install --global dotnet-project-licenses --version 2.4.0
)

REM npm package licenses (via license-checker)
echo   - npm package licenses...
if not exist "%NpmLicenseDir%" mkdir "%NpmLicenseDir%"
cd ui\default
where npx >nul 2>&1
if %errorlevel% equ 0 (
    call npx license-checker --production --json --out "%NpmLicenseDir%\licenses.json" >nul 2>&1
    call npx license-checker --production --csv --out "%NpmLicenseDir%\licenses.csv" >nul 2>&1
    echo     npm licenses collected.
) else (
    echo     WARNING: npx not found, skipping npm license collection.
)
cd ..\..
) else (
echo   - Licenses: SKIPPED ^(quick build, reusing existing^)
)

echo.
echo ========================================
echo Build Complete!
echo ========================================
echo.
echo Release Directory: %ReleaseDir%
echo.
echo Directory Structure:
echo   ChillPatcher\
echo   +-- ChillPatcher.dll            (Main Plugin)
echo   +-- NAudio.*.dll
echo   +-- rime.dll                    (RIME library)
echo   +-- licenses\                   (License files)
echo   ^|   +-- nuget\                 (NuGet package licenses)
echo   ^|   +-- npm\                   (npm package licenses)
echo   +-- native\
echo   ^|   +-- x64\
echo   ^|       +-- vcruntime140*.dll   (VC++ Runtime)
echo   ^|       +-- msvcp140.dll
echo   ^|       +-- concrt140.dll
echo   ^|       +-- ChillAudioDecoder.dll
echo   ^|       +-- ChillFlacDecoder.dll
echo   ^|       +-- ChillSmtcBridge.dll
echo   ^|       +-- ChillNetease.dll
echo   +-- rime-data\
echo   ^|   +-- shared\                 (RIME schemas and dictionaries)
echo   ^|   ^|   +-- *.yaml, *.txt
echo   ^|   ^|   +-- opencc\             (OpenCC data)
echo   ^|   +-- user\                   (User data, empty initially)
echo   +-- SDK\
echo   ^|   +-- ChillPatcher.SDK.dll    (For module developers)
echo   +-- Modules\
echo       +-- LocalFolder\
echo       ^|   +-- ChillPatcher.Module.LocalFolder.dll
echo       ^|   +-- TagLibSharp.dll     (For module cover loading)
echo       ^|   +-- System.Data.SQLite.dll
echo       ^|   +-- Newtonsoft.Json.dll
echo       ^|   +-- native\
echo       ^|       +-- x64\
echo       ^|           +-- SQLite.Interop.dll
echo       +-- Netease\
echo           +-- ChillPatcher.Module.Netease.dll
echo           +-- Newtonsoft.Json.dll
echo.
echo To deploy: Copy ChillPatcher folder to
echo   ^<game^>\BepInEx\plugins\
echo.
echo ========================================

endlocal
exit /b 0
