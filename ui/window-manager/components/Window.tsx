import { h } from "preact"
import { useState, useRef, useEffect } from "preact/hooks"

declare const CS: any
declare const chill: any

// ---- Layout constants ----
const GRAB_ZONE_HEIGHT = 30
const GRAB_PILL_WIDTH = 40
const GRAB_PILL_HEIGHT = 4
const DRAG_BAR_HEIGHT = GRAB_ZONE_HEIGHT
const COLLAPSED_RADIUS = GRAB_PILL_HEIGHT / 2
const EXPANDED_RADIUS = (DRAG_BAR_HEIGHT + DRAG_BAR_HEIGHT) / 2
const ARC_RADIUS = 14
const ARC_THICKNESS = 3
const ARC_CAP_R = ARC_THICKNESS / 2
const ARC_HANDLE_PAD = Math.ceil(ARC_CAP_R)
const ARC_HANDLE_SIZE = ARC_RADIUS + ARC_HANDLE_PAD
const RESIZE_MARGIN = 6
const MIN_WIDTH = 120
const MIN_HEIGHT = 80
const EDGE_THRESHOLD = 60
const HYSTERESIS = 20 // 防抖区间：脱离阈值 = EDGE_THRESHOLD + HYSTERESIS
const PICKING_IGNORE = 1
const WINDOW_RADIUS = 20
const PERSISTENCE_INTERVAL = 5000 // 定时刷写间隔：5秒

// ---- Types ----
export interface CompactDef {
    width: number
    height: number
    component: any
}

export interface WindowProps {
    title: string
    width?: number
    height?: number
    initialX?: number
    initialY?: number
    resizable?: boolean
    canClose?: boolean
    compact?: CompactDef
    hoverEnabled?: boolean
    hoverScale?: number
    hoverDuration?: number
    onFocus?: () => void
    onGeometryChange?: (x: number, y: number, w: number, h: number) => void
    visible?: boolean
    onClose?: () => void
    children?: any
}

// ---- Component ----
export const Window = ({
    title,
    width = 300,
    height = 400,
    initialX = 200,
    initialY = 100,
    resizable = false,
    canClose = true,
    compact,
    hoverEnabled = true,
    hoverScale = 1.03,
    hoverDuration = 0.4,
    onFocus,
    onGeometryChange,
    visible = true,
    onClose,
    children,
}: WindowProps) => {
    const [pos, setPos] = useState({ x: initialX, y: initialY })
    const [normalSize, setNormalSize] = useState({
        w: Math.max(MIN_WIDTH, width),
        h: Math.max(MIN_HEIGHT, height),
    })
    const [isCompact, setIsCompact] = useState(false)
    const [dockedEdge, setDockedEdge] = useState<string | null>(null)
    const drag = useRef({ active: false, ox: 0, oy: 0 })
    const resize = useRef({ active: false, ox: 0, oy: 0, ow: 0, oh: 0 })
    // 拖动期间锁定的 canvas size
    const lockedCanvasSize = useRef<{ w: number; h: number } | null>(null)
    // 拖动期间锁定的窗口尺寸
    const lockedWindowSize = useRef<{ w: number; h: number } | null>(null)
    const [hovered, setHovered] = useState(false)
    const [interacting, setInteracting] = useState(false)
    const [grabHovered, setGrabHovered] = useState(false)
    const [snapping, setSnapping] = useState(false)
    const containerRef = useRef<any>(null)
    const snapTimer = useRef<any>(null)
    const skipDrag = useRef(false)
    const arcRef = useRef<any>(null)
    
    // 持久化相关
    const persistenceTimer = useRef<any>(null)
    const lastPersistedState = useRef<string>("")
    const isStateDirty = useRef(false)

    // Draw arc via Canvas2D Painter2D
    useEffect(() => {
        const el = arcRef.current
        if (!el?.ve || !canResize) return
        const ve = el.ve as any
        
        const cx = ARC_THICKNESS / 2
        const cy = ARC_THICKNESS / 2

        const R = ARC_HANDLE_SIZE - ARC_THICKNESS
        
        ve.ClearCommands()
        
        // 纯白色
        ve.SetStrokeColor("rgb(255, 255, 255)")
        ve.SetLineWidth(ARC_THICKNESS)

        ve.SetLineCap(1) 
        
        ve.BeginPath()

        ve.Arc(cx, cy, R, 0, 90)
        
        ve.Stroke()
        ve.Commit()
    })
    
    // 获取窗口状态标识符
    const getWindowStateId = () => `window-${title.replace(/\s+/g, '-')}`
    
    // 获取窗口状态
    const getWindowState = () => ({
        id: getWindowStateId(),
        x: pos.x,
        y: pos.y,
        width: normalSize.w,
        height: normalSize.h,
        isCompact,
        dockedEdge,
        timestamp: Date.now()
    })
    
    // 保存窗口状态到文件
    const persistWindowState = () => {
        try {
            const state = getWindowState()
            const stateJson = JSON.stringify(state)
            
            // 避免重复写入相同状态
            if (stateJson === lastPersistedState.current) return
            
            const stateDir = "window-states"
            const stateFile = `${stateDir}/${getWindowStateId()}.json`
            
            // 确保目录存在
            if (!chill.io.exists(stateDir)) {
                // 无法直接创建目录，使用写入文件的方式间接创建
                chill.io.writeText(`${stateDir}/.keep`, "")
            }
            
            // 写入状态文件
            chill.io.writeText(stateFile, stateJson)
            lastPersistedState.current = stateJson
            isStateDirty.current = false
            
            console.log(`[Window] 状态已持久化: ${title}`)
        } catch (e) {
            console.error(`[Window] 状态持久化失败: ${title}`, e)
        }
    }
    
    // 加载窗口状态
    const loadWindowState = () => {
        try {
            const stateFile = `window-states/${getWindowStateId()}.json`
            if (!chill.io.exists(stateFile)) return false
            
            const stateJson = chill.io.readText(stateFile)
            if (!stateJson) return false
            
            const state = JSON.parse(stateJson)
            
            // 应用状态
            if (typeof state.x === 'number' && typeof state.y === 'number') {
                setPos({ x: state.x, y: state.y })
            }
            if (typeof state.width === 'number' && typeof state.height === 'number') {
                setNormalSize({ 
                    w: Math.max(MIN_WIDTH, state.width), 
                    h: Math.max(MIN_HEIGHT, state.height) 
                })
            }
            if (typeof state.isCompact === 'boolean') {
                setIsCompact(state.isCompact)
            }
            if (state.dockedEdge) {
                setDockedEdge(state.dockedEdge)
            }
            
            console.log(`[Window] 状态已加载: ${title}`)
            return true
        } catch (e) {
            console.error(`[Window] 状态加载失败: ${title}`, e)
            return false
        }
    }
    
    // 标记状态为脏（需要持久化）
    const markStateDirty = () => {
        isStateDirty.current = true
    }
    
    // 定时刷写持久化文件
    useEffect(() => {
        persistenceTimer.current = setInterval(() => {
            if (isStateDirty.current) {
                persistWindowState()
            }
        }, PERSISTENCE_INTERVAL)
        
        return () => {
            if (persistenceTimer.current) {
                clearInterval(persistenceTimer.current)
            }
        }
    }, [])
    
    // 组件挂载时加载状态
    useEffect(() => {
        loadWindowState()
    }, [])

    // ---- Computed ----
    const displaySize =
        isCompact && compact
            ? { w: compact.width, h: compact.height }
            : normalSize
    const canResize = resizable && !isCompact
    const showDragBar = grabHovered || drag.current.active

    const isActive = () => drag.current.active || resize.current.active

    const getCanvasSize = () => {
        // 拖动期间使用锁定的 canvas size
        if (lockedCanvasSize.current) {
            return lockedCanvasSize.current
        }
        
        try {
            // 使用屏幕的实际尺寸，而不是窗口的布局尺寸
            if (typeof chill !== 'undefined' && chill.screen) {
                return { w: chill.screen.width || 1920, h: chill.screen.height || 1080 }
            }
            return { w: 1920, h: 1080 }
        } catch (_) {
            return { w: 1920, h: 1080 }
        }
    }
    
    // 锁定 canvas size（拖动开始时调用）
    const lockCanvasSize = () => {
        try {
            if (typeof chill !== 'undefined' && chill.screen) {
                lockedCanvasSize.current = { 
                    w: chill.screen.width || 1920, 
                    h: chill.screen.height || 1080 
                }
            } else {
                lockedCanvasSize.current = { w: 1920, h: 1080 }
            }
        } catch (_) {
            lockedCanvasSize.current = { w: 1920, h: 1080 }
        }
    }
    
    // 解锁 canvas size（拖动结束时调用）
    const unlockCanvasSize = () => {
        lockedCanvasSize.current = null
    }
    const bringToFront = () => {
        try {
            containerRef.current?.ve?.BringToFront()
        } catch (_) {}
    }

    const focus = () => {
        bringToFront()
        onFocus?.()
    }

    // ---- Compact toggle ----
    const toggleCompact = () => {
        if (!compact) return
        if (isCompact) {
            setIsCompact(false)
            setDockedEdge(null)
        } else {
            setIsCompact(true)
        }
        markStateDirty() // 标记状态为脏
    }

    // ---- Event handlers ----
    const handleMove = (e: any) => {
        if (drag.current.active) {
            const mx = e.position.x
            const my = e.position.y

            // Edge proximity detection (only if compact mode available)
            if (compact) {
                const canvas = getCanvasSize()
                const nearLeft = mx < EDGE_THRESHOLD
                const nearRight = mx > canvas.w - EDGE_THRESHOLD
                const nearTop = my < EDGE_THRESHOLD
                const nearBottom = my > canvas.h - EDGE_THRESHOLD

                // 使用防抖区间：脱离阈值 = EDGE_THRESHOLD + HYSTERESIS
                const detachThreshold = EDGE_THRESHOLD + HYSTERESIS
                
                // 如果当前已经吸附到边缘
                if (dockedEdge) {
                    // 检查是否需要脱离边缘
                    const shouldDetach = 
                        (dockedEdge === "left" && mx > detachThreshold) ||
                        (dockedEdge === "right" && mx < canvas.w - detachThreshold) ||
                        (dockedEdge === "top" && my > detachThreshold) ||
                        (dockedEdge === "bottom" && my < canvas.h - detachThreshold)

                    if (shouldDetach) {
                        const cw = compact.width
                        const ch = compact.height
                        
                        // 脱离边缘时，让拖动柄中心跟随鼠标
                        // 拖动柄在窗口顶部水平居中，垂直位置在 GRAB_ZONE_HEIGHT / 2
                        // 所以鼠标相对于窗口的位置是：水平居中，垂直在拖动柄中心
                        const newX = Math.max(0, Math.min(mx - cw / 2, canvas.w - cw))
                        const newY = Math.max(0, Math.min(my - GRAB_ZONE_HEIGHT / 2, canvas.h - ch))
                        
                        setPos({ x: newX, y: newY })
                        // 更新偏移量，让鼠标在拖动柄中心
                        drag.current.ox = cw / 2
                        drag.current.oy = GRAB_ZONE_HEIGHT / 2
                        // 清除停靠状态
                        setDockedEdge(null)
                        return
                    } else {
                        // 保持吸附状态，只沿边缘滑动
                        const cw = compact.width
                        const ch = compact.height
                        
                        switch (dockedEdge) {
                            case "left":
                                // 在左侧边缘，只改变垂直位置
                                setPos({
                                    x: 0,
                                    y: Math.max(
                                        0,
                                        Math.min(my - GRAB_ZONE_HEIGHT / 2, canvas.h - ch)
                                    )
                                })
                                break
                            case "right":
                                // 在右侧边缘，只改变垂直位置
                                setPos({
                                    x: canvas.w - cw,
                                    y: Math.max(
                                        0,
                                        Math.min(my - GRAB_ZONE_HEIGHT / 2, canvas.h - ch)
                                    )
                                })
                                break
                            case "top":
                                // 在顶部边缘，只改变水平位置
                                setPos({
                                    x: Math.max(
                                        0,
                                        Math.min(mx - cw / 2, canvas.w - cw)
                                    ),
                                    y: 0
                                })
                                break
                            case "bottom":
                                // 在底部边缘，只改变水平位置
                                setPos({
                                    x: Math.max(
                                        0,
                                        Math.min(mx - cw / 2, canvas.w - cw)
                                    ),
                                    y: canvas.h - ch
                                })
                                break
                        }
                        return
                    }
                }
                
                // 没有吸附时的普通拖动
                // 检查是否需要吸附到边缘（使用更小的阈值避免频繁吸附）
                const attachThreshold = EDGE_THRESHOLD
                const shouldAttach = 
                    nearLeft || nearRight || nearTop || nearBottom
                
                if (shouldAttach) {
                    const edges: { edge: string; dist: number }[] = []
                    if (nearLeft) edges.push({ edge: "left", dist: mx })
                    if (nearRight)
                        edges.push({ edge: "right", dist: canvas.w - mx })
                    if (nearTop) edges.push({ edge: "top", dist: my })
                    if (nearBottom)
                        edges.push({ edge: "bottom", dist: canvas.h - my })
                    const nearest = edges.sort((a, b) => a.dist - b.dist)[0]

                    const cw = compact.width
                    const ch = compact.height
                    
                    let sx = 0,
                        sy = 0
                    switch (nearest.edge) {
                        case "left":
                            sx = 0
                            // 垂直位置：让鼠标在拖动手柄中心，手柄在窗口顶部
                            sy = Math.max(
                                0,
                                Math.min(my - GRAB_ZONE_HEIGHT / 2, canvas.h - ch)
                            )
                            break
                        case "right":
                            sx = canvas.w - cw
                            // 垂直位置：让鼠标在拖动手柄中心
                            sy = Math.max(
                                0,
                                Math.min(my - GRAB_ZONE_HEIGHT / 2, canvas.h - ch)
                            )
                            break
                        case "top":
                            // 水平位置：让鼠标在拖动手柄中心
                            sx = Math.max(
                                0,
                                Math.min(mx - cw / 2, canvas.w - cw)
                            )
                            sy = 0
                            break
                        case "bottom":
                            // 水平位置：让鼠标在拖动手柄中心
                            sx = Math.max(
                                0,
                                Math.min(mx - cw / 2, canvas.w - cw)
                            )
                            sy = canvas.h - ch
                            break
                    }

                    setPos({ x: sx, y: sy })
                    // 重新计算拖动偏移量，以保持鼠标与窗口的相对位置
                    drag.current.ox = mx - sx
                    drag.current.oy = my - sy
                    if (!isCompact) setIsCompact(true)
                    setDockedEdge(nearest.edge)
                    return
                }
            }

            // Normal drag
            setPos({
                x: mx - drag.current.ox,
                y: my - drag.current.oy,
            })
            markStateDirty() // 标记状态为脏
        } else if (resize.current.active) {
            const dx = e.position.x - resize.current.ox
            const dy = e.position.y - resize.current.oy
            setNormalSize({
                w: Math.max(MIN_WIDTH, resize.current.ow + dx),
                h: Math.max(MIN_HEIGHT, resize.current.oh + dy),
            })
            markStateDirty() // 标记状态为脏
        }
    }

    const handleUp = (e?: any) => {
        // 释放指针捕获
        if (e?.target?.releasePointerCapture && e.pointerId !== undefined) {
            try { e.target.releasePointerCapture(e.pointerId) } catch (_) {}
        }

        drag.current.active = false
        resize.current.active = false
        setInteracting(false)
        
        // 解锁 canvas size
        unlockCanvasSize()
        // 解锁窗口尺寸
        lockedWindowSize.current = null

        // Snap back if not docked and outside bounds
        if (!dockedEdge) {
            const canvas = getCanvasSize()
            const cx = Math.max(
                0,
                Math.min(pos.x, canvas.w - displaySize.w)
            )
            const cy = Math.max(
                0,
                Math.min(pos.y, canvas.h - displaySize.h)
            )
            if (cx !== pos.x || cy !== pos.y) {
                setSnapping(true)
                setPos({ x: cx, y: cy })
                if (snapTimer.current) clearTimeout(snapTimer.current)
                snapTimer.current = setTimeout(() => setSnapping(false), 350)
            }
        }
        onGeometryChange?.(pos.x, pos.y, normalSize.w, normalSize.h)
        
        // 立即持久化状态
        markStateDirty()
        persistWindowState()
    }
    const r = WINDOW_RADIUS
    const borderRadii = !dockedEdge
        ? {
              borderTopLeftRadius: r,
              borderTopRightRadius: r,
              borderBottomRightRadius: r,
              borderBottomLeftRadius: r,
          }
        : dockedEdge === "left"
          ? {
                borderTopLeftRadius: 0,
                borderTopRightRadius: r,
                borderBottomRightRadius: r,
                borderBottomLeftRadius: 0,
            }
          : dockedEdge === "right"
            ? {
                  borderTopLeftRadius: r,
                  borderTopRightRadius: 0,
                  borderBottomRightRadius: 0,
                  borderBottomLeftRadius: r,
              }
            : dockedEdge === "top"
              ? {
                    borderTopLeftRadius: 0,
                    borderTopRightRadius: 0,
                    borderBottomRightRadius: r,
                    borderBottomLeftRadius: r,
                }
              : {
                    borderTopLeftRadius: r,
                    borderTopRightRadius: r,
                    borderBottomRightRadius: 0,
                    borderBottomLeftRadius: 0,
                }

    // ---- Border width: hide docked-side border to eliminate 1px gap ----
    const borderWidths = !dockedEdge
        ? { borderTopWidth: 1, borderRightWidth: 1, borderBottomWidth: 1, borderLeftWidth: 1 }
        : dockedEdge === "left"
          ? { borderTopWidth: 1, borderRightWidth: 1, borderBottomWidth: 1, borderLeftWidth: 0 }
          : dockedEdge === "right"
            ? { borderTopWidth: 1, borderRightWidth: 0, borderBottomWidth: 1, borderLeftWidth: 1 }
            : dockedEdge === "top"
              ? { borderTopWidth: 0, borderRightWidth: 1, borderBottomWidth: 1, borderLeftWidth: 1 }
              : { borderTopWidth: 1, borderRightWidth: 1, borderBottomWidth: 0, borderLeftWidth: 1 }

    // ---- Render ----
    if (!visible) return null

    return (
        <div
            ref={containerRef}
            picking-mode={PICKING_IGNORE}
            style={{
                position: "Absolute",
                top: 0,
                left: 0,
                right: 0,
                bottom: 0,
            }}
        >
            {/* 全屏覆盖层 - 拖拽/缩放时捕获窗口外事件 */}
            {interacting && (
                <div
                    style={{
                        position: "Absolute",
                        top: 0,
                        left: 0,
                        right: 0,
                        bottom: 0,
                    }}
                    onPointerMove={handleMove}
                    onPointerUp={handleUp}
                />
            )}
            {/* 窗口 */}
            <div
                style={{
                    position: "Absolute",
                    left: pos.x,
                    top: pos.y,
                    width: displaySize.w,
                    height: displaySize.h,
                    ...borderRadii,
                    ...borderWidths,
                    borderColor: "rgba(255,255,255,0.1)",
                    flexDirection: "Column",
                    display: "Flex",
                    overflow: "Hidden",
                    scale: hoverEnabled && hovered ? hoverScale : 1.0,
                    transitionProperty: snapping ? "scale, left, top" : "scale",
                    transitionDuration: snapping
                        ? `${hoverDuration}s, 0.3s, 0.3s`
                        : `${hoverDuration}s`,
                    transitionTimingFunction: "ease-out",
                }}
                onPointerEnter={() => setHovered(true)}
                onPointerLeave={() => {
                    if (!isActive()) setHovered(false)
                }}
                onPointerDown={() => focus()}
                onPointerMove={handleMove}
                onPointerUp={handleUp}
            >
                {/* 内容区 */}
                <div
                    style={{
                        flexGrow: 1,
                        display: "Flex",
                        flexDirection: "Column",
                        overflow: "Hidden",
                    }}
                >
                    {isCompact && compact ? <compact.component /> : children}
                </div>

                {/* 拖拽手柄裁剪容器 */}
                <div
                    style={{
                        position: "Absolute",
                        top: 0,
                        left: 0,
                        right: 0,
                        height: DRAG_BAR_HEIGHT,
                        overflow: "Hidden",
                    }}
                    picking-mode={PICKING_IGNORE}
                >
                    {/* 拖拽手柄药丸 */}
                    <div
                        style={{
                            position: "Absolute",
                            top: showDragBar
                                ? 0
                                : (DRAG_BAR_HEIGHT - GRAB_PILL_HEIGHT) / 2,
                            left: showDragBar
                                ? -EXPANDED_RADIUS
                                : (displaySize.w - GRAB_PILL_WIDTH) / 2,
                            width: showDragBar
                                ? displaySize.w + EXPANDED_RADIUS * 2
                                : GRAB_PILL_WIDTH,
                            height: showDragBar
                                ? DRAG_BAR_HEIGHT + EXPANDED_RADIUS
                                : GRAB_PILL_HEIGHT,
                            borderRadius: showDragBar
                                ? EXPANDED_RADIUS
                                : COLLAPSED_RADIUS,
                            backgroundColor: showDragBar
                                ? "rgba(20,20,34,0.85)"
                                : "rgba(255,255,255,0.25)",
                            overflow: "Hidden",
                            transitionProperty:
                                "top, left, width, height, border-radius, background-color",
                            transitionDuration: "0.25s",
                            transitionTimingFunction: "ease-out",
                        }}
                        onPointerEnter={() => setGrabHovered(true)}
                        onPointerLeave={() => {
                            if (!isActive()) setGrabHovered(false)
                        }}
                        onPointerDown={(e: any) => {
                            // 锁定指针到当前元素
                            if (e.target?.setPointerCapture) {
                                e.target.setPointerCapture(e.pointerId)
                            }

                            if (skipDrag.current) {
                                skipDrag.current = false
                                return
                            }
                            if (snapTimer.current) {
                                clearTimeout(snapTimer.current)
                                snapTimer.current = null
                                setSnapping(false)
                            }
                            // 锁定 canvas size，防止拖动期间变化
                            lockCanvasSize()
                            // 锁定窗口尺寸，防止插件在拖动期间修改窗口大小
                            lockedWindowSize.current = { w: displaySize.w, h: displaySize.h }
                            drag.current = {
                                active: true,
                                ox: e.position.x - pos.x,
                                oy: e.position.y - pos.y,
                            }
                            setInteracting(true)
                            focus()
                        }}
                    >
                        {/* 标题内容 */}
                        <div
                            style={{
                                position: "Absolute",
                                top: 0,
                                left: showDragBar ? 14 + EXPANDED_RADIUS : 0,
                                right: showDragBar ? 14 + EXPANDED_RADIUS : 0,
                                height: DRAG_BAR_HEIGHT,
                                flexDirection: "Row",
                                display: "Flex",
                                alignItems: "Center",
                                justifyContent: "SpaceBetween",
                                opacity: showDragBar ? 1 : 0,
                                transitionProperty: "opacity, left, right",
                                transitionDuration: "0.15s",
                            }}
                        >
                            <div style={{ fontSize: 12, color: "#89b4fa" }}>
                                {title}
                            </div>
                            {/* 右侧按钮组 */}
                            <div style={{ flexDirection: "Row", display: "Flex", alignItems: "Center" }}>
                                {/* 精简模式切换按钮 */}
                                {compact ? (
                                    <div
                                        style={{
                                            fontSize: 13,
                                            color: isCompact
                                                ? "#a6e3a1"
                                                : "#6c7086",
                                            paddingLeft: 6,
                                            paddingRight: 2,
                                            paddingTop: 2,
                                            paddingBottom: 2,
                                        }}
                                        onPointerDown={() => {
                                            skipDrag.current = true
                                            toggleCompact()
                                        }}
                                    >
                                        {isCompact ? "" : ""}
                                    </div>
                                ) : (
                                    <div
                                        style={{
                                            fontSize: 11,
                                            color: "#6c7086",
                                            paddingLeft: 6,
                                            paddingRight: 2,
                                            paddingTop: 2,
                                            paddingBottom: 2,
                                        }}
                                    >
                                        ⠿
                                    </div>
                                )}
                                {/* 关闭按钮 */}
                                {canClose && (
                                    <div
                                        style={{
                                            fontSize: 14,
                                            color: "#f38ba8",
                                            paddingLeft: 2,
                                            paddingRight: 4,
                                            paddingTop: 2,
                                            paddingBottom: 2,
                                            marginLeft: 2,
                                        }}
                                        onPointerDown={() => {
                                            skipDrag.current = true
                                            onClose?.()
                                        }}
                                    >
                                        ✕
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                </div>

                {/* 缩放手柄 - 右下角四分之一圆弧 */}
                {canResize && (
                    <div
                        style={{
                            position: "Absolute",
                            right: RESIZE_MARGIN - ARC_HANDLE_PAD,
                            bottom: RESIZE_MARGIN - ARC_HANDLE_PAD,
                            width: ARC_HANDLE_SIZE,
                            height: ARC_HANDLE_SIZE,
                        }}
                        onPointerDown={(e: any) => {
                            // 锁定指针到当前元素
                            if (e.target?.setPointerCapture) {
                                e.target.setPointerCapture(e.pointerId)
                            }

                            resize.current = {
                                active: true,
                                ox: e.position.x,
                                oy: e.position.y,
                                ow: normalSize.w,
                                oh: normalSize.h,
                            }
                            setInteracting(true)
                            focus()
                        }}
                    >
                        <canvas-2d
                            ref={arcRef}
                            style={{
                                position: "Absolute",
                                top: 0,
                                left: 0,
                                width: ARC_HANDLE_SIZE,
                                height: ARC_HANDLE_SIZE,
                                overflow: "Hidden",
                            }}
                            picking-mode={PICKING_IGNORE}
                        />
                    </div>
                )}
            </div>
        </div>
    )
}
