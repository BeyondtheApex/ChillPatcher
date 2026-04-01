# 网易云音乐认证恢复与试听检测

> 解决 [#58](https://github.com/BeyondtheApex/ChillPatcher/issues/58)：网易云播放时歌曲无法完播，cookie 过期后静默降级为试听片段。

## 问题根因

1. **Fallback API 不检测试听**：`NeteaseGetSongURL` 的 v1 API 正确检测了 `FreeTrialInfo`，但 fallback API（`SongUrlService`）的响应结构体缺少该字段，盲目接受试听 URL
2. **无 cookie 过期感知**：运行时从不验证 cookie 有效性，过期后所有歌曲静默降级为 ~30s 试听
3. **无恢复机制**：播放器收到 EOF 后直接跳到下一首，无法区分"歌曲播完"和"试听结束"
4. **无账号管理 UI**：没有登录状态显示、登出、重新登录入口

## 设计方案

采用分层架构（方案 B），新增 `NeteaseSessionManager` 管理认证生命周期，不改动现有 `QRLoginManager`。

### 恢复策略

静默 `RefreshLogin` 优先，失败则自动弹 QR 登录。当前歌曲暂停等待恢复，恢复后重新 resolve。

## 1. Go Bridge 层改动

### 1.1 SongURL — 试听标记

`SongURL` struct 新增 `IsTrial bool` 字段：

```go
type SongURL struct {
    ID      int64  `json:"id"`
    URL     string `json:"url"`
    Size    int64  `json:"size"`
    Type    string `json:"type"`
    IsTrial bool   `json:"isTrial"`
}
```

`NeteaseGetSongURL` 改动：

- Fallback API 响应结构体加上 `FreeTrialInfo interface{}` 字段
- 两个 API 都检测到试听时，仍返回试听 URL（C# 层决定是否使用），但设置 `IsTrial: true`
- 返回试听 URL 而非 null 的原因：C# 层可以选择先播着等重新登录

兼容性：JSON 新增字段是纯加法，旧版 C# 没有此属性会自动忽略，默认 `false`。

### 1.2 UserInfo — VIP 状态

`UserInfo` struct 新增 `VipType int` 字段：

```go
type UserInfo struct {
    UserID    int64  `json:"userId"`
    Nickname  string `json:"nickname"`
    AvatarURL string `json:"avatarUrl"`
    VipType   int    `json:"vipType"`
}
```

- `NeteaseRefreshLogin` 从 `AccountInfo()` 的 raw JSON 额外提取 `profile.vipType`
- `NeteaseGetUserInfo` 返回时包含 `vipType`

兼容性：老用户升级后首次启动 `vipType=0`（显示为"免费用户"），一次 `RefreshLogin` 后更新为真实值。

### 1.3 NeteaseLogout — 新增函数

```go
//export NeteaseLogout
func NeteaseLogout() C.int
```

执行：
1. 清除内存中 `currentUser`
2. 删除 cookie 文件（`{dataDir}/cookie`）
3. 用空 jar 替换全局 cookie jar
4. 清除本地 DB 中的用户信息

### 1.4 错误码区分

`NeteaseRefreshLogin` 需要区分网络错误和认证错误：

| 返回值 | 含义 |
|--------|------|
| 1 | 成功 |
| 0 | 认证失败（cookie 无效） |
| -1 | 网络错误（超时/DNS/连接失败） |

判断依据：HTTP 请求层错误 → 网络错误；HTTP 200 但账号信息获取失败 → 认证失败。

## 2. C# Bridge 层改动

### NeteaseBridge.cs

- `SongUrl` model 加 `bool IsTrial` 属性（`[JsonProperty("isTrial")]`）
- `UserInfo` 加 `int VipType` 属性（`[JsonProperty("vipType")]`）
- 新增 `Logout()` P/Invoke 包装
- `RefreshLogin()` 返回值映射为枚举：`Success` / `AuthFailed` / `NetworkError`

## 3. NeteaseSessionManager（新文件）

### 职责

认证生命周期的单一入口。NeteaseModule 只通过它处理认证相关的一切。

### 类结构

```csharp
public class NeteaseSessionManager
{
    // 状态
    public SessionState State { get; }       // LoggedIn / Expired / LoggingIn / LoggedOut
    public UserInfo UserInfo { get; }        // 昵称/头像/VipType
    public event Action<SessionState> OnStateChanged;

    // 核心方法
    public async Task<bool> ValidateAndRefreshAsync()
    public void TriggerQRLogin()
    public void Logout()

    // 试听恢复，Recovered 时 RecoveredSongUrl 包含新的完整 URL
    public async Task<TrialRecoveryResult> HandleTrialAsync(long songId, string quality)
    public SongUrl RecoveredSongUrl { get; }  // HandleTrialAsync 恢复成功后的新 URL
}

public enum SessionState { LoggedOut, LoggedIn, Expired, LoggingIn }
public enum TrialRecoveryResult { Recovered, VipRestricted, NetworkError }
```

### 与现有组件的关系

```
NeteaseModule
  ├─ NeteaseSessionManager (new)
  │    ├─ calls NeteaseBridge (RefreshLogin, IsLoggedIn, GetUserInfo, Logout)
  │    ├─ calls QRLoginManager (StartLogin, CancelLogin) ← 不改 QRLoginManager
  │    └─ exposes ICustomJSApi → UI 面板
  └─ ResolveAsync()
       └─ 检测 isTrial → sessionManager.HandleTrialAsync()
```

### HandleTrialAsync 流程

```
HandleTrialAsync(songId, quality):
  1. previousState = State
  2. RefreshLogin()
     → 网络错误 → State = previousState（回滚），return NetworkError
     → 认证失败 → State = Expired
       3. TriggerQRLogin()
       4. State = LoggingIn
       5. await QR 登录成功回调
       6. 重新 GetSongUrl(songId, quality)
         → 非 trial → return Recovered
         → 仍 trial → return VipRestricted
     → 成功 → State = LoggedIn
       → 重新 GetSongUrl(songId, quality)
         → 非 trial → return Recovered
         → 仍 trial → return VipRestricted
```

### NeteaseModule.ResolveAsync 改动

```csharp
var songUrl = await GetSongUrl(songId, quality);
if (songUrl.IsTrial)
{
    var result = await _sessionManager.HandleTrialAsync(songId, quality);
    switch (result)
    {
        case Recovered:
            songUrl = _sessionManager.RecoveredSongUrl; // 直接使用恢复后的 URL
            break;
        case VipRestricted:
            _logger.LogWarning($"Song {songId} requires VIP, skipping");
            return null;
        case NetworkError:
            _logger.LogWarning($"Network unavailable, skipping song {songId}");
            return null;
    }
}
```

### 初始化时机

模块 `InitializeAsync` 中，`IsLoggedIn == true` 时主动调用 `ValidateAndRefreshAsync()`：
- 刷新 VIP 状态（解决老用户升级后 vipType=0 的问题）
- 提前发现 cookie 过期

## 4. NeteaseAccountApi（新文件）

注册为 `chill.custom.get("netease_account")`。

### 属性（UI 轮询读取）

| 属性 | 类型 | 说明 |
|------|------|------|
| `sessionState` | string | `"logged_in"` / `"logged_out"` / `"expired"` / `"logging_in"` |
| `nickname` | string | 昵称，未登录时 `""` |
| `avatarUrl` | string | 头像 URL |
| `vipType` | int | 0=免费, 11=黑胶VIP 等 |
| `statusMessage` | string | QR 扫码状态提示文案 |
| `qrCodeBase64` | string | QR 图片 base64 PNG，空串表示无 QR |

### 操作（UI 按钮调用）

| 方法 | 说明 |
|------|------|
| `triggerLogin()` | 手动触发 QR 登录 |
| `logout()` | 登出 |
| `refreshSession()` | 手动刷新登录态 |

### 数据流

```
NeteaseSessionManager
  ├─ State/UserInfo 变化 → 更新 NeteaseAccountApi 属性
  └─ QRLoginManager.OnStatusChanged → 更新 statusMessage/qrCodeBase64

UI (500ms poll)
  ├─ 读属性 → 渲染
  └─ 按钮点击 → 调操作方法
```

## 5. UI 面板

### 文件位置

`ui/window-manager/plugins/netease/index.tsx`

### 界面结构

```
┌─────────────────────────────┐
│  网易云音乐                   │
├─────────────────────────────┤
│  [头像]  昵称                 │
│          黑胶VIP / 免费用户    │
│          ● 已登录              │
├─────────────────────────────┤
│  [刷新登录态]  [登出]          │
├─────────────────────────────┤
│  (QR 登录区域 - 仅未登录/     │
│   过期时显示)                  │
│                               │
│  [QR Code Image]              │
│  请使用网易云 APP 扫码         │
└─────────────────────────────┘
```

### 状态 → UI 映射

| sessionState | 显示 |
|---|---|
| `logged_in` | 头像 + 昵称 + VIP 状态 + 绿色"已登录"，按钮：刷新 / 登出 |
| `logged_out` | 灰色"未登录"，自动展开 QR 区域，按钮：登录 |
| `expired` | 橙色"登录已过期"，自动展开 QR 区域，按钮：重新登录 / 登出 |
| `logging_in` | "扫码中..." + statusMessage + QR 图片，按钮禁用 |

### VIP 状态显示

| vipType | 显示 | 颜色 |
|---|---|---|
| 0 | 免费用户 | textMuted |
| 11 | 黑胶VIP | `#e7515a`（网易红） |
| 其他非 0 | VIP | accent |

### 样式

沿用 `theme.ts` 配色，与 Spotify 面板风格一致。

## 6. 边界条件

| 场景 | 处理 |
|---|---|
| RefreshLogin 网络超时 | 返回 NetworkError，State 保持 LoggedIn，不触发 QR |
| 用户断网 | 同上，不登出不弹 QR，等网络恢复后下一首重试 |
| QR 登录中用户切歌 | HandleTrialAsync 的 context 被 cancel，新歌正常走 ResolveAsync |
| 连续多首 trial | 第一首触发恢复流程，后续歌曲检查 `State == LoggingIn` 时等待（不重复触发 QR） |
| 登出后正在播放的歌 | 不中断当前播放（已下载的流不受影响） |
| QR 超时无人扫码 | 复用 QRLoginManager 现有逻辑：90s 后自动刷新 QR |
| 启动时 cookie 已过期 | InitializeAsync 调 ValidateAndRefreshAsync → 失败 → State=Expired，UI 显示过期 |
| VipType=0 但歌曲非 trial | 正常播放，VIP 仅供显示 |
| 老用户升级 cookie 有效 | vipType=0 显示"免费用户"，一次 RefreshLogin 后更新真实值 |
| Go bridge 新版 + C# 旧版 | 旧版忽略 isTrial/vipType 新字段，行为等同旧版 |

## 7. 文件改动清单

| 层 | 文件 | 改动类型 |
|---|---|---|
| Go bridge | `netease_bridge/main.go` | 修改 |
| C# bridge | `ChillPatcher.Module.Netease/NeteaseBridge.cs` | 修改 |
| C# module | `ChillPatcher.Module.Netease/NeteaseModule.cs` | 修改 |
| C# 新文件 | `ChillPatcher.Module.Netease/NeteaseSessionManager.cs` | 新增 |
| C# 新文件 | `ChillPatcher.Module.Netease/NeteaseAccountApi.cs` | 新增 |
| UI 新文件 | `ui/window-manager/plugins/netease/index.tsx` | 新增 |

### 不改动的文件

| 文件 | 原因 |
|---|---|
| `QRLoginManager.cs` | 保持不变，被 SessionManager 调用 |
| `pcm_cache.go` / `pcm_stream.go` | 缓存/流无需改动 |
| `PluginConfig.cs` | 不新增配置项 |
| `SettingsPanel.tsx` / `ModulesPanel.tsx` | 不改现有面板 |

## 8. 日志规范

所有关键路径都有日志，统一 `[NeteaseSession]` 前缀，便于 grep 排查：

- **Info**：状态变迁（LoggedIn→Expired→LoggingIn→LoggedIn）、登录/登出操作、试听检测触发、HandleTrialAsync 结果
- **Debug**：RefreshLogin 结果详情、GetSongUrl 的 isTrial 值、QR 状态轮询、VipType 解析、AccountApi 属性更新
- **Warning**：RefreshLogin 失败原因（网络/认证）、连续 trial 检测、VipRestricted 跳过
