import { h } from "preact"
import { useEffect, useMemo, useRef, useState } from "preact/hooks"

declare const __registerPlugin: any
declare const chill: any

type EventType = "countdown" | "elapsed"
type SettingsMenu = "main" | "colors"
type SettingsNumberKey = "eventsPerPage" | "pageLabelCount"
type CardDensity = "simple" | "standard"

type CalendarEvent = {
    id: string
    title: string
    targetDate: string
    type: EventType
    createdAt: string
    pinned?: boolean
}

type CalendarWidgetConfig = {
    events: CalendarEvent[]
    eventsFilePath: string
    compactCount: 1 | 2 | 4
    cardDensity: CardDensity
    eventsPerPage: number
    pageLabelCount: number
    compactCardWidth: number
    compactCardHeight: number
    titleColor: string
    daysColor: string
    backgroundColor: string
    leftPanelBgColor: string
    rightPanelBgColor: string
    textColor: string
}

type CalendarCell = {
    iso: string
    year: number
    month: number
    day: number
    inCurrentMonth: boolean
}

type LayoutHint = {
    windowWidth: number
    windowHeight: number
    listHeight: number
    pagerWidth: number
}

const CONFIG_FILE = "window-states/countdown-days.json"
const EVENTS_FILE = "window-states/countdown-days-events.json"
const WINDOW_STATE_FILE = "window-states/window-Countdown-Days.json"
const PLUGIN_ID = "countdown-days"
const AUTO_REMOUNT_FLAG = "__countdownDaysAutoRemountDone"
const DEFAULT_EVENTS_PER_PAGE = 8
const DEFAULT_PAGE_LABEL_COUNT = 5
const DEFAULT_COMPACT_CARD_WIDTH = 122
const DEFAULT_COMPACT_CARD_HEIGHT = 62
const DEFAULT_WINDOW_WIDTH = 390
const DEFAULT_WINDOW_HEIGHT = 830
const DEFAULT_LAYOUT_HINT: LayoutHint = {
    windowWidth: DEFAULT_WINDOW_WIDTH,
    windowHeight: DEFAULT_WINDOW_HEIGHT,
    listHeight: 0,
    pagerWidth: 0,
}
const EVENT_CARD_ROW_HEIGHT_STANDARD = 90
const EVENT_CARD_ROW_HEIGHT_SIMPLE = 74
const PAGER_FIXED_SPACE = 170
const PAGER_BUTTON_WIDTH = 34
const COMPACT_INNER_WIDTH = 258
const COMPACT_INNER_HEIGHT = 132
const COMPACT_CARD_GAP = 4
const LAYOUT_HINT_SETTLE_DELAY_MS = 140
const LAYOUT_HINT_POLL_INTERVAL_MS = 1800
const COMPACT_SYNC_INTERVAL_MS = 30000

const WEEK_LABELS = ["一", "二", "三", "四", "五", "六", "日"]

const MONTH_LABELS = [
    "1月", "2月", "3月", "4月", "5月", "6月",
    "7月", "8月", "9月", "10月", "11月", "12月",
]

const PRESET_COLORS = [
    "#0f172a", "#1e293b", "#334155", "#475569", "#0b1120", "#082f49",
    "#0c4a6e", "#155e75", "#164e63", "#1d4ed8", "#2563eb", "#4f46e5",
    "#6366f1", "#7c3aed", "#9333ea", "#be185d", "#dc2626", "#ea580c",
    "#ca8a04", "#4d7c0f", "#15803d", "#0f766e",
]

type ThemeColors = Pick<CalendarWidgetConfig, "titleColor" | "daysColor" | "backgroundColor" | "leftPanelBgColor" | "rightPanelBgColor" | "textColor">

const THEME_PRESETS: Array<{ id: string; name: string; config: ThemeColors }> = [
    {
        id: "deep-sea",
        name: "深海蓝",
        config: {
            titleColor: "#dbeafe",
            daysColor: "#22d3ee",
            backgroundColor: "#0b1120",
            leftPanelBgColor: "#0f172a",
            rightPanelBgColor: "#13264a",
            textColor: "#cbd5e1",
        },
    },
    {
        id: "forest-night",
        name: "夜森林",
        config: {
            titleColor: "#dcfce7",
            daysColor: "#34d399",
            backgroundColor: "#0a1f16",
            leftPanelBgColor: "#113126",
            rightPanelBgColor: "#14382b",
            textColor: "#bbf7d0",
        },
    },
    {
        id: "sunset-red",
        name: "落日红",
        config: {
            titleColor: "#fee2e2",
            daysColor: "#f97316",
            backgroundColor: "#3b0b15",
            leftPanelBgColor: "#5a1523",
            rightPanelBgColor: "#6b1d2c",
            textColor: "#fecaca",
        },
    },
    {
        id: "steel-gray",
        name: "钢铁灰",
        config: {
            titleColor: "#e2e8f0",
            daysColor: "#38bdf8",
            backgroundColor: "#111827",
            leftPanelBgColor: "#1f2937",
            rightPanelBgColor: "#243244",
            textColor: "#cbd5e1",
        },
    },
    {
        id: "day-light",
        name: "日间亮色",
        config: {
            titleColor: "#1f2937",
            daysColor: "#0ea5e9",
            backgroundColor: "#eaf2ff",
            leftPanelBgColor: "#dbeafe",
            rightPanelBgColor: "#eff6ff",
            textColor: "#1e293b",
        },
    },
    {
        id: "mocha-brown",
        name: "摩卡棕",
        config: {
            titleColor: "#fef3c7",
            daysColor: "#f59e0b",
            backgroundColor: "#2b1b12",
            leftPanelBgColor: "#3a2418",
            rightPanelBgColor: "#4a2d1f",
            textColor: "#fde68a",
        },
    },
    {
        id: "mint-green",
        name: "薄荷绿",
        config: {
            titleColor: "#052e2b",
            daysColor: "#0d9488",
            backgroundColor: "#dffaf2",
            leftPanelBgColor: "#c8f5e7",
            rightPanelBgColor: "#ecfdf5",
            textColor: "#115e59",
        },
    },
    {
        id: "sakura-pink",
        name: "樱花粉",
        config: {
            titleColor: "#4a044e",
            daysColor: "#db2777",
            backgroundColor: "#fce7f3",
            leftPanelBgColor: "#fbcfe8",
            rightPanelBgColor: "#fdf2f8",
            textColor: "#831843",
        },
    },
    {
        id: "ivory-paper",
        name: "米白纸",
        config: {
            titleColor: "#3f3a2a",
            daysColor: "#ca8a04",
            backgroundColor: "#f8f3e8",
            leftPanelBgColor: "#f4ead7",
            rightPanelBgColor: "#fdf8ee",
            textColor: "#57534e",
        },
    },
    {
        id: "midnight-black",
        name: "极夜黑",
        config: {
            titleColor: "#f3f4f6",
            daysColor: "#60a5fa",
            backgroundColor: "#05070d",
            leftPanelBgColor: "#0b1020",
            rightPanelBgColor: "#111827",
            textColor: "#d1d5db",
        },
    },
]

const DEFAULT_CONFIG: CalendarWidgetConfig = {
    events: [],
    eventsFilePath: EVENTS_FILE,
    compactCount: 2,
    cardDensity: "standard",
    eventsPerPage: DEFAULT_EVENTS_PER_PAGE,
    pageLabelCount: DEFAULT_PAGE_LABEL_COUNT,
    compactCardWidth: DEFAULT_COMPACT_CARD_WIDTH,
    compactCardHeight: DEFAULT_COMPACT_CARD_HEIGHT,
    titleColor: "#dbeafe",
    daysColor: "#22d3ee",
    backgroundColor: "#13264a",
    leftPanelBgColor: "#1a305a",
    rightPanelBgColor: "#1a305a",
    textColor: "#dbeafe",
}

const normalizeText = (value: unknown, fallback: string) => {
    if (typeof value !== "string") return fallback
    const trimmed = value.trim()
    return trimmed.length > 0 ? trimmed : fallback
}

const normalizeColor = (value: unknown, fallback: string) => {
    const text = normalizeText(value, fallback)
    if (/^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/.test(text)) return text
    return fallback
}

const normalizeCompactCount = (value: unknown, fallback: 1 | 2 | 4): 1 | 2 | 4 => {
    if (value === 1 || value === 2 || value === 4) return value
    const n = Number(value)
    if (n === 1 || n === 2 || n === 4) return n
    return fallback
}

const normalizeCardDensity = (value: unknown, fallback: CardDensity): CardDensity => {
    if (value === "simple" || value === "standard") return value
    return fallback
}

const normalizeIntInRange = (value: unknown, fallback: number, min: number, max: number) => {
    const n = Math.round(Number(value))
    if (!Number.isFinite(n)) return fallback
    return Math.max(min, Math.min(max, n))
}

const normalizeConfigPath = (value: unknown, fallback: string) => {
    const text = normalizeText(value, fallback).replace(/\\/g, "/")
    return text.length > 0 ? text : fallback
}

const canonicalizePath = (value: string) => {
    return normalizeConfigPath(value, "")
        .replace(/^\.\//, "")
        .replace(/\/{2,}/g, "/")
        .toLowerCase()
}

const isCombinedStoragePath = (eventsFilePath: string) => {
    return canonicalizePath(eventsFilePath) === canonicalizePath(CONFIG_FILE)
}

const hexToRgba = (hex: string, alpha: number) => {
    const raw = normalizeColor(hex, "#0f172a").replace("#", "")
    if (raw.length !== 6) return `rgba(15,23,42,${alpha})`

    const r = parseInt(raw.slice(0, 2), 16)
    const g = parseInt(raw.slice(2, 4), 16)
    const b = parseInt(raw.slice(4, 6), 16)

    return `rgba(${r},${g},${b},${alpha})`
}

const mixHex = (baseHex: string, mixHexColor: string, ratio: number) => {
    const r = Math.max(0, Math.min(1, ratio))
    const base = normalizeColor(baseHex, "#0f172a").slice(1)
    const mix = normalizeColor(mixHexColor, "#000000").slice(1)

    const br = parseInt(base.slice(0, 2), 16)
    const bg = parseInt(base.slice(2, 4), 16)
    const bb = parseInt(base.slice(4, 6), 16)

    const mr = parseInt(mix.slice(0, 2), 16)
    const mg = parseInt(mix.slice(2, 4), 16)
    const mb = parseInt(mix.slice(4, 6), 16)

    const toHex = (value: number) => Math.round(value).toString(16).padStart(2, "0")

    const outR = br + (mr - br) * r
    const outG = bg + (mg - bg) * r
    const outB = bb + (mb - bb) * r

    return `#${toHex(outR)}${toHex(outG)}${toHex(outB)}`
}

const derivePanelColors = (backgroundColor: string) => {
    const base = normalizeColor(backgroundColor, DEFAULT_CONFIG.backgroundColor).slice(1)
    const r = parseInt(base.slice(0, 2), 16)
    const g = parseInt(base.slice(2, 4), 16)
    const b = parseInt(base.slice(4, 6), 16)
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255

    if (luminance > 0.65) {
        return {
            leftPanelBgColor: mixHex(backgroundColor, "#cbd5e1", 0.18),
            rightPanelBgColor: mixHex(backgroundColor, "#ffffff", 0.18),
        }
    }

    return {
        leftPanelBgColor: mixHex(backgroundColor, "#000000", 0.1),
        rightPanelBgColor: mixHex(backgroundColor, "#000000", 0.06),
    }
}

const getDateTimePart = (source: any, field: string, getterName: string) => {
    const directValue = source?.[field]
    if (directValue !== undefined && directValue !== null) {
        const num = Number(directValue)
        if (Number.isFinite(num)) return num
    }

    const getter = source?.[getterName]
    if (typeof getter === "function") {
        const num = Number(getter.call(source))
        if (Number.isFinite(num)) return num
    }

    return null
}

const nowFromHost = () => {
    const g = globalThis as any

    try {
        const raw = g?.CS?.System?.DateTime?.Now ?? g?.CS?.System?.DateTime?.get_Now?.()
        const dt = typeof raw === "function" ? raw() : raw
        if (dt) {
            const year = getDateTimePart(dt, "Year", "get_Year")
            const month = getDateTimePart(dt, "Month", "get_Month")
            const day = getDateTimePart(dt, "Day", "get_Day")
            const hour = getDateTimePart(dt, "Hour", "get_Hour") ?? 0
            const minute = getDateTimePart(dt, "Minute", "get_Minute") ?? 0
            const second = getDateTimePart(dt, "Second", "get_Second") ?? 0
            const ms = getDateTimePart(dt, "Millisecond", "get_Millisecond") ?? 0

            if (year && month && day) {
                return new Date(year, month - 1, day, hour, minute, second, ms)
            }
        }
    } catch (_) {}

    return new Date()
}

const toIsoDate = (date: Date) => {
    const y = String(date.getFullYear()).padStart(4, "0")
    const m = String(date.getMonth() + 1).padStart(2, "0")
    const d = String(date.getDate()).padStart(2, "0")
    return `${y}-${m}-${d}`
}

const parseIsoDate = (value: string): Date | null => {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return null

    const [yText, mText, dText] = value.split("-")
    const y = Number(yText)
    const m = Number(mText)
    const d = Number(dText)

    if (!Number.isFinite(y) || !Number.isFinite(m) || !Number.isFinite(d)) return null

    const date = new Date(y, m - 1, d)
    if (date.getFullYear() !== y || date.getMonth() !== m - 1 || date.getDate() !== d) return null
    return date
}

const todayIso = () => toIsoDate(nowFromHost())

const sortEvents = (events: CalendarEvent[]) => {
    return [...events].sort((a, b) => {
        const dateCompare = a.targetDate.localeCompare(b.targetDate)
        if (dateCompare !== 0) return dateCompare

        const pinCompare = Number(Boolean(b.pinned)) - Number(Boolean(a.pinned))
        if (pinCompare !== 0) return pinCompare

        return a.createdAt.localeCompare(b.createdAt)
    })
}

const sortEventsPinnedFirst = (events: CalendarEvent[]) => {
    return [...events].sort((a, b) => {
        const pinCompare = Number(Boolean(b.pinned)) - Number(Boolean(a.pinned))
        if (pinCompare !== 0) return pinCompare

        const dateCompare = a.targetDate.localeCompare(b.targetDate)
        if (dateCompare !== 0) return dateCompare

        return a.createdAt.localeCompare(b.createdAt)
    })
}

const areEventsEqual = (a: CalendarEvent[], b: CalendarEvent[]) => {
    if (a === b) return true
    if (a.length !== b.length) return false

    for (let i = 0; i < a.length; i += 1) {
        const left = a[i]
        const right = b[i]
        if (
            left.id !== right.id ||
            left.title !== right.title ||
            left.targetDate !== right.targetDate ||
            left.type !== right.type ||
            left.createdAt !== right.createdAt ||
            Boolean(left.pinned) !== Boolean(right.pinned)
        ) {
            return false
        }
    }

    return true
}

const areCompactConfigEqual = (a: CalendarWidgetConfig, b: CalendarWidgetConfig) => {
    return (
        a.compactCount === b.compactCount &&
        a.titleColor === b.titleColor &&
        a.daysColor === b.daysColor &&
        a.rightPanelBgColor === b.rightPanelBgColor &&
        a.textColor === b.textColor &&
        areEventsEqual(a.events, b.events)
    )
}

const normalizeEvent = (raw: any, index: number): CalendarEvent | null => {
    if (!raw || typeof raw !== "object") return null

    const targetDate = normalizeText(raw.targetDate, "")
    if (!parseIsoDate(targetDate)) return null

    const rawType = normalizeText(raw.type, "countdown")
    const type: EventType = rawType === "elapsed" ? "elapsed" : "countdown"

    return {
        id: normalizeText(raw.id, `evt-${index}-${Math.random().toString(36).slice(2, 8)}`),
        title: normalizeText(raw.title, "未命名事件"),
        targetDate,
        type,
        createdAt: normalizeText(raw.createdAt, new Date().toISOString()),
        pinned: Boolean(raw.pinned),
    }
}

const normalizeConfig = (raw: any): CalendarWidgetConfig => {
    const source = raw && typeof raw === "object" ? raw : {}
    const rawEvents = Array.isArray(source.events) ? source.events : []

    const events = rawEvents
        .map((item, index) => normalizeEvent(item, index))
        .filter((item): item is CalendarEvent => item !== null)

    return {
        events: sortEvents(events),
        eventsFilePath: normalizeConfigPath(source.eventsFilePath, DEFAULT_CONFIG.eventsFilePath),
        compactCount: normalizeCompactCount(source.compactCount, DEFAULT_CONFIG.compactCount),
        cardDensity: normalizeCardDensity(source.cardDensity, DEFAULT_CONFIG.cardDensity),
        eventsPerPage: normalizeIntInRange(source.eventsPerPage, DEFAULT_CONFIG.eventsPerPage, 1, 20),
        pageLabelCount: normalizeIntInRange(source.pageLabelCount, DEFAULT_CONFIG.pageLabelCount, 3, 9),
        compactCardWidth: normalizeIntInRange(source.compactCardWidth, DEFAULT_CONFIG.compactCardWidth, 96, COMPACT_INNER_WIDTH),
        compactCardHeight: normalizeIntInRange(source.compactCardHeight, DEFAULT_CONFIG.compactCardHeight, 46, 160),
        titleColor: normalizeColor(source.titleColor, DEFAULT_CONFIG.titleColor),
        daysColor: normalizeColor(source.daysColor, DEFAULT_CONFIG.daysColor),
        backgroundColor: normalizeColor(source.backgroundColor, DEFAULT_CONFIG.backgroundColor),
        leftPanelBgColor: normalizeColor(source.leftPanelBgColor, DEFAULT_CONFIG.leftPanelBgColor),
        rightPanelBgColor: normalizeColor(source.rightPanelBgColor, DEFAULT_CONFIG.rightPanelBgColor),
        textColor: normalizeColor(source.textColor, DEFAULT_CONFIG.textColor),
    }
}

const loadConfig = (): CalendarWidgetConfig => {
    try {
        if (!chill?.io?.exists?.(CONFIG_FILE)) return DEFAULT_CONFIG
        const text = chill?.io?.readText?.(CONFIG_FILE)
        if (!text) return DEFAULT_CONFIG
        return normalizeConfig(JSON.parse(text))
    } catch (e) {
        console.error("[countdown-days] load config failed", CONFIG_FILE, e)
        return DEFAULT_CONFIG
    }
}

const saveConfig = (config: CalendarWidgetConfig) => {
    try {
        chill?.io?.writeText?.(CONFIG_FILE, JSON.stringify(config, null, 2))
    } catch (e) {
        console.error("[countdown-days] save config failed", CONFIG_FILE, e)
    }
}

const loadEventsFromFile = (eventsFilePath: string): CalendarEvent[] | null => {
    try {
        if (!chill?.io?.exists?.(eventsFilePath)) return null
        const text = chill?.io?.readText?.(eventsFilePath)
        if (!text) return []
        const raw = JSON.parse(text)
        const eventsRaw = Array.isArray(raw) ? raw : (Array.isArray(raw?.events) ? raw.events : [])
        return eventsRaw
            .map((item, index) => normalizeEvent(item, index))
            .filter((item): item is CalendarEvent => item !== null)
    } catch (e) {
        console.error("[countdown-days] load events failed", eventsFilePath, e)
        return null
    }
}

const saveEventsToFile = (events: CalendarEvent[], eventsFilePath: string) => {
    try {
        chill?.io?.writeText?.(eventsFilePath, JSON.stringify({ events }, null, 2))
    } catch (e) {
        console.error("[countdown-days] save events failed", eventsFilePath, e)
    }
}

const buildConfigForDisk = (config: CalendarWidgetConfig) => {
    const normalized = normalizeConfig(config)
    const eventsFilePath = normalizeConfigPath(normalized.eventsFilePath, DEFAULT_CONFIG.eventsFilePath)
    if (isCombinedStoragePath(eventsFilePath)) return { ...normalized, eventsFilePath }
    return { ...normalized, eventsFilePath, events: [] }
}

const loadWindowSizeFromState = () => {
    try {
        if (!chill?.io?.exists?.(WINDOW_STATE_FILE)) return null
        const text = chill?.io?.readText?.(WINDOW_STATE_FILE)
        if (!text) return null
        const raw = JSON.parse(text)
        const width = Number(raw?.width)
        const height = Number(raw?.height)
        if (!Number.isFinite(width) || !Number.isFinite(height)) return null
        return {
            width: Math.max(220, Math.round(width)),
            height: Math.max(220, Math.round(height)),
        }
    } catch {
        return null
    }
}

const calculateDays = (targetDate: string) => {
    const target = parseIsoDate(targetDate)
    if (!target) return 0

    const now = nowFromHost()
    const todayStamp = Date.UTC(now.getFullYear(), now.getMonth(), now.getDate())
    const targetStamp = Date.UTC(target.getFullYear(), target.getMonth(), target.getDate())
    return Math.floor((targetStamp - todayStamp) / 86400000)
}

const formatDaysText = (days: number) => {
    if (days > 0) return `还剩 ${days} 天`
    if (days === 0) return "就在今天"
    return `已经过去 ${Math.abs(days)} 天`
}

const inferEventType = (targetDate: string): EventType => {
    return calculateDays(targetDate) < 0 ? "elapsed" : "countdown"
}

const monthTitle = (year: number, month: number) => `${year}年 ${month + 1}月`

const shiftMonth = (year: number, month: number, delta: number) => {
    const total = year * 12 + month + delta
    const nextYear = Math.floor(total / 12)
    let nextMonth = total % 12
    if (nextMonth < 0) nextMonth += 12
    return { year: nextYear, month: nextMonth }
}

const buildCalendarCells = (year: number, month: number): CalendarCell[] => {
    const firstDay = new Date(year, month, 1)
    const firstWeekdayMonday0 = (firstDay.getDay() + 6) % 7
    const gridStart = new Date(year, month, 1 - firstWeekdayMonday0)

    const cells: CalendarCell[] = []
    for (let i = 0; i < 42; i++) {
        const date = new Date(gridStart.getFullYear(), gridStart.getMonth(), gridStart.getDate() + i)
        cells.push({
            iso: toIsoDate(date),
            year: date.getFullYear(),
            month: date.getMonth(),
            day: date.getDate(),
            inCurrentMonth: date.getMonth() === month,
        })
    }

    return cells
}

const ActionButton = ({
    text,
    onClick,
    color,
    bg,
    disabled,
    compact,
    fullWidth,
}: {
    text: string
    onClick: () => void
    color?: string
    bg?: string
    disabled?: boolean
    compact?: boolean
    fullWidth?: boolean
}) => (
    <div
        onPointerDown={(e: any) => {
            e?.stopPropagation?.()
            if (!disabled) onClick()
        }}
        onPointerUp={(e: any) => {
            e?.stopPropagation?.()
        }}
        style={{
            fontSize: compact ? 10 : 11,
            color: disabled ? "#64748b" : (color || "#dbeafe"),
            backgroundColor: disabled ? "rgba(51,65,85,0.3)" : (bg || "#334155"),
            paddingLeft: compact ? 7 : 8,
            paddingRight: compact ? 7 : 8,
            paddingTop: compact ? 4 : 5,
            paddingBottom: compact ? 4 : 5,
            borderRadius: 6,
            minHeight: compact ? 20 : 22,
            width: fullWidth ? "100%" : undefined,
            unityTextAlign: "MiddleCenter",
        }}
    >
        {text}
    </div>
)
const ColorEditorRow = ({
    label,
    value,
    onChange,
    textColor,
    inputBg,
    inputBorder,
}: {
    label: string
    value: string
    onChange: (nextValue: string) => void
    textColor: string
    inputBg: string
    inputBorder: string
}) => (
    <div style={{ marginBottom: 9 }}>
        <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>{label}</div>

        <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 5 }}>
            <div
                style={{
                    width: 20,
                    height: 20,
                    borderRadius: 4,
                    marginRight: 6,
                    backgroundColor: value,
                    borderWidth: 1,
                    borderColor: hexToRgba(textColor, 0.45),
                }}
            />
            <textfield
                value={value}
                multiline={false}
                onValueChanged={(e: any) => onChange(e?.newValue ?? "")}
                style={{
                    flexGrow: 1,
                    width: "100%",
                    height: 24,
                    fontSize: 10,
                    backgroundColor: inputBg,
                    borderWidth: 1,
                    borderColor: inputBorder,
                    color: textColor,
                    paddingLeft: 8,
                    paddingRight: 8,
                    paddingTop: 0,
                    paddingBottom: 0,
                    unityTextAlign: "MiddleLeft",
                }}
            />
        </div>

        <div style={{ display: "Flex", flexDirection: "Row", flexWrap: "Wrap" }}>
            {PRESET_COLORS.map((color) => (
                <div
                    key={`${label}-${color}`}
                    onPointerDown={() => onChange(color)}
                    style={{
                        width: 16,
                        height: 16,
                        borderRadius: 4,
                        marginRight: 4,
                        marginBottom: 4,
                        backgroundColor: color,
                        borderWidth: value.toLowerCase() === color.toLowerCase() ? 2 : 1,
                        borderColor: value.toLowerCase() === color.toLowerCase() ? textColor : hexToRgba(textColor, 0.4),
                    }}
                />
            ))}
        </div>
    </div>
)

const useClockRerender = (intervalMs: number = 30000) => {
    const [, setTick] = useState(0)

    useEffect(() => {
        const timer = setInterval(() => {
            setTick((tick) => tick + 1)
        }, intervalMs)
        return () => clearInterval(timer)
    }, [intervalMs])
}

const useAutoRemountOnFirstMount = () => {
    useEffect(() => {
        const g = globalThis as any
        if (g[AUTO_REMOUNT_FLAG]) return

        let tries = 0
        const timer = setInterval(() => {
            const control = g.__wmPluginControl
            if (!control?.togglePluginVisible) {
                tries += 1
                if (tries > 180) clearInterval(timer)
                return
            }

            clearInterval(timer)
            g[AUTO_REMOUNT_FLAG] = true

            try {
                control.togglePluginVisible(PLUGIN_ID)
                setTimeout(() => {
                    control.togglePluginVisible(PLUGIN_ID)
                }, 100)
            } catch (e) {
                console.error("[countdown-days] auto remount failed", e)
            }
        }, 66)

        return () => clearInterval(timer)
    }, [])
}

const useWindowLayoutHint = (eventListRef: any, pagerRowRef: any) => {
    const [layoutHint, setLayoutHint] = useState<LayoutHint>(DEFAULT_LAYOUT_HINT)

    useEffect(() => {
        const readLayout = (readWindowState: boolean = true) => {
            const windowSize = readWindowState ? loadWindowSizeFromState() : null

            const listHeightRaw = Number(eventListRef.current?.ve?.layout?.height ?? 0)
            const pagerWidthRaw = Number(pagerRowRef.current?.ve?.layout?.width ?? 0)
            const nextListHeight = Number.isFinite(listHeightRaw) ? Math.max(0, Math.round(listHeightRaw)) : 0
            const nextPagerWidth = Number.isFinite(pagerWidthRaw) ? Math.max(0, Math.round(pagerWidthRaw)) : 0

            setLayoutHint((prev) => {
                const nextWindowWidth = windowSize?.width ?? prev.windowWidth
                const nextWindowHeight = windowSize?.height ?? prev.windowHeight
                const same =
                    Math.abs(prev.windowWidth - nextWindowWidth) <= 1 &&
                    Math.abs(prev.windowHeight - nextWindowHeight) <= 1 &&
                    Math.abs(prev.listHeight - nextListHeight) <= 1 &&
                    Math.abs(prev.pagerWidth - nextPagerWidth) <= 1

                if (same) return prev

                return {
                    windowWidth: nextWindowWidth,
                    windowHeight: nextWindowHeight,
                    listHeight: nextListHeight,
                    pagerWidth: nextPagerWidth,
                }
            })
        }

        readLayout(true)
        let settleTimer: any = null
        let fallbackTimer: any = null
        const g = globalThis as any
        const doc = g?.document as any

        const flushLayoutAfterResize = () => {
            readLayout(true)
            if (settleTimer) clearTimeout(settleTimer)
            settleTimer = setTimeout(() => readLayout(true), LAYOUT_HINT_SETTLE_DELAY_MS)
        }

        const onPointerUp = () => flushLayoutAfterResize()
        const onMouseUp = () => flushLayoutAfterResize()
        const onTouchEnd = () => flushLayoutAfterResize()
        const onResize = () => flushLayoutAfterResize()

        g?.addEventListener?.("pointerup", onPointerUp)
        g?.addEventListener?.("mouseup", onMouseUp)
        g?.addEventListener?.("touchend", onTouchEnd)
        g?.addEventListener?.("resize", onResize)
        doc?.addEventListener?.("pointerup", onPointerUp)
        doc?.addEventListener?.("mouseup", onMouseUp)
        doc?.addEventListener?.("touchend", onTouchEnd)
        fallbackTimer = setInterval(() => {
            if (!eventListRef.current && !pagerRowRef.current) return
            readLayout(false)
        }, LAYOUT_HINT_POLL_INTERVAL_MS)

        return () => {
            if (settleTimer) clearTimeout(settleTimer)
            if (fallbackTimer) clearInterval(fallbackTimer)
            g?.removeEventListener?.("pointerup", onPointerUp)
            g?.removeEventListener?.("mouseup", onMouseUp)
            g?.removeEventListener?.("touchend", onTouchEnd)
            g?.removeEventListener?.("resize", onResize)
            doc?.removeEventListener?.("pointerup", onPointerUp)
            doc?.removeEventListener?.("mouseup", onMouseUp)
            doc?.removeEventListener?.("touchend", onTouchEnd)
        }
    }, [eventListRef, pagerRowRef])

    return layoutHint
}

const CountdownPanel = () => {
    const [config, setConfig] = useState<CalendarWidgetConfig>(DEFAULT_CONFIG)
    useClockRerender(30000)
    useAutoRemountOnFirstMount()

    const [selectedDate, setSelectedDate] = useState(todayIso())
    const [viewYear, setViewYear] = useState(new Date().getFullYear())
    const [viewMonth, setViewMonth] = useState(new Date().getMonth())

    const [draftTitle, setDraftTitle] = useState("")
    const [editDialogOpen, setEditDialogOpen] = useState(false)
    const [editDialogId, setEditDialogId] = useState<string | null>(null)
    const [editDialogTitle, setEditDialogTitle] = useState("")
    const [editDialogDate, setEditDialogDate] = useState(todayIso())

    const [error, setError] = useState("")

    const [monthPickerOpen, setMonthPickerOpen] = useState(false)
    const [yearCursor, setYearCursor] = useState(viewYear)

    const [settingsOpen, setSettingsOpen] = useState(false)
    const [settingsMenu, setSettingsMenu] = useState<SettingsMenu>("main")
    const [settingsDraft, setSettingsDraft] = useState({
        backgroundColor: DEFAULT_CONFIG.backgroundColor,
        textColor: DEFAULT_CONFIG.textColor,
        daysColor: DEFAULT_CONFIG.daysColor,
        cardDensity: DEFAULT_CONFIG.cardDensity as CardDensity,
        eventsPerPage: DEFAULT_CONFIG.eventsPerPage,
        pageLabelCount: DEFAULT_CONFIG.pageLabelCount,
        eventsFilePath: DEFAULT_CONFIG.eventsFilePath,
    })
    const [eventPage, setEventPage] = useState(1)
    const [middleMode, setMiddleMode] = useState(false)
    const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)
    const eventListRef = useRef<any>(null)
    const pagerRowRef = useRef<any>(null)
    const layoutHint = useWindowLayoutHint(eventListRef, pagerRowRef)

    useEffect(() => {
        const loadedSettings = loadConfig()
        const loadedEventsPath = normalizeConfigPath(loadedSettings.eventsFilePath, DEFAULT_CONFIG.eventsFilePath)
        const loadedEvents = loadEventsFromFile(loadedEventsPath)
        const configEvents = sortEvents(loadedSettings.events)
        const nextEvents = sortEvents(loadedEvents ?? configEvents)
        const nextConfig = { ...loadedSettings, eventsFilePath: loadedEventsPath, events: nextEvents }
        const splitStorage = !isCombinedStoragePath(loadedEventsPath)

        if (splitStorage) {
            if (loadedEvents === null && configEvents.length > 0) {
                saveEventsToFile(configEvents, loadedEventsPath)
            }
            if (configEvents.length > 0 || loadedSettings.eventsFilePath !== loadedEventsPath) {
                saveConfig(buildConfigForDisk(nextConfig))
            }
        }

        setConfig(nextConfig)

        const todayDate = todayIso()
        const parsed = parseIsoDate(todayDate)

        if (parsed) {
            setSelectedDate(todayDate)
            setViewYear(parsed.getFullYear())
            setViewMonth(parsed.getMonth())
        }
    }, [])

    const events = useMemo(() => sortEvents(config.events), [config.events])

    const eventCountByDate = useMemo(() => {
        const map = new Map<string, number>()
        for (const eventItem of events) {
            map.set(eventItem.targetDate, (map.get(eventItem.targetDate) || 0) + 1)
        }
        return map
    }, [events])

    const allEvents = useMemo(() => sortEventsPinnedFirst(events), [events])
    const configuredEventsPerPage = Math.max(1, config.eventsPerPage)
    const fallbackListHeight = middleMode
        ? Math.max(160, layoutHint.windowHeight - 210)
        : Math.max(120, layoutHint.windowHeight - 380)
    const activeListHeight = layoutHint.listHeight > 0 ? layoutHint.listHeight : fallbackListHeight
    const estimatedCardRowHeight = config.cardDensity === "simple" ? EVENT_CARD_ROW_HEIGHT_SIMPLE : EVENT_CARD_ROW_HEIGHT_STANDARD
    const autoRowsPerPage = Math.max(1, Math.floor((activeListHeight + 6) / estimatedCardRowHeight))
    const eventsPerPage = Math.max(1, Math.min(20, autoRowsPerPage || configuredEventsPerPage))

    const configuredPageLabelCount = Math.max(3, config.pageLabelCount)
    const fallbackPagerWidth = Math.max(220, layoutHint.windowWidth - 40)
    const activePagerWidth = layoutHint.pagerWidth > 0 ? layoutHint.pagerWidth : fallbackPagerWidth
    const autoPageLabelCount = Math.floor((activePagerWidth - PAGER_FIXED_SPACE) / PAGER_BUTTON_WIDTH)
    const pageLabelCount = Math.max(3, Math.min(9, Number.isFinite(autoPageLabelCount) ? autoPageLabelCount : configuredPageLabelCount))
    const totalEventPages = useMemo(
        () => Math.max(1, Math.ceil(allEvents.length / eventsPerPage)),
        [allEvents.length, eventsPerPage]
    )

    const pagedEvents = useMemo(() => {
        const start = (eventPage - 1) * eventsPerPage
        return allEvents.slice(start, start + eventsPerPage)
    }, [allEvents, eventPage, eventsPerPage])

    const visiblePageNumbers = useMemo(() => {
        if (totalEventPages <= pageLabelCount) {
            return Array.from({ length: totalEventPages }, (_, index) => index + 1)
        }

        const halfWindow = Math.floor(pageLabelCount / 2)
        let start = Math.max(1, eventPage - halfWindow)
        let end = start + pageLabelCount - 1

        if (end > totalEventPages) {
            end = totalEventPages
            start = end - pageLabelCount + 1
        }

        return Array.from({ length: end - start + 1 }, (_, index) => start + index)
    }, [eventPage, totalEventPages, pageLabelCount])
    const todayIsoValue = todayIso()

    const calendarCells = useMemo(() => buildCalendarCells(viewYear, viewMonth), [viewYear, viewMonth])

    const calendarRows = useMemo(() => {
        const rows: CalendarCell[][] = []
        for (let i = 0; i < 6; i++) {
            rows.push(calendarCells.slice(i * 7, i * 7 + 7))
        }
        return rows
    }, [calendarCells])

    const textColor = config.textColor
    const mutedText = hexToRgba(config.textColor, 0.72)
    const subtleText = mixHex(config.textColor, config.backgroundColor, 0.48)
    const panelBorder = mixHex(config.textColor, config.backgroundColor, 0.74)
    const panelInnerBg = mixHex(config.backgroundColor, "#000000", 0.22)
    const softActionBg = mixHex(config.backgroundColor, "#000000", 0.15)
    const inputBg = mixHex(config.backgroundColor, "#000000", 0.36)
    const inputBorder = mixHex(config.textColor, config.backgroundColor, 0.65)
    const calendarCellBg = mixHex(config.leftPanelBgColor, "#000000", 0.2)
    const calendarCellMutedBg = mixHex(config.leftPanelBgColor, "#000000", 0.32)
    const selectedCellBg = mixHex(config.daysColor, "#000000", 0.06)
    const accentButtonBg = mixHex(config.daysColor, "#000000", 0.1)
    const settingsNumberInputStyle = {
        width: "100%",
        height: 24,
        fontSize: 10,
        backgroundColor: inputBg,
        borderWidth: 1,
        borderColor: inputBorder,
        color: textColor,
        paddingLeft: 8,
        paddingRight: 8,
        unityTextAlign: "MiddleLeft",
    }

    const persist = (nextConfig: CalendarWidgetConfig) => {
        const normalized = normalizeConfig(nextConfig)
        const targetEventsPath = normalizeConfigPath(normalized.eventsFilePath, DEFAULT_CONFIG.eventsFilePath)
        const nextNormalized = { ...normalized, eventsFilePath: targetEventsPath }
        const splitStorage = !isCombinedStoragePath(targetEventsPath)
        setConfig(nextNormalized)
        saveConfig(buildConfigForDisk(nextNormalized))
        if (splitStorage) saveEventsToFile(nextNormalized.events, targetEventsPath)
    }

    const focusDate = (iso: string) => {
        const parsed = parseIsoDate(iso)
        if (!parsed) return
        setSelectedDate(iso)
        setViewYear(parsed.getFullYear())
        setViewMonth(parsed.getMonth())
    }

    const clearDraft = () => {
        setDraftTitle("")
        setDeleteConfirmId(null)
    }

    const saveEventForSelectedDate = () => {
        const title = draftTitle.trim()
        if (!title) {
            setError("事件名称不能为空")
            return
        }

        if (!parseIsoDate(selectedDate)) {
            setError("当前选中日期无效")
            return
        }

        const nextEvents = [
            ...config.events,
            {
                id: `evt-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 7)}`,
                title,
                targetDate: selectedDate,
                type: inferEventType(selectedDate),
                pinned: false,
                createdAt: new Date().toISOString(),
            },
        ]

        persist({ ...config, events: sortEvents(nextEvents) })
        setDeleteConfirmId(null)
        clearDraft()
        setError("")
    }

    const startEdit = (item: CalendarEvent) => {
        setDeleteConfirmId(null)
        setEditDialogId(item.id)
        setEditDialogTitle(item.title)
        setEditDialogDate(item.targetDate)
        setEditDialogOpen(true)
        setError("")
    }

    const closeEditDialog = () => {
        setEditDialogOpen(false)
        setEditDialogId(null)
        setEditDialogTitle("")
        setEditDialogDate(selectedDate)
    }

    const saveEditedEvent = () => {
        const id = editDialogId
        if (!id) return

        const title = editDialogTitle.trim()
        if (!title) {
            setError("事件名称不能为空")
            return
        }

        const targetDate = normalizeText(editDialogDate, "")
        if (!parseIsoDate(targetDate)) {
            setError("目标日期无效，请使用 YYYY-MM-DD")
            return
        }

        const nextEvents = config.events.map((item) =>
            item.id === id
                ? { ...item, title, targetDate, type: inferEventType(targetDate) }
                : item
        )

        persist({ ...config, events: sortEvents(nextEvents) })
        focusDate(targetDate)
        setDeleteConfirmId(null)
        closeEditDialog()
        setError("")
    }

    const removeEvent = (id: string) => {
        const nextEvents = config.events.filter((item) => item.id !== id)
        persist({ ...config, events: nextEvents })
        setDeleteConfirmId(null)
        if (editDialogId === id) closeEditDialog()
    }

    const togglePinned = (id: string) => {
        setDeleteConfirmId(null)
        const nextEvents = config.events.map((item) =>
            item.id === id ? { ...item, pinned: !item.pinned } : item
        )
        persist({ ...config, events: nextEvents })
    }

    const goToToday = () => {
        focusDate(todayIso())
    }

    const shiftView = (delta: number) => {
        const shifted = shiftMonth(viewYear, viewMonth, delta)
        setViewYear(shifted.year)
        setViewMonth(shifted.month)
    }

    const openMonthPicker = () => {
        setEditDialogOpen(false)
        setYearCursor(viewYear)
        setMonthPickerOpen(true)
    }

    const openSettings = () => {
        setEditDialogOpen(false)
        setSettingsMenu("main")
        setSettingsDraft({
            backgroundColor: config.backgroundColor,
            textColor: config.textColor,
            daysColor: config.daysColor,
            cardDensity: config.cardDensity,
            eventsPerPage: config.eventsPerPage,
            pageLabelCount: config.pageLabelCount,
            eventsFilePath: config.eventsFilePath,
        })
        setSettingsOpen(true)
    }

    const enterMiddleMode = () => {
        setMonthPickerOpen(false)
        setSettingsOpen(false)
        setEditDialogOpen(false)
        setMiddleMode(true)
    }

    const exitMiddleMode = () => {
        setMiddleMode(false)
    }

    const applyThemePreset = (presetId: string) => {
        const preset = THEME_PRESETS.find((item) => item.id === presetId)
        if (!preset) return

        setSettingsDraft((prev) => ({
            ...prev,
            backgroundColor: preset.config.backgroundColor,
            textColor: preset.config.textColor,
            daysColor: preset.config.daysColor,
        }))
    }

    const setCompactCount = (count: 1 | 2 | 4) => {
        persist({ ...config, compactCount: count })
    }

    const colorDraftRows: Array<{ label: string; key: "backgroundColor" | "textColor" | "daysColor" }> = [
        { label: "整体背景", key: "backgroundColor" },
        { label: "主文字颜色", key: "textColor" },
        { label: "天数高亮色", key: "daysColor" },
    ]

    const paginationNumberRows: Array<{ label: string; key: SettingsNumberKey }> = [
        { label: "每页事件数", key: "eventsPerPage" },
        { label: "一页标签数（页码按钮）", key: "pageLabelCount" },
    ]

    const updateSettingsDraftNumber = (key: SettingsNumberKey, rawValue: unknown) => {
        const n = Math.round(Number(rawValue ?? ""))
        if (!Number.isFinite(n)) return
        setSettingsDraft((prev) => ({ ...prev, [key]: n }))
    }

    const renderSettingsNumberRow = (label: string, key: SettingsNumberKey) => (
        <div key={`settings-number-${key}`} style={{ marginBottom: 8 }}>
            <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>{label}</div>
            <textfield
                value={`${settingsDraft[key]}`}
                multiline={false}
                onValueChanged={(e: any) => updateSettingsDraftNumber(key, e?.newValue)}
                style={settingsNumberInputStyle}
            />
        </div>
    )

    const applySettings = () => {
        const nextEventsFilePath = normalizeConfigPath(settingsDraft.eventsFilePath, config.eventsFilePath)
        const backgroundColor = normalizeColor(settingsDraft.backgroundColor, config.backgroundColor)
        const textColor = normalizeColor(settingsDraft.textColor, config.textColor)
        const daysColor = normalizeColor(settingsDraft.daysColor, config.daysColor)
        const cardDensity = normalizeCardDensity(settingsDraft.cardDensity, config.cardDensity)
        const panelColors = derivePanelColors(backgroundColor)

        const nextConfig: CalendarWidgetConfig = {
            ...config,
            titleColor: textColor,
            daysColor,
            cardDensity,
            backgroundColor,
            leftPanelBgColor: panelColors.leftPanelBgColor,
            rightPanelBgColor: panelColors.rightPanelBgColor,
            textColor,
            eventsPerPage: normalizeIntInRange(settingsDraft.eventsPerPage, config.eventsPerPage, 1, 20),
            pageLabelCount: normalizeIntInRange(settingsDraft.pageLabelCount, config.pageLabelCount, 3, 9),
            eventsFilePath: nextEventsFilePath,
        }

        persist(nextConfig)
        setSettingsOpen(false)
    }

    useEffect(() => {
        setEventPage((page) => Math.min(page, totalEventPages))
    }, [totalEventPages])

    useEffect(() => {
        setDeleteConfirmId(null)
    }, [eventPage])

    const renderEventCard = (item: CalendarEvent) => {
        const dayDiff = calculateDays(item.targetDate)
        const dayText = formatDaysText(dayDiff)
        const isSimpleCard = config.cardDensity === "simple"
        const isEditing = editDialogOpen && editDialogId === item.id
        const linkedToSelected = item.targetDate === selectedDate
        const isDeletePending = deleteConfirmId === item.id
        const cardBg = isEditing ? mixHex(config.daysColor, config.rightPanelBgColor, 0.82) : panelInnerBg
        const cardBorder = linkedToSelected
            ? mixHex(config.daysColor, config.rightPanelBgColor, 0.24)
            : mixHex(config.textColor, config.rightPanelBgColor, 0.78)
        const actionBaseBg = mixHex(config.rightPanelBgColor, "#000000", 0.18)
        const actionPinBg = item.pinned ? mixHex(config.daysColor, config.rightPanelBgColor, 0.22) : actionBaseBg
        const actionDeleteBg = mixHex("#7f1d1d", config.rightPanelBgColor, 0.45)

        return (
            <div
                key={item.id}
                onPointerDown={() => focusDate(item.targetDate)}
                style={{
                    backgroundColor: cardBg,
                    borderWidth: 1,
                    borderColor: cardBorder,
                    borderRadius: 10,
                    flexShrink: 0,
                    minHeight: isSimpleCard ? 68 : 84,
                    paddingLeft: 9,
                    paddingRight: 9,
                    paddingTop: 6,
                    paddingBottom: 6,
                    marginBottom: 5,
                    position: "Relative",
                    overflow: "Hidden",
                }}
            >
                {!isSimpleCard ? (
                    <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "SpaceBetween", marginBottom: 4 }}>
                        <div style={{ fontSize: 9, color: subtleText }}>{item.targetDate}</div>

                        <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                            <div style={{ width: 34 }}>
                                <ActionButton
                                    text={item.pinned ? "UNP" : "PIN"}
                                    onClick={() => togglePinned(item.id)}
                                    color={item.pinned ? mixHex("#0b1120", config.textColor, 0.2) : textColor}
                                    bg={actionPinBg}
                                    compact
                                />
                            </div>
                            <div style={{ width: 4 }} />
                            <div style={{ width: 34 }}>
                                <ActionButton text="EDT" onClick={() => startEdit(item)} color={textColor} bg={actionBaseBg} compact />
                            </div>
                            <div style={{ width: 4 }} />
                            <div style={{ width: 34 }}>
                                <ActionButton text="DEL" onClick={() => setDeleteConfirmId(item.id)} color="#fecaca" bg={actionDeleteBg} compact />
                            </div>
                        </div>
                    </div>
                ) : null}

                <div
                    style={{
                        fontSize: 12,
                        color: config.titleColor,
                        unityFontStyleAndWeight: "Bold",
                        whiteSpace: "NoWrap",
                        overflow: "Hidden",
                        textOverflow: "Ellipsis",
                        marginBottom: isSimpleCard ? 4 : 6,
                    }}
                >
                    {item.title}
                </div>

                <div style={{ fontSize: 15, color: config.daysColor, unityFontStyleAndWeight: "Bold" }}>{dayText}</div>

                {isDeletePending ? (
                    <div
                        onPointerDown={(e: any) => {
                            e?.stopPropagation?.()
                        }}
                        style={{
                            position: "Absolute",
                            left: 0,
                            right: 0,
                            top: 0,
                            bottom: 0,
                            backgroundColor: mixHex(config.rightPanelBgColor, "#000000", 0.28),
                            borderRadius: 10,
                            display: "Flex",
                            flexDirection: "Column",
                            justifyContent: "Center",
                            alignItems: "Center",
                        }}
                    >
                        <div style={{ fontSize: 10, color: "#fca5a5", unityFontStyleAndWeight: "Bold", marginBottom: 6 }}>
                            CONFIRM DELETE?
                        </div>
                        <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                            <div style={{ width: 36 }}>
                                <ActionButton text="YES" onClick={() => removeEvent(item.id)} color="#dcfce7" bg="#14532d" compact />
                            </div>
                            <div style={{ width: 4 }} />
                            <div style={{ width: 36 }}>
                                <ActionButton text="NO" onClick={() => setDeleteConfirmId(null)} color={textColor} bg={actionBaseBg} compact />
                            </div>
                        </div>
                    </div>
                ) : null}
            </div>
        )
    }

    const renderPagedEventCards = (showCountLabel: boolean) => (
        <>
            {showCountLabel ? (
                <div
                    style={{
                        fontSize: 10,
                        color: textColor,
                        marginBottom: 4,
                        whiteSpace: "NoWrap",
                        display: "Flex",
                        flexDirection: "Row",
                        alignItems: "Center",
                    }}
                >
                    {`全部事件（置顶优先）: ${allEvents.length}`}
                </div>
            ) : null}

            <div
                ref={eventListRef}
                style={{
                    flexGrow: 1,
                    flexShrink: 1,
                    minHeight: 0,
                    backgroundColor: panelInnerBg,
                    borderWidth: 1,
                    borderColor: panelBorder,
                    borderRadius: 8,
                    padding: 6,
                    overflow: "Hidden",
                }}
            >
                {allEvents.length === 0 ? (
                    <div style={{ fontSize: 10, color: mutedText }}>暂无事件，先在左侧选日期后添加一个。</div>
                ) : (
                    <div style={{ display: "Flex", flexDirection: "Column", alignItems: "Stretch", minWidth: 0 }}>
                        {pagedEvents.map((item) => renderEventCard(item))}
                    </div>
                )}
            </div>

            <div ref={pagerRowRef} style={{ display: "Flex", justifyContent: "Center", alignItems: "Center", marginTop: 4 }}>
                <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                    <ActionButton text="<<" onClick={() => setEventPage(1)} disabled={eventPage <= 1} color={textColor} bg={softActionBg} compact />
                    <div style={{ width: 4 }} />
                    <ActionButton text="<" onClick={() => setEventPage((page) => Math.max(1, page - 1))} disabled={eventPage <= 1} color={textColor} bg={softActionBg} compact />
                    <div style={{ width: 4 }} />
                    {visiblePageNumbers.map((pageNumber) => (
                        <div key={`page-${pageNumber}`} style={{ marginRight: 4 }}>
                            <ActionButton
                                text={`${pageNumber}`}
                                onClick={() => setEventPage(pageNumber)}
                                color={textColor}
                                bg={eventPage === pageNumber ? accentButtonBg : softActionBg}
                                compact
                            />
                        </div>
                    ))}
                    <ActionButton
                        text=">"
                        onClick={() => setEventPage((page) => Math.min(totalEventPages, page + 1))}
                        disabled={eventPage >= totalEventPages}
                        color={textColor}
                        bg={softActionBg}
                        compact
                    />
                    <div style={{ width: 4 }} />
                    <ActionButton
                        text=">>"
                        onClick={() => setEventPage(totalEventPages)}
                        disabled={eventPage >= totalEventPages}
                        color={textColor}
                        bg={softActionBg}
                        compact
                    />
                </div>
            </div>
        </>
    )

    return (
        <div
            style={{
                flexGrow: 1,
                width: "100%",
                height: "100%",
                display: "Flex",
                flexDirection: "Column",
                backgroundColor: config.backgroundColor,
                paddingLeft: 8,
                paddingRight: 8,
                paddingTop: 8,
                paddingBottom: 8,
                position: "Relative",
            }}
        >
            {middleMode ? (
                <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", minHeight: 0, alignItems: "FlexStart" }}>
                    <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 6 }}>
                        <ActionButton text="展开" onClick={exitMiddleMode} color={textColor} bg={accentButtonBg} compact />
                    </div>

                    <div
                        style={{
                            flexGrow: 1,
                            width: "100%",
                            maxWidth: 452,
                            backgroundColor: config.rightPanelBgColor,
                            borderRadius: 8,
                            borderWidth: 1,
                            borderColor: panelBorder,
                            paddingLeft: 7,
                            paddingRight: 7,
                            paddingTop: 7,
                            paddingBottom: 7,
                            display: "Flex",
                            flexDirection: "Column",
                            minHeight: 0,
                            overflow: "Hidden",
                        }}
                    >
                        {renderPagedEventCards(false)}
                    </div>
                </div>
            ) : (
                <>
                    <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 6 }}>
                        <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                            <ActionButton text="收起" onClick={enterMiddleMode} color={textColor} bg={softActionBg} compact />
                            <div style={{ width: 6 }} />
                            <div style={{ fontSize: 12, color: textColor, unityFontStyleAndWeight: "Bold" }}>日历计时</div>
                        </div>
                        <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                            <div style={{ fontSize: 9, color: subtleText, marginRight: 4 }}>小卡</div>
                            {[1, 2, 4].map((countValue) => (
                                <div key={`compact-count-${countValue}`} style={{ marginRight: 3 }}>
                                    <ActionButton
                                        text={`${countValue}`}
                                        onClick={() => setCompactCount(countValue as 1 | 2 | 4)}
                                        color={textColor}
                                        bg={config.compactCount === countValue ? accentButtonBg : softActionBg}
                                        compact
                                    />
                                </div>
                            ))}
                            <ActionButton text="设置" onClick={openSettings} bg={accentButtonBg} color={textColor} compact />
                        </div>
                    </div>

                    {error ? <div style={{ fontSize: 10, color: "#fca5a5", marginBottom: 6 }}>{error}</div> : null}

                    <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", minHeight: 0 }}>
                        <div
                            style={{
                                backgroundColor: config.leftPanelBgColor,
                                borderRadius: 8,
                                borderWidth: 1,
                                borderColor: panelBorder,
                                paddingLeft: 7,
                                paddingRight: 7,
                                paddingTop: 7,
                                paddingBottom: 7,
                                marginBottom: 6,
                            }}
                        >
                            <div style={{ width: "100%", maxWidth: 360, marginLeft: "Auto", marginRight: "Auto" }}>
                                <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 6, minWidth: 0, width: "100%" }}>
                                    <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                                        <ActionButton text="今天" onClick={goToToday} color={textColor} bg={softActionBg} compact />
                                        <div style={{ width: 3 }} />
                                        <ActionButton text="<" onClick={() => shiftView(-1)} color={textColor} bg={softActionBg} compact />
                                    </div>
                                    <div style={{ flexGrow: 1, minWidth: 0, paddingLeft: 4, paddingRight: 4, display: "Flex", justifyContent: "Center" }}>
                                        <div
                                            onPointerDown={openMonthPicker}
                                            style={{
                                                fontSize: 11,
                                                color: textColor,
                                                backgroundColor: panelInnerBg,
                                                borderRadius: 6,
                                                borderWidth: 1,
                                                borderColor: panelBorder,
                                                paddingLeft: 8,
                                                paddingRight: 8,
                                                paddingTop: 4,
                                                paddingBottom: 4,
                                                minWidth: 84,
                                                maxWidth: "100%",
                                                whiteSpace: "NoWrap",
                                                overflow: "Hidden",
                                                textOverflow: "Ellipsis",
                                                unityTextAlign: "MiddleCenter",
                                            }}
                                        >
                                            {monthTitle(viewYear, viewMonth)}
                                        </div>
                                    </div>
                                    <ActionButton text=">" onClick={() => shiftView(1)} color={textColor} bg={softActionBg} compact />
                                </div>

                                <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 3, width: "100%" }}>
                                    {WEEK_LABELS.map((label) => (
                                        <div
                                            key={`week-${label}`}
                                            style={{
                                                width: "14.2857%",
                                                flexGrow: 0,
                                                flexShrink: 0,
                                                fontSize: 9,
                                                color: subtleText,
                                                unityTextAlign: "MiddleCenter",
                                            }}
                                        >
                                            {label}
                                        </div>
                                    ))}
                                </div>

                                {calendarRows.map((row, rowIndex) => (
                                    <div key={`row-${rowIndex}`} style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 3, width: "100%" }}>
                                        {row.map((cell) => {
                                            const isSelected = cell.iso === selectedDate
                                            const isToday = cell.iso === todayIsoValue
                                            const count = eventCountByDate.get(cell.iso) || 0

                                            return (
                                                <div
                                                    key={cell.iso}
                                                    onPointerDown={() => focusDate(cell.iso)}
                                                    style={{
                                                        width: "14.2857%",
                                                        flexGrow: 0,
                                                        flexShrink: 0,
                                                        height: 33,
                                                        borderRadius: 6,
                                                        backgroundColor: isSelected
                                                            ? selectedCellBg
                                                            : (cell.inCurrentMonth ? calendarCellBg : calendarCellMutedBg),
                                                        borderWidth: isToday ? 1 : 0,
                                                        borderColor: isToday ? config.daysColor : "transparent",
                                                        display: "Flex",
                                                        flexDirection: "Column",
                                                        justifyContent: "Center",
                                                        alignItems: "Center",
                                                    }}
                                                >
                                                    <div
                                                        style={{
                                                            fontSize: 10,
                                                            color: isSelected ? "#0b1120" : (cell.inCurrentMonth ? textColor : subtleText),
                                                            unityFontStyleAndWeight: isSelected ? "Bold" : "Normal",
                                                        }}
                                                    >
                                                        {cell.day}
                                                    </div>
                                                    <div style={{ fontSize: 8, color: isSelected ? "#0b1120" : config.daysColor }}>{count > 0 ? `•${count}` : ""}</div>
                                                </div>
                                            )
                                        })}
                                    </div>
                                ))}
                            </div>
                        </div>

                        <div
                            style={{
                                flexGrow: 1,
                                backgroundColor: config.rightPanelBgColor,
                                borderRadius: 8,
                                borderWidth: 1,
                                borderColor: panelBorder,
                                paddingLeft: 7,
                                paddingRight: 7,
                                paddingTop: 7,
                                paddingBottom: 7,
                                display: "Flex",
                                flexDirection: "Column",
                                minHeight: 0,
                                overflow: "Hidden",
                            }}
                        >
                            <div style={{ fontSize: 11, color: textColor, marginBottom: 3, unityFontStyleAndWeight: "Bold" }}>
                                选中日期: {selectedDate}
                            </div>

                            <div style={{ backgroundColor: panelInnerBg, borderRadius: 8, borderWidth: 1, borderColor: panelBorder, padding: 7, marginBottom: 6 }}>
                                <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>快速添加事件</div>

                                <textfield
                                    value={draftTitle}
                                    multiline={false}
                                    onValueChanged={(e: any) => setDraftTitle(e?.newValue ?? "")}
                                    style={{
                                        width: "100%",
                                        flexGrow: 1,
                                        height: 24,
                                        fontSize: 10,
                                        backgroundColor: inputBg,
                                        borderWidth: 1,
                                        borderColor: inputBorder,
                                        color: textColor,
                                        paddingLeft: 8,
                                        paddingRight: 8,
                                        paddingTop: 3,
                                        paddingBottom: 3,
                                        unityTextAlign: "MiddleLeft",
                                        marginBottom: 5,
                                    }}
                                />

                                <div style={{ display: "Flex", flexDirection: "Row" }}>
                                    <ActionButton text="添加到选中日期" onClick={saveEventForSelectedDate} color={textColor} bg={accentButtonBg} compact />
                                </div>
                            </div>

                            {renderPagedEventCards(true)}
                        </div>
                    </div>
                </>
            )}
            {monthPickerOpen ? (
                <div
                    style={{
                        position: "Absolute",
                        left: 0,
                        right: 0,
                        top: 0,
                        bottom: 0,
                        width: "100%",
                        height: "100%",
                        backgroundColor: config.backgroundColor,
                        display: "Flex",
                        justifyContent: "Center",
                        alignItems: "Center",
                        paddingLeft: 20,
                        paddingRight: 20,
                    }}
                >
                    <div
                        style={{
                            width: "92%",
                            maxWidth: 340,
                            maxHeight: "88%",
                            overflow: "Auto",
                            backgroundColor: mixHex(config.leftPanelBgColor, "#000000", 0.08),
                            borderRadius: 10,
                            paddingLeft: 10,
                            paddingRight: 10,
                            paddingTop: 10,
                            paddingBottom: 10,
                            borderWidth: 1,
                            borderColor: panelBorder,
                        }}
                    >
                        <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 8 }}>
                            <div style={{ fontSize: 12, color: textColor, unityFontStyleAndWeight: "Bold" }}>选择年月</div>
                            <ActionButton text="关闭" onClick={() => setMonthPickerOpen(false)} color={textColor} bg={softActionBg} compact />
                        </div>

                        <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "Center", alignItems: "Center", marginBottom: 8 }}>
                            <ActionButton text="-10" onClick={() => setYearCursor((y) => y - 10)} color={textColor} bg={softActionBg} compact />
                            <div style={{ width: 3 }} />
                            <ActionButton text="-1" onClick={() => setYearCursor((y) => y - 1)} color={textColor} bg={softActionBg} compact />
                            <div style={{ width: 8 }} />
                            <div style={{ fontSize: 12, color: textColor, width: 70, unityTextAlign: "MiddleCenter" }}>{yearCursor}年</div>
                            <div style={{ width: 8 }} />
                            <ActionButton text="+1" onClick={() => setYearCursor((y) => y + 1)} color={textColor} bg={softActionBg} compact />
                            <div style={{ width: 3 }} />
                            <ActionButton text="+10" onClick={() => setYearCursor((y) => y + 10)} color={textColor} bg={softActionBg} compact />
                        </div>

                        <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "Center", marginBottom: 8 }}>
                            {Array.from({ length: 5 }, (_, i) => yearCursor - 2 + i).map((year, i) => (
                                <div key={`year-select-${year}`} style={{ marginRight: i === 4 ? 0 : 4 }}>
                                    <ActionButton
                                        text={`${year}`}
                                        onClick={() => setYearCursor(year)}
                                        color={textColor}
                                        bg={year === yearCursor ? accentButtonBg : softActionBg}
                                        compact
                                    />
                                </div>
                            ))}
                        </div>

                        <div style={{ display: "Flex", flexDirection: "Row", flexWrap: "Wrap", justifyContent: "Center" }}>
                            {MONTH_LABELS.map((label, monthIndex) => (
                                <div key={`month-${monthIndex}`} style={{ width: 76, marginRight: 4, marginBottom: 4 }}>
                                    <ActionButton
                                        text={label}
                                        onClick={() => {
                                            setViewYear(yearCursor)
                                            setViewMonth(monthIndex)
                                            setMonthPickerOpen(false)
                                        }}
                                        color={textColor}
                                        bg={yearCursor === viewYear && monthIndex === viewMonth ? accentButtonBg : softActionBg}
                                        compact
                                    />
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            ) : null}

            {editDialogOpen ? (
                <div
                    style={{
                        position: "Absolute",
                        left: 0,
                        right: 0,
                        top: 0,
                        bottom: 0,
                        width: "100%",
                        height: "100%",
                        backgroundColor: config.backgroundColor,
                        display: "Flex",
                        justifyContent: "Center",
                        alignItems: "Center",
                        paddingLeft: 20,
                        paddingRight: 20,
                    }}
                >
                    <div
                        style={{
                            width: "92%",
                            maxWidth: 360,
                            maxHeight: "88%",
                            overflow: "Auto",
                            backgroundColor: mixHex(config.leftPanelBgColor, "#000000", 0.08),
                            borderRadius: 10,
                            paddingLeft: 10,
                            paddingRight: 10,
                            paddingTop: 10,
                            paddingBottom: 10,
                            borderWidth: 1,
                            borderColor: panelBorder,
                        }}
                    >
                        <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 8 }}>
                            <div style={{ fontSize: 12, color: textColor, unityFontStyleAndWeight: "Bold" }}>编辑事件</div>
                            <ActionButton text="关闭" onClick={closeEditDialog} color={textColor} bg={softActionBg} compact />
                        </div>

                        <div style={{ marginBottom: 8 }}>
                            <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>事件名称</div>
                            <textfield
                                value={editDialogTitle}
                                multiline={false}
                                onValueChanged={(e: any) => setEditDialogTitle(e?.newValue ?? "")}
                                style={{
                                    width: "100%",
                                    height: 24,
                                    fontSize: 10,
                                    backgroundColor: inputBg,
                                    borderWidth: 1,
                                    borderColor: inputBorder,
                                    color: textColor,
                                    paddingLeft: 8,
                                    paddingRight: 8,
                                    unityTextAlign: "MiddleLeft",
                                }}
                            />
                        </div>

                        <div style={{ marginBottom: 8 }}>
                            <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>目标日期（YYYY-MM-DD）</div>
                            <textfield
                                value={editDialogDate}
                                multiline={false}
                                onValueChanged={(e: any) => setEditDialogDate(e?.newValue ?? "")}
                                style={{
                                    width: "100%",
                                    height: 24,
                                    fontSize: 10,
                                    backgroundColor: inputBg,
                                    borderWidth: 1,
                                    borderColor: inputBorder,
                                    color: textColor,
                                    paddingLeft: 8,
                                    paddingRight: 8,
                                    unityTextAlign: "MiddleLeft",
                                }}
                            />
                        </div>

                        <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "FlexEnd", alignItems: "Center" }}>
                            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                                <ActionButton text="保存" onClick={saveEditedEvent} color={textColor} bg={accentButtonBg} compact />
                                <div style={{ width: 4 }} />
                                <ActionButton text="取消" onClick={closeEditDialog} color={textColor} bg={softActionBg} compact />
                            </div>
                        </div>
                    </div>
                </div>
            ) : null}

            {settingsOpen ? (
                <div
                    style={{
                        position: "Absolute",
                        left: 0,
                        right: 0,
                        top: 0,
                        bottom: 0,
                        width: "100%",
                        height: "100%",
                        backgroundColor: config.backgroundColor,
                        display: "Flex",
                        justifyContent: "Center",
                        alignItems: "Center",
                        paddingLeft: 20,
                        paddingRight: 20,
                    }}
                >
                    <div
                        style={{
                            width: "92%",
                            maxWidth: 380,
                            maxHeight: "88%",
                            overflow: "Auto",
                            backgroundColor: mixHex(config.leftPanelBgColor, "#000000", 0.08),
                            borderRadius: 10,
                            paddingLeft: 10,
                            paddingRight: 10,
                            paddingTop: 10,
                            paddingBottom: 10,
                            borderWidth: 1,
                            borderColor: panelBorder,
                        }}
                    >
                        <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 8 }}>
                            <div style={{ fontSize: 12, color: textColor, unityFontStyleAndWeight: "Bold" }}>
                                {settingsMenu === "main" ? "设置" : "设置 / 自定义颜色"}
                            </div>
                            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                                {settingsMenu === "colors" ? (
                                    <>
                                        <ActionButton text="返回" onClick={() => setSettingsMenu("main")} color={textColor} bg={softActionBg} compact />
                                        <div style={{ width: 4 }} />
                                    </>
                                ) : null}
                                <ActionButton text="关闭" onClick={() => setSettingsOpen(false)} color={textColor} bg={softActionBg} compact />
                            </div>
                        </div>

                        {settingsMenu === "main" ? (
                            <div key="settings-main-panel" style={{ display: "Flex", flexDirection: "Column" }}>
                                <div style={{ marginBottom: 10 }}>
                                    <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>事件 JSON 路径</div>
                                    <textfield
                                        value={settingsDraft.eventsFilePath}
                                        multiline={false}
                                        onValueChanged={(e: any) => setSettingsDraft((prev) => ({ ...prev, eventsFilePath: e?.newValue ?? "" }))}
                                        style={settingsNumberInputStyle}
                                    />
                                    <div style={{ fontSize: 9, color: mutedText, marginTop: 3 }}>
                                        例如：window-states/countdown-days-events.json
                                    </div>
                                </div>

                                <div key="settings-main-color-entry" style={{ marginBottom: 10, display: "Flex", flexDirection: "Column", alignItems: "Stretch" }}>
                                    <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>颜色</div>
                                    <div style={{ width: "100%" }}>
                                        <ActionButton text="自定义颜色" onClick={() => setSettingsMenu("colors")} color={textColor} bg={accentButtonBg} compact fullWidth />
                                    </div>
                                </div>

                                <div style={{ marginBottom: 10 }}>
                                    <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>卡片信息密度</div>
                                    <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                                        <ActionButton
                                            text="简洁"
                                            onClick={() => setSettingsDraft((prev) => ({ ...prev, cardDensity: "simple" }))}
                                            color={textColor}
                                            bg={settingsDraft.cardDensity === "simple" ? accentButtonBg : softActionBg}
                                            compact
                                        />
                                        <div style={{ width: 6 }} />
                                        <ActionButton
                                            text="标准"
                                            onClick={() => setSettingsDraft((prev) => ({ ...prev, cardDensity: "standard" }))}
                                            color={textColor}
                                            bg={settingsDraft.cardDensity === "standard" ? accentButtonBg : softActionBg}
                                            compact
                                        />
                                    </div>
                                </div>

                                <div style={{ fontSize: 10, color: textColor, marginBottom: 6 }}>分页设置</div>
                                {paginationNumberRows.map((row) => renderSettingsNumberRow(row.label, row.key))}
                            </div>
                        ) : (
                            <div key="settings-colors-panel" style={{ display: "Flex", flexDirection: "Column" }}>
                                <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>默认主题</div>
                                <div style={{ display: "Flex", flexDirection: "Row", flexWrap: "Wrap", marginBottom: 8 }}>
                                    {THEME_PRESETS.map((preset) => (
                                        <div key={preset.id} style={{ marginRight: 4, marginBottom: 4 }}>
                                            <ActionButton
                                                text={preset.name}
                                                onClick={() => applyThemePreset(preset.id)}
                                                color={textColor}
                                                bg={softActionBg}
                                                compact
                                            />
                                        </div>
                                    ))}
                                </div>

                                <div style={{ fontSize: 10, color: textColor, marginBottom: 6 }}>自定义颜色</div>
                                {colorDraftRows.map((row) => (
                                    <ColorEditorRow
                                        key={`color-row-${row.key}`}
                                        label={row.label}
                                        value={settingsDraft[row.key]}
                                        onChange={(value) => setSettingsDraft((prev) => ({ ...prev, [row.key]: value }))}
                                        textColor={textColor}
                                        inputBg={inputBg}
                                        inputBorder={inputBorder}
                                    />
                                ))}
                            </div>
                        )}

                        {settingsMenu === "main" ? (
                            <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "FlexEnd" }}>
                                <ActionButton text="应用设置" onClick={applySettings} color={textColor} bg={accentButtonBg} />
                            </div>
                        ) : (
                            <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "FlexEnd" }}>
                                <ActionButton text="应用颜色" onClick={applySettings} color={textColor} bg={accentButtonBg} compact />
                            </div>
                        )}
                    </div>
                </div>
            ) : null}
        </div>
    )
}

const CountdownCompact = () => {
    const [config, setConfig] = useState<CalendarWidgetConfig>(DEFAULT_CONFIG)
    const [, setClockTick] = useState(0)

    useEffect(() => {
        const syncCompactConfig = () => {
            const loadedSettings = loadConfig()
            const loadedEventsPath = normalizeConfigPath(loadedSettings.eventsFilePath, DEFAULT_CONFIG.eventsFilePath)
            const loadedEvents = loadEventsFromFile(loadedEventsPath)
            const nextConfig = {
                ...loadedSettings,
                eventsFilePath: loadedEventsPath,
                events: sortEvents(loadedEvents ?? loadedSettings.events),
            }
            setConfig((prev) => (areCompactConfigEqual(prev, nextConfig) ? prev : nextConfig))
        }

        syncCompactConfig()
        const timer = setInterval(() => {
            setClockTick((tick) => tick + 1)
            syncCompactConfig()
        }, COMPACT_SYNC_INTERVAL_MS)

        return () => clearInterval(timer)
    }, [])

    const compactEvents = useMemo(() => {
        return sortEventsPinnedFirst(config.events).slice(0, config.compactCount)
    }, [config.events, config.compactCount])

    const compactPanel = mixHex(config.rightPanelBgColor, "#000000", 0.18)
    const compactBorder = mixHex(config.textColor, config.rightPanelBgColor, 0.68)
    const autoCols = config.compactCount >= 4 ? 2 : 1
    const compactRows = useMemo(() => {
        if (compactEvents.length === 0) return [] as CalendarEvent[][]
        if (autoCols <= 1) return compactEvents.map((eventItem) => [eventItem])

        const rows: CalendarEvent[][] = []
        for (let i = 0; i < compactEvents.length; i += autoCols) {
            rows.push(compactEvents.slice(i, i + autoCols))
        }
        return rows
    }, [compactEvents, autoCols])

    const compactFont = config.compactCount === 4 ? 13 : 16
    const titleFont = compactFont
    const dayFont = compactFont
    const showCompactDate = config.compactCount === 1 && compactEvents.length === 1

    return (
        <div
            style={{
                flexGrow: 1,
                display: "Flex",
                flexDirection: "Column",
                backgroundColor: config.rightPanelBgColor,
                paddingLeft: 6,
                paddingRight: 6,
                paddingTop: 30,
                paddingBottom: 8,
                overflow: "Hidden",
            }}
        >
            {compactEvents.length === 0 ? (
                <div
                    style={{
                        flexGrow: 1,
                        fontSize: 18,
                        color: hexToRgba(config.textColor, 0.72),
                        unityFontStyleAndWeight: "Bold",
                        unityTextAlign: "MiddleCenter",
                    }}
                >
                    暂无事件
                </div>
            ) : (
                <div
                    style={{
                        flexGrow: 1,
                        display: "Flex",
                        flexDirection: "Column",
                        minHeight: 0,
                    }}
                >
                    {compactRows.map((row, rowIndex) => (
                        <div
                            key={`compact-row-${rowIndex}`}
                            style={{
                                display: "Flex",
                                flexDirection: "Row",
                                flexGrow: 1,
                                minHeight: 0,
                                marginBottom: rowIndex < compactRows.length - 1 ? COMPACT_CARD_GAP : 0,
                            }}
                        >
                            {row.map((item, colIndex) => (
                                <div
                                    key={item.id}
                                    style={{
                                        flexGrow: 1,
                                        flexShrink: 1,
                                        flexBasis: autoCols > 1 ? 0 : "100%",
                                        width: autoCols > 1 ? undefined : "100%",
                                        height: "100%",
                                        marginRight: autoCols > 1 && colIndex < autoCols - 1 ? COMPACT_CARD_GAP : 0,
                                        backgroundColor: compactPanel,
                                        borderWidth: 1,
                                        borderColor: compactBorder,
                                        borderRadius: 8,
                                        paddingLeft: 7,
                                        paddingRight: 7,
                                        paddingTop: 5,
                                        paddingBottom: 5,
                                        minWidth: 0,
                                        minHeight: 0,
                                        overflow: "Hidden",
                                        display: "Flex",
                                        flexDirection: "Column",
                                        justifyContent: "Center",
                                        alignItems: "Center",
                                    }}
                                >
                                    <div
                                        style={{
                                            width: "100%",
                                            display: "Flex",
                                            flexDirection: "Column",
                                            alignItems: "Center",
                                            justifyContent: "Center",
                                        }}
                                    >
                                        <div
                                            style={{
                                                fontSize: titleFont,
                                                color: config.titleColor,
                                                unityFontStyleAndWeight: "Bold",
                                                unityTextAlign: "MiddleCenter",
                                                marginBottom: 2,
                                            }}
                                        >
                                            {item.pinned ? "* " : ""}{item.title}
                                        </div>
                                        {showCompactDate ? (
                                            <div
                                                style={{
                                                    fontSize: compactFont,
                                                    color: hexToRgba(config.textColor, 0.72),
                                                    unityTextAlign: "MiddleCenter",
                                                    marginBottom: 2,
                                                }}
                                            >
                                                {item.targetDate}
                                            </div>
                                        ) : null}
                                    </div>
                                    <div
                                        style={{
                                            fontSize: dayFont,
                                            color: config.daysColor,
                                            unityFontStyleAndWeight: "Bold",
                                            unityTextAlign: "MiddleCenter",
                                        }}
                                    >
                                        {formatDaysText(calculateDays(item.targetDate))}
                                    </div>
                                </div>
                            ))}
                            {autoCols > 1 && row.length < autoCols ? (
                                <div
                                    style={{
                                        flexGrow: 1,
                                        flexShrink: 1,
                                        flexBasis: 0,
                                        minWidth: 0,
                                    }}
                                />
                            ) : null}
                        </div>
                    ))}
                </div>
            )}
        </div>
    )
}

__registerPlugin({
    id: "countdown-days",
    title: "Countdown Days",
    width: 390,
    height: 830,
    initialX: 320,
    initialY: 120,
    resizable: true,
    compact: {
        width: 270,
        height: 170,
        component: CountdownCompact,
    },
    launcher: {
        text: "DD",
        background: "#0ea5e9",
    },
    component: CountdownPanel,
})
