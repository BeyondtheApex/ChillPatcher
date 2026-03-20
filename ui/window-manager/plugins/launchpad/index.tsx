import { h } from "preact"
import { useEffect, useState, useMemo, useCallback, useRef } from "preact/hooks"

declare const __registerPlugin: any
declare const __wmPluginControl: any
declare const chill: any

interface PluginItem {
    id: string
    title: string
    enabled: boolean
    launcher: {
        text: string
        background: string
    }
}

const BG = "#0b1020"
const CARD = "#111827"
const TEXT = "#e5e7eb"
const DIM = "#94a3b8"
const ACCENT = "#7dd3fc"

interface LaunchpadConfig {
    blur: boolean
    iconScale: number
    compactScale: number
    maxItems: number
}

const defaultCfg: LaunchpadConfig = { blur: true, iconScale: 1, compactScale: 1, maxItems: 12 }

function normalizeCfg(raw: Partial<LaunchpadConfig>): LaunchpadConfig {
    return {
        blur: raw.blur !== false,
        iconScale: Math.max(0.7, Math.min(1.8, Number(raw.iconScale) || defaultCfg.iconScale)),
        compactScale: Math.max(0.6, Math.min(1.8, Number(raw.compactScale) || defaultCfg.compactScale)),
        maxItems: Math.max(4, Math.min(48, Number(raw.maxItems) || defaultCfg.maxItems)),
    }
}

function getRootRel() {
    const base = String(chill.io.basePath).replace(/\\/g, "/").replace(/\/$/, "")
    const wd = String(chill.workingDir).replace(/\\/g, "/").replace(/\/$/, "")
    return wd.startsWith(base + "/") ? wd.substring(base.length + 1) : "ui/window-manager"
}

const cfgPath = `${getRootRel()}/state/launchpad-config.json`

// 全局状态，用于compact getter访问当前items和配置
const globalState = {
    items: [] as PluginItem[],
    cfg: loadCfg(),
}

// 立即初始化items（在模块加载时）
try {
    const all = (__wmPluginControl?.listPlugins?.() || []) as PluginItem[]
    globalState.items = all.filter(p => p.id !== "launchpad").sort((a, b) => a.title.localeCompare(b.title))
} catch {
    // 如果__wmPluginControl还未可用，保持空数组
}

// 计算compact尺寸的函数
function calcCompactSize(items: PluginItem[], cfg: LaunchpadConfig) {
    const scale = cfg.compactScale
    const edgePadding = 10
    const topPadding = Math.round(edgePadding * 3.5)
    const shown = items.slice(0, cfg.maxItems)
    const innerWidth = shown.length * (42 * scale) + Math.max(0, shown.length - 1) * 8
    return {
        w: Math.max(120, Math.min(900, 20 + innerWidth)),
        h: Math.round(topPadding + (42 * scale) + edgePadding)
    }
}

function loadCfg(): LaunchpadConfig {
    try {
        if (!chill.io.exists(cfgPath)) return defaultCfg
        const raw = JSON.parse(chill.io.readText(cfgPath) || "{}")
        return normalizeCfg(raw || {})
    } catch {
        return defaultCfg
    }
}

function saveCfg(cfg: LaunchpadConfig) {
    chill.io.writeText(cfgPath, JSON.stringify(cfg, null, 2))
}

// 声明全局函数
declare const __refreshPlugins: any

// 使用 useRef 存储窗口尺寸，避免每次渲染都更新全局变量
const usePluginItems = () => {
    const [items, setItems] = useState<PluginItem[]>([])
    
    const refresh = useCallback(() => {
        const all = (__wmPluginControl?.listPlugins?.() || []) as PluginItem[]
        const filtered = all.filter(p => p.id !== "launchpad").sort((a, b) => a.title.localeCompare(b.title))
        setItems(filtered)
        // 更新全局状态，以便compact getter可以访问
        globalState.items = filtered
        // 通知窗口管理器重新渲染，以便更新compact尺寸
        try {
            if (typeof __refreshPlugins === "function") {
                __refreshPlugins()
            }
        } catch {}
    }, [])
    
    useEffect(() => {
        refresh()
        const off = __wmPluginControl?.subscribe?.(refresh)
        return () => { if (typeof off === "function") off() }
    }, [refresh])
    
    const toggle = useCallback((id: string) => {
        __wmPluginControl?.togglePluginVisible?.(id)
    }, [])
    
    return { items, toggle }
}

const LaunchpadCompact = () => {
    const { items, toggle } = usePluginItems()
    const [cfg] = useState(loadCfg)
    const compactSizeRef = useRef<{ w: number; h: number }>({ w: 420, h: 87 })
    
    const scale = cfg.compactScale
    const edgePadding = 10
    const topPadding = Math.round(edgePadding * 3.5)

    // 更新全局状态，以便compact getter可以访问
    useEffect(() => {
        globalState.items = items
        globalState.cfg = cfg
    }, [items, cfg])

    // 使用 useMemo 缓存计算结果，只在 items.length 或 scale 变化时重新计算
    const compactSize = useMemo(() => {
        const shown = items.slice(0, cfg.maxItems)
        const innerWidth = shown.length * (42 * scale) + Math.max(0, shown.length - 1) * 8
        return {
            w: Math.max(120, Math.min(900, 20 + innerWidth)),
            h: Math.round(topPadding + (42 * scale) + edgePadding)
        }
    }, [items.length, scale, cfg.maxItems, topPadding, edgePadding])

    // 只在 compactSize 变化时更新 ref 和窗口尺寸
    useEffect(() => {
        compactSizeRef.current = compactSize
        try {
            const win = (document.body as any)?.firstChild
            if (win?.style) {
                win.style.width = compactSize.w
                win.style.height = compactSize.h
            }
        } catch {}
    }, [compactSize])

    // 使用 useMemo 缓存 shown 列表，避免每次渲染都重新 slice
    const shown = useMemo(() => items.slice(0, cfg.maxItems), [items, cfg.maxItems])

    // 使用 useCallback 缓存点击处理函数
    const handleItemClick = useCallback((id: string) => {
        toggle(id)
    }, [toggle])

    const content = useMemo(() => (
        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Row", alignItems: "Center", paddingLeft: edgePadding, paddingRight: edgePadding, paddingTop: topPadding, paddingBottom: edgePadding, overflow: "Hidden" }}>
            {shown.map((item, index) => (
                <div 
                    key={item.id} 
                    onPointerDown={() => handleItemClick(item.id)} 
                    style={{ 
                        width: 42 * scale, 
                        height: 42 * scale, 
                        borderRadius: 10 * scale, 
                        backgroundColor: item.launcher.background, 
                        marginRight: index === shown.length - 1 ? 0 : 8, 
                        display: "Flex", 
                        justifyContent: "Center", 
                        alignItems: "Center", 
                        fontSize: 16 * scale, 
                        color: "#fff", 
                        opacity: item.enabled ? 1 : 0.35 
                    }}
                >
                    {item.launcher.text}
                </div>
            ))}
        </div>
    ), [shown, scale, edgePadding, topPadding, handleItemClick])

    return cfg.blur
        ? <blur-panel downsample={1} blur-iterations={4} interval={1} tint="#ffffff1a" style={{ flexGrow: 1, display: "Flex", backgroundColor: CARD }}>{content}</blur-panel>
        : <div style={{ flexGrow: 1, display: "Flex", backgroundColor: CARD }}>{content}</div>
}

const LaunchpadPanel = () => {
    const { items, toggle } = usePluginItems()
    const [cfg, setCfg] = useState(loadCfg)
    const [showCfg, setShowCfg] = useState(false)

    const patchCfg = useCallback((partial: Partial<LaunchpadConfig>) => {
        const next = normalizeCfg({ ...cfg, ...partial })
        setCfg(next)
        saveCfg(next)
    }, [cfg])

    const toggleShowCfg = useCallback(() => setShowCfg(prev => !prev), [])

    const shown = useMemo(() => items.slice(0, cfg.maxItems), [items, cfg.maxItems])
    const stepBtnStyle = { fontSize: 11, color: "#cbd5e1", marginLeft: 6, marginRight: 6 }

    const iconSize = 56 * cfg.iconScale; 
    const marginSize = 10 * cfg.iconScale;

    // 使用 useCallback 缓存点击处理函数
    const handleItemClick = useCallback((id: string) => {
        toggle(id)
    }, [toggle])

    const content = useMemo(() => showCfg ? (
        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", color: TEXT, fontSize: 11, padding: 10 }}>
            <div style={{ marginBottom: 8 }}>Launchpad 设置</div>
            <div onPointerDown={() => patchCfg({ blur: !cfg.blur })} style={{ marginBottom: 6, color: cfg.blur ? "#86efac" : DIM }}>毛玻璃: {cfg.blur ? "开" : "关"}</div>
            <div style={{ marginBottom: 6, display: "Flex", flexDirection: "Row", alignItems: "Center" }}>图标缩放 <div onPointerDown={() => patchCfg({ iconScale: Math.round((cfg.iconScale - 0.1) * 10) / 10 })} style={stepBtnStyle}>-</div> {cfg.iconScale.toFixed(1)} <div onPointerDown={() => patchCfg({ iconScale: Math.round((cfg.iconScale + 0.1) * 10) / 10 })} style={stepBtnStyle}>+</div></div>
            <div style={{ marginBottom: 6, display: "Flex", flexDirection: "Row", alignItems: "Center" }}>折叠缩放 <div onPointerDown={() => patchCfg({ compactScale: Math.round((cfg.compactScale - 0.1) * 10) / 10 })} style={stepBtnStyle}>-</div> {cfg.compactScale.toFixed(1)} <div onPointerDown={() => patchCfg({ compactScale: Math.round((cfg.compactScale + 0.1) * 10) / 10 })} style={stepBtnStyle}>+</div></div>
            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>最多展示 <div onPointerDown={() => patchCfg({ maxItems: cfg.maxItems - 1 })} style={stepBtnStyle}>-</div> {cfg.maxItems} <div onPointerDown={() => patchCfg({ maxItems: cfg.maxItems + 1 })} style={stepBtnStyle}>+</div></div>
        </div>
    ) : (
        <div style={{ 
            width: "100%", 
            height: "100%", 
            display: "Flex", 
            flexDirection: "Row", 
            flexWrap: "Wrap", 
            justifyContent: "FlexStart", // 修改点：从左侧开始对齐
            alignContent: "Center",      // 垂直方向依然保持整体居中
            overflow: "Hidden" 
        }}>
            {shown.map((item) => (
                <div 
                    key={item.id} 
                    onPointerDown={() => handleItemClick(item.id)} 
                    style={{ 
                        width: iconSize, 
                        height: iconSize,
                        margin: marginSize, 
                        borderRadius: 14 * cfg.iconScale, 
                        backgroundColor: item.launcher.background, 
                        display: "Flex", 
                        justifyContent: "Center", 
                        alignItems: "Center",
                        opacity: item.enabled ? 1 : 0.35,
                    }}
                >
                    <div style={{ 
                        color: "#fff", 
                        fontSize: 28 * cfg.iconScale, 
                        display: "Flex", 
                        justifyContent: "Center", 
                        alignItems: "Center" 
                    }}>
                        {item.launcher.text}
                    </div>
                </div>
            ))}
        </div>
    ), [showCfg, cfg, shown, iconSize, marginSize, patchCfg, handleItemClick])

    return (
        <div
            style={{
                flexGrow: 1,
                display: "Flex",
                flexDirection: "Column",
                backgroundColor: BG,
                paddingLeft: 12,
                paddingRight: 12,
                paddingTop: 10,
                paddingBottom: 10,
            }}
        >
            <div style={{ fontSize: 12, color: ACCENT, marginBottom: 8, unityFontStyleAndWeight: "Bold", display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" }}>
                <div>Launchpad</div>
                <div onPointerDown={toggleShowCfg} style={{ color: "#cbd5e1" }}>{showCfg ? "完成" : "设置"}</div>
            </div>
            {content}
        </div>
    )
}

__registerPlugin({
    id: "launchpad",
    title: "Launchpad",
    width: 560,
    height: 220,
    initialX: 80,
    initialY: 120,
    resizable: true,
    canClose: false,
    launcher: {
        text: "",
        background: "#0ea5e9",
    },
    compact: {
        get width() { 
            try {
                // 如果items为空，立即尝试获取
                if (globalState.items.length === 0) {
                    try {
                        const all = (__wmPluginControl?.listPlugins?.() || []) as PluginItem[]
                        globalState.items = all.filter(p => p.id !== "launchpad").sort((a, b) => a.title.localeCompare(b.title))
                    } catch {}
                }
                const size = calcCompactSize(globalState.items, globalState.cfg)
                return size.w
            } catch {
                return 420
            }
        },
        get height() { 
            try {
                // 如果items为空，立即尝试获取
                if (globalState.items.length === 0) {
                    try {
                        const all = (__wmPluginControl?.listPlugins?.() || []) as PluginItem[]
                        globalState.items = all.filter(p => p.id !== "launchpad").sort((a, b) => a.title.localeCompare(b.title))
                    } catch {}
                }
                const size = calcCompactSize(globalState.items, globalState.cfg)
                return size.h
            } catch {
                return 87
            }
        },
        component: LaunchpadCompact,
    },
    component: () => <LaunchpadPanel />,
})