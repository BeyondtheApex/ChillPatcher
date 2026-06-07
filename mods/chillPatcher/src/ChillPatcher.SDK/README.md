# ChillPatcher SDK — ⚠️ 已废弃

> **此 SDK 已停止维护，仅保留兼容性空壳。所有新模块开发请使用 OmniMixPlayer.SDK。**

## 迁移到 OmniMixPlayer.SDK

请参考 **[OmniMixPlayer.SDK 模块开发指南](../../../OmniMixPlayer/OmniMixPlayer.SDK/README.md)**。

### 快速对照

| 旧 (`ChillPatcher.SDK`)       | 新 (`OmniMixPlayer.SDK`)        |
| ----------------------------- | ------------------------------- |
| 命名空间 `ChillPatcher.SDK.*` | `OmniMixPlayer.SDK.*`           |
| `context.MusicRegistry`       | `context.Library`               |
| `context.TagRegistry`         | `context.Library.UpsertTag()`   |
| `context.AlbumRegistry`       | `context.Library.UpsertAlbum()` |
| `context.AudioLoader`         | `context.StreamingService`      |
| `MusicInfo` 数据模型          | Protobuf 生成的 `Track`         |

### 核心变更

1. **统一注册 API**：不再有分别的 MusicRegistry/TagRegistry/AlbumRegistry，统一使用 `ILibraryRegistry`
2. **Protobuf 数据模型**：所有模型类型由 `.proto` 文件生成，提供跨语言兼容性
3. **流式解码服务**：`IStreamingService` 替代旧的 `AudioLoader`，支持异步等待就绪

### 当前 ChillPatcher.SDK 保留的接口

此 SDK 目录中仅保留以下接口用于兼容现有 ChillPatcher 插件内部调用：

| 文件                             | 用途                                     |
| -------------------------------- | ---------------------------------------- |
| `Interfaces/ICoreServices.cs`    | ChillPatcher 内部核心服务                |
| `Interfaces/ICustomJSApi.cs`     | OneJS UI 桥接                            |
| `Interfaces/IPcmStreamReader.cs` | 旧版 PCM 读取器（ChillPatcher 内部使用） |
| `Interfaces/IPlaybackBridge.cs`  | 播放控制桥接                             |

**新模块不应引用或实现这些接口**。它们仅服务于 ChillPatcher 内部已有功能，不对外提供扩展点。

// 取消订阅
subscription.Dispose();

````

### 可用事件类型

| 事件                | 说明          |
| ------------------- | ------------- |
| `PlayStartedEvent`  | 播放开始      |
| `PlayEndedEvent`    | 播放结束      |
| `PlayPausedEvent`   | 播放暂停/恢复 |
| `PlayProgressEvent` | 播放进度变化  |

## 注册表接口

### ITagRegistry

```csharp
TagInfo RegisterTag(string tagId, string displayName, string moduleId);
void UnregisterTag(string tagId);
TagInfo GetTag(string tagId);
IReadOnlyList<TagInfo> GetAllTags();
IReadOnlyList<TagInfo> GetTagsByModule(string moduleId);
````

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
