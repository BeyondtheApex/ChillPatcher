# Bilibili 音乐模块

Bilibili 收藏夹音乐集成模块，纯 C# 实现，无需原生桥接。通过 Bilibili 官方 API 同步收藏夹中的视频音频。

> 作者：[@xiaogouqianqian](https://github.com/xiaogouqianqian)

## 功能特性

- **二维码登录** — 内置 Bilibili 扫码登录，Session 持久化
- **收藏夹同步** — 自动加载所有 B 站收藏夹，每个收藏夹生成独立的歌单标签
- **流式播放** — 边下边播，10 秒环形缓冲；完整下载后支持 Seek
- **智能封面** — 自动加载视频封面并居中裁切为正方形
- **大歌单支持** — 自动分页加载，支持 1000+ 首歌曲
- **访问控制** — 可配置翻页延迟防止 412 限流（默认 300ms）
- **收藏夹过滤** — 支持白名单/黑名单模式，按 fid 筛选收藏夹

## 架构

```
┌──────────────────────────────────────────┐
│              OmniMixPlayer.SDK            │
│  IMusicModule / IStreamingMusicSource...  │
└──────────────────┬───────────────────────┘
                   │
┌──────────────────▼───────────────────────┐
│        C# 模块层 (BilibiliModule)         │
│  命名空间: OmniMixPlayer.Module.Bilibili  │
│  实现: IStreamingMusicSourceProvider,     │
│        ICoverProvider, IModuleUIProvider  │
│  API: api.bilibili.com / passport.bilibili.com│
└──────────────────────────────────────────┘
```

## 编译

纯 C# 项目，无需 Go/Rust 编译：

```batch
cd OmniMixPlayer\modules\Bilibili
dotnet restore
dotnet build -c Release
```

## 文件结构

```
OmniMixPlayer/modules/Bilibili/
├── BilibiliModule.cs             # 主模块入口
├── BilibiliBridge.cs             # Bilibili API 客户端
├── BilibiliModels.cs             # API 数据模型
├── BilibiliSongRegistry.cs       # 歌曲注册（ILibraryRegistry）
├── QRLoginManager.cs             # 二维码登录
└── README.md
```

## 登录数据

Session 保存在：

```
C:\Users\<用户名>\AppData\LocalLow\nestopi\Chill With You\ChillPatcher\com.chillpatcher.bilibili\bilibili_session.json
```

## 配置

通过 `IModuleConfigManager` 管理（图形化设置面板）：

| 配置项                | 默认值 | 说明                           |
| --------------------- | ------ | ------------------------------ |
| `PageLoadDelay`       | 300    | 翻页延迟(ms)，防止 412 限流    |
| `ImportFilterEnabled` | false  | 启用收藏夹过滤                 |
| `ImportFilterMode`    | allow  | `allow`=白名单 / `deny`=黑名单 |
| `ImportFolderIds`     | (空)   | 逗号分隔的 fid 列表            |

> 注意：加载大型收藏夹可能需要较长时间，请耐心等待。

## 实现接口

| 接口                            | 说明                                      |
| ------------------------------- | ----------------------------------------- |
| `IMusicModule`                  | 基础模块入口                              |
| `IStreamingMusicSourceProvider` | 流媒体音源（提供 URL，后端负责下载+解码） |
| `ICoverProvider`                | 视频封面（正方形裁切）                    |
| `IModuleUIProvider`             | 二维码登录 UI 面板                        |

## 许可证

本项目仅供学习研究使用。请遵守 Bilibili 服务条款。
