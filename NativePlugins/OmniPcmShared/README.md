# OmniPcmShared — 原生客户端嵌入 SDK

OmniPcmShared 是面向游戏/应用开发者的**原生 C ABI SDK**。它封装了与 OmniMixPlayer 后端通信的全部复杂性——共享内存 PCM 读取、gRPC-Web 控制面、WebSocket 事件——暴露为一组纯 C 函数，可从 **C、C++、C# (P/Invoke)、Rust、Unity、Unreal、Godot** 等任意语言/引擎调用。

> 📌 **这是 C ABI 嵌入层**。如果你在开发 C# 音乐源模块，应使用 [OmniMixPlayer.SDK](../../OmniMixPlayer/OmniMixPlayer.SDK/README.md)。OmniPcmShared 面向的是**消费端**——把 OmniMixPlayer 的音频流嵌入你的游戏或应用中。

---

## 架构

```
┌─────────────────────────────────────────────────────┐
│                  你的游戏 / 应用                      │
│  ┌──────────────────┐  ┌─────────────────────────┐  │
│  │ 音频回调 (PCM)    │  │ 控制逻辑 (UI/游戏循环)    │  │
│  │ OmniPcm_ReadFrames│  │ OmniPcmClient_*         │  │
│  └────────┬─────────┘  └───────────┬─────────────┘  │
└───────────┼─────────────────────────┼────────────────┘
            │ P/Invoke / FFI          │ P/Invoke / FFI
┌───────────▼─────────────────────────▼────────────────┐
│              OmniPcmShared.dll (C ABI)               │
│  ┌──────────────────┐  ┌─────────────────────────┐  │
│  │ 共享内存读取器    │  │ gRPC-Web 控制面客户端    │  │
│  │ (ring buffer)    │  │ (cpp-httplib + protobuf) │  │
│  └────────┬─────────┘  └───────────┬─────────────┘  │
└───────────┼─────────────────────────┼────────────────┘
            │ Windows Named            │ HTTP/WS
            │ Shared Memory            │ localhost
┌───────────▼─────────────────────────▼────────────────┐
│            OmniMixPlayer.Backend (ASP.NET Core)       │
│  音频解码 · 流媒体管理 · 歌单 · 播放队列 · 音量/EQ    │
└─────────────────────────────────────────────────────┘
```

**两套独立的 API：**

| API 前缀          | 职责                                    | 典型调用频率       |
| ----------------- | --------------------------------------- | ------------------ |
| `OmniPcm_*`       | 从共享内存读取解码后的 float PCM 音频帧 | 每音频回调 (~10ms) |
| `OmniPcmClient_*` | 控制面：连接、播放、队列、音量、事件    | 按需 / 每秒心跳    |

---

## 构建

```bat
cd NativePlugins\OmniPcmShared
build.bat
```

输出：

```text
bin/native/x64/OmniPcmShared.dll
bin/native/x86/OmniPcmShared.dll
```

### 依赖

- **CMake** 3.20+
- **MSVC** (Visual Studio 2022) 或 Clang-CL
- protobuf-lite (通过 vcpkg 或系统包管理)

---

## 第一部分：共享内存音频 API (`OmniPcm_*`)

这组 API 从 OmniMixPlayer.Backend 创建的 Windows 命名共享内存中**零拷贝**读取解码后的 float PCM 音频帧。

### 生命周期

```c
// 1. 打开共享内存
OmniPcmHandle h = OmniPcm_Open(L"Global\\OmniMixPlayer_PCM");
if (!OmniPcm_IsOpen(h)) { /* 后端未运行或共享内存未创建 */ }

// 2. 等待格式就绪（知道采样率/声道数后才能创建音频对象）
OmniPcm_WaitForFormatReady(h, "track-uuid", 5000);

OmniPcmInfo info;
OmniPcm_GetInfo(h, &info);
// info.sample_rate, info.channels → 创建游戏的 AudioSource

// 3. 绑定到当前播放流
OmniPcm_BindCurrentStream(h);

// 4. 音频回调中读取 PCM
float buffer[4096];  // frames * channels
int64_t read = OmniPcm_ReadFrames(h, buffer, 1024);

// 5. 游戏更新循环中汇报播放位置
OmniPcm_ReportAudioSourcePosition(h, currentTimeSamples);

// 6. 检查播放是否完成
if (OmniPcm_IsPlaybackComplete(h, 4096)) {
    // 切下一首
}

// 7. 关闭
OmniPcm_Close(h);
```

### API 参考

#### 连接管理

| 函数                           | 说明                                                |
| ------------------------------ | --------------------------------------------------- |
| `OmniPcm_Open(wchar_t* name)`  | 打开命名共享内存，默认名 `Global\OmniMixPlayer_PCM` |
| `OmniPcm_OpenUtf8(char* name)` | UTF-8 版本                                          |
| `OmniPcm_Close(handle)`        | 关闭句柄                                            |
| `OmniPcm_IsOpen(handle)`       | 检查共享内存是否可访问                              |
| `OmniPcm_GetVersion(handle)`   | 获取协议版本（1 或 2）                              |

#### 格式与状态

| 函数                                                  | 说明                                       |
| ----------------------------------------------------- | ------------------------------------------ |
| `OmniPcm_GetInfo(handle, &info)`                      | 获取音频格式：采样率、声道数、帧数         |
| `OmniPcm_GetSnapshot(handle, &snap)`                  | 获取共享内存完整快照（游标、状态、错误码） |
| `OmniPcm_GetCurrentUuid(handle)`                      | 当前播放曲目的 UUID                        |
| `OmniPcm_IsFormatReady(handle)`                       | 格式信息是否已就绪                         |
| `OmniPcm_WaitForFormatReady(handle, uuid, timeoutMs)` | 阻塞等待指定 UUID 的格式就绪               |

#### 流绑定

| 函数                                | 说明                 |
| ----------------------------------- | -------------------- |
| `OmniPcm_BindCurrentStream(handle)` | 绑定到当前活跃流     |
| `OmniPcm_BindStream(handle, uuid)`  | 绑定到指定 UUID 的流 |
| `OmniPcm_GetBoundStreamId(handle)`  | 获取当前绑定的流 ID  |

#### PCM 读取

| 函数                                                     | 说明                                                                |
| -------------------------------------------------------- | ------------------------------------------------------------------- |
| `OmniPcm_ReadFrames(handle, buffer, n)`                  | 读取最多 n 帧交错 float32 PCM。返回实际读取帧数，EOF/错误时返回负数 |
| `OmniPcm_RequestSeek(handle, frame)`                     | 请求 Seek，重置游戏音频源后用                                       |
| `OmniPcm_CancelPendingSeek(handle)`                      | 取消待定 Seek                                                       |
| `OmniPcm_SetAudibleCursor(handle, frame, allowBackward)` | 报告已实际听到的帧位置                                              |
| `OmniPcm_ReportAudioSourcePosition(handle, timeSamples)` | 报告游戏音频引擎的当前播放位置（采样数）                            |

#### 流状态查询

| 函数                                                  | 说明                           |
| ----------------------------------------------------- | ------------------------------ |
| `OmniPcm_HasDecoderEof(handle)`                       | 解码器是否已到达 EOF           |
| `OmniPcm_IsPlaybackComplete(handle, toleranceFrames)` | 播放是否完全结束（含末尾缓冲） |
| `OmniPcm_HasError(handle)`                            | 流是否处于错误状态             |

### 数据结构

**OmniPcmInfo** — 音频格式信息：

```c
typedef struct {
    int32_t sample_rate;           // 采样率 (Hz)，如 44100
    int32_t channels;              // 声道数，如 2
    int32_t bytes_per_frame;       // 每帧字节数 (channels * sizeof(float))
    int32_t buffer_frames;         // 环形缓冲区总帧数
    int64_t total_frames_hint;     // 预估总帧数
    int64_t decoded_total_frames;  // 已解码帧数
    int64_t effective_total_frames;// 有效总帧数（用于进度计算）
} OmniPcmInfo;
```

**OmniPcmSnapshot** — 共享内存完整快照：

```c
typedef struct {
    uint32_t version;              // 协议版本 (1 或 2)
    int32_t sample_rate, channels, bytes_per_frame, buffer_frames;
    uint32_t flags;                // OmniPcmStreamFlags 位掩码
    int64_t write_cursor;          // 服务端写入位置
    int64_t read_cursor;           // 客户端读取位置
    int64_t stream_id;             // 当前流标识
    int32_t state;                 // OmniPcmStreamState
    int32_t error_code;            // OmniPcmStreamError
    int64_t final_write_cursor;    // 流结束时的最终写入位置
    int64_t audible_cursor;        // 客户端报告的已听到位置
    int64_t seek_frame;            // 待处理的 Seek 目标帧
    int64_t seek_generation;       // Seek 代数（版本控制）
    char    current_uuid[64];      // 当前曲目 UUID
} OmniPcmSnapshot;
```

### PCM 数据格式

- **编码**：float32, 交错 (interleaved)
- **范围**：`[-1.0, +1.0]`
- **布局**：`[L0, R0, L1, R1, L2, R2, ...]`
- **每帧**：`channels × sizeof(float)` 字节

---

## 第二部分：控制面 API (`OmniPcmClient_*`)

控制面客户端通过 gRPC-Web (HTTP/1.1) 与后端通信，内部使用 protobuf-lite。

### 客户端生命周期

```c
// 创建客户端（自动发现后端端口）
OmniPcmClientConfig cfg = {0};
// cfg.host = "127.0.0.1";     // 默认
// cfg.port = 0;                // 0 = 自动发现 ominmix_port.txt
// cfg.timeout_ms = 3000;
OmniPcmClientHandle client = OmniPcmClient_Create(&cfg);

// ... 使用 API ...

OmniPcmClient_Destroy(client);
```

**端口发现**：当 `port = 0` 时，DLL 在工作目录及父目录中搜索 `omnimix_port.txt` 文件。

### 实例管理

| 函数                                                 | 说明                     |
| ---------------------------------------------------- | ------------------------ |
| `OmniPcmClient_ConnectInstance(client, opts, &info)` | 创建或重连实例           |
| `OmniPcmClient_Heartbeat(client, id, &alive)`        | 发送心跳（建议每秒一次） |
| `OmniPcmClient_DisconnectInstance(client, id)`       | 断开连接                 |
| `OmniPcmClient_DeleteInstance(client, id, &deleted)` | 永久删除实例及其配置     |
| `OmniPcmClient_ListInstances(client, arr, &count)`   | 列出所有实例             |

**OmniPcmConnectOptions**：

```c
typedef struct {
    const char* client_id;            // 客户端唯一标识
    const char* mod_id;               // Mod 标识
    const char* game_name;            // 游戏名
    const char* display_name;         // 显示名
    int32_t     kind;                 // OMNI_PCM_INSTANCE_KIND_GAME_MOD 等
    uint32_t    capability_flags;     // 能力位掩码
    int32_t     no_instance;          // 禁止自动创建实例
    int32_t     max_imported_playlists;
    int32_t     max_tags;
    int32_t     max_playlist_entries;
} OmniPcmConnectOptions;
```

### 播放控制

| 函数                                             | 说明                               |
| ------------------------------------------------ | ---------------------------------- |
| `OmniPcmClient_GetStatus(client, id, &status)`   | 获取播放状态                       |
| `OmniPcmClient_PlaybackCommand(client, id, cmd)` | 通用命令（播放/暂停/下一首等）     |
| `OmniPcmClient_Play(client, id, uuid)`           | 播放指定曲目（uuid=NULL 恢复播放） |
| `OmniPcmClient_Seek(client, id, seconds)`        | Seek 到指定秒数                    |
| `OmniPcmClient_SetVolume(client, id, vol)`       | 设置音量 0.0~1.0                   |
| `OmniPcmClient_GetVolume(client, id, &vol)`      | 获取音量                           |
| `OmniPcmClient_SetShuffle(client, id, enabled)`  | 切换随机播放                       |
| `OmniPcmClient_SetRepeatMode(client, id, mode)`  | 设置循环模式                       |

### 队列与历史

| 函数                                                         | 说明               |
| ------------------------------------------------------------ | ------------------ |
| `OmniPcmClient_GetQueue(client, id, arr, &count)`            | 获取播放队列       |
| `OmniPcmClient_AddToQueue(client, id, uuid)`                 | 追加到队尾         |
| `OmniPcmClient_InsertIntoQueue(client, id, uuids, n, index)` | 在指定位置批量插入 |
| `OmniPcmClient_SetQueue(client, id, uuids, n)`               | 替换整个队列       |
| `OmniPcmClient_RemoveFromQueueIndex/Uuid(client, id, ...)`   | 移除队列项         |
| `OmniPcmClient_MoveInQueue(client, id, from, to)`            | 移动队列项         |
| `OmniPcmClient_ClearQueue(client, id)`                       | 清空队列           |
| `OmniPcmClient_GetHistory(client, id, arr, &count)`          | 获取播放历史       |
| `OmniPcmClient_RemoveFromHistory(client, id, index)`         | 移除历史项         |
| `OmniPcmClient_ClearHistory(client, id)`                     | 清空历史           |

### 歌单源

| 函数                                                        | 说明             |
| ----------------------------------------------------------- | ---------------- |
| `OmniPcmClient_GetPlaylistSources(client, id, arr, &count)` | 获取已选的歌单源 |
| `OmniPcmClient_SetPlaylistSources(client, id, sources, n)`  | 设置歌单源选择   |

### 音乐库查询

| 函数                                                        | 说明         |
| ----------------------------------------------------------- | ------------ |
| `OmniPcmClient_QueryTracks(client, &query, arr, &count)`    | 查询歌曲     |
| `OmniPcmClient_GetTrack(client, uuid, &track)`              | 获取单曲详情 |
| `OmniPcmClient_SetTrackExcluded(client, uuid, excluded)`    | 设置排除状态 |
| `OmniPcmClient_QueryAlbums(client, &query, arr, &count)`    | 查询专辑     |
| `OmniPcmClient_QueryTags(client, &query, arr, &count)`      | 查询标签     |
| `OmniPcmClient_QueryPlaylists(client, &query, arr, &count)` | 查询歌单     |

### 均衡器

| 函数                                                             | 说明               |
| ---------------------------------------------------------------- | ------------------ |
| `OmniPcmClient_GetEqualizer(client, id, &state, points, &count)` | 获取 EQ 状态和频点 |
| `OmniPcmClient_SetEqualizer(client, id, &state, points, n)`      | 设置 EQ            |

### 存档

| 函数                                                                   | 说明           |
| ---------------------------------------------------------------------- | -------------- |
| `OmniPcmClient_GetProfile(client, id, &profile)`                       | 获取实例配置   |
| `OmniPcmClient_UpdateProfile(client, &profile, &saved)`                | 更新配置       |
| `OmniPcmClient_ArchiveInstance(client, id, label, &archive)`           | 存档当前配置   |
| `OmniPcmClient_ListArchives(client, arr, &count)`                      | 列出存档       |
| `OmniPcmClient_GetArchive(client, archiveId, &archive)`                | 获取存档       |
| `OmniPcmClient_DeleteArchive(client, archiveId, &deleted)`             | 删除存档       |
| `OmniPcmClient_InheritFromArchive(client, newId, archiveId, &profile)` | 从存档创建实例 |

### 后端管理

| 函数                                          | 说明                   |
| --------------------------------------------- | ---------------------- |
| `OmniPcmClient_GetBackendInfo(client, &info)` | 获取后端健康状态和版本 |
| `OmniPcmClient_StopBackend(client)`           | 停止后端服务           |

### WebSocket 事件

```c
void OnEvent(const OmniPcmEventInfo* e, void* user_data) {
    // e->type: "track_changed", "playback_state_changed",
    //          "volume_changed", "queue_changed", "backend_status", ...
    // e->instance_id, e->track_uuid, e->title, e->artist,
    // e->position, e->duration, e->state, ...
}

OmniPcmClient_StartEvents(client, OnEvent, NULL);
// ...
OmniPcmClient_StopEvents(client);
```

事件通过 WebSocket (`/ws`) 推送，DLL 内部将 protobuf 二进制消息解码为扁平的 `OmniPcmEventInfo` 结构体。

---

## 错误处理

所有函数通过返回值表示结果。`OmniPcm_*` 函数返回负数表示错误：

| 返回值                       | 含义                                     |
| ---------------------------- | ---------------------------------------- |
| `OMNI_PCM_OK` (0)            | 成功                                     |
| `OMNI_PCM_ERROR` (-1)        | 一般错误（调用 `GetLastError` 获取详情） |
| `OMNI_PCM_NOT_READY` (-2)    | 资源未就绪（重试）                       |
| `OMNI_PCM_EOF` (-3)          | 流已结束                                 |
| `OMNI_PCM_BAD_ARGUMENT` (-4) | 参数无效                                 |
| `OMNI_PCM_UNSUPPORTED` (-5)  | 不支持的操作                             |
| `OMNI_PCM_WRONG_STREAM` (-6) | 流 ID 不匹配（曲目已切换）               |

获取详细错误信息：

```c
const char* err = OmniPcm_GetLastError(handle);        // PCM API
const char* err = OmniPcmClient_GetLastError(client);   // Client API
```

### List API 约定

使用 `out_arr == NULL && *inout_count = 0` 查询所需条目数：

```c
int32_t count = 0;
OmniPcmClient_GetQueue(client, id, NULL, &count);
// 返回 OMNI_PCM_NOT_READY，count = 实际条目数

OmniPcmQueueTrackInfo* tracks = malloc(count * sizeof(OmniPcmQueueTrackInfo));
OmniPcmClient_GetQueue(client, id, tracks, &count);
// 返回 OMNI_PCM_OK
```

---

## 集成指南

### C/C++

```c
#include "omni_pcm_shared.h"
// 链接 OmniPcmShared.lib，运行时需要 OmniPcmShared.dll
```

### C# (Unity / BepInEx)

项目自带即用型 P/Invoke 封装：

```
NativePlugins/OmniPcmShared/
├── OmniPcmShared.Interop.cs    ← PCM 共享内存绑定
└── OmniPcmClient.Interop.cs    ← 控制面客户端封装 (IDisposable)
```

使用方式：

```csharp
using OmniPcmShared.Interop;

// 控制面
using var client = new OmniPcmClient();
var info = client.ConnectInstance("my-game", OmniPcmCapabilityFlags.AudioPlayback);
client.Heartbeat(info.InstanceId);
var status = client.GetStatus(info.InstanceId);

// 事件
client.StartEvents((e) => {
    Debug.Log($"Track: {e.Title} - {e.Artist}");
});
```

> 这两个 `.cs` 文件可独立放入任何 C# 项目，无 NuGet 依赖。只需确保 `OmniPcmShared.dll` 在输出目录。

### Unity 具体步骤

1. 将 `OmniPcmShared.dll` 放入 `Assets/Plugins/x86_64/`
2. 将 `OmniPcmShared.Interop.cs` 和 `OmniPcmClient.Interop.cs` 放入项目中
3. 在 `MonoBehaviour` 中创建 `OmniPcmClient`，在 `OnAudioFilterRead` 中调用 P/Invoke

### Unreal Engine

```cpp
// 在 Build.cs 中添加
PublicAdditionalLibraries.Add(Path.Combine(PluginDir, "Binaries/Win64/OmniPcmShared.lib"));
PublicDelayLoadDLLs.Add("OmniPcmShared.dll");

// 使用标准 C ABI 调用
```

---

## EOF 语义

对于 v2 协议流，播放结束的条件：

```
(DecoderEof 或 SyntheticEof)
AND readCursor >= finalWriteCursor
AND audibleCursor >= finalWriteCursor - tolerance
```

- **readCursor**：游戏音频回调已从共享内存复制走的 PCM 帧位置
- **audibleCursor**：游戏认为这些帧已实际到达扬声器的位置
- **tolerance**：允许的末尾容差帧数（如 4096）

这种三重检查确保不会在最后一段缓冲音频被听到之前就提前切歌。

---

## 线程安全

- `OmniPcm_ReadFrames` 可从音频线程调用
- `OmniPcmClient_*` 应从主/游戏线程调用
- 不同 `OmniPcmHandle` 之间线程安全；同一 handle 的并发访问需要外部同步

---

## 重新生成 Protobuf 类型

```powershell
# 在项目根目录执行
$protoc = "$env:TEMP\protoc\bin\protoc.exe"
& $protoc `
  --proto_path=OmniMixPlayer\OmniMixPlayer.SDK\Protos `
  --cpp_out=lite:NativePlugins\OmniPcmShared\generated `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\common.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\track.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\album.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\tag.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\playlist.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\query.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\models\instance.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\events\ws_events.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\services\library.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\services\playback.proto `
  OmniMixPlayer\OmniMixPlayer.SDK\Protos\omni_mix_player\services\instance.proto
```

---

## 许可证

OmniPcmShared 采用 **MIT 许可证**，可自由嵌入闭源商业游戏/软件中，无需公开源码。
