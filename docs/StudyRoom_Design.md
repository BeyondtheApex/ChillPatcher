# 自习室模式（Study Room）技术设计文档

## 1. 概述

自习室模式允许多名玩家联机同步，共享一个学习/工作环境。主机端（Host）正常运行游戏，所有行为事件通过 Steam P2P 实时广播给客户端（Client）。客户端禁用本地 AI，完全跟随主机的状态。

### 1.1 核心原则

- **主机权威**: 所有随机决策只在主机端发生，客户端无自主行为
- **高度抽象**: 同步协议传输标识符 (int/string)，不关心具体内容，官方新增内容自动兼容
- **最小侵入**: 尽量复用游戏现有系统（状态机、语音控制器、场景阅读器）
- **纯 C# 实现**: 不经过 JSApi 层，避免 Lock 状态影响

### 1.2 文件规划

```
Integration/StudyRoom/
  ├── StudyRoomService.cs        — 房间生命周期管理（创建/加入/离开）
  ├── SteamLobbyManager.cs       — Steam Lobby 创建/搜索/加入/元数据
  ├── P2PTransport.cs            — Steam Networking Messages 收发
  ├── SyncProtocol.cs            — 消息类型定义 + 序列化/反序列化
  ├── HostSyncManager.cs         — 主机端：事件捕获 + 状态快照 + 广播
  ├── ClientSyncManager.cs       — 客户端：接收 + 状态应用 + 服务锁定
  ├── InteractionLock.cs         — 点击互动独占锁（分布式互斥）
  ├── SaveDataSyncManager.cs     — 存档数据协同编辑（待办/日历/备忘录/习惯）
  └── StudyRoomConfig.cs         — 配置常量

JSApi/
  └── ChillStudyRoomApi.cs       — StudyRoom JSApi 层（OneJS UI 调用）

Patches/
  └── StudyRoomPatches.cs        — Harmony Patches（主机端事件捕获 + 客户端保存拦截）
```

---

## 2. 游戏事件系统分析

### 2.1 事件层级与同步策略

游戏的事件分为三个层级，同步策略各不相同：

```
层级1: AI决策层 (HeroineAI)
  ├── AutoActionChange() → 选择下一个行为
  ├── NextActionSelector → 随机选择工作/休息/野生动作
  └── 结果: ChangeState(ActionStateType)
  
  同步策略: ❌ 不同步决策过程，只同步决策结果 (StateChanged)

层级2: 场景驱动层 (ScenarioReader)
  ├── FacilityVoiceTextScenario → 触发对话场景
  ├── ScenarioReader → 逐行执行: 动画→字幕→语音→等待
  └── 结果: 一系列有时序的动画/语音/字幕指令
  
  同步策略: ✅ 同步场景触发点 (ScenarioType + EpisodeNumber)
            客户端本地执行 ScenarioReader，自动处理时序

层级3: 底层执行层
  ├── HeroineService.ChangeAnimation() → 播放动画
  ├── HeroineVoiceController.PlayVoice() → 播放语音+嘴巴
  ├── ScenarioTextMessage.StartText() → 显示字幕
  └── 结果: 视觉/音频表现
  
  同步策略: ⚠️ 仅在独立触发时同步（不通过场景触发的语音/字幕）
```

### 2.2 关键发现：为什么场景级同步能保证节奏一致

游戏的 ScenarioReader 内部已经实现了严格的时序控制：

```
CommandText 执行序列:
  1. ChangeAnimation(bodyMotion)      — 立即
  2. ChangeFacial(facialMotion)       — 立即  
  3. ChangeLookScale(...)             — 立即
  4. [可选延迟 Arg1 秒]
  5. StartText(subtitle)              — 字幕显示
  6. PlayVoice(voiceName)             — 语音开始
  7. WaitUntil(字幕完成 && 语音完成)   — 阻塞
  8. → 下一行对话
```

**这意味着**: 只要两端同时启动同一个 Scenario，ScenarioReader 会在本地自动保证动画→字幕→语音的正确时序。不需要我们逐帧同步底层指令。

### 2.3 同步点分类

| 事件源 | 触发方式 | 同步内容 | 客户端处理 |
|-------|---------|---------|-----------|
| 状态切换 | `HeroineAI.ChangeState()` | `ActionStateType (string)` | `CharacterApiService.setState()` → 动画自动跟随 |
| 番茄钟事件 | `PomodoroService` 各事件 | 事件类型 + 状态快照 | 直接操作 PomodoroService |
| 番茄钟语音场景 | `FacilityVoiceTextScenario.WantPlay()` | `ScenarioType + episodeNumber` | 本地触发同一场景 → 时序自动一致 |
| 自言自语 | `HeroineSelfTalkController` | `ScenarioType + episodeNumber` | 同上 |
| 点击互动 | `FacilityClickHeroine` | `ScenarioType + episodeNumber` | 同上 |
| 独立语音 | `PlayVoice()` (非场景触发) | `voiceName + moveMouth` | `VoiceController.PlayVoice()` |
| 独立字幕 | 外部字幕显示 | `text + duration` | `SubtitleApiService.show()` |
| 主线/支线故事 | `WantTalk` → 故事开始 | `ScenarioType + episodeNumber` | 等待确认后同步启动 |

---

## 3. 网络节奏同步方案

### 3.1 核心问题：网络延迟下如何保持节奏一致？

本地运行时，语音、动画、字幕的时序由 `ScenarioReader` 保证。网络同步的挑战在于：
- P2P 消息有 30-150ms 延迟
- 各客户端收到消息的时间不一致

### 3.2 解决方案：场景级同步 + 本地播放

**核心思路**: 不同步每一帧的底层指令，而是同步"启动场景"这个高层事件，让客户端本地的 ScenarioReader 自己驱动时序。

```
主机端:                              客户端:
PomodoroService.OnWorkStart()        
  ↓                                  
HeroineAI.StartWork()                
  ↓ ChangeState(WorkPC)              
  ↓                                  收到 StateChanged{WorkPC}
                                       ↓ ai.DebugChangeState(WorkPC)
                                       ↓ 动画自动播放 ✅ (本地状态机驱动)

FacilityVoiceTextScenario            
  .WantPlay(PomodoroWork, ep=3)      
  ↓                                  收到 ScenarioPlay{PomodoroWork, 3}
                                       ↓ 本地触发同一场景
                                       ↓ ScenarioReader 本地执行
                                       ↓ 动画→字幕→语音 时序自动一致 ✅
```

**为什么这能工作**:
1. 状态切换消息 < 1KB，传输快
2. 客户端本地有完整的场景数据（对话文本、语音文件、动画数据）
3. ScenarioReader 是确定性的——相同参数产生相同序列
4. 唯一的延迟是消息到达时间，约 50-150ms，人眼不可察觉

### 3.3 番茄钟同步时序

番茄钟需要更精确的同步，因为它控制 UI 上的倒计时显示。

```
方案: 事件驱动 + 定期快照修正

主机端每秒发送: PomodoroSnapshot {
  isRunning, isPaused, isWorking,
  currentPhaseElapsedSeconds,     // 当前阶段已过秒数
  totalWorkSeconds,
  workMinutes, breakMinutes,
  loopCurrent, loopTotal,
  serverTimestampMs               // 主机时间戳
}

客户端收到后:
  1. 直接设置本地 PomodoroData / PlayerData 的对应值
  2. 不启动本地计时器，完全由快照驱动
  3. 延迟补偿: 若 isRunning 且 RTT 有效 (0 < rttMs < 5000)，
     将 RTT/2 的秒数补偿到 currentWorkSeconds 和 totalWorkSeconds
```

**关键决策**: 客户端 **不运行** 本地番茄钟计时器。每秒的快照就是 UI 的数据源。这样：
- 避免两端累积误差
- 暂停/继续瞬间一致
- 断线后倒计时自动冻结

### 3.4 断线降级策略

```
检测断线: 3秒无 Heartbeat → 标记断线

断线后客户端行为:
  1. 保持当前状态不变（工作中就继续工作动画，休息就继续休息动画）
  2. HeroineAI._isUse 保持 false（不启用本地AI随机事件）
  3. 番茄钟 UI 显示"连接中断"，倒计时冻结
  4. 子存档保持不变（可用于重连后恢复）

重连流程:
  1. 发送 ReconnectRequest{lastSeqNumber, attempt}
  2. 主机尝试增量重放 (seq > lastSeqNumber)，若无法增量则回复 FullSnapshot
  3. 客户端应用快照/重放消息，继续同步
  4. 若客户端已不在 Lobby 中，StudyRoomService 会先尝试
     JoinLobby(_reconnectLobbyId) 恢复 Lobby 成员关系，
     然后继续发送 ReconnectRequest

超时退出: 60秒无法重连 → 提示用户 → 退出自习室 → 切回主存档
```

---

## 4. 点击互动独占锁

### 4.1 问题

多人同时点击角色会导致冲突——游戏的 `FacilityClickHeroine` 有内置锁（`_mainState != Idle` 时拒绝新点击），但这只是本地锁。

### 4.2 分布式互斥方案

```
任何人点击角色:
  1. 发送 InteractionRequest{requestId, playerId, type="ClickHeroine"}
  2. 主机端 InteractionLock 检查:
     - 当前无人占用 → 授权，广播 InteractionGranted{requestId, playerId}
     - 已被占用 → 拒绝，回复 InteractionDenied{requestId, reason}
  3. 授权的玩家:
     - 主机: 直接执行点击反应（选择对话场景）
     - 客户端: 等待主机选择的场景 → 广播 ScenarioPlay
  4. 场景播放完成 → 主机广播 InteractionRelease{type}

客户端本地 UI 行为:
  - 发送请求后: 显示等待指示（可选）
  - 收到授权: 无特殊处理，等待场景广播
  - 收到拒绝: 显示"其他人正在互动"提示
```

### 4.3 实际执行流程

```
玩家A(客户端)点击角色:
  → A 发送 InteractionRequest 到主机
  → 主机检查锁：空闲
  → 主机广播 InteractionGranted{playerA}
  → 主机执行点击反应: 
      FacilityClickHeroine.ReactionReady() 
      → ReactionTalkSelector.GetNextNormalClickShortTalk()
      → 得到 ScenarioType + episodeNumber
  → 主机广播 ScenarioPlay{type, episode}
  → 所有客户端（包括A）本地触发 ScenarioReader
  → 场景结束 → 主机广播 InteractionRelease

期间如果玩家B也点击:
  → B 发送 InteractionRequest
  → 主机检查锁：被A占用
  → 主机回复 InteractionDenied 给 B
```

### 4.4 主机本人点击

主机点击直接本地执行，同时广播：
```
主机点击 → 本地获取锁(肯定成功) → 执行反应 → 广播结果
```

---

## 5. 故事同步（主线章节 MainScenario）

### 5.1 故事系统说明

游戏的"故事"是**主线章节（MainScenario）**，通过玩家等级解锁：

```
ScenarioProgressData:
  FinishReadMainEpisodeNumber  — 已完成章节数
  NextEpisodeNumber            — 下一待解锁章节号
  NextEpisodeUnlockLevel       — 该章节的等级要求
  PlayedScenarioGroupIDs       — 已看过的全部剧情ID

解锁条件: 当前等级 >= NextEpisodeUnlockLevel
触发: HeroineAI → IsNeedChangeWantTalk() → WantTalk状态 → 玩家点击 → 开始故事
```

### 5.2 同步方案：全员等待 + 超时启动

```
主机端检测到 WantTalk:
  1. 广播 StoryReady{scenarioType, episodeNumber}
  2. 所有客户端收到后显示提示："故事即将开始，按任意键准备"
  3. 每个客户端发送 StoryAck{playerId}
  4. 主机收集 Ack:
     - 全部收到 → 广播 StoryStart
     - 超时 5秒 → 强制广播 StoryStart（不等了）
  5. 所有端收到 StoryStart → 本地启动 ScenarioReader
  6. 故事由 ScenarioReader 本地驱动（确定性播放）
  7. 故事结束 → 正常恢复

特殊处理:
  - 如果客户端没有对应的故事数据（版本不同）→ 跳过，保持当前状态
  - 故事进度只保存在自习室子存档中，不影响主存档
```

---

## 6. 密码验证：挑战-应答机制

### 6.1 为什么不能明文传递密码

P2P 消息可能被中间人截获。即使 Steam SDR 加密了传输，仍应避免明文。

### 6.2 HMAC 挑战-应答流程

```
客户端请求加入:
  → JoinRequest{steamId}

主机生成挑战:
  challenge = RandomBytes(32)
  → Challenge{challenge}

客户端计算应答:
  response = HMAC-SHA256(key=password, message=challenge)
  → ChallengeResponse{response}

主机验证:
  expected = HMAC-SHA256(key=storedPassword, message=challenge)
  if (response == expected) → JoinAccepted
  else → JoinRejected{reason="密码错误"}
```

**安全性**: 
- 密码不传输，只传 HMAC 摘要
- challenge 每次不同，防止重放攻击
- 即使截获 response，无法反推密码

---

## 7. 子存档与环境同步

### 7.1 自习室存档生命周期

```
创建自习室:
  主机选择:
    a) 继承主存档: createProfile("_sr_{roomId}", inheritFrom: ["*"])
    b) 空白存档:   createProfile("_sr_{roomId}")
    c) 选择性继承: createProfile("_sr_{roomId}", inheritFrom: [指定文件列表])
  → switchProfile → 场景重载 → 开始接受连接

客户端加入:
  1. 主机发送完整存档数据（ES3序列化的全部文件）
  2. 客户端创建子存档: createProfile("_sr_c_{roomId}")
  3. 写入收到的存档文件
  4. switchProfile → 场景重载
  5. 重载完成 → SyncReady → 开始接收同步

退出自习室:
  1. switchProfile(null) → 回到主存档 → 场景重载
  2. deleteProfile("_sr_...") → 删除临时存档
```

### 7.2 环境状态快照

加入时发送一次完整快照，之后增量同步：

```csharp
FullSnapshot {
    // 角色状态
    ActionStateType currentState;       // int
    UpdateStateType updateState;        // int
    
    // 番茄钟
    PomodoroType type;
    bool isRunning, isPaused;
    float currentPhaseElapsed;
    int workMinutes, breakMinutes;
    int loopCurrent, loopTotal;
    double currentWorkSeconds, totalWorkSeconds;
    
    // 环境 (详见 §8.7.2)
    Dictionary<string, bool> windowViews;         // WindowViewType → IsActive
    Dictionary<string, float> soundVolumes;       // AmbientSoundType → Volume
    Dictionary<string, bool> soundMutes;          // AmbientSoundType → IsMute
    bool autoTimeEnabled;
    float timeDayStart, timeSunsetStart, timeNightStart;
    int envPresetIndex;
    
    // 装饰 (详见 §8.7.1)
    Dictionary<string, bool> decorations;         // DecorationSkinType → IsActive
    int decPresetIndex;
    
    // 模式 (详见 §8.7.3)
    string currentMode;                           // CollaborationType
    
    // 经济 (详见 §8.8)
    int point;
    string[] purchasedEnvironments;
    string[] purchasedDecorations;
    string[] purchasedAmbientSounds;
    
    // 经验/等级
    int level;
    float exp;
    
    // 服务器时间戳
    long serverTimestampMs;
}
```

---

## 8. 存档数据协同编辑

### 8.1 问题分析

游戏有 4 大用户数据系统：**待办事项清单**、**日历/日记**、**备忘录**、**习惯追踪**，均存储在本地 ES3 存档中。自习室中需要：
- 所有人看到相同的待办事项/日记/备忘录/习惯打卡状态
- 任何人都能创建/编辑/完成条目
- 编辑不冲突

**为什么不改成网络请求**: 游戏内部直接读写 `SaveDataManager` 内存对象，改为网络请求等于重写整个存档系统，风险极高。

### 8.2 方案：主机权威 + 操作广播 + 乐观锁

```
架构:
  主机端: SaveDataManager 是唯一真实数据源
  客户端: 本地 SaveDataManager 是镜像副本

编辑流程 (以TODO为例):
  客户端A编辑一个条目:
    1. 发送 SaveDataOp{type:TodoUpdate, itemId:123, data:{text:"新内容"}}
    2. 本地 UI 乐观更新 (立即显示新内容)
    3. 主机收到 → 检查版本 → 应用到本地 SaveDataManager → 保存
    4. 主机广播 SaveDataChanged{type:TodoUpdate, itemId:123, data:{...}, version:N}
    5. 所有客户端(包括A) 收到广播 → 更新本地数据

  冲突处理 (Last-Write-Wins):
    如果A和B同时编辑同一条目:
    → 主机按收到的顺序处理，后到的覆盖先到的
    → 广播最终结果给所有人
    → 各端保持一致

  编辑指示:
    客户端开始编辑时: 发送 EditingStart{type:Todo, itemId:123, playerId}
    主机广播给其他人 → 其他人的UI显示"玩家A正在编辑此条目"
    编辑完成/取消: 发送 EditingEnd{type:Todo, itemId:123}
```

### 8.3 需要同步的存档数据类型

游戏共有 4 大用户数据系统 + 故事进度：

#### 8.3.1 待办事项清单 (Todo)

| 数据类型 | 结构 | 操作 |
|---------|------|------|
| **TodoAllData** | Dict\<listId, TodoListData\> | 创建/删除/重命名列表 |
| **TodoListData** | UniqueID, TitleText, TodoDic, TodoOrderList | 添加/删除/排序条目 |
| **TodoData** | UniqueID, TodoText, CurrentState(Working/Complete), completeDateTimeString, expireDateTimeString | 编辑文本/完成/取消完成 |

- 每个 TodoList 独立保存 (ES3 Key: `"{listId}"`)
- TodoOrderList 控制显示顺序，需同步排序操作
- 完成 Todo 会同时更新 CalendarDateData.CompleteTodoListDic

#### 8.3.2 日历与日记 (Calendar / Diary)

| 数据类型 | 结构 | 操作 |
|---------|------|------|
| **CalendarData** | 缓存最多 10 个月的 CalenderMonthlyData | — |
| **CalenderMonthlyData** | Year, Month, DiaryList: Dict\<day, CalendarDateData\> | — |
| **CalendarDateData** | WorkTimeSeconds(double), CompleteTodoListDic, DiaryText(string) | 编辑日记文本、番茄钟更新学习时长 |

- 按月份保存 (ES3 Key: `"{year:D4}{month:D2}"`)
- DiaryText = 日记内容 (点击日期编辑)
- WorkTimeSeconds = 当天累计学习秒数 (番茄钟自动更新)
- CompleteTodoListDic = 当日完成的 Todo 关联 (由 Todo 完成操作自动更新)

#### 8.3.3 备忘录 (Note/Memo)

| 数据类型 | 结构 | 操作 |
|---------|------|------|
| **NoteDataV2** | NoteList(标题+排序), pageCache(最多缓存3页) | — |
| **NoteList** | Titles: Dict\<pageId, string\>, PageOrderList: List\<ulong\> | 创建/删除/重命名/排序页面 |
| **PageDataV2** | UniqueID(ulong), MainText(string) | 编辑备忘录内容 |

- NoteList 保存所有页面的标题和顺序 (一个 ES3 文件)
- 每个 PageDataV2 独立保存 (ES3 Key: `"{pageId}"`)
- 游戏内缓存最多 3 个页面，其余按需从磁盘加载

#### 8.3.4 习惯追踪 (Habit Tracker)

| 数据类型 | 结构 | 操作 |
|---------|------|------|
| **HabitAllHeaderData** | List\<HabitHeaderData\>, List\<DeletedHabitHeaderData\> | — |
| **HabitHeaderData** | HabitID(GUID string), CreatedDate, Title, DayOfWeekBitFlag(byte) | 创建/删除/编辑习惯 |
| **HabitAllMonthlyData** | Dict\<habitId, HabitDateSpanData\> 每月一份 | — |
| **HabitDateSpanData** | Dict\<dayOfMonth, HabitDateData\> | — |
| **HabitDateData** | Completed(bool), HideOnCalendar(bool) | 打卡/取消打卡 |
| **HabitAllDeadPeriodData** | Dict\<habitId, List\<DatePeriodData\>\> | — |
| **DatePeriodData** | StartDate, EndDate? | 暂停/恢复习惯 |

- HabitHeaders 包含所有习惯的元数据 (一个 ES3 文件)
- DayOfWeekBitFlag: 位掩码标记启用的星期几 (0xFF = 每天)
- MonthlyData 按月保存 (ES3 Key: `"{year:D4}{month:D2}"`)
- DeadPeriods 记录习惯暂停区间 (EndDate 为 null 表示仍在暂停中)
- 使用 ThrottledSave 防止频繁写盘

#### 8.3.5 故事进度 (Story Progress)

| 数据类型 | 结构 | 操作 |
|---------|------|------|
| **ScenarioProgressData** | FinishReadMainEpisodeNumber, NextEpisodeNumber, NextEpisodeUnlockLevel | 主机驱动，只读同步 |

### 8.4 操作消息设计

```csharp
// 存档操作类型
enum SaveDataOpType : byte
{
    // === 待办事项 (1-19) ===
    TodoListCreate    = 1,   // {listName}
    TodoListDelete    = 2,   // {listId}
    TodoListRename    = 3,   // {listId, newName}
    TodoItemAdd       = 10,  // {listId, text, expireDate?}
    TodoItemDelete    = 11,  // {listId, itemId}
    TodoItemUpdate    = 12,  // {listId, itemId, text?, state?, completeDate?}
    TodoItemReorder   = 13,  // {listId, itemId, newIndex}
    
    // === 日历/日记 (20-29) ===
    CalendarDiaryEdit = 20,  // {year, month, day, diaryText}
    CalendarWorkTime  = 21,  // {year, month, day, workTimeSeconds} — 番茄钟自动更新
    
    // === 备忘录 (30-39) ===
    NotePageCreate    = 30,  // {title}
    NotePageDelete    = 31,  // {pageId}
    NotePageUpdate    = 32,  // {pageId, mainText}
    NotePageRename    = 33,  // {pageId, newTitle}
    NotePageReorder   = 34,  // {pageId, newIndex}
    
    // === 习惯追踪 (40-49) ===
    HabitCreate       = 40,  // {title, dayOfWeekBitFlag}
    HabitDelete       = 41,  // {habitId}
    HabitUpdate       = 42,  // {habitId, title?, dayOfWeekBitFlag?}
    HabitToggleDay    = 43,  // {habitId, year, month, day, completed} — 打卡/取消
    HabitHideOnCal    = 44,  // {habitId, year, month, day, hide} — 日历显示开关
    HabitPause        = 45,  // {habitId, startDate} — 开始暂停
    HabitResume       = 46,  // {habitId, endDate} — 结束暂停
}
```

### 8.5 Harmony Patches 捕获存档写入

需要 Patch 的方法清单 (均在 `SaveDataManager` 上):

| 数据系统 | Save 方法 | 说明 |
|---------|-----------|------|
| 待办事项 | `SaveTodoList(TodoListData)` | 每个列表独立保存 |
| 日历/日记 | `SaveCalenderData(CalenderMonthlyData)` | 按月保存 |
| 备忘录 | `SaveNoteList()` | 标题和排序 |
| 备忘录 | `SavePageData(PageDataV2)` | 单页内容 |
| 习惯追踪 | `SaveHabitHeaders()` | 所有习惯元数据 |
| 习惯追踪 | `SaveHabitsMonthlyData(int, int)` | 每月打卡记录 |
| 习惯追踪 | `SaveHabitDeadPeriods()` | 暂停区间 |

```csharp
// 模式示例: 所有 Save 方法使用相同的 Prefix/Postfix 模式

// 主机端 Postfix: 捕获本地变更 → 广播给客户端
[HarmonyPostfix, HarmonyPatch(typeof(SaveDataManager), "SaveTodoList")]
static void OnTodoSaved(TodoListData todoListData)
{
    if (!StudyRoomService.IsHost) return;
    SaveDataSyncManager.BroadcastTodoChange(todoListData);
}

// 客户端 Prefix: 拦截本地保存 → 转发操作给主机
[HarmonyPrefix, HarmonyPatch(typeof(SaveDataManager), "SaveTodoList")]  
static bool OnClientTodoSave(TodoListData todoListData)
{
    if (!StudyRoomService.IsClient) return true; // 非自习室正常保存
    SaveDataSyncManager.SendTodoUpdateToHost(todoListData);
    return false; // 阻止本地写盘
}

// 习惯追踪也遵循相同模式:
[HarmonyPrefix, HarmonyPatch(typeof(SaveDataManager), "SaveHabitHeaders")]
static bool OnClientHabitHeadersSave() { ... }

[HarmonyPrefix, HarmonyPatch(typeof(SaveDataManager), "SaveHabitsMonthlyData")]
static bool OnClientHabitMonthlySave(int year, int month) { ... }

[HarmonyPrefix, HarmonyPatch(typeof(SaveDataManager), "SaveHabitDeadPeriods")]
static bool OnClientHabitDeadPeriodsSave() { ... }
```

**注意**:
- 客户端 Prefix 返回 `false` 阻止本地直接写盘，确保只有主机广播的数据才被应用
- 内存中的数据可以乐观更新用于 UI 显示
- 习惯追踪的 ThrottledSave 可能延迟触发，但 Patch 在实际 Save 方法上，仍然能正确捕获

### 8.6 初始同步：完整存档传输

```
客户端加入时:
  1. 主机读取当前子存档目录下的所有 ES3 文件
  2. 打包为 byte[] (ZIP 压缩)
  3. 分片发送 (每片 < 512KB，Steam P2P 单消息限制)
  4. 客户端收齐后解压 → 写入子存档目录
  5. switchProfile → 场景重载 → 数据加载

后续增量:
  - 所有变更通过操作消息增量同步
  - 不再传输完整存档
```

### 8.7 房间外观与环境同步

与 §8.1-8.6 的协同编辑不同，装饰/环境/模式是**主机单方面控制**的房间状态，客户端只接收和应用。

#### 8.7.1 装饰系统 (Decoration)

**数据结构:**
- `DecorationsData`: Dict\<DecorationSkinType, DecorationData\>，每项有 IsActive 标记
- `DecorationPresetsData`: 5 个预设位置 + SelectedIndex
- 分类: Cup / Headphone / Glasses / BookLayout / Book / Keyboard / StandLight / Desk / Chair / CoffeeMaker / Badge
- 同类别互斥: 同一分类只能激活一个皮肤

**同步方式 (全量快照):**
```
主机切换装饰/加载预设:
  1. SaveDecorationThrottled() 触发
  2. Harmony Postfix 捕获 → 广播当前完整 DecorationSaveData 快照
  3. 用 DecorationChange 消息类型传输，payload 包含全量 decorationData
  4. 客户端收到 → PopulateObject 覆写本地 DecorationSaveData → reloadFromSave()

Harmony Patches:
  - [Postfix] SaveDataManager.SaveDecorationThrottled → 主机广播全量快照
  - [Prefix] 客户端: 阻止本地写盘 (return false)
```

#### 8.7.2 环境系统 (Environment)

**数据结构:**
- `EnviromentData`: WindowViewDic + AmbientSoundDic
  - WindowViewDic: Dict\<WindowViewType, WindowViewData\> (42种窗景，IsActive)
  - AmbientSoundDic: Dict\<AmbientSoundType, AmbientSoundData\> (29种环境音，SoundVolume + IsMuteAmbient)
- `EnviromentPresetsData`: 5 个预设 + SelectedIndex
- `AutoTimeWindowChangeData`: IsActiveAuto, TimeDayStart, TimeSunsetStart, TimeNightStart

**同步方式 (全量快照):**
```
主机任何环境变更 (窗景/环境音/预设/自动时间):
  1. SaveEnviromentThrottled() / SaveAutoTimeWindowChangeData() 触发
  2. Harmony Postfix 捕获 → 广播当前完整 EnviromentData / AutoTimeData 快照
  3. 用 EnvironmentViewChange/EnvironmentAutoTime 等消息类型传输，payload 包含全量数据
  4. 客户端收到 → PopulateObject 覆写本地 EnviromentData → reloadFromSave()

客户端:
  - 服务已锁定，阻止客户端 UI 操作环境设置

Harmony Patches:
  - [Postfix] SaveDataManager.SaveEnviromentThrottled → 主机广播全量环境快照
  - [Postfix] SaveDataManager.SaveAutoTimeWindowChangeData → 主机广播自动时间快照
  - [Prefix] 客户端: 阻止本地写盘 (return false)
```

#### 8.7.3 模式系统 (Collaboration Mode)

**数据结构:**
- `CollaborationSaveData`:
  - CurrentType: CollaborationType (None / AlterEgo / BearsRestaurant / Valentine2026 / LunaNewYear2026 / NearSpring2026)
  - 每种模式有独立的 SaveData (如 AlterEgoSaveData: 含 NextEpisodeNumber, LevelData)

**同步方式:**
```
主机切换模式:
  1. 广播 ModeChange{mode: CollaborationType, modeData: {...}}
  2. 客户端收到 → switchProfile 重载场景以适应新模式
  → 或: 如果模式变更不需要场景重载，仅更新 CollaborationSaveData

注意:
  - 模式切换可能改变角色外观、可用装饰、环境等，需场景重载
  - 模式内的独立 LevelData/StoryProgress 由主机权威管理
  - 时间限制活动(如Valentine2026)的有效期检查由主机执行
```

### 8.8 经济系统同步

#### 8.8.1 数据结构

```csharp
// PointPurchaseDataV3
int Point;                           // 当前可用点数
List<string> PurchasedEnvironments;  // 已购买窗景列表
List<string> PurchasedDecorations;   // 已购买装饰列表
List<string> PurchasedAmbientSounds; // 已购买环境音列表

// PlayerDataV3.LevelData
int CurrentLevel;
float CurrentExp;
float NextLevelNecessaryExp;
double CurrentWorkSeconds;
double PomodoroTotalWorkSeconds;
```

#### 8.8.2 同步策略

```
经济操作: 仅主机可执行购买

购买流程:
  1. 主机在商店购买装饰/环境/环境音
  2. SavePointPurchaseData() 被 Postfix 捕获
  3. 广播 PointPurchaseSync{point, newItemType, newItemId}
  4. 客户端更新本地 PointPurchaseData 镜像
  → 装饰/环境实际激活通过 §8.7 的 DecorationChange/EnvironmentViewChange 单独广播

经验/等级:
  - 番茄钟完成 → 主机分配经验 → ExpChanged/LevelChanged 广播
  - 已在 §9.2 定义: ExpChanged=40, LevelChanged=41, WorkSecondsSync=42

客户端限制:
  - 客户端 UI 中隐藏或禁用购买按钮
  - 或: 客户端发送购买请求 → 主机判断余额 → 执行/拒绝

Harmony Patches:
  - [Postfix] SavePointPurchaseData → 主机广播购买数据变更
  - [Prefix] 客户端: 阻止本地购买(return false)
```

---

## 9. 消息协议

### 9.1 消息格式

```
[1 byte: MessageType] [4 bytes: PayloadLength LE] [N bytes: JSON Payload]
```

使用 JSON 而非二进制序列化，因为：
- 消息频率低（最多每秒几条），性能不是瓶颈
- 可读性好，便于调试
- 字段可扩展，向前兼容

### 9.2 消息类型枚举

```csharp
enum SyncMessageType : byte
{
    // === 握手 (1-9) ===
    JoinRequest       = 1,   // {steamId}
    Challenge         = 2,   // {challenge: base64}
    ChallengeResponse = 3,   // {response: base64}
    JoinAccepted      = 4,   // {profileData: ...}
    JoinRejected      = 5,   // {reason: string}
    SyncReady         = 6,   // {}
    ReconnectRequest  = 7,   // {lastSeqNumber}
    
    // === 状态快照 (10-19) ===
    FullSnapshot      = 10,  // {完整状态JSON}
    
    // === 角色同步 (20-29) ===
    StateChanged      = 20,  // {state: int}
    ScenarioPlay      = 21,  // {scenarioType: string, episode: int}
    VoicePlay         = 22,  // {voice: string, moveMouth: bool}
    VoiceCancel       = 23,  // {}
    SubtitleShow      = 24,  // {text: string, duration: float}
    SubtitleHide      = 25,  // {}
    
    // === 番茄钟 (30-39) ===
    PomodoroSnapshot  = 30,  // {完整番茄钟状态}
    PomodoroEvent     = 31,  // {event: string} start/pause/skip/reset/complete
    
    // === 进度 (40-49) ===
    ExpChanged        = 40,  // {exp: float}
    LevelChanged      = 41,  // {level: int}
    WorkSecondsSync   = 42,  // {current: double, total: double}
    
    // === 环境/装饰/模式 (50-59) ===
    EnvironmentViewChange  = 50,  // {windowViewType, isActive}
    EnvironmentSoundChange = 51,  // {ambientSoundType, volume?, isMute?}
    EnvironmentPresetLoad  = 52,  // {presetIndex}
    EnvironmentAutoTime    = 53,  // {isActive, dayStart, sunsetStart, nightStart}
    DecorationChange       = 54,  // {skinType, isActive, category}
    DecorationPresetLoad   = 55,  // {presetIndex}
    ModeChange             = 56,  // {mode: CollaborationType, modeData}
    PointPurchaseSync      = 57,  // {point, purchasedEnvs[], purchasedDecs[], purchasedSounds[]}
    
    // === 互动 (60-69) ===
    InteractionReq    = 60,  // {requestId, type}
    InteractionGrant  = 61,  // {requestId, playerId}
    InteractionDeny   = 62,  // {requestId, reason}
    InteractionEnd    = 63,  // {type}
    
    // === 故事 (70-79) ===
    StoryReady        = 70,  // {scenarioType, episode}
    StoryAck          = 71,  // {playerId}
    StoryStart        = 72,  // {}
    StorySkip         = 73,  // {playerId} — 某人跳过/无法播放
    
    // === 存档协同 (80-89) ===
    SaveDataOp        = 80,  // {opType, data} — 客户端→主机的操作请求
    SaveDataChanged   = 81,  // {opType, data, version} — 主机→全体的变更广播
    SaveDataFull      = 82,  // {chunkIndex, totalChunks, data} — 完整存档分片传输
    EditingStart      = 83,  // {dataType, itemId, playerId} — 开始编辑指示
    EditingEnd        = 84,  // {dataType, itemId, playerId} — 结束编辑指示
    
    // === 预留: 语音聊天 (100-109) ===
    VoiceChatData     = 100, // {pcmData: byte[]}
    
    // === 预留: 音乐广播 (110-119) ===
    MusicSync         = 110, // {action, songId, position}
    
    // === 控制 (200-209) ===
    Heartbeat         = 200, // {timestampMs}
    Kick              = 201, // {reason}
    RoomClosed        = 202, // {}
    PlayerJoined      = 203, // {steamId, personaName}
    PlayerLeft        = 204, // {steamId}
    
    // === 序列号包装 (250) ===
    SeqWrapper        = 250, // {seq: uint, inner: Message}
}
```

### 9.3 可靠性分层

```
可靠传输 (k_nSteamNetworkingSend_Reliable):
  - 握手消息 (1-7)
  - 快照 (10)
  - 场景/故事触发 (21, 70-73)
  - 互动锁 (60-63)
  - 控制消息 (200-204)

不可靠传输 (k_nSteamNetworkingSend_Unreliable):
  - 番茄钟快照 (30) — 每秒发送，丢包无影响
  - 心跳 (200)
```

---

## 10. Harmony Patches 设计

### 10.1 主机端捕获 Patches

只在主机模式激活，客户端不需要这些 Patch。

```csharp
// Patch 1: 角色状态变化
[HarmonyPostfix]
[HarmonyPatch(typeof(HeroineAI), "ChangeState", 
    new Type[] { typeof(HeroineAI.ActionStateType) })]
static void OnStateChanged(HeroineAI.ActionStateType nextAction)
{
    if (!StudyRoomService.IsHost) return;
    HostSyncManager.BroadcastStateChanged(nextAction.ToString());
}

// Patch 2: 场景对话触发
[HarmonyPostfix]
[HarmonyPatch(typeof(FacilityVoiceTextScenario), 
    "WantPlayVoiceTextScenario")]
static void OnScenarioPlay(ScenarioType scenarioType, int episodeNumber)
{
    if (!StudyRoomService.IsHost) return;
    HostSyncManager.BroadcastScenarioPlay(scenarioType.ToString(), episodeNumber);
}

// Patch 3: 独立语音播放（非场景触发的语音）
[HarmonyPostfix]
[HarmonyPatch(typeof(HeroineVoiceController), "PlayVoice")]
static void OnVoicePlay(string voice, bool isMoveMouse, bool isStory)
{
    if (!StudyRoomService.IsHost) return;
    if (isStory) return; // 故事语音由 ScenarioPlay 同步，不重复发送
    HostSyncManager.BroadcastVoicePlay(voice, isMoveMouse);
}
```

**为什么只需要3个 Patch**:
1. `ChangeState` 覆盖所有角色行为变化（工作/休息/野生动作/离席）
2. `WantPlayVoiceTextScenario` 覆盖所有场景对话（番茄钟语音、自言自语、点击反应）
3. `PlayVoice` 覆盖非场景触发的独立语音

官方新增行为/语音/场景 → 自动被这3个 Patch 捕获 → **零维护成本**

### 10.2 客户端禁用 Patch

```csharp
// 客户端加入自习室时:
HeroineAI ai = FindObjectOfType<HeroineAI>();
ai.SetIsUse(false);  // 禁用全部AI自主决策

// 可选: 也禁用本地番茄钟自动触发
// PomodoroService 通过反射设置为暂停/不自动启动
```

### 10.3 存档协同 Patches

主机端捕获本地存档变更并广播，客户端拦截本地保存并转发到主机。详见第 8.5 节。

---

## 11. Steam Lobby 设计

### 11.1 房间元数据

```csharp
// 设置 Lobby 元数据（可搜索的键值对）
SteamMatchmaking.SetLobbyData(lobbyId, "chill_studyroom", "1");
SteamMatchmaking.SetLobbyData(lobbyId, "room_name", roomName);
SteamMatchmaking.SetLobbyData(lobbyId, "host_name", SteamFriends.GetPersonaName());
SteamMatchmaking.SetLobbyData(lobbyId, "password_required", hasPassword ? "1" : "0");
SteamMatchmaking.SetLobbyData(lobbyId, "protocol_version", "1");
SteamMatchmaking.SetLobbyData(lobbyId, "member_count", memberCount.ToString());
SteamMatchmaking.SetLobbyData(lobbyId, "max_members", maxMembers.ToString());
SteamMatchmaking.SetLobbyData(lobbyId, "pomodoro_state", pomodoroState); // working/resting/idle
SteamMatchmaking.SetLobbyData(lobbyId, "mode", mode);                     // CollaborationType
```

### 11.2 房间列表搜索

```csharp
SteamMatchmaking.AddRequestLobbyListStringFilter(
    "chill_studyroom", "1", ELobbyComparison.k_ELobbyComparisonEqual);
SteamMatchmaking.AddRequestLobbyListDistanceFilter(
    ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
SteamMatchmaking.RequestLobbyList();
// → Callback: OnLobbyMatchList
```

### 11.3 人数限制

初始设定: **最多 8 人**（Steam Lobby 默认限制较高，但 P2P 广播开销与人数线性增长）

---

## 12. 完整生命周期时序图

### 12.1 创建自习室

```
Host                          Steam                       
──────────────────────────────────────────────────────────
StudyRoomService.CreateRoom()
  │
  ├─ SaveProfileService.createProfile("_sr_temp")
  ├─ SaveProfileService.switchProfile("_sr_temp")
  │   └─ 场景重载...
  │
  [场景重载完成]
  │
  ├─ SteamMatchmaking.CreateLobby(Public, maxMembers)
  │                                → LobbyCreated callback
  ├─ 设置 Lobby 元数据
  ├─ P2PTransport.StartListening()
  ├─ HostSyncManager.Start()
  │   ├─ 安装 Harmony Patches
  │   └─ 开始心跳广播
  └─ 状态 = Active
```

### 12.2 加入自习室

```
Client                        Host                        
──────────────────────────────────────────────────────────
SteamMatchmaking.JoinLobby()
                              ← LobbyEnter callback
P2P: JoinRequest{steamId}  →
                              验证 Steam 是否在 Lobby
                            ← Challenge{nonce}
P2P: ChallengeResponse{hmac}→
                              HMAC验证
                            ← JoinAccepted{snapshotData}

createProfile("_sr_client")
写入环境/装饰/设置数据
switchProfile("_sr_client")
  └─ 场景重载...

[场景重载完成]
P2P: SyncReady{} →
                              发送 FullSnapshot
                            ← FullSnapshot{完整状态}
应用快照:
  ai.SetIsUse(false)
  ai.DebugChangeState(state)
  设置番茄钟
  设置环境/装饰
  
开始接收增量同步 ✅
```

### 12.3 退出自习室

```
Client                        Host
──────────────────────────────────────────────────────────
StudyRoomService.LeaveRoom()
  │
  ├─ P2P: PlayerLeft →
  ├─ SteamMatchmaking.LeaveLobby()
  ├─ ai.SetIsUse(true)        — 恢复本地AI
  ├─ switchProfile(null)       — 回到主存档
  │   └─ 场景重载...
  └─ deleteProfile("_sr_client")

                              收到 PlayerLeft
                              更新成员列表
                              广播 PlayerLeft 给其他人
```

### 12.4 系统驱动机制

自习室系统的更新驱动依赖 `PlayerLoopInjector`，与 OneJS 引擎等现有系统使用相同的入口。

#### 12.4.1 更新链路

```
Unity PlayerLoop (PostLateUpdate)
  └─ PlayerLoopInjector.OnUpdate()     [每帧调用]
       ├─ BuildSetupOverlay.Tick()
       ├─ SteamReconnectManager.Tick()
       ├─ OneJSBridge.Tick()
       └─ StudyRoomService.Tick()       [新增] ◄── 自习室驱动入口
```

`PlayerLoopInjector.OnUpdate()` 需新增一行:
```csharp
private static void OnUpdate()
{
    BuildSetupOverlay.Tick();
    ChillPatcher.Patches.SteamReconnectManager.Tick();
    OneJSBridge.Tick();
    StudyRoomService.Tick();   // ← 新增
}
```

#### 12.4.2 StudyRoomService.Tick() 职责

```csharp
public static void Tick()
{
    if (!IsActive) return;  // 未开启自习室则立即返回
    
    // 1. 接收网络消息 (事件驱动的核心)
    P2PTransport.PollMessages();   // 读取 Steam P2P 队列 → 触发 OnMessageReceived 事件
    
    // 2. 周期性任务 (少量循环同步)
    _heartbeatTimer += Time.deltaTime;
    if (_heartbeatTimer >= HEARTBEAT_INTERVAL)  // 每 1 秒 (HeartbeatIntervalSeconds=1.0f)
    {
        _heartbeatTimer = 0;
        SendHeartbeat();
        CheckPeerTimeout();  // 检测断线
    }
    
    // 3. 主机端: 番茄钟快照 (每秒)
    if (IsHost)
    {
        _pomodoroTimer += Time.deltaTime;
        if (_pomodoroTimer >= POMODORO_SYNC_INTERVAL)  // 每 1 秒
        {
            _pomodoroTimer = 0;
            HostSyncManager.BroadcastPomodoroSnapshot();
        }
    }
}
```

#### 12.4.3 事件驱动 vs 循环同步

| 类型 | 驱动方式 | 触发源 | 频率 |
|------|---------|--------|------|
| 角色状态变化 | **事件驱动** | Harmony Postfix on `ChangeState` | 不定期 |
| 场景对话 | **事件驱动** | Harmony Postfix on `WantPlayVoiceTextScenario` | 不定期 |
| 独立语音 | **事件驱动** | Harmony Postfix on `PlayVoice` | 不定期 |
| 装饰/环境切换 | **事件驱动** | Harmony Postfix on Save方法 | 不定期 |
| 存档数据变更 | **事件驱动** | Harmony Prefix/Postfix on Save方法 | 不定期 |
| 番茄钟时间线 | **循环同步** | Tick() 定时器 | 1秒/次 |
| 心跳/断线检测 | **循环同步** | Tick() 定时器 | 2秒/次 |
| 网络消息轮询 | **循环同步** | Tick() → PollMessages | 每帧 |

**原则**: 90% 的同步由 Harmony Patch 事件驱动，只有时间相关的持续数据和网络IO需要帧循环。

#### 12.4.4 P2PTransport.PollMessages()

```csharp
// 每帧调用，从 Steam 消息队列中取出所有待处理消息
public static void PollMessages()
{
    // SteamNetworkingMessages.ReceiveMessagesOnChannel(channel, msgs, maxMsgs)
    int count = SteamNetworkingMessages.ReceiveMessagesOnChannel(
        CHANNEL_ID, _messageBuffer, MAX_MESSAGES_PER_POLL);
    
    for (int i = 0; i < count; i++)
    {
        var msg = _messageBuffer[i];
        var sender = msg.m_identityPeer;
        var data = ReadMessageData(msg);
        msg.Release();
        
        // 事件触发 → HostSyncManager 或 ClientSyncManager 处理
        OnMessageReceived?.Invoke(sender, data);
    }
}
```

**消息流**: Steam P2P 队列 → PollMessages() → OnMessageReceived 事件 → SyncProtocol.Deserialize() → Handler 分发

---

## 13. 客户端准备工作（受控端初始化）

客户端加入房间后、接收同步前，需完成一系列准备步骤来确保本地环境处于可控状态。

### 13.1 服务锁定

锁定所有 Integration 层服务，阻止游戏原生 UI **和** JS 端发起操作：

```csharp
static void LockAllServices()
{
    // 每个服务独立 Lock (服务级布尔值)
    CharacterApiService.Instance.Locked = true;   // 阻止状态切换
    DecorationApiService.Instance.Locked = true;   // 阻止装饰更换
    EnvironmentApiService.Instance.Locked = true;  // 阻止环境切换
    ModeApiService.Instance.Locked = true;         // 阻止模式切换
    SubtitleApiService.Instance.Locked = true;     // 阻止字幕播放
    VoiceApiService.Instance.Locked = true;        // 阻止语音播放
}
```

**Lock 行为说明**:
- **游戏 UI**: 按钮灰化/不响应，由游戏内部检查 Locked 状态实现
- **JSApi 层**: `ChillCharacterApi.setState()` 等方法检查 `locked` 后返回 false
- **C# 同步代码**: 绕过 JSApi 层，直接调用 Integration 层服务方法（不受 Lock 影响）

因此客户端同步管理器 `ClientSyncManager` 直接调用:
```csharp
// 同步代码不经过 Lock 检查，直接操作
CharacterApiService.Instance.setState(stateId);  // 总是生效
DecorationApiService.Instance.setDecoration(skinType, save: false);
```

### 13.2 AI 禁用

```csharp
HeroineAI ai = FindObjectOfType<HeroineAI>();
ai.SetIsUse(false);  // UpdateHeroineAI() 立即 return，不再执行任何随机行为
```

### 13.3 UI 元素隐藏

部分游戏 UI 在受控端没有意义（如番茄钟设置、环境切换）:

```csharp
// 隐藏/禁用的 UI 列表（后续补充具体清单）
static readonly string[] HiddenUIElements = {
    // TODO: 由用户确定具体需要隐藏的 UI 元素名称
    // 例: "PomodoroSettingsPanel", "EnvironmentPanel", "ShopPanel"
};
```

**实现方式**: 通过 Harmony Patch 拦截 UI 初始化，或通过 `GameObject.SetActive(false)` 直接隐藏。

### 13.4 初始同步流程

```
客户端准备序列:
  1. LockAllServices()           — 锁定所有服务 API
  2. ai.SetIsUse(false)          — 禁用 AI
  3. 安装客户端 Harmony Patches   — 拦截本地保存，转发到主机
  4. 发送 SyncReady 给主机
  5. 接收 FullSnapshot            — 应用角色状态、番茄钟、环境、装饰
  6. 接收 SaveDataFull            — 完整存档数据 (分片)
  7. 开始接收增量同步消息
```

### 13.5 退出清理

```csharp
static void UnlockAllServicesAndRestore()
{
    // 卸载客户端 Harmony Patches
    StudyRoomPatches.UnpatchClient();
    
    // 解锁所有服务
    CharacterApiService.Instance.Locked = false;
    DecorationApiService.Instance.Locked = false;
    EnvironmentApiService.Instance.Locked = false;
    ModeApiService.Instance.Locked = false;
    SubtitleApiService.Instance.Locked = false;
    VoiceApiService.Instance.Locked = false;
    
    // 恢复 AI
    HeroineAI ai = FindObjectOfType<HeroineAI>();
    ai.SetIsUse(true);
    
    // 恢复 UI 可见性
    // RestoreHiddenUI();
    
    // 切换回主存档
    SaveProfileApiService.Instance.switchProfile(null);
}
```

---

## 14. StudyRoom JSApi

自习室系统通过 OneJS UI 控制，需要暴露高层 JSApi 接口供 JS 端调用。

### 14.1 API 结构

```csharp
// 挂载到 chill.studyRoom
public class ChillStudyRoomApi : IDisposable
{
    // ─── 房间管理 ───
    public string createRoom(string options);   // {password?, maxMembers?, inheritSave?}
    public string joinRoom(string lobbyId, string password);
    public void leaveRoom();
    public void closeRoom();                    // 主机关闭房间
    
    // ─── 大厅浏览 ───
    public string getLobbyList();               // → JSON: [{lobbyId, hostName, memberCount, maxMembers, hasPassword, mode}]
    public string searchByInviteCode(string code); // → JSON: {lobbyId, hostName, ...} | null
    public void refreshLobbyList();             // 异步刷新，完成后触发 "lobbyListUpdated" 事件
    
    // ─── 房间状态 ───
    public string getRoomInfo();                // → JSON: {lobbyId, isHost, hostSteamId, inviteCode, password?}
    public string getMembers();                 // → JSON: [{steamId, personaName, isHost, syncState}]
    public string getMyInfo();                  // → JSON: {steamId, personaName, isHost}
    public bool isInRoom();
    public bool isHost();
    
    // ─── 成员信息 ───
    public string getMemberSyncState(string steamId); // → JSON: {connected, latencyMs, lastHeartbeat}
    
    // ─── 故事应答 ───
    public void ackStory();                     // 响应 StoryReady → 发送 StoryAck
    public void skipStory();                    // 跳过故事 → 发送 StorySkip
    
    // ─── 编辑状态 ───
    public string getEditingStatus();           // → JSON: [{dataType, itemId, playerId, personaName}]
    
    // ─── 事件订阅 ───
    public string on(string eventName, Action<string> handler);
    public bool off(string token);
}
```

### 14.2 事件列表

```javascript
// JS 端事件订阅
chill.studyRoom.on("eventName", (jsonStr) => {
    const event = JSON.parse(jsonStr);
    // ...
});
```

| 事件名 | 触发时机 | Payload |
|--------|---------|---------|
| **roomCreated** | 房间创建成功 | `{lobbyId, inviteCode}` |
| **roomJoined** | 成功加入房间 | `{lobbyId, hostName, isHost}` |
| **roomClosed** | 房间被关闭 | `{reason}` |
| **roomLeft** | 本地离开房间 | `{}` |
| **playerJoined** | 新玩家加入 | `{steamId, personaName}` |
| **playerLeft** | 玩家离开 | `{steamId, personaName}` |
| **syncReady** | 初始同步完成 | `{}` |
| **kicked** | 被踢出 | `{reason}` |
| **lobbyListUpdated** | 大厅列表刷新完成 | `{lobbies: [...]}` |
| **storyReady** | 主机准备播放故事 | `{scenarioType, episode}` |
| **storyStarted** | 所有人应答，故事开始 | `{}` |
| **storySkipped** | 有人跳过故事 | `{steamId, personaName}` |
| **editingStarted** | 有人开始编辑数据 | `{dataType, itemId, steamId, personaName}` |
| **editingEnded** | 编辑结束 | `{dataType, itemId, steamId}` |
| **joinFailed** | 加入房间失败 | `{reason}` (密码错误/房间满/不存在) |
| **connectionLost** | 与主机断线 | `{steamId}` |
| **reconnecting** | 正在重连 | `{attempt}` |
| **reconnected** | 重连成功 | `{}` |
| **memberSyncUpdate** | 成员同步状态变化 | `{steamId, latencyMs, connected}` |
| **pomodoroSync** | 番茄钟状态同步 | `{type, isRunning, elapsed, ...}` |

### 14.3 JS 端使用示例

```javascript
const sr = chill.studyRoom;

// 创建房间
const result = JSON.parse(sr.createRoom(JSON.stringify({
    password: "1234",
    maxMembers: 4,
    inheritSave: true
})));

// 浏览大厅
sr.on("lobbyListUpdated", (json) => {
    const { lobbies } = JSON.parse(json);
    lobbies.forEach(lobby => {
        console.log(`${lobby.hostName} (${lobby.memberCount}/${lobby.maxMembers})`);
    });
});
sr.refreshLobbyList();

// 加入房间
sr.joinRoom(lobbyId, "1234");

// 监听事件
sr.on("playerJoined", (json) => {
    const { personaName } = JSON.parse(json);
    showNotification(`${personaName} 加入了自习室`);
});

sr.on("storyReady", (json) => {
    const { scenarioType, episode } = JSON.parse(json);
    showStoryPrompt(`准备播放 ${scenarioType} #${episode}`, () => sr.ackStory());
});

sr.on("editingStarted", (json) => {
    const { dataType, itemId, personaName } = JSON.parse(json);
    highlightItem(dataType, itemId, `${personaName} 正在编辑`);
});

// 查看成员信息
const members = JSON.parse(sr.getMembers());
members.forEach(m => {
    console.log(`${m.personaName} host=${m.isHost} sync=${m.syncState}`);
});
```

### 14.4 注册方式

遵循现有 JSApi 架构，在 `ChillJSApi` 中添加属性:

```csharp
// ChillJSApi.cs 中新增
public ChillStudyRoomApi studyRoom { get; }

// 构造函数中初始化
studyRoom = new ChillStudyRoomApi(studyRoomService);
```

JS 端即可通过 `chill.studyRoom.*` 访问所有接口。

---

## 15. 未来扩展预留

### 15.1 语音聊天 (VoiceChatData = 100-109)

#### 15.1.1 Opus 编码分级

语音和音乐使用 Opus 编解码器，但编码参数不同：

| 用途 | 模式 | 采样率 | 声道 | 比特率 | 帧长 | 约带宽/人 |
|------|------|--------|------|--------|------|-----------|
| 语音聊天 | VOIP | 16kHz | Mono | 16-24 kbps | 20ms | ~2-3 KB/s |
| 音乐广播 | AUDIO | 48kHz | Stereo | 96-128 kbps | 20ms | ~12-16 KB/s |

Opus Native 入口已存在于 `NativePlugins/dr_libs/dr_opus.h`(仅解码)。编码需引入 `libopus` 或使用 `concentus` (C# 纯管理实现)。

#### 15.1.2 语音聊天驱动

```
麦克风采集 (Unity Microphone API, 已用于 AIChatBridge):
  1. Microphone.Start() → AudioClip (16kHz, Mono)
  2. 每 20ms (由 Tick 定时器驱动):
     - AudioClip.GetData() → float[] PCM 帧
     - Opus.Encode(pcm, VOIP mode) → byte[] opusFrame
     - P2P 发送 VoiceChatData{senderId, seqNum, opusFrame} (不可靠通道)

接收端:
  1. PollMessages() 收到 VoiceChatData
  2. Opus.Decode(opusFrame) → float[] PCM 帧
  3. Jitter Buffer (按 seqNum 排序，补偿丢包/乱序)
  4. 写入 AudioClip 环形缓冲区 → AudioSource 播放

StudyRoomService.Tick() 中的驱动:
  _voiceCaptureTimer += Time.deltaTime;
  if (_voiceCaptureTimer >= VOICE_FRAME_INTERVAL)  // 20ms
  {
      _voiceCaptureTimer = 0;
      VoiceChatManager.CaptureAndSendFrame();  // 采集 → 编码 → 发送
  }
  VoiceChatManager.ProcessReceivedFrames();    // 每帧: 解码 → Jitter Buffer → 播放
```

#### 15.1.3 消息类型

```csharp
VoiceChatData     = 100, // {senderId, seqNum, opusFrame: byte[]} — 不可靠通道
VoiceChatMute     = 101, // {steamId, isMuted} — 静音/取消静音
VoiceChatState    = 102, // {steamId, isSpeaking} — 语音活动检测 (VAD)
```

### 15.2 音乐广播 (MusicSync = 110-119)

#### 15.2.1 音频采集点

游戏音频经过分层 AudioMixer:
```
MusicManager → MusicGroup ─┐
BGMManager   → BGMGroup   ─┤
VoiceManager → VoiceGroup ─┼─→ Master Mixer → AudioListener → 扬声器
SEManager    → SEGroup    ─┤
AmbientBGM   → AmbientBGM ─┘
```

**采集方案: 在 AudioListener 上挂载 `OnAudioFilterRead`**

不论游戏用哪种方式加载音乐（AudioClip、CoreStreamingService、模块各自的 PCM 流等），
最终混合音频都经过 AudioListener。通过 `OnAudioFilterRead` 回调直接读取最终输出的 PCM 数据：

```csharp
// 挂载到 AudioListener 所在的 GameObject 上
public class AudioOutputCapture : MonoBehaviour
{
    private readonly RingBuffer<float> _captureBuffer;  // 环形缓冲区
    
    // Unity 在音频线程中回调此方法，提供最终混合的 PCM 数据
    void OnAudioFilterRead(float[] data, int channels)
    {
        // data = 交错采样 float[], channels = 声道数 (通常 2)
        // 采样率 = AudioSettings.outputSampleRate (通常 48000)
        _captureBuffer.Write(data, 0, data.Length);
        // 注意: 不修改 data，保持原样输出到扬声器
    }
}
```

**优点**:
- 一个采集点覆盖所有音源（游戏音乐、Mod 音乐、环境音等）
- 不依赖任何特定的音频加载方式
- AudioSettings.outputSampleRate 通常已是 48kHz，与 Opus AUDIO 模式匹配

#### 15.2.2 编码与传输

```
音频线程:
  OnAudioFilterRead → _captureBuffer.Write()

Tick() 主线程 (20ms 定时器):
  1. _captureBuffer.Read(960 frames × 2ch = 1920 floats)  // 48kHz × 20ms = 960 frames
  2. Opus.Encode(pcm, AUDIO mode, 96-128kbps) → byte[]
  3. P2P 发送 MusicStreamData{seqNum, opusFrame} (不可靠通道)

每 50ms:
  MusicTimeSync{positionMs, serverTimeMs} → 用于客户端漂移修正
```

#### 15.2.3 接收端

```
客户端 PollMessages():
  收到 MusicStreamData → Jitter Buffer (排序 + 补偿丢包)
  收到 MusicTimeSync → 校准播放时钟

每帧 ProcessReceivedFrames():
  1. Jitter Buffer 取出下一帧
  2. Opus.Decode(opusFrame) → float[] PCM
  3. 写入播放用 AudioClip 环形缓冲区 → AudioSource.Play()
```

#### 15.2.4 音量/混音控制

客户端可独立控制广播音频的音量，不影响本地游戏音效:
- 广播 AudioSource 连接到独立的 AudioMixerGroup
- JS 端通过 `chill.studyRoom.setMusicBroadcastVolume(0.5)` 控制

#### 15.2.5 消息类型

```csharp
MusicSync         = 110, // {action: play|pause|seek|stop, songInfo?} — 元数据同步, 可靠
MusicStreamData   = 111, // {seqNum, opusFrame: byte[]} — Opus音频帧, 不可靠通道
MusicTimeSync     = 112, // {positionMs, serverTimeMs} — 播放位置修正, 不可靠
MusicInfo         = 113, // {title, artist, coverUrl, duration} — 歌曲元数据, 可靠
```

### 15.3 邀请码系统
- Steam Lobby ID 编码为短邀请码
- 客户端通过邀请码直接加入，绕过公开列表

### 15.4 聊天系统 (ChatMessage = 120-129)

#### 15.4.1 驱动机制

文本聊天是**纯事件驱动**，不需要循环同步：

```
发送: JS UI → ChillStudyRoomApi.sendChat(text) → P2P 发送
接收: PollMessages() → OnMessageReceived → "chatReceived" 事件 → JS UI 渲染

消息通过可靠通道传输，确保不丢失。
```

#### 15.4.2 消息类型

```csharp
ChatMessage       = 120, // {senderId, personaName, text, timestampMs} — 可靠通道
ChatEmote         = 121, // {senderId, emoteId} — 表情/快捷反应
```

### 15.5 高频循环定时器汇总

```
StudyRoomService.Tick() 最终形态:

  每帧 (必需):
    P2PTransport.PollMessages()
    VoiceChatManager.ProcessReceivedFrames()      [§15.1, 解码+播放]
    MusicBroadcastManager.ProcessReceivedFrames()  [§15.2, 客户端端解码+播放]

  20ms 定时器 (语音采集, 仅开启语音时):
    VoiceChatManager.CaptureAndSendFrame()

  20ms 定时器 (音乐广播, 仅主机+开启广播时):
    AudioOutputCapture → _captureBuffer.Read(960 frames)
    MusicBroadcastManager.EncodeAndBroadcastFrame()

  50ms 定时器 (音乐位置同步, 仅主机+开启广播时):
    MusicBroadcastManager.BroadcastTimeSync()

  1s 定时器 (番茄钟快照, 仅主机):
    HostSyncManager.BroadcastPomodoroSnapshot()

  2s 定时器 (心跳):
    SendHeartbeat() + CheckPeerTimeout()

音频采集 (音频线程, 独立于 Tick):
    AudioOutputCapture.OnAudioFilterRead() → 写入环形缓冲区
    (主线程 Tick 从环形缓冲区读取)

其余 (事件驱动):
    角色状态/场景/语音/装饰/环境/存档 → 全部由 Harmony Patch 触发
```

---

## 16. 配置参数

```csharp
static class StudyRoomConfig
{
    // 心跳
    const float HeartbeatIntervalSeconds = 1.0f;
    const float DisconnectTimeoutSeconds = 3.0f;
    const float ReconnectTimeoutSeconds = 60.0f;
    
    // 番茄钟快照
    const float PomodoroSnapshotIntervalSeconds = 1.0f;
    
    // 互动锁
    const float InteractionLockTimeoutSeconds = 30.0f; // 超时自动释放
    
    // 故事等待
    const float StoryAckTimeoutSeconds = 5.0f;
    
    // 房间
    const int MaxMembers = 8;
    const string LobbyFilterKey = "chill_studyroom";
    const string ProtocolVersion = "1";
    
    // 子存档
    const string HostProfilePrefix = "_sr_host_";
    const string ClientProfilePrefix = "_sr_client_";
}
```

---

## 17. 实现优先级

### Phase 1: 核心联机 (MVP)
1. `SteamLobbyManager` — 房间创建/搜索/加入
2. `P2PTransport` — Steam P2P 消息收发
3. `SyncProtocol` — 消息序列化
4. `StudyRoomPatches` — Harmony Patches
5. `HostSyncManager` — 主机状态捕获+广播
6. `ClientSyncManager` — 客户端接收+应用
7. `StudyRoomService` — 生命周期管理 + 子存档

### Phase 2: 完整体验
8. `ChillStudyRoomApi` — JSApi 层（房间管理/大厅浏览/事件订阅）
9. 客户端准备流程（Lock/AI禁用/UI隐藏）
10. `InteractionLock` — 点击独占
11. `SaveDataSyncManager` — 存档协同编辑
12. 故事同步 (StoryReady/Ack/Start)
13. 密码挑战-应答
14. 断线重连

### Phase 3: 扩展功能
15. 语音聊天
16. 音乐广播
17. 邀请码
18. 聊天系统

