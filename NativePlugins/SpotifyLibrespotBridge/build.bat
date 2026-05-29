@echo off
setlocal EnableExtensions
pushd "%~dp0"

cargo build --release --locked
if errorlevel 1 (
    popd
    exit /b 1
)

if not exist "..\..\OmniMixPlayer\modules\Spotify\native\x64" mkdir "..\..\OmniMixPlayer\modules\Spotify\native\x64"
copy /y "target\release\spotify_librespot_bridge.dll" "..\..\OmniMixPlayer\modules\Spotify\native\x64\SpotifyLibrespotBridge.dll" >nul

popd
exit /b 0
