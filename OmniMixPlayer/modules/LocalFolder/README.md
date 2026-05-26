# ChillPatcher.Module.LocalFolder

这是 ChillPatcher SDK 的参考实现模块，演示如何使用 SDK 创建自定义音乐源模块。

> **注意**：本文档主要作为 SDK 使用示例，功能说明请参见 [主 README](../README.md) 的"本地文件夹模块设置"部分。

## 模块概述

本模块从本地文件夹扫描音乐文件，并将其注册到 ChillPatcher 的音乐系统中。

- **模块 ID**: `com.chillpatcher.localfolder`
- **显示名称**: 本地文件夹
- **版本**: 1.0.0

## 项目结构

```
ChillPatcher.Module.LocalFolder/
├── LocalFolderModule.cs        ← 模块主入口
├── ModuleInfo.cs               ← 模块信息常量
├── Models/
│   └── JsonModels.cs           ← 数据模型
└── Services/
    ├── LocalDatabase.cs        ← 数据库服务入口
    ├── Cover/                  ← 封面加载服务
    │   ├── CoverLoader.cs
    │   ├── CoverSearcher.cs
    │   └── ImageLoader.cs
    ├── Database/               ← 数据库仓储
    │   ├── DatabaseCore.cs
    │   ├── CacheRepository.cs
    │   ├── CoverCacheRepository.cs
    │   ├── FavoriteRepository.cs
    │   ├── ExcludedRepository.cs
    │   ├── PlayStatsRepository.cs
    │   └── CleanupService.cs
    └── Scanner/                ← 文件扫描服务
        ├── FolderScanner.cs
        ├── MetadataReader.cs
        ├── CacheManager.cs
        ├── AudioFileHelper.cs
        ├── RescanFlagManager.cs
        └── ScanResult.cs
```

## SDK 使用示例

### 1. 模块声明

使用 `[MusicModule]` 属性声明模块：

```csharp
[MusicModule(ModuleInfo.MODULE_ID, ModuleInfo.MODULE_NAME, 
    Version = ModuleInfo.MODULE_VERSION, 
    Author = ModuleInfo.MODULE_AUTHOR,
    Description = ModuleInfo.MODULE_DESCRIPTION,
    Priority = 10)]
public class LocalFolderModule : IMusicModule, IMusicSourceProvider, ICoverProvider, IFavoriteExcludeHandler
{
    // ...
}
```

### 2. 实现多个接口

本模块实现了以下接口：

| 接口 | 用途 |
|------|------|
| `IMusicModule` | 基础模块接口（必须） |
| `IMusicSourceProvider` | 提供音乐列表和加载功能 |
| `ICoverProvider` | 提供封面加载功能 |
| `IFavoriteExcludeHandler` | 管理收藏和排除状态 |

### 3. 模块能力声明

```csharp
public ModuleCapabilities Capabilities => new ModuleCapabilities
{
    CanDelete = false,      // 不支持删除（保护本地文件）
    CanFavorite = true,     // 支持收藏
    CanExclude = true,      // 支持排除
    SupportsLiveUpdate = false,  // 不支持实时更新
    ProvidesCover = true,   // 提供封面
    ProvidesAlbum = true    // 提供专辑
};
```

### 4. 初始化流程

```csharp
public async Task InitializeAsync(IModuleContext context)
{
    _context = context;

    // 1. 加载原生依赖
    LoadNativeDependencies();

    // 2. 注册配置项
    RegisterConfig();

    // 3. 初始化数据库
    var dbPath = Path.Combine(_dataPath, ".localfolder.db");
    _database = new LocalDatabase(dbPath, context.Logger);

    // 4. 初始化封面加载器
    _coverLoader = new CoverLoader(_database, context.DefaultCover, context.Logger);

    // 5. 初始化文件夹扫描器
    _scanner = new FolderScanner(...);

    // 6. 订阅事件
    SubscribeEvents();

    // 7. 扫描并注册
    await ScanAndRegisterAsync();
}
```

### 5. 配置注册

使用模块配置管理器注册配置项：

```csharp
private void RegisterConfig()
{
    var config = _context.ConfigManager;

    _rootFolder = config.BindDefault(
        "RootFolder",
        Path.Combine(Environment.SpecialFolder.MyMusic, "ChillWithYou"),
        "本地音乐根目录。"
    );

    _forceRescan = config.BindDefault(
        "ForceRescan",
        false,
        "是否每次启动都强制重新扫描。"
    );
}
```

配置将保存到 `[Module:com.chillpatcher.localfolder]` 分区。

### 6. 注册歌曲、专辑和标签

```csharp
private async Task ScanAndRegisterAsync()
{
    var scanResult = await _scanner.ScanAsync();

    foreach (var tag in scanResult.Tags)
    {
        // 注册 Tag（播放列表）
        _context.TagRegistry.RegisterTag(tag);
    }

    foreach (var album in scanResult.Albums)
    {
        // 注册专辑
        _context.AlbumRegistry.RegisterAlbum(album);
    }

    foreach (var music in scanResult.Music)
    {
        // 注册歌曲
        _context.MusicRegistry.RegisterMusic(music);
    }
}
```

### 7. 实现音频加载

```csharp
public async Task<AudioClip> LoadAudioAsync(string uuid)
{
    var musicInfo = GetMusicInfo(uuid);
    if (musicInfo == null) return null;

    return await _context.AudioLoader.LoadAsync(musicInfo.FilePath);
}
```

### 8. 事件订阅

```csharp
private void SubscribeEvents()
{
    _context.EventBus.Subscribe<RescanRequestEvent>(OnRescanRequest);
}

private void OnRescanRequest(RescanRequestEvent evt)
{
    if (evt.ModuleId == ModuleId)
    {
        _ = ScanAndRegisterAsync();
    }
}
```

## 原生依赖

本模块使用 SQLite 作为本地数据库，需要加载原生 SQLite 库：

```
native/
├── x64/
│   └── SQLite.Interop.dll
└── x86/
    └── SQLite.Interop.dll
```

加载方式：

```csharp
private void LoadNativeDependencies()
{
    var moduleDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    var arch = IntPtr.Size == 8 ? "x64" : "x86";
    var sqlitePath = Path.Combine(moduleDir, "native", arch, "SQLite.Interop.dll");
    
    NativeMethods.LoadLibrary(sqlitePath);
}
```

## 开发新模块

如果你想基于本模块开发自己的音乐源模块：

1. 创建新的类库项目
2. 引用 `ChillPatcher.SDK.dll`
3. 实现 `IMusicModule` 接口
4. 根据需要实现其他可选接口
5. 使用 `[MusicModule]` 属性声明模块
6. 编译并部署到 `BepInEx/plugins/ChillPatcher/modules/<ModuleId>/`

详细的 SDK 文档请参见 [ChillPatcher.SDK](../ChillPatcher.SDK/README.md)。
