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

| 成员 | 说明 |
|------|------|
| `ModuleId` | 模块唯一标识符，推荐格式：`com.author.modulename` |
| `DisplayName` | 模块显示名称 |
| `Version` | 模块版本 |
| `Priority` | 加载优先级（越小越先加载） |
| `Capabilities` | 模块能力声明 |
| `InitializeAsync(context)` | 初始化模块 |
| `OnEnable()` | 启用模块时调用 |
| `OnDisable()` | 禁用模块时调用 |
| `OnUnload()` | 卸载模块时调用 |

### ModuleCapabilities

模块能力声明，告知主程序模块支持的功能。

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `CanDelete` | false | 是否支持删除歌曲 |
| `CanFavorite` | true | 是否支持收藏 |
| `CanExclude` | true | 是否支持排除 |
| `SupportsLiveUpdate` | false | 是否支持实时更新（文件监控等） |
| `ProvidesCover` | true | 是否提供自己的封面 |
| `ProvidesAlbum` | true | 是否提供自己的专辑 |

### IModuleContext

模块上下文，由主程序提供，包含所有可用服务。

| 成员 | 说明 |
|------|------|
| `TagRegistry` | Tag 注册表，用于注册自定义播放列表 |
| `AlbumRegistry` | 专辑注册表 |
| `MusicRegistry` | 歌曲注册表 |
| `ConfigManager` | 配置管理器 |
| `EventBus` | 事件总线 |
| `Logger` | 日志记录器 |
| `DefaultCover` | 默认封面提供器 |
| `AudioLoader` | 音频加载器 |
| `DependencyLoader` | 原生依赖加载器 |

### IMusicSourceProvider

音乐源提供器接口，模块通过此接口提供音乐列表和加载功能。

```csharp
public interface IMusicSourceProvider
{
    Task<List<MusicInfo>> GetMusicListAsync();
    Task<AudioClip> LoadAudioAsync(string uuid);
    Task<AudioClip> LoadAudioAsync(string uuid, CancellationToken cancellationToken);
    void UnloadAudio(string uuid);
    Task RefreshAsync();
    MusicSourceType SourceType { get; }
}
```

### ICoverProvider

封面提供器接口。

```csharp
public interface ICoverProvider
{
    Task<Sprite> GetMusicCoverAsync(string uuid);
    Task<Sprite> GetAlbumCoverAsync(string albumId);
    Task<(byte[] data, string mimeType)> GetMusicCoverBytesAsync(string uuid);
    void ClearCache();
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
    string GetDeleteConfirmMessage(string uuid);
}
```

## 数据模型

### MusicInfo

歌曲信息模型。

| 属性 | 类型 | 说明 |
|------|------|------|
| `UUID` | string | 歌曲唯一标识符 |
| `Title` | string | 歌曲标题 |
| `Artist` | string | 艺术家 |
| `AlbumId` | string | 所属专辑 ID |
| `TagId` | string | 所属 Tag ID |
| `SourceType` | MusicSourceType | 音乐源类型（File/Url/Clip/Stream） |
| `SourcePath` | string | 源路径（文件路径或 URL） |
| `Duration` | float | 时长（秒） |
| `ModuleId` | string | 所属模块 ID |
| `IsUnlocked` | bool | 是否已解锁（默认 true） |
| `IsExcluded` | bool | 是否被排除 |
| `IsFavorite` | bool | 是否收藏 |
| `PlayCount` | int | 播放次数 |
| `ExtendedData` | object | 扩展数据（模块自定义） |

静态方法：
- `MusicInfo.GenerateUUID()` - 生成随机 UUID
- `MusicInfo.GenerateUUID(string sourcePath)` - 根据路径生成确定性 UUID

### AlbumInfo

专辑信息模型。

| 属性 | 类型 | 说明 |
|------|------|------|
| `AlbumId` | string | 专辑唯一标识符 |
| `DisplayName` | string | 专辑显示名称 |
| `Artist` | string | 专辑艺术家 |
| `TagId` | string | 所属 Tag ID |
| `ModuleId` | string | 所属模块 ID |
| `DirectoryPath` | string | 专辑目录路径 |
| `CoverPath` | string | 封面图片路径 |
| `SongCount` | int | 专辑中的歌曲数量 |
| `SortOrder` | int | 排序顺序 |
| `IsDefault` | bool | 是否是默认专辑 |
| `ExtendedData` | object | 扩展数据（模块自定义） |

### TagInfo

标签（播放列表）信息模型。

| 属性 | 类型 | 说明 |
|------|------|------|
| `TagId` | string | Tag 唯一标识符 |
| `DisplayName` | string | 显示名称 |
| `ModuleId` | string | 所属模块 ID |
| `BitValue` | ulong | Tag 的位值（用于游戏内部位运算） |
| `SortOrder` | int | 排序顺序 |
| `IconPath` | string | 图标路径 |
| `AlbumCount` | int | Tag 下的专辑数量 |
| `SongCount` | int | Tag 下的歌曲数量 |
| `IsVisible` | bool | 是否显示在 Tag 列表中 |
| `ExtendedData` | object | 扩展数据（模块自定义） |

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

| 事件 | 说明 |
|------|------|
| `PlayStartedEvent` | 播放开始 |
| `PlayEndedEvent` | 播放结束 |
| `PlayPausedEvent` | 播放暂停/恢复 |
| `PlayProgressEvent` | 播放进度变化 |

## 注册表接口

### ITagRegistry

```csharp
TagInfo RegisterTag(string tagId, string displayName, string moduleId);
void UnregisterTag(string tagId);
TagInfo GetTag(string tagId);
IReadOnlyList<TagInfo> GetAllTags();
IReadOnlyList<TagInfo> GetTagsByModule(string moduleId);
```

### IAlbumRegistry

```csharp
void RegisterAlbum(AlbumInfo album, string moduleId);
void UnregisterAlbum(string albumId);
AlbumInfo GetAlbum(string albumId);
IReadOnlyList<AlbumInfo> GetAlbumsByTag(string tagId);
```

### IMusicRegistry

```csharp
void RegisterMusic(MusicInfo music, string moduleId);
void RegisterMusicBatch(IEnumerable<MusicInfo> musicList, string moduleId);
void UnregisterMusic(string uuid);
MusicInfo GetMusic(string uuid);
IReadOnlyList<MusicInfo> GetMusicByAlbum(string albumId);
IReadOnlyList<MusicInfo> GetMusicByTag(string tagId);
```

## 核心服务接口

### IAudioLoader

音频加载器，由主程序提供。

```csharp
public interface IAudioLoader
{
    string[] SupportedFormats { get; }
    bool IsSupportedFormat(string filePath);
    Task<AudioClip> LoadFromFileAsync(string filePath);
    Task<AudioClip> LoadFromUrlAsync(string url);
    Task<(AudioClip clip, string title, string artist)> LoadWithMetadataAsync(string filePath);
    void UnloadClip(AudioClip clip);
}
```

### IDefaultCoverProvider

默认封面提供器。

```csharp
public interface IDefaultCoverProvider
{
    Sprite DefaultMusicCover { get; }
    Sprite DefaultAlbumCover { get; }
    Sprite LocalMusicCover { get; }
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

参见 [ChillPatcher.Module.LocalFolder](../ChillPatcher.Module.LocalFolder/README.md) 获取完整的模块开发示例。
