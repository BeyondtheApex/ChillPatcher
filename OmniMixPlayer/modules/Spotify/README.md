# Spotify 模块

基于 [librespot](https://github.com/librespot-org/librespot) 的 Spotify 集成模块，通过 Rust 原生桥接实现音频解码和 Spotify Connect 设备模拟。

## 功能特性

- **OAuth 2.0 PKCE 登录** — 浏览器授权，无需密码
- **播放列表同步** — 自动加载收藏歌曲和歌单
- **Spotify Connect 远程控制** — 手机/桌面 Spotify 客户端控制播放
- **原生 PCM 解码** — librespot 直接解码为交错 float32 PCM
- **封面显示** — 自动加载专辑封面
- **收藏同步** — 喜欢/取消喜欢双向同步

## 使用方式

### 在 OmniMix 内点歌

1. 配置 Spotify Client ID 并完成 OAuth 登录（需要 Premium 账号）
2. 在 Spotify 模块中选择歌单/收藏曲目播放
3. 模块将播放转移到本地 librespot Connect 设备，PCM 通过 OmniMix IPC 管线进入游戏

### 手机/其他客户端控制

1. 在 Spotify 模块中点击"启动 Connect 接收"
2. 在手机/桌面 Spotify 的设备列表中选择 OmniMixPlayer-\*
3. 在 Spotify 客户端播放任意内容
4. librespot 输出的 PCM 通过共享内存 IPC 进入游戏

## 架构

```
┌──────────────────────────────────────────┐
│              OmniMixPlayer.SDK            │
│  IMusicModule / IStreamingMusicSource...  │
│  IModuleAudioDecoderProvider (PCM 直出)   │
└──────────────────┬───────────────────────┘
                   │
┌──────────────────▼───────────────────────┐
│         C# 模块层 (SpotifyModule)         │
│  命名空间: OmniMixPlayer.Module.Spotify   │
│  OAuth PKCE / Web API / librespot PCM    │
└──────────────────┬───────────────────────┘
                   │ P/Invoke (cdecl)
┌──────────────────▼───────────────────────┐
│       Rust 原生桥接 (librespot)           │
│  NativePlugins/SpotifyLibrespotBridge/   │
│  Spotify Connect 设备 + PCM 输出          │
└──────────────────┬───────────────────────┘
                   │ HTTPS / Spotify Protocol
┌──────────────────▼───────────────────────┐
│            Spotify 服务器                 │
│    api.spotify.com / Spotify CDN         │
└──────────────────────────────────────────┘
```

## 编译

### 环境要求

- **Rust** 1.70+ (通过 [rustup](https://rustup.rs/) 安装)
- **.NET Framework 4.7.2** SDK

### 编译步骤

```batch
# 1. 编译 Rust 原生桥接
cd NativePlugins\SpotifyLibrespotBridge
build.bat

# 2. 编译 C# 模块
cd OmniMixPlayer\modules\Spotify
dotnet restore
dotnet build -c Release
```

## 文件结构

```
OmniMixPlayer/modules/Spotify/
├── SpotifyModule.cs                      # 主模块入口
├── SpotifyBridge.cs                      # Spotify Web API 客户端
├── SpotifyModels.cs                      # 数据模型
├── SpotifySongRegistry.cs                # 歌曲注册（ILibraryRegistry）
├── OAuthManager.cs                       # OAuth 2.0 PKCE 认证流程
├── NativeLibrespotPcmReader.cs           # librespot PCM 读取器
├── NativeLibrespotPcmReaderLease.cs      # 播放管线租约管理
├── Resources/                            # UI 资源
└── native/x64/
    └── SpotifyLibrespotBridge.dll        # Rust 编译产物

NativePlugins/SpotifyLibrespotBridge/      # Rust 桥接源码
├── src/lib.rs                            # C 导出函数
├── Cargo.toml / Cargo.lock               # Rust 依赖
└── build.bat                             # 编译脚本
```

## 实现接口

| 接口                            | 说明                                |
| ------------------------------- | ----------------------------------- |
| `IMusicModule`                  | 基础模块入口                        |
| `IStreamingMusicSourceProvider` | 流媒体音源（提供 URL）              |
| `IModuleAudioDecoderProvider`   | 向 IPC 管线提供 librespot PCM 流    |
| `ICoverProvider`                | 专辑封面                            |
| `IFavoriteExcludeHandler`       | 收藏状态（持久化到 Spotify 服务端） |
| `IModuleUIProvider`             | OAuth 登录 + Connect 控制面板       |

## 许可证

本项目仅供学习研究使用。请遵守 Spotify 服务条款。
