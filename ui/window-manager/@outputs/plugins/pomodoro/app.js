(() => {
  // plugin-shims/preact-module.js
  var __p = globalThis.__preact;
  var h = __p.h;
  var Fragment = __p.Fragment;
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

  // plugins/pomodoro/index.tsx
  var BG = "#0b1020";
  var CARD = "#111827";
  var PANEL = "#1e293b";
  var TEXT = "#e5e7eb";
  var DIM = "#94a3b8";
  var ACCENT = "#7dd3fc";
  var OK = "#34d399";
  var WARN = "#f59e0b";
  var SETTINGS_FILE = "window-manager/pomodoro-settings.json";
  var HIDE_TARGET_PATHS = [
    "Paremt/PCPlatform/Canvas/UI/UI_FacilityPomodoro",
    "Paremt/PCPlatform/Canvas/UI/UI_FacilityPlayerLevel"
  ];
  var OFFSET_TARGET_PATH = "Paremt/PCPlatform/Canvas/UI/MostFrontArea/TopIcons";
  var OFFSET_Y_WHEN_HIDDEN = 250;
  var DEFAULT_SETTINGS = {
    hideDefaultPomodoroUI: true,
    showSeconds: false
  };
  var offsetTargetOrigin = null;
  var pad2 = (n) => String(n).padStart(2, "0");
  var hhmmss = (s) => `${pad2(Math.floor(Math.max(0, s) / 3600))}:${pad2(Math.floor(Math.max(0, s) % 3600 / 60))}:${pad2(Math.floor(Math.max(0, s) % 60))}`;
  var json = (v, fb) => {
    try {
      return typeof v === "string" ? JSON.parse(v) : v;
    } catch {
      return fb;
    }
  };
  var clamp = (v, min, max) => Math.max(min, Math.min(max, v));
  var loadPomodoroSettings = () => {
    try {
      if (!chill?.io?.exists?.(SETTINGS_FILE))
        return DEFAULT_SETTINGS;
      const txt = chill?.io?.readText?.(SETTINGS_FILE);
      const parsed = json(txt, null);
      if (!parsed || typeof parsed !== "object")
        return DEFAULT_SETTINGS;
      return {
        hideDefaultPomodoroUI: !!parsed.hideDefaultPomodoroUI,
        showSeconds: !!parsed.showSeconds
      };
    } catch {
      return DEFAULT_SETTINGS;
    }
  };
  var savePomodoroSettings = (settings) => {
    try {
      chill?.io?.writeText?.(SETTINGS_FILE, JSON.stringify(settings, null, 2));
    } catch {
    }
  };
  var setNodeActive = (path, active) => {
    if (!path || !path.trim())
      return;
    chill?.ui?.setActive?.(path, active);
  };
  var applyOffsetForTarget = (hideEnabled) => {
    if (!OFFSET_TARGET_PATH || !OFFSET_TARGET_PATH.trim())
      return;
    if (!hideEnabled) {
      if (offsetTargetOrigin) {
        chill?.ui?.setPosition?.(OFFSET_TARGET_PATH, offsetTargetOrigin.x, offsetTargetOrigin.y);
      }
      return;
    }
    if (!offsetTargetOrigin) {
      const rect = json(chill?.ui?.getRect?.(OFFSET_TARGET_PATH), null);
      if (!rect)
        return;
      const x = Number(rect.x);
      const y = Number(rect.y);
      if (Number.isNaN(x) || Number.isNaN(y))
        return;
      offsetTargetOrigin = { x, y };
    }
    chill?.ui?.setPosition?.(OFFSET_TARGET_PATH, offsetTargetOrigin.x, offsetTargetOrigin.y + OFFSET_Y_WHEN_HIDDEN);
  };
  var syncDefaultPomodoroUI = (hideEnabled) => {
    const active = !hideEnabled;
    for (const path of HIDE_TARGET_PATHS) {
      setNodeActive(path, active);
    }
    applyOffsetForTarget(hideEnabled);
  };
  var buildFallbackClock = () => {
    const d = /* @__PURE__ */ new Date();
    const date = `${d.getFullYear()}/${pad2(d.getMonth() + 1)}/${pad2(d.getDate())}(${d.toLocaleDateString(void 0, { weekday: "short" })})`;
    const time = `${pad2(d.getHours())}:${pad2(d.getMinutes())}`;
    return { available: false, date, time, amPm: "", dateTime: `${date} ${time}` };
  };
  var readClock = (prev) => {
    const fallback = prev || buildFallbackClock();
    return json(chill?.game?.getGameClock?.(), fallback);
  };
  var phaseDefaultsFromState = (state) => {
    const isResting = !!state?.isResting;
    const mins = isResting ? Number(state?.breakMinutes || 5) : Number(state?.workMinutes || 25);
    const total = Math.max(1, Math.round(mins * 60));
    return { total, remain: total };
  };
  var nextControlState = (state) => {
    if (state?.isPaused)
      return "paused";
    if (state?.isRunning)
      return "running";
    const started = state?.type === "Work" || state?.type === "Break" || state?.isWorking || state?.isResting;
    if (!started)
      return "idle";
    return "idle";
  };
  var isProgressPayload = (payload) => payload && payload.remainingSeconds !== void 0 && payload.totalSeconds !== void 0;
  var hasPomodoroState = (payload) => payload && payload.workMinutes !== void 0 && payload.breakMinutes !== void 0;
  var hasPlayerProgress = (payload) => payload && payload.level !== void 0 && payload.nextLevelExp !== void 0;
  var formatClockWithSeconds = (clock) => {
    const unixMs = Number(clock?.unixMs || 0);
    const d = unixMs > 0 ? new Date(unixMs) : /* @__PURE__ */ new Date();
    if (clock?.amPm) {
      const hh = d.getHours() % 12 || 12;
      return `${pad2(hh)}:${pad2(d.getMinutes())}:${pad2(d.getSeconds())} ${clock.amPm}`;
    }
    return `${pad2(d.getHours())}:${pad2(d.getMinutes())}:${pad2(d.getSeconds())}`;
  };
  var formatClockDisplay = (clock, showSeconds) => {
    if (showSeconds)
      return formatClockWithSeconds(clock);
    const unixMs = Number(clock?.unixMs || 0);
    if (unixMs > 0) {
      const d = new Date(unixMs);
      if (clock?.amPm) {
        const hh = d.getHours() % 12 || 12;
        return `${pad2(hh)}:${pad2(d.getMinutes())}${clock?.amPm ? ` ${clock.amPm}` : ""}`;
      }
      return `${pad2(d.getHours())}:${pad2(d.getMinutes())}`;
    }
    const fallback = `${clock?.time || "--:--"}`;
    return `${fallback}${clock?.amPm ? ` ${clock.amPm}` : ""}`;
  };
  var SettingsCheckbox = (props) => /* @__PURE__ */ h(
    "div",
    {
      style: {
        display: "Flex",
        flexDirection: "Row",
        alignItems: "Center",
        backgroundColor: CARD,
        borderRadius: 8,
        paddingLeft: 10,
        paddingRight: 10,
        paddingTop: 10,
        paddingBottom: 10,
        marginBottom: 8
      },
      onPointerDown: props.onToggle
    },
    /* @__PURE__ */ h("div", { style: {
      width: 20,
      height: 20,
      borderWidth: 2,
      borderColor: props.checked ? ACCENT : DIM,
      borderRadius: 4,
      marginRight: 10,
      justifyContent: "Center",
      alignItems: "Center",
      display: "Flex",
      backgroundColor: props.checked ? "rgba(125,211,252,0.16)" : "transparent",
      flexShrink: 0
    } }, props.checked && /* @__PURE__ */ h("div", { style: { fontSize: 12, color: TEXT } }, "\u2713")),
    /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Column", flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: TEXT } }, props.label), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM, display: props.hint ? "Flex" : "None", marginTop: 2 } }, props.hint || ""))
  );
  var ParamCard = (props) => {
    const [editing, setEditing] = useState(false);
    const [draft, setDraft] = useState("");
    const submit = () => {
      const parsed = Number(draft);
      if (Number.isNaN(parsed)) {
        setEditing(false);
        return;
      }
      props.onCommit(Math.round(parsed));
      setEditing(false);
    };
    const startEdit = () => {
      setDraft(`${props.value}`);
      setEditing(true);
    };
    const chip = (t, on, bg = "rgba(125,211,252,0.15)", color = ACCENT) => /* @__PURE__ */ h("div", { onPointerDown: on, style: { fontSize: 12, color, backgroundColor: bg, borderRadius: 8, paddingLeft: 10, paddingRight: 10, paddingTop: 6, paddingBottom: 6 } }, t);
    return /* @__PURE__ */ h("div", { style: { backgroundColor: PANEL, borderRadius: 10, padding: 8, width: 98 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM } }, props.title), editing ? /* @__PURE__ */ h(
      "textfield",
      {
        value: draft,
        onValueChanged: (e) => setDraft(e?.newValue ?? e?.target?.value ?? draft),
        onKeyDown: (e) => {
          if (e?.keyCode === 13)
            submit();
        },
        style: { fontSize: 18, color: TEXT, backgroundColor: BG, borderRadius: 6, paddingLeft: 6, paddingRight: 6, paddingTop: 2, paddingBottom: 2, marginBottom: 6 }
      }
    ) : /* @__PURE__ */ h("div", { style: { fontSize: 18, color: TEXT, marginBottom: 6 }, onPointerDown: startEdit }, `${props.value}${props.unit || ""}`), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" } }, editing ? chip("OK", submit, "rgba(52,211,153,0.16)", OK) : /* @__PURE__ */ h("div", { style: { display: "flex", flexDirection: "row" } }, chip("-", props.onMinus), /* @__PURE__ */ h("div", { style: { width: 8 } }), chip("+", props.onPlus))));
  };
  var PomodoroPanel = () => {
    const [clock, setClock] = useState(buildFallbackClock());
    const [pm, setPm] = useState({ available: false, type: "Unknown", loopCurrent: 0, loopTotal: 0, workMinutes: 25, breakMinutes: 5, isRunning: false, isWorking: false, isResting: false });
    const [pg, setPg] = useState({ available: false, level: 0, exp: 0, nextLevelExp: 1, totalWorkSeconds: 0 });
    const [remain, setRemain] = useState(0);
    const [total, setTotal] = useState(1);
    const [settings, setSettings] = useState(DEFAULT_SETTINGS);
    const [showSettingsPage, setShowSettingsPage] = useState(false);
    const commitWork = (v) => chill?.game?.setWorkMinutes?.(Math.max(1, v));
    const commitBreak = (v) => chill?.game?.setBreakMinutes?.(Math.max(1, v));
    const commitLoop = (v) => chill?.game?.setLoopCount?.(Math.max(1, v));
    const applyIdlePresetReset = () => {
      commitWork(45);
      commitBreak(15);
      commitLoop(3);
    };
    const syncPhaseFromState = (state, keepRemainWhenRunning = true) => {
      const defaults = phaseDefaultsFromState(state);
      setTotal(defaults.total);
      setRemain((prev) => {
        if (keepRemainWhenRunning && state?.isRunning && prev > 0)
          return prev;
        if (state?.isRunning && prev > defaults.total)
          return defaults.total;
        return defaults.remain;
      });
    };
    useEffect(() => {
      chill?.game?.ensureEventBridge?.();
      const loaded = loadPomodoroSettings();
      setSettings(loaded);
      syncDefaultPomodoroUI(loaded.hideDefaultPomodoroUI);
      const bootPm = json(chill?.game?.getPomodoroState?.(), pm);
      const bootPg = json(chill?.game?.getPlayerProgress?.(), pg);
      const bootClock = readClock(clock);
      setPm(bootPm);
      syncPhaseFromState(bootPm, false);
      setPg(bootPg);
      setClock(bootClock);
      const tk = chill?.game?.on?.("*", (evtJson) => {
        const evt = json(evtJson, null);
        if (!evt)
          return;
        const payload = evt.payload || {};
        if (evt.name === "sceneReloaded") {
          offsetTargetOrigin = null;
          const s = loadPomodoroSettings();
          setSettings(s);
          syncDefaultPomodoroUI(s.hideDefaultPomodoroUI);
          const freshPm = json(chill?.game?.getPomodoroState?.(), pm);
          const freshPg = json(chill?.game?.getPlayerProgress?.(), pg);
          const freshClock = readClock(clock);
          setPm(freshPm);
          syncPhaseFromState(freshPm, false);
          setPg(freshPg);
          setClock(freshClock);
          return;
        }
        if (evt.name === "gameClockTick" || evt.name === "gameDateChanged") {
          setClock(payload);
          return;
        }
        if (evt.name === "pomodoroProgress") {
          setRemain(Math.max(0, Math.round(Number(payload.remainingSeconds || 0))));
          setTotal(Math.max(1, Math.round(Number(payload.totalSeconds || 1))));
          if (hasPomodoroState(payload.state)) {
            setPm(payload.state);
          }
          return;
        }
        if (hasPomodoroState(payload)) {
          setPm(payload);
          syncPhaseFromState(payload, false);
        } else if (hasPomodoroState(payload.state)) {
          setPm(payload.state);
          syncPhaseFromState(payload.state, false);
        }
        if (hasPlayerProgress(payload)) {
          setPg(payload);
        } else if (String(evt.name || "").indexOf("level") >= 0 || String(evt.name || "").indexOf("exp") >= 0 || String(evt.name || "").indexOf("workSeconds") >= 0) {
          setPg(json(chill?.game?.getPlayerProgress?.(), pg));
        }
      });
      return () => {
        if (tk)
          chill?.game?.off?.(tk);
      };
    }, []);
    const updateSettings = (patch) => {
      setSettings((prev) => {
        const next = { ...prev, ...patch };
        savePomodoroSettings(next);
        syncDefaultPomodoroUI(next.hideDefaultPomodoroUI);
        return next;
      });
    };
    const timerText = useMemo(() => {
      if (remain > 0)
        return `${hhmmss(remain)}`;
      const defaults = phaseDefaultsFromState(pm);
      return `${hhmmss(defaults.remain)}`;
    }, [pm, remain]);
    const progress = useMemo(() => clamp(1 - remain / Math.max(1, total), 0, 1), [remain, total]);
    const levelProg = useMemo(() => Math.min(1, Number(pg?.exp || 0) / Math.max(1, Number(pg?.nextLevelExp || 1))), [pg]);
    const controlState = nextControlState(pm);
    const headerClockText = formatClockDisplay(clock, settings.showSeconds);
    const btn = (t, on, bg = "rgba(125,211,252,0.15)", color = ACCENT) => /* @__PURE__ */ h("div", { onPointerDown: on, style: { fontSize: 12, color, backgroundColor: bg, borderRadius: 8, paddingLeft: 10, paddingRight: 10, paddingTop: 6, paddingBottom: 6 } }, t);
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", backgroundColor: BG, padding: 12 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 8 } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: ACCENT, unityFontStyleAndWeight: "Bold", letterSpacing: 1 } }, `${clock?.date || ""}`), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: TEXT } }, `${headerClockText}`), /* @__PURE__ */ h(
      "div",
      {
        onPointerDown: () => setShowSettingsPage((v) => !v),
        style: {
          fontSize: 12,
          color: ACCENT,
          backgroundColor: "rgba(125,211,252,0.15)",
          borderRadius: 6,
          paddingLeft: 8,
          paddingRight: 8,
          paddingTop: 4,
          paddingBottom: 4,
          marginLeft: 6
        }
      },
      showSettingsPage ? "\u2190" : "\uEB51",
      /* @__PURE__ */ h("div", { style: { width: 12 } })
    ))), /* @__PURE__ */ h("div", { style: { display: showSettingsPage ? "None" : "Flex", flexDirection: "Column", backgroundColor: CARD, borderRadius: 12, padding: 12, marginBottom: 10 } }, /* @__PURE__ */ h("div", { style: { fontSize: 11, color: DIM, marginBottom: 4 } }, `${pm?.isWorking ? "WORK" : pm?.isResting ? "BREAK" : "IDLE"} \xB7 LOOP ${pm?.loopCurrent || 0}/${pm?.loopTotal || 0}`), /* @__PURE__ */ h("div", { style: { fontSize: 42, color: TEXT, unityFontStyleAndWeight: "Bold", unityTextAlign: "MiddleCenter", marginBottom: 8 } }, timerText), /* @__PURE__ */ h("div", { style: { height: 3, borderRadius: 0, backgroundColor: "rgba(148,163,184,0.25)", overflow: "Hidden", marginBottom: 10 } }, /* @__PURE__ */ h("div", { style: { width: `${Math.round(progress * 100)}%`, height: 3, backgroundColor: pm?.isWorking ? ACCENT : WARN } })), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" } }, controlState === "idle" && btn("Reset", applyIdlePresetReset, "rgba(125,211,252,0.15)", ACCENT), controlState === "idle" && btn("Start", () => chill?.game?.startPomodoro?.(), "rgba(52,211,153,0.16)", OK), controlState === "running" && btn("Pause ", () => chill?.game?.togglePomodoroPause?.()), controlState === "running" && btn(" Skip ", () => chill?.game?.skipPomodoroPhase?.()), controlState === "running" && btn(" Stop ", () => chill?.game?.resetPomodoro?.(), "rgba(245,158,11,0.16)", WARN), controlState === "paused" && btn("Resume", () => chill?.game?.togglePomodoroPause?.(), "rgba(52,211,153,0.16)", OK), controlState === "paused" && btn(" Skip ", () => chill?.game?.skipPomodoroPhase?.()), controlState === "paused" && btn(" Stop ", () => chill?.game?.resetPomodoro?.(), "rgba(245,158,11,0.16)", WARN))), /* @__PURE__ */ h("div", { style: { display: showSettingsPage ? "None" : "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 10 } }, /* @__PURE__ */ h(
      ParamCard,
      {
        title: "Work",
        unit: "m",
        value: Number(pm?.workMinutes || 25),
        onCommit: commitWork,
        onMinus: () => commitWork(Number(pm?.workMinutes || 25) - 1),
        onPlus: () => commitWork(Number(pm?.workMinutes || 25) + 1)
      }
    ), /* @__PURE__ */ h(
      ParamCard,
      {
        title: "Break",
        unit: "m",
        value: Number(pm?.breakMinutes || 5),
        onCommit: commitBreak,
        onMinus: () => commitBreak(Number(pm?.breakMinutes || 5) - 1),
        onPlus: () => commitBreak(Number(pm?.breakMinutes || 5) + 1)
      }
    ), /* @__PURE__ */ h(
      ParamCard,
      {
        title: "Loop",
        value: Number(pm?.loopTotal || 1),
        onCommit: commitLoop,
        onMinus: () => commitLoop(Number(pm?.loopTotal || 1) - 1),
        onPlus: () => commitLoop(Number(pm?.loopTotal || 1) + 1)
      }
    )), /* @__PURE__ */ h("div", { style: { display: showSettingsPage ? "None" : "Flex", flexDirection: "Column", backgroundColor: CARD, borderRadius: 12, padding: 12 } }, /* @__PURE__ */ h("div", { style: { fontSize: 11, color: DIM, marginBottom: 4 } }, "Player Level"), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 6 } }, /* @__PURE__ */ h("div", { style: { fontSize: 16, color: TEXT, unityFontStyleAndWeight: "Bold" } }, `Lv.${pg?.level || 0}`), /* @__PURE__ */ h("div", { style: { fontSize: 12, color: DIM } }, `${Math.round(Number(pg?.exp || 0))}/${Math.round(Number(pg?.nextLevelExp || 0))}`)), /* @__PURE__ */ h("div", { style: { height: 3, borderRadius: 0, backgroundColor: "rgba(148,163,184,0.25)", overflow: "Hidden", marginBottom: 6 } }, /* @__PURE__ */ h("div", { style: { width: `${Math.round(levelProg * 100)}%`, height: 3, backgroundColor: OK } })), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: DIM } }, `Total Focus: ${Math.round(Number(pg?.totalWorkSeconds || 0) / 3600 * 10) / 10}h`)), /* @__PURE__ */ h("div", { style: { display: showSettingsPage ? "Flex" : "None", flexDirection: "Column", flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: { backgroundColor: PANEL, borderRadius: 12, padding: 12, marginBottom: 8 } }, /* @__PURE__ */ h("div", { style: { fontSize: 13, color: TEXT, unityFontStyleAndWeight: "Bold", marginBottom: 8 } }, "\u8BBE\u7F6E"), /* @__PURE__ */ h(
      SettingsCheckbox,
      {
        checked: settings.hideDefaultPomodoroUI,
        label: "\u9690\u85CF\u6E38\u620F\u9ED8\u8BA4\u756A\u8304\u949F UI",
        hint: "\u542F\u7528\u540E\u5C06\u6309\u4E0A\u65B9\u6570\u7EC4\u9010\u9879\u9690\u85CF\uFF0C\u5E76\u5E94\u7528\u6307\u5B9A\u504F\u79FB",
        onToggle: () => updateSettings({ hideDefaultPomodoroUI: !settings.hideDefaultPomodoroUI })
      }
    ), /* @__PURE__ */ h(
      SettingsCheckbox,
      {
        checked: settings.showSeconds,
        label: "\u65F6\u949F\u663E\u793A\u79D2",
        hint: "\u6298\u53E0\u4E0E\u5C55\u5F00\u72B6\u6001\u7684\u53F3\u4E0A\u89D2\u65F6\u949F\u7EDF\u4E00\u8DDF\u968F\u8BE5\u9009\u9879",
        onToggle: () => updateSettings({ showSeconds: !settings.showSeconds })
      }
    )), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM, paddingLeft: 2, paddingRight: 2 } }, `\u914D\u7F6E\u4FDD\u5B58\u5728 ${SETTINGS_FILE}`)));
  };
  var PomodoroCompact = () => {
    const [clock, setClock] = useState(buildFallbackClock());
    const [state, setState] = useState({ isRunning: false, isResting: false, workMinutes: 25, breakMinutes: 5 });
    const [remain, setRemain] = useState(0);
    const [total, setTotal] = useState(1);
    const [settings, setSettings] = useState(DEFAULT_SETTINGS);
    const commitWork = (v) => chill?.game?.setWorkMinutes?.(Math.max(1, v));
    const commitBreak = (v) => chill?.game?.setBreakMinutes?.(Math.max(1, v));
    const commitLoop = (v) => chill?.game?.setLoopCount?.(Math.max(1, v));
    const applyIdlePresetReset = () => {
      commitWork(45);
      commitBreak(15);
      commitLoop(3);
    };
    const syncCompactState = (next, keepRemain = true) => {
      setState(next);
      const defaults = phaseDefaultsFromState(next);
      setTotal(defaults.total);
      setRemain((prev) => {
        if (keepRemain && next?.isRunning && prev > 0)
          return Math.min(prev, defaults.total);
        return defaults.remain;
      });
    };
    useEffect(() => {
      chill?.game?.ensureEventBridge?.();
      const loaded = loadPomodoroSettings();
      setSettings(loaded);
      syncDefaultPomodoroUI(loaded.hideDefaultPomodoroUI);
      const bootState = json(chill?.game?.getPomodoroState?.(), state);
      syncCompactState(bootState, false);
      const currentClock = json(chill?.game?.getGameClock?.(), clock);
      setClock(currentClock);
      const tk = chill?.game?.on?.("*", (e) => {
        const evt = json(e, null);
        if (!evt)
          return;
        const payload = evt.payload || {};
        if (evt.name === "sceneReloaded") {
          offsetTargetOrigin = null;
          const s = loadPomodoroSettings();
          setSettings(s);
          syncDefaultPomodoroUI(s.hideDefaultPomodoroUI);
          const freshState = json(chill?.game?.getPomodoroState?.(), state);
          syncCompactState(freshState, false);
          setClock(json(chill?.game?.getGameClock?.(), clock));
          return;
        }
        if (evt.name === "gameClockTick" || evt.name === "gameDateChanged") {
          setClock(payload);
          return;
        }
        if (evt.name === "pomodoroProgress" && isProgressPayload(payload)) {
          setRemain(Math.max(0, Math.round(Number(payload.remainingSeconds || 0))));
          setTotal(Math.max(1, Math.round(Number(payload.totalSeconds || 1))));
          if (hasPomodoroState(payload.state)) {
            syncCompactState(payload.state);
          }
          return;
        }
        if (hasPomodoroState(payload)) {
          syncCompactState(payload, false);
        } else if (hasPomodoroState(payload.state)) {
          syncCompactState(payload.state, false);
        }
      });
      return () => {
        if (tk)
          chill?.game?.off?.(tk);
      };
    }, []);
    const controlState = nextControlState(state);
    const isBreakPhase = state?.isResting || state?.type === "Break";
    const isWorkPhase = state?.isWorking || state?.type === "Work";
    const statusText = controlState === "idle" ? "\u5F85\u5F00\u59CB" : isBreakPhase ? controlState === "paused" ? "\u4F11\u606F\u6682\u505C" : "\u4F11\u606F\u4E2D" : isWorkPhase ? controlState === "paused" ? "\u4E13\u6CE8\u6682\u505C" : "\u4E13\u6CE8\u4E2D" : controlState === "paused" ? "\u5DF2\u6682\u505C" : "\u4E13\u6CE8\u4E2D";
    const statusColor = controlState === "idle" ? DIM : isBreakPhase ? "#4CAF50" : ACCENT;
    const timerText = remain > 0 ? hhmmss(remain) : hhmmss(total);
    const compactClockText = formatClockDisplay(clock, settings.showSeconds);
    const cbtn = (t, on, bg = "rgba(125,211,252,0.15)", color = ACCENT) => /* @__PURE__ */ h(
      "div",
      {
        onPointerDown: on,
        style: {
          fontSize: 11,
          color,
          backgroundColor: bg,
          borderRadius: 6,
          paddingLeft: 6,
          paddingRight: 6,
          paddingTop: 4,
          paddingBottom: 4,
          unityTextAlign: "MiddleCenter",
          flexGrow: 1,
          flexBasis: 0,
          marginLeft: 3,
          marginRight: 3
        }
      },
      t
    );
    return /* @__PURE__ */ h("div", { style: {
      flexGrow: 1,
      display: "Flex",
      flexDirection: "Column",
      backgroundColor: CARD,
      borderRadius: 8,
      paddingLeft: 10,
      paddingRight: 10,
      paddingTop: 8,
      paddingBottom: 8
    } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 4 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: TEXT, opacity: 0.7 } }, "Pomodoro"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: TEXT, opacity: 0.85 } }, `${compactClockText}`)), /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }), /* @__PURE__ */ h("div", { style: { fontSize: 13, color: statusColor, unityFontStyleAndWeight: "Bold", unityTextAlign: "MiddleCenter", marginBottom: 2 } }, `${statusText}`), /* @__PURE__ */ h("div", { style: { fontSize: 24, color: TEXT, unityFontStyleAndWeight: "Bold", unityTextAlign: "MiddleCenter", marginBottom: 8 } }, `${timerText}`), /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceEvenly" } }, controlState === "idle" && cbtn("Reset", applyIdlePresetReset, "rgba(125,211,252,0.15)", ACCENT), controlState === "idle" && cbtn("Start", () => chill?.game?.startPomodoro?.(), "rgba(52,211,153,0.16)", OK), controlState === "running" && cbtn("Pause", () => chill?.game?.togglePomodoroPause?.()), controlState === "running" && cbtn("Skip", () => chill?.game?.skipPomodoroPhase?.()), controlState === "running" && cbtn("Stop", () => chill?.game?.resetPomodoro?.(), "rgba(245,158,11,0.16)", WARN), controlState === "paused" && cbtn("Resume", () => chill?.game?.togglePomodoroPause?.(), "rgba(52,211,153,0.16)", OK), controlState === "paused" && cbtn("Skip", () => chill?.game?.skipPomodoroPhase?.()), controlState === "paused" && cbtn("Stop", () => chill?.game?.resetPomodoro?.(), "rgba(245,158,11,0.16)", WARN)));
  };
  __registerPlugin({
    id: "pomodoro",
    title: "Pomodoro",
    width: 340,
    height: 520,
    initialX: 260,
    initialY: 120,
    resizable: true,
    launcher: { text: "\u{F0109}", background: "#0ea5e9" },
    compact: { width: 180, height: 180, component: PomodoroCompact },
    component: PomodoroPanel
  });
})();
//# sourceMappingURL=app.js.map
