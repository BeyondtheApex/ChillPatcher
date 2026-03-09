# 🖥️ 自定义 UI 系统

## 新增功能

### 图形化设置面板
- 无需手动编辑配置文件，所有设置均可在游戏内可视化修改
- 包含 IME 设置面板：实时调整模糊效果、候选词数量等参数

### 窗口管理器（桌面小组件）
- 全新的可拖拽/可缩放窗口系统
- 插件化架构：开发者可通过 `__registerPlugin` 注册自定义窗口插件
- 内置天气小组件（Open-Meteo API）
- 内置相机控制器：在 UI 层直接操控游戏相机

### 场景浏览器
- 运行时查看和调试游戏内 UI 元素树

### 自定义 DOM 元素
- `BlurPanel`：毛玻璃模糊面板（用于 IME 候选词等）
- `CameraView`：内嵌相机渲染视图
- `Canvas2D`：2D 画布（画线、形状等）

## 开发体验
- 游戏启动自动执行 npm install + esbuild 编译
- esbuild watch + 热重载：修改源码后自动重编译并刷新 UI，无需重启游戏
- 提供完整的 JS API（`chill.config`、`chill.ime`、`chill.ui`）
- TypeScript 类型定义（global.d.ts）

## 其他改进
- 键盘钩子/输入调度优化
- RIME 输入法引擎重构
- UI 实例配置版本管理机制
- 构建脚本更新
