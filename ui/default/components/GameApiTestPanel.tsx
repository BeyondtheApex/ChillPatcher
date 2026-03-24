import { h } from "preact"
import { useState, useEffect, useCallback } from "preact/hooks"
import { theme } from "./theme"
import { parse } from "./utils"
import { Pagination } from "./Pagination"

declare const chill: any

// ─── Tree node definition ───
interface TreeNode {
    id: string
    label: string
    children?: TreeNode[]
    /** leaf action: call this and show result */
    action?: () => any
    /** for toggle switches */
    toggle?: { get: () => boolean; set: (v: boolean) => void }
}

const ITEMS_PER_PAGE = 8

export const GameApiTestPanel = () => {
    const [expanded, setExpanded] = useState<Record<string, boolean>>({})
    const [result, setResult] = useState<string | null>(null)
    const [resultKey, setResultKey] = useState("")
    const [page, setPage] = useState(0)

    const toggle = (id: string) => {
        setExpanded(prev => ({ ...prev, [id]: !prev[id] }))
    }

    const run = (label: string, action: () => any) => {
        try {
            const r = action()
            const text = typeof r === "string" ? r : JSON.stringify(r, null, 2)
            setResult(text ?? "void")
            setResultKey(label)
        } catch (e) {
            setResult(`Error: ${e}`)
            setResultKey(label)
        }
    }

    const tree = buildTree()
    const flat = flattenVisible(tree, expanded)
    const totalPages = Math.max(1, Math.ceil(flat.length / ITEMS_PER_PAGE))
    const pageItems = flat.slice(page * ITEMS_PER_PAGE, (page + 1) * ITEMS_PER_PAGE)

    return (
        <div style={{ flexDirection: "Column", display: "Flex", flexGrow: 1 }}>
            {/* Result bar */}
            {result !== null && (
                <div style={{
                    backgroundColor: theme.bgCard,
                    borderRadius: theme.radius,
                    paddingTop: 8,
                    paddingBottom: 8,
                    paddingLeft: 12,
                    paddingRight: 12,
                    marginBottom: 8,
                    maxHeight: 120,
                    overflow: "Hidden",
                }}>
                    <div style={{
                        flexDirection: "Row",
                        display: "Flex",
                        justifyContent: "SpaceBetween",
                        alignItems: "Center",
                        marginBottom: 4,
                    }}>
                        <div style={{ fontSize: 11, color: theme.accent }}>{resultKey}</div>
                        <div style={{ fontSize: 11, color: theme.textMuted }}
                            onClick={() => setResult(null)}>{"✕"}</div>
                    </div>
                    <div style={{ fontSize: 11, color: theme.text, whiteSpace: "Normal" }}>
                        {result.length > 500 ? result.substring(0, 500) + "..." : result}
                    </div>
                </div>
            )}

            {/* Tree list */}
            {pageItems.map((item, i) => (
                <TreeRow
                    key={`row-${i}`}
                    item={item}
                    expanded={!!expanded[item.node.id]}
                    onToggle={() => toggle(item.node.id)}
                    onRun={(label, action) => run(label, action)}
                />
            ))}

            {flat.length === 0 && (
                <div style={{ fontSize: 12, color: theme.textMuted, padding: 12 }}>
                    {"（无可用 API）"}
                </div>
            )}

            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </div>
    )
}

// ─── Flatten tree for visible items ───
interface FlatItem {
    node: TreeNode
    depth: number
    hasChildren: boolean
}

function flattenVisible(nodes: TreeNode[], expanded: Record<string, boolean>, depth = 0): FlatItem[] {
    const out: FlatItem[] = []
    for (const n of nodes) {
        const hasChildren = !!(n.children && n.children.length > 0)
        out.push({ node: n, depth, hasChildren })
        if (hasChildren && expanded[n.id]) {
            out.push(...flattenVisible(n.children!, expanded, depth + 1))
        }
    }
    return out
}

// ─── Row component ───
const TreeRow = ({ item, expanded, onToggle, onRun }: {
    item: FlatItem
    expanded: boolean
    onToggle: () => void
    onRun: (label: string, action: () => any) => void
}) => {
    const { node, depth, hasChildren } = item
    const indent = depth * 16
    const isLeaf = !hasChildren

    return (
        <div style={{
            flexDirection: "Row",
            display: "Flex",
            alignItems: "Center",
            backgroundColor: theme.bgCard,
            borderRadius: 4,
            paddingLeft: 10 + indent,
            paddingRight: 10,
            marginBottom: 3,
            height: 36,
        }}>
            {/* Expand/collapse icon or leaf icon */}
            <div
                style={{
                    width: 20,
                    fontSize: 12,
                    color: hasChildren ? theme.accent : theme.textMuted,
                    flexShrink: 0,
                }}
                onClick={hasChildren ? onToggle : undefined}
            >
                {hasChildren ? (expanded ? "▼" : "▶") : "•"}
            </div>

            {/* Label */}
            <div
                style={{
                    flexGrow: 1,
                    fontSize: 12,
                    color: hasChildren ? theme.accent : theme.text,
                    overflow: "Hidden",
                }}
                onClick={hasChildren ? onToggle : undefined}
            >
                {node.label}
            </div>

            {/* Action button for leaf nodes */}
            {isLeaf && node.action && (
                <div
                    style={{
                        paddingTop: 2,
                        paddingBottom: 2,
                        paddingLeft: 8,
                        paddingRight: 8,
                        borderRadius: 4,
                        fontSize: 11,
                        backgroundColor: theme.accentDark,
                        color: theme.textBright,
                        flexShrink: 0,
                    }}
                    onClick={() => onRun(node.label, node.action!)}
                >
                    {"执行"}
                </div>
            )}

            {/* Toggle switch for boolean properties */}
            {node.toggle && (
                <ToggleButton toggle={node.toggle} onResult={(l, a) => onRun(l, a)} label={node.label} />
            )}
        </div>
    )
}

// ─── Toggle button component ───
const ToggleButton = ({ toggle, onResult, label }: {
    toggle: { get: () => boolean; set: (v: boolean) => void }
    onResult: (label: string, action: () => any) => void
    label: string
}) => {
    const [val, setVal] = useState(() => { try { return toggle.get() } catch { return false } })
    return (
        <div
            style={{
                paddingTop: 2,
                paddingBottom: 2,
                paddingLeft: 8,
                paddingRight: 8,
                borderRadius: 4,
                fontSize: 11,
                backgroundColor: val ? theme.success : theme.danger,
                color: theme.textBright,
                flexShrink: 0,
            }}
            onClick={() => {
                const next = !val
                try { toggle.set(next); setVal(next) }
                catch (e) { onResult(label, () => `Error: ${e}`) }
            }}
        >
            {val ? "ON" : "OFF"}
        </div>
    )
}

// ─── Build the API tree definition ───
function buildTree(): TreeNode[] {
    const g = typeof chill !== "undefined" ? chill.game : null
    if (!g) return [{ id: "unavailable", label: "chill.game 不可用" }]

    return [
        buildEnvironmentTree(g.environment),
        buildDecorationTree(g.decoration),
        buildModeTree(g.mode),
        buildCharacterTree(g.character),
        buildSubtitleTree(g.subtitle),
        buildVoiceTree(g.voice),
        buildPomodoroTree(g),
    ]
}

function buildEnvironmentTree(api: any): TreeNode {
    if (!api) return { id: "env", label: "环境 (unavailable)" }
    return {
        id: "env",
        label: "🌄 环境 (Environment)",
        children: [
            { id: "env.locked", label: "locked (只读, C#控制)", action: () => String(api.locked) },
            { id: "env.getEnvironments", label: "getEnvironments()", action: () => api.getEnvironments() },
            { id: "env.getAutoTime", label: "getAutoTimeSettings()", action: () => api.getAutoTimeSettings() },
            { id: "env.presetIndex", label: "getCurrentPresetIndex()", action: () => api.getCurrentPresetIndex() },
            { id: "env.loadPreset0", label: "loadPreset(0)", action: () => api.loadPreset(0) },
            { id: "env.loadPreset1", label: "loadPreset(1)", action: () => api.loadPreset(1) },
            { id: "env.loadPreset2", label: "loadPreset(2)", action: () => api.loadPreset(2) },
        ],
    }
}

function buildDecorationTree(api: any): TreeNode {
    if (!api) return { id: "deco", label: "装饰 (unavailable)" }
    return {
        id: "deco",
        label: "🪑 装饰 (Decoration)",
        children: [
            { id: "deco.locked", label: "locked (只读, C#控制)", action: () => String(api.locked) },
            { id: "deco.getCategories", label: "getCategories()", action: () => api.getCategories() },
            { id: "deco.getDecorations", label: "getDecorations()", action: () => api.getDecorations() },
            { id: "deco.getCurrentModels", label: "getCurrentModels()", action: () => api.getCurrentModels() },
            { id: "deco.reloadFromSave", label: "reloadFromSave()", action: () => api.reloadFromSave() },
        ],
    }
}

function buildModeTree(api: any): TreeNode {
    if (!api) return { id: "mode", label: "模式 (unavailable)" }
    return {
        id: "mode",
        label: "🎭 模式 (Mode)",
        children: [
            { id: "mode.locked", label: "locked (只读, C#控制)", action: () => String(api.locked) },
            { id: "mode.getAvailable", label: "getAvailableModes()", action: () => api.getAvailableModes() },
            { id: "mode.getCurrent", label: "getCurrentMode()", action: () => api.getCurrentMode() },
            { id: "mode.getState", label: "getModeState()", action: () => api.getModeState() },
            { id: "mode.canChange", label: "canChangeMode()", action: () => api.canChangeMode() },
            { id: "mode.setNone", label: "setMode('None')", action: () => api.setMode("None") },
            { id: "mode.setAlterEgo", label: "setMode('AlterEgo')", action: () => api.setMode("AlterEgo") },
        ],
    }
}

function buildCharacterTree(api: any): TreeNode {
    if (!api) return { id: "char", label: "角色 (unavailable)" }
    return {
        id: "char",
        label: "👤 角色 (Character)",
        children: [
            { id: "char.locked", label: "locked (只读, C#控制)", action: () => String(api.locked) },
            { id: "char.getAvailable", label: "getAvailableStates()", action: () => api.getAvailableStates() },
            { id: "char.getState", label: "getState()", action: () => api.getState() },
            { id: "char.startWork", label: "startWork()", action: () => api.startWork() },
            { id: "char.startBreak", label: "startBreak()", action: () => api.startBreak() },
            { id: "char.cancelChange", label: "cancelChange()", action: () => api.cancelChange() },
            { id: "char.matchAction", label: "matchCurrentAction()", action: () => api.matchCurrentAction() },
            { id: "char.matchWild", label: "matchCurrentActionWithWild()", action: () => api.matchCurrentActionWithWild() },
        ],
    }
}

function buildSubtitleTree(api: any): TreeNode {
    if (!api) return { id: "sub", label: "字幕 (unavailable)" }
    return {
        id: "sub",
        label: "💬 字幕 (Subtitle)",
        children: [
            { id: "sub.locked", label: "locked (只读, C#控制)", action: () => String(api.locked) },
            { id: "sub.isShowing", label: "isShowing()", action: () => api.isShowing() },
            { id: "sub.show", label: "show('Hello!', 5)", action: () => api.show("Hello!", 5) },
            { id: "sub.hide", label: "hide()", action: () => api.hide() },
            { id: "sub.getScenarioState", label: "getScenarioState()", action: () => api.getScenarioState() },
        ],
    }
}

function buildVoiceTree(api: any): TreeNode {
    if (!api) return { id: "voice", label: "语音 (unavailable)" }
    return {
        id: "voice",
        label: "🔊 语音 (Voice)",
        children: [
            { id: "voice.locked", label: "locked (只读, C#控制)", action: () => String(api.locked) },
            { id: "voice.getState", label: "getState()", action: () => api.getState() },
            { id: "voice.isFinished", label: "isFinished()", action: () => api.isFinished() },
            { id: "voice.isMouthMoving", label: "isMouthMoving()", action: () => api.isMouthMoving() },
            { id: "voice.getScenarioTypes", label: "getScenarioTypes()", action: () => api.getScenarioTypes() },
            { id: "voice.cancel", label: "cancelVoice()", action: () => api.cancelVoice() },
        ],
    }
}

function buildPomodoroTree(g: any): TreeNode {
    return {
        id: "pomodoro",
        label: "🍅 番茄钟 (Pomodoro)",
        children: [
            { id: "pom.getState", label: "getPomodoroState()", action: () => g.getPomodoroState() },
            { id: "pom.getProgress", label: "getPlayerProgress()", action: () => g.getPlayerProgress() },
            { id: "pom.getClock", label: "getGameClock()", action: () => g.getGameClock() },
            { id: "pom.start", label: "startPomodoro()", action: () => g.startPomodoro() },
            { id: "pom.togglePause", label: "togglePomodoroPause()", action: () => g.togglePomodoroPause() },
            { id: "pom.skip", label: "skipPomodoroPhase()", action: () => g.skipPomodoroPhase() },
            { id: "pom.reset", label: "resetPomodoro()", action: () => g.resetPomodoro() },
            { id: "pom.complete", label: "completePomodoroNow()", action: () => g.completePomodoroNow() },
            { id: "pom.eventNames", label: "getEventNames()", action: () => g.getEventNames() },
        ],
    }
}
