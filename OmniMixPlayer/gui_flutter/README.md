# OmniMix GUI 客户端

OmniMixPlayer 的 Flutter 桌面 GUI 客户端，提供可视化操作界面。

## 主要功能

- **游戏集成管理** — 一键安装/部署 Mod 到游戏目录
- **音源扫码登录** — 网易云、QQ 音乐、Bilibili 二维码登录
- **可视化播放控制** — 歌单浏览、播放队列管理、播放状态控制
- **模块配置面板** — 各音乐源模块的图形化设置
- **多端远程控制** — 提供 Web 服务，支持手机浏览器局域网控制

## 构建

```bash
cd OmniMixPlayer/gui_flutter
flutter pub get
flutter build windows
```

需要 Flutter SDK 3.x+，Windows 桌面支持。

## 项目结构

```
gui_flutter/
├── lib/
│   ├── main.dart                  # 应用入口
│   ├── pages/                     # 页面
│   ├── widgets/                   # 组件
│   ├── services/                  # 后端通信服务
│   └── models/                    # 数据模型
├── pubspec.yaml                   # Flutter 依赖
└── README.md
```

## 后端通信

Flutter GUI 通过 HTTP REST API 和 WebSocket 与 `OmniMixPlayer.Backend` 通信：

- REST API: `http://localhost:<port>/api/...`
- WebSocket: `ws://localhost:<port>/ws`
- 端口配置: 自动发现或通过配置文件指定

## 相关文档

- [OmniMixPlayer.Backend](../OmniMixPlayer.Backend/) — 后端服务
- [OmniMixPlayer.SDK](../OmniMixPlayer.SDK/README.md) — 模块开发 SDK
