@echo off
setlocal

:: Change to script directory
cd /d "%~dp0"

echo Building QQ Music Bridge DLL...

:: Set Go environment
set CGO_ENABLED=1
set GOOS=windows
set GOARCH=amd64

:: 设置 Go 路径（如果不在 PATH 中）
where go >nul 2>&1 || (
    if exist "C:\Program Files\Go\bin\go.exe" set "PATH=C:\Program Files\Go\bin;%PATH%"
    if exist "D:\Program Files\Go\bin\go.exe" set "PATH=D:\Program Files\Go\bin;%PATH%"
)

:: 设置 C 编译器（优先用 CC 环境变量，否则自动找）
:: clang -fuse-ld=lld 用 LLVM lld 替代 MSVC link.exe，兼容 Go cgo
if not defined CC (
    where clang >nul 2>&1 && set CC=clang -fuse-ld=lld
)
if not defined CC (
    where gcc >nul 2>&1 && set CC=gcc
)
if defined CC echo Using C compiler: %CC%

:: Download dependencies
echo Downloading dependencies...
go mod tidy

:: Build DLL
echo Compiling...
go build -buildmode=c-shared -o ChillQQMusic.dll -ldflags "-s -w" .

if %ERRORLEVEL% equ 0 (
    echo.
    echo Build successful: ChillQQMusic.dll
) else (
    echo.
    echo Build failed!
)

if "%1"=="" pause
