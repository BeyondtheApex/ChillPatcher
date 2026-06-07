# =============================================================================
# OmniMixPlayer 安装程序构建脚本
# 用法: .\build_installer.ps1 [-Version "1.0.0"] [-SkipVCRedistCheck]
# =============================================================================

param(
    [string]$Version,
    [switch]$SkipVCRedistCheck
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path "$ScriptDir\.."

# ═══ 路径配置 ═══
$InnoSetup = "C:\Program Files\Inno Setup 7\ISCC.exe"
$IssFile = "$ScriptDir\installer\OmniMixPlayer.iss"
$PlayerBuildDir = "$ProjectRoot\playerbuild"
$OutputDir = "$ProjectRoot\release"
$VCRedistFile = "$PlayerBuildDir\VC_redist.x64.exe"

# ═══ 读取版本号 ═══
if (-not $Version) {
    $VersionInfoFile = "$PlayerBuildDir\version_info.json"
    if (Test-Path $VersionInfoFile) {
        $VersionInfo = Get-Content $VersionInfoFile -Raw | ConvertFrom-Json
        $Version = $VersionInfo.flutter_version -replace '\+.*$', ''
    }
    if (-not $Version) {
        $Version = "1.0.0"
    }
}

Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   OmniMixPlayer Installer Builder           ║" -ForegroundColor Cyan
Write-Host "╠══════════════════════════════════════════════╣" -ForegroundColor Cyan
Write-Host "║ Version: $Version" -ForegroundColor Cyan
Write-Host "║ Output:  $OutputDir" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ═══ 检查 Inno Setup 是否安装 ═══
if (-not (Test-Path $InnoSetup)) {
    Write-Host "[ERROR] Inno Setup 7 未找到: $InnoSetup" -ForegroundColor Red
    Write-Host "请安装 Inno Setup 7: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    exit 1
}
Write-Host "[OK] 找到 Inno Setup 7: $InnoSetup" -ForegroundColor Green

# ═══ 检查 playerbuild 目录 ═══
if (-not (Test-Path $PlayerBuildDir)) {
    Write-Host "[ERROR] playerbuild 目录不存在: $PlayerBuildDir" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] playerbuild 目录存在" -ForegroundColor Green

# ═══ 检查关键文件 ═══
$RequiredFiles = @(
    "$PlayerBuildDir\omnimix_gui.exe",
    "$PlayerBuildDir\OmniMixPlayer.Backend.exe"
)
foreach ($f in $RequiredFiles) {
    if (-not (Test-Path $f)) {
        Write-Host "[ERROR] 缺失文件: $f" -ForegroundColor Red
        exit 1
    }
}
Write-Host "[OK] 主程序文件检查通过" -ForegroundColor Green

# ═══ 检查 VC_redist.x64.exe ═══
if (-not $SkipVCRedistCheck) {
    if (-not (Test-Path $VCRedistFile)) {
        Write-Host "[WARN] VC_redist.x64.exe 未找到在 playerbuild 目录" -ForegroundColor Yellow
        Write-Host "       将从 Microsoft 下载..." -ForegroundColor Yellow
        
        $VCRedistUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe"
        Write-Host "       下载地址: $VCRedistUrl" -ForegroundColor Gray
        
        try {
            Invoke-WebRequest -Uri $VCRedistUrl -OutFile $VCRedistFile -UseBasicParsing
            Write-Host "[OK] VC_redist.x64.exe 下载完成" -ForegroundColor Green
        }
        catch {
            Write-Host "[ERROR] 下载 VC_redist.x64.exe 失败: $_" -ForegroundColor Red
            Write-Host "请手动下载并放到: $VCRedistFile" -ForegroundColor Yellow
            exit 1
        }
    }
    else {
        Write-Host "[OK] VC_redist.x64.exe 已存在" -ForegroundColor Green
    }
}

# ═══ 创建输出目录 ═══
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# ═══ 更新 .iss 文件中的版本号 ═══
Write-Host ""
Write-Host "[INFO] 更新版本号为 $Version ..." -ForegroundColor White

$IssContent = Get-Content $IssFile -Raw -Encoding UTF8
$IssContent = $IssContent -replace '#define MyAppVersion ".*?"', "#define MyAppVersion ""$Version"""
$IssContent = $IssContent -replace 'OutputBaseFilename=OmniMixPlayer_V.*?_installer', "OutputBaseFilename=OmniMixPlayer_V${Version}_installer"
Set-Content $IssFile -Value $IssContent -Encoding UTF8 -NoNewline
# 确保文件末尾有换行
Add-Content $IssFile -Value "" -Encoding UTF8

Write-Host "[OK] 版本号已更新" -ForegroundColor Green

# ═══ 编译安装程序 ═══
Write-Host ""
Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   正在编译安装程序...                        ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$Process = Start-Process -FilePath $InnoSetup -ArgumentList "`"$IssFile`"" -NoNewWindow -Wait -PassThru

if ($Process.ExitCode -eq 0) {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║   构建成功!                                  ║" -ForegroundColor Green
    Write-Host "╠══════════════════════════════════════════════╣" -ForegroundColor Green
    Write-Host "║ 输出: $OutputDir\OmniMixPlayer_V${Version}_installer.exe" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "[ERROR] 编译失败，退出代码: $($Process.ExitCode)" -ForegroundColor Red
    exit $Process.ExitCode
}
