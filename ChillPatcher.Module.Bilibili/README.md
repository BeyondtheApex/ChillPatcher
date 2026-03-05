# ChillPatcher - Bilibili Music Module

这是一个为 ChillPatcher 开发的 Bilibili 音乐扩展模块。它允许你直接登录 Bilibili 账号，同步收藏夹，并播放其中音乐

## ✨ 主要功能

* **🎧 流式播放**：利用 FFmpeg 实时流式传输音频，无需等待完整下载。
* **📂 大歌单支持**：支持1000 首歌曲的大型收藏夹，自动分页加载。
* **🖼️ 智能封面**：自动将收藏夹中第一个视频的封面设置为歌单封面，美观直观。
* **🔓 二维码登录**：内置二维码登录流程。
* **⚙️ 自定义配置**：支持在 BepInEx 配置文件中调整加载延迟。

## 🛠️ 安装要求

本模块依赖 **FFmpeg** 进行音频解码。

1.  下载 **FFmpeg (Static Version)**：[官网下载](https://ffmpeg.org/download.html) 或 [GitHub Releases](https://github.com/BtbN/FFmpeg-Builds/releases) (推荐 `ffmpeg-master-latest-win64-gpl.zip`)。
2.  解压后，找到 `bin` 文件夹下的 **`ffmpeg.exe`**。
3.  将 `ffmpeg.exe` 放入本模块的文件夹中：
    `.../BepInEx/plugins/ChillPatcher/modules/com.chillpatcher.bilibili/ffmpeg.exe`

## ⚙️ 配置文件

首次运行游戏后，会在 `BepInEx/config/` 下生成 `com.chillpatcher.plugin.cfg` 文件。你可以在此调整参数：

```ini
[Module:com.chillpatcher.bilibili]

## 翻页加载延迟(毫秒)。过低可能导致412错误，建议保持在300以上。
# Setting type: Int32
# Default value: 300
PageLoadDelay = 300

`````

## 注意

加载收藏夹较慢，请耐心等待