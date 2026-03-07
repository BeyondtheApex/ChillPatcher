# OneJS 开发踩坑记录

> OneJS 基于 Unity UI Toolkit + Preact + Puerts(V8)，表面上写法类似 React/Web 前端，但底层差异很大。以下是实际开发中踩过的坑。

---

## 1. 没有 `requestAnimationFrame`

OneJS 运行时默认**不提供** `requestAnimationFrame`。部分入口（如 Preloads/builtin.mjs）会定义一个 `setTimeout(..., 1)` 的 polyfill，但不保证在所有 entry point 加载。

- **现象**: `requestAnimationFrame is not defined` 运行时报错
- **解决**: 在文件顶部手动添加 polyfill：
  ```ts
  if (typeof requestAnimationFrame === "undefined") {
    (globalThis as any).requestAnimationFrame = (cb: Function) => setTimeout(cb, 1);
    (globalThis as any).cancelAnimationFrame = (id: any) => clearTimeout(id);
  }
  ```
- **注意**: 回调不是帧同步的，卸载组件后可能仍会执行。建议封装 hook 维护 `mounted` 标志。

---

## 2. 没有 `fetch` / HTTP API

OneJS 底层没有 `fetch`、`XMLHttpRequest` 或任何 HTTP 请求 API。

- **解决**: 在 C# 侧创建网络 API（使用 `UnityWebRequest` + `StaticCoroutine`），通过 Puerts 桥接暴露给 JS。
- **参考**: 本项目的 `JSApi/ChillNetApi.cs`，JS 端通过 `chill.net.get(url, callback)` 调用。

---

## 3. 没有 SVG 支持

UI Toolkit 没有 SVG 相关元素。`<svg>`、`<path>`、`<circle>` 等标签**完全不支持**。

- **影响**: lucide-react、heroicons、react-icons 等 SVG 图标库全部不可用。
- **替代方案**:
  - Unicode 符号（☀ ☁ ☂ ❄ ⚡ 等）
  - TTF 图标字体
  - PNG/纹理图片

---

## 4. 没有 CSS `@keyframes` 动画

UI Toolkit 不支持 `@keyframes` 和 `animation` 属性。framer-motion、react-spring 等动画库完全不可用。

**可用的动画方式:**
- CSS `transition` + 状态驱动（改变 style → 自动过渡）
- `requestAnimationFrame` 逐帧更新（需 polyfill）

---

## 5. `rgba(r,g,b,0)` 透明色 BUG

这是最坑的一个。`rgba(255,255,255,0)` 理论上是完全透明，但 OneJS/USS 的颜色解析器会将其渲染为**不透明白色**。

- **现象**: 设置 `backgroundColor: "rgba(255,255,255,0)"` 后出现白色色块
- **原因**: USS 颜色解析器对 alpha=0 的处理有 bug
- **解决**: 不要用 `rgba` 设透明背景。不需要背景色时，直接不设置 `backgroundColor` 属性。

```tsx
// ❌ 错误
style={{ backgroundColor: isHovered ? "rgba(255,255,255,0.1)" : "rgba(255,255,255,0)" }}

// ✅ 正确
style={{ ...(isHovered ? { backgroundColor: "rgba(255,255,255,0.1)" } : {}) }}
```

---

## 6. CSS `transition` 简写可能解析失败

`transition: "scale 0.4s ease-out"` 简写形式可能解析出错。

**推荐分开声明:**
```tsx
style={{
  transitionProperty: "translate, opacity",
  transitionDuration: "0.2s, 0.3s",
  transitionTimingFunction: "ease-out, ease-out",
}}
```

注意 `transitionProperty` 中使用 **kebab-case**（如 `background-color` 而非 `backgroundColor`）。

---

## 7. 样式枚举值首字母大写

USS 枚举值与 CSS 不同，需要**首字母大写**或特定格式：

| CSS 值 | OneJS/USS 值 |
|--------|-------------|
| `display: flex` | `display: "Flex"` |
| `position: absolute` | `position: "Absolute"` |
| `align-items: center` | `alignItems: "Center"` |
| `justify-content: space-between` | `justifyContent: "SpaceBetween"` |
| `flex-direction: row` | `flexDirection: "Row"` |
| `align-items: flex-start` | `alignItems: "Flex-Start"` |

---

## 8. 默认 `flexDirection` 是 Column

Web CSS 的默认 `flex-direction` 是 `row`，但 UI Toolkit 的默认是 **`Column`**（垂直排列）。

如果需要水平排列子元素，必须显式声明 `flexDirection: "Row"`。

---

## 9. 渐变、阴影和滤镜限制

| 特性 | 支持情况 |
|------|---------|
| `linear-gradient()` | ❌ 不支持（需 GradientRect 自定义元素） |
| `box-shadow` | ❌ 不支持 |
| `backdrop-filter: blur()` | ❌ 不支持 |
| `filter: blur()` | ⚠️ 仅 Unity 6.0.3+ |
| `border-radius` | ✅ 支持 |
| `opacity` | ✅ 支持 |

---

## 10. 文本样式差异

| Web CSS | OneJS/USS |
|---------|-----------|
| `font-weight: bold` | `unityFontStyleAndWeight: "Bold"` |
| `text-align: center` | `unityTextAlign: "MiddleCenter"` |
| `font-size: 16px` | `fontSize: 16` |
| `letter-spacing: 2px` | `letterSpacing: 2` |

---

## 11. 事件系统

**支持的事件:**
- `onPointerDown` / `onPointerUp` / `onPointerMove`
- `onPointerEnter` / `onPointerLeave`
- `onMouseEnter` / `onMouseLeave`
- `onClick`

**不支持:**
- HTML5 拖放 API（`onDrag` / `onDrop` / `onDragStart`）
- 需手动用 pointer 事件实现拖拽

---

## 12. `translate` 用于 hover 效果比 `backgroundColor` 更安全

由于透明色 bug（见第 5 条），hover 效果用位移比用背景色变化更可靠：

```tsx
style={{
  translate: isHovered ? "0 -3" : "0 0",
  transitionProperty: "translate",
  transitionDuration: "0.2s",
  transitionTimingFunction: "ease-out",
}}
```

---

## 13. Preact Hook 可用性

| Hook | 可用 |
|------|------|
| `useState` | ✅ |
| `useEffect` | ✅ |
| `useRef` | ✅ |
| `useMemo` / `useCallback` | ✅ |
| `useErrorBoundary` | ✅ |
| `useContext` | ✅ |

`useErrorBoundary` 可以捕获子组件的渲染错误，**推荐在根组件使用**。

---

## 总结

OneJS 写起来像 React，但运行在 Unity UI Toolkit 上。核心差异：
1. **没有浏览器 API**（fetch、RAF、SVG、DOM）
2. **样式是 USS 不是 CSS**（枚举大写、属性名不同、颜色解析有 bug）
3. **动画只有 transition**（无 keyframes、无动画库）
4. **默认布局是纵向**（flexDirection 默认 Column）
