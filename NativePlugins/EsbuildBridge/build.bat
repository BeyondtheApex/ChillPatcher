@echo off
REM ChillPatcher - EsbuildBridge Go DLL Build Script
REM Requires: Go 1.21+, GCC (MinGW-w64 or TDM-GCC) for CGO
REM Output: bin\native\x64\ChillEsbuildBridge.dll

setlocal
cd /d %~dp0

echo Building EsbuildBridge (Go c-shared DLL)...

REM 设置 C 编译器（优先 clang，fallback gcc）
if not defined CC (
    where clang >nul 2>&1 && set CC=clang -fuse-ld=lld
)
if not defined CC (
    where gcc >nul 2>&1 && set CC=gcc
)
if defined CC echo Using C compiler: %CC%

REM Ensure dependencies
go mod tidy
if %errorlevel% neq 0 (
    echo ERROR: go mod tidy failed
    exit /b 1
)

set CGO_ENABLED=1
set GOOS=windows
set GOARCH=amd64

go build -buildmode=c-shared -trimpath -ldflags="-s -w" -o ..\..\bin\native\x64\ChillEsbuildBridge.dll .
if %errorlevel% neq 0 (
    echo ERROR: Go build failed!
    exit /b 1
)

REM Remove .h file (not needed, C# uses P/Invoke)
if exist "..\..\bin\native\x64\ChillEsbuildBridge.h" del "..\..\bin\native\x64\ChillEsbuildBridge.h"

echo Build succeeded: bin\native\x64\ChillEsbuildBridge.dll
