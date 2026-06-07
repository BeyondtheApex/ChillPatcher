# QQ 音乐模块 (QQMusic)

QQ 音乐集成模块，通过 Go 原生桥接 DLL 调用 QQ 音乐官方接口，支持二维码登录和多音质流式播放。

## 功能特性

- **二维码登录** — 支持 QQ 扫码和微信扫码两种登录方式
- **收藏同步** — 喜欢的歌曲自动同步到 QQ 音乐"我喜欢的音乐"
- **歌单导入** — 支持通过歌单 ID 导入自定义歌单
- **每日推荐** — 自动加载每日推荐歌曲
- **流式播放** — 边下边播，后端负责下载+解码，支持 MP3 / FLAC 格式
- **多音质** — 标准(128k) / HQ(320k) / 无损(FLAC) / Hi-Res
- **封面显示** — 自动加载专辑封面
- **歌词显示** — 自动加载 LRC 歌词

## 架构

```
┌──────────────────────────────────────────┐
│              OmniMixPlayer.SDK            │
│  IMusicModule / IStreamingMusicSource...  │
└──────────────────┬───────────────────────┘
                   │
┌──────────────────▼───────────────────────┐
│          C# 模块层 (QQMusicModule)        │
│  命名空间: OmniMixPlayer.Module.QQMusic   │
│  实现: IStreamingMusicSourceProvider      │
│        (提供可播放URL,后端负责下载+解码),  │
│        ICoverProvider, ILyricProvider,    │
│        IFavoriteExcludeHandler,           │
│        IModuleUIProvider                  │
└──────────────────┬───────────────────────┘
                   │ P/Invoke (cdecl)
┌──────────────────▼───────────────────────┐
│          Go 网桥层 (qqmusic_bridge)       │
│  NativePlugins/qqmusic_bridge/           │
│  CGO → c-shared DLL                      │
└──────────────────┬───────────────────────┘
                   │ HTTPS
┌──────────────────▼───────────────────────┐
│            QQ 音乐服务器                  │
│    u.y.qq.com / dl.stream.qqmusic.qq.com │
└──────────────────────────────────────────┘
```

## 编译

### 环境要求

- **Go** 1.21+ (CGO + GCC，推荐 [TDM-GCC](https://jmeubank.github.io/tdm-gcc/))
- **.NET Framework 4.7.2** SDK

### 编译步骤

```batch
# 1. 编译 Go 网桥 DLL
cd NativePlugins\qqmusic_bridge
build.bat

# 2. 编译 C# 模块
cd OmniMixPlayer\modules\QQMusic
dotnet restore
dotnet build -c Release
```

## 文件结构

```
OmniMixPlayer/modules/QQMusic/
├── QQMusicModule.cs              # 主模块入口
├── ModuleInfo.cs                 # 模块 ID/名称/版本常量
├── QQMusicBridge.cs              # P/Invoke Go DLL 桥接
├── QQMusicSongRegistry.cs        # 歌曲注册（ILibraryRegistry）
├── QQMusicCoverLoader.cs         # 封面图片加载
├── QQMusicFavoriteManager.cs     # 收藏状态管理
├── QQMusicLyricApi.cs            # LRC 歌词获取
├── QRLoginManager.cs             # QQ/微信扫码登录
└── native/x64/
    └── qqmusic_bridge.dll        # Go 编译产物

NativePlugins/qqmusic_bridge/     # Go 网桥源码
├── main.go                       # C 导出函数入口
├── api/                          # API 客户端
│   ├── client.go                 # HTTP 客户端
│   ├── qrlogin.go                # 二维码登录 API
│   ├── user.go                   # 用户 API
│   ├── song.go                   # 歌曲 API
│   └── playlist.go               # 歌单 API
├── crypto/sign.go                # 签名算法
├── stream/                       # 流处理
│   ├── cache.go                  # 缓存管理
│   └── pcm_stream.go             # PCM 流
├── models/types.go               # 数据结构
└── build.bat                     # 编译脚本
```

## 登录数据

登录 Cookie 保存在 `<游戏目录>\BepInEx\plugins\ChillPatcher\Modules\com.chillpatcher.qqmusic\data\qqmusic_cookie.json`。如需重新登录，删除此文件后重启即可。

## 配置

模块通过 `IModuleConfigManager` 管理配置，首次运行后在图形化设置中可调整：

- **音质等级**：0=标准 / 1=HQ / 2=无损 / 3=Hi-Res
- **自定义歌单 ID**：逗号分隔的歌单 ID 列表（从 y.qq.com 链接获取）
- **PCM 流超时**：等待音频流就绪的最大时间（毫秒）

## 实现接口

| 接口                            | 说明                                      |
| ------------------------------- | ----------------------------------------- |
| `IMusicModule`                  | 基础模块入口                              |
| `IStreamingMusicSourceProvider` | 流媒体音源（提供 URL，后端负责下载+解码） |
| `ICoverProvider`                | 专辑封面                                  |
| `ILyricProvider`                | LRC 歌词                                  |
| `IFavoriteExcludeHandler`       | 收藏状态管理                              |
| `IModuleUIProvider`             | 自定义设置面板（二维码/歌单管理）         |

## 许可证

本项目仅供学习研究使用。请遵守 QQ 音乐服务条款。
