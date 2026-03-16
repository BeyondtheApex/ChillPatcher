import { h } from "preact"
import { useState, useRef, useEffect } from "preact/hooks"

declare const CS: any

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
const PICKING_IGNORE = 1
const WINDOW_RADIUS = 20

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
    initialCompact?: boolean
    resizable?: boolean
    compact?: CompactDef
    hoverEnabled?: boolean
    hoverScale?: number
    hoverDuration?: number
    onFocus?: () => void
    onGeometryChange?: (x: number, y: number, w: number, h: number, isCompact: boolean) => void
    children?: any
}

// ---- Component ----
export const Window = ({
    title,
    width = 300,
    height = 400,
    initialX = 200,
    initialY = 100,
    initialCompact = false,
    resizable = false,
    compact,
    hoverEnabled = true,
    hoverScale = 1.03,
    hoverDuration = 0.4,
    onFocus,
    onGeometryChange,
    children,
}: WindowProps) => {
    const [pos, setPos] = useState({ x: initialX, y: initialY })
    const [normalSize, setNormalSize] = useState({
        w: Math.max(MIN_WIDTH, width),
        h: Math.max(MIN_HEIGHT, height),
    })
    const [isCompact, setIsCompact] = useState(initialCompact && !!compact)
    const [dockedEdge, setDockedEdge] = useState<string | null>(null)
    const drag = useRef({ active: false, ox: 0, oy: 0 })
    const resize = useRef({ active: false, ox: 0, oy: 0, ow: 0, oh: 0 })
    const [hovered, setHovered] = useState(false)
    const [interacting, setInteracting] = useState(false)
    const [grabHovered, setGrabHovered] = useState(false)
    const [snapping, setSnapping] = useState(false)
    const containerRef = useRef<any>(null)
    const snapTimer = useRef<any>(null)
    const skipDrag = useRef(false)
    const arcRef = useRef<any>(null)

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

    // ---- Computed ----
    const displaySize =
        isCompact && compact
            ? { w: compact.width, h: compact.height }
            : normalSize
    const canResize = resizable && !isCompact
    const showDragBar = grabHovered || drag.current.active

    const isActive = () => drag.current.active || resize.current.active

    const getCanvasSize = () => {
        try {
            const layout = containerRef.current?.ve?.layout
            return { w: layout.width, h: layout.height }
        } catch (_) {
            return { w: 1920, h: 1080 }
        }
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
        const next = !isCompact
        if (next) {
            setIsCompact(true)
        } else {
            setIsCompact(false)
            setDockedEdge(null)
        }
        // 延迟通知，等状态更新
        setTimeout(() => onGeometryChange?.(pos.x, pos.y, normalSize.w, normalSize.h, next), 50)
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

                if (nearLeft || nearRight || nearTop || nearBottom) {
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
                            sy = Math.max(
                                0,
                                Math.min(my - ch / 2, canvas.h - ch)
                            )
                            break
                        case "right":
                            sx = canvas.w - cw
                            sy = Math.max(
                                0,
                                Math.min(my - ch / 2, canvas.h - ch)
                            )
                            break
                        case "top":
                            sx = Math.max(
                                0,
                                Math.min(mx - cw / 2, canvas.w - cw)
                            )
                            sy = 0
                            break
                        case "bottom":
                            sx = Math.max(
                                0,
                                Math.min(mx - cw / 2, canvas.w - cw)
                            )
                            sy = canvas.h - ch
                            break
                    }

                    setPos({ x: sx, y: sy })
                    if (!isCompact) setIsCompact(true)
                    if (dockedEdge !== nearest.edge)
                        setDockedEdge(nearest.edge)
                    return
                }
            }

            // Normal drag
            setPos({
                x: mx - drag.current.ox,
                y: my - drag.current.oy,
            })
            if (isCompact && dockedEdge) {
                // Undock: recalculate drag offset for normal size
                setIsCompact(false)
                setDockedEdge(null)
                drag.current.ox = normalSize.w / 2
                drag.current.oy = DRAG_BAR_HEIGHT / 2
            }
        } else if (resize.current.active) {
            const dx = e.position.x - resize.current.ox
            const dy = e.position.y - resize.current.oy
            setNormalSize({
                w: Math.max(MIN_WIDTH, resize.current.ow + dx),
                h: Math.max(MIN_HEIGHT, resize.current.oh + dy),
            })
        }
    }

    const handleUp = () => {
        drag.current.active = false
        resize.current.active = false
        setInteracting(false)

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
        onGeometryChange?.(pos.x, pos.y, normalSize.w, normalSize.h, isCompact)
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
                            if (skipDrag.current) {
                                skipDrag.current = false
                                return
                            }
                            if (snapTimer.current) {
                                clearTimeout(snapTimer.current)
                                snapTimer.current = null
                                setSnapping(false)
                            }
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
                            {/* 精简模式切换按钮 / 拖动图标 */}
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
                                    }}
                                >
                                    ⠿
                                </div>
                            )}
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
