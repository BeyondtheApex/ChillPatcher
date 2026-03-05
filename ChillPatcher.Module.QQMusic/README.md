# ChillPatcher QQ 音乐模块

为 ChillPatcher 开发的 QQ 音乐集成模块，让游戏在 Wallpaper Engine 中运行时可以播放 QQ 音乐的歌曲。

## 功能特性

- **二维码登录** - 使用 QQ 音乐 APP 扫码登录
- **收藏同步** - 喜欢的歌曲自动同步到 QQ 音乐
- **歌单导入** - 支持导入自定义歌单
- **每日推荐** - 自动加载每日推荐歌曲
- **边下边播** - PCM 流式播放，无需等待完整下载
- **多音质支持** - 标准(128k)、HQ(320k)、无损(FLAC)、Hi-Res
- **封面显示** - 自动加载专辑封面

## 编译要求

### Go 环境
- Go 1.21 或更高版本
- CGO 支持（需要 GCC 编译器）
  - Windows 推荐：[TDM-GCC](https://jmeubank.github.io/tdm-gcc/) 或 [MinGW-w64](https://www.mingw-w64.org/)

### .NET 环境
- .NET SDK（支持 .NET Framework 4.7.2）
- Visual Studio 2019+ 或 `dotnet` CLI

## 编译方法

### 方法一：一键编译

在项目根目录运行：

```batch
build_qqmusic.bat
```

### 方法二：分步编译

1. **编译 Go 网桥**

```batch
cd qqmusic_bridge
set CGO_ENABLED=1
set GOOS=windows
set GOARCH=amd64
go mod tidy
go build -buildmode=c-shared -o ChillQQMusic.dll -ldflags "-s -w" .
```

2. **编译 C# 模块**

```batch
cd ChillPatcher.Module.QQMusic
dotnet restore
dotnet build -c Release
```

## 安装方法

1. 将编译好的文件复制到 BepInEx 插件目录：

```
BepInEx/plugins/ChillPatcher/
├── Modules/
│   └── ChillPatcher.Module.QQMusic.dll
└── native/
    └── x64/
        └── ChillQQMusic.dll
```

2. 启动游戏，模块会自动加载

## 配置说明

配置文件位于：`BepInEx/config/ChillPatcher.cfg`

首次运行后会自动生成配置项：

```ini
[Module:com.chillpatcher.qqmusic]

## QQ音乐数据目录（留空使用默认路径）
# 默认路径: BepInEx/plugins/ChillPatcher/data/com.chillpatcher.qqmusic/
DataDirectory =

## 音质等级
# 0 = 标准 (128kbps MP3)
# 1 = HQ (320kbps MP3) - 默认，需要绿钻
# 2 = SQ (FLAC 无损) - 需要绿钻
# 3 = Hi-Res (高解析度) - 需要豪华绿钻
AudioQuality = 1

## 自定义歌单 ID
# 多个歌单用逗号分隔，例如: 123456789, 987654321
# 歌单 ID 可从 QQ 音乐网页版链接获取
CustomPlaylistIds =

## PCM 流就绪超时（毫秒）
# 等待音频流准备就绪的最大时间
StreamReadyTimeoutMs = 20000
```

## 获取歌单 ID

1. 打开 [QQ 音乐网页版](https://y.qq.com/)
2. 进入想要导入的歌单
3. 从地址栏复制歌单 ID，例如：
   - 链接：`https://y.qq.com/n/ryqq/playlist/7890123456`
   - 歌单 ID：`7890123456`

## 使用流程

1. **首次登录**
   - 启动游戏后，模块会显示二维码
   - 打开 QQ 音乐 APP，扫描二维码
   - 在手机上确认登录

2. **播放音乐**
   - 登录成功后自动加载收藏的歌曲
   - 选择歌曲即可播放

3. **收藏歌曲**
   - 在游戏中收藏的歌曲会同步到 QQ 音乐

## 文件结构

```
ChillPatcher.Module.QQMusic/
├── ChillPatcher.Module.QQMusic.csproj  # 项目文件
├── ModuleInfo.cs                        # 模块元数据
├── QQMusicModule.cs                     # 主模块类
├── QQMusicBridge.cs                     # P/Invoke 桥接
├── QQMusicPcmStreamReader.cs            # PCM 流读取器
├── QQMusicSongRegistry.cs               # 歌曲注册管理
├── QQMusicCoverLoader.cs                # 封面加载器
├── QQMusicFavoriteManager.cs            # 收藏管理器
├── QRLoginManager.cs                    # 二维码登录
├── SilentPcmReader.cs                   # 静音播放器
└── README.md                            # 本文件

qqmusic_bridge/
├── go.mod                               # Go 模块定义
├── main.go                              # 导出函数入口
├── build.bat                            # Go 编译脚本
├── api/
│   ├── client.go                        # HTTP 客户端
│   ├── qrlogin.go                       # 二维码登录 API
│   ├── user.go                          # 用户 API
│   ├── song.go                          # 歌曲 API
│   └── playlist.go                      # 歌单 API
├── crypto/
│   └── sign.go                          # 签名加密
├── stream/
│   ├── cache.go                         # 缓存管理
│   └── pcm_stream.go                    # PCM 流处理
└── models/
    └── types.go                         # 数据结构
```

## 常见问题

### Q: 登录后没有歌曲显示？
A: 检查你的 QQ 音乐账号是否有收藏的歌曲。也可以在配置文件中添加歌单 ID 来导入歌单。

### Q: 歌曲无法播放？
A:
- 检查网络连接
- 部分歌曲可能需要 VIP 权限
- 尝试降低音质设置

### Q: 二维码无法显示？
A: 检查 Go DLL 是否正确放置在 `native/x64/` 目录下。

### Q: 编译 Go DLL 失败？
A:
- 确保安装了 GCC（TDM-GCC 或 MinGW-w64）
- 确保 `CGO_ENABLED=1`
- 运行 `go mod tidy` 下载依赖

## 技术说明

### 架构

```
┌─────────────────────────────────────┐
│           C# 模块层                  │
│    ChillPatcher.Module.QQMusic      │
└──────────────┬──────────────────────┘
               │ P/Invoke (cdecl)
               ▼
┌─────────────────────────────────────┐
│           Go 网桥层                  │
│         qqmusic_bridge              │
└──────────────┬──────────────────────┘
               │ HTTPS
               ▼
┌─────────────────────────────────────┐
│         QQ 音乐服务器                │
│  u.y.qq.com / dl.stream.qqmusic.qq.com
└─────────────────────────────────────┘
```

### 实现的 SDK 接口

| 接口 | 说明 |
|------|------|
| `IMusicModule` | 基础模块接口 |
| `IStreamingMusicSourceProvider` | 流媒体音乐源 |
| `ICoverProvider` | 封面提供 |
| `IFavoriteExcludeHandler` | 收藏/排除处理 |

## 许可证

本项目仅供学习和个人使用。请遵守 QQ 音乐的服务条款。
