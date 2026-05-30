# ForzaRadioBridge

Early reverse-engineering probe for the Forza Horizon 6 radio replacement path.

This builds a `version.dll` proxy. It forwards the normal Windows version APIs
and starts a small probe thread from `DllMain`. The probe writes
`forza_radio_bridge.log` next to the loaded DLL and scans the main game module
for key FMOD/radio strings:

- `RadioStreamFmod`
- `System::createSound`
- `System::playSound`
- `Channel::setChannelGroup`
- `Bus::lockChannelGroup`
- `/Master/Radio/Track/`

Build from an x64 Visual Studio developer shell:

```bat
powershell -ExecutionPolicy Bypass -File "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\Tools\Launch-VsDevShell.ps1" -Arch amd64
build.bat
```

Do not install it for online play. Build output goes to `bin\version.dll`.
