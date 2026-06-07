# OmniMixPlayer SDK — 模块开发指南

OmniMixPlayer.SDK 是音乐源模块与平台后端的唯一契约层。所有模块（网易云、QQ 音乐、Bilibili、Spotify、LocalFolder 等）都通过此 SDK 与 `OmniMixPlayer.Backend` 通信。

> **⚠️ 旧版 ChillPatcher.SDK 已废弃**。如果你之前在 `ChillPatcher.SDK` 上开发，请迁移到本 SDK。`ChillPatcher.SDK` 仅作为兼容性空壳保留，不再接受新功能。

---

## 系统架构

```
┌─────────────────────────────────────────────────────────┐
│                    游戏 / 应用 (消费端)                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  FH6 (C++)   │  │ ChillWithYou │  │ Flutter GUI  │  │
│  │ OmniPcmShared│  │  (C# BepInEx)│  │  (控制面)    │  │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │
└─────────┼──────────────────┼──────────────────┼─────────┘
          │ 共享内存 PCM     │ gRPC-Web / WS    │ HTTP/WS
          │ (OmniPcmShared)  │                  │
┌─────────▼──────────────────▼──────────────────▼─────────┐
│             OmniMixPlayer.Backend (ASP.NET Core)         │
│  ┌──────────────────────────────────────────────────┐   │
│  │              模块系统 (Module System)              │   │
│  │  ┌─────────┐ ┌──────────┐ ┌──────────┐          │   │
│  │  │ Netease │ │ QQMusic  │ │ Bilibili │ ...      │   │
│  │  │ Module  │ │ Module   │ │ Module   │          │   │
│  │  └────┬────┘ └────┬─────┘ └────┬─────┘          │   │
│  │       └───────────┴────────────┘                 │   │
│  │             OmniMixPlayer.SDK                     │   │
│  └──────────────────────────────────────────────────┘   │
│  音频解码 · 流媒体管理 · 歌单库 · 播放控制 · 共享内存   │
└─────────────────────────────────────────────────────────┘
```

### 关键概念

**OmniMixPlayer.Backend** — ASP.NET Core 后台服务，是模块的**运行宿主**。它负责：

- 加载和管理所有音乐源模块
- 维护统一的音乐库（Track/Album/Tag/Playlist）
- 音频 URL 解析、流式下载、PCM 解码
- 将解码后的 float32 PCM 写入 Windows 命名共享内存
- 通过 gRPC-Web 和 WebSocket 向游戏客户端提供播放控制

**实例 (Instance)** — 每个连接到后端的游戏/客户端都会创建一个**实例**（用 `instance_id` 标识）。实例保存了该客户端的独立状态：

| 实例状态                      | 说明                           |
| ----------------------------- | ------------------------------ |
| 当前播放队列 + 历史           | 每实例独立                     |
| 音量 / 均衡器 / 延迟          | 每实例独立                     |
| 已选歌单源 (Playlist Sources) | 从模块提供的歌单中选择哪些导入 |
| 播放进度 / Seek 状态          | 与共享内存中的流绑定           |
| 在线/离线状态                 | 通过心跳维持                   |

> 📌 **模块开发者无需直接操作实例**。模块只负责声明音乐库数据（`ILibraryRegistry`）和提供可播放源（`IPlayableSourceResolver`）。实例管理由后端和游戏客户端 SDK（OmniPcmShared）处理。

**共享内存** — 后端将解码后的音频帧写入 Windows Named Shared Memory (`Global\OmniMixPlayer_PCM`)，游戏客户端通过 [OmniPcmShared](../../NativePlugins/OmniPcmShared/README.md) 零拷贝读取。模块不需要关心这个传输层。

---

## 快速开始

### 1. 创建模块项目

```xml
<!-- YourModule.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <AssemblyName>YourModule</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\OmniMixPlayer.SDK\OmniMixPlayer.SDK.csproj" />
  </ItemGroup>
</Project>
```

### 2. 实现模块入口

```csharp
using OmniMixPlayer.SDK.Attributes;
using OmniMixPlayer.SDK.Interfaces;
using OmniMixPlayer.SDK.Protos.Models;

[MusicModule("com.example.helloworld", "Hello World",
    Version = "1.0.0",
    Author = "Your Name",
    Description = "一个示例模块")]
[ModuleDependency("com.omnimixplayer.backend", MinVersion = "1.0.0")]
public class HelloWorldModule : IMusicModule, IMusicSourceProvider
{
    public string ModuleId => "com.example.helloworld";
    public string DisplayName => "Hello World";
    public string Version => "1.0.0";
    public int Priority => 100;
    public ModuleCapabilities Capabilities => new()
    {
        CanFavorite = true,
        CanExclude = true,
        ProvidesCover = false,
        ProvidesAlbum = false,
        ProvidesPlaylist = true
    };

    private IModuleContext _ctx;
    public SourceType SourceType => SourceType.Remote;

    public async Task InitializeAsync(IModuleContext context)
    {
        _ctx = context;

        // 1. 声明歌单
        var playlist = new Playlist
        {
            Id = "hello_playlist_1",
            DisplayName = "我的歌单",
            ModuleId = ModuleId,
            CoverUrl = ""
        };
        context.Library.UpsertPlaylist(playlist);

        // 2. 声明歌曲
        var track = new Track
        {
            Uuid = Track.GenerateUuid("hello_track_1"),
            Title = "示例歌曲",
            Artist = "示例歌手",
            DurationSeconds = 180f,
            ModuleId = ModuleId
        };
        context.Library.UpsertTrack(track);

        // 3. 歌曲加入歌单
        context.Library.ReplacePlaylistEntries("hello_playlist_1", new[]
        {
            new PlaylistEntrySpec { TrackUuid = track.Uuid }
        });

        context.Logger.LogInformation("HelloWorld 模块初始化完成");
    }

    public Task<List<Track>> GetMusicListAsync()
        => Task.FromResult(new List<Track>());

    public Task RefreshAsync()
    {
        // 重新扫描/刷新数据时调用
        // 通常先 UnregisterModule 再重新 Upsert
        return Task.CompletedTask;
    }

    public void OnEnable() { }
    public void OnDisable() { }
    public void OnUnload() { }
}
```

### 3. 部署模块

模块 DLL 放在 `modules/<ModuleId>/` 目录下：

```
OmniMixPlayer/
  modules/
    com.example.helloworld/
      YourModule.dll
      YourModule.deps.json
      native/               ← 原生依赖（可选）
        x64/
          your_decoder.dll
```

---

## 核心接口详解

### `IMusicModule` — 模块入口

所有模块必须实现。`[MusicModule]` attribute 提供元数据：

| 成员                                        | 类型                 | 说明                                  |
| ------------------------------------------- | -------------------- | ------------------------------------- |
| `ModuleId`                                  | `string`             | 唯一标识，推荐 `com.author.name` 格式 |
| `DisplayName`                               | `string`             | UI 中显示的名称                       |
| `Version`                                   | `string`             | 语义化版本                            |
| `Priority`                                  | `int`                | 加载优先级，越小越先加载（默认 100）  |
| `Capabilities`                              | `ModuleCapabilities` | 声明模块能力                          |
| `InitializeAsync(IModuleContext)`           | `Task`               | 初始化：注册 Track/Album/Tag/Playlist |
| `OnEnable()` / `OnDisable()` / `OnUnload()` | `void`               | 生命周期回调                          |

### `ModuleCapabilities` — 能力声明

| 属性                 | 默认值  | 说明                       |
| -------------------- | ------- | -------------------------- |
| `CanDelete`          | `false` | 是否支持删除歌曲           |
| `CanFavorite`        | `true`  | 是否支持收藏/取消收藏      |
| `CanExclude`         | `true`  | 是否支持排除歌曲           |
| `SupportsLiveUpdate` | `false` | 是否支持文件监控等实时更新 |
| `ProvidesCover`      | `true`  | 是否自行提供封面图片       |
| `ProvidesAlbum`      | `true`  | 是否有专辑概念             |
| `ProvidesPlaylist`   | `false` | 是否提供歌单               |

### `IModuleContext` — 模块上下文

`InitializeAsync` 中获得的上下文对象，提供所有平台服务：

| 服务                      | 类型                    | 说明                                              |
| ------------------------- | ----------------------- | ------------------------------------------------- |
| `Library`                 | `ILibraryRegistry`      | **核心 API**：声明式管理 Track/Album/Tag/Playlist |
| `ConfigManager`           | `IModuleConfigManager`  | 模块配置读写（自动持久化）                        |
| `EventBus`                | `IEventBus`             | 模块间事件通信                                    |
| `Logger`                  | `ILogger`               | 结构化日志                                        |
| `DefaultCover`            | `IDefaultCoverProvider` | 默认封面图片                                      |
| `StreamingService`        | `IStreamingService`     | PCM 流式解码服务                                  |
| `DependencyLoader`        | `IDependencyLoader`     | 原生 DLL 加载器                                   |
| `GetModuleDataPath(id)`   | `string`                | 获取模块数据目录                                  |
| `GetModuleNativePath(id)` | `string`                | 获取模块原生库目录                                |

---

## ILibraryRegistry — 音乐库声明 API

所有对音乐库的修改都通过 `ILibraryRegistry` 的声明式 API 完成。**模块不需要管理存储、去重、过滤或持久化**——这些都交给后端处理。

### Track（歌曲）

```csharp
// 添加/更新歌曲
var track = new Track
{
    Uuid = Track.GenerateUuid("unique_key"),  // 确定性 UUID
    Title = "歌曲标题",
    Artist = "艺术家",
    AlbumId = "album_1",        // 可选
    DurationSeconds = 240f,
    ModuleId = ModuleId,
    SourceUrl = "https://...",  // 可选：音频源 URL
    IsPlayable = true
};

ctx.Library.UpsertTrack(track);                    // 添加/更新单曲
ctx.Library.UpsertTracks(new[] { track1, track2 }); // 批量添加
var found = ctx.Library.GetTrack("uuid");           // 查询单曲
var list = ctx.Library.QueryTracks(new TrackQuery()); // 查询列表
ctx.Library.DeleteTrack("uuid");                    // 删除
```

### Tag（标签）

Tag 是歌曲的多对多分类标签，一个歌曲可以属于多个 Tag：

```csharp
var tag = new Tag
{
    Id = "tag_chill",
    DisplayName = "纯音乐",
    ModuleId = ModuleId
};
ctx.Library.UpsertTag(tag);

// 管理歌曲标签
ctx.Library.SetTrackTags("track_uuid", new[] { "tag_chill", "tag_study" });
ctx.Library.AddTrackTag("track_uuid", "tag_focus");
ctx.Library.RemoveTrackTag("track_uuid", "tag_chill");
var tags = ctx.Library.GetTrackTags("track_uuid");
```

### Album（专辑）

```csharp
var album = new Album
{
    Id = "album_1",
    DisplayName = "专辑名称",
    Artist = "艺术家",
    CoverUrl = "https://...",  // 可选：封面图 URL
    ModuleId = ModuleId
};
ctx.Library.UpsertAlbum(album);
```

### Playlist（歌单）

Playlist 是有序的歌曲列表：

```csharp
var playlist = new Playlist
{
    Id = "playlist_1",
    DisplayName = "我的歌单",
    ModuleId = ModuleId,
    CoverUrl = ""
};
ctx.Library.UpsertPlaylist(playlist);

// 替换整个歌单内容
ctx.Library.ReplacePlaylistEntries("playlist_1", new[]
{
    new PlaylistEntrySpec { TrackUuid = "uuid_1" },
    new PlaylistEntrySpec { TrackUuid = "uuid_2" },
});

// 增删改单条
ctx.Library.InsertPlaylistEntry("playlist_1",
    new PlaylistEntrySpec { TrackUuid = "uuid_3" }, index: 1);
ctx.Library.RemovePlaylistEntry("entry_id");
ctx.Library.MovePlaylistEntry("entry_id", newIndex: 0);

// 查询歌单（含条目）
var full = ctx.Library.GetPlaylistWithEntries("playlist_1");
```

### 模块注销

卸载/刷新模块时调用，会清理该模块声明的所有数据：

```csharp
var stats = ctx.Library.UnregisterModule(ModuleId);
// stats.TracksRemoved, stats.AlbumsRemoved, ...
```

---

## 可选接口

### `IMusicSourceProvider` — 音乐源提供

```csharp
public interface IMusicSourceProvider
{
    Task<List<Track>> GetMusicListAsync();  // 返回模块所有歌曲
    Task RefreshAsync();                     // 重新扫描/刷新
    SourceType SourceType { get; }           // Local / Remote
}
```

### `ICoverProvider` — 封面提供

```csharp
public interface ICoverProvider
{
    Task<(byte[] data, string mimeType)> GetMusicCoverAsync(string uuid);
    Task<(byte[] data, string mimeType)> GetAlbumCoverAsync(string albumId);
    void ClearCache();
    void RemoveMusicCoverCache(string uuid);
    void RemoveAlbumCoverCache(string albumId);
}
```

### `IFavoriteExcludeHandler` — 收藏/排除

```csharp
public interface IFavoriteExcludeHandler
{
    bool IsFavorite(string uuid);
    void SetFavorite(string uuid, bool isFavorite);
    bool IsExcluded(string uuid);
    void SetExcluded(string uuid, bool isExcluded);
    IReadOnlyList<string> GetFavorites();
    IReadOnlyList<string> GetExcluded();
}
```

### `IDeleteHandler` — 删除

```csharp
public interface IDeleteHandler
{
    bool CanDelete { get; }
    bool Delete(string uuid);
}
```

### `ILyricProvider` — 歌词提供

```csharp
public interface ILyricProvider
{
    string GetLyric(string uuid);  // 返回 LRC 格式歌词
}
```

### `IModuleUIProvider` — 自定义 UI

为模块提供自定义设置面板和快捷入口。详见各模块参考实现。

### `IStreamingMusicSourceProvider` — 流媒体音源（核心接口）

**所有网络音源模块都应实现此接口。** 它继承 `IMusicSourceProvider` + `IPlayableSourceResolver`。

> 📌 此接口的职责是**提供可播放 URL**。后端拿到 URL 后负责实际的下载、解析、PCM 解码和共享内存传输。模块不需要自己解码音频。

```csharp
public interface IStreamingMusicSourceProvider : IMusicSourceProvider, IPlayableSourceResolver
{
    bool IsReady { get; }                          // 模块是否就绪（已登录等）
    event Action<bool> OnReadyStateChanged;         // 就绪状态变化事件
}
```

### `IPlayableSourceResolver` — 可播放源解析

将歌曲 UUID 解析为实际的可播放源（本地路径或远程 URL）：

```csharp
public interface IPlayableSourceResolver
{
    // 解析歌曲为可播放源（返回 PlayableSource）
    Task<PlayableSource> ResolveAsync(
        string uuid,
        AudioQuality quality = AudioQuality.ExHigh,
        CancellationToken cancellationToken = default);

    // 刷新已过期的 URL（流媒体 URL 通常有时效）
    Task<PlayableSource> RefreshUrlAsync(
        string uuid,
        AudioQuality quality = AudioQuality.ExHigh,
        CancellationToken cancellationToken = default);
}
```

`PlayableSource` 可以是本地文件（`SourceType.Local`）或远程 URL（`SourceType.Remote`），还可附带 HTTP 请求头（Referer 等）用于防盗链。后端拿到 `PlayableSource` 后负责实际的下载和解码。

### `IModuleAudioDecoderProvider` — 模块自带解码器

少数模块需要自己完成音频解码（如 Spotify 通过 librespot 输出 PCM）：

```csharp
public interface IModuleAudioDecoderProvider
{
    bool CanDecode(string uuid);
    Task<IPcmStreamReader> CreateDecoderAsync(
        string uuid,
        AudioQuality quality = AudioQuality.ExHigh,
        CancellationToken cancellationToken = default);
}
```

> 📌 大多数模块不需要实现此接口。平台内置的 `IStreamingService` 已覆盖 MP3/FLAC/AAC 等常见格式的解码。

---

## IStreamingService — 流式 PCM 解码

平台提供统一的音频解码服务，模块**不需要自行编译解码库**：

```csharp
// 获取服务
var streaming = ctx.StreamingService;

// 同步创建流（边下边播）
var reader = streaming.CreateStream(
    url: "https://example.com/song.flac",
    format: "flac",
    durationSeconds: 240f,
    cacheKey: "mymodule_song_123",
    headers: new Dictionary<string, string> { ["Referer"] = "https://..." }
);

// 异步等待就绪
var reader2 = await streaming.CreateStreamAndWaitAsync(
    url, "flac", 240f, "mymodule_song_456",
    timeoutMs: 20000,
    cancellationToken: ct
);
```

创建的 `IPcmStreamReader` 接口：

```csharp
public interface IPcmStreamReader : IDisposable
{
    PcmStreamInfo Info { get; }          // SampleRate, Channels, TotalFrames
    ulong CurrentFrame { get; }          // 当前帧位置
    bool IsReady { get; }                // 是否有足够缓冲
    bool IsEndOfStream { get; }          // 是否到达末尾
    bool CanSeek { get; }                // 是否支持 Seek

    long ReadFrames(float[] buffer, int framesToRead);
    bool Seek(ulong frameIndex);
}
```

---

## IEventBus — 模块间事件

```csharp
// 订阅
var sub = ctx.EventBus.Subscribe<TrackPlayedEvent>(e =>
{
    ctx.Logger.LogInformation("播放: {Title}", e.TrackTitle);
});

// 发布
ctx.EventBus.Publish(new TrackPlayedEvent { TrackUuid = "..." });

// 取消订阅
sub.Dispose();
```

---

## IModuleConfigManager — 配置管理

配置自动持久化到 JSON 文件，无需手动序列化：

```csharp
var cfg = ctx.ConfigManager;

// 读取（带默认值）
int quality = cfg.GetInt("AudioQuality", defaultValue: 1);
string token = cfg.GetString("AuthToken", "");
bool enabled = cfg.GetBool("FeatureEnabled", true);

// 泛型读写
var obj = cfg.GetValue("ComplexSetting", new MySettings());
cfg.SetValue("ComplexSetting", obj);

// 保存到磁盘
cfg.Save();
```

配置文件位置：`data/<ModuleId>/config.json`

---

## IDependencyLoader — 原生库加载

模块的原生 DLL 放在 `<ModuleDir>/native/<arch>/` 下：

```csharp
// 从系统路径加载
ctx.DependencyLoader.LoadNativeLibrary("mylib.dll", ModuleId);

// 从模块 native 目录加载
ctx.DependencyLoader.LoadNativeLibraryFromModulePath("x64/mylib.dll", ModuleId);
```

---

## 数据模型 (Protobuf)

所有模型类型由 `Protos/omni_mix_player/models/*.proto` 生成：

| Proto            | C# 类型                          | 说明                                   |
| ---------------- | -------------------------------- | -------------------------------------- |
| `track.proto`    | `Track`                          | 歌曲：uuid、标题、艺术家、时长、源 URL |
| `album.proto`    | `Album`                          | 专辑：id、名称、艺术家、封面           |
| `tag.proto`      | `Tag`                            | 标签：id、名称（多对多关联歌曲）       |
| `playlist.proto` | `Playlist` / `PlaylistEntrySpec` | 歌单及其有序条目                       |
| `query.proto`    | `TrackQuery` / `AlbumQuery` 等   | 查询条件                               |

---

## 现有模块参考

| 模块            | 路径                                 | 特点                              |
| --------------- | ------------------------------------ | --------------------------------- |
| **LocalFolder** | `OmniMixPlayer/modules/LocalFolder/` | SDK 最简参考实现，本地文件扫描    |
| **Netease**     | `OmniMixPlayer/modules/Netease/`     | Go 桥接、二维码登录、歌词         |
| **QQMusic**     | `OmniMixPlayer/modules/QQMusic/`     | Go 桥接、多音质、自定义歌单       |
| **Bilibili**    | `OmniMixPlayer/modules/Bilibili/`    | 纯 C#、收藏夹同步、智能封面       |
| **Spotify**     | `OmniMixPlayer/modules/Spotify/`     | Rust 桥接 (librespot)、OAuth PKCE |

---

## WebSocket 事件

后端通过 WebSocket 推送播放状态变更事件，使用 Protobuf 二进制编码（`WsEvent`）。

模块的 UI 推送消息保持 JSON 文本帧（因为 UI 树是动态的，不属于稳定的播放/库事件协议）。

---

## 迁移指南：从 ChillPatcher.SDK 迁移

如果你之前使用旧版 `ChillPatcher.SDK`：

| 旧 API (`ChillPatcher.SDK`)                        | 新 API (`OmniMixPlayer.SDK`)                        |
| -------------------------------------------------- | --------------------------------------------------- |
| `ChillPatcher.SDK.Attributes.MusicModuleAttribute` | `OmniMixPlayer.SDK.Attributes.MusicModuleAttribute` |
| `ChillPatcher.SDK.Interfaces.IMusicModule`         | `OmniMixPlayer.SDK.Interfaces.IMusicModule`         |
| `ChillPatcher.SDK.Interfaces.IModuleContext`       | `OmniMixPlayer.SDK.Interfaces.IModuleContext`       |
| `context.MusicRegistry`                            | `context.Library` (声明式 upsert)                   |
| `context.TagRegistry`                              | `context.Library.UpsertTag()`                       |
| `context.AlbumRegistry`                            | `context.Library.UpsertAlbum()`                     |
| `context.AudioLoader`                              | `context.StreamingService`                          |
| `IMusicSourceProvider` (返回 `MusicInfo`)          | `IMusicSourceProvider` (返回 `Track`)               |
| `MusicInfo` 类型                                   | `Track` (Protobuf 生成)                             |

核心变更：

1. **命名空间**：`ChillPatcher.SDK` → `OmniMixPlayer.SDK`
2. **注册 API**：旧的 `MusicRegistry`/`TagRegistry`/`AlbumRegistry` 合并为统一的 `ILibraryRegistry`
3. **数据模型**：自定义 `MusicInfo`/`AlbumInfo` → Protobuf 生成的 `Track`/`Album`/`Tag`/`Playlist`
4. **声明式**：模块只需 upsert 数据，后端负责持久化和查询
