# 探针实验记录

## 第二代探针 (Gen 2)：同进程 RTTI/COL/vtable 动态发现

### 第一轮：RTTI 定位

**输入：**

```
version.dll 代理 DLL 被游戏进程 (PID 29424) 旁加载
probe thread 启动，90s 后执行第一轮扫描
主模块基址: 0x7FF6985A0000, 大小: 187715584 bytes
```

**输出：**

```
rtti ok=1
typedesc = 0x7FF6A25DB018, RVA = 0xA03B018
COL      = 0x7FF6A064A168, RVA = 0x80AA168
vtable   = 0x7FF69EF2E7F0, RVA = 0x698E7F0
```

**分析：** `_Ref_count_obj2<RadioStreamFmod>` 虚表定位链路闭合（RTTI→COL→vtable），无需硬编码 RVA。

---

### 第二轮：Heap → SampleProperties 链闭合

**输入：**

```
游戏进程 PID 6040，地图运行 >180s 后暂停选单
exe_base: 0x7FF6985A0000
RTTI/COL/vtable RVA 同第一轮
```

**输出：**

```
heap scan: 5 个 _Ref_count_obj2<RadioStreamFmod> 候选

高分候选 (score=115):
  refcount          = 0x1F301A64370
  stream_object     = 0x1F301A64380    (refcount + 0x10)
  uses=1, weaks=1
  object_vtable     = 0x7FF69EF28168
  fmod_sound        = 0x1F303CDC4D8    (refcount + 0x18)
  props_owner        = 0x1F301C2AF60    (refcount + 0x58)
  sample_properties  = 0x1F301C6D0A0    (props_owner + 0x18)
  SoundName   = "HZ6_R5_NuTone_Clarity"       (+0x10, heap string, len 21)
  DisplayName = "Clarity"                       (+0x30, SSO string, len 7)
  Artist      = "Nu:Tone x Solah"               (+0x50, SSO string, len 15)
```

**分析：** 运行时字段偏移全部验证通过。低分 4 个候选（fmod_sound=0）为空闲 control block。

---

### 第三轮：R10 / Streamer Mode active 链验证

**输入：**

```
修复 radio_state_singleton 签名偏移：
  原错误: 把 disp32 当指令起点
  修正: disp32 地址 = hit + 0x15
```

**输出：**

```
radio_state:
  sig_found              = true
  chain_ok               = true
  active_streamer_mode   = true
  global_ptr_slot        = 0x7FF6A2EDFB70, RVA = 0xA93FB70
  p1 = 0x1EC4C204460
  p2 = 0x1EC4C2C42D0
  p3 = 0x1EC4C415960
  station_name           = "Streamer Mode"    (p3 + 0x200, SSO, len 13)

高分候选:
  sound_name = "HZ6_R8_MitchMurder_SniperRouge"
  r10_fallback_sound = false
```

**分析：** R10 active 时当前曲目是普通曲库条目，PeterBroderick 只是 fallback sample。active 判断应优先用 radio_state_singleton。

---

## 第三代探针 (Gen 3)：HTTP 实时控制接口

### 探针端点实现

**新增结构体：**

- `ModuleInfo`: base, size, .text/.rdata/.data SectionInfo
- `RttiResult`: typeName, typeDesc, col, vtable, 对应 RVA
- `MsvcString`: ok, field, data, len, cap, sso, value
- `Candidate`: refcount, streamObject, uses, weaks, objectVtable, fmodSound, propsOwner, sampleProperties, handle32, soundName/displayName/artist, r10FallbackSound, score
- `RadioStateResult`: sigFound, chainOk, globalPtrSlot/Rva, p1/p2/p3, stationName, activeStreamerMode
- `LockState`: locked, refcount, fmodSound, propsOwner, sampleProperties, handle32, sinceTick, liveStatus
- `ControlState`: mod, rtti, radio, candidates, scanCount, lastScanTick, phase, lastCommand, lastError, haveOriginalMeta, originalSampleProperties, originalSoundName/DisplayName/Artist, lock, handlePath, apiResult, topo, nativePatterns, systemIHits

**新增函数：**

- `GetMainModuleInfo()`: PE 解析 → .text/.rdata/.data section 定位
- `ResolveRefcountRtti()`: 搜索 ".?AV?$_Ref_count_obj2@VRadioStreamFmod@@@std@@" → TypeDescriptor → CompleteObjectLocator → vtable
- `ReadMsvcString()`: SSO/heap string 读取，限制 len≤0x1000, cap≤0x100000
- `WriteMsvcString()`: 写入 SSO buffer 或 heap buffer + 更新 len 字段
- `ScanHeapCandidates()`: VirtualQuery → committed private RW pages → 逐 16 字节匹配 vtable → 读 uses/weaks/fmod_sound/props_owner/handle32 → 解引用 sample_properties → 读三字符串 → ScoreCandidate 评分 → 排序取 top 64
- `ScoreCandidate()`: 基础分+字符串分+特殊标记分，最高 115
- `ResolveRadioState()`: 字节模式扫描 radio_state_singleton → 解析 p1→p2→p3→station_name
- `RunScan()`: 串联 GetMainModuleInfo + ResolveRefcountRtti + ResolveRadioState + ScanHeapCandidates
- `ApplyMetadata()`: 写 title→+0x30, artist→+0x50, sound→+0x10
- `RestoreMetadata()`: 恢复首次 /meta 写入时的原始值

**新增 HTTP 端点：**

```
GET  /state         → ControlState JSON 快照
POST /scan          → 全量扫描（RTTI + radio state + heap scan）
POST /meta          → 写 metadata（title/artist/sound）
POST /meta/restore  → 恢复原始 metadata
POST /skip          → 预留（返回 pending）
POST /tone          → 预留（返回 pending）
```

### 切歌前后扫描实验

**Step 1 — 写入 metadata：**

```
输入:  POST /meta?title=TEST_TITLE&artist=TEST_ARTIST&sound=TEST_SOUND
```

```
输出:  HUD 立即变化，歌曲继续正常播放
```

**Step 2 — 切歌前扫描：**

```
输入:  POST /scan
```

```
输出:
  refcount          = 0x20664114370
  fmod_sound        = 0x2066639E8C8
  props_owner        = 0x206642DA880
  sample_properties  = 0x2066431B840
  sound_name         = TEST_SOUND
  display_name       = TEST_TITLE
  artist             = TEST_ARTIST
```

**Step 3 — 等待自然切歌到 "Thread The Needle"**

**Step 4 — 切歌后扫描：**

```
输出:
  refcount          = 0x20664114370    ← 不变
  stream_object     = 0x20664114380    ← 不变
  object_vtable     = 0x7FF69EF28168   ← 不变
  fmod_sound        = 0x20665247E88   ← 改变
  props_owner        = 0x206642DB360   ← 改变
  sample_properties  = 0x2066431DA60   ← 改变
  sound_name         = HZ6_R5_HoaxSoWhat_ThreadTheNeedle
  display_name       = Thread The Needle (feat. Dynamite MC)
  artist             = Hoax x So What?
```

**分析：** RadioStreamFmod 是持久播放器对象；切歌本质是替换内部 fmod_sound / props_owner / sample_properties 指针；手动 metadata 写入切歌后会被覆盖。

---

## 第四代探针 (Gen 4)：Lock/Live + Handle Path + FMOD API Resolver

### Probe 改动

**新增结构体：**

- `HandlePathDump`: sampleProperties, handleBase, vecBegin, vecEnd, vecSize, items[]
- `HandlePathElement`: elemAddr, field_00/08/10, ptrAt18, field_20/28, handle, field_34/38/40, soundInModule
- `FmodApiEntry`: name, stringRva, leaRva, funcRva, funcAddr, found, note
- `FmodApiResult`: ok, entries[], systemI, error
- `TopologyDump`: ok, targetSound, systemI, masterCg, groupsSeen, channelsSeen, matched, matchedChannel, matchedGroup, handleFallback, error, log[]

**新增函数：**

- `CmdLock()`: 锁定当前最佳 candidate → ControlState.lock
- `CmdUnlock()`: 清除 lock
- `CmdLive()`: 重读 locked refcount 的 uses/weaks (alive check) → 读 cur fmod_sound/props/sample_properties → 比较 old/new 检测 song_changed
- `ReadHandlePathDump()`: 读 sample_properties +0x58/+0x80/+0x88 → 遍历 vector 元素 → 读每元素 64 字节字段 → 返回 HandlePathDump
- `ResolveFmodApis()`: .rdata 搜索 FMOD API 字符串 → .text 找 LEA RIP-rel 引用 → prologue backtrack → 返回函数入口
- `RunTopologyWalk()`: fmod_sound + 多偏移 → FindSystemICandidates → 读 SystemI vtable → 匹配 resolve-api 地址
- `CmdDumpStream()`: dump streamObject 内存窗口 + fmod_sound + 关键指针 + bus_path
- `CmdDumpPtr()`: dump 任意地址内存窗口 + in_module 标记 + MSVC string 识别
- `CmdDumpGraph()`: 从 stream 出发 dump fmod_sound + ptr40/ptr48/ptr80/ptr98 的子字段
- `CmdDumpPlaybackNode()`: 读 stream+0x80 → 校验 current_sound_match / stream_backref_match → 展开子指针

**新增 HTTP 端点：**

```
POST /lock                    → 锁定最佳 refcount
POST /unlock                  → 清除锁定
GET  /live                    → 快速刷新 (~50μs)
POST /fmod/handle-path        → handle vector dump
POST /fmod/resolve-api        → FMOD API 函数解析
POST /fmod/topology-dump      → Topology Walk
GET  /dump/stream             → streamObject dump
GET  /dump/ptr                → 任意地址 dump
GET  /dump/graph              → 对象关系图
GET  /dump/playback-node      → playback node dump
POST /game/stop-active        → Channel::stop (预留)
```

### Lock + Live 切歌监视实验

**输入：**

```
POST /lock → 锁定 refcount
GET  /live → 高频轮询
等待游戏自然切歌 (Telecompn→Damage)
```

**输出：**

```
切歌前:
  fmod_sound        = 0x16E1CDAFD18
  sample_properties  = 0x16E1AD1E830
  sound_name         = HZ6_R8_GridlusterJohnatron_Telecompn
  display_name       = Telecompn[[今夜]]
  artist             = Gridluster, Johnatron

切歌后:
  fmod_sound        = 0x16E1BDE1DE8    ← 改变
  sample_properties  = 0x16E1AD1DF40    ← 改变
  song_changed       = true
  stream_object      = 0x16E1AB14380    ← 不变
```

**分析：** Lock/Live 可用作切歌监视器。每轮 ~50μs。

### Handle Path 偏移失败

**输入：**

```
POST /fmod/handle-path → 从 SampleProperties +0x58/+0x80/+0x88 读 handle vector
```

**输出：**

```
vector invalid / 数据不可用
```

**分析：** 这些偏移是原作者 `InProcessInjector` 结构体的成员，不是 SampleProperties 的内部字段。handle 路径信息在 `RadioStreamFmod +0x40/+0x48/+0x80` 等字段指向的对象中。

### FMOD API Resolver 结果

**输入：**

```
POST /fmod/resolve-api → .rdata 搜索 14 个 FMOD API 字符串
```

**输出：**

```
9/10 核心 API 在 .rdata 找到字符串并在 .text 找到 LEA 引用:
  System::getMasterChannelGroup   ✓
  ChannelGroup::getNumChannels    ✓
  ChannelGroup::getChannel        ✓
  ChannelGroup::getNumGroups      ✓
  ChannelGroup::getGroup          ✓
  Channel::getCurrentSound        ✓
  Channel::setChannelGroup        ✓
  Channel::stop                   ✗ (字符串不存在)
  System::createSound             ✓
  System::playSound               ✓
注: LEA 地址是函数内部指令，不是函数入口；prologue backtrack 大部分不命中
```

### Topology Walk 失败

**输入：**

```
fmod_sound + 0x18 的指针其 vtable 不在模块内 → 失败
改为多偏移探测: 0x08/0x10/0x18/0x20/0x28/0x40/0x48
```

**输出：**

```
fmod_sound + 0x40 → 命中一个对象 (vtable in module)
但 vtable 条目与 resolve-api 函数地址不匹配（匹配数=0）
```

**分析：** 找到了"长得像"的对象但不能证实是 FMOD::SystemI。

### Playback Node 确认

**输入：**

```
stream +0x80 → playback_node
读取并验证双向引用
```

**输出：**

```
current_sound_match = true    (node+0x08 == stream_fmod_sound)
stream_backref_match = true   (node+0x20 == stream_object)

node +0xC8 = 0x7FF69EF26910   (module vtable-like, RVA 0x6986910)
node +0xF8 = 0x7FF69EF26910   (same)
node +0xD8 = 0x1E5A6513E00    (heap object, vtable 0x6989310)
node +0xE0 = 0x1E5A6513E00    (same)
```

**分析：** stream+0x80 是当前 radio playback/binding 节点，随切歌更新，稳定持有 current fmod_sound 与 stream_object 双向关系。

---

## 第五代探针 (Gen 5)：SystemI 上下文反搜

### Probe 改动

**新增结构体：**

- `NativePatternHit`: addr, rva, funcAddr, funcRva, alt
- `NativePatternResult`: name, pattern, hits[]
- `SystemIContextHit`: ctx, rangeBase, rangeSize, vecBegin, vecEnd, vecCount, slot, elem, p1, systemI, vtable, index, textEntries, rdataEntries, dataEntries, moduleEntries, nonModuleEntries, asciiLikeEntries, prefixTextEntries, firstBadIndex, score

**新增函数：**

- `ParsePattern()`: 字节模式解析（含 ?? 通配符）
- `FindPattern()`: 单次字节模式搜索
- `FindPatternAllInBytes()`: 多次字节模式搜索
- `ResolveNativePatterns()`: 扫描 systemCreateDSP, dspRelease, channelControlAddDSP, channelControlRemoveDSP, fmod_handle_resolver, fmod_handle_unlock, radio_set_station_by_name
- `ScanSystemIContexts()`: VirtualQuery → 每 region 逐 8 字节找 ctx 形状 → 验证 rangeBase/rangeSize → 读 vec → 遍历元素 → elem+0x18→p1→p1+0xC0→systemI → vtable in range → vtable 前 64 项分类统计

**新增 HTTP 端点：**

```
POST /fmod/resolve-native-patterns      → 字节模式扫描
POST /fmod/find-systemi-contexts?max=N  → 形状反搜 SystemI 候选
POST /fmod/find-systemi-contexts-strict → strict 过滤 (prefix_text≥12 或 text_entries≥32 且 score≥120)
GET  /fmod/systemi-vtable?system=X&count=N → dump 指定 SystemI vtable
```

### Native Pattern 结果

**输入：**

```
POST /fmod/resolve-native-patterns
```

**输出：**

```
fmod_handle_resolver       → unique hit, RVA = 0x5737EB0
fmod_handle_unlock         → unique hit, RVA = 0x5726B20
radio_set_station_by_name  → unique hit, pattern RVA = 0x3180750, func = 0x31806A0
systemCreateDSP / other    → multiple hits (非唯一定位)
```

### SystemI Context 反搜结果

**输入：**

```
POST /fmod/find-systemi-contexts?max=32
```

**输出：**

```
候选 1: systemI=0x2AC8ABFB480, vtable=0x7FF6A2D19170 (RVA 0xA779170)
  前 64 项: text=?, rdata=?, ascii_like 大量
  判断: vtable[0]=0x3204005A (非代码地址)，误报

候选 2: systemI=0x2AD6A03BEA0, vtable=0x7FF69EE46C60 (RVA 0x68A6C60)
  前 50 项: 模块内地址，第 51 项起 ASCII-like (InvalidPose/Merge)
  判断: 误报

候选 3: systemI=0x2AD69F2D908, vtable=0x7FF69EE50E98 (RVA 0x68B0E98)
  前半: 模块内地址，中段: Anim/InputSkeletonBone/TransformMask 等
  判断: 误报
```

**分析：** FUN_180010fb0 形状反搜太宽，不符合 SystemI 消费形状。三个候选都不是真正的 FMOD::SystemI。

### Strict 过滤结果

**输入：**

```
POST /fmod/find-systemi-contexts-strict?max=16
```

**输出：**

```
raw_count = 2, strict_count = 2

候选 A: score=227, text=56, prefix_text=4, first_bad_index=4
  判断: prefix_text 只有 4 -- 不像 C++ vtable

候选 B: score=179, text=50, prefix_text=3, first_bad_index=3
  判断: 同样不像 C++ vtable
```

**分析：** 两个候选跨重启稳定存在，但 prefix_text 太少，更像 mixed dispatch table。

---

## 第六代探针 (Gen 6)：FMOD Wrapper / Code-Window 分析

### Probe 改动

**新增函数：**

- `BacktrackFunctionStart()`: 从指令偏移往回找函数 prologue（支持 CC/NOP padding 跳过、RET 回溯、回退搜索）
- `LooksFunctionStart()`: 匹配 MSVC x64 prologue 模式（15+ 种）
- `CmdNativeXrefs()`: 定位目标函数 → 扫 .text 中 E8 direct call → 每个 call 附近 post_calls (direct/vcall/vjmp) → 扫 .rdata/.data 中 qword 引用
- `CmdCodeWindow()`: dump 指定 RVA 的 .text 窗口 → 标注 direct_call/rip_lea/rip_mov/vcall/vjmp 事件

**修正：**

- `ResolveFmodApis()`: func_rva 计算修正（之前少加 .text RVA）
- `post_calls`: 新增 `kind:vjmp` 识别（`48 FF A0/A1 disp32`）

**新增 HTTP 端点：**

```
GET /fmod/native-xrefs?name=fmod_handle_resolver   → resolver xref
GET /fmod/native-xrefs?rva=5737EB0                  → 按 RVA 查 xref
GET /fmod/code-window?rva=RVA&span=HEX              → .text 窗口 dump
```

### Resolver Xref 结果

**输入：**

```
GET /fmod/native-xrefs?name=fmod_handle_resolver
```

**输出：**

```
10 个 direct call:

call RVA    func RVA    判断
0x571B21D   0x571B1E0   Channel::getCurrentSound wrapper
0x571B302   0x571B2C0   Channel/ChannelControl wrapper
0x571B3EA   0x571B3A0   wrapper
0x571B525   0x571B4E0   wrapper，内部 vcall +0x1F0
0x571B5FD   0x571B5C0   Channel::setChannelGroup wrapper
0x571B6CC   0x571B5C0   setChannelGroup 另一路径
0x571B7AF   0x571B760   wrapper
0x571B974   0x571B930   wrapper
0x571BA9C   0x571B930   wrapper 另一路径
0x58D8917   0x58D8890   上层消费点

resolve-api ↔ wrapper 对应:
  Channel::getCurrentSound         → 0x571B1E0
  Channel::setChannelGroup         → 0x571B5C0
  System::getMasterChannelGroup    → 0x5714FC0
  System::createSound              → 0x5713F90
  System::playSound                → 0x5715A30
```

### Code-Window 关键发现

**0x5734FB0 helper:**

```
输入:  /fmod/code-window?rva=5734FB0&span=180
输出:  vcall offset=0x120 (index 36), 逻辑: obj=*(this+0x1C0); out=*(obj+0x18)
分析: 是 resolver 后的 handle/object helper，vcall +0x120 可能是 handle owner lookup
```

**0x571B4E0 wrapper:**

```
输入:  /fmod/code-window?rva=571B4E0&span=180
输出:
  direct_call → fmod_handle_resolver 0x5737EB0
  vcall       → offset 0x1F0 (index 62)
  direct_call → helper 0x5727410
  direct_call → fmod_handle_unlock 0x5726B20
分析: 完整 handle 调用链: resolver→vcall+0x1F0→unlock。vcall+0x1F0 是首个确认真实虚表调用
```

**0x58D8890 上层消费点:**

```
输入:  /fmod/code-window?rva=58D8890&span=200
输出:
  vcall offset=0xC0 (index 24) ×2
  direct_call → fmod_handle_resolver
  direct_call → 0x58D9050
  vcall offset=0x40 (index 8)
  vcall offset=0xC8 (index 25)
分析: 不是普通 FMOD wrapper，更像上层遍历/消费当前播放节点的函数
```

**0x5736200 vjmp:**

```
输入:  /fmod/code-window?rva=5736200&span=180
输出:
  function_rva = 0x5736130
  vjmp offset=0x90 (index 18)  -- 新增 vjmp 识别生效
分析: obj=*(rcx+0x1C0); if ok → obj->vcall[0x90] else ERROR
```

### Playback Node 扩展验证

**输入：**

```
GET /dump/playback-node
```

**输出：**

```
current_sound_match = true
stream_backref_match = true

node +0x08 = fmod_sound
node +0x10 = 0x1E5A6EFE288
node +0x20 = stream_object (backref)
node +0xC8 = 0x7FF69EF26910 (module, RVA 0x6986910)
node +0xF8 = 0x7FF69EF26910 (same)
node +0xD8 = heap object, vtable RVA 0x6989310
node +0xE0 = same heap object

切歌后: stream+0x80 换新对象，仍满足 node+0x08/0x20 双向关系
```

---

## 第七代探针 (Gen 7)：SystemI 路径最终闭合

### 关键纠正

**输入：** Ghidra 搜索 `"[fmod-inject] SystemI* = "` 字符串（位于 `0x18015a9e8`）

**输出：**

```
xref → FUN_180010fb0 (地址 0x180011171, 0x1800111d1, 0x1800111fe)
NOT FUN_180009590 (文档错误 -- 后者是 JSON 解析器)
```

### Ghidra 汇编取证

**输入：** 反编译 `FUN_180010fb0` + 反汇编交叉验证

**输出：**

```asm
; 循环初始化
180010fe8: MOV RAX, [RCX + 0x40]      ; *(FmodInject + 0x40) = InProcessInjector*
180010fec: MOV RBX, [RAX + 0x80]      ; *(base_ptr + 0x80) = refcount_hits_begin
180010ff3: MOV R12, [RAX + 0x88]      ; *(base_ptr + 0x88) = refcount_hits_end

; 循环体
180011003: MOV RCX, [RBX]             ; elem = *vec_begin
180011008: ADD RCX, 0x18              ; elem + 0x18 = fmod_sound*
180011014: CALL FUN_180010150          ; *out = *(elem + 0x18)

18001102c: ADD RCX, 0xC0              ; fmod_sound + 0xC0
180011038: CALL FUN_180010150          ; *out = *(fmod_sound + 0xC0) = SystemI

; 范围验证
18001104b: MOV RAX, [R15 + 0x40]
18001104f: MOV RSI, [RAX + 0x58]      ; exe_base
180011053: MOV R14, [RAX + 0x60]      ; exe_size
180011064: CALL FUN_180010150          ; vtable = *SystemI
180011072: CMP R9, RSI                ; vtable >= exe_base?
18001107e: JC found
```

**偏移语义：**

| 汇编偏移             | 实际含义                                     |
| -------------------- | -------------------------------------------- |
| *(FmodInject + 0x40) | InProcessInjector*                           |
| *(base_ptr + 0x58)   | exe_base                                     |
| *(base_ptr + 0x60)   | exe_size                                     |
| *(base_ptr + 0x80)   | refcount_hits_begin (不是 SampleProperties!) |
| *(base_ptr + 0x88)   | refcount_hits_end                            |
| *(elem + 0x18)       | fmod_sound*                                  |
| *(fmod_sound + 0xC0) | SystemI                                      |

### 运行时验证

**输入：**

```
refcount          = 0x1CFB1614370
*(refcount+0x18)  → fmod_sound
*(fmod_sound+0xC0) → SystemI candidate
```

**输出：**

```
refcount              = 0x1CFB1614370
*(refcount+0x18)      = 0x1CFB3AAB858    (fmod_sound, 与 playback_node 一致 ✓)
*(fmod_sound+0xC0)    = 0x1CD9D2A7108    (SystemI object ✓)
*(SystemI)            = 0x7FF69F6FC1E0    (vtable, RVA=0x715C1E0)
范围检查: 0x7FF6985A0000 ≤ 0x7FF69F6FC1E0 < exe_end ✓

SystemI vtable (0x715C1E0):
  [0] 0x571EEA0 -- MSVC COM 多重继承初始化 (LEA vtable + MOV [RCX],RAX + ADD 0x11A10)
  [1] 0x578FD00 -- 调用 0x59E09C0, 引用全局 0x8F7D400
  [2] 0x578FED0 -- 调用 0x5753A50, 引用全局 0x8F6D110
  [3] 0x578FCC0 -- 调用 0x5753EA0

RTTI 字符串 (vtable+0x20):
  D:\p4\fort_main\external\FMOD\Source\core\api\system\systemi.cpp
```

**分析：** SystemI 确认闭合。vtable 只有 4 个方法是因为 MSVC 多重继承（完整 SystemI 有多个 vtable 视图）。

### fmod_sound 子对象全量扫描

**输入：**

```
fmod_sound 全量扫描 0x00–0x200
```

**输出：**

```
+0x0C0 → SystemI      (vtable 0x715C1E0, 4 methods)
+0x118 → SoundI        (vtable 0x715ED60, 48 methods, 自引用)
+0x140 → SoundI        (same)
+0x158 → SoundI        (same)
+0x160 → 另一 FMOD 对象 (vtable 0x7185FF0, 26 methods)
+0x168 → SoundI 同类    (vtable 0x7187090, 51 methods)
```

---

## 第八代探针 (Gen 8)：FMOD API 调用与 CG 树探索

### Probe 改动

**新增结构体/函数：**

- `SafeCallRaw4()`: SEH-safe 4 参数 x64 fastcall
- `SafeCallRaw3()`: SEH-safe 3 参数 x64 fastcall
- `CmdCall()`: 通用 /call?addr=RVA&rcx=&rdx=&r8=&r9=
- `CmdVCall()`: /vcall?object=&index=&rcx=&rdx=&r8=
- `CmdPtrChain()`: /ptrchain?base=&offsets=0x18,0xC0
- `CmdFmodCallGetMasterCG()`: call exeBase+0x5715FC0(systemI, &masterCG)
- `CmdFmodCallCreateSound()`: createSound 测试 (44100/stereo/0x48a)
- `CmdFmodGetSystemI()`: 一键获取 SystemI
- `CmdCgGetNumGroups()`: getMasterChannelGroup → getNumGroups
- `CmdCgGetGroup()`: getMasterChannelGroup → getGroup(index)
- `CmdCgGetChildGroup()`: vtable-based getGroup
- `CmdCgVCall()`: CG vtable 调用 (带栈上输出缓冲)
- `CmdCgScanGetGroup()`: 暴力扫描 vtable[0-79] 调用 getGroup(index=0)
- `CmdVCallBuf()`: /vcall-buf (2 参数 getter)
- `CmdVCallOut()`: /vcall-out (4 参数，返回 out1/out2)
- `CmdVtableDump()`: /fmod/vtable?rva=&count=

**新增 HTTP 端点：**

```
POST /call?addr=RVA&rcx=&rdx=&r8=&r9=
POST /vcall?object=&index=&rcx=&rdx=&r8=
GET  /ptrchain?base=&offsets=0x18,0xC0
GET  /fmod/get-systemi
POST /fmod/call-get-master-cg
POST /fmod/call-create-sound
GET  /fmod/cg-get-num-groups
GET  /fmod/cg-get-group?index=N
GET  /fmod/cg-vcall?index=N
GET  /fmod/cg-scan-getgroup
GET  /vcall-out?object=&index=&rdx=
GET  /vcall-buf?object=&index=
GET  /fmod/vtable?rva=RVA&count=N
```

### 地址修正

**输入：**

```
对比 resolve-api 与 code-window backtrack 差异
```

**输出：**

```
getMasterChannelGroup:
  code-window backtrack → 0x5715E80 (错误)
  resolve-api 正确地址  → 0x5715FC0 (正确)
  验证: 0x5715FC0 返回 INVALID_PARAM (正确行为)

createSound:
  code-window backtrack → 0x5714E50 (错误)
  resolve-api 正确地址  → 0x5714F90 (正确)
```

### getMasterChannelGroup 跨重启验证

**输入：**

```
POST /fmod/call-get-master-cg
```

**输出：**

```
fmod_result = 0 (FMOD_OK)
master_cg = 有效堆地址 (如 0x1B39B614008)
wrapper_addr = 0x7FF69DCB5FC0 (RVA=0x5715FC0)

master CG 结构:
  +0x00 = vtable (0x715D728, in .rdata)
  +0x08 = SystemI* (回指)
  +0x18 = 1
  +0x50 = self-ref
  +0x58 = 0x3F800000 (1.0f)
```

### CG vtable 探测

**输入：**

```
对 master CG vtable[0-15] 做 vcall-buf 探测
```

**输出：**

```
[0]  RVA 0x5735350 → out1=堆地址 (QueryInterface)
[7]  RVA 0x57415B0 → out1=1 → getNumGroups (确认返回 1) ✓
[8]  RVA 0x5738420 → vcall+0x120 (thunk/handle 转发)
[9]  RVA 0x5739450 → rax=0 → getGroup 候选
[12] RVA 0x5741520 → out1=1 → getNumChannels 候选
[15] RVA 0x5739C80 → crashed (危险)
```

### getGroup 暴力扫描

**输入：**

```
/fmod/cg-scan-getgroup: vtable[0-79] 调用 getGroup(0, &child)
```

**输出：**

```
4 hits:
vi=27: child=0x3F0C0000000 (无效)
vi=29: child=0xD234FDC40   (无模块 vtable, 原始 FMOD 对象)
vi=36: child=0x13495854330  (vtable 0x715CB08, 游戏侧 Channel 包装器) ←
vi=49: child=0x234FDC58    (无效)

vtable[36] 子对象 RTTI:
  "FMOD::DSP::ChannelFormat"
  "Ignoring channel mask..."
```

### getNumGroups/getGroup 包装器阻塞

**输入：**

```
调用 fmod wrapper (0x571BC40) 的 getNumGroups(masterCG, &n)
调用 fmod wrapper (0x571BC40) 的 getGroup(masterCG, 0, &child)
```

**输出：**

```
返回 INVALID_PARAM (r≠0)
```

**分析：** 这些 wrapper 调用 fmod_handle_unlock，走 FMOD handle 系统。原始 CG 指针不直接作为参数 —— 需要经过 handle 封装。

---

## 第九代探针 (Gen 9)：Ghidra 签名解析链分析

**输入：**

```
Ghidra 反编译某闭源 mod:
  InProcessInjector 初始化 (FUN_180017160)
    → FUN_1800a14a0: PE 解析 → .pdata 函数边界枚举
    → FUN_1800a3a10: 签名解析
        → FUN_1800a1c20: 字符串匹配 (.rdata 搜索 FMOD API → .text LEA → .pdata filter → 函数入口)
        → FUN_1800a2cb0: 字节模式匹配 (handle_resolver 等)
        → FUN_1800a3140: radio_set_station_by_name
```

**输出 — 签名表布局 (injector + 0xa8 → FmodInject.sigs):**

```
+0x00: createSound               (字符串匹配)
+0x08: playSound                 (字符串匹配)
+0x28: channelSetChannelGroup   (字符串匹配)
+0x58: core_getMasterChannelGroup(字符串匹配)
+0x60: cg_getNumGroups           (字符串匹配)
+0x68: cg_getGroup               (字符串匹配)
+0x70: cg_getNumChannels         (字符串匹配)
+0x78: cg_getChannel             (字符串匹配)
+0x80: channel_getCurrentSound   (字符串匹配)
+0x88: fmod_handle_resolver      (字节模式)
+0x90: fmod_handle_unlock        (字节模式)
+0x98: radio_state_singleton      (字节模式)
```

**分析：** getNumGroups/getGroup/getNumChannels/getChannel 四个 LEA 引用点全部在同一函数 0x571BC40 内，内部调用 fmod_handle_unlock，证实走 handle 系统。

---

## 第十代探针 (Gen 10)：handle_resolver 修复与 fh6-universal-radio

### Probe 改动

发现了一个开源项目 fh6-universal-radio 从中补齐了逻辑空缺.

**修正：**

- `CmdResolveChannel()`: 从 hardcoded RVA 改为 native pattern 动态定位
- `ResolveNativePatterns()`: 使用 fh6-universal-radio 的正确 byte patterns
- handle_resolver 调用: 从 4 参数改为 3 参数 (SafeCallRaw3)
- 真实签名: `uint32_t Handle::open(uint32_t handle, void** out_inst, uint64_t* out_kind)`

**新增字段：**

- `Candidate.handle32`: refcount +0x30 读 uint32 handle
- `LockState.handle32`: 同上

**评分更新：**

- `ScoreCandidate`: handle32 非零 +15 分

**API 列表扩充：**

- `ResolveFmodApis`: 新增 createDSP, addDSP, setVolume, getChannelGroup

### handle_resolver 调用实验

**输入：**

```
handle32 = *(refcount + 0x30)
resolver RVA = 0x5737EB0 (native pattern 动态定位)
3 参数签名: Handle::open(handle, &channel, &kind)
```

**输出：**

```
待游戏重启测试
预期: channel 有效指针，vtable in module
```

### fh6-universal-radio 交叉验证

**输入：** 分析 `G:\fh6-universal-radio` 开源项目 (GPLv3)

**输出：**

```
架构: version.dll proxy + FMOD DSP 注入 + IAudioSource 抽象

DSP 注入流程:
  RadioStreamFmod +0x20 → uint32 handle
    → Handle::open(handle, &channel, &kind) → Channel*
    → System::createDSP(system, &desc, &dsp)
    → ChannelControl::addDSP(channel, 0, dsp)
  FMOD mixer callback → DSPBridge::read_callback
    → RingBuffer::read() → S16→float → gain → output

DSP 四种模式:
  mode 1 (passthrough): memcpy(input→output)
  mode 2 (silence):     memset(output, 0)
  mode 3 (tone):        生成正弦波
  mode 4 (pcm):         从 ring buffer 读 S16→float→output (丢弃游戏音频)

IAudioSource 接口:
  name(), initialize(), shutdown(), play(), pause(), stop(),
  pump(RingBuffer&), current_track(), playback_state(), next()

PCM 格式: S16 LE / 48000Hz / stereo / 4 bytes per frame
```

**分析：** 游戏注入层不需要重写。只需实现 OmniPcmSource : IAudioSource (~200 行)。

### 已验证通过全链

```
主模块解析:
  exe_base = GetModuleHandleW(NULL)
  .text/.rdata/.data → PE section 表

RTTI 发现:
  .rdata 搜索 "RadioStreamFmod" → TypeDescriptor
  → CompleteObjectLocator (sig=1) → vtable + vtable[0] in .text

Heap 扫描:
  VirtualQuery → 逐 16B 匹配 vtable
  uses/weaks ∈ (0,0x80]

对象字段:
  refcount + 0x08 → uses          ✓
  refcount + 0x0C → weaks         ✓
  refcount + 0x10 → RadioStreamFmod ✓
  refcount + 0x18 → FMOD Sound*    ✓
  refcount + 0x30 → uint32 handle  ✓
  refcount + 0x58 → props_owner    ✓
  props_owner+0x18 → SampleProperties* ✓

SampleProperties:
  +0x10 → SoundName    ✓ (读+写)
  +0x30 → DisplayName  ✓ (读+写, HUD 即时更新)
  +0x50 → Artist       ✓ (读+写)

SystemI:
  *(fmod_sound + 0xC0) → SystemI  ✓
  vtable RVA 0x715C1E0
  RTTI: FMOD/Source/core/api/system/systemi.cpp

电台状态:
  radio_state_singleton → p1→+0x40→p2→+0x50→p3
  p3 + 0x200 → station_name ✓ "Streamer Mode"

FMOD API:
  System::getMasterChannelGroup  ✓ 0x5715FC0
  System::createSound            ✓ 0x5714F90
  System::playSound              ✓ 0x5716A30
  System::createDSP              ✓ 0x5714E50
  ChannelControl::addDSP         ✓ 0x571C780
  handle_resolver                ✓ 0x5737EB0 (唯一命中)
  handle_unlock                  ✓ 0x5726B20 (唯一命中)

待验证:
  handle_resolver 调用 (签名修正后, 等游戏重启)
  CG 树导航通过 handle 路径 (非 wrapper)
```

### OmniPcmShared 集成方案

**数据流：**

```
后端进程 → shared memory (float/48000/stereo)
  → OmniPcmShared.dll → OmniPcm_ReadFrames → float*
  → OmniPcmSource::pump() → float→S16 → RingBuffer(S16)
  → DSPBridge::read_callback → S16→float → FMOD output
```

**格式转换：** float32 interleaved → S16 LE（48000Hz/stereo 原样输出，OmniPcmShared 后端控制采样率）

---

## 端点实现演进总表

| 代     | 新增端点                                                                                                                                                                                                                                                                                              | 状态                                                                                     |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| Gen 3  | `/state`, `/scan`, `/meta`, `/meta/restore`, `/skip`, `/tone`                                                                                                                                                                                                                             | 可用                                                                                     |
| Gen 4  | `/lock`, `/unlock`, `/live`, `/fmod/handle-path`, `/fmod/resolve-api`, `/fmod/topology-dump`, `/dump/stream`, `/dump/ptr`, `/dump/graph`, `/dump/playback-node`, `/game/stop-active`                                                                                            | lock/live/dump 可用；handle-path 证伪；topology-dump 证伪；stop-active 未接线            |
| Gen 5  | `/fmod/resolve-native-patterns`, `/fmod/find-systemi-contexts`, `/fmod/find-systemi-contexts-strict`, `/fmod/systemi-vtable`                                                                                                                                                                  | native-patterns 可用；systemi-contexts 误报率高                                          |
| Gen 6  | `/fmod/native-xrefs`, `/fmod/code-window`                                                                                                                                                                                                                                                         | 可用                                                                                     |
| Gen 7  | (无新端点, 仅修正逻辑: FUN_180010fb0 → fmod_sound+0xC0→SystemI)                                                                                                                                                                                                                                     | 验证通过                                                                                 |
| Gen 8  | `/call`, `/vcall`, `/ptrchain`, `/fmod/get-systemi`, `/fmod/call-get-master-cg`, `/fmod/call-create-sound`, `/fmod/cg-get-num-groups`, `/fmod/cg-get-group`, `/fmod/cg-get-child`, `/fmod/cg-vcall`, `/fmod/cg-scan-getgroup`, `/vcall-out`, `/vcall-buf`, `/fmod/vtable` | call/vcall/ptrchain/systemi/get-master-cg 可用；cg-* 通过 vtable 可用但通过 wrapper 失败 |
| Gen 10 | `/fmod/resolve-channel` (修复: native pattern 动态定位 + 3 参数签名)                                                                                                                                                                                                                                | 待测试                                                                                   |
