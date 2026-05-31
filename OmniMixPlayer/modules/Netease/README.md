# ChillPatcher 网易云音乐模块

为 ChillPatcher 开发的网易云音乐集成模块，基于 [go-musicfox](https://github.com/go-musicfox/go-musicfox) 实现。

## 功能特性

- **二维码登录** - 使用网易云音乐 APP 扫码登录
- **收藏歌单 & 每日推荐** - 自动加载收藏的歌曲和每日推荐
- **私人 FM** - 支持私人 FM 模式
- **歌词显示** - 自动加载歌曲歌词
- **边下边播** - PCM 流式播放，无需等待完整下载
- **多音质支持** - 标准(128k)、HQ(320k)、无损(FLAC)、Hi-Res
- **封面显示** - 自动加载专辑封面
- **收藏同步** - 喜欢的歌曲双向同步到网易云音乐

## 编译要求

### Go 环境

- Go 1.21 或更高版本
- CGO 支持（需要 GCC 编译器）
  - Windows 推荐：[TDM-GCC](https://jmeubank.github.io/tdm-gcc/) 或 [MinGW-w64](https://www.mingw-w64.org/)

### .NET 环境

- .NET SDK（支持 .NET Framework 4.7.2）
- Visual Studio 2019+ 或 `dotnet` CLI

## 编译方法

### 1. 编译 Go 网桥

```batch
cd NativePlugins\netease_bridge
build.bat
```

### 2. 编译 C# 模块

```batch
cd OmniMixPlayer\modules\Netease
dotnet restore
dotnet build -c Release
```

## 架构说明

```
┌─────────────────────────────────────┐
│           C# 模块层                  │
│    ChillPatcher.Module.Netease      │
└──────────────┬──────────────────────┘
               │ P/Invoke (cdecl)
               ▼
┌─────────────────────────────────────┐
│           Go 网桥层                  │
│         netease_bridge              │
│   (基于 go-musicfox 核心逻辑)        │
└──────────────┬──────────────────────┘
               │ HTTPS
               ▼
┌─────────────────────────────────────┐
│         网易云音乐服务器              │
│    music.163.com / music.126.net    │
└─────────────────────────────────────┘
```

## 文件结构

```
ChillPatcher.Module.Netease/
├── ChillPatcher.Module.Netease.csproj
├── ModuleInfo.cs                        # 模块元数据
├── NeteaseModule.cs                     # 主模块类
├── NeteaseBridge.cs                     # P/Invoke 桥接
├── NeteasePcmStreamReader.cs            # PCM 流读取器
├── NeteaseSongRegistry.cs               # 歌曲注册管理
├── NeteaseCoverLoader.cs                # 封面加载器
├── NeteaseFavoriteManager.cs            # 收藏管理器
├── NeteaseLyricApi.cs                   # 歌词 API
├── NeteaseAccountApi.cs                 # 账号 API
├── NeteaseSessionManager.cs             # Session 管理
├── NeteaseFileCache.cs                  # 文件缓存
├── NeteaseMediaTagger.cs                # 媒体标签写入
├── QRLoginManager.cs                    # 二维码登录
├── PersonalFMManager.cs                 # 私人 FM 管理
├── SilentPcmReader.cs                   # 静音播放器
├── deploy.bat                           # 部署脚本
└── README.md                            # 本文件

NativePlugins/netease_bridge/            # Go 网桥源码
├── main.go                              # 导出函数入口
├── pcm_stream.go                        # PCM 流处理
├── pcm_streaming_decoder.go             # 流式解码
├── pcm_seekable_decoder.go              # 可 Seek 解码
├── pcm_flac_decoder.go                  # FLAC 解码
├── pcm_cache.go                         # 缓存管理
├── build.bat                            # 编译脚本
└── ChillNetease.h                       # C 头文件
```

## 实现的 SDK 接口

| 接口                            | 说明           |
| ------------------------------- | -------------- |
| `IMusicModule`                  | 基础模块接口   |
| `IStreamingMusicSourceProvider` | 流媒体音乐源   |
| `ICoverProvider`                | 封面提供       |
| `IFavoriteExcludeHandler`       | 收藏/排除处理  |
| `IDeleteHandler`                | 删除处理       |
| `ILyricProvider`                | 歌词提供       |
| `IModuleUIProvider`             | 自定义 UI 面板 |

## 许可证

本项目仅供学习和个人使用。请遵守网易云音乐的服务条款。
