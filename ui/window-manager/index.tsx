import { h, Fragment, render } from "preact"
import { useState, useEffect, useErrorBoundary } from "preact/hooks"
import { Window } from "./components/Window"

declare const chill: any
declare const CS: any

// ---- RAF polyfill ----
if (typeof (globalThis as any).requestAnimationFrame === "undefined") {
    ;(globalThis as any).requestAnimationFrame = (cb: (t: number) => void) =>
        setTimeout(
            () =>
                cb(
                    typeof CS !== "undefined"
                        ? CS.UnityEngine.Time.realtimeSinceStartupAsDouble * 1000
                        : Date.now()
                ),
            1
        )
    ;(globalThis as any).cancelAnimationFrame = (id: number) =>
        clearTimeout(id)
}

// ---- Plugin types ----
interface PluginDef {
    id: string
    title: string
    width?: number
    height?: number
    initialX?: number
    initialY?: number
    resizable?: boolean
    compact?: {
        width: number
        height: number
        component: any
    }
    component: any
    onGeometryChange?: (x: number, y: number, w: number, h: number) => void
}

// ---- Plugin registry ----
const pluginRegistry: PluginDef[] = []
let _refreshPlugins: (() => void) | null = null

;(globalThis as any).__registerPlugin = (def: PluginDef) => {
    pluginRegistry.push(def)
    console.log(`[WindowManager] Plugin registered: ${def.id}`)
}

;(globalThis as any).__unregisterPlugin = (id: string) => {
    const idx = pluginRegistry.findIndex(p => p.id === id)
    if (idx >= 0) pluginRegistry.splice(idx, 1)
}

;(globalThis as any).__refreshPlugins = () => {
    _refreshPlugins?.()
}

// ---- Load plugins from @outputs/plugins/ ----
function loadPlugins() {
    try {
        const base = String(chill.io.basePath).replace(/\\/g, "/").replace(/\/$/, "")
        const wd = String(chill.workingDir).replace(/\\/g, "/").replace(/\/$/, "")
        const relPrefix = wd.startsWith(base) ? wd.substring(base.length + 1) : wd
        const pluginsRel = relPrefix + "/@outputs/plugins"

        if (!chill.io.exists(pluginsRel)) return

        const dirs: string[] = JSON.parse(chill.io.listDirs(pluginsRel))
        for (const dirName of dirs) {
            try {
                chill.evalFile(`@outputs/plugins/${dirName}/app.js`)
            } catch (e) {
                console.error(`[WM] Failed to load plugin '${dirName}':`, e)
            }
        }
    } catch (e) {
        console.error("[WM] Plugin discovery failed:", e)
    }
}

// ---- Error boundary ----
const ErrorBoundary = ({ children }: { children?: any }) => {
    const [error, resetError] = useErrorBoundary((err) => {
        console.error("[WindowManager] Error boundary caught:", err)
    })
    if (error) {
        return (
            <div
                style={{
                    flexGrow: 1,
                    justifyContent: "Center",
                    alignItems: "Center",
                    display: "Flex",
                    flexDirection: "Column",
                    backgroundColor: "#1e293b",
                    paddingLeft: 20,
                    paddingRight: 20,
                }}
            >
                <div style={{ fontSize: 14, color: "#f87171", marginBottom: 8 }}>
                    插件渲染出错
                </div>
                <div
                    style={{
                        fontSize: 11,
                        color: "rgba(255,255,255,0.5)",
                        marginBottom: 16,
                        unityTextAlign: "MiddleCenter",
                    }}
                >
                    {String(error)}
                </div>
                <div
                    style={{
                        fontSize: 12,
                        color: "#89b4fa",
                        paddingTop: 6,
                        paddingBottom: 6,
                        paddingLeft: 16,
                        paddingRight: 16,
                        borderRadius: 6,
                        borderWidth: 1,
                        borderColor: "#89b4fa",
                    }}
                    onPointerDown={() => resetError()}
                >
                    重置
                </div>
            </div>
        )
    }
    return children
}

// ---- Plugin enable config ----
const pluginEnabledCache: Record<string, boolean> = {}

function isPluginEnabled(id: string): boolean {
    if (!(id in pluginEnabledCache)) {
        pluginEnabledCache[id] = chill.config.appGetOrCreate(
            `Plugin.${id}.Enabled`, true,
            `是否启用 ${id} 小组件 (重启生效)`)
    }
    return pluginEnabledCache[id]
}

// ---- Window state persistence (via chill.config) ----
interface WindowState {
    x: number
    y: number
    w: number
    h: number
    compact: boolean
    dockedEdge: string
}

function getWindowState(id: string, defaults: { x: number, y: number, w: number, h: number }): WindowState {
    return {
        x: chill.config.appGetOrCreate(`Window.${id}.X`, defaults.x, `${id} 窗口 X 坐标`),
        y: chill.config.appGetOrCreate(`Window.${id}.Y`, defaults.y, `${id} 窗口 Y 坐标`),
        w: chill.config.appGetOrCreate(`Window.${id}.W`, defaults.w, `${id} 窗口宽度`),
        h: chill.config.appGetOrCreate(`Window.${id}.H`, defaults.h, `${id} 窗口高度`),
        compact: chill.config.appGetOrCreate(`Window.${id}.Compact`, false, `${id} 窗口是否为紧凑模式`),
        dockedEdge: chill.config.appGetOrCreate(`Window.${id}.DockedEdge`, "", `${id} 窗口吸附边缘 (left/right/top/bottom/空)`),
    }
}

function updateWindowState(id: string, x: number, y: number, w: number, h: number, compact: boolean, dockedEdge: string | null) {
    try {
        chill.config.appSet(`Window.${id}.X`, Math.round(x))
        chill.config.appSet(`Window.${id}.Y`, Math.round(y))
        chill.config.appSet(`Window.${id}.W`, Math.round(w))
        chill.config.appSet(`Window.${id}.H`, Math.round(h))
        chill.config.appSet(`Window.${id}.Compact`, compact)
        chill.config.appSet(`Window.${id}.DockedEdge`, dockedEdge || "")
    } catch (e) {
        console.error(`[WM] Failed to save state for ${id}:`, e)
    }
}

// ---- Hover effect config ----
const hoverEnabled = chill.config.appGetOrCreate("HoverEffect.Enabled", true, "是否启用窗口 hover 放大效果")
const hoverScale = chill.config.appGetOrCreate("HoverEffect.Scale", 1.03, "hover 放大倍数 (1.0 = 无放大)")
const hoverDuration = chill.config.appGetOrCreate("HoverEffect.Duration", 0.4, "hover 动画时长 (秒)")

// ---- App ----
const App = () => {
    const [plugins, setPlugins] = useState<PluginDef[]>([])

    useEffect(() => {
        loadPlugins()
        const enabled = pluginRegistry.filter(p => isPluginEnabled(p.id))
        setPlugins(enabled)
        _refreshPlugins = () => setPlugins(pluginRegistry.filter(p => isPluginEnabled(p.id)))
        console.log(`[WM] Loaded ${pluginRegistry.length} plugin(s), enabled ${enabled.length}`)
        return () => { _refreshPlugins = null }
    }, [])

    return (
        <>
            {plugins.map((p) => {
                const saved = getWindowState(p.id, {
                    x: p.initialX ?? 200,
                    y: p.initialY ?? 100,
                    w: p.width ?? 300,
                    h: p.height ?? 400,
                })
                return (
                    <Window
                        key={p.id}
                        title={p.title}
                        width={saved.w}
                        height={saved.h}
                        initialX={saved.x}
                        initialY={saved.y}
                        initialCompact={saved.compact}
                        initialDockedEdge={saved.dockedEdge || null}
                        resizable={p.resizable}
                        compact={p.compact}
                        hoverEnabled={hoverEnabled}
                        hoverScale={hoverScale}
                        hoverDuration={hoverDuration}
                        onGeometryChange={(x, y, w, h, isCompact, dockedEdge) => {
                            updateWindowState(p.id, x, y, w, h, isCompact, dockedEdge)
                            p.onGeometryChange?.(x, y, w, h)
                        }}
                    >
                        <ErrorBoundary>
                            <p.component />
                        </ErrorBoundary>
                    </Window>
                )
            })}
        </>
    )
}

render(<App />, document.body)
