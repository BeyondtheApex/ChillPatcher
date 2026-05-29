@echo off
REM OmniMixPlayer - Complete Build Script
REM Builds native integrations, C# backend/modules, and optional Flutter GUI into playerbuild.
REM Usage: build_player.bat [skip-flutter] [full]
REM   (no args)     - Build native integrations + C# projects + Flutter
REM   skip-flutter  - Build native integrations + C# projects only
REM   full          - Also restore NuGet packages

setlocal EnableExtensions EnableDelayedExpansion
goto :Main

:BuildModule
REM %1 = step number, %2 = source folder name, %3 = moduleId
echo.
echo [%~1/8] Building module: %~2...
set "ModuleProj=modules\%~2\ChillPatcher.Module.%~2.csproj"
if "%FULL_BUILD%"=="1" (
    dotnet restore "%ModuleProj%"
    if errorlevel 1 (
        echo ERROR: Module %~2 restore failed!
        exit /b 1
    )
)
dotnet build "%ModuleProj%" -c "%Configuration%" --no-restore
if errorlevel 1 (
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
mkdir "%DstDir%" 2>nul

copy /y "%SrcDir%\*.dll" "%DstDir%\" >nul 2>&1
copy /y "%SrcDir%\*.json" "%DstDir%\" >nul 2>&1
copy /y "%SrcDir%\*.png" "%DstDir%\" >nul 2>&1

if exist "%SrcDir%\native\x64\*.dll" (
    mkdir "%DstDir%\native\x64" 2>nul
    copy /y "%SrcDir%\native\x64\*.dll" "%DstDir%\native\x64\" >nul
    echo       + native DLLs
)
if exist "%SrcDir%\native\x64\*.exe" (
    mkdir "%DstDir%\native\x64" 2>nul
    copy /y "%SrcDir%\native\x64\*.exe" "%DstDir%\native\x64\" >nul
    echo       + native EXEs
)

set "ModuleSrcDir=%RootDir%OmniMixPlayer\modules\%~1"
if exist "%ModuleSrcDir%\native\x64\*.dll" (
    mkdir "%DstDir%\native\x64" 2>nul
    copy /y "%ModuleSrcDir%\native\x64\*.dll" "%DstDir%\native\x64\" >nul
)
if exist "%ModuleSrcDir%\native\x64\*.exe" (
    mkdir "%DstDir%\native\x64" 2>nul
    copy /y "%ModuleSrcDir%\native\x64\*.exe" "%DstDir%\native\x64\" >nul
)

exit /b 0

:Main
set "RootDir=%~dp0"
set "SKIP_FLUTTER=0"
set "FULL_BUILD=0"
for %%a in (%*) do (
    if /i "%%~a"=="skip-flutter" set "SKIP_FLUTTER=1"
    if /i "%%~a"=="full" set "FULL_BUILD=1"
)

set "Configuration=Release"
set "PlayerBuildDir=%RootDir%playerbuild"
set "BackendBuildDir=%RootDir%OmniMixPlayer\bin\Backend"
set "SdkBuildDir=%RootDir%OmniMixPlayer\bin\SDK"
set "ModulesBuildDir=%RootDir%OmniMixPlayer\bin\Modules"
set "FlutterDir=%RootDir%OmniMixPlayer\gui_flutter"
set "FlutterBuildDir=%FlutterDir%\build\windows\x64\runner\Release"

echo ========================================
echo OmniMixPlayer Build Script
echo ========================================
if "%FULL_BUILD%"=="1" echo   [full mode: clean + restore]
if "%SKIP_FLUTTER%"=="1" echo   [skipping Flutter]
echo.

echo [0] Cleaning playerbuild...
if exist "%PlayerBuildDir%" rmdir /s /q "%PlayerBuildDir%"
mkdir "%PlayerBuildDir%"
mkdir "%PlayerBuildDir%\modules"
mkdir "%PlayerBuildDir%\native\x64"
mkdir "%PlayerBuildDir%\integrations\fh6"

echo.
echo [native] Building OmniPcmShared...
pushd "%RootDir%NativePlugins\OmniPcmShared"
call build.bat
set "BUILD_RESULT=%ERRORLEVEL%"
popd
if not "%BUILD_RESULT%"=="0" (
    echo ERROR: OmniPcmShared build failed!
    exit /b 1
)

echo.
echo [native] Building Forza Horizon 6 bridge...
pushd "%RootDir%NativePlugins\ForzaHorizon6OmniBridge"
call build.bat
set "BUILD_RESULT=%ERRORLEVEL%"
popd
if not "%BUILD_RESULT%"=="0" (
    echo ERROR: Forza Horizon 6 bridge build failed!
    exit /b 1
)

echo.
echo [native] Building Spotify librespot bridge...
pushd "%RootDir%NativePlugins\SpotifyLibrespotBridge"
call build.bat
set "BUILD_RESULT=%ERRORLEVEL%"
popd
if not "%BUILD_RESULT%"=="0" (
    echo ERROR: Spotify librespot bridge build failed!
    exit /b 1
)

pushd "%RootDir%OmniMixPlayer"

echo.
echo [1/8] Building OmniMixPlayer.SDK...
if "%FULL_BUILD%"=="1" (
    dotnet restore OmniMixPlayer.SDK\OmniMixPlayer.SDK.csproj
    if errorlevel 1 (
        echo ERROR: SDK restore failed!
        popd
        exit /b 1
    )
)
dotnet build OmniMixPlayer.SDK\OmniMixPlayer.SDK.csproj -c "%Configuration%" --no-restore
if errorlevel 1 (
    echo ERROR: SDK build failed!
    popd
    exit /b 1
)

echo.
echo [2/8] Building OmniMixPlayer.Backend...
if "%FULL_BUILD%"=="1" (
    dotnet restore OmniMixPlayer.Backend\OmniMixPlayer.Backend.csproj
    if errorlevel 1 (
        echo ERROR: Backend restore failed!
        popd
        exit /b 1
    )
)
dotnet build OmniMixPlayer.Backend\OmniMixPlayer.Backend.csproj -c "%Configuration%" --no-restore
if errorlevel 1 (
    echo ERROR: Backend build failed!
    popd
    exit /b 1
)

call :BuildModule 3 "LocalFolder" "com.chillpatcher.localfolder"
if errorlevel 1 (
    popd
    exit /b 1
)
call :BuildModule 4 "Netease" "com.chillpatcher.netease"
if errorlevel 1 (
    popd
    exit /b 1
)
call :BuildModule 5 "Bilibili" "com.chillpatcher.bilibili"
if errorlevel 1 (
    popd
    exit /b 1
)
call :BuildModule 6 "QQMusic" "com.chillpatcher.qqmusic"
if errorlevel 1 (
    popd
    exit /b 1
)
call :BuildModule 7 "Spotify" "com.chillpatcher.spotify"
if errorlevel 1 (
    popd
    exit /b 1
)

popd

echo.
echo [8/8] Assembling playerbuild...

echo   - Backend...
robocopy "%BackendBuildDir%" "%PlayerBuildDir%" /E /NFL /NDL /NJH /NJS /NP /XD native modules config >nul 2>&1
if exist "%BackendBuildDir%\Native\x64\*.dll" (
    copy /y "%BackendBuildDir%\Native\x64\*.dll" "%PlayerBuildDir%\native\x64\" >nul
    echo     Native decoders copied
)

echo   - Modules...
call :CopyModule "LocalFolder" "com.chillpatcher.localfolder"
call :CopyModule "Netease" "com.chillpatcher.netease"
call :CopyModule "Bilibili" "com.chillpatcher.bilibili"
call :CopyModule "QQMusic" "com.chillpatcher.qqmusic"
call :CopyModule "Spotify" "com.chillpatcher.spotify"

echo   - Game integrations...
if exist "%RootDir%NativePlugins\ForzaHorizon6OmniBridge\bin\version.dll" (
    copy /y "%RootDir%NativePlugins\ForzaHorizon6OmniBridge\bin\version.dll" "%PlayerBuildDir%\integrations\fh6\" >nul
) else (
    echo     WARNING: FH6 version.dll not found
)
if exist "%RootDir%bin\native\x64\OmniPcmShared.dll" (
    copy /y "%RootDir%bin\native\x64\OmniPcmShared.dll" "%PlayerBuildDir%\integrations\fh6\" >nul
) else (
    echo     WARNING: OmniPcmShared.dll not found
)

echo   - FH6 Flutter asset...
call "%RootDir%build_fh6_omni_bridge_assets.bat" skip-build
if errorlevel 1 (
    echo ERROR: FH6 Flutter asset packaging failed!
    exit /b 1
)

if "%SKIP_FLUTTER%"=="1" (
    echo   - Flutter GUI: SKIPPED
    goto :Done
)

echo   - Flutter GUI...
echo     Building Flutter...
pushd "%FlutterDir%"
call flutter build windows --release
set "BUILD_RESULT=%ERRORLEVEL%"
popd
if not "%BUILD_RESULT%"=="0" (
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
echo   ^|   +-- com.chillpatcher.netease\
echo   ^|   +-- com.chillpatcher.bilibili\
echo   ^|   +-- com.chillpatcher.qqmusic\
echo   ^|   +-- com.chillpatcher.spotify\
echo   +-- integrations\
echo   ^|   +-- fh6\
echo   ^|       +-- version.dll
echo   ^|       +-- OmniPcmShared.dll
echo   +-- data\  (Flutter assets)
echo   +-- *.dll  (Flutter engine + backend deps)
echo.
echo To run (process mode - GUI auto-spawns backend):
echo   playerbuild\omnimix_gui.exe
echo.
echo To run (service mode):
echo   sc.exe create OmniMixPlayerBackend binPath= "%PlayerBuildDir%\OmniMixPlayer.Backend.exe" start=demand
echo   playerbuild\omnimix_gui.exe
echo.
echo To test backend alone:
echo   playerbuild\OmniMixPlayer.Backend.exe
echo.
echo ========================================

endlocal
exit /b 0
