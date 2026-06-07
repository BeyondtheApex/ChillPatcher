# LocalFolder 模块

本地文件夹音乐模块，扫描用户指定的音乐根目录，将发现的音频文件声明到 OmniMixPlayer 音乐库中。

这是 **OmniMixPlayer.SDK 的最简参考实现**，适合作为开发新模块的起点。

## 目录模型

```
<RootFolder>/
├── Playlist A/                 ← 一级子文件夹 = 歌单 + 标签
│   ├── playlist.json           ← 可选：自定义歌单名称
│   ├── cover.jpg               ← 歌单封面
│   ├── song1.mp3               ← 根目录散装歌曲 → 默认专辑
│   ├── Album X/                ← 二级子文件夹 = 专辑
│   │   ├── album.json          ← 可选：自定义专辑名
│   │   ├── cover.jpg           ← 专辑封面
│   │   ├── track1.flac
│   │   └── track2.mp3
│   └── Album Y/
│       └── track3.ogg
├── Playlist B/
│   └── ...
└── loose-track.mp3             ← 根目录散装 → "default" 歌单
```

## 支持的音频格式

`.mp3`, `.wav`, `.ogg`, `.egg`, `.flac`, `.aiff`, `.aif`

## 专辑封面查找

按以下优先级自动查找：

1. 专辑目录中的图片文件：`cover.jpg` > `cover.png` > `folder.jpg` > `folder.png` > `album.jpg` > `album.png` > `front.jpg` > `front.png`
2. 音频文件内嵌封面（MP3 ID3、FLAC metadata）

## 增量扫描

首次运行后，每个歌单文件夹内生成 `!rescan_playlist` 标记文件。数据库缓存位于 `<RootFolder>/.localfolder.db`。

**添加新歌曲：**

1. 放入歌单/专辑文件夹
2. 删除对应歌单的 `!rescan_playlist`
3. 重启 → 增量扫描（保留已有歌曲的 UUID/收藏/排除状态）

也可设置 `ForceRescan = true` 强制每次全量扫描。

## 后端声明流程

扫描时模块通过 `ILibraryRegistry` 执行：

1. `UpsertTag()` — 歌单文件夹作为标签
2. `UpsertPlaylist()` — 歌单作为歌单
3. `UpsertAlbum()` — 专辑文件夹
4. `UpsertTrack()` — 音频文件（确定性 UUID）
5. `SetTrackTags()` — 歌曲关联标签
6. `ReplacePlaylistEntries()` — 歌单有序条目

数据去重、过滤、持久化均由后端 `OmniMixPlayer.Backend` 管理。

## 刷新

`RefreshAsync` 先调用 `UnregisterModule()` 清除本模块所有声明，再执行完整重新扫描。

## 配置

通过 `IModuleConfigManager` 管理，可在图形化设置面板调整：

| 配置项        | 默认值                                 | 说明                     |
| ------------- | -------------------------------------- | ------------------------ |
| `RootFolder`  | `C:\Users\<用户名>\Music\ChillWithYou` | 音乐根目录路径           |
| `ForceRescan` | `false`                                | 强制全量扫描（忽略缓存） |

## 实现接口

| 接口                      | 说明                           |
| ------------------------- | ------------------------------ |
| `IMusicModule`            | 基础模块入口                   |
| `IMusicSourceProvider`    | 本地音乐源（SourceType.Local） |
| `ICoverProvider`          | 封面图片（文件系统 + 内嵌）    |
| `IFavoriteExcludeHandler` | 收藏/排除状态本地持久化        |
| `IDeleteHandler`          | 文件删除                       |
| `IModuleUIProvider`       | 设置面板（根目录选择）         |

## 作为 SDK 参考

此模块展示了 OmniMixPlayer.SDK 的标准模式：

- `InitializeAsync(IModuleContext)` 中通过 `context.Library` 声明数据
- 使用 `IModuleConfigManager` 读写配置
- `RefreshAsync` 中先 `UnregisterModule` 再重新注册
- 确定性 UUID (`Track.GenerateUuid(path)`) 保证重复扫描不产生重复歌曲
