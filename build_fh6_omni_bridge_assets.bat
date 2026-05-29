@echo off
setlocal EnableExtensions

set "RootDir=%~dp0"
set "SkipBuild=0"
if /i "%~1"=="skip-build" set "SkipBuild=1"

set "StageDir=%RootDir%release\FH6OmniBridge"
set "ZipPath=%RootDir%release\FH6OmniBridge.zip"
set "AssetsDir=%RootDir%OmniMixPlayer\gui_flutter\assets"
set "VersionDll=%RootDir%NativePlugins\ForzaHorizon6OmniBridge\bin\version.dll"
set "SharedDll=%RootDir%bin\native\x64\OmniPcmShared.dll"

echo ========================================
echo FH6 Omni Bridge Asset Packager
echo ========================================

if "%SkipBuild%"=="0" (
    echo.
    echo [1/3] Building OmniPcmShared...
    pushd "%RootDir%NativePlugins\OmniPcmShared"
    call build.bat
    set "BUILD_RESULT=%ERRORLEVEL%"
    popd
    if not "%BUILD_RESULT%"=="0" (
        echo ERROR: OmniPcmShared build failed!
        exit /b 1
    )

    echo.
    echo [2/3] Building Forza Horizon 6 Omni Bridge...
    pushd "%RootDir%NativePlugins\ForzaHorizon6OmniBridge"
    call build.bat
    set "BUILD_RESULT=%ERRORLEVEL%"
    popd
    if not "%BUILD_RESULT%"=="0" (
        echo ERROR: Forza Horizon 6 Omni Bridge build failed!
        exit /b 1
    )
) else (
    echo.
    echo [1/3] Native build skipped.
)

echo.
echo [3/3] Packaging Flutter asset...
if not exist "%VersionDll%" (
    echo ERROR: Missing %VersionDll%
    exit /b 1
)
if not exist "%SharedDll%" (
    echo ERROR: Missing %SharedDll%
    exit /b 1
)

if exist "%StageDir%" rmdir /s /q "%StageDir%"
mkdir "%StageDir%" 2>nul
mkdir "%AssetsDir%" 2>nul

copy /y "%VersionDll%" "%StageDir%\version.dll" >nul
copy /y "%SharedDll%" "%StageDir%\OmniPcmShared.dll" >nul
if exist "%RootDir%NativePlugins\ForzaHorizon6OmniBridge\README.md" (
    copy /y "%RootDir%NativePlugins\ForzaHorizon6OmniBridge\README.md" "%StageDir%\README.md" >nul
)

if exist "%ZipPath%" del /f /q "%ZipPath%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%StageDir%\*' -DestinationPath '%ZipPath%' -Force"
if errorlevel 1 (
    echo ERROR: Failed to create %ZipPath%
    exit /b 1
)

copy /y "%ZipPath%" "%AssetsDir%\FH6OmniBridge.zip" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy FH6OmniBridge.zip to Flutter assets.
    exit /b 1
)

echo.
echo Done: %AssetsDir%\FH6OmniBridge.zip
endlocal
exit /b 0
