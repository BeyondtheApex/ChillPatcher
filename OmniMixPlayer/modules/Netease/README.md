# 网易云音乐模块 (Netease)

基于 [go-musicfox](https://github.com/go-musicfox/go-musicfox) 的网易云音乐集成模块，通过 Go 原生桥接 DLL 调用网易云 API。

## 功能特性

- **二维码登录** — 使用网易云音乐 APP 扫码登录，Session 持久化到 `go-musicfox/cookie`
- **收藏歌单** — 自动加载收藏歌曲和创建的歌单
- **每日推荐** — 自动加载每日推荐歌曲
- **私人 FM** — 支持私人 FM 模式，无限流式推荐
- **歌词显示** — 自动加载 LRC 歌词，支持实时滚动
- **流式播放** — 边下边播，后端负责下载+解码，无需等待完整下载
- **多音质** — 标准(128k) / HQ(320k) / 无损(FLAC) / Hi-Res
- **封面显示** — 自动加载专辑封面
- **收藏同步** — 喜欢/取消喜欢双向同步到网易云

## 架构

```
┌──────────────────────────────────────────┐
│              OmniMixPlayer.SDK            │
│  IMusicModule / IStreamingMusicSource...  │
└──────────────────┬───────────────────────┘
                   │
┌──────────────────▼───────────────────────┐
│          C# 模块层 (NeteaseModule)        │
│  命名空间: OmniMixPlayer.Module.Netease   │
│  实现: IStreamingMusicSourceProvider      │
│        (提供可播放URL,后端负责下载+解码),  │
│        ICoverProvider, ILyricProvider,    │
│        IFavoriteExcludeHandler,           │
│        IDeleteHandler, IModuleUIProvider  │
└──────────────────┬───────────────────────┘
                   │ P/Invoke (cdecl)
┌──────────────────▼───────────────────────┐
│          Go 网桥层 (netease_bridge)       │
│  NativePlugins/netease_bridge/           │
│  CGO → c-shared DLL                      │
│  基于 go-musicfox 核心 API 逻辑           │
└──────────────────┬───────────────────────┘
                   │ HTTPS
┌──────────────────▼───────────────────────┐
│           网易云音乐服务器                 │
│     music.163.com / interface.music.163.com│
└──────────────────────────────────────────┘
```

## 编译

### 环境要求

- **Go** 1.21+ (需 CGO + GCC，推荐 [TDM-GCC](https://jmeubank.github.io/tdm-gcc/))
- **.NET Framework 4.7.2** SDK

### 编译步骤

```batch
# 1. 编译 Go 网桥 DLL
cd NativePlugins\netease_bridge
build.bat

# 2. 编译 C# 模块
cd OmniMixPlayer\modules\Netease
dotnet restore
dotnet build -c Release
```

## 文件结构

```
OmniMixPlayer/modules/Netease/
├── NeteaseModule.cs              # 主模块入口
├── ModuleInfo.cs                 # 模块 ID/名称/版本常量
├── NeteaseBridge.cs              # P/Invoke Go DLL 桥接
├── NeteaseSongRegistry.cs        # 歌曲注册（ILibraryRegistry）
├── NeteaseCoverLoader.cs         # 封面图片加载
├── NeteaseFavoriteManager.cs     # 收藏状态管理
├── NeteaseLyricApi.cs            # LRC 歌词获取
├── NeteaseAccountApi.cs          # 账号信息 API
├── NeteaseSessionManager.cs      # Session/Cookie 管理
├── NeteaseFileCache.cs           # 流媒体文件缓存
├── NeteaseMediaTagger.cs         # 媒体标签写入
├── QRLoginManager.cs             # 二维码登录流程
├── PersonalFMManager.cs          # 私人 FM 管理
├── deploy.bat                    # 部署脚本
└── native/x64/
    └── netease_bridge.dll        # Go 编译产物

NativePlugins/netease_bridge/     # Go 网桥源码
├── main.go                       # C 导出函数入口
├── pcm_stream.go                 # PCM 流处理
├── pcm_streaming_decoder.go      # 流式解码
├── pcm_seekable_decoder.go       # 可 Seek 解码
├── pcm_flac_decoder.go           # FLAC 解码
├── pcm_cache.go                  # 缓存管理
├── build.bat                     # 编译脚本
└── ChillNetease.h                # C 头文件
```

## 登录数据

登录凭证保存在 `C:\Users\<用户名>\AppData\Local\go-musicfox\cookie`。如需重新登录，删除此文件后重启即可。

## 配置

模块通过 `IModuleConfigManager` 读写配置，无需手动编辑文件。首次运行后在图形化设置中可调整参数。

## 实现的 SDK 接口

| 接口                            | 说明                                      |
| ------------------------------- | ----------------------------------------- |
| `IMusicModule`                  | 基础模块入口                              |
| `IStreamingMusicSourceProvider` | 流媒体音源（提供 URL，后端负责下载+解码） |
| `ICoverProvider`                | 专辑/歌曲封面                             |
| `ILyricProvider`                | LRC 歌词                                  |
| `IFavoriteExcludeHandler`       | 收藏/排除状态管理                         |
| `IDeleteHandler`                | 歌曲删除                                  |
| `IModuleUIProvider`             | 自定义设置面板                            |

## 许可证

本项目仅供学习研究使用。请遵守网易云音乐服务条款。
