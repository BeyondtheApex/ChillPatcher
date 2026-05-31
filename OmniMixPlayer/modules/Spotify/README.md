# ChillPatcher Spotify 模块

为 ChillPatcher 开发的 Spotify 集成模块，基于 [librespot](https://github.com/librespot-org/librespot) 实现原生音频解码与播放控制。

## 功能特性

- **OAuth 2.0 PKCE 登录** - 通过浏览器完成 Spotify 账号授权，无需密码
- **播放列表同步** - 自动加载收藏的歌曲和歌单
- **Spotify Connect 远程控制** - 通过 Web API 控制播放状态
- **原生音频解码** - 基于 librespot 的 Rust 原生解码器，支持高音质播放
- **封面显示** - 自动加载专辑封面
- **收藏同步** - 喜欢的歌曲双向同步到 Spotify

## 编译要求

### Rust 环境

- Rust 1.70 或更高版本（安装 [rustup](https://rustup.rs/)）
- Cargo（Rust 包管理器）

### .NET 环境

- .NET SDK（支持 .NET Framework 4.7.2）
- Visual Studio 2019+ 或 `dotnet` CLI

## 编译方法

### 1. 编译 Rust 原生桥接

```batch
cd NativePlugins\SpotifyLibrespotBridge
build.bat
```

### 2. 编译 C# 模块

```batch
cd OmniMixPlayer\modules\Spotify
dotnet restore
dotnet build -c Release
```

## 架构说明

```
┌─────────────────────────────────────┐
│           C# 模块层                  │
│    ChillPatcher.Module.Spotify      │
├─────────────────────────────────────┤
│         OAuth 2.0 PKCE              │
│     (浏览器授权流程)                 │
└──────────────┬──────────────────────┘
               │ P/Invoke (cdecl)
               ▼
┌─────────────────────────────────────┐
│        Rust 原生桥接层               │
│    SpotifyLibrespotBridge           │
│     (基于 librespot)                │
├─────────────────────────────────────┤
│      Spotify Web API (HTTPS)        │
└─────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│          Spotify 服务器              │
│    api.spotify.com / Spotify CDN    │
└─────────────────────────────────────┘
```

## 文件结构

```
ChillPatcher.Module.Spotify/
├── ChillPatcher.Module.Spotify.csproj
├── SpotifyModule.cs                     # 主模块类
├── SpotifyBridge.cs                     # Web API 客户端
├── SpotifyModels.cs                     # 数据模型
├── SpotifySongRegistry.cs               # 歌曲注册管理
├── OAuthManager.cs                      # OAuth 2.0 PKCE 认证
├── NativeLibrespotPcmReader.cs          # 原生解码器 PCM 读取
├── SilentPcmReader.cs                   # 静音播放器
├── Resources/                           # 资源文件
├── native/
│   └── x64/
│       └── SpotifyLibrespotBridge.dll   # Rust 原生 DLL
└── README.md                            # 本文件

NativePlugins/SpotifyLibrespotBridge/    # Rust 桥接源码
├── src/lib.rs                           # Rust 导出函数
├── Cargo.toml                           # Rust 依赖配置
├── Cargo.lock
└── build.bat                            # 编译脚本
```

## 实现的 SDK 接口

| 接口                            | 说明           |
| ------------------------------- | -------------- |
| `IMusicModule`                  | 基础模块接口   |
| `IStreamingMusicSourceProvider` | 流媒体音乐源   |
| `IModuleAudioDecoderProvider`   | 音频解码器提供 |
| `ICoverProvider`                | 封面提供       |
| `IFavoriteExcludeHandler`       | 收藏/排除处理  |
| `IModuleUIProvider`             | 自定义 UI 面板 |

## 许可证

本项目仅供学习和个人使用。请遵守 Spotify 的服务条款。
