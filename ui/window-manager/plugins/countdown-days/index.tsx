import { h } from "preact"
import { useCallback, useEffect, useMemo, useRef, useState } from "preact/hooks"

declare const __registerPlugin: any
declare const chill: any

type EventType = "countdown" | "elapsed"
type SettingsMenu = "main" | "colors"
type SettingsNumberKey = "eventsPerPage" | "pageLabelCount"
type CardDensity = "simple" | "standard"
type SettingsColorKey = "backgroundColor" | "leftPanelBgColor" | "rightPanelBgColor" | "textColor" | "titleColor" | "daysColor"

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
    titleColor: string
    daysColor: string
    backgroundColor: string
    leftPanelBgColor: string
    rightPanelBgColor: string
    textColor: string
}

type PersistedSettings = Omit<CalendarWidgetConfig, "events">

type CalendarCell = {
    iso: string
    year: number
    month: number
    day: number
    inCurrentMonth: boolean
}

type SettingsDraft = {
    backgroundColor: string
    leftPanelBgColor: string
    rightPanelBgColor: string
    textColor: string
    titleColor: string
    daysColor: string
    cardDensity: CardDensity
    eventsPerPage: string
    pageLabelCount: string
    eventsFilePath: string
}

type LayoutHint = {
    windowWidth: number
    windowHeight: number
    listWidth: number
    listHeight: number
}

const CONFIG_FILE = "window-states/countdown-days.json"
const EVENTS_FILE = "window-states/countdown-days-events.json"
const DEFAULT_EVENTS_PER_PAGE = 8
const DEFAULT_PAGE_LABEL_COUNT = 5
const DEFAULT_LAYOUT_HINT: LayoutHint = {
    windowWidth: 0,
    windowHeight: 0,
    listWidth: 0,
    listHeight: 0,
}
const EVENT_CARD_ROW_HEIGHT_STANDARD = 90
const EVENT_CARD_ROW_HEIGHT_SIMPLE = 74
const PAGER_SLOT_WIDTH = 34
const PAGER_BUTTON_GAP = 4
const PAGER_SIDE_GROUP_WIDTH = PAGER_SLOT_WIDTH * 2 + PAGER_BUTTON_GAP
const COMPACT_CARD_GAP = 4
const LAYOUT_HINT_SETTLE_DELAY_MS = 140
const LAYOUT_HINT_JITTER_PX = 2

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

const DEFAULT_SETTINGS: PersistedSettings = {
    eventsFilePath: EVENTS_FILE,
    compactCount: 2,
    cardDensity: "standard",
    eventsPerPage: DEFAULT_EVENTS_PER_PAGE,
    pageLabelCount: DEFAULT_PAGE_LABEL_COUNT,
    titleColor: "#dbeafe",
    daysColor: "#22d3ee",
    backgroundColor: "#13264a",
    leftPanelBgColor: "#1a305a",
    rightPanelBgColor: "#1a305a",
    textColor: "#dbeafe",
}

const DEFAULT_CONFIG: CalendarWidgetConfig = {
    ...DEFAULT_SETTINGS,
    events: [],
}

const createSettingsDraft = (config: CalendarWidgetConfig): SettingsDraft => ({
    backgroundColor: normalizeColor(config.backgroundColor, DEFAULT_CONFIG.backgroundColor),
    leftPanelBgColor: normalizeColor(config.leftPanelBgColor, DEFAULT_CONFIG.leftPanelBgColor),
    rightPanelBgColor: normalizeColor(config.rightPanelBgColor, DEFAULT_CONFIG.rightPanelBgColor),
    textColor: normalizeColor(config.textColor, DEFAULT_CONFIG.textColor),
    titleColor: normalizeColor(config.titleColor, DEFAULT_CONFIG.titleColor),
    daysColor: normalizeColor(config.daysColor, DEFAULT_CONFIG.daysColor),
    cardDensity: normalizeCardDensity(config.cardDensity, DEFAULT_CONFIG.cardDensity),
    eventsPerPage: String(normalizeIntInRange(config.eventsPerPage, DEFAULT_CONFIG.eventsPerPage, 1, 20)),
    pageLabelCount: String(normalizeIntInRange(config.pageLabelCount, DEFAULT_CONFIG.pageLabelCount, 3, 9)),
    eventsFilePath: normalizeEventsFilePath(config.eventsFilePath, DEFAULT_SETTINGS.eventsFilePath),
})

const sanitizeSettingsNumberText = (value: unknown) => {
    return String(value ?? "").replace(/[^0-9]/g, "").slice(0, 2)
}

const ensureSettingsNumberText = (value: unknown, fallback: number) => {
    if (typeof value === "string") return value
    if (value === undefined || value === null) return String(fallback)
    return sanitizeSettingsNumberText(value)
}

const parseSettingsNumberDraft = (value: unknown, fallback: number, min: number, max: number) => {
    const text = typeof value === "string" ? value.trim() : String(value ?? "").trim()
    if (text.length === 0) return fallback
    return normalizeIntInRange(Number(text), fallback, min, max)
}

const ensureSettingsNumber = (value: unknown, fallback: number) => {
    const n = Math.round(Number(value))
    return Number.isFinite(n) ? n : fallback
}

const ensureColorString = (value: unknown, fallback: string) => {
    return typeof value === "string" && value.trim().length > 0 ? value : fallback
}

const ensureColorDraftText = (value: unknown, fallback: string) => {
    if (typeof value === "string") return value
    return fallback
}

const getDraftColorSwatch = (value: unknown, fallback: string) => {
    const raw = typeof value === "string" ? value : fallback
    return normalizeColor(raw, fallback)
}

const sameColor = (a: unknown, b: unknown) => getDraftColorSwatch(a, "").toLowerCase() === getDraftColorSwatch(b, "").toLowerCase()

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

const normalizeEventsFilePath = (value: unknown, fallback: string) => {
    const normalizedFallback = normalizeConfigPath(fallback, EVENTS_FILE)
    const normalized = normalizeConfigPath(value, normalizedFallback)
    return canonicalizePath(normalized) === canonicalizePath(CONFIG_FILE)
        ? normalizedFallback
        : normalized
}

const extractPersistedSettings = (config: PersistedSettings | CalendarWidgetConfig): PersistedSettings => ({
    eventsFilePath: normalizeEventsFilePath(config.eventsFilePath, DEFAULT_SETTINGS.eventsFilePath),
    compactCount: normalizeCompactCount(config.compactCount, DEFAULT_SETTINGS.compactCount),
    cardDensity: normalizeCardDensity(config.cardDensity, DEFAULT_SETTINGS.cardDensity),
    eventsPerPage: normalizeIntInRange(config.eventsPerPage, DEFAULT_SETTINGS.eventsPerPage, 1, 20),
    pageLabelCount: normalizeIntInRange(config.pageLabelCount, DEFAULT_SETTINGS.pageLabelCount, 3, 9),
    titleColor: normalizeColor(config.titleColor, DEFAULT_SETTINGS.titleColor),
    daysColor: normalizeColor(config.daysColor, DEFAULT_SETTINGS.daysColor),
    backgroundColor: normalizeColor(config.backgroundColor, DEFAULT_SETTINGS.backgroundColor),
    leftPanelBgColor: normalizeColor(config.leftPanelBgColor, DEFAULT_SETTINGS.leftPanelBgColor),
    rightPanelBgColor: normalizeColor(config.rightPanelBgColor, DEFAULT_SETTINGS.rightPanelBgColor),
    textColor: normalizeColor(config.textColor, DEFAULT_SETTINGS.textColor),
})

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

const getTodayStampFromNow = (now: Date) => Date.UTC(now.getFullYear(), now.getMonth(), now.getDate())

const getTodayInfo = () => {
    const now = nowFromHost()
    const nextRefresh = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1, 0, 0, 0, 50)

    return {
        iso: toIsoDate(now),
        stamp: getTodayStampFromNow(now),
        delayMs: Math.max(1000, nextRefresh.getTime() - now.getTime()),
    }
}

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


const getAdaptiveCompactTitleFontSize = (
    title: string,
    baseFont: number,
    compactCount: 1 | 2 | 4,
    showDate: boolean
) => {
    const len = String(title ?? "").trim().length
    let size = baseFont

    if (compactCount === 4) {
        if (len >= 14) size -= 3
        else if (len >= 10) size -= 2
        else if (len >= 7) size -= 1
    } else if (compactCount === 2) {
        if (len >= 20) size -= 4
        else if (len >= 15) size -= 3
        else if (len >= 11) size -= 2
        else if (len >= 8) size -= 1
    } else {
        if (len >= 28) size -= 4
        else if (len >= 22) size -= 3
        else if (len >= 16) size -= 2
        else if (len >= 12) size -= 1
    }

    if (showDate && compactCount !== 1) size -= 1
    return Math.max(compactCount === 1 ? 12 : 10, size)
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

const normalizePersistedSettings = (raw: any): PersistedSettings => {
    const source = raw && typeof raw === "object" ? raw : {}

    return {
        eventsFilePath: normalizeEventsFilePath(source.eventsFilePath, DEFAULT_SETTINGS.eventsFilePath),
        compactCount: normalizeCompactCount(source.compactCount, DEFAULT_SETTINGS.compactCount),
        cardDensity: normalizeCardDensity(source.cardDensity, DEFAULT_SETTINGS.cardDensity),
        eventsPerPage: normalizeIntInRange(source.eventsPerPage, DEFAULT_SETTINGS.eventsPerPage, 1, 20),
        pageLabelCount: normalizeIntInRange(source.pageLabelCount, DEFAULT_SETTINGS.pageLabelCount, 3, 9),
        titleColor: normalizeColor(source.titleColor, DEFAULT_SETTINGS.titleColor),
        daysColor: normalizeColor(source.daysColor, DEFAULT_SETTINGS.daysColor),
        backgroundColor: normalizeColor(source.backgroundColor, DEFAULT_SETTINGS.backgroundColor),
        leftPanelBgColor: normalizeColor(source.leftPanelBgColor, DEFAULT_SETTINGS.leftPanelBgColor),
        rightPanelBgColor: normalizeColor(source.rightPanelBgColor, DEFAULT_SETTINGS.rightPanelBgColor),
        textColor: normalizeColor(source.textColor, DEFAULT_SETTINGS.textColor),
    }
}

const normalizeEvents = (rawEvents: unknown): CalendarEvent[] => {
    const source = Array.isArray(rawEvents) ? rawEvents : []

    return sortEvents(
        source
            .map((item, index) => normalizeEvent(item, index))
            .filter((item): item is CalendarEvent => item !== null)
    )
}

const composeRuntimeConfig = (settings: PersistedSettings, events: CalendarEvent[]): CalendarWidgetConfig => ({
    ...settings,
    eventsFilePath: normalizeEventsFilePath(settings.eventsFilePath, DEFAULT_SETTINGS.eventsFilePath),
    events: normalizeEvents(events),
})

const ensureEventsFileInitialized = (eventsFilePath: string) => {
    const normalizedPath = normalizeEventsFilePath(eventsFilePath, DEFAULT_SETTINGS.eventsFilePath)
    if (!chill?.io?.exists?.(normalizedPath)) {
        chill?.io?.writeText?.(normalizedPath, JSON.stringify({ events: [] }, null, 2))
        return
    }

    const text = chill?.io?.readText?.(normalizedPath)
    if (!text) chill?.io?.writeText?.(normalizedPath, JSON.stringify({ events: [] }, null, 2))
}

const loadConfig = (): PersistedSettings => {
    try {
        if (!chill?.io?.exists?.(CONFIG_FILE)) {
            saveConfig(DEFAULT_SETTINGS)
            return normalizePersistedSettings(DEFAULT_SETTINGS)
        }

        const text = chill?.io?.readText?.(CONFIG_FILE)
        if (!text) {
            saveConfig(DEFAULT_SETTINGS)
            return normalizePersistedSettings(DEFAULT_SETTINGS)
        }

        return normalizePersistedSettings(JSON.parse(text))
    } catch (e) {
        console.error("[countdown-days] load config failed", CONFIG_FILE, e)
        saveConfig(DEFAULT_SETTINGS)
        return normalizePersistedSettings(DEFAULT_SETTINGS)
    }
}

const saveConfig = (config: PersistedSettings | CalendarWidgetConfig) => {
    try {
        chill?.io?.writeText?.(CONFIG_FILE, JSON.stringify(extractPersistedSettings(config), null, 2))
    } catch (e) {
        console.error("[countdown-days] save config failed", CONFIG_FILE, e)
    }
}

const loadEventsFromFile = (eventsFilePath: string): CalendarEvent[] | null => {
    try {
        if (!chill?.io?.exists?.(eventsFilePath)) {
            chill?.io?.writeText?.(eventsFilePath, JSON.stringify({ events: [] }, null, 2))
            return []
        }
        const text = chill?.io?.readText?.(eventsFilePath)
        if (!text) {
            chill?.io?.writeText?.(eventsFilePath, JSON.stringify({ events: [] }, null, 2))
            return []
        }
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

const loadRuntimeConfig = (): CalendarWidgetConfig => {
    const settings = loadConfig()
    const eventsFilePath = normalizeEventsFilePath(settings.eventsFilePath, DEFAULT_SETTINGS.eventsFilePath)
    ensureEventsFileInitialized(eventsFilePath)
    const loadedEvents = loadEventsFromFile(eventsFilePath)

    return composeRuntimeConfig(
        { ...settings, eventsFilePath },
        Array.isArray(loadedEvents) ? loadedEvents : []
    )
}

let runtimeConfigSnapshot: CalendarWidgetConfig = DEFAULT_CONFIG
let runtimeConfigLoaded = false
const runtimeConfigListeners = new Set<(config: CalendarWidgetConfig) => void>()

const notifyRuntimeConfigListeners = (nextConfig: CalendarWidgetConfig) => {
    runtimeConfigListeners.forEach((listener) => {
        try {
            listener(nextConfig)
        } catch (error) {
            console.error("[countdown-days] runtime config listener failed", error)
        }
    })
}

const getRuntimeConfigSnapshot = () => {
    if (!runtimeConfigLoaded) {
        runtimeConfigSnapshot = loadRuntimeConfig()
        runtimeConfigLoaded = true
    }
    return runtimeConfigSnapshot
}

const subscribeRuntimeConfig = (listener: (config: CalendarWidgetConfig) => void) => {
    runtimeConfigListeners.add(listener)
    return () => {
        runtimeConfigListeners.delete(listener)
    }
}

const persistRuntimeConfig = (nextConfig: CalendarWidgetConfig) => {
    const normalizedSettings = normalizePersistedSettings(nextConfig)
    const normalizedEvents = normalizeEvents(nextConfig.events)
    const normalizedConfig = composeRuntimeConfig(
        {
            ...normalizedSettings,
            eventsFilePath: normalizeEventsFilePath(normalizedSettings.eventsFilePath, DEFAULT_SETTINGS.eventsFilePath),
        },
        normalizedEvents
    )

    saveConfig(normalizedConfig)
    saveEventsToFile(normalizedConfig.events, normalizedConfig.eventsFilePath)

    runtimeConfigSnapshot = normalizedConfig
    runtimeConfigLoaded = true
    notifyRuntimeConfigListeners(normalizedConfig)
    return normalizedConfig
}

const useRuntimeConfig = () => {
    const [config, setConfig] = useState<CalendarWidgetConfig>(() => getRuntimeConfigSnapshot())

    useEffect(() => {
        setConfig(getRuntimeConfigSnapshot())
        return subscribeRuntimeConfig(setConfig)
    }, [])

    const persist = useCallback((nextConfig: CalendarWidgetConfig) => {
        persistRuntimeConfig(nextConfig)
    }, [])

    return [config, persist] as const
}

const calculateDaysFromTodayStamp = (targetDate: string, todayStamp: number) => {
    const target = parseIsoDate(targetDate)
    if (!target) return 0

    const targetStamp = Date.UTC(target.getFullYear(), target.getMonth(), target.getDate())
    return Math.floor((targetStamp - todayStamp) / 86400000)
}

const formatDaysText = (days: number) => {
    if (days > 0) return `还剩 ${days} 天`
    if (days === 0) return "就在今天"
    return `已经过去 ${Math.abs(days)} 天`
}

const resolveEventType = (targetDate: string, todayStamp: number): EventType => (
    calculateDaysFromTodayStamp(targetDate, todayStamp) < 0 ? "elapsed" : "countdown"
)

const clampConfiguredByLayout = (configured: number, layoutLimit: number, min: number, max: number) => {
    const boundedConfigured = Math.max(min, Math.min(max, Math.round(configured)))
    if (!Number.isFinite(layoutLimit) || layoutLimit <= 0) return boundedConfigured
    return Math.max(min, Math.min(max, Math.min(boundedConfigured, Math.round(layoutLimit))))
}

const getMaxPageLabelsByWidth = (listWidth: number) => {
    if (!Number.isFinite(listWidth) || listWidth <= 0) return 0

    const centerWidth = Math.max(0, listWidth - PAGER_SIDE_GROUP_WIDTH * 2)
    if (centerWidth <= 0) return 1

    return Math.max(1, Math.floor((centerWidth + PAGER_BUTTON_GAP) / (PAGER_SLOT_WIDTH + PAGER_BUTTON_GAP)))
}

const buildPageNumbers = (currentPage: number, totalPages: number, labelCount: number) => {
    if (totalPages <= labelCount) {
        return Array.from({ length: totalPages }, (_, index) => index + 1)
    }

    const halfWindow = Math.floor(labelCount / 2)
    let start = Math.max(1, currentPage - halfWindow)
    let end = start + labelCount - 1

    if (end > totalPages) {
        end = totalPages
        start = end - labelCount + 1
    }

    return Array.from({ length: end - start + 1 }, (_, index) => start + index)
}

const chunkItems = <T,>(items: T[], chunkSize: number): T[][] => {
    if (chunkSize <= 0) return []

    const rows: T[][] = []
    for (let i = 0; i < items.length; i += chunkSize) rows.push(items.slice(i, i + chunkSize))
    return rows
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

const CountdownActionButton = ({
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
            whiteSpace: "NoWrap",
            overflow: "Auto",
            textOverflow: "Ellipsis",
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
    fallbackColor,
}: {
    label: string
    value: string | undefined
    onChange: (nextValue: string) => void
    textColor: string
    inputBg: string
    inputBorder: string
    fallbackColor: string
}) => {
    const draftText = ensureColorDraftText(value, fallbackColor)
    const swatchColor = getDraftColorSwatch(draftText, fallbackColor)

    return (
        <div style={{ marginBottom: 9 }}>
            <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>{label}</div>

            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 5 }}>
                <div
                    style={{
                        width: 20,
                        height: 20,
                        borderRadius: 4,
                        marginRight: 6,
                        backgroundColor: swatchColor,
                        borderWidth: 1,
                        borderColor: hexToRgba(textColor, 0.45),
                    }}
                />
                <textfield
                    value={draftText}
                    multiline={false}
                    onValueChanged={(e: any) => onChange(String(e?.newValue ?? ""))}
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
                            borderWidth: sameColor(draftText, color) ? 2 : 1,
                            borderColor: sameColor(draftText, color) ? textColor : hexToRgba(textColor, 0.4),
                        }}
                    />
                ))}
            </div>
        </div>
    )
}

const useTodayInfo = () => {
    const [todayInfo, setTodayInfo] = useState(() => getTodayInfo())

    useEffect(() => {
        let timer: any = null

        const schedule = () => {
            const nextInfo = getTodayInfo()
            setTodayInfo((prev) => (
                prev.iso === nextInfo.iso && prev.stamp === nextInfo.stamp
                    ? prev
                    : nextInfo
            ))
            timer = setTimeout(schedule, nextInfo.delayMs)
        }

        schedule()
        return () => {
            if (timer) clearTimeout(timer)
        }
    }, [])

    return todayInfo
}

const useWindowLayoutHint = (eventListRef: any, enabled: boolean = true, measureKey: string = "") => {
    const [layoutHint, setLayoutHint] = useState<LayoutHint>(DEFAULT_LAYOUT_HINT)

    useEffect(() => {
        if (!enabled) return

        let settleTimer: any = null
        let rafId: any = null
        let animationLocked = false
        const detachListeners: Array<() => void> = []

        const getMeasuredNode = (node: any) => {
            if (!node) return null
            return node.base ?? node
        }

        const readNodeHeight = (node: any) => {
            const measured = getMeasuredNode(node)
            const veHeight = Number(node?.ve?.layout?.height ?? measured?.ve?.layout?.height ?? 0)
            if (Number.isFinite(veHeight) && veHeight > 0) return Math.round(veHeight)
            const rectHeight = Number(measured?.getBoundingClientRect?.().height ?? measured?.offsetHeight ?? 0)
            return Number.isFinite(rectHeight) && rectHeight > 0 ? Math.round(rectHeight) : 0
        }

        const readNodeWidth = (node: any) => {
            const measured = getMeasuredNode(node)
            const veWidth = Number(node?.ve?.layout?.width ?? measured?.ve?.layout?.width ?? 0)
            if (Number.isFinite(veWidth) && veWidth > 0) return Math.round(veWidth)
            const rectWidth = Number(measured?.getBoundingClientRect?.().width ?? measured?.offsetWidth ?? 0)
            return Number.isFinite(rectWidth) && rectWidth > 0 ? Math.round(rectWidth) : 0
        }

        const applyLayout = () => {
            animationLocked = false
            const nextListWidth = Math.max(0, readNodeWidth(eventListRef.current))
            const nextListHeight = Math.max(0, readNodeHeight(eventListRef.current))
            const nextWindowWidth = Math.max(0, Math.round(Number((globalThis as any)?.innerWidth ?? 0)))
            const nextWindowHeight = Math.max(0, Math.round(Number((globalThis as any)?.innerHeight ?? 0)))

            setLayoutHint((prev) => {
                const same =
                    Math.abs(prev.listWidth - nextListWidth) <= LAYOUT_HINT_JITTER_PX &&
                    Math.abs(prev.listHeight - nextListHeight) <= LAYOUT_HINT_JITTER_PX &&
                    prev.windowWidth === nextWindowWidth &&
                    prev.windowHeight === nextWindowHeight

                if (same) return prev

                return {
                    windowWidth: nextWindowWidth,
                    windowHeight: nextWindowHeight,
                    listWidth: nextListWidth,
                    listHeight: nextListHeight,
                }
            })
        }

        const scheduleRead = () => {
            if (settleTimer) clearTimeout(settleTimer)
            if (animationLocked) {
                settleTimer = setTimeout(applyLayout, LAYOUT_HINT_SETTLE_DELAY_MS)
                return
            }
            animationLocked = true
            if (rafId) cancelAnimationFrame(rafId)
            rafId = requestAnimationFrame(() => {
                applyLayout()
                settleTimer = setTimeout(applyLayout, LAYOUT_HINT_SETTLE_DELAY_MS)
            })
        }

        const GEOMETRY_CHANGED_EVENT = "geometrychanged"

        const bindGeometryChanged = (node: any) => {
            const measured = getMeasuredNode(node)
            if (!measured?.addEventListener) return false
            const handler = () => scheduleRead()

            try {
                measured.addEventListener(GEOMETRY_CHANGED_EVENT, handler)
                detachListeners.push(() => measured.removeEventListener?.(GEOMETRY_CHANGED_EVENT, handler))
                return true
            } catch (_) {
                return false
            }
        }

        scheduleRead()

        const eventListNode = eventListRef.current
        const hasGeometryListener = bindGeometryChanged(eventListNode)

        if (!hasGeometryListener) {
            // No element-level geometry event is available in this host. In that case,
            // layout will still be measured on mount and whenever measureKey changes,
            // but we intentionally do not fall back to ResizeObserver here.
        }

        return () => {
            if (settleTimer) clearTimeout(settleTimer)
            if (rafId) cancelAnimationFrame(rafId)
            animationLocked = false
            detachListeners.forEach((detach) => {
                try { detach() } catch (_) {}
            })
        }
    }, [enabled, measureKey])

    return layoutHint
}

type EventCardProps = {
    item: CalendarEvent
    todayStamp: number
    isSimpleCard: boolean
    isEditing: boolean
    linkedToSelected: boolean
    isDeletePending: boolean
    subtleText: string
    textColor: string
    titleColor: string
    daysColor: string
    panelInnerBg: string
    rightPanelBgColor: string
    selectedEventBorder: string
    defaultEventBorder: string
    focusDate: (iso: string) => void
    togglePinned: (id: string) => void
    startEdit: (item: CalendarEvent) => void
    setDeleteConfirmId: (id: string | null) => void
    removeEvent: (id: string) => void
}

const EventCard = ({
    item,
    todayStamp,
    isSimpleCard,
    isEditing,
    linkedToSelected,
    isDeletePending,
    subtleText,
    textColor,
    titleColor,
    daysColor,
    panelInnerBg,
    rightPanelBgColor,
    selectedEventBorder,
    defaultEventBorder,
    focusDate,
    togglePinned,
    startEdit,
    setDeleteConfirmId,
    removeEvent,
}: EventCardProps) => {
    const dayDiff = calculateDaysFromTodayStamp(item.targetDate, todayStamp)
    const dayText = formatDaysText(dayDiff)
    const cardBg = isEditing ? mixHex(daysColor, rightPanelBgColor, 0.82) : panelInnerBg
    const cardBorder = linkedToSelected ? selectedEventBorder : defaultEventBorder
    const actionBaseBg = mixHex(rightPanelBgColor, "#000000", 0.18)
    const actionPinBg = item.pinned ? mixHex(daysColor, rightPanelBgColor, 0.22) : actionBaseBg
    const actionDeleteBg = mixHex("#7f1d1d", rightPanelBgColor, 0.45)

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
                        <div style={{ width: 40 }}>
                            <CountdownActionButton
                                text={item.pinned ? "UNP" : "PIN"}
                                onClick={() => togglePinned(item.id)}
                                color={item.pinned ? mixHex("#0b1120", textColor, 0.2) : textColor}
                                bg={actionPinBg}
                                compact
                            />
                        </div>
                        <div style={{ width: 4 }} />
                        <div style={{ width: 40 }}>
                            <CountdownActionButton text="EDT" onClick={() => startEdit(item)} color={textColor} bg={actionBaseBg} compact />
                        </div>
                        <div style={{ width: 4 }} />
                        <div style={{ width: 40 }}>
                            <CountdownActionButton text="DEL" onClick={() => setDeleteConfirmId(item.id)} color="#fecaca" bg={actionDeleteBg} compact />
                        </div>
                    </div>
                </div>
            ) : null}

            <div style={{ fontSize: 12, color: titleColor, unityFontStyleAndWeight: "Bold", whiteSpace: "NoWrap", overflow: "Hidden", textOverflow: "Ellipsis", marginBottom: isSimpleCard ? 4 : 6 }}>
                {item.title}
            </div>

            <div style={{ fontSize: 15, color: daysColor, unityFontStyleAndWeight: "Bold" }}>{dayText}</div>

            {isDeletePending ? (
                <div
                    onPointerDown={(e: any) => { e?.stopPropagation?.() }}
                    style={{ position: "Absolute", left: 0, right: 0, top: 0, bottom: 0, backgroundColor: mixHex(rightPanelBgColor, "#000000", 0.28), borderRadius: 10, display: "Flex", flexDirection: "Column", justifyContent: "Center", alignItems: "Center" }}
                >
                    <div style={{ fontSize: 10, color: "#fca5a5", unityFontStyleAndWeight: "Bold", marginBottom: 6 }}>CONFIRM DELETE?</div>
                    <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                        <div style={{ width: 36 }}>
                            <CountdownActionButton text="YES" onClick={() => removeEvent(item.id)} color="#dcfce7" bg="#14532d" compact />
                        </div>
                        <div style={{ width: 4 }} />
                        <div style={{ width: 36 }}>
                            <CountdownActionButton text="NO" onClick={() => setDeleteConfirmId(null)} color={textColor} bg={actionBaseBg} compact />
                        </div>
                    </div>
                </div>
            ) : null}
        </div>
    )
}

type EventListSectionProps = {
    showCountLabel: boolean
    allEventsLength: number
    mutedText: string
    panelInnerBg: string
    panelBorder: string
    pagedEvents: CalendarEvent[]
    eventPage: number
    totalEventPages: number
    visiblePageNumbers: number[]
    textColor: string
    softActionBg: string
    accentButtonBg: string
    eventListRef: any
    setEventPage: any
    todayStamp: number
    isSimpleCard: boolean
    selectedDate: string
    editDialogId: string | null
    deleteConfirmId: string | null
    subtleText: string
    titleColor: string
    daysColor: string
    rightPanelBgColor: string
    selectedEventBorder: string
    defaultEventBorder: string
    focusDate: (iso: string) => void
    togglePinned: (id: string) => void
    startEdit: (item: CalendarEvent) => void
    setDeleteConfirmId: (id: string | null) => void
    removeEvent: (id: string) => void
}

const EventListSection = ({ showCountLabel, allEventsLength, mutedText, panelInnerBg, panelBorder, pagedEvents, eventPage, totalEventPages, visiblePageNumbers, textColor, softActionBg, accentButtonBg, eventListRef, setEventPage, todayStamp, isSimpleCard, selectedDate, editDialogId, deleteConfirmId, subtleText, titleColor, daysColor, rightPanelBgColor, selectedEventBorder, defaultEventBorder, focusDate, togglePinned, startEdit, setDeleteConfirmId, removeEvent }: EventListSectionProps) => (
    <>
        {showCountLabel ? (
            <div style={{ fontSize: 10, color: textColor, marginBottom: 4, whiteSpace: "NoWrap", display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                {`全部事件（置顶优先）: ${allEventsLength}`}
            </div>
        ) : null}
        <div ref={eventListRef} style={{ flexGrow: 1, flexShrink: 1, minHeight: 0, backgroundColor: panelInnerBg, borderWidth: 1, borderColor: panelBorder, borderRadius: 8, padding: 6, overflow: "Hidden" }}>
            {allEventsLength === 0 ? (
                <div style={{ fontSize: 10, color: mutedText }}>暂无事件，先在左侧选日期后添加一个。</div>
            ) : (
                <div style={{ display: "Flex", flexDirection: "Column", alignItems: "Stretch", minWidth: 0 }}>
                    {pagedEvents.map((item) => (
                        <EventCard
                            key={item.id}
                            item={item}
                            todayStamp={todayStamp}
                            isSimpleCard={isSimpleCard}
                            isEditing={editDialogId === item.id}
                            linkedToSelected={item.targetDate === selectedDate}
                            isDeletePending={deleteConfirmId === item.id}
                            subtleText={subtleText}
                            textColor={textColor}
                            titleColor={titleColor}
                            daysColor={daysColor}
                            panelInnerBg={panelInnerBg}
                            rightPanelBgColor={rightPanelBgColor}
                            selectedEventBorder={selectedEventBorder}
                            defaultEventBorder={defaultEventBorder}
                            focusDate={focusDate}
                            togglePinned={togglePinned}
                            startEdit={startEdit}
                            setDeleteConfirmId={setDeleteConfirmId}
                            removeEvent={removeEvent}
                        />
                    ))}
                </div>
            )}
        </div>
        <div style={{ display: "Flex", justifyContent: "Center", alignItems: "Center", marginTop: 4, width: "100%" }}>
            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "Center", width: "100%", minWidth: 0 }}>
                <div style={{ width: PAGER_SIDE_GROUP_WIDTH, minWidth: PAGER_SIDE_GROUP_WIDTH, flexShrink: 0, display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "FlexStart", gap: PAGER_BUTTON_GAP }}>
                    <div style={{ width: PAGER_SLOT_WIDTH, flexShrink: 0 }}>
                        <CountdownActionButton text="<<" onClick={() => setEventPage(1)} disabled={eventPage <= 1} color={textColor} bg={softActionBg} compact fullWidth />
                    </div>
                    <div style={{ width: PAGER_SLOT_WIDTH, flexShrink: 0 }}>
                        <CountdownActionButton text="<" onClick={() => setEventPage((page: number) => Math.max(1, page - 1))} disabled={eventPage <= 1} color={textColor} bg={softActionBg} compact fullWidth />
                    </div>
                </div>
                <div style={{ flexGrow: 1, minWidth: 0, display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "Center", gap: PAGER_BUTTON_GAP }}>
                    {visiblePageNumbers.map((pageNumber) => (
                        <div key={`page-${pageNumber}`} style={{ width: PAGER_SLOT_WIDTH, flexShrink: 0 }}>
                            <CountdownActionButton text={`${pageNumber}`} onClick={() => setEventPage(pageNumber)} color={textColor} bg={eventPage === pageNumber ? accentButtonBg : softActionBg} compact fullWidth />
                        </div>
                    ))}
                </div>
                <div style={{ width: PAGER_SIDE_GROUP_WIDTH, minWidth: PAGER_SIDE_GROUP_WIDTH, flexShrink: 0, display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "FlexEnd", gap: PAGER_BUTTON_GAP }}>
                    <div style={{ width: PAGER_SLOT_WIDTH, flexShrink: 0 }}>
                        <CountdownActionButton text=">" onClick={() => setEventPage((page: number) => Math.min(totalEventPages, page + 1))} disabled={eventPage >= totalEventPages} color={textColor} bg={softActionBg} compact fullWidth />
                    </div>
                    <div style={{ width: PAGER_SLOT_WIDTH, flexShrink: 0 }}>
                        <CountdownActionButton text=">>" onClick={() => setEventPage(totalEventPages)} disabled={eventPage >= totalEventPages} color={textColor} bg={softActionBg} compact fullWidth />
                    </div>
                </div>
            </div>
        </div>
    </>
)

const WeekLabelsRow = ({ subtleText }: { subtleText: string }) => (
    <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 3, width: "100%" }}>
        {WEEK_LABELS.map((label) => (
            <div key={`week-${label}`} style={{ width: "14.2857%", flexGrow: 0, flexShrink: 0, fontSize: 9, color: subtleText, unityTextAlign: "MiddleCenter" }}>
                {label}
            </div>
        ))}
    </div>
)

type CalendarGridRowsProps = {
    calendarRows: ReturnType<typeof chunkItems<ReturnType<typeof buildCalendarCells>[number]>>
    selectedDate: string
    todayIsoValue: string
    eventCountByDate: Map<string, number>
    focusDate: (iso: string) => void
    selectedCellBg: string
    calendarCellBg: string
    calendarCellMutedBg: string
    daysColor: string
    textColor: string
    subtleText: string
}

const CalendarGridRows = ({ calendarRows, selectedDate, todayIsoValue, eventCountByDate, focusDate, selectedCellBg, calendarCellBg, calendarCellMutedBg, daysColor, textColor, subtleText }: CalendarGridRowsProps) => (
    <>
        {calendarRows.map((row, rowIndex) => (
            <div key={`row-${rowIndex}`} style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 3, width: "100%" }}>
                {row.map((cell) => {
                    const isSelected = cell.iso === selectedDate
                    const isToday = cell.iso === todayIsoValue
                    const count = eventCountByDate.get(cell.iso) || 0
                    return (
                        <div key={cell.iso} onPointerDown={() => focusDate(cell.iso)} style={{ width: "14.2857%", flexGrow: 0, flexShrink: 0, height: 33, borderRadius: 6, backgroundColor: isSelected ? selectedCellBg : (cell.inCurrentMonth ? calendarCellBg : calendarCellMutedBg), borderWidth: isToday ? 1 : 0, borderColor: isToday ? daysColor : "transparent", display: "Flex", flexDirection: "Column", justifyContent: "Center", alignItems: "Center" }}>
                            <div style={{ fontSize: 10, color: isSelected ? "#0b1120" : (cell.inCurrentMonth ? textColor : subtleText), unityFontStyleAndWeight: isSelected ? "Bold" : "Normal" }}>{cell.day}</div>
                            <div style={{ fontSize: 8, color: isSelected ? "#0b1120" : daysColor }}>{count > 0 ? `•${count}` : ""}</div>
                        </div>
                    )
                })}
            </div>
        ))}
    </>
)



type EventSectionModel = {
    eventCountByDate: Map<string, number>
    allEvents: CalendarEvent[]
    totalEventPages: number
    pagedEvents: CalendarEvent[]
    visiblePageNumbers: number[]
    isSimpleCard: boolean
    selectedEventBorder: string
    defaultEventBorder: string
}

const useEventSectionModel = ({
    events,
    cardDensity,
    rightPanelBgColor,
    textColor,
    daysColor,
    configuredEventsPerPage,
    configuredPageLabelCount,
    listWidth,
    listHeight,
    eventPage,
}: {
    events: CalendarEvent[]
    cardDensity: CardDensity
    rightPanelBgColor: string
    textColor: string
    daysColor: string
    configuredEventsPerPage: number
    configuredPageLabelCount: number
    listWidth: number
    listHeight: number
    eventPage: number
}): EventSectionModel => {
    const sortedEvents = useMemo(() => sortEvents(events), [events])

    const eventCountByDate = useMemo(() => {
        const map = new Map<string, number>()
        for (const eventItem of sortedEvents) {
            map.set(eventItem.targetDate, (map.get(eventItem.targetDate) || 0) + 1)
        }
        return map
    }, [sortedEvents])

    const allEvents = useMemo(() => sortEventsPinnedFirst(sortedEvents), [sortedEvents])
    const isSimpleCard = cardDensity === "simple"
    const estimatedCardRowHeight = isSimpleCard ? EVENT_CARD_ROW_HEIGHT_SIMPLE : EVENT_CARD_ROW_HEIGHT_STANDARD
    const measuredRowsPerPage = listHeight > 0
        ? Math.max(1, Math.floor((listHeight + 6) / estimatedCardRowHeight))
        : configuredEventsPerPage
    const eventsPerPage = clampConfiguredByLayout(configuredEventsPerPage, measuredRowsPerPage, 1, 20)

    const measuredPageLabelCount = listWidth > 0
        ? getMaxPageLabelsByWidth(listWidth)
        : configuredPageLabelCount
    const pageLabelCount = clampConfiguredByLayout(configuredPageLabelCount, measuredPageLabelCount, 1, 9)

    const totalEventPages = useMemo(
        () => Math.max(1, Math.ceil(allEvents.length / eventsPerPage)),
        [allEvents.length, eventsPerPage]
    )

    const pagedEvents = useMemo(() => {
        const start = (eventPage - 1) * eventsPerPage
        return allEvents.slice(start, start + eventsPerPage)
    }, [allEvents, eventPage, eventsPerPage])

    const visiblePageNumbers = useMemo(
        () => buildPageNumbers(eventPage, totalEventPages, pageLabelCount),
        [eventPage, totalEventPages, pageLabelCount]
    )

    const selectedEventBorder = useMemo(
        () => mixHex(daysColor, rightPanelBgColor, 0.24),
        [daysColor, rightPanelBgColor]
    )
    const defaultEventBorder = useMemo(
        () => mixHex(textColor, rightPanelBgColor, 0.78),
        [textColor, rightPanelBgColor]
    )

    return {
        eventCountByDate,
        allEvents,
        totalEventPages,
        pagedEvents,
        visiblePageNumbers,
        isSimpleCard,
        selectedEventBorder,
        defaultEventBorder,
    }
}

const CountdownPanel = () => {
    const [config, persist] = useRuntimeConfig()
    const todayInfo = useTodayInfo()

    const [selectedDate, setSelectedDate] = useState(todayInfo.iso)
    const [viewYear, setViewYear] = useState(() => {
        const parsed = parseIsoDate(todayInfo.iso)
        return parsed ? parsed.getFullYear() : nowFromHost().getFullYear()
    })
    const [viewMonth, setViewMonth] = useState(() => {
        const parsed = parseIsoDate(todayInfo.iso)
        return parsed ? parsed.getMonth() : nowFromHost().getMonth()
    })

    const [draftTitle, setDraftTitle] = useState("")
    const [editDialogOpen, setEditDialogOpen] = useState(false)
    const [editDialogId, setEditDialogId] = useState<string | null>(null)
    const [editDialogTitle, setEditDialogTitle] = useState("")
    const [editDialogDate, setEditDialogDate] = useState(todayInfo.iso)

    const [error, setError] = useState("")

    const [monthPickerOpen, setMonthPickerOpen] = useState(false)
    const [yearCursor, setYearCursor] = useState(viewYear)

    const [settingsOpen, setSettingsOpen] = useState(false)
    const [settingsMenu, setSettingsMenu] = useState<SettingsMenu>("main")
    const [settingsDraft, setSettingsDraft] = useState<SettingsDraft>(() => createSettingsDraft(DEFAULT_CONFIG))
    const [eventPage, setEventPage] = useState(1)
    const [middleMode, setMiddleMode] = useState(false)
    const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)
    const [middleModeTransitioning, setMiddleModeTransitioning] = useState(false)
    const middleModeToggleLockRef = useRef(false)
    const middleModeToggleTimerRef = useRef<any>(null)
    const eventListRef = useRef<any>(null)
    const layoutHintEnabled = !settingsOpen && !monthPickerOpen && !editDialogOpen && !middleModeTransitioning
    const layoutMeasureKey = `${middleMode ? "middle" : "full"}|${config.cardDensity}|${settingsOpen ? 1 : 0}|${monthPickerOpen ? 1 : 0}|${editDialogOpen ? 1 : 0}|${config.events.length}`
    const layoutHint = useWindowLayoutHint(eventListRef, layoutHintEnabled, layoutMeasureKey)

    const configuredEventsPerPage = Math.max(1, config.eventsPerPage)
    const configuredPageLabelCount = Math.max(3, ensureSettingsNumber(config.pageLabelCount, DEFAULT_CONFIG.pageLabelCount))
    const eventSection = useEventSectionModel({
        events: config.events,
        cardDensity: config.cardDensity,
            rightPanelBgColor: config.rightPanelBgColor,
        textColor: config.textColor,
        daysColor: config.daysColor,
        configuredEventsPerPage,
        configuredPageLabelCount,
        listWidth: layoutHint.listWidth,
        listHeight: layoutHint.listHeight,
        eventPage,
    })
    const {
        eventCountByDate,
        allEvents,
        totalEventPages,
        pagedEvents,
        visiblePageNumbers,
        isSimpleCard,
        selectedEventBorder,
        defaultEventBorder,
    } = eventSection
    const todayIsoValue = todayInfo.iso
    const todayStamp = todayInfo.stamp

    const calendarCells = useMemo(() => buildCalendarCells(viewYear, viewMonth), [viewYear, viewMonth])
    const calendarRows = useMemo(() => chunkItems(calendarCells, 7), [calendarCells])

    const textColor = config.textColor
    const mutedText = hexToRgba(textColor, 0.72)
    const subtleText = mixHex(textColor, config.rightPanelBgColor, 0.48)
    const panelBorder = mixHex(textColor, config.rightPanelBgColor, 0.74)
    const panelInnerBg = mixHex(config.rightPanelBgColor, "#000000", 0.22)
    const softActionBg = mixHex(config.rightPanelBgColor, "#000000", 0.15)
    const inputBg = mixHex(config.rightPanelBgColor, "#000000", 0.36)
    const inputBorder = mixHex(textColor, config.rightPanelBgColor, 0.65)
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

        const focusDate = useCallback((iso: string) => {
        const parsed = parseIsoDate(iso)
        if (!parsed) return
        setSelectedDate(iso)
        setViewYear(parsed.getFullYear())
        setViewMonth(parsed.getMonth())
    }, [])

    const clearDraft = useCallback(() => {
        setDraftTitle("")
        setDeleteConfirmId(null)
    }, [])

    const saveEventForSelectedDate = useCallback(() => {
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
                type: calculateDaysFromTodayStamp(selectedDate, todayStamp) < 0 ? "elapsed" : "countdown",
                pinned: false,
                createdAt: new Date().toISOString(),
            },
        ]

        persist({ ...config, events: sortEvents(nextEvents) })
        setDeleteConfirmId(null)
        clearDraft()
        setError("")
    }, [config, draftTitle, selectedDate, persist, clearDraft])

    const startEdit = useCallback((item: CalendarEvent) => {
        setDeleteConfirmId(null)
        setEditDialogId(item.id)
        setEditDialogTitle(item.title)
        setEditDialogDate(item.targetDate)
        setEditDialogOpen(true)
        setError("")
    }, [])

    const closeEditDialog = useCallback(() => {
        setEditDialogOpen(false)
        setEditDialogId(null)
        setEditDialogTitle("")
        setEditDialogDate(selectedDate)
    }, [selectedDate])

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
                ? { ...item, title, targetDate, type: resolveEventType(targetDate, getTodayInfo().stamp) }
                : item
        )

        persist({ ...config, events: sortEvents(nextEvents) })
        focusDate(targetDate)
        setDeleteConfirmId(null)
        closeEditDialog()
        setError("")
    }

    const removeEvent = useCallback((id: string) => {
        const nextEvents = config.events.filter((item) => item.id !== id)
        persist({ ...config, events: nextEvents })
        setDeleteConfirmId(null)
        if (editDialogId === id) closeEditDialog()
    }, [config, editDialogId, closeEditDialog, persist])

    const togglePinned = useCallback((id: string) => {
        setDeleteConfirmId(null)
        const nextEvents = config.events.map((item) =>
            item.id === id ? { ...item, pinned: !item.pinned } : item
        )
        persist({ ...config, events: nextEvents })
    }, [config, persist])

    const goToToday = useCallback(() => {
        focusDate(todayInfo.iso)
    }, [focusDate, todayInfo.iso])

    const shiftView = useCallback((delta: number) => {
        const shifted = shiftMonth(viewYear, viewMonth, delta)
        setViewYear(shifted.year)
        setViewMonth(shifted.month)
    }, [viewYear, viewMonth])

    const openMonthPicker = useCallback(() => {
        setEditDialogOpen(false)
        setYearCursor(viewYear)
        setMonthPickerOpen(true)
    }, [viewYear])

    const openSettings = () => {
        setEditDialogOpen(false)
        setSettingsMenu("main")
        setSettingsDraft(createSettingsDraft(config))
        setSettingsOpen(true)
    }

    const scheduleMiddleModeUnlock = useCallback(() => {
        if (middleModeToggleTimerRef.current) clearTimeout(middleModeToggleTimerRef.current)
        middleModeToggleTimerRef.current = setTimeout(() => {
            middleModeToggleLockRef.current = false
            setMiddleModeTransitioning(false)
            middleModeToggleTimerRef.current = null
        }, 180)
    }, [])

    const enterMiddleMode = useCallback(() => {
        if (middleModeToggleLockRef.current || middleMode) return
        middleModeToggleLockRef.current = true
        setMiddleModeTransitioning(true)
        setMonthPickerOpen(false)
        setSettingsOpen(false)
        setEditDialogOpen(false)
        setMiddleMode(true)
        scheduleMiddleModeUnlock()
    }, [middleMode, scheduleMiddleModeUnlock])

    const exitMiddleMode = useCallback(() => {
        if (middleModeToggleLockRef.current || !middleMode) return
        middleModeToggleLockRef.current = true
        setMiddleModeTransitioning(true)
        setMiddleMode(false)
        scheduleMiddleModeUnlock()
    }, [middleMode, scheduleMiddleModeUnlock])

    const applyThemePreset = (presetId: string) => {
        const preset = THEME_PRESETS.find((item) => item.id === presetId)
        if (!preset) return

        setSettingsDraft((prev) => ({
            ...prev,
            backgroundColor: preset.config.backgroundColor,
            leftPanelBgColor: preset.config.leftPanelBgColor,
            rightPanelBgColor: preset.config.rightPanelBgColor,
            textColor: preset.config.textColor,
            titleColor: preset.config.titleColor,
            daysColor: preset.config.daysColor,
        }))
    }

    const colorDraftRows: Array<{ label: string; key: SettingsColorKey }> = [
        { label: "整体背景", key: "backgroundColor" },
        { label: "左侧面板背景", key: "leftPanelBgColor" },
        { label: "右侧面板背景", key: "rightPanelBgColor" },
        { label: "主文字颜色", key: "textColor" },
        { label: "标题文字颜色", key: "titleColor" },
        { label: "天数高亮色", key: "daysColor" },
    ]

    const paginationNumberRows: Array<{ label: string; key: SettingsNumberKey }> = [
        { label: "每页事件数", key: "eventsPerPage" },
        { label: "一页标签数（页码按钮）", key: "pageLabelCount" },
    ]

    const updateSettingsDraftNumber = (key: SettingsNumberKey, rawValue: unknown) => {
        const nextText = sanitizeSettingsNumberText(rawValue)
        setSettingsDraft((prev) => ({ ...prev, [key]: nextText }))
    }

    const renderSettingsNumberRow = (label: string, key: SettingsNumberKey) => (
        <div key={`settings-number-${key}`} style={{ marginBottom: 8 }}>
            <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>{label}</div>
            <textfield
                value={ensureSettingsNumberText(settingsDraft[key], key === "eventsPerPage" ? DEFAULT_CONFIG.eventsPerPage : DEFAULT_CONFIG.pageLabelCount)}
                multiline={false}
                onValueChanged={(e: any) => updateSettingsDraftNumber(key, e?.newValue)}
                style={settingsNumberInputStyle}
            />
        </div>
    )

    const applySettings = () => {
        const nextEventsFilePath = normalizeEventsFilePath(settingsDraft.eventsFilePath, config.eventsFilePath)
        const backgroundColor = normalizeColor(settingsDraft.backgroundColor, config.backgroundColor)
        const leftPanelBgColor = normalizeColor(settingsDraft.leftPanelBgColor, config.leftPanelBgColor)
        const rightPanelBgColor = normalizeColor(settingsDraft.rightPanelBgColor, config.rightPanelBgColor)
        const textColor = normalizeColor(settingsDraft.textColor, config.textColor)
        const titleColor = normalizeColor(settingsDraft.titleColor, config.titleColor)
        const daysColor = normalizeColor(settingsDraft.daysColor, config.daysColor)
        const cardDensity = normalizeCardDensity(settingsDraft.cardDensity, config.cardDensity)

        const nextConfig: CalendarWidgetConfig = {
            ...config,
            titleColor,
            daysColor,
            cardDensity,
            backgroundColor,
            leftPanelBgColor,
            rightPanelBgColor,
            textColor,
            eventsPerPage: parseSettingsNumberDraft(settingsDraft.eventsPerPage, config.eventsPerPage, 1, 20),
            pageLabelCount: parseSettingsNumberDraft(settingsDraft.pageLabelCount, config.pageLabelCount, 3, 9),
            eventsFilePath: nextEventsFilePath,
        }

        persist(nextConfig)
        setSettingsOpen(false)
    }

    useEffect(() => {
        setEventPage((page) => Math.min(page, totalEventPages))
    }, [totalEventPages])

    useEffect(() => {
        return () => {
            if (middleModeToggleTimerRef.current) {
                clearTimeout(middleModeToggleTimerRef.current)
                middleModeToggleTimerRef.current = null
            }
            middleModeToggleLockRef.current = false
        }
    }, [])

    useEffect(() => {
        setDeleteConfirmId(null)
    }, [eventPage])


    const overlayOpen = monthPickerOpen || editDialogOpen || settingsOpen

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
                        <CountdownActionButton text="展开" onClick={exitMiddleMode} disabled={middleModeTransitioning} color={textColor} bg={accentButtonBg} compact />
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
                        {<EventListSection showCountLabel={false} allEventsLength={allEvents.length} mutedText={mutedText} panelInnerBg={panelInnerBg} panelBorder={panelBorder} pagedEvents={pagedEvents} eventPage={eventPage} totalEventPages={totalEventPages} visiblePageNumbers={visiblePageNumbers} textColor={textColor} softActionBg={softActionBg} accentButtonBg={accentButtonBg} eventListRef={eventListRef} setEventPage={setEventPage} todayStamp={todayStamp} isSimpleCard={isSimpleCard} selectedDate={selectedDate} editDialogId={editDialogId} deleteConfirmId={deleteConfirmId} subtleText={subtleText} titleColor={config.titleColor} daysColor={config.daysColor} rightPanelBgColor={config.rightPanelBgColor} selectedEventBorder={selectedEventBorder} defaultEventBorder={defaultEventBorder} focusDate={focusDate} togglePinned={togglePinned} startEdit={startEdit} setDeleteConfirmId={setDeleteConfirmId} removeEvent={removeEvent} />}
                    </div>
                </div>
            ) : (
                <>
                    <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 6 }}>
                        <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                            <CountdownActionButton text="收起" onClick={enterMiddleMode} disabled={middleModeTransitioning} color={textColor} bg={softActionBg} compact />
                            <div style={{ width: 6 }} />
                            <div style={{ fontSize: 12, color: textColor, unityFontStyleAndWeight: "Bold" }}>日历计时</div>
                        </div>
                        <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                            <CountdownActionButton text="设置" onClick={openSettings} bg={accentButtonBg} color={textColor} compact />
                        </div>
                    </div>

                    {error ? <div style={{ fontSize: 10, color: "#fca5a5", marginBottom: 6 }}>{error}</div> : null}

                    {!overlayOpen ? (
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
                                        <CountdownActionButton text="今天" onClick={goToToday} color={textColor} bg={softActionBg} compact />
                                        <div style={{ width: 3 }} />
                                        <CountdownActionButton text="<" onClick={() => shiftView(-1)} color={textColor} bg={softActionBg} compact />
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
                                    <CountdownActionButton text=">" onClick={() => shiftView(1)} color={textColor} bg={softActionBg} compact />
                                </div>

                                <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 3, width: "100%" }}>
                                    {<WeekLabelsRow subtleText={subtleText} />}
                                </div>

                                {<CalendarGridRows calendarRows={calendarRows} selectedDate={selectedDate} todayIsoValue={todayIsoValue} eventCountByDate={eventCountByDate} focusDate={focusDate} selectedCellBg={selectedCellBg} calendarCellBg={calendarCellBg} calendarCellMutedBg={calendarCellMutedBg} daysColor={config.daysColor} textColor={textColor} subtleText={subtleText} />}
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
                                    <CountdownActionButton text="添加到选中日期" onClick={saveEventForSelectedDate} color={textColor} bg={accentButtonBg} compact />
                                </div>
                            </div>

                            {<EventListSection showCountLabel={true} allEventsLength={allEvents.length} mutedText={mutedText} panelInnerBg={panelInnerBg} panelBorder={panelBorder} pagedEvents={pagedEvents} eventPage={eventPage} totalEventPages={totalEventPages} visiblePageNumbers={visiblePageNumbers} textColor={textColor} softActionBg={softActionBg} accentButtonBg={accentButtonBg} eventListRef={eventListRef} setEventPage={setEventPage} todayStamp={todayStamp} isSimpleCard={isSimpleCard} selectedDate={selectedDate} editDialogId={editDialogId} deleteConfirmId={deleteConfirmId} subtleText={subtleText} titleColor={config.titleColor} daysColor={config.daysColor} rightPanelBgColor={config.rightPanelBgColor} selectedEventBorder={selectedEventBorder} defaultEventBorder={defaultEventBorder} focusDate={focusDate} togglePinned={togglePinned} startEdit={startEdit} setDeleteConfirmId={setDeleteConfirmId} removeEvent={removeEvent} />}
                        </div>
                    </div>
                    ) : null}
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
                            <CountdownActionButton text="关闭" onClick={() => setMonthPickerOpen(false)} color={textColor} bg={softActionBg} compact />
                        </div>

                        <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "Center", alignItems: "Center", marginBottom: 8 }}>
                            <CountdownActionButton text="-10" onClick={() => setYearCursor((y) => y - 10)} color={textColor} bg={softActionBg} compact />
                            <div style={{ width: 3 }} />
                            <CountdownActionButton text="-1" onClick={() => setYearCursor((y) => y - 1)} color={textColor} bg={softActionBg} compact />
                            <div style={{ width: 8 }} />
                            <div style={{ fontSize: 12, color: textColor, width: 70, unityTextAlign: "MiddleCenter" }}>{yearCursor}年</div>
                            <div style={{ width: 8 }} />
                            <CountdownActionButton text="+1" onClick={() => setYearCursor((y) => y + 1)} color={textColor} bg={softActionBg} compact />
                            <div style={{ width: 3 }} />
                            <CountdownActionButton text="+10" onClick={() => setYearCursor((y) => y + 10)} color={textColor} bg={softActionBg} compact />
                        </div>

                        <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "Center", marginBottom: 8 }}>
                            {Array.from({ length: 5 }, (_, i) => yearCursor - 2 + i).map((year, i) => (
                                <div key={`year-select-${year}`} style={{ marginRight: i === 4 ? 0 : 4 }}>
                                    <CountdownActionButton
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
                                    <CountdownActionButton
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
                            <CountdownActionButton text="关闭" onClick={closeEditDialog} color={textColor} bg={softActionBg} compact />
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
                                <CountdownActionButton text="保存" onClick={saveEditedEvent} color={textColor} bg={accentButtonBg} compact />
                                <div style={{ width: 4 }} />
                                <CountdownActionButton text="取消" onClick={closeEditDialog} color={textColor} bg={softActionBg} compact />
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
                                        <CountdownActionButton text="返回" onClick={() => setSettingsMenu("main")} color={textColor} bg={softActionBg} compact />
                                        <div style={{ width: 4 }} />
                                    </>
                                ) : null}
                                <CountdownActionButton text="关闭" onClick={() => setSettingsOpen(false)} color={textColor} bg={softActionBg} compact />
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
                                        <CountdownActionButton text="自定义颜色" onClick={() => setSettingsMenu("colors")} color={textColor} bg={accentButtonBg} compact fullWidth />
                                    </div>
                                </div>

                                <div style={{ marginBottom: 10 }}>
                                    <div style={{ fontSize: 10, color: textColor, marginBottom: 4 }}>卡片信息密度</div>
                                    <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                                        <CountdownActionButton
                                            text="简洁"
                                            onClick={() => setSettingsDraft((prev) => ({ ...prev, cardDensity: "simple" }))}
                                            color={textColor}
                                            bg={settingsDraft.cardDensity === "simple" ? accentButtonBg : softActionBg}
                                            compact
                                        />
                                        <div style={{ width: 6 }} />
                                        <CountdownActionButton
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
                                            <CountdownActionButton
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
                                        onChange={(value) => setSettingsDraft((prev) => ({ ...prev, [row.key]: String(value ?? "") }))}
                                        textColor={textColor}
                                        inputBg={inputBg}
                                        inputBorder={inputBorder}
                                        fallbackColor={DEFAULT_CONFIG[row.key]}
                                    />
                                ))}
                            </div>
                        )}

                        {settingsMenu === "main" ? (
                            <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "FlexEnd" }}>
                                <CountdownActionButton text="应用设置" onClick={applySettings} color={textColor} bg={accentButtonBg} />
                            </div>
                        ) : (
                            <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "FlexEnd" }}>
                                <CountdownActionButton text="应用颜色" onClick={applySettings} color={textColor} bg={accentButtonBg} compact />
                            </div>
                        )}
                    </div>
                </div>
            ) : null}
        </div>
    )
}

const CountdownCompact = () => {
    const [config, persist] = useRuntimeConfig()
    const todayInfo = useTodayInfo()

    const compactEvents = useMemo(() => {
        return sortEventsPinnedFirst(config.events).slice(0, config.compactCount)
    }, [config.events, config.compactCount])

    const compactPanel = mixHex(config.rightPanelBgColor, "#000000", 0.18)
    const compactBorder = mixHex(config.textColor, config.rightPanelBgColor, 0.68)
    const compactButtonBg = mixHex(config.rightPanelBgColor, "#000000", 0.18)
    const compactButtonActiveBg = mixHex(config.daysColor, "#000000", 0.1)
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

    const compactFont = config.compactCount === 4 ? 13 : (config.compactCount === 1 ? 17 : 16)
    const titleFont = config.compactCount === 1 ? 22 : compactFont
    const dayFont = config.compactCount === 1 ? 18 : compactFont
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
                position: "Relative",
            }}
        >
            <div
                style={{
                    position: "Absolute",
                    top: 6,
                    right: 6,
                    display: "Flex",
                    flexDirection: "Row",
                    alignItems: "Center",
                    zIndex: 2,
                    pointerEvents: "Auto",
                    gap: 3,
                }}
            >
                {[1, 2, 4].map((countValue) => (
                    <div key={`compact-top-count-${countValue}`} style={{ width: 28, flexShrink: 0 }}>
                        <CountdownActionButton
                            text={`${countValue}`}
                            onClick={() => persist({ ...config, compactCount: countValue as 1 | 2 | 4 })}
                            color={config.textColor}
                            bg={config.compactCount === countValue ? compactButtonActiveBg : compactButtonBg}
                            compact
                            fullWidth
                        />
                    </div>
                ))}
            </div>
            <div
                style={{
                    flexGrow: 1,
                    display: "Flex",
                    flexDirection: "Column",
                    minHeight: 0,
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
                                                fontSize: getAdaptiveCompactTitleFontSize(item.title, titleFont, config.compactCount, showCompactDate),
                                                color: config.titleColor,
                                                unityFontStyleAndWeight: "Bold",
                                                unityTextAlign: "MiddleCenter",
                                                width: "100%",
                                                whiteSpace: "NoWrap",
                                                overflow: "Hidden",
                                                textOverflow: "Ellipsis",
                                                marginBottom: 2,
                                            }}
                                        >
                                            {item.title}
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
                                        {formatDaysText(calculateDaysFromTodayStamp(item.targetDate, todayInfo.stamp))}
                                    </div>
                                </div>
                            ))}
                            {autoCols > 1 && row.length < autoCols
                                ? Array.from({ length: autoCols - row.length }, (_, placeholderIndex) => (
                                    <div
                                        key={`compact-placeholder-${rowIndex}-${placeholderIndex}`}
                                        style={{
                                            flexGrow: 1,
                                            flexShrink: 1,
                                            flexBasis: 0,
                                            height: "100%",
                                            marginRight: placeholderIndex < autoCols - row.length - 1 ? COMPACT_CARD_GAP : 0,
                                            minWidth: 0,
                                            minHeight: 0,
                                            backgroundColor: compactPanel,
                                            borderWidth: 1,
                                            borderColor: compactBorder,
                                            borderRadius: 8,
                                            paddingLeft: 7,
                                            paddingRight: 7,
                                            paddingTop: 5,
                                            paddingBottom: 5,
                                            overflow: "Hidden",
                                            opacity: 0,
                                            pointerEvents: "None",
                                        }}
                                    />
                                ))
                                : null}
                        </div>
                    ))}
                </div>
            )}
            </div>
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
        text: "",
        background: "#0ea5e9",
    },
    component: CountdownPanel,
})
