@echo off
setlocal EnableExtensions
pushd "%~dp0"

echo [OmniAudioDecoder] Building with Symphonia 0.6...
cargo build --release
if errorlevel 1 (
    echo [OmniAudioDecoder] Build FAILED
    popd
    exit /b 1
)

echo [OmniAudioDecoder] Copying DLLs...

:: Copy to global bin (build_all.py source)
if not exist "..\..\bin\native\x64" mkdir "..\..\bin\native\x64"
copy /y "target\release\omni_audio_decoder.dll" "..\..\bin\native\x64\OmniAudioDecoder.dll" >nul
copy /y "target\release\omni_audio_decoder.dll" "..\..\bin\native\x64\ChillAudioDecoder.dll" >nul
copy /y "target\release\omni_audio_decoder.dll" "..\..\bin\native\x64\ChillFlacDecoder.dll" >nul

:: Copy to OmniMixPlayer.Backend native
if not exist "..\..\OmniMixPlayer\OmniMixPlayer.Backend\native\x64" mkdir "..\..\OmniMixPlayer\OmniMixPlayer.Backend\native\x64"
copy /y "target\release\omni_audio_decoder.dll" "..\..\OmniMixPlayer\OmniMixPlayer.Backend\native\x64\OmniAudioDecoder.dll" >nul
copy /y "target\release\omni_audio_decoder.dll" "..\..\OmniMixPlayer\OmniMixPlayer.Backend\native\x64\ChillAudioDecoder.dll" >nul
copy /y "target\release\omni_audio_decoder.dll" "..\..\OmniMixPlayer\OmniMixPlayer.Backend\native\x64\ChillFlacDecoder.dll" >nul

:: Copy to chillPatcher native (game mod)
if not exist "..\..\mods\chillPatcher\bin\native\x64" mkdir "..\..\mods\chillPatcher\bin\native\x64"
copy /y "target\release\omni_audio_decoder.dll" "..\..\mods\chillPatcher\bin\native\x64\OmniAudioDecoder.dll" >nul
copy /y "target\release\omni_audio_decoder.dll" "..\..\mods\chillPatcher\bin\native\x64\ChillAudioDecoder.dll" >nul
copy /y "target\release\omni_audio_decoder.dll" "..\..\mods\chillPatcher\bin\native\x64\ChillFlacDecoder.dll" >nul

echo [OmniAudioDecoder] Done. Copied to bin/ + Backend + chillPatcher (as OmniAudioDecoder + ChillAudioDecoder + ChillFlacDecoder)
popd
exit /b 0
