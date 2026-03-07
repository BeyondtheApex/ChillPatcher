@echo off
setlocal

:: Change to script directory
cd /d "%~dp0"

echo Building QQ Music Bridge DLL...

:: Set Go environment
set CGO_ENABLED=1
set GOOS=windows
set GOARCH=amd64

:: Add Go to path
set PATH=C:\Program Files\Go\bin;%PATH%

:: Add GCC to path if not already (TDM-GCC is commonly used)
if exist "C:\TDM-GCC-64\bin" set PATH=C:\TDM-GCC-64\bin;%PATH%
if exist "C:\mingw64\bin" set PATH=C:\mingw64\bin;%PATH%
if exist "C:\Users\JinCao\Downloads\x86_64-15.2.0-release-posix-seh-ucrt-rt_v13-rev0\mingw64\bin" set PATH=C:\Users\JinCao\Downloads\x86_64-15.2.0-release-posix-seh-ucrt-rt_v13-rev0\mingw64\bin;%PATH%

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
