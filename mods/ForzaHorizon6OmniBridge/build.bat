@echo off
setlocal

set BUILD_DIR=%~dp0build
set OUTPUT_DIR=%~dp0bin

echo ========================================
echo Forza Horizon 6 Omni Bridge Build Script
echo ========================================

where cl >nul 2>&1
if %errorlevel% neq 0 (
    :: CMake uses vswhere internally to find VS compiler — no need for vcvarsall
    echo NOTE: cl.exe not in PATH, CMake will auto-detect via vswhere
)

where cmake >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: CMake not found in PATH
    exit /b 1
)

mkdir "%BUILD_DIR%" 2>nul
cd /d "%BUILD_DIR%"

cmake -A x64 -DCMAKE_BUILD_TYPE=Release ..
if %errorlevel% neq 0 exit /b 1

cmake --build . --config Release
if %errorlevel% neq 0 exit /b 1

mkdir "%OUTPUT_DIR%" 2>nul
if exist "%BUILD_DIR%\Release\version.dll" copy /Y "%BUILD_DIR%\Release\version.dll" "%OUTPUT_DIR%\" >nul
if exist "%BUILD_DIR%\version.dll" copy /Y "%BUILD_DIR%\version.dll" "%OUTPUT_DIR%\" >nul
if exist "%OUTPUT_DIR%\Release\version.dll" copy /Y "%OUTPUT_DIR%\Release\version.dll" "%OUTPUT_DIR%\version.dll" >nul

echo.
echo Build Complete: %OUTPUT_DIR%\version.dll
cd /d "%~dp0"
exit /b 0
