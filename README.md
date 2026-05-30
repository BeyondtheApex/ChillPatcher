# ChillPatcher & OmniMix Game Mod Repository

**English** | [简体中文](README_ZH.md)

Welcome! This repository hosts custom audio-enhancement and feature-extending mods developed for multiple games. Currently, it supports *Forza Horizon 6* and *Chill with You : Lo-Fi Story* (a lo-fi study/focus game).

> [!WARNING]
> **⚠️ Disclaimer & Risk Warning**
> *   Since this Mod involves modifying game assets and runtime memory, it does not comply with the official EULA specifications. Please be aware of the associated risks before downloading. Any adverse consequences resulting from using this project (such as account bans, save corruption, etc.) are solely the user's responsibility, and have no association with this project or its developers.
> *   This project's releases and source code **do not contain any copyrighted game assets**. All modifications to game assets are performed locally on the player's system by the automated patching utility.
> *   This project is intended strictly for personal educational research in reverse engineering, network communications, and streaming platform interactions. It has no affiliation with any streaming platforms, game developers, or publishers.

---

## 🎨 Call for Logos, GUI Styles, & Project Names (Contributions Welcome!)

Since this project currently lacks an official logo, the platform/project names are temporary, and **the current GUI client interface styles are rather plain/unpolished (kind of ugly)**: **You are highly encouraged to suggest project names, submit logo designs, or submit visual styling improvements/themes for the GUI!**
*   If you have a logo concept or custom CSS/theme files for improving the GUI styling, please open a Pull Request (placing images under the `img/` folder) or start an Issue to showcase it.
*   If you have suggestions for a better project name or visual layout design, please start a thread in the Issues section!

---

## 🚀 Quick Installation

To install these mods, this platform provides a very convenient one-click deployment method:
1. Open the **OmniMix Client App** (Flutter GUI client).
2. Click the **"Game Integration"** option in the menu.
3. Set your **game installation folder path** on the card of the target game.
4. Click **"Install"**, and the client will automatically download, unpack, and deploy all required mod components and dependencies!

---

## 🎮 Game Mods & Quick Links

### 1. "Forza Horizon 6" Radio Hijack Mod
A C++ dll-injection mod for *Forza Horizon 6* designed to bring custom streaming radio channels directly into your driving cockpit:
*   **FMOD DSP Injection**: The injection logic for Forza Horizon 6 is derived from the injection portion of the open-source project [fh6-universal-radio](https://github.com/g0ldyy/fh6-universal-radio). It hooks FMOD engine DSP callbacks via `version.dll` proxying, intercepting and hijacking the game's default "Streamer Mode" radio channel.
*   **Custom Playlists with Multiple Sources**: Supports custom playlists combining local audio files and various online streaming sources (NetEase, QQ Music, Bilibili, etc.).
*   **Full Control & Queue Management**: Allows you to add, sort, or remove tracks in the playback queue at any time, with full control over playback state and seek operations.
*   **Multi-Device & Browser Remote Control**: Beyond Flutter desktop app control, it supports remote track selection and playback controls via the local network using any browser (e.g., from a mobile phone).
*   **IPC Shared Memory Pipe**: Streams PCM audio frames directly from a Windows named shared memory mapping, skipping network overhead to deliver zero-latency, low-CPU audio.
*   **Seamless Game HUD Integration**: Fully retains game UI elements, such as the "Now Playing" HUD banner, radio selectors, gamepad inputs, and in-game volumes.
*   **Asset Generator CLI**: Automatically backs up original game files, generates silent FMOD FSB5 skeletons, and compresses PNG graphics into BC7 format inside the game's `Anthem.zip` HUD archive.

👉 **For deployment and generation instructions, refer to**: [《FH6 Omni Bridge Mod Detailed README.md》](file:///g:/Csharp/Chill/mods/ForzaHorizon6OmniBridge/README.md)

---

### 2. "Chill with You : Lo-Fi Story" Mod — ChillPatcher
A comprehensive BepInEx plugin for the Unity-based game *Chill with You : Lo-Fi Story*. While preserving the game's cozy lo-fi aesthetics, it upgrades the music player and system features:
*   **Native FLAC Playback**: Integrates a dedicated native C++ decoder (`NativePlugins/FlacDecoder`) based on `dr_flac` to bypass Unity runtime audio constraints. Enjoy lossless audio with low memory footprints and full seeking support.
*   **Recursive Folder Playlists**: Overcomes the default 100-song limit, generating a hierarchical "Playlist-Album-Song" catalog from your local directories. Automatically extracts album covers and metadata.
*   **Wallpaper Engine Support**: Provides a pure offline mode to skip Steam login constraints, enabling direct desktop clicks and caching achievements locally (synced back to Steam when launching online).
*   **In-game RIME IME**: Bundles the RIME input engine alongside a global keyboard hook, allowing you to search for songs in Chinese directly from your desktop.
*   **OneJS + Preact UI Overlay**: Adds a custom overlay supporting draggable widgets (Lyrics, Weather, Camera) and in-game hot-reloading for UI developers.
*   **Steam P2P Multiplayer "Study Room"** (⚠️ **Unfinished / Work-in-Progress**): Introduces collaborative co-working lobbies. Syncs character animations, Pomodoro timers, and isolates sandbox progress logs (Todos, Diaries, Memos, Habits, Level Economy).

👉 **For installation and detailed settings, refer to**: [《ChillWithYou Mod Detailed README.md》](file:///g:/Csharp/Chill/mods/chillPatcher/src/README.md)

---

## 🎛️ Behind the Scene: The OmniMixPlayer Streaming Platform

To feed games and mods with unified audio sources, the project utilizes a shared local streaming platform called **OmniMixPlayer**:
1.  **OmniMix Backend (`OmniMixPlayer.Backend`)**: A lightweight background ASP.NET Core service that manages streams and playlists.
2.  **Flutter GUI Client (`gui_flutter`)**: Provides account QR scans (NetEase, QQ Music, Bilibili), visual controls, and one-click mod installers.

### 🌟 Key Advantages for App/Game Developers

*   **Low-Code & Decoder-Free**: When embedding a media player into a game or software, **you do not need to compile any decoding libraries** (like FLAC, MP3, or AAC). Authentication, streaming, buffering, and decoding are entirely managed by the `OmniMixPlayer` service.
*   **Shared Memory Pipeline**: Decoded float PCM audio frames are pushed directly into a Windows named shared memory map. Client applications read frames with a few lines of code, achieving near-zero latency.
*   **Zero API Maintenance**: Retrieve playlists, favorites, and multi-platform media streams directly from the backend, avoiding the need to adapt or maintain dynamic API wrappers for NetEase, QQ Music, or Bilibili.

---

## 🔌 Developer Guidelines & References

If you are a developer looking to build custom audio modules or embed this streaming channel into your own apps, consult:

*   **Developing New Music Source Modules**: To add support for new online sources (e.g., Spotify, TikTok), see:
    👉 [《OmniMixPlayer.SDK Module Development Guide》](file:///g:/Csharp/Chill/OmniMixPlayer/OmniMixPlayer.SDK/README.md)
*   **Embedding Audio Streams Into Your App**: To consume streams from your C++/C# applications or game mods, see the client SDK which hides ring-buffer complexity:
    👉 [《OmniPcmShared Client SDK Reference》](file:///g:/Csharp/Chill/NativePlugins/OmniPcmShared/README.md)

---

## ⚖️ License

*   **Main Project**: The codebase is open-sourced under the **GNU General Public License v3 (GPL-3.0)**.
*   **Client SDK (`NativePlugins/OmniPcmShared`)**: To encourage developers to freely integrate these audio streams into closed-source or commercial games and software, the client integration SDK code is licensed under the **MIT License**. You can invoke the `OmniPcmShared` library **without copyleft restrictions**.
