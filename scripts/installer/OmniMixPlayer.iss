; =============================================================================
; OmniMixPlayer Installer Script
; 使用 Inno Setup 7 构建: "C:\Program Files\Inno Setup 7\ISCC.exe" this_script.iss
; =============================================================================

#define MyAppName "OmniMixPlayer"
#define MyAppVersion "3.0"
#define MyAppPublisher "Kevin-2483"
#define MyAppURL "https://github.com/Kevin-2483/Chill"
#define MyAppExeName "omnimix_gui.exe"
#define MyAppBackendName "OmniMixPlayer.Backend.exe"
#define MyServiceName "OmniMixPlayerBackend"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only).
;PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=G:\Csharp\Chill\release
OutputBaseFilename=OmniMixPlayer_V3.0_installer
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; 最小 Windows 版本: Windows 10 (10.0.10240) 或 Windows 11 on Arm
MinVersion=10.0.10240

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; ═══ 主程序 ═══
Source: "G:\Csharp\Chill\playerbuild\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "G:\Csharp\Chill\playerbuild\{#MyAppBackendName}"; DestDir: "{app}"; Flags: ignoreversion

; ═══ DLL 文件 ═══
Source: "G:\Csharp\Chill\playerbuild\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; ═══ 配置文件 ═══
Source: "G:\Csharp\Chill\playerbuild\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "G:\Csharp\Chill\playerbuild\*.config"; DestDir: "{app}"; Flags: ignoreversion

; ═══ data 目录 (Flutter 资源) ═══
Source: "G:\Csharp\Chill\playerbuild\data\*"; DestDir: "{app}\data"; Flags: ignoreversion recursesubdirs createallsubdirs

; ═══ wwwroot 目录 (Web GUI) ═══
Source: "G:\Csharp\Chill\playerbuild\wwwroot\*"; DestDir: "{app}\wwwroot"; Flags: ignoreversion recursesubdirs createallsubdirs

; ═══ modules 目录 (插件模块) ═══
Source: "G:\Csharp\Chill\playerbuild\modules\*"; DestDir: "{app}\modules"; Flags: ignoreversion recursesubdirs createallsubdirs

; ═══ native 目录 (原生库) ═══
Source: "G:\Csharp\Chill\playerbuild\native\*"; DestDir: "{app}\native"; Flags: ignoreversion recursesubdirs createallsubdirs

; ═══ VC++ 运行库安装器 (复制到临时目录，安装后删除) ═══
Source: "G:\Csharp\Chill\playerbuild\VC_redist.x64.exe"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall
; NOTE: 不要对共享系统文件使用 "Flags: ignoreversion"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; VC++ 运行库 — 仅在未安装时运行
Filename: "{tmp}\VC_redist.x64.exe"; Parameters: "/quiet /norestart"; \
  StatusMsg: "正在安装 VC++ 运行库..."; Flags: waituntilterminated; \
  Check: ShouldInstallVCRedist

; 安装后启动 GUI
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; 卸载前停止并删除服务
Filename: "sc.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete {#MyServiceName}"; Flags: runhidden

; ═════════════════════════════════════════════════════════════════════════════
; [Code] 部分 — Pascal 脚本
; ═════════════════════════════════════════════════════════════════════════════

[Code]

// ── 检查 VC++ 2015-2022 x64 运行库是否已安装 ──
function IsVCRedistInstalled(): Boolean;
var
  installValue: Cardinal;
begin
  // 检查 Visual Studio 2015-2022 (v14.0+) x64 运行库注册表
  if RegQueryDWordValue(
    HKEY_LOCAL_MACHINE,
    'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64',
    'Installed',
    installValue
  ) then
  begin
    Result := (installValue = 1);
  end
  else
    Result := False;
end;

// ── 决定是否需要安装 VC++ 运行库 ──
function ShouldInstallVCRedist(): Boolean;
begin
  Result := not IsVCRedistInstalled();
end;

// ── 从文件读取内容的辅助函数 ──
function GetFileContents(const FileName: string): string;
var
  Lines: TArrayOfString;
  I: Integer;
begin
  Result := '';
  if LoadStringsFromFile(FileName, Lines) then
  begin
    for I := 0 to GetArrayLength(Lines) - 1 do
      Result := Result + Lines[I] + #13#10;
  end;
end;

// ── 检查服务是否存在 (sc query 不存在时返回 exit code 1060) ──
function ServiceExists(ServiceName: string): Boolean;
var
  ResultCode: Integer;
begin
  Exec('sc.exe', 'query "' + ServiceName + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := (ResultCode = 0);
end;

// ── 检查并停止服务 ──
function StopAndRemoveService(ServiceName: string): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  
  // 停止服务
  if ServiceExists(ServiceName) then
  begin
    Exec('sc.exe', 'stop "' + ServiceName + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    // 等待服务停止（最多等 30 秒）
    Sleep(3000);
    // 删除服务，安装后会重新创建
    Exec('sc.exe', 'delete "' + ServiceName + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;

// ── 检查进程是否在运行 ──
function IsProcessRunning(ProcName: string): Boolean;
var
  ResultCode: Integer;
  TempFile: string;
begin
  TempFile := ExpandConstant('{tmp}\process_check.txt');
  // 使用 tasklist 检查进程
  Exec('cmd.exe', '/c tasklist /FI "IMAGENAME eq ' + ProcName + '" /NH > "' + TempFile + '"',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := FileExists(TempFile) and (Pos(ProcName, GetFileContents(TempFile)) > 0);
  if FileExists(TempFile) then
    DeleteFile(TempFile);
end;

// ── 强制终止进程 ──
procedure KillProcess(ProcName: string);
var
  ResultCode: Integer;
begin
  if IsProcessRunning(ProcName) then
  begin
    Exec('taskkill', '/F /IM "' + ProcName + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    // 等待进程退出
    Sleep(2000);
  end;
end;

// ── 准备安装：初始化时调用 ──
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';

  // 1. 检查并停止 OmniMixPlayerBackend 服务
  if ServiceExists('{#MyServiceName}') then
  begin
    Log('检测到 {#MyServiceName} 服务正在运行，正在停止...');
    StopAndRemoveService('{#MyServiceName}');
  end;

  // 2. 终止 GUI 进程
  if IsProcessRunning('{#MyAppExeName}') then
  begin
    Log('检测到 {#MyAppExeName} 正在运行，正在终止...');
    KillProcess('{#MyAppExeName}');
  end;

  // 3. 终止 Backend 进程（可能以非服务方式运行）
  if IsProcessRunning('{#MyAppBackendName}') then
  begin
    Log('检测到 {#MyAppBackendName} 正在运行，正在终止...');
    KillProcess('{#MyAppBackendName}');
  end;

  // 再次确认所有进程已停止
  Sleep(1000);
end;

// ── 安装完成后重新创建服务 ──
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  BackendPath: string;
begin
  if CurStep = ssPostInstall then
  begin
    BackendPath := ExpandConstant('"{app}\{#MyAppBackendName}"');
    
    // 重新创建 Windows 服务 (手动启动)
    if Exec('sc.exe', 'create {#MyServiceName} binPath= ' + BackendPath + ' start= demand',
           '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      Log('服务 {#MyServiceName} 创建成功');
      
      // 配置 DACL：允许 Authenticated Users (AU) 无提权启停服务
      Exec('sc.exe', 'sdset {#MyServiceName} D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)(A;;CCDCRPWP;;;AU)',
          '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      
      Log('服务 DACL 配置完成');
    end
    else
      Log('警告：服务 {#MyServiceName} 创建失败，请检查权限');
  end;
end;




