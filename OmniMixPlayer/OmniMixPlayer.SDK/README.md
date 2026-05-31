# ChillPatcher SDK

ChillPatcher SDK 为《Chill With You》游戏提供音乐模块开发接口，允许开发者创建自定义音乐源模块。

## 快速开始

### 1. 引用 SDK

在你的模块项目中引用 `ChillPatcher.SDK.dll`：

```xml
<ItemGroup>
  <Reference Include="ChillPatcher.SDK">
    <HintPath>..\path\to\ChillPatcher.SDK.dll</HintPath>
  </Reference>
</ItemGroup>
```

### 2. 创建模块类

```csharp
using ChillPatcher.SDK.Attributes;
using ChillPatcher.SDK.Interfaces;

[MusicModule("com.yourname.modulename", "模块显示名称",
    Version = "1.0.0",
    Author = "Your Name",
    Description = "模块描述")]
public class YourModule : IMusicModule, IMusicSourceProvider
{
    public string ModuleId => "com.yourname.modulename";
    public string DisplayName => "模块显示名称";
    public string Version => "1.0.0";
    public int Priority => 100;

    public ModuleCapabilities Capabilities => new ModuleCapabilities
    {
        CanDelete = false,
        CanFavorite = true,
        CanExclude = true,
        ProvidesCover = true,
        ProvidesAlbum = true
    };

    public async Task InitializeAsync(IModuleContext context)
    {
        // 初始化模块，注册歌曲、专辑和标签
    }

    public void OnEnable() { }
    public void OnDisable() { }
    public void OnUnload() { }
}
```

## 核心接口

### IMusicModule

所有音乐模块必须实现的基础接口。

| 成员                       | 说明                                              |
| -------------------------- | ------------------------------------------------- |
| `ModuleId`                 | 模块唯一标识符，推荐格式：`com.author.modulename` |
| `DisplayName`              | 模块显示名称                                      |
| `Version`                  | 模块版本                                          |
| `Priority`                 | 加载优先级（越小越先加载）                        |
| `Capabilities`             | 模块能力声明                                      |
| `InitializeAsync(context)` | 初始化模块                                        |
| `OnEnable()`               | 启用模块时调用                                    |
| `OnDisable()`              | 禁用模块时调用                                    |
| `OnUnload()`               | 卸载模块时调用                                    |

### ModuleCapabilities

模块能力声明，告知主程序模块支持的功能。

| 属性                 | 默认值 | 说明                           |
| -------------------- | ------ | ------------------------------ |
| `CanDelete`          | false  | 是否支持删除歌曲               |
| `CanFavorite`        | true   | 是否支持收藏                   |
| `CanExclude`         | true   | 是否支持排除                   |
| `SupportsLiveUpdate` | false  | 是否支持实时更新（文件监控等） |
| `ProvidesCover`      | true   | 是否提供自己的封面             |
| `ProvidesAlbum`      | true   | 是否提供自己的专辑             |

### IModuleContext

模块上下文，由主程序提供，包含所有可用服务。

| 成员                            | 说明                               |
| ------------------------------- | ---------------------------------- |
| `TagRegistry`                   | Tag 注册表，用于注册自定义播放列表 |
| `AlbumRegistry`                 | 专辑注册表                         |
| `MusicRegistry`                 | 歌曲注册表                         |
| `ConfigManager`                 | 配置管理器                         |
| `EventBus`                      | 事件总线                           |
| `Logger`                        | 日志记录器                         |
| `DefaultCover`                  | 默认封面提供器                     |
| `StreamingService`              | 流式音频服务（创建 PCM 流读取器）  |
| `PlayQueue`                     | 播放队列控制器                     |
| `DependencyLoader`              | 原生依赖加载器                     |
| `GetModuleDataPath(moduleId)`   | 获取模块数据目录路径               |
| `GetModuleNativePath(moduleId)` | 获取模块原生 DLL 目录路径          |

### IMusicSourceProvider

音乐源提供器接口，模块通过此接口提供音乐列表和加载功能。

```csharp
public interface IMusicSourceProvider
{
    Task<List<MusicInfo>> GetMusicListAsync();
    Task RefreshAsync();
    MusicSourceType SourceType { get; }
}
```

### ICoverProvider

封面提供器接口。

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

### IFavoriteExcludeHandler

收藏和排除状态管理接口。

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

### IDeleteHandler

删除处理器接口（可选实现）。

```csharp
public interface IDeleteHandler
{
    bool CanDelete { get; }
    bool Delete(string uuid);
}
```

### IStreamingMusicSourceProvider

流媒体音乐源提供器（继承 `IMusicSourceProvider`）。用于在线流媒体模块（QQ音乐、B站等），增加 URL 解析和就绪状态管理能力。

```csharp
public interface IStreamingMusicSourceProvider : IMusicSourceProvider, IPlayableSourceResolver
{
    bool IsReady { get; }
    event Action<bool> OnReadyStateChanged;
}
```

### ILyricProvider

歌词提供器接口（可选实现）。

```csharp
public interface ILyricProvider
{
    string GetLyric(string uuid);
}
```

### IPlayQueue

播放队列控制器接口，通过 `IModuleContext.PlayQueue` 访问。

```csharp
public interface IPlayQueue
{
    MusicInfo CurrentTrack { get; }
    bool IsPlaying { get; }
    float Position { get; }
    float Volume { get; set; }
    bool Shuffle { get; set; }
    RepeatMode RepeatMode { get; set; }
    IReadOnlyList<MusicInfo> Queue { get; }
    int QueueCount { get; }
    IReadOnlyList<MusicInfo> History { get; }

    void Play(string uuid = null);
    void Pause();
    void Resume();
    void Toggle();
    void Next();
    void Prev();
    void Seek(float position);
    void SetVolume(float volume);
    void AddToQueue(string uuid);
    void RemoveFromQueue(int index);
    void ClearQueue();

    bool IsFavorite(string uuid);
    void SetFavorite(string uuid, bool isFavorite);
    bool IsExcluded(string uuid);
    void SetExcluded(string uuid, bool isExcluded);

    event EventHandler<QueueChangedEventArgs> OnQueueChanged;
    event EventHandler<MusicInfo> OnTrackChanged;
    event EventHandler OnStateChanged;
    event EventHandler<float> OnPositionChanged;
}
```

## 数据模型

### MusicInfo

歌曲信息模型。

| 属性           | 类型            | 说明                                   |
| -------------- | --------------- | -------------------------------------- |
| `UUID`         | string          | 歌曲唯一标识符                         |
| `Title`        | string          | 歌曲标题                               |
| `Artist`       | string          | 艺术家                                 |
| `AlbumId`      | string          | 所属专辑 ID                            |
| `TagId`        | string          | 所属 Tag ID（已废弃，请使用 TagIds）   |
| `TagIds`       | List\<string\>  | 所属 Tag ID 列表（可同时属于多个 Tag） |
| `SourceType`   | MusicSourceType | 音乐源类型（File/Url/Clip/Stream）     |
| `CoverUrl`     | string          | 封面图片 URL                           |
| `SourcePath`   | string          | 源路径（文件路径或 URL）               |
| `Duration`     | float           | 时长（秒）                             |
| `ModuleId`     | string          | 所属模块 ID                            |
| `IsUnlocked`   | bool            | 是否已解锁（默认 true）                |
| `IsExcluded`   | bool            | 是否被排除                             |
| `IsFavorite`   | bool            | 是否收藏                               |
| `IsDeletable`  | bool?           | 是否可删除（null 表示使用模块默认）    |
| `PlayCount`    | int             | 播放次数                               |
| `CreatedAt`    | DateTime        | 创建时间                               |
| `LastPlayedAt` | DateTime?       | 最后播放时间                           |
| `ExtendedData` | object          | 扩展数据（模块自定义）                 |

静态方法：

- `MusicInfo.GenerateUUID()` - 生成随机 UUID
- `MusicInfo.GenerateUUID(string sourcePath)` - 根据路径生成确定性 UUID

### AlbumInfo

专辑信息模型。

| 属性              | 类型           | 说明                                     |
| ----------------- | -------------- | ---------------------------------------- |
| `AlbumId`         | string         | 专辑唯一标识符                           |
| `DisplayName`     | string         | 专辑显示名称                             |
| `Artist`          | string         | 专辑艺术家                               |
| `TagId`           | string         | 所属 Tag ID（已废弃，请使用 TagIds）     |
| `TagIds`          | List\<string\> | 所属 Tag ID 列表（可同时属于多个 Tag）   |
| `ModuleId`        | string         | 所属模块 ID                              |
| `DirectoryPath`   | string         | 专辑目录路径                             |
| `CoverPath`       | string         | 封面图片路径                             |
| `SongCount`       | int            | 专辑中的歌曲数量                         |
| `SortOrder`       | int            | 排序顺序                                 |
| `IsDefault`       | bool           | 是否是默认专辑                           |
| `IsGrowableAlbum` | bool           | 是否为增长专辑（滚动到底部触发加载更多） |
| `ExtendedData`    | object         | 扩展数据（模块自定义）                   |

### TagInfo

标签（播放列表）信息模型。

| 属性               | 类型                | 说明                                         |
| ------------------ | ------------------- | -------------------------------------------- |
| `TagId`            | string              | Tag 唯一标识符                               |
| `DisplayName`      | string              | 显示名称                                     |
| `ModuleId`         | string              | 所属模块 ID                                  |
| `BitValue`         | ulong               | Tag 的位值（用于游戏内部位运算）             |
| `SortOrder`        | int                 | 排序顺序                                     |
| `IconPath`         | string              | 图标路径                                     |
| `AlbumCount`       | int                 | Tag 下的专辑数量                             |
| `SongCount`        | int                 | Tag 下的歌曲数量                             |
| `IsVisible`        | bool                | 是否显示在 Tag 列表中                        |
| `IsGrowableList`   | bool                | 是否为增长列表（无限滚动，一次只能选中一个） |
| `GrowableAlbumId`  | string              | 增长专辑的 ID（为空则所有专辑视为增长）      |
| `LoadMoreCallback` | Func\<Task\<int\>\> | 增长列表的加载更多回调                       |
| `ExtendedData`     | object              | 扩展数据（模块自定义）                       |

## 配置管理

使用 `IModuleConfigManager` 注册模块配置项：

```csharp
public async Task InitializeAsync(IModuleContext context)
{
    var config = context.ConfigManager;

    // 绑定到模块默认 section: [Module:com.yourname.modulename]
    var rootFolder = config.BindDefault(
        "RootFolder",
        @"C:\Music",
        "音乐根目录"
    );

    // 绑定到自定义 section
    var customSetting = config.Bind(
        "CustomSection",
        "SettingKey",
        "default value",
        "设置描述"
    );
}
```

## 事件系统

使用 `IEventBus` 订阅和发布事件：

```csharp
// 订阅事件（返回 IDisposable，用于取消订阅）
var subscription = context.EventBus.Subscribe<PlayStartedEvent>(OnPlayStarted);

// 发布事件
context.EventBus.Publish(new PlayStartedEvent { Music = musicInfo });

// 取消订阅
subscription.Dispose();
```

### 可用事件类型

| 事件                          | 说明                                     |
| ----------------------------- | ---------------------------------------- |
| `PlayStartedEvent`            | 播放开始                                 |
| `PlayEndedEvent`              | 播放结束                                 |
| `PlayPausedEvent`             | 播放暂停/恢复                            |
| `PlayProgressEvent`           | 播放进度变化                             |
| `MusicResourcesReleasedEvent` | 歌曲资源释放（文件锁已释放，可安全写入） |

## 注册表接口

### ITagRegistry

```csharp
TagInfo RegisterTag(string tagId, string displayName, string moduleId);
void SetLoadMoreCallback(string tagId, Func<Task<int>> loadMoreCallback);
void MarkAsGrowableTag(string tagId, string growableAlbumId);
void UnregisterTag(string tagId);
TagInfo GetTag(string tagId);
IReadOnlyList<TagInfo> GetAllTags();
IReadOnlyList<TagInfo> GetTagsByModule(string moduleId);
bool IsTagRegistered(string tagId);
TagInfo GetTagByBitValue(ulong bitValue);
void UnregisterAllByModule(string moduleId);
TagInfo GetCurrentGrowableTag();
void SetCurrentGrowableTag(string tagId);
IReadOnlyList<TagInfo> GetGrowableTags();
```

### IAlbumRegistry

```csharp
void RegisterAlbum(AlbumInfo album, string moduleId);
void UnregisterAlbum(string albumId);
AlbumInfo GetAlbum(string albumId);
IReadOnlyList<AlbumInfo> GetAllAlbums();
IReadOnlyList<AlbumInfo> GetAlbumsByTag(string tagId);
IReadOnlyList<AlbumInfo> GetAlbumsByModule(string moduleId);
bool IsAlbumRegistered(string albumId);
void UnregisterAllByModule(string moduleId);
```

### IMusicRegistry

```csharp
void RegisterMusic(MusicInfo music, string moduleId);
void RegisterMusicBatch(IEnumerable<MusicInfo> musicList, string moduleId);
void UnregisterMusic(string uuid);
MusicInfo GetMusic(string uuid);
IReadOnlyList<MusicInfo> GetAllMusic();
IReadOnlyList<MusicInfo> GetMusicByAlbum(string albumId);
IReadOnlyList<MusicInfo> GetMusicByTag(string tagId);
IReadOnlyList<MusicInfo> GetMusicByModule(string moduleId);
bool IsMusicRegistered(string uuid);
void UpdateMusic(MusicInfo music);
void UnregisterAllByModule(string moduleId);
```

## 核心服务接口

### IStreamingService

流式音频服务，由主程序提供。用于创建 PCM 流式读取器，支持边下边播。

```csharp
public interface IStreamingService
{
    bool IsAvailable { get; }
    IPcmStreamReader CreateStream(string url, string format, float durationSeconds, string cacheKey, Dictionary<string, string> headers = null);
    IPcmStreamReader CreateStreamAndWait(string url, string format, float durationSeconds, string cacheKey, int timeoutMs = 20000, Dictionary<string, string> headers = null);
    bool WaitForReady(IPcmStreamReader reader, int timeoutMs);
    Task<IPcmStreamReader> CreateStreamAndWaitAsync(string url, string format, float durationSeconds, string cacheKey, int timeoutMs = 20000, Dictionary<string, string> headers = null, CancellationToken cancellationToken = default);
    Task<bool> WaitForReadyAsync(IPcmStreamReader reader, int timeoutMs, CancellationToken cancellationToken = default);
}
```

> **注意**：旧版 `IAudioLoader` 接口已移除，音频加载请使用 `IStreamingService` 创建 PCM 流。

### IDefaultCoverProvider

默认封面提供器。

```csharp
public interface IDefaultCoverProvider
{
    byte[] DefaultMusicCover { get; }
    byte[] DefaultAlbumCover { get; }
    byte[] LocalMusicCover { get; }
}
```

### IDependencyLoader

原生依赖加载器，用于加载模块的原生 DLL。

```csharp
public interface IDependencyLoader
{
    bool LoadNativeLibrary(string dllName, string moduleId);
    bool LoadNativeLibraryFromModulePath(string dllPath, string moduleId);
    bool IsLoaded(string dllName);
    string GetModuleNativePath(string moduleId);
}
```

## 模块部署

编译后的模块 DLL 放置在：

```
BepInEx/plugins/ChillPatcher/modules/<ModuleId>/
├── YourModule.dll
├── native/              ← 原生依赖（可选）
│   ├── x64/
│   └── x86/
└── ...
```

## 示例项目

参见 [LocalFolder 参考实现模块](../modules/LocalFolder/README.md) 获取完整的模块开发示例。
