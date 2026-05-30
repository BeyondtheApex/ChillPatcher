(() => {
  // plugin-shims/preact-module.js
  var __p = globalThis.__preact;
  var h = __p.h;
  var Fragment2 = __p.Fragment;
  var createElement = __p.createElement;
  var render = __p.render;
  var createRef = __p.createRef;
  var isValidElement = __p.isValidElement;
  var Component = __p.Component;
  var cloneElement = __p.cloneElement;
  var createContext = __p.createContext;
  var toChildArray = __p.toChildArray;
  var options = __p.options;

  // plugin-shims/preact-hooks-module.js
  var __ph = globalThis.__preactHooks;
  var useState = __ph.useState;
  var useEffect = __ph.useEffect;
  var useCallback = __ph.useCallback;
  var useMemo = __ph.useMemo;
  var useRef = __ph.useRef;
  var useErrorBoundary = __ph.useErrorBoundary;
  var useReducer = __ph.useReducer;
  var useContext = __ph.useContext;
  var useLayoutEffect = __ph.useLayoutEffect;
  var useImperativeHandle = __ph.useImperativeHandle;
  var useDebugValue = __ph.useDebugValue;
  var useEventfulState = __ph.useEventfulState;

  // plugins/countdown-days/index.tsx
  var CONFIG_FILE = "window-states/countdown-days.json";
  var EVENTS_FILE = "window-states/countdown-days-events.json";
  var DEFAULT_EVENTS_PER_PAGE = 8;
  var DEFAULT_PAGE_LABEL_COUNT = 5;
  var DEFAULT_LAYOUT_HINT = {
    windowWidth: 0,
    windowHeight: 0,
    listWidth: 0,
    listHeight: 0
  };
  var EVENT_CARD_ROW_HEIGHT_STANDARD = 90;
  var EVENT_CARD_ROW_HEIGHT_SIMPLE = 74;
  var PAGER_SLOT_WIDTH = 34;
  var PAGER_BUTTON_GAP = 4;
  var PAGER_SIDE_GROUP_WIDTH = PAGER_SLOT_WIDTH * 2 + PAGER_BUTTON_GAP;
  var COMPACT_CARD_GAP = 4;
  var LAYOUT_HINT_SETTLE_DELAY_MS = 140;
  var LAYOUT_HINT_JITTER_PX = 2;
  var WEEK_LABELS = ["\u4E00", "\u4E8C", "\u4E09", "\u56DB", "\u4E94", "\u516D", "\u65E5"];
  var MONTH_LABELS = [
    "1\u6708",
    "2\u6708",
    "3\u6708",
    "4\u6708",
    "5\u6708",
    "6\u6708",
    "7\u6708",
    "8\u6708",
    "9\u6708",
    "10\u6708",
    "11\u6708",
    "12\u6708"
  ];
  var PRESET_COLORS = [
    "#0f172a",
    "#1e293b",
    "#334155",
    "#475569",
    "#0b1120",
    "#082f49",
    "#0c4a6e",
    "#155e75",
    "#164e63",
    "#1d4ed8",
    "#2563eb",
    "#4f46e5",
    "#6366f1",
    "#7c3aed",
    "#9333ea",
    "#be185d",
    "#dc2626",
    "#ea580c",
    "#ca8a04",
    "#4d7c0f",
    "#15803d",
    "#0f766e"
  ];
  var THEME_PRESETS = [
    {
      id: "deep-sea",
      name: "\u6DF1\u6D77\u84DD",
      config: {
        titleColor: "#dbeafe",
        daysColor: "#22d3ee",
        backgroundColor: "#0b1120",
        leftPanelBgColor: "#0f172a",
        rightPanelBgColor: "#13264a",
        textColor: "#cbd5e1"
      }
    },
    {
      id: "forest-night",
      name: "\u591C\u68EE\u6797",
      config: {
        titleColor: "#dcfce7",
        daysColor: "#34d399",
        backgroundColor: "#0a1f16",
        leftPanelBgColor: "#113126",
        rightPanelBgColor: "#14382b",
        textColor: "#bbf7d0"
      }
    },
    {
      id: "sunset-red",
      name: "\u843D\u65E5\u7EA2",
      config: {
        titleColor: "#fee2e2",
        daysColor: "#f97316",
        backgroundColor: "#3b0b15",
        leftPanelBgColor: "#5a1523",
        rightPanelBgColor: "#6b1d2c",
        textColor: "#fecaca"
      }
    },
    {
      id: "steel-gray",
      name: "\u94A2\u94C1\u7070",
      config: {
        titleColor: "#e2e8f0",
        daysColor: "#38bdf8",
        backgroundColor: "#111827",
        leftPanelBgColor: "#1f2937",
        rightPanelBgColor: "#243244",
        textColor: "#cbd5e1"
      }
    },
    {
      id: "day-light",
      name: "\u65E5\u95F4\u4EAE\u8272",
      config: {
        titleColor: "#1f2937",
        daysColor: "#0ea5e9",
        backgroundColor: "#eaf2ff",
        leftPanelBgColor: "#dbeafe",
        rightPanelBgColor: "#eff6ff",
        textColor: "#1e293b"
      }
    },
    {
      id: "mocha-brown",
      name: "\u6469\u5361\u68D5",
      config: {
        titleColor: "#fef3c7",
        daysColor: "#f59e0b",
        backgroundColor: "#2b1b12",
        leftPanelBgColor: "#3a2418",
        rightPanelBgColor: "#4a2d1f",
        textColor: "#fde68a"
      }
    },
    {
      id: "mint-green",
      name: "\u8584\u8377\u7EFF",
      config: {
        titleColor: "#052e2b",
        daysColor: "#0d9488",
        backgroundColor: "#dffaf2",
        leftPanelBgColor: "#c8f5e7",
        rightPanelBgColor: "#ecfdf5",
        textColor: "#115e59"
      }
    },
    {
      id: "sakura-pink",
      name: "\u6A31\u82B1\u7C89",
      config: {
        titleColor: "#4a044e",
        daysColor: "#db2777",
        backgroundColor: "#fce7f3",
        leftPanelBgColor: "#fbcfe8",
        rightPanelBgColor: "#fdf2f8",
        textColor: "#831843"
      }
    },
    {
      id: "ivory-paper",
      name: "\u7C73\u767D\u7EB8",
      config: {
        titleColor: "#3f3a2a",
        daysColor: "#ca8a04",
        backgroundColor: "#f8f3e8",
        leftPanelBgColor: "#f4ead7",
        rightPanelBgColor: "#fdf8ee",
        textColor: "#57534e"
      }
    },
    {
      id: "midnight-black",
      name: "\u6781\u591C\u9ED1",
      config: {
        titleColor: "#f3f4f6",
        daysColor: "#60a5fa",
        backgroundColor: "#05070d",
        leftPanelBgColor: "#0b1020",
        rightPanelBgColor: "#111827",
        textColor: "#d1d5db"
      }
    }
  ];
  var DEFAULT_SETTINGS = {
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
    textColor: "#dbeafe"
  };
  var DEFAULT_CONFIG = {
    ...DEFAULT_SETTINGS,
    events: []
  };
  var createSettingsDraft = (config) => ({
    backgroundColor: normalizeColor(config.backgroundColor, DEFAULT_CONFIG.backgroundColor),
    leftPanelBgColor: normalizeColor(config.leftPanelBgColor, DEFAULT_CONFIG.leftPanelBgColor),
    rightPanelBgColor: normalizeColor(config.rightPanelBgColor, DEFAULT_CONFIG.rightPanelBgColor),
    textColor: normalizeColor(config.textColor, DEFAULT_CONFIG.textColor),
    titleColor: normalizeColor(config.titleColor, DEFAULT_CONFIG.titleColor),
    daysColor: normalizeColor(config.daysColor, DEFAULT_CONFIG.daysColor),
    cardDensity: normalizeCardDensity(config.cardDensity, DEFAULT_CONFIG.cardDensity),
    eventsPerPage: String(normalizeIntInRange(config.eventsPerPage, DEFAULT_CONFIG.eventsPerPage, 1, 20)),
    pageLabelCount: String(normalizeIntInRange(config.pageLabelCount, DEFAULT_CONFIG.pageLabelCount, 3, 9)),
    eventsFilePath: normalizeEventsFilePath(config.eventsFilePath, DEFAULT_SETTINGS.eventsFilePath)
  });
  var sanitizeSettingsNumberText = (value) => {
    return String(value ?? "").replace(/[^0-9]/g, "").slice(0, 2);
  };
  var ensureSettingsNumberText = (value, fallback) => {
    if (typeof value === "string")
      return value;
    if (value === void 0 || value === null)
      return String(fallback);
    return sanitizeSettingsNumberText(value);
  };
  var parseSettingsNumberDraft = (value, fallback, min, max) => {
    const text = typeof value === "string" ? value.trim() : String(value ?? "").trim();
    if (text.length === 0)
      return fallback;
    return normalizeIntInRange(Number(text), fallback, min, max);
  };
  var ensureSettingsNumber = (value, fallback) => {
    const n = Math.round(Number(value));
    return Number.isFinite(n) ? n : fallback;
  };
  var ensureColorDraftText = (value, fallback) => {
    if (typeof value === "string")
      return value;
    return fallback;
  };
  var getDraftColorSwatch = (value, fallback) => {
    const raw = typeof value === "string" ? value : fallback;
    return normalizeColor(raw, fallback);
  };
  var sameColor = (a, b) => getDraftColorSwatch(a, "").toLowerCase() === getDraftColorSwatch(b, "").toLowerCase();
  var normalizeText = (value, fallback) => {
    if (typeof value !== "string")
      return fallback;
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : fallback;
  };
  var normalizeColor = (value, fallback) => {
    const text = normalizeText(value, fallback);
    if (/^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/.test(text))
      return text;
    return fallback;
  };
  var normalizeCompactCount = (value, fallback) => {
    if (value === 1 || value === 2 || value === 4)
      return value;
    const n = Number(value);
    if (n === 1 || n === 2 || n === 4)
      return n;
    return fallback;
  };
  var normalizeCardDensity = (value, fallback) => {
    if (value === "simple" || value === "standard")
      return value;
    return fallback;
  };
  var normalizeIntInRange = (value, fallback, min, max) => {
    const n = Math.round(Number(value));
    if (!Number.isFinite(n))
      return fallback;
    return Math.max(min, Math.min(max, n));
  };
  var normalizeConfigPath = (value, fallback) => {
    const text = normalizeText(value, fallback).replace(/\\/g, "/");
    return text.length > 0 ? text : fallback;
  };
  var canonicalizePath = (value) => {
    return normalizeConfigPath(value, "").replace(/^\.\//, "").replace(/\/{2,}/g, "/").toLowerCase();
  };
  var normalizeEventsFilePath = (value, fallback) => {
    const normalizedFallback = normalizeConfigPath(fallback, EVENTS_FILE);
    const normalized = normalizeConfigPath(value, normalizedFallback);
    return canonicalizePath(normalized) === canonicalizePath(CONFIG_FILE) ? normalizedFallback : normalized;
  };
  var extractPersistedSettings = (config) => ({
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
    textColor: normalizeColor(config.textColor, DEFAULT_SETTINGS.textColor)
  });
  var hexToRgba = (hex, alpha) => {
    const raw = normalizeColor(hex, "#0f172a").replace("#", "");
    if (raw.length !== 6)
      return `rgba(15,23,42,${alpha})`;
    const r = parseInt(raw.slice(0, 2), 16);
    const g = parseInt(raw.slice(2, 4), 16);
    const b = parseInt(raw.slice(4, 6), 16);
    return `rgba(${r},${g},${b},${alpha})`;
  };
  var mixHex = (baseHex, mixHexColor, ratio) => {
    const r = Math.max(0, Math.min(1, ratio));
    const base = normalizeColor(baseHex, "#0f172a").slice(1);
    const mix = normalizeColor(mixHexColor, "#000000").slice(1);
    const br = parseInt(base.slice(0, 2), 16);
    const bg = parseInt(base.slice(2, 4), 16);
    const bb = parseInt(base.slice(4, 6), 16);
    const mr = parseInt(mix.slice(0, 2), 16);
    const mg = parseInt(mix.slice(2, 4), 16);
    const mb = parseInt(mix.slice(4, 6), 16);
    const toHex = (value) => Math.round(value).toString(16).padStart(2, "0");
    const outR = br + (mr - br) * r;
    const outG = bg + (mg - bg) * r;
    const outB = bb + (mb - bb) * r;
    return `#${toHex(outR)}${toHex(outG)}${toHex(outB)}`;
  };
  var getDateTimePart = (source, field, getterName) => {
    const directValue = source?.[field];
    if (directValue !== void 0 && directValue !== null) {
      const num = Number(directValue);
      if (Number.isFinite(num))
        return num;
    }
    const getter = source?.[getterName];
    if (typeof getter === "function") {
      const num = Number(getter.call(source));
      if (Number.isFinite(num))
        return num;
    }
    return null;
  };
  var nowFromHost = () => {
    const g = globalThis;
    try {
      const raw = g?.CS?.System?.DateTime?.Now ?? g?.CS?.System?.DateTime?.get_Now?.();
      const dt = typeof raw === "function" ? raw() : raw;
      if (dt) {
        const year = getDateTimePart(dt, "Year", "get_Year");
        const month = getDateTimePart(dt, "Month", "get_Month");
        const day = getDateTimePart(dt, "Day", "get_Day");
        const hour = getDateTimePart(dt, "Hour", "get_Hour") ?? 0;
        const minute = getDateTimePart(dt, "Minute", "get_Minute") ?? 0;
        const second = getDateTimePart(dt, "Second", "get_Second") ?? 0;
        const ms = getDateTimePart(dt, "Millisecond", "get_Millisecond") ?? 0;
        if (year && month && day) {
          return new Date(year, month - 1, day, hour, minute, second, ms);
        }
      }
    } catch (_) {
    }
    return /* @__PURE__ */ new Date();
  };
  var toIsoDate = (date) => {
    const y = String(date.getFullYear()).padStart(4, "0");
    const m = String(date.getMonth() + 1).padStart(2, "0");
    const d = String(date.getDate()).padStart(2, "0");
    return `${y}-${m}-${d}`;
  };
  var parseIsoDate = (value) => {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value))
      return null;
    const [yText, mText, dText] = value.split("-");
    const y = Number(yText);
    const m = Number(mText);
    const d = Number(dText);
    if (!Number.isFinite(y) || !Number.isFinite(m) || !Number.isFinite(d))
      return null;
    const date = new Date(y, m - 1, d);
    if (date.getFullYear() !== y || date.getMonth() !== m - 1 || date.getDate() !== d)
      return null;
    return date;
  };
  var getTodayStampFromNow = (now) => Date.UTC(now.getFullYear(), now.getMonth(), now.getDate());
  var getTodayInfo = () => {
    const now = nowFromHost();
    const nextRefresh = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1, 0, 0, 0, 50);
    return {
      iso: toIsoDate(now),
      stamp: getTodayStampFromNow(now),
      delayMs: Math.max(1e3, nextRefresh.getTime() - now.getTime())
    };
  };
  var sortEvents = (events) => {
    return [...events].sort((a, b) => {
      const dateCompare = a.targetDate.localeCompare(b.targetDate);
      if (dateCompare !== 0)
        return dateCompare;
      const pinCompare = Number(Boolean(b.pinned)) - Number(Boolean(a.pinned));
      if (pinCompare !== 0)
        return pinCompare;
      return a.createdAt.localeCompare(b.createdAt);
    });
  };
  var sortEventsPinnedFirst = (events) => {
    return [...events].sort((a, b) => {
      const pinCompare = Number(Boolean(b.pinned)) - Number(Boolean(a.pinned));
      if (pinCompare !== 0)
        return pinCompare;
      const dateCompare = a.targetDate.localeCompare(b.targetDate);
      if (dateCompare !== 0)
        return dateCompare;
      return a.createdAt.localeCompare(b.createdAt);
    });
  };
  var getAdaptiveCompactTitleFontSize = (title, baseFont, compactCount, showDate) => {
    const len = String(title ?? "").trim().length;
    let size = baseFont;
    if (compactCount === 4) {
      if (len >= 14)
        size -= 3;
      else if (len >= 10)
        size -= 2;
      else if (len >= 7)
        size -= 1;
    } else if (compactCount === 2) {
      if (len >= 20)
        size -= 4;
      else if (len >= 15)
        size -= 3;
      else if (len >= 11)
        size -= 2;
      else if (len >= 8)
        size -= 1;
    } else {
      if (len >= 28)
        size -= 4;
      else if (len >= 22)
        size -= 3;
      else if (len >= 16)
        size -= 2;
      else if (len >= 12)
        size -= 1;
    }
    if (showDate && compactCount !== 1)
      size -= 1;
    return Math.max(compactCount === 1 ? 12 : 10, size);
  };
  var normalizeEvent = (raw, index) => {
    if (!raw || typeof raw !== "object")
      return null;
    const targetDate = normalizeText(raw.targetDate, "");
    if (!parseIsoDate(targetDate))
      return null;
    const rawType = normalizeText(raw.type, "countdown");
    const type = rawType === "elapsed" ? "elapsed" : "countdown";
    return {
      id: normalizeText(raw.id, `evt-${index}-${Math.random().toString(36).slice(2, 8)}`),
      title: normalizeText(raw.title, "\u672A\u547D\u540D\u4E8B\u4EF6"),
      targetDate,
      type,
      createdAt: normalizeText(raw.createdAt, (/* @__PURE__ */ new Date()).toISOString()),
      pinned: Boolean(raw.pinned)
    };
  };
  var normalizePersistedSettings = (raw) => {
    const source = raw && typeof raw === "object" ? raw : {};
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
      textColor: normalizeColor(source.textColor, DEFAULT_SETTINGS.textColor)
    };
  };
  var normalizeEvents = (rawEvents) => {
    const source = Array.isArray(rawEvents) ? rawEvents : [];
    return sortEvents(
      source.map((item, index) => normalizeEvent(item, index)).filter((item) => item !== null)
    );
  };
  var composeRuntimeConfig = (settings, events) => ({
    ...settings,
    eventsFilePath: normalizeEventsFilePath(settings.eventsFilePath, DEFAULT_SETTINGS.eventsFilePath),
    events: normalizeEvents(events)
  });
  var ensureEventsFileInitialized = (eventsFilePath) => {
    const normalizedPath = normalizeEventsFilePath(eventsFilePath, DEFAULT_SETTINGS.eventsFilePath);
    if (!chill?.io?.exists?.(normalizedPath)) {
      chill?.io?.writeText?.(normalizedPath, JSON.stringify({ events: [] }, null, 2));
      return;
    }
    const text = chill?.io?.readText?.(normalizedPath);
    if (!text)
      chill?.io?.writeText?.(normalizedPath, JSON.stringify({ events: [] }, null, 2));
  };
  var loadConfig = () => {
    try {
      if (!chill?.io?.exists?.(CONFIG_FILE)) {
        saveConfig(DEFAULT_SETTINGS);
        return normalizePersistedSettings(DEFAULT_SETTINGS);
      }
      const text = chill?.io?.readText?.(CONFIG_FILE);
      if (!text) {
        saveConfig(DEFAULT_SETTINGS);
        return normalizePersistedSettings(DEFAULT_SETTINGS);
      }
      return normalizePersistedSettings(JSON.parse(text));
    } catch (e) {
      console.error("[countdown-days] load config failed", CONFIG_FILE, e);
      saveConfig(DEFAULT_SETTINGS);
      return normalizePersistedSettings(DEFAULT_SETTINGS);
    }
  };
  var saveConfig = (config) => {
    try {
      chill?.io?.writeText?.(CONFIG_FILE, JSON.stringify(extractPersistedSettings(config), null, 2));
    } catch (e) {
      console.error("[countdown-days] save config failed", CONFIG_FILE, e);
    }
  };
  var loadEventsFromFile = (eventsFilePath) => {
    try {
      if (!chill?.io?.exists?.(eventsFilePath)) {
        chill?.io?.writeText?.(eventsFilePath, JSON.stringify({ events: [] }, null, 2));
        return [];
      }
      const text = chill?.io?.readText?.(eventsFilePath);
      if (!text) {
        chill?.io?.writeText?.(eventsFilePath, JSON.stringify({ events: [] }, null, 2));
        return [];
      }
      const raw = JSON.parse(text);
      const eventsRaw = Array.isArray(raw) ? raw : Array.isArray(raw?.events) ? raw.events : [];
      return eventsRaw.map((item, index) => normalizeEvent(item, index)).filter((item) => item !== null);
    } catch (e) {
      console.error("[countdown-days] load events failed", eventsFilePath, e);
      return null;
    }
  };
  var saveEventsToFile = (events, eventsFilePath) => {
    try {
      chill?.io?.writeText?.(eventsFilePath, JSON.stringify({ events }, null, 2));
    } catch (e) {
      console.error("[countdown-days] save events failed", eventsFilePath, e);
    }
  };
  var loadRuntimeConfig = () => {
    const settings = loadConfig();
    const eventsFilePath = normalizeEventsFilePath(settings.eventsFilePath, DEFAULT_SETTINGS.eventsFilePath);
    ensureEventsFileInitialized(eventsFilePath);
    const loadedEvents = loadEventsFromFile(eventsFilePath);
    return composeRuntimeConfig(
      { ...settings, eventsFilePath },
      Array.isArray(loadedEvents) ? loadedEvents : []
    );
  };
  var runtimeConfigSnapshot = DEFAULT_CONFIG;
  var runtimeConfigLoaded = false;
  var runtimeConfigListeners = /* @__PURE__ */ new Set();
  var notifyRuntimeConfigListeners = (nextConfig) => {
    runtimeConfigListeners.forEach((listener) => {
      try {
        listener(nextConfig);
      } catch (error) {
        console.error("[countdown-days] runtime config listener failed", error);
      }
    });
  };
  var getRuntimeConfigSnapshot = () => {
    if (!runtimeConfigLoaded) {
      runtimeConfigSnapshot = loadRuntimeConfig();
      runtimeConfigLoaded = true;
    }
    return runtimeConfigSnapshot;
  };
  var subscribeRuntimeConfig = (listener) => {
    runtimeConfigListeners.add(listener);
    return () => {
      runtimeConfigListeners.delete(listener);
    };
  };
  var persistRuntimeConfig = (nextConfig) => {
    const normalizedSettings = normalizePersistedSettings(nextConfig);
    const normalizedEvents = normalizeEvents(nextConfig.events);
    const normalizedConfig = composeRuntimeConfig(
      {
        ...normalizedSettings,
        eventsFilePath: normalizeEventsFilePath(normalizedSettings.eventsFilePath, DEFAULT_SETTINGS.eventsFilePath)
      },
      normalizedEvents
    );
    saveConfig(normalizedConfig);
    saveEventsToFile(normalizedConfig.events, normalizedConfig.eventsFilePath);
    runtimeConfigSnapshot = normalizedConfig;
    runtimeConfigLoaded = true;
    notifyRuntimeConfigListeners(normalizedConfig);
    return normalizedConfig;
  };
  var useRuntimeConfig = () => {
    const [config, setConfig] = useState(() => getRuntimeConfigSnapshot());
    useEffect(() => {
      setConfig(getRuntimeConfigSnapshot());
      return subscribeRuntimeConfig(setConfig);
    }, []);
    const persist = useCallback((nextConfig) => {
      persistRuntimeConfig(nextConfig);
    }, []);
    return [config, persist];
  };
  var calculateDaysFromTodayStamp = (targetDate, todayStamp) => {
    const target = parseIsoDate(targetDate);
    if (!target)
      return 0;
    const targetStamp = Date.UTC(target.getFullYear(), target.getMonth(), target.getDate());
    return Math.floor((targetStamp - todayStamp) / 864e5);
  };
  var formatDaysText = (days) => {
    if (days > 0)
      return `\u8FD8\u5269 ${days} \u5929`;
    if (days === 0)
      return "\u5C31\u5728\u4ECA\u5929";
    return `\u5DF2\u7ECF\u8FC7\u53BB ${Math.abs(days)} \u5929`;
  };
  var resolveEventType = (targetDate, todayStamp) => calculateDaysFromTodayStamp(targetDate, todayStamp) < 0 ? "elapsed" : "countdown";
  var clampConfiguredByLayout = (configured, layoutLimit, min, max) => {
    const boundedConfigured = Math.max(min, Math.min(max, Math.round(configured)));
    if (!Number.isFinite(layoutLimit) || layoutLimit <= 0)
      return boundedConfigured;
    return Math.max(min, Math.min(max, Math.min(boundedConfigured, Math.round(layoutLimit))));
  };
  var getMaxPageLabelsByWidth = (listWidth) => {
    if (!Number.isFinite(listWidth) || listWidth <= 0)
      return 0;
    const centerWidth = Math.max(0, listWidth - PAGER_SIDE_GROUP_WIDTH * 2);
    if (centerWidth <= 0)
      return 1;
    return Math.max(1, Math.floor((centerWidth + PAGER_BUTTON_GAP) / (PAGER_SLOT_WIDTH + PAGER_BUTTON_GAP)));
  };
  var buildPageNumbers = (currentPage, totalPages, labelCount) => {
    if (totalPages <= labelCount) {
      return Array.from({ length: totalPages }, (_, index) => index + 1);
    }
    const halfWindow = Math.floor(labelCount / 2);
    let start = Math.max(1, currentPage - halfWindow);
    let end = start + labelCount - 1;
    if (end > totalPages) {
      end = totalPages;
      start = end - labelCount + 1;
    }
    return Array.from({ length: end - start + 1 }, (_, index) => start + index);
  };
  var chunkItems = (items, chunkSize) => {
    if (chunkSize <= 0)
      return [];
    const rows = [];
    for (let i = 0; i < items.length; i += chunkSize)
      rows.push(items.slice(i, i + chunkSize));
    return rows;
  };
  var monthTitle = (year, month) => `${year}\u5E74 ${month + 1}\u6708`;
  var shiftMonth = (year, month, delta) => {
    const total = year * 12 + month + delta;
    const nextYear = Math.floor(total / 12);
    let nextMonth = total % 12;
    if (nextMonth < 0)
      nextMonth += 12;
    return { year: nextYear, month: nextMonth };
  };
  var buildCalendarCells = (year, month) => {
    const firstDay = new Date(year, month, 1);
    const firstWeekdayMonday0 = (firstDay.getDay() + 6) % 7;
    const gridStart = new Date(year, month, 1 - firstWeekdayMonday0);
    const cells = [];
    for (let i = 0; i < 42; i++) {
      const date = new Date(gridStart.getFullYear(), gridStart.getMonth(), gridStart.getDate() + i);
      cells.push({
        iso: toIsoDate(date),
        year: date.getFullYear(),
        month: date.getMonth(),
        day: date.getDate(),
        inCurrentMonth: date.getMonth() === month
      });
    }
    return cells;
  };
  var CountdownActionButton = ({
    text,
    onClick,
    color,
    bg,
    disabled,
    compact,
    fullWidth
  }) => /* @__PURE__ */ h(
    "div",
    {
      onPointerDown: (e) => {
        e?.stopPropagation?.();
        if (!disabled)
          onClick();
      },
      onPointerUp: (e) => {
        e?.stopPropagation?.();
      },
      style: {
        fontSize: compact ? 10 : 11,
        color: disabled ? "#64748b" : color || "#dbeafe",
        backgroundColor: disabled ? "rgba(51,65,85,0.3)" : bg || "#334155",
        paddingLeft: compact ? 7 : 8,
        paddingRight: compact ? 7 : 8,
        paddingTop: compact ? 4 : 5,
        paddingBottom: compact ? 4 : 5,
        borderRadius: 6,
        minHeight: compact ? 20 : 22,
        width: fullWidth ? "100%" : void 0,
        whiteSpace: "NoWrap",
        overflow: "Auto",
        textOverflow: "Ellipsis",
        unityTextAlign: "MiddleCenter"
      }
    },
    text
  );
  var ColorEditorRow = ({
    label,
    value,
    onChange,
    textColor,
    inputBg,
    inputBorder,
    fallbackColor
  }) => {
    const draftText = ensureColorDraftText(value, fallbackColor);
    const swatchColor = getDraftColorSwatch(draftText, fallbackColor);
    return /* @__PURE__ */ h("div", { style: { marginBottom: 9 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 4 } }, label), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 5 } }, /* @__PURE__ */ h(
      "div",
      {
        style: {
          width: 20,
          height: 20,
          borderRadius: 4,
          marginRight: 6,
          backgroundColor: swatchColor,
          borderWidth: 1,
          borderColor: hexToRgba(textColor, 0.45)
        }
      }
    ), /* @__PURE__ */ h(
      "textfield",
      {
        value: draftText,
        multiline: false,
        onValueChanged: (e) => onChange(String(e?.newValue ?? "")),
        style: {
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
          unityTextAlign: "MiddleLeft"
        }
      }
    )), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", flexWrap: "Wrap" } }, PRESET_COLORS.map((color) => /* @__PURE__ */ h(
      "div",
      {
        key: `${label}-${color}`,
        onPointerDown: () => onChange(color),
        style: {
          width: 16,
          height: 16,
          borderRadius: 4,
          marginRight: 4,
          marginBottom: 4,
          backgroundColor: color,
          borderWidth: sameColor(draftText, color) ? 2 : 1,
          borderColor: sameColor(draftText, color) ? textColor : hexToRgba(textColor, 0.4)
        }
      }
    ))));
  };
  var useTodayInfo = () => {
    const [todayInfo, setTodayInfo] = useState(() => getTodayInfo());
    useEffect(() => {
      let timer = null;
      const schedule = () => {
        const nextInfo = getTodayInfo();
        setTodayInfo((prev) => prev.iso === nextInfo.iso && prev.stamp === nextInfo.stamp ? prev : nextInfo);
        timer = setTimeout(schedule, nextInfo.delayMs);
      };
      schedule();
      return () => {
        if (timer)
          clearTimeout(timer);
      };
    }, []);
    return todayInfo;
  };
  var useWindowLayoutHint = (eventListRef, enabled = true, measureKey = "") => {
    const [layoutHint, setLayoutHint] = useState(DEFAULT_LAYOUT_HINT);
    useEffect(() => {
      if (!enabled)
        return;
      let settleTimer = null;
      let rafId = null;
      let animationLocked = false;
      const detachListeners = [];
      const getMeasuredNode = (node) => {
        if (!node)
          return null;
        return node.base ?? node;
      };
      const readNodeHeight = (node) => {
        const measured = getMeasuredNode(node);
        const veHeight = Number(node?.ve?.layout?.height ?? measured?.ve?.layout?.height ?? 0);
        if (Number.isFinite(veHeight) && veHeight > 0)
          return Math.round(veHeight);
        const rectHeight = Number(measured?.getBoundingClientRect?.().height ?? measured?.offsetHeight ?? 0);
        return Number.isFinite(rectHeight) && rectHeight > 0 ? Math.round(rectHeight) : 0;
      };
      const readNodeWidth = (node) => {
        const measured = getMeasuredNode(node);
        const veWidth = Number(node?.ve?.layout?.width ?? measured?.ve?.layout?.width ?? 0);
        if (Number.isFinite(veWidth) && veWidth > 0)
          return Math.round(veWidth);
        const rectWidth = Number(measured?.getBoundingClientRect?.().width ?? measured?.offsetWidth ?? 0);
        return Number.isFinite(rectWidth) && rectWidth > 0 ? Math.round(rectWidth) : 0;
      };
      const applyLayout = () => {
        animationLocked = false;
        const nextListWidth = Math.max(0, readNodeWidth(eventListRef.current));
        const nextListHeight = Math.max(0, readNodeHeight(eventListRef.current));
        const nextWindowWidth = Math.max(0, Math.round(Number(globalThis?.innerWidth ?? 0)));
        const nextWindowHeight = Math.max(0, Math.round(Number(globalThis?.innerHeight ?? 0)));
        setLayoutHint((prev) => {
          const same = Math.abs(prev.listWidth - nextListWidth) <= LAYOUT_HINT_JITTER_PX && Math.abs(prev.listHeight - nextListHeight) <= LAYOUT_HINT_JITTER_PX && prev.windowWidth === nextWindowWidth && prev.windowHeight === nextWindowHeight;
          if (same)
            return prev;
          return {
            windowWidth: nextWindowWidth,
            windowHeight: nextWindowHeight,
            listWidth: nextListWidth,
            listHeight: nextListHeight
          };
        });
      };
      const scheduleRead = () => {
        if (settleTimer)
          clearTimeout(settleTimer);
        if (animationLocked) {
          settleTimer = setTimeout(applyLayout, LAYOUT_HINT_SETTLE_DELAY_MS);
          return;
        }
        animationLocked = true;
        if (rafId)
          cancelAnimationFrame(rafId);
        rafId = requestAnimationFrame(() => {
          applyLayout();
          settleTimer = setTimeout(applyLayout, LAYOUT_HINT_SETTLE_DELAY_MS);
        });
      };
      const GEOMETRY_CHANGED_EVENT = "geometrychanged";
      const bindGeometryChanged = (node) => {
        const measured = getMeasuredNode(node);
        if (!measured?.addEventListener)
          return false;
        const handler = () => scheduleRead();
        try {
          measured.addEventListener(GEOMETRY_CHANGED_EVENT, handler);
          detachListeners.push(() => measured.removeEventListener?.(GEOMETRY_CHANGED_EVENT, handler));
          return true;
        } catch (_) {
          return false;
        }
      };
      scheduleRead();
      const eventListNode = eventListRef.current;
      const hasGeometryListener = bindGeometryChanged(eventListNode);
      if (!hasGeometryListener) {
      }
      return () => {
        if (settleTimer)
          clearTimeout(settleTimer);
        if (rafId)
          cancelAnimationFrame(rafId);
        animationLocked = false;
        detachListeners.forEach((detach) => {
          try {
            detach();
          } catch (_) {
          }
        });
      };
    }, [enabled, measureKey]);
    return layoutHint;
  };
  var EventCard = ({
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
    removeEvent
  }) => {
    const dayDiff = calculateDaysFromTodayStamp(item.targetDate, todayStamp);
    const dayText = formatDaysText(dayDiff);
    const cardBg = isEditing ? mixHex(daysColor, rightPanelBgColor, 0.82) : panelInnerBg;
    const cardBorder = linkedToSelected ? selectedEventBorder : defaultEventBorder;
    const actionBaseBg = mixHex(rightPanelBgColor, "#000000", 0.18);
    const actionPinBg = item.pinned ? mixHex(daysColor, rightPanelBgColor, 0.22) : actionBaseBg;
    const actionDeleteBg = mixHex("#7f1d1d", rightPanelBgColor, 0.45);
    return /* @__PURE__ */ h(
      "div",
      {
        key: item.id,
        onPointerDown: () => focusDate(item.targetDate),
        style: {
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
          overflow: "Hidden"
        }
      },
      !isSimpleCard ? /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "SpaceBetween", marginBottom: 4 } }, /* @__PURE__ */ h("div", { style: { fontSize: 9, color: subtleText } }, item.targetDate), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { width: 40 } }, /* @__PURE__ */ h(
        CountdownActionButton,
        {
          text: item.pinned ? "UNP" : "PIN",
          onClick: () => togglePinned(item.id),
          color: item.pinned ? mixHex("#0b1120", textColor, 0.2) : textColor,
          bg: actionPinBg,
          compact: true
        }
      )), /* @__PURE__ */ h("div", { style: { width: 4 } }), /* @__PURE__ */ h("div", { style: { width: 40 } }, /* @__PURE__ */ h(CountdownActionButton, { text: "EDT", onClick: () => startEdit(item), color: textColor, bg: actionBaseBg, compact: true })), /* @__PURE__ */ h("div", { style: { width: 4 } }), /* @__PURE__ */ h("div", { style: { width: 40 } }, /* @__PURE__ */ h(CountdownActionButton, { text: "DEL", onClick: () => setDeleteConfirmId(item.id), color: "#fecaca", bg: actionDeleteBg, compact: true })))) : null,
      /* @__PURE__ */ h("div", { style: { fontSize: 12, color: titleColor, unityFontStyleAndWeight: "Bold", whiteSpace: "NoWrap", overflow: "Hidden", textOverflow: "Ellipsis", marginBottom: isSimpleCard ? 4 : 6 } }, item.title),
      /* @__PURE__ */ h("div", { style: { fontSize: 15, color: daysColor, unityFontStyleAndWeight: "Bold" } }, dayText),
      isDeletePending ? /* @__PURE__ */ h(
        "div",
        {
          onPointerDown: (e) => {
            e?.stopPropagation?.();
          },
          style: { position: "Absolute", left: 0, right: 0, top: 0, bottom: 0, backgroundColor: mixHex(rightPanelBgColor, "#000000", 0.28), borderRadius: 10, display: "Flex", flexDirection: "Column", justifyContent: "Center", alignItems: "Center" }
        },
        /* @__PURE__ */ h("div", { style: { fontSize: 10, color: "#fca5a5", unityFontStyleAndWeight: "Bold", marginBottom: 6 } }, "CONFIRM DELETE?"),
        /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { width: 36 } }, /* @__PURE__ */ h(CountdownActionButton, { text: "YES", onClick: () => removeEvent(item.id), color: "#dcfce7", bg: "#14532d", compact: true })), /* @__PURE__ */ h("div", { style: { width: 4 } }), /* @__PURE__ */ h("div", { style: { width: 36 } }, /* @__PURE__ */ h(CountdownActionButton, { text: "NO", onClick: () => setDeleteConfirmId(null), color: textColor, bg: actionBaseBg, compact: true })))
      ) : null
    );
  };
  var EventListSection = ({ showCountLabel, allEventsLength, mutedText, panelInnerBg, panelBorder, pagedEvents, eventPage, totalEventPages, visiblePageNumbers, textColor, softActionBg, accentButtonBg, eventListRef, setEventPage, todayStamp, isSimpleCard, selectedDate, editDialogId, deleteConfirmId, subtleText, titleColor, daysColor, rightPanelBgColor, selectedEventBorder, defaultEventBorder, focusDate, togglePinned, startEdit, setDeleteConfirmId, removeEvent }) => /* @__PURE__ */ h(Fragment, null, showCountLabel ? /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 4, whiteSpace: "NoWrap", display: "Flex", flexDirection: "Row", alignItems: "Center" } }, `\u5168\u90E8\u4E8B\u4EF6\uFF08\u7F6E\u9876\u4F18\u5148\uFF09: ${allEventsLength}`) : null, /* @__PURE__ */ h("div", { ref: eventListRef, style: { flexGrow: 1, flexShrink: 1, minHeight: 0, backgroundColor: panelInnerBg, borderWidth: 1, borderColor: panelBorder, borderRadius: 8, padding: 6, overflow: "Hidden" } }, allEventsLength === 0 ? /* @__PURE__ */ h("div", { style: { fontSize: 10, color: mutedText } }, "\u6682\u65E0\u4E8B\u4EF6\uFF0C\u5148\u5728\u5DE6\u4FA7\u9009\u65E5\u671F\u540E\u6DFB\u52A0\u4E00\u4E2A\u3002") : /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Column", alignItems: "Stretch", minWidth: 0 } }, pagedEvents.map((item) => /* @__PURE__ */ h(
    EventCard,
    {
      key: item.id,
      item,
      todayStamp,
      isSimpleCard,
      isEditing: editDialogId === item.id,
      linkedToSelected: item.targetDate === selectedDate,
      isDeletePending: deleteConfirmId === item.id,
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
      removeEvent
    }
  )))), /* @__PURE__ */ h("div", { style: { display: "Flex", justifyContent: "Center", alignItems: "Center", marginTop: 4, width: "100%" } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "Center", width: "100%", minWidth: 0 } }, /* @__PURE__ */ h("div", { style: { width: PAGER_SIDE_GROUP_WIDTH, minWidth: PAGER_SIDE_GROUP_WIDTH, flexShrink: 0, display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "FlexStart", gap: PAGER_BUTTON_GAP } }, /* @__PURE__ */ h("div", { style: { width: PAGER_SLOT_WIDTH, flexShrink: 0 } }, /* @__PURE__ */ h(CountdownActionButton, { text: "<<", onClick: () => setEventPage(1), disabled: eventPage <= 1, color: textColor, bg: softActionBg, compact: true, fullWidth: true })), /* @__PURE__ */ h("div", { style: { width: PAGER_SLOT_WIDTH, flexShrink: 0 } }, /* @__PURE__ */ h(CountdownActionButton, { text: "<", onClick: () => setEventPage((page) => Math.max(1, page - 1)), disabled: eventPage <= 1, color: textColor, bg: softActionBg, compact: true, fullWidth: true }))), /* @__PURE__ */ h("div", { style: { flexGrow: 1, minWidth: 0, display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "Center", gap: PAGER_BUTTON_GAP } }, visiblePageNumbers.map((pageNumber) => /* @__PURE__ */ h("div", { key: `page-${pageNumber}`, style: { width: PAGER_SLOT_WIDTH, flexShrink: 0 } }, /* @__PURE__ */ h(CountdownActionButton, { text: `${pageNumber}`, onClick: () => setEventPage(pageNumber), color: textColor, bg: eventPage === pageNumber ? accentButtonBg : softActionBg, compact: true, fullWidth: true })))), /* @__PURE__ */ h("div", { style: { width: PAGER_SIDE_GROUP_WIDTH, minWidth: PAGER_SIDE_GROUP_WIDTH, flexShrink: 0, display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "FlexEnd", gap: PAGER_BUTTON_GAP } }, /* @__PURE__ */ h("div", { style: { width: PAGER_SLOT_WIDTH, flexShrink: 0 } }, /* @__PURE__ */ h(CountdownActionButton, { text: ">", onClick: () => setEventPage((page) => Math.min(totalEventPages, page + 1)), disabled: eventPage >= totalEventPages, color: textColor, bg: softActionBg, compact: true, fullWidth: true })), /* @__PURE__ */ h("div", { style: { width: PAGER_SLOT_WIDTH, flexShrink: 0 } }, /* @__PURE__ */ h(CountdownActionButton, { text: ">>", onClick: () => setEventPage(totalEventPages), disabled: eventPage >= totalEventPages, color: textColor, bg: softActionBg, compact: true, fullWidth: true }))))));
  var WeekLabelsRow = ({ subtleText }) => /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 3, width: "100%" } }, WEEK_LABELS.map((label) => /* @__PURE__ */ h("div", { key: `week-${label}`, style: { width: "14.2857%", flexGrow: 0, flexShrink: 0, fontSize: 9, color: subtleText, unityTextAlign: "MiddleCenter" } }, label)));
  var CalendarGridRows = ({ calendarRows, selectedDate, todayIsoValue, eventCountByDate, focusDate, selectedCellBg, calendarCellBg, calendarCellMutedBg, daysColor, textColor, subtleText }) => /* @__PURE__ */ h(Fragment, null, calendarRows.map((row, rowIndex) => /* @__PURE__ */ h("div", { key: `row-${rowIndex}`, style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 3, width: "100%" } }, row.map((cell) => {
    const isSelected = cell.iso === selectedDate;
    const isToday = cell.iso === todayIsoValue;
    const count = eventCountByDate.get(cell.iso) || 0;
    return /* @__PURE__ */ h("div", { key: cell.iso, onPointerDown: () => focusDate(cell.iso), style: { width: "14.2857%", flexGrow: 0, flexShrink: 0, height: 33, borderRadius: 6, backgroundColor: isSelected ? selectedCellBg : cell.inCurrentMonth ? calendarCellBg : calendarCellMutedBg, borderWidth: isToday ? 1 : 0, borderColor: isToday ? daysColor : "transparent", display: "Flex", flexDirection: "Column", justifyContent: "Center", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: isSelected ? "#0b1120" : cell.inCurrentMonth ? textColor : subtleText, unityFontStyleAndWeight: isSelected ? "Bold" : "Normal" } }, cell.day), /* @__PURE__ */ h("div", { style: { fontSize: 8, color: isSelected ? "#0b1120" : daysColor } }, count > 0 ? `\u2022${count}` : ""));
  }))));
  var useEventSectionModel = ({
    events,
    cardDensity,
    rightPanelBgColor,
    textColor,
    daysColor,
    configuredEventsPerPage,
    configuredPageLabelCount,
    listWidth,
    listHeight,
    eventPage
  }) => {
    const sortedEvents = useMemo(() => sortEvents(events), [events]);
    const eventCountByDate = useMemo(() => {
      const map = /* @__PURE__ */ new Map();
      for (const eventItem of sortedEvents) {
        map.set(eventItem.targetDate, (map.get(eventItem.targetDate) || 0) + 1);
      }
      return map;
    }, [sortedEvents]);
    const allEvents = useMemo(() => sortEventsPinnedFirst(sortedEvents), [sortedEvents]);
    const isSimpleCard = cardDensity === "simple";
    const estimatedCardRowHeight = isSimpleCard ? EVENT_CARD_ROW_HEIGHT_SIMPLE : EVENT_CARD_ROW_HEIGHT_STANDARD;
    const measuredRowsPerPage = listHeight > 0 ? Math.max(1, Math.floor((listHeight + 6) / estimatedCardRowHeight)) : configuredEventsPerPage;
    const eventsPerPage = clampConfiguredByLayout(configuredEventsPerPage, measuredRowsPerPage, 1, 20);
    const measuredPageLabelCount = listWidth > 0 ? getMaxPageLabelsByWidth(listWidth) : configuredPageLabelCount;
    const pageLabelCount = clampConfiguredByLayout(configuredPageLabelCount, measuredPageLabelCount, 1, 9);
    const totalEventPages = useMemo(
      () => Math.max(1, Math.ceil(allEvents.length / eventsPerPage)),
      [allEvents.length, eventsPerPage]
    );
    const pagedEvents = useMemo(() => {
      const start = (eventPage - 1) * eventsPerPage;
      return allEvents.slice(start, start + eventsPerPage);
    }, [allEvents, eventPage, eventsPerPage]);
    const visiblePageNumbers = useMemo(
      () => buildPageNumbers(eventPage, totalEventPages, pageLabelCount),
      [eventPage, totalEventPages, pageLabelCount]
    );
    const selectedEventBorder = useMemo(
      () => mixHex(daysColor, rightPanelBgColor, 0.24),
      [daysColor, rightPanelBgColor]
    );
    const defaultEventBorder = useMemo(
      () => mixHex(textColor, rightPanelBgColor, 0.78),
      [textColor, rightPanelBgColor]
    );
    return {
      eventCountByDate,
      allEvents,
      totalEventPages,
      pagedEvents,
      visiblePageNumbers,
      isSimpleCard,
      selectedEventBorder,
      defaultEventBorder
    };
  };
  var CountdownPanel = () => {
    const [config, persist] = useRuntimeConfig();
    const todayInfo = useTodayInfo();
    const [selectedDate, setSelectedDate] = useState(todayInfo.iso);
    const [viewYear, setViewYear] = useState(() => {
      const parsed = parseIsoDate(todayInfo.iso);
      return parsed ? parsed.getFullYear() : nowFromHost().getFullYear();
    });
    const [viewMonth, setViewMonth] = useState(() => {
      const parsed = parseIsoDate(todayInfo.iso);
      return parsed ? parsed.getMonth() : nowFromHost().getMonth();
    });
    const [draftTitle, setDraftTitle] = useState("");
    const [editDialogOpen, setEditDialogOpen] = useState(false);
    const [editDialogId, setEditDialogId] = useState(null);
    const [editDialogTitle, setEditDialogTitle] = useState("");
    const [editDialogDate, setEditDialogDate] = useState(todayInfo.iso);
    const [error, setError] = useState("");
    const [monthPickerOpen, setMonthPickerOpen] = useState(false);
    const [yearCursor, setYearCursor] = useState(viewYear);
    const [settingsOpen, setSettingsOpen] = useState(false);
    const [settingsMenu, setSettingsMenu] = useState("main");
    const [settingsDraft, setSettingsDraft] = useState(() => createSettingsDraft(DEFAULT_CONFIG));
    const [eventPage, setEventPage] = useState(1);
    const [middleMode, setMiddleMode] = useState(false);
    const [deleteConfirmId, setDeleteConfirmId] = useState(null);
    const [middleModeTransitioning, setMiddleModeTransitioning] = useState(false);
    const middleModeToggleLockRef = useRef(false);
    const middleModeToggleTimerRef = useRef(null);
    const eventListRef = useRef(null);
    const layoutHintEnabled = !settingsOpen && !monthPickerOpen && !editDialogOpen && !middleModeTransitioning;
    const layoutMeasureKey = `${middleMode ? "middle" : "full"}|${config.cardDensity}|${settingsOpen ? 1 : 0}|${monthPickerOpen ? 1 : 0}|${editDialogOpen ? 1 : 0}|${config.events.length}`;
    const layoutHint = useWindowLayoutHint(eventListRef, layoutHintEnabled, layoutMeasureKey);
    const configuredEventsPerPage = Math.max(1, config.eventsPerPage);
    const configuredPageLabelCount = Math.max(3, ensureSettingsNumber(config.pageLabelCount, DEFAULT_CONFIG.pageLabelCount));
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
      eventPage
    });
    const {
      eventCountByDate,
      allEvents,
      totalEventPages,
      pagedEvents,
      visiblePageNumbers,
      isSimpleCard,
      selectedEventBorder,
      defaultEventBorder
    } = eventSection;
    const todayIsoValue = todayInfo.iso;
    const todayStamp = todayInfo.stamp;
    const calendarCells = useMemo(() => buildCalendarCells(viewYear, viewMonth), [viewYear, viewMonth]);
    const calendarRows = useMemo(() => chunkItems(calendarCells, 7), [calendarCells]);
    const textColor = config.textColor;
    const mutedText = hexToRgba(textColor, 0.72);
    const subtleText = mixHex(textColor, config.rightPanelBgColor, 0.48);
    const panelBorder = mixHex(textColor, config.rightPanelBgColor, 0.74);
    const panelInnerBg = mixHex(config.rightPanelBgColor, "#000000", 0.22);
    const softActionBg = mixHex(config.rightPanelBgColor, "#000000", 0.15);
    const inputBg = mixHex(config.rightPanelBgColor, "#000000", 0.36);
    const inputBorder = mixHex(textColor, config.rightPanelBgColor, 0.65);
    const calendarCellBg = mixHex(config.leftPanelBgColor, "#000000", 0.2);
    const calendarCellMutedBg = mixHex(config.leftPanelBgColor, "#000000", 0.32);
    const selectedCellBg = mixHex(config.daysColor, "#000000", 0.06);
    const accentButtonBg = mixHex(config.daysColor, "#000000", 0.1);
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
      unityTextAlign: "MiddleLeft"
    };
    const focusDate = useCallback((iso) => {
      const parsed = parseIsoDate(iso);
      if (!parsed)
        return;
      setSelectedDate(iso);
      setViewYear(parsed.getFullYear());
      setViewMonth(parsed.getMonth());
    }, []);
    const clearDraft = useCallback(() => {
      setDraftTitle("");
      setDeleteConfirmId(null);
    }, []);
    const saveEventForSelectedDate = useCallback(() => {
      const title = draftTitle.trim();
      if (!title) {
        setError("\u4E8B\u4EF6\u540D\u79F0\u4E0D\u80FD\u4E3A\u7A7A");
        return;
      }
      if (!parseIsoDate(selectedDate)) {
        setError("\u5F53\u524D\u9009\u4E2D\u65E5\u671F\u65E0\u6548");
        return;
      }
      const nextEvents = [
        ...config.events,
        {
          id: `evt-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 7)}`,
          title,
          targetDate: selectedDate,
          type: calculateDaysFromTodayStamp(selectedDate, todayStamp) < 0 ? "elapsed" : "countdown",
          pinned: false,
          createdAt: (/* @__PURE__ */ new Date()).toISOString()
        }
      ];
      persist({ ...config, events: sortEvents(nextEvents) });
      setDeleteConfirmId(null);
      clearDraft();
      setError("");
    }, [config, draftTitle, selectedDate, persist, clearDraft]);
    const startEdit = useCallback((item) => {
      setDeleteConfirmId(null);
      setEditDialogId(item.id);
      setEditDialogTitle(item.title);
      setEditDialogDate(item.targetDate);
      setEditDialogOpen(true);
      setError("");
    }, []);
    const closeEditDialog = useCallback(() => {
      setEditDialogOpen(false);
      setEditDialogId(null);
      setEditDialogTitle("");
      setEditDialogDate(selectedDate);
    }, [selectedDate]);
    const saveEditedEvent = () => {
      const id = editDialogId;
      if (!id)
        return;
      const title = editDialogTitle.trim();
      if (!title) {
        setError("\u4E8B\u4EF6\u540D\u79F0\u4E0D\u80FD\u4E3A\u7A7A");
        return;
      }
      const targetDate = normalizeText(editDialogDate, "");
      if (!parseIsoDate(targetDate)) {
        setError("\u76EE\u6807\u65E5\u671F\u65E0\u6548\uFF0C\u8BF7\u4F7F\u7528 YYYY-MM-DD");
        return;
      }
      const nextEvents = config.events.map(
        (item) => item.id === id ? { ...item, title, targetDate, type: resolveEventType(targetDate, getTodayInfo().stamp) } : item
      );
      persist({ ...config, events: sortEvents(nextEvents) });
      focusDate(targetDate);
      setDeleteConfirmId(null);
      closeEditDialog();
      setError("");
    };
    const removeEvent = useCallback((id) => {
      const nextEvents = config.events.filter((item) => item.id !== id);
      persist({ ...config, events: nextEvents });
      setDeleteConfirmId(null);
      if (editDialogId === id)
        closeEditDialog();
    }, [config, editDialogId, closeEditDialog, persist]);
    const togglePinned = useCallback((id) => {
      setDeleteConfirmId(null);
      const nextEvents = config.events.map(
        (item) => item.id === id ? { ...item, pinned: !item.pinned } : item
      );
      persist({ ...config, events: nextEvents });
    }, [config, persist]);
    const goToToday = useCallback(() => {
      focusDate(todayInfo.iso);
    }, [focusDate, todayInfo.iso]);
    const shiftView = useCallback((delta) => {
      const shifted = shiftMonth(viewYear, viewMonth, delta);
      setViewYear(shifted.year);
      setViewMonth(shifted.month);
    }, [viewYear, viewMonth]);
    const openMonthPicker = useCallback(() => {
      setEditDialogOpen(false);
      setYearCursor(viewYear);
      setMonthPickerOpen(true);
    }, [viewYear]);
    const openSettings = () => {
      setEditDialogOpen(false);
      setSettingsMenu("main");
      setSettingsDraft(createSettingsDraft(config));
      setSettingsOpen(true);
    };
    const scheduleMiddleModeUnlock = useCallback(() => {
      if (middleModeToggleTimerRef.current)
        clearTimeout(middleModeToggleTimerRef.current);
      middleModeToggleTimerRef.current = setTimeout(() => {
        middleModeToggleLockRef.current = false;
        setMiddleModeTransitioning(false);
        middleModeToggleTimerRef.current = null;
      }, 180);
    }, []);
    const enterMiddleMode = useCallback(() => {
      if (middleModeToggleLockRef.current || middleMode)
        return;
      middleModeToggleLockRef.current = true;
      setMiddleModeTransitioning(true);
      setMonthPickerOpen(false);
      setSettingsOpen(false);
      setEditDialogOpen(false);
      setMiddleMode(true);
      scheduleMiddleModeUnlock();
    }, [middleMode, scheduleMiddleModeUnlock]);
    const exitMiddleMode = useCallback(() => {
      if (middleModeToggleLockRef.current || !middleMode)
        return;
      middleModeToggleLockRef.current = true;
      setMiddleModeTransitioning(true);
      setMiddleMode(false);
      scheduleMiddleModeUnlock();
    }, [middleMode, scheduleMiddleModeUnlock]);
    const applyThemePreset = (presetId) => {
      const preset = THEME_PRESETS.find((item) => item.id === presetId);
      if (!preset)
        return;
      setSettingsDraft((prev) => ({
        ...prev,
        backgroundColor: preset.config.backgroundColor,
        leftPanelBgColor: preset.config.leftPanelBgColor,
        rightPanelBgColor: preset.config.rightPanelBgColor,
        textColor: preset.config.textColor,
        titleColor: preset.config.titleColor,
        daysColor: preset.config.daysColor
      }));
    };
    const colorDraftRows = [
      { label: "\u6574\u4F53\u80CC\u666F", key: "backgroundColor" },
      { label: "\u5DE6\u4FA7\u9762\u677F\u80CC\u666F", key: "leftPanelBgColor" },
      { label: "\u53F3\u4FA7\u9762\u677F\u80CC\u666F", key: "rightPanelBgColor" },
      { label: "\u4E3B\u6587\u5B57\u989C\u8272", key: "textColor" },
      { label: "\u6807\u9898\u6587\u5B57\u989C\u8272", key: "titleColor" },
      { label: "\u5929\u6570\u9AD8\u4EAE\u8272", key: "daysColor" }
    ];
    const paginationNumberRows = [
      { label: "\u6BCF\u9875\u4E8B\u4EF6\u6570", key: "eventsPerPage" },
      { label: "\u4E00\u9875\u6807\u7B7E\u6570\uFF08\u9875\u7801\u6309\u94AE\uFF09", key: "pageLabelCount" }
    ];
    const updateSettingsDraftNumber = (key, rawValue) => {
      const nextText = sanitizeSettingsNumberText(rawValue);
      setSettingsDraft((prev) => ({ ...prev, [key]: nextText }));
    };
    const renderSettingsNumberRow = (label, key) => /* @__PURE__ */ h("div", { key: `settings-number-${key}`, style: { marginBottom: 8 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 4 } }, label), /* @__PURE__ */ h(
      "textfield",
      {
        value: ensureSettingsNumberText(settingsDraft[key], key === "eventsPerPage" ? DEFAULT_CONFIG.eventsPerPage : DEFAULT_CONFIG.pageLabelCount),
        multiline: false,
        onValueChanged: (e) => updateSettingsDraftNumber(key, e?.newValue),
        style: settingsNumberInputStyle
      }
    ));
    const applySettings = () => {
      const nextEventsFilePath = normalizeEventsFilePath(settingsDraft.eventsFilePath, config.eventsFilePath);
      const backgroundColor = normalizeColor(settingsDraft.backgroundColor, config.backgroundColor);
      const leftPanelBgColor = normalizeColor(settingsDraft.leftPanelBgColor, config.leftPanelBgColor);
      const rightPanelBgColor = normalizeColor(settingsDraft.rightPanelBgColor, config.rightPanelBgColor);
      const textColor2 = normalizeColor(settingsDraft.textColor, config.textColor);
      const titleColor = normalizeColor(settingsDraft.titleColor, config.titleColor);
      const daysColor = normalizeColor(settingsDraft.daysColor, config.daysColor);
      const cardDensity = normalizeCardDensity(settingsDraft.cardDensity, config.cardDensity);
      const nextConfig = {
        ...config,
        titleColor,
        daysColor,
        cardDensity,
        backgroundColor,
        leftPanelBgColor,
        rightPanelBgColor,
        textColor: textColor2,
        eventsPerPage: parseSettingsNumberDraft(settingsDraft.eventsPerPage, config.eventsPerPage, 1, 20),
        pageLabelCount: parseSettingsNumberDraft(settingsDraft.pageLabelCount, config.pageLabelCount, 3, 9),
        eventsFilePath: nextEventsFilePath
      };
      persist(nextConfig);
      setSettingsOpen(false);
    };
    useEffect(() => {
      setEventPage((page) => Math.min(page, totalEventPages));
    }, [totalEventPages]);
    useEffect(() => {
      return () => {
        if (middleModeToggleTimerRef.current) {
          clearTimeout(middleModeToggleTimerRef.current);
          middleModeToggleTimerRef.current = null;
        }
        middleModeToggleLockRef.current = false;
      };
    }, []);
    useEffect(() => {
      setDeleteConfirmId(null);
    }, [eventPage]);
    const overlayOpen = monthPickerOpen || editDialogOpen || settingsOpen;
    return /* @__PURE__ */ h(
      "div",
      {
        style: {
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
          position: "Relative"
        }
      },
      middleMode ? /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", minHeight: 0, alignItems: "FlexStart" } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 6 } }, /* @__PURE__ */ h(CountdownActionButton, { text: "\u5C55\u5F00", onClick: exitMiddleMode, disabled: middleModeTransitioning, color: textColor, bg: accentButtonBg, compact: true })), /* @__PURE__ */ h(
        "div",
        {
          style: {
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
            overflow: "Hidden"
          }
        },
        /* @__PURE__ */ h(EventListSection, { showCountLabel: false, allEventsLength: allEvents.length, mutedText, panelInnerBg, panelBorder, pagedEvents, eventPage, totalEventPages, visiblePageNumbers, textColor, softActionBg, accentButtonBg, eventListRef, setEventPage, todayStamp, isSimpleCard, selectedDate, editDialogId, deleteConfirmId, subtleText, titleColor: config.titleColor, daysColor: config.daysColor, rightPanelBgColor: config.rightPanelBgColor, selectedEventBorder, defaultEventBorder, focusDate, togglePinned, startEdit, setDeleteConfirmId, removeEvent })
      )) : /* @__PURE__ */ h(Fragment, null, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 6 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h(CountdownActionButton, { text: "\u6536\u8D77", onClick: enterMiddleMode, disabled: middleModeTransitioning, color: textColor, bg: softActionBg, compact: true }), /* @__PURE__ */ h("div", { style: { width: 6 } }), /* @__PURE__ */ h("div", { style: { fontSize: 12, color: textColor, unityFontStyleAndWeight: "Bold" } }, "\u65E5\u5386\u8BA1\u65F6")), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h(CountdownActionButton, { text: "\u8BBE\u7F6E", onClick: openSettings, bg: accentButtonBg, color: textColor, compact: true }))), error ? /* @__PURE__ */ h("div", { style: { fontSize: 10, color: "#fca5a5", marginBottom: 6 } }, error) : null, !overlayOpen ? /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", minHeight: 0 } }, /* @__PURE__ */ h(
        "div",
        {
          style: {
            backgroundColor: config.leftPanelBgColor,
            borderRadius: 8,
            borderWidth: 1,
            borderColor: panelBorder,
            paddingLeft: 7,
            paddingRight: 7,
            paddingTop: 7,
            paddingBottom: 7,
            marginBottom: 6
          }
        },
        /* @__PURE__ */ h("div", { style: { width: "100%", maxWidth: 360, marginLeft: "Auto", marginRight: "Auto" } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 6, minWidth: 0, width: "100%" } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h(CountdownActionButton, { text: "\u4ECA\u5929", onClick: goToToday, color: textColor, bg: softActionBg, compact: true }), /* @__PURE__ */ h("div", { style: { width: 3 } }), /* @__PURE__ */ h(CountdownActionButton, { text: "<", onClick: () => shiftView(-1), color: textColor, bg: softActionBg, compact: true })), /* @__PURE__ */ h("div", { style: { flexGrow: 1, minWidth: 0, paddingLeft: 4, paddingRight: 4, display: "Flex", justifyContent: "Center" } }, /* @__PURE__ */ h(
          "div",
          {
            onPointerDown: openMonthPicker,
            style: {
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
              unityTextAlign: "MiddleCenter"
            }
          },
          monthTitle(viewYear, viewMonth)
        )), /* @__PURE__ */ h(CountdownActionButton, { text: ">", onClick: () => shiftView(1), color: textColor, bg: softActionBg, compact: true })), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 3, width: "100%" } }, /* @__PURE__ */ h(WeekLabelsRow, { subtleText })), /* @__PURE__ */ h(CalendarGridRows, { calendarRows, selectedDate, todayIsoValue, eventCountByDate, focusDate, selectedCellBg, calendarCellBg, calendarCellMutedBg, daysColor: config.daysColor, textColor, subtleText }))
      ), /* @__PURE__ */ h(
        "div",
        {
          style: {
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
            overflow: "Hidden"
          }
        },
        /* @__PURE__ */ h("div", { style: { fontSize: 11, color: textColor, marginBottom: 3, unityFontStyleAndWeight: "Bold" } }, "\u9009\u4E2D\u65E5\u671F: ", selectedDate),
        /* @__PURE__ */ h("div", { style: { backgroundColor: panelInnerBg, borderRadius: 8, borderWidth: 1, borderColor: panelBorder, padding: 7, marginBottom: 6 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 4 } }, "\u5FEB\u901F\u6DFB\u52A0\u4E8B\u4EF6"), /* @__PURE__ */ h(
          "textfield",
          {
            value: draftTitle,
            multiline: false,
            onValueChanged: (e) => setDraftTitle(e?.newValue ?? ""),
            style: {
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
              marginBottom: 5
            }
          }
        ), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row" } }, /* @__PURE__ */ h(CountdownActionButton, { text: "\u6DFB\u52A0\u5230\u9009\u4E2D\u65E5\u671F", onClick: saveEventForSelectedDate, color: textColor, bg: accentButtonBg, compact: true }))),
        /* @__PURE__ */ h(EventListSection, { showCountLabel: true, allEventsLength: allEvents.length, mutedText, panelInnerBg, panelBorder, pagedEvents, eventPage, totalEventPages, visiblePageNumbers, textColor, softActionBg, accentButtonBg, eventListRef, setEventPage, todayStamp, isSimpleCard, selectedDate, editDialogId, deleteConfirmId, subtleText, titleColor: config.titleColor, daysColor: config.daysColor, rightPanelBgColor: config.rightPanelBgColor, selectedEventBorder, defaultEventBorder, focusDate, togglePinned, startEdit, setDeleteConfirmId, removeEvent })
      )) : null),
      monthPickerOpen ? /* @__PURE__ */ h(
        "div",
        {
          style: {
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
            paddingRight: 20
          }
        },
        /* @__PURE__ */ h(
          "div",
          {
            style: {
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
              borderColor: panelBorder
            }
          },
          /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 8 } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: textColor, unityFontStyleAndWeight: "Bold" } }, "\u9009\u62E9\u5E74\u6708"), /* @__PURE__ */ h(CountdownActionButton, { text: "\u5173\u95ED", onClick: () => setMonthPickerOpen(false), color: textColor, bg: softActionBg, compact: true })),
          /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "Center", alignItems: "Center", marginBottom: 8 } }, /* @__PURE__ */ h(CountdownActionButton, { text: "-10", onClick: () => setYearCursor((y) => y - 10), color: textColor, bg: softActionBg, compact: true }), /* @__PURE__ */ h("div", { style: { width: 3 } }), /* @__PURE__ */ h(CountdownActionButton, { text: "-1", onClick: () => setYearCursor((y) => y - 1), color: textColor, bg: softActionBg, compact: true }), /* @__PURE__ */ h("div", { style: { width: 8 } }), /* @__PURE__ */ h("div", { style: { fontSize: 12, color: textColor, width: 70, unityTextAlign: "MiddleCenter" } }, yearCursor, "\u5E74"), /* @__PURE__ */ h("div", { style: { width: 8 } }), /* @__PURE__ */ h(CountdownActionButton, { text: "+1", onClick: () => setYearCursor((y) => y + 1), color: textColor, bg: softActionBg, compact: true }), /* @__PURE__ */ h("div", { style: { width: 3 } }), /* @__PURE__ */ h(CountdownActionButton, { text: "+10", onClick: () => setYearCursor((y) => y + 10), color: textColor, bg: softActionBg, compact: true })),
          /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "Center", marginBottom: 8 } }, Array.from({ length: 5 }, (_, i) => yearCursor - 2 + i).map((year, i) => /* @__PURE__ */ h("div", { key: `year-select-${year}`, style: { marginRight: i === 4 ? 0 : 4 } }, /* @__PURE__ */ h(
            CountdownActionButton,
            {
              text: `${year}`,
              onClick: () => setYearCursor(year),
              color: textColor,
              bg: year === yearCursor ? accentButtonBg : softActionBg,
              compact: true
            }
          )))),
          /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", flexWrap: "Wrap", justifyContent: "Center" } }, MONTH_LABELS.map((label, monthIndex) => /* @__PURE__ */ h("div", { key: `month-${monthIndex}`, style: { width: 76, marginRight: 4, marginBottom: 4 } }, /* @__PURE__ */ h(
            CountdownActionButton,
            {
              text: label,
              onClick: () => {
                setViewYear(yearCursor);
                setViewMonth(monthIndex);
                setMonthPickerOpen(false);
              },
              color: textColor,
              bg: yearCursor === viewYear && monthIndex === viewMonth ? accentButtonBg : softActionBg,
              compact: true
            }
          ))))
        )
      ) : null,
      editDialogOpen ? /* @__PURE__ */ h(
        "div",
        {
          style: {
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
            paddingRight: 20
          }
        },
        /* @__PURE__ */ h(
          "div",
          {
            style: {
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
              borderColor: panelBorder
            }
          },
          /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 8 } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: textColor, unityFontStyleAndWeight: "Bold" } }, "\u7F16\u8F91\u4E8B\u4EF6"), /* @__PURE__ */ h(CountdownActionButton, { text: "\u5173\u95ED", onClick: closeEditDialog, color: textColor, bg: softActionBg, compact: true })),
          /* @__PURE__ */ h("div", { style: { marginBottom: 8 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 4 } }, "\u4E8B\u4EF6\u540D\u79F0"), /* @__PURE__ */ h(
            "textfield",
            {
              value: editDialogTitle,
              multiline: false,
              onValueChanged: (e) => setEditDialogTitle(e?.newValue ?? ""),
              style: {
                width: "100%",
                height: 24,
                fontSize: 10,
                backgroundColor: inputBg,
                borderWidth: 1,
                borderColor: inputBorder,
                color: textColor,
                paddingLeft: 8,
                paddingRight: 8,
                unityTextAlign: "MiddleLeft"
              }
            }
          )),
          /* @__PURE__ */ h("div", { style: { marginBottom: 8 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 4 } }, "\u76EE\u6807\u65E5\u671F\uFF08YYYY-MM-DD\uFF09"), /* @__PURE__ */ h(
            "textfield",
            {
              value: editDialogDate,
              multiline: false,
              onValueChanged: (e) => setEditDialogDate(e?.newValue ?? ""),
              style: {
                width: "100%",
                height: 24,
                fontSize: 10,
                backgroundColor: inputBg,
                borderWidth: 1,
                borderColor: inputBorder,
                color: textColor,
                paddingLeft: 8,
                paddingRight: 8,
                unityTextAlign: "MiddleLeft"
              }
            }
          )),
          /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "FlexEnd", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h(CountdownActionButton, { text: "\u4FDD\u5B58", onClick: saveEditedEvent, color: textColor, bg: accentButtonBg, compact: true }), /* @__PURE__ */ h("div", { style: { width: 4 } }), /* @__PURE__ */ h(CountdownActionButton, { text: "\u53D6\u6D88", onClick: closeEditDialog, color: textColor, bg: softActionBg, compact: true })))
        )
      ) : null,
      settingsOpen ? /* @__PURE__ */ h(
        "div",
        {
          style: {
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
            paddingRight: 20
          }
        },
        /* @__PURE__ */ h(
          "div",
          {
            style: {
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
              borderColor: panelBorder
            }
          },
          /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 8 } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: textColor, unityFontStyleAndWeight: "Bold" } }, settingsMenu === "main" ? "\u8BBE\u7F6E" : "\u8BBE\u7F6E / \u81EA\u5B9A\u4E49\u989C\u8272"), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, settingsMenu === "colors" ? /* @__PURE__ */ h(Fragment, null, /* @__PURE__ */ h(CountdownActionButton, { text: "\u8FD4\u56DE", onClick: () => setSettingsMenu("main"), color: textColor, bg: softActionBg, compact: true }), /* @__PURE__ */ h("div", { style: { width: 4 } })) : null, /* @__PURE__ */ h(CountdownActionButton, { text: "\u5173\u95ED", onClick: () => setSettingsOpen(false), color: textColor, bg: softActionBg, compact: true }))),
          settingsMenu === "main" ? /* @__PURE__ */ h("div", { key: "settings-main-panel", style: { display: "Flex", flexDirection: "Column" } }, /* @__PURE__ */ h("div", { style: { marginBottom: 10 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 4 } }, "\u4E8B\u4EF6 JSON \u8DEF\u5F84"), /* @__PURE__ */ h(
            "textfield",
            {
              value: settingsDraft.eventsFilePath,
              multiline: false,
              onValueChanged: (e) => setSettingsDraft((prev) => ({ ...prev, eventsFilePath: e?.newValue ?? "" })),
              style: settingsNumberInputStyle
            }
          ), /* @__PURE__ */ h("div", { style: { fontSize: 9, color: mutedText, marginTop: 3 } }, "\u4F8B\u5982\uFF1Awindow-states/countdown-days-events.json")), /* @__PURE__ */ h("div", { key: "settings-main-color-entry", style: { marginBottom: 10, display: "Flex", flexDirection: "Column", alignItems: "Stretch" } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 4 } }, "\u989C\u8272"), /* @__PURE__ */ h("div", { style: { width: "100%" } }, /* @__PURE__ */ h(CountdownActionButton, { text: "\u81EA\u5B9A\u4E49\u989C\u8272", onClick: () => setSettingsMenu("colors"), color: textColor, bg: accentButtonBg, compact: true, fullWidth: true }))), /* @__PURE__ */ h("div", { style: { marginBottom: 10 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 4 } }, "\u5361\u7247\u4FE1\u606F\u5BC6\u5EA6"), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h(
            CountdownActionButton,
            {
              text: "\u7B80\u6D01",
              onClick: () => setSettingsDraft((prev) => ({ ...prev, cardDensity: "simple" })),
              color: textColor,
              bg: settingsDraft.cardDensity === "simple" ? accentButtonBg : softActionBg,
              compact: true
            }
          ), /* @__PURE__ */ h("div", { style: { width: 6 } }), /* @__PURE__ */ h(
            CountdownActionButton,
            {
              text: "\u6807\u51C6",
              onClick: () => setSettingsDraft((prev) => ({ ...prev, cardDensity: "standard" })),
              color: textColor,
              bg: settingsDraft.cardDensity === "standard" ? accentButtonBg : softActionBg,
              compact: true
            }
          ))), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 6 } }, "\u5206\u9875\u8BBE\u7F6E"), paginationNumberRows.map((row) => renderSettingsNumberRow(row.label, row.key))) : /* @__PURE__ */ h("div", { key: "settings-colors-panel", style: { display: "Flex", flexDirection: "Column" } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 4 } }, "\u9ED8\u8BA4\u4E3B\u9898"), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", flexWrap: "Wrap", marginBottom: 8 } }, THEME_PRESETS.map((preset) => /* @__PURE__ */ h("div", { key: preset.id, style: { marginRight: 4, marginBottom: 4 } }, /* @__PURE__ */ h(
            CountdownActionButton,
            {
              text: preset.name,
              onClick: () => applyThemePreset(preset.id),
              color: textColor,
              bg: softActionBg,
              compact: true
            }
          )))), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: textColor, marginBottom: 6 } }, "\u81EA\u5B9A\u4E49\u989C\u8272"), colorDraftRows.map((row) => /* @__PURE__ */ h(
            ColorEditorRow,
            {
              key: `color-row-${row.key}`,
              label: row.label,
              value: settingsDraft[row.key],
              onChange: (value) => setSettingsDraft((prev) => ({ ...prev, [row.key]: String(value ?? "") })),
              textColor,
              inputBg,
              inputBorder,
              fallbackColor: DEFAULT_CONFIG[row.key]
            }
          ))),
          settingsMenu === "main" ? /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "FlexEnd" } }, /* @__PURE__ */ h(CountdownActionButton, { text: "\u5E94\u7528\u8BBE\u7F6E", onClick: applySettings, color: textColor, bg: accentButtonBg })) : /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "FlexEnd" } }, /* @__PURE__ */ h(CountdownActionButton, { text: "\u5E94\u7528\u989C\u8272", onClick: applySettings, color: textColor, bg: accentButtonBg, compact: true }))
        )
      ) : null
    );
  };
  var CountdownCompact = () => {
    const [config, persist] = useRuntimeConfig();
    const todayInfo = useTodayInfo();
    const compactEvents = useMemo(() => {
      return sortEventsPinnedFirst(config.events).slice(0, config.compactCount);
    }, [config.events, config.compactCount]);
    const compactPanel = mixHex(config.rightPanelBgColor, "#000000", 0.18);
    const compactBorder = mixHex(config.textColor, config.rightPanelBgColor, 0.68);
    const compactButtonBg = mixHex(config.rightPanelBgColor, "#000000", 0.18);
    const compactButtonActiveBg = mixHex(config.daysColor, "#000000", 0.1);
    const autoCols = config.compactCount >= 4 ? 2 : 1;
    const compactRows = useMemo(() => {
      if (compactEvents.length === 0)
        return [];
      if (autoCols <= 1)
        return compactEvents.map((eventItem) => [eventItem]);
      const rows = [];
      for (let i = 0; i < compactEvents.length; i += autoCols) {
        rows.push(compactEvents.slice(i, i + autoCols));
      }
      return rows;
    }, [compactEvents, autoCols]);
    const compactFont = config.compactCount === 4 ? 13 : config.compactCount === 1 ? 17 : 16;
    const titleFont = config.compactCount === 1 ? 22 : compactFont;
    const dayFont = config.compactCount === 1 ? 18 : compactFont;
    const showCompactDate = config.compactCount === 1 && compactEvents.length === 1;
    return /* @__PURE__ */ h(
      "div",
      {
        style: {
          flexGrow: 1,
          display: "Flex",
          flexDirection: "Column",
          backgroundColor: config.rightPanelBgColor,
          paddingLeft: 6,
          paddingRight: 6,
          paddingTop: 30,
          paddingBottom: 8,
          overflow: "Hidden",
          position: "Relative"
        }
      },
      /* @__PURE__ */ h(
        "div",
        {
          style: {
            position: "Absolute",
            top: 6,
            right: 6,
            display: "Flex",
            flexDirection: "Row",
            alignItems: "Center",
            zIndex: 2,
            pointerEvents: "Auto",
            gap: 3
          }
        },
        [1, 2, 4].map((countValue) => /* @__PURE__ */ h("div", { key: `compact-top-count-${countValue}`, style: { width: 28, flexShrink: 0 } }, /* @__PURE__ */ h(
          CountdownActionButton,
          {
            text: `${countValue}`,
            onClick: () => persist({ ...config, compactCount: countValue }),
            color: config.textColor,
            bg: config.compactCount === countValue ? compactButtonActiveBg : compactButtonBg,
            compact: true,
            fullWidth: true
          }
        )))
      ),
      /* @__PURE__ */ h(
        "div",
        {
          style: {
            flexGrow: 1,
            display: "Flex",
            flexDirection: "Column",
            minHeight: 0,
            overflow: "Hidden"
          }
        },
        compactEvents.length === 0 ? /* @__PURE__ */ h(
          "div",
          {
            style: {
              flexGrow: 1,
              fontSize: 18,
              color: hexToRgba(config.textColor, 0.72),
              unityFontStyleAndWeight: "Bold",
              unityTextAlign: "MiddleCenter"
            }
          },
          "\u6682\u65E0\u4E8B\u4EF6"
        ) : /* @__PURE__ */ h(
          "div",
          {
            style: {
              flexGrow: 1,
              display: "Flex",
              flexDirection: "Column",
              minHeight: 0
            }
          },
          compactRows.map((row, rowIndex) => /* @__PURE__ */ h(
            "div",
            {
              key: `compact-row-${rowIndex}`,
              style: {
                display: "Flex",
                flexDirection: "Row",
                flexGrow: 1,
                minHeight: 0,
                marginBottom: rowIndex < compactRows.length - 1 ? COMPACT_CARD_GAP : 0
              }
            },
            row.map((item, colIndex) => /* @__PURE__ */ h(
              "div",
              {
                key: item.id,
                style: {
                  flexGrow: 1,
                  flexShrink: 1,
                  flexBasis: autoCols > 1 ? 0 : "100%",
                  width: autoCols > 1 ? void 0 : "100%",
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
                  alignItems: "Center"
                }
              },
              /* @__PURE__ */ h(
                "div",
                {
                  style: {
                    width: "100%",
                    display: "Flex",
                    flexDirection: "Column",
                    alignItems: "Center",
                    justifyContent: "Center"
                  }
                },
                /* @__PURE__ */ h(
                  "div",
                  {
                    style: {
                      fontSize: getAdaptiveCompactTitleFontSize(item.title, titleFont, config.compactCount, showCompactDate),
                      color: config.titleColor,
                      unityFontStyleAndWeight: "Bold",
                      unityTextAlign: "MiddleCenter",
                      width: "100%",
                      whiteSpace: "NoWrap",
                      overflow: "Hidden",
                      textOverflow: "Ellipsis",
                      marginBottom: 2
                    }
                  },
                  item.title
                ),
                showCompactDate ? /* @__PURE__ */ h(
                  "div",
                  {
                    style: {
                      fontSize: compactFont,
                      color: hexToRgba(config.textColor, 0.72),
                      unityTextAlign: "MiddleCenter",
                      marginBottom: 2
                    }
                  },
                  item.targetDate
                ) : null
              ),
              /* @__PURE__ */ h(
                "div",
                {
                  style: {
                    fontSize: dayFont,
                    color: config.daysColor,
                    unityFontStyleAndWeight: "Bold",
                    unityTextAlign: "MiddleCenter"
                  }
                },
                formatDaysText(calculateDaysFromTodayStamp(item.targetDate, todayInfo.stamp))
              )
            )),
            autoCols > 1 && row.length < autoCols ? Array.from({ length: autoCols - row.length }, (_, placeholderIndex) => /* @__PURE__ */ h(
              "div",
              {
                key: `compact-placeholder-${rowIndex}-${placeholderIndex}`,
                style: {
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
                  pointerEvents: "None"
                }
              }
            )) : null
          ))
        )
      )
    );
  };
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
      component: CountdownCompact
    },
    launcher: {
      text: "\uF073",
      background: "#0ea5e9"
    },
    component: CountdownPanel
  });
})();
//# sourceMappingURL=app.js.map
