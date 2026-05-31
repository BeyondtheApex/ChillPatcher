# ChillPatcher & OmniMix 游戏 Mod 项目库

[English](README.md) | **简体中文**

欢迎！这个项目主要用于托管我为多款游戏开发的音频增强与功能拓展 Mod。目前已支持的修补游戏包括赛车游戏《Forza Horizon 6》（极限竞速：地平线6）以及自习音乐游戏《Chill with You : Lo-Fi Story》（放松时光：与你共享Lo-Fi故事）。

## 🎮 寻找安装方法? (fh6/chill with you)

- [🚀 简易安装](#quick-install)
- [🎮 游戏 Mod 介绍](#game-mods)

> [!WARNING]
> **⚠️ 免责声明与风险提示**
>
> - 由于本 Mod 涉及修改游戏资产及运行时内存，并不符合官方 EULA 规范。请在下载前知悉相关风险，因使用本项目而导致的任何不良后果（如封号、存档损坏等），需由使用者自行承担，与本项目及开发者无关。
> - 本项目的发布包与源代码中**不包含任何受版权保护的游戏资产**，对游戏资产的所有修改均在玩家本地通过程序补丁工具自动执行。
> - 本项目仅用于个人学习研究逆向工程、网络通信及流媒体平台交互技术，与各流媒体平台、游戏开发商及发行商无任何关系。

---

## 🎨 项目 Logo、GUI 样式与名称征集 (Contributions Welcome!)

由于本项目目前缺少官方 Logo，并且当前的平台和项目名称也是临时定的。此外，**目前的 GUI 客户端界面样式还比较简陋和粗糙（有点丑）**。**非常欢迎大家为本项目设计并提交 Logo、美化 GUI 的样式/主题风格，或者提出更有趣、更贴合的项目名称建议！**

- 如果您有设计好的 Logo，或者有美化后的 GUI CSS/样式主题，欢迎直接提交 Pull Request（图片可放置在 `img/` 目录下）或开 Issue 展示。
- 如果您对项目名称或界面设计有更好的点子，请随时在 Issue 中发帖讨论！

---

## 🧩 征集社区模块与游戏集成 (Contributions Welcome!)

除了视觉贡献，我们也热忱欢迎开发者贡献**新的音源模块**或**游戏集成**——SDK 设计轻量、解耦度高，极易上手。

### 🎵 新的音源流媒体模块

想把你的常用平台接入这个生态？使用 **OmniMixPlayer.SDK** 创建新模块——纯 C# 开发，基础音源无需原生代码。

### 🎮 新的游戏集成

想为你喜欢的游戏添加自定义音乐或音频功能？可以通过以下方式集成：

- **C# 集成（OmniMixPlayer.SDK）** — 引用 SDK，少量代码即可消费音频流。
- **原生 C ABI 集成（OmniPcmShared）** — 即插即用的共享内存读取器，适用于任何语言/框架（C++、Rust、Unity、Unreal 等）。

> 👉 参考 [🔌 开发者指南](#dev-guide)。

两个 SDK 都提供了**高度解耦**的架构——模块或游戏集成通过定义良好的接口和共享内存与后端通信，而非紧耦合。

---

<a id="quick-install"></a>

## 🚀 简易安装方式

如果您想安装这些 Mod，本平台提供了非常方便的一键部署方式：

1. 打开 **OmniMix 客户端** (Flutter GUI 客户端)。
2. 点击菜单中的 **“游戏集成”** 选项。
3. 在对应的游戏卡片中设置您的**游戏安装目录地址**。
4. 点击 **“安装”**，客户端将自动为您解压并部署所有必需的 Mod 组件与依赖项！

---

<a id="game-mods"></a>

## 🎮 游戏 Mod 介绍与快捷链接

- [**1. 《极限竞速：地平线 6》电台劫持 Mod**](#fh6-mod)
- [**2. 《Chill with You》Mod — ChillPatcher**](#chillpatcher-mod)

<a id="fh6-mod"></a>

### 1. 《极限竞速：地平线 6 (Forza Horizon 6)》电台劫持 Mod

针对《Forza Horizon 6》的 C++ 注入式 Mod，用于将自定义音乐电台完美融入到游戏驾驶体验中：

- **FMOD DSP 管道注入**：地平线 6 的注入逻辑基于开源项目 [fh6-universal-radio](https://github.com/g0ldyy/fh6-universal-radio) 的注入部分。通过 `version.dll` 劫持机制直接挂接 FMOD 声音引擎的 DSP 回调，拦截并重定向游戏内置的“Streamer Mode”（主播电台）。
- **多源定制歌单**：支持定制包含本地音频文件以及多种在线流媒体音频源（网易云、QQ音乐、B站等）的专属歌单。
- **完全控制与队列管理**：支持随时在播放队列中增加、排序或删除歌曲，允许完全控制播放进度与状态。
- **多端与浏览器远程控制**：除了支持 Flutter 桌面端程序控制外，还支持在局域网内通过手机等任何设备的浏览器进行远程点歌与播放控制。
- **极速共享内存通道**：直接通过命名共享内存读取 PCM 浮点音频流，无需本地回环网卡，零 CPU 开销、零延迟。
- **完美兼容游戏 UI**：游戏 HUD 的“正在播放”（Now Playing）浮窗、电台选择菜单、手柄操作及游戏音量设置完全保留。
- **自动化媒体生成**：配套的 CLI 工具会自动备份游戏文件，重构 FSB5 音频包（生成静音骨架），并将用户自定义的 PNG 图标编码压缩为 BC7 格式写入游戏 UI 包 `Anthem.zip` 中。

👉 **详细安装与部署说明请参阅**：[《FH6 Omni Bridge Mod 详细 README.md》](mods/ForzaHorizon6OmniBridge/README.md)

---

<a id="chillpatcher-mod"></a>

### 2. 《放松时光：与你共享Lo-Fi故事 (Chill with You : Lo-Fi Story)》Mod — ChillPatcher

针对 Unity 自习音乐游戏《Chill with You : Lo-Fi Story》的 BepInEx 插件。它在保留游戏原有温馨画面的基础上，全面升级了播放器和系统交互体验：

- **原生 FLAC 无损解码**：攻克 Unity 对 FLAC 运行时加载卡顿的限制，搭载专用原生 C++ 解码器，无损播放音轨并支持任意 Seek 进度。
- **无限本地文件夹歌单**：突破原版 100 首限制，根据硬盘目录递归识别生成“歌单-专辑-单曲”结构，自动提取专辑封面及标签元数据。
- **Wallpaper Engine 完美兼容**：提供纯离线运行模式，解除 Steam 进程依赖，在桌面上直接点击交互，支持成就本地缓存与云同步。
- **游戏内 RIME 中文输入法**：集成 RIME（中州韵）输入法引擎，配置全局键盘钩子，在壁纸模式下可从桌面直接向游戏内打字搜索。
- **OneJS + Preact 可视化 UI**：新增精美的游戏内 UI 窗口。提供歌词、天气、相机控制器等可拖拽小组件，支持 UI 热重载。
- **多人 P2P 联机“自习室”**（⚠️ **未完成 / 开发中**）：基于 Steam P2P 的协同自习室。采用“主机权威事件同步”消除延迟抖动，可隔离并协同编辑 Todo、日记、日历、打卡与关卡金币。

👉 **详细安装与使用说明请参阅**：[《ChillWithYou Mod 详细 README.md》](mods/chillPatcher/src/README.md)

---

## 🎛️ 后台流媒体音乐平台 (OmniMixPlayer)

为了让上述多个不同的游戏 Mod 能方便地获取多种网络和本地音源，本项目设计并实现了一个通用的后台音乐分发平台——**OmniMixPlayer**。

该平台由以下几部分组成：

1. **OmniMix 服务端 (`OmniMixPlayer.Backend`)**：运行在后台的轻量级媒体控制与音频流分发服务。
2. **Flutter GUI 客户端 (`gui_flutter`)**：提供可视化操作、音源扫码登录（网易云、QQ音乐、B站）与 Mod 一键安装部署。

### 🌟 平台的核心优势（方便接入游戏和软件）

- **低代码免解码嵌入**：当你想为游戏或者应用增加播放器功能时，**你不需要在你的软件/游戏中集成任何复杂的音频解码库**（如 FLAC、MP3、AAC 解码）。所有的网络连接、身份认证、API 请求、流媒体边下边播、甚至音频解码工作都由后台的 `OmniMixPlayer` 帮你搞定。
- **共享内存传输**：服务端解码完成后，直接将原始浮点 PCM 数据写入 Windows 命名共享内存中。宿主软件或游戏 Mod 仅需要几行代码就能从这片内存中读取音频帧直接播放，实现接近零延迟、低系统消耗的嵌入体验。
- **免去繁琐的 API 维护**：你的应用可以直接使用后台获取到的歌单、收藏夹和多平台音源，无需自己去适配网易云、QQ音乐或 B站的动态 API 接口。

---

<a id="dev-guide"></a>

## 🔌 开发者指南与链接

如果你是开发者，想要开发自己的音乐模块，或者想将此播放平台嵌入到你自己的游戏/软件中，请参考以下指南：

- **开发新的音源流媒体模块**：如果你希望为本平台接入新的网络音源（例如 Spotify、抖音等），可以参考：
  👉 [《OmniMixPlayer.SDK 模块开发指南》](OmniMixPlayer/OmniMixPlayer.SDK/README.md)
- **模块示例（参考实现）**：
  - 👉 [网易云音乐模块](OmniMixPlayer/modules/Netease/README.md) — 网易云音源，支持二维码登录、私人 FM、歌词
  - 👉 [QQ 音乐模块](OmniMixPlayer/modules/QQMusic/README.md) — QQ 音乐音源，支持二维码登录、多音质
  - 👉 [Bilibili 音乐模块](OmniMixPlayer/modules/Bilibili/README.md) — B站收藏夹集成，智能封面
  - 👉 [Spotify 模块](OmniMixPlayer/modules/Spotify/README.md) — Spotify OAuth 登录、Connect 控制、Rust 原生解码
  - 👉 [本地文件夹参考模块](OmniMixPlayer/modules/LocalFolder/README.md) — SDK 参考实现，演示文件扫描和注册流程
- **将音频流嵌入你自己的应用**：如果你希望在自己的 C++/C# 游戏或软件中接入这个播放通道，可以使用项目自带的客户端 SDK。它隐藏了复杂的环形缓冲区读写和同步逻辑：
  👉 [《OmniPcmShared 客户端嵌入接口说明》](NativePlugins/OmniPcmShared/README.md)

---

## ⚖️ 协议说明 (License)

- **项目整体**：本项目主要源码开源在 **GNU General Public License v3 (GPL-3.0)** 协议下。
- **客户端 SDK 嵌入部分 (`NativePlugins/OmniPcmShared`)**：为了方便广大开发者自由将音频流嵌入到各种闭源游戏、商业软件中，客户端嵌入 SDK 的代码采用 **MIT 协议**。这意味着您可以**不受 GPL 限制**地在任何软件和游戏里随意调用 `OmniPcmShared` 动态链接库来消费平台音源。
