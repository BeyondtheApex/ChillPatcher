import { h } from "preact"
import { useState, useEffect, useCallback } from "preact/hooks"
import { theme } from "./theme"
import { parse } from "./utils"
import { Pagination } from "./Pagination"

declare const chill: any

interface NodeInfo {
    name: string
    path?: string
    active: boolean
    activeInHierarchy?: boolean
    childCount: number
    alpha?: number
    interactables?: string[]
}

const ITEMS_PER_PAGE = 8

export const UIExplorerPanel = () => {
    // null = root level (scene roots)
    const [currentPath, setCurrentPath] = useState<string | null>(null)
    const [children, setChildren] = useState<NodeInfo[]>([])
    const [page, setPage] = useState(0)
    const [error, setError] = useState<string | null>(null)

    const refresh = useCallback(() => {
        try {
            setError(null)
            if (currentPath === null) {
                // Root level: get scene root objects
                const json = chill.ui.getRoots() as string
                const roots = parse<NodeInfo[]>(json) || []
                setChildren(roots.map(r => ({ ...r, path: r.name })))
            } else {
                // Get children of current path using getTree depth=1
                const json = chill.ui.getTree(currentPath, 1) as string
                const node = parse<any>(json)
                if (!node) {
                    setError(`路径不存在: ${currentPath}`)
                    setChildren([])
                    return
                }
                const kids: NodeInfo[] = (node.children || []).map((c: any) => ({
                    name: c.name,
                    path: c.path,
                    active: c.active,
                    activeInHierarchy: c.activeInHierarchy,
                    childCount: c.childCount || 0,
                    alpha: c.alpha,
                    interactables: c.interactables,
                }))
                setChildren(kids)
            }
        } catch (e) {
            setError(String(e))
            setChildren([])
        }
    }, [currentPath])

    useEffect(() => {
        refresh()
        setPage(0)
    }, [currentPath])

    const navigateTo = (path: string) => {
        setCurrentPath(path)
    }

    const navigateUp = () => {
        if (currentPath === null) return
        const idx = currentPath.lastIndexOf("/")
        if (idx <= 0) {
            setCurrentPath(null)
        } else {
            setCurrentPath(currentPath.substring(0, idx))
        }
    }

    const toggleNode = (node: NodeInfo) => {
        try {
            const path = node.path || node.name
            chill.ui.setActive(path, !node.active)
            refresh()
        } catch (e) {
            console.error("Toggle error:", e)
        }
    }

    // Breadcrumb path segments
    const pathSegments = currentPath ? currentPath.split("/") : []
    const totalPages = Math.max(1, Math.ceil(children.length / ITEMS_PER_PAGE))
    const pageItems = children.slice(page * ITEMS_PER_PAGE, (page + 1) * ITEMS_PER_PAGE)

    return (
        <div style={{ flexDirection: "Column", display: "Flex", flexGrow: 1 }}>
            {/* Breadcrumb navigation */}
            <div style={{
                flexDirection: "Row",
                display: "Flex",
                flexWrap: "Wrap",
                alignItems: "Center",
                marginBottom: 8,
                paddingTop: 4,
                paddingBottom: 4,
                paddingLeft: 8,
                paddingRight: 8,
                backgroundColor: theme.bgPanel,
                borderRadius: theme.radius,
                minHeight: 28,
            }}>
                <div
                    style={{
                        fontSize: 12,
                        color: currentPath === null ? theme.text : theme.accent,
                        paddingRight: 4,
                    }}
                    onClick={() => setCurrentPath(null)}
                >
                    {" Scene"}
                </div>
                {pathSegments.map((seg, i) => {
                    const segPath = pathSegments.slice(0, i + 1).join("/")
                    const isLast = i === pathSegments.length - 1
                    return (
                        <div key={i} style={{ flexDirection: "Row", display: "Flex", alignItems: "Center" }}>
                            <div style={{ fontSize: 12, color: theme.textMuted, paddingLeft: 2, paddingRight: 2 }}>
                                {"/"}
                            </div>
                            <div
                                style={{
                                    fontSize: 12,
                                    color: isLast ? theme.text : theme.accent,
                                    paddingLeft: 2,
                                    paddingRight: 2,
                                }}
                                onClick={() => { if (!isLast) navigateTo(segPath) }}
                            >
                                {seg}
                            </div>
                        </div>
                    )
                })}
            </div>

            {/* Toolbar */}
            <div style={{
                flexDirection: "Row",
                display: "Flex",
                marginBottom: 6,
                alignItems: "Center",
            }}>
                <div
                    style={{
                        fontSize: 12,
                        color: theme.accent,
                        paddingTop: 4,
                        paddingBottom: 4,
                        paddingLeft: 8,
                        paddingRight: 8,
                        backgroundColor: theme.bgCard,
                        borderRadius: 4,
                        marginRight: 8,
                        display: currentPath !== null ? "Flex" : "None",
                    }}
                    onClick={navigateUp}
                >
                    {" 返回上级"}
                </div>
                <div
                    style={{
                        fontSize: 12,
                        color: theme.accent,
                        paddingTop: 4,
                        paddingBottom: 4,
                        paddingLeft: 8,
                        paddingRight: 8,
                        backgroundColor: theme.bgCard,
                        borderRadius: 4,
                    }}
                    onClick={refresh}
                >
                    {"󰑓 刷新"}
                </div>
                <div style={{ fontSize: 11, color: theme.textMuted, marginLeft: 8 }}>
                    {`共 ${children.length} 项`}
                </div>
            </div>

            {/* Error display */}
            <div style={{
                fontSize: 11,
                color: theme.danger,
                padding: 8,
                backgroundColor: "#2a0000",
                borderRadius: 4,
                marginBottom: 6,
                display: error ? "Flex" : "None",
            }}>
                {error || ""}
            </div>

            {/* Node list - always render ITEMS_PER_PAGE slots with stable keys */}
            {Array.from({ length: ITEMS_PER_PAGE }).map((_, i) => {
                const node = pageItems[i]
                return (
                    <div key={`slot-${i}`} style={{
                        height: NODE_ROW_HEIGHT,
                        marginBottom: 3,
                    }}>
                        {node && (
                            <NodeRow
                                node={node}
                                onNavigate={() => navigateTo(node.path || node.name)}
                                onToggle={() => toggleNode(node)}
                            />
                        )}
                    </div>
                )
            })}

            <div style={{
                fontSize: 12,
                color: theme.textMuted,
                padding: 12,
                textAlign: "Center",
                display: children.length === 0 && !error ? "Flex" : "None",
            }}>
                {"（空）"}
            </div>

            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </div>
    )
}

const NODE_ROW_HEIGHT = 48

const NodeRow = ({ node, onNavigate, onToggle }: {
    node: NodeInfo
    onNavigate: () => void
    onToggle: () => void
}) => {
    const isActive = node.active !== false
    const hasChildren = node.childCount > 0

    return (
        <div style={{
            flexDirection: "Row",
            display: "Flex",
            alignItems: "Center",
            backgroundColor: theme.bgCard,
            borderRadius: 4,
            paddingLeft: 10,
            paddingRight: 10,
            marginBottom: 3,
            height: NODE_ROW_HEIGHT,
        }}>
            {/* Checkbox for toggle visibility */}
            <div
                style={{
                    width: 20,
                    height: 20,
                    borderWidth: 2,
                    borderColor: isActive ? theme.accent : theme.textMuted,
                    borderRadius: 4,
                    marginRight: 8,
                    justifyContent: "Center",
                    alignItems: "Center",
                    display: "Flex",
                    backgroundColor: isActive ? theme.accentDark : "transparent",
                    flexShrink: 0,
                }}
                onClick={onToggle}
            >
                {isActive && (
                    <div style={{ fontSize: 12, color: theme.textBright }}>{"✓"}</div>
                )}
            </div>

            {/* Name - clickable to navigate if has children */}
            <div
                style={{
                    flexGrow: 1,
                    flexDirection: "Column",
                    display: "Flex",
                    overflow: "Hidden",
                }}
                onClick={hasChildren ? onNavigate : undefined}
            >
                <div style={{
                    fontSize: 13,
                    color: hasChildren ? theme.accent : theme.text,
                    overflow: "Hidden",
                }}>
                    {(hasChildren ? " " : " ") + node.name}
                </div>
                {/* Extra info row */}
                <div style={{
                    flexDirection: "Row",
                    display: "Flex",
                    marginTop: 2,
                }}>
                    {hasChildren && (
                        <div style={{ fontSize: 10, color: theme.textMuted, marginRight: 8 }}>
                            {`${node.childCount} 子节点`}
                        </div>
                    )}
                    {node.alpha !== undefined && node.alpha < 1 && (
                        <div style={{ fontSize: 10, color: theme.warning, marginRight: 8 }}>
                            {`α=${(node.alpha as number).toFixed(2)}`}
                        </div>
                    )}
                    {node.interactables && node.interactables.length > 0 && (
                        <div style={{ fontSize: 10, color: theme.success }}>
                            {node.interactables.join(", ")}
                        </div>
                    )}
                </div>
            </div>

            {/* Navigate arrow */}
            {hasChildren && (
                <div
                    style={{
                        fontSize: 14,
                        color: theme.accent,
                        paddingLeft: 8,
                        paddingRight: 4,
                        flexShrink: 0,
                    }}
                    onClick={onNavigate}
                >
                    {"›"}
                </div>
            )}
        </div>
    )
}
