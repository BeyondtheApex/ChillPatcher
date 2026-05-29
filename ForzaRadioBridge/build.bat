@echo off
setlocal
set ROOT=%~dp0
if /i not "%VSCMD_ARG_TGT_ARCH%"=="x64" echo WARNING: build this from an x64 VS developer shell.
if not exist "%ROOT%bin" mkdir "%ROOT%bin"
cl /nologo /utf-8 /std:c++17 /EHa /O2 /LD "%ROOT%version_proxy.cpp" /link /DEF:"%ROOT%version.def" /OUT:"%ROOT%bin\version.dll" /PDB:"%ROOT%bin\version.pdb" psapi.lib ws2_32.lib
endlocal
