# OmniMixPlayer 部署方案

## 一、架构关系

```
┌──────────────────────────────────────────────────────────┐
│                    OmniMixPlayer 体系                      │
│                                                          │
│  ┌─ Flutter GUI ──── Unix Socket ──── C# Backend ────┐  │
│  │  (omnimix_gui.exe)   %TEMP%/       (OmniMixPlayer. │  │
│  │   用户面板          omnimix.sock    Backend.exe)    │  │
│  │   可选运行                          ← 核心服务       │  │
│  │                                    ← 可作Windows    │  │
│  │                                      服务运行       │  │
│  └────────────────────────────────────────────────────┘  │
│                          │                               │
│                          │ HTTP + SharedMemory            │
│                          ▼                               │
│  ┌─ 游戏进程 (BepInEx) ──────────────────────────────┐  │
│  │  ChillPatcher.dll                                    │  │
│  │  ├── OmniMixPlayerClient (HTTP/WS)                  │  │
│  │  └── SharedMemoryReader (PCM)                       │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  ┌─ 浏览器 (手机/平板) ──────────────────────────────┐  │
│  │  http://192.168.1.x:17890                           │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

**关键点**：

- **后端是独立进程**，GUI 和游戏都是它的"客户端"
- 后端可以脱离 GUI 运行（作为 Windows Service 或独立进程）
- 通信走 Unix Domain Socket（本地）或 TCP:17890（远程）
- 模块 DLL、原生解码器 DLL 都放在**后端 exe 旁边**

---

## 二、运行模式

### 模式 A: Process（开发/便携）

```
Flutter GUI 启动 → spawn OmniMixPlayer.Backend.exe 子进程
                 → 通过 Unix Socket 通信
                 → GUI 关闭时 kill 后端
```

- 后端 exe 由 Flutter GUI 的 `BackendManager._findBackendExe()` 查找
- Backend 以 `--mode=standalone` 参数启动
- 适合开发测试，不需要管理员权限

### 模式 B: Service（生产/持久）

```
sc.exe create OmniMixPlayerBackend binPath="C:\...\OmniMixPlayer.Backend.exe" start=demand
Flutter GUI 启动 → sc.exe start OmniMixPlayerBackend
                 → 等待 Unix Socket 出现
                 → 连接后端
```

- 后端作为 Windows Service (demand start) 运行
- `Program.cs` 中已调用 `builder.Host.UseWindowsService()` 支持 SCM
- 游戏 Mod 可以在 GUI 不运行的情况下连接后端
- 需要管理员权限安装服务（仅一次）

---

## 三、部署目录结构

```
C:\Program Files\OmniMixPlayer\          ← 安装根目录
│
├── OmniMixPlayer.Backend.exe            ← C# .NET 8 后端入口
├── OmniMixPlayer.Backend.dll
├── OmniMixPlayer.Backend.runtimeconfig.json
├── *.dll                                ← 所有 .NET 依赖
│
├── OmniMixPlayer.Gui.exe                ← Flutter 桌面 GUI
├── data\                                ← Flutter 引擎文件
│   └── flutter_assets\
│
├── modules\                             ← 模块 DLL（相对于后端exe）
│   ├── com.chillpatcher.localfolder\
│   │   ├── ChillPatcher.Module.LocalFolder.dll
│   │   ├── TagLibSharp.dll
│   │   ├── System.Data.SQLite.dll
│   │   ├── Newtonsoft.Json.dll
│   │   └── native\x64\
│   │       └── SQLite.Interop.dll
│   ├── com.chillpatcher.netease\
│   │   ├── ChillPatcher.Module.Netease.dll
│   │   ├── Newtonsoft.Json.dll
│   │   └── native\x64\
│   │       └── ChillNetease.dll
│   ├── com.chillpatcher.bilibili\
│   │   ├── ChillPatcher.Module.Bilibili.dll
│   │   └── Newtonsoft.Json.dll
│   ├── com.chillpatcher.qqmusic\
│   │   ├── ChillPatcher.Module.QQMusic.dll
│   │   ├── Newtonsoft.Json.dll
│   │   └── native\x64\
│   │       └── ChillQQMusic.dll
│   └── com.chillpatcher.spotify\
│       ├── ChillPatcher.Module.Spotify.dll
│       └── Newtonsoft.Json.dll
│
├── native\x64\                          ← 原生解码器 DLL
│   ├── ChillAudioDecoder.dll
│   ├── ChillFlacDecoder.dll
│   ├── avcodec-*.dll (FFmpeg)
│   └── ...
│
├── config\                              ← 运行时配置（自动创建）
│   ├── modules.json
│   └── global.json
│
└── wwwroot\                             ← Web 静态文件（可选）
```

---

## 四、关键路径解析

### 后端查找模块目录

```csharp
// Program.cs
var pluginPath = AppDomain.CurrentDomain.BaseDirectory;  // = exe 所在目录
var modulesPath = Path.Combine(pluginPath, "modules");    // = exe目录\modules\
```

所以 **modules 目录必须在后端 exe 同级**。

### 后端查找原生解码器

```csharp
// DecoderEngine 初始化
DecoderEngine.Initialize(logger, pluginPath);
// 内部会找 pluginPath/native/x64/*.dll
```

所以 **native 目录也必须在后端 exe 同级**。

### Flutter GUI 查找后端 exe

```dart
// BackendManager._findBackendExe()
// 从 Flutter GUI 所在目录开始，尝试多个相对路径：
//   1. <gui_dir>/OmniMixPlayer.Backend.exe           ← 同目录（部署时）
//   2. <gui_dir>/../OmniMixPlayer.Backend/...exe      ← 开发时
//   3. <gui_dir>/../../../../OmniMixPlayer.Backend/...exe  ← 深层开发路径
```

**部署时**：Flutter GUI 和后端 exe 放在同一目录即可。

### Unix Socket 路径

```csharp
// 后端 Program.cs 和 Flutter main.dart 都使用:
Path.Combine(Path.GetTempPath(), "omnimix.sock")
// Windows: C:\Users\<user>\AppData\Local\Temp\omnimix.sock
```

所有本地客户端自动在此 rendezvous，无需配置。

---

## 五、构建流程

### 当前状态

`build_release.bat` 只构建 **ChillPatcher（游戏插件）**，不含 OmniMixPlayer。

### 需要的构建步骤

```batch
REM 1. 构建后端
dotnet publish OmniMixPlayer\OmniMixPlayer.Backend\OmniMixPlayer.Backend.csproj ^
    -c Release -o release\OmniMixPlayer\

REM 2. 构建模块（复制到 release\OmniMixPlayer\modules\）
dotnet publish OmniMixPlayer\modules\*\*.csproj -c Release

REM 3. 构建 Flutter GUI
cd OmniMixPlayer\gui_flutter
flutter build windows --release
REM 输出到 build\windows\x64\runner\Release\

REM 4. 合并到一个目录
xcopy gui_flutter\build\windows\x64\runner\Release\* release\OmniMixPlayer\ /E
```

---

## 六、服务安装命令

```powershell
# 安装（需管理员）
sc.exe create OmniMixPlayerBackend `
    binPath= "C:\Program Files\OmniMixPlayer\OmniMixPlayer.Backend.exe" `
    start= demand

# 启动
sc.exe start OmniMixPlayerBackend

# 停止
sc.exe stop OmniMixPlayerBackend

# 卸载
sc.exe delete OmniMixPlayerBackend
```

Flutter GUI 中切换 Mode 为 "service" 时会自动调用这些命令（`PlatformService.installService()`）。

---

## 七、当前待办

- [ ] 编写 OmniMixPlayer 专用构建脚本
- [ ] Flutter GUI 的 `_findBackendExe()` 在部署目录下路径需验证
- [ ] 服务模式需验证 `UseWindowsService()` 在无 GUI 环境下正常工作
- [ ] 模块 DLL 的依赖解析（`DependencyLoader`）需测试
