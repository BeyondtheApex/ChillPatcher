@echo off
REM OmniPcmShared - Build Script for Windows
REM Builds both x64 and x86 versions

setlocal

set BUILD_DIR=%~dp0build
set OUTPUT_DIR=%~dp0..\..\bin\native

echo ========================================
echo OmniPcmShared Native SDK Build Script
echo ========================================

where cmake >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: CMake not found in PATH
    exit /b 1
)

where cl >nul 2>&1
if %errorlevel% neq 0 (
    echo Searching for Visual Studio...
    call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1
    if %errorlevel% neq 0 (
        call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x64 >nul 2>&1
    )
)

echo.
echo Building x64 version...
set BUILD_X64=%BUILD_DIR%\x64
mkdir "%BUILD_X64%" 2>nul
cd /d "%BUILD_X64%"

cmake -A x64 -DCMAKE_BUILD_TYPE=Release ../..
if %errorlevel% neq 0 exit /b 1

cmake --build . --config Release
if %errorlevel% neq 0 exit /b 1

echo.
echo Building x86 version...
set BUILD_X86=%BUILD_DIR%\x86
mkdir "%BUILD_X86%" 2>nul
cd /d "%BUILD_X86%"

cmake -A Win32 -DCMAKE_BUILD_TYPE=Release ../..
if %errorlevel% neq 0 exit /b 1

cmake --build . --config Release
if %errorlevel% neq 0 exit /b 1

echo.
echo Installing binaries...
mkdir "%OUTPUT_DIR%\x64" 2>nul
mkdir "%OUTPUT_DIR%\x86" 2>nul

copy /Y "%BUILD_X64%\bin\Release\OmniPcmShared.dll" "%OUTPUT_DIR%\x64\" >nul
copy /Y "%BUILD_X86%\bin\Release\OmniPcmShared.dll" "%OUTPUT_DIR%\x86\" >nul

echo.
echo ========================================
echo Build Complete!
echo x64 DLL: %OUTPUT_DIR%\x64\OmniPcmShared.dll
echo x86 DLL: %OUTPUT_DIR%\x86\OmniPcmShared.dll
echo ========================================

cd /d %~dp0
exit /b 0
