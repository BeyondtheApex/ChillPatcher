import { h } from "preact"
import { useEffect, useMemo, useState } from "preact/hooks"

declare const __registerPlugin: any
declare const chill: any

const BG = "#0b1020", CARD = "#111827", PANEL = "#1e293b", TEXT = "#e5e7eb", DIM = "#94a3b8", ACCENT = "#7dd3fc", OK = "#34d399", WARN = "#f59e0b"
const SETTINGS_FILE = "window-manager/pomodoro-settings.json"
const HIDE_TARGET_PATHS: string[] = [
  "Paremt/PCPlatform/Canvas/UI/UI_FacilityPomodoro",
  "Paremt/PCPlatform/Canvas/UI/UI_FacilityPlayerLevel"
]
const OFFSET_TARGET_PATH: string = "Paremt/PCPlatform/Canvas/UI/MostFrontArea/TopIcons"
const OFFSET_Y_WHEN_HIDDEN = 250

type PomodoroSettings = {
  hideDefaultPomodoroUI: boolean
  showSeconds: boolean
}

const DEFAULT_SETTINGS: PomodoroSettings = {
  hideDefaultPomodoroUI: true,
  showSeconds: false,
}

let offsetTargetOrigin: { x: number, y: number } | null = null

const pad2 = (n: number) => String(n).padStart(2, "0")
const hhmmss = (s: number) => `${pad2(Math.floor(Math.max(0, s) / 3600))}:${pad2(Math.floor((Math.max(0, s) % 3600) / 60))}:${pad2(Math.floor(Math.max(0, s) % 60))}`
const json = (v: any, fb: any) => { try { return typeof v === "string" ? JSON.parse(v) : v } catch { return fb } }
const clamp = (v: number, min: number, max: number) => Math.max(min, Math.min(max, v))

const loadPomodoroSettings = (): PomodoroSettings => {
  try {
    if (!chill?.io?.exists?.(SETTINGS_FILE)) return DEFAULT_SETTINGS
    const txt = chill?.io?.readText?.(SETTINGS_FILE)
    const parsed = json(txt, null)
    if (!parsed || typeof parsed !== "object") return DEFAULT_SETTINGS
    return {
      hideDefaultPomodoroUI: !!parsed.hideDefaultPomodoroUI,
      showSeconds: !!parsed.showSeconds,
    }
  } catch {
    return DEFAULT_SETTINGS
  }
}

const savePomodoroSettings = (settings: PomodoroSettings) => {
  try {
    chill?.io?.writeText?.(SETTINGS_FILE, JSON.stringify(settings, null, 2))
  } catch { }
}

const setNodeActive = (path: string, active: boolean) => {
  if (!path || !path.trim()) return
  chill?.ui?.setActive?.(path, active)
}

const applyOffsetForTarget = (hideEnabled: boolean) => {
  if (!OFFSET_TARGET_PATH || !OFFSET_TARGET_PATH.trim()) return

  if (!hideEnabled) {
    if (offsetTargetOrigin) {
      chill?.ui?.setPosition?.(OFFSET_TARGET_PATH, offsetTargetOrigin.x, offsetTargetOrigin.y)
    }
    return
  }

  if (!offsetTargetOrigin) {
    const rect = json(chill?.ui?.getRect?.(OFFSET_TARGET_PATH), null)
    if (!rect) return
    const x = Number(rect.x)
    const y = Number(rect.y)
    if (Number.isNaN(x) || Number.isNaN(y)) return
    offsetTargetOrigin = { x, y }
  }

  chill?.ui?.setPosition?.(OFFSET_TARGET_PATH, offsetTargetOrigin.x, offsetTargetOrigin.y + OFFSET_Y_WHEN_HIDDEN)
}

const syncDefaultPomodoroUI = (hideEnabled: boolean) => {
  const active = !hideEnabled
  for (const path of HIDE_TARGET_PATHS) {
    setNodeActive(path, active)
  }
  applyOffsetForTarget(hideEnabled)
}

const buildFallbackClock = () => {
  const d = new Date()
  const date = `${d.getFullYear()}/${pad2(d.getMonth() + 1)}/${pad2(d.getDate())}(${d.toLocaleDateString(undefined, { weekday: "short" })})`
  const time = `${pad2(d.getHours())}:${pad2(d.getMinutes())}`
  return { available: false, date, time, amPm: "", dateTime: `${date} ${time}` }
}

const readClock = (prev?: any) => {
  const fallback = prev || buildFallbackClock()
  return json(chill?.game?.getGameClock?.(), fallback)
}

const phaseDefaultsFromState = (state: any) => {
  const isResting = !!state?.isResting
  const mins = isResting ? Number(state?.breakMinutes || 5) : Number(state?.workMinutes || 25)
  const total = Math.max(1, Math.round(mins * 60))
  return { total, remain: total }
}

const nextControlState = (state: any) => {
  if (state?.isPaused) return "paused"
  if (state?.isRunning) return "running"
  const started = state?.type === "Work" || state?.type === "Break" || state?.isWorking || state?.isResting
  if (!started) return "idle"
  return "idle"
}

const isProgressPayload = (payload: any) => payload && payload.remainingSeconds !== undefined && payload.totalSeconds !== undefined

const hasPomodoroState = (payload: any) => payload && payload.workMinutes !== undefined && payload.breakMinutes !== undefined

const hasPlayerProgress = (payload: any) => payload && payload.level !== undefined && payload.nextLevelExp !== undefined

const formatClockWithSeconds = (clock: any) => {
  const unixMs = Number(clock?.unixMs || 0)
  const d = unixMs > 0 ? new Date(unixMs) : new Date()
  if (clock?.amPm) {
    const hh = d.getHours() % 12 || 12
    return `${pad2(hh)}:${pad2(d.getMinutes())}:${pad2(d.getSeconds())} ${clock.amPm}`
  }
  return `${pad2(d.getHours())}:${pad2(d.getMinutes())}:${pad2(d.getSeconds())}`
}

const formatClockDisplay = (clock: any, showSeconds: boolean) => {
  if (showSeconds) return formatClockWithSeconds(clock)

  const unixMs = Number(clock?.unixMs || 0)
  if (unixMs > 0) {
    const d = new Date(unixMs)
    if (clock?.amPm) {
      const hh = d.getHours() % 12 || 12
      return `${pad2(hh)}:${pad2(d.getMinutes())}${clock?.amPm ? ` ${clock.amPm}` : ""}`
    }
    return `${pad2(d.getHours())}:${pad2(d.getMinutes())}`
  }

  const fallback = `${clock?.time || "--:--"}`
  return `${fallback}${clock?.amPm ? ` ${clock.amPm}` : ""}`
}

const SettingsCheckbox = (props: { checked: boolean, label: string, hint?: string, onToggle: () => void }) => (
  <div
    style={{
      display: "Flex",
      flexDirection: "Row",
      alignItems: "Center",
      backgroundColor: CARD,
      borderRadius: 8,
      paddingLeft: 10,
      paddingRight: 10,
      paddingTop: 10,
      paddingBottom: 10,
      marginBottom: 8,
    }}
    onPointerDown={props.onToggle}
  >
    <div style={{
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
      flexShrink: 0,
    }}>
      {props.checked && (<div style={{ fontSize: 12, color: TEXT }}>{"✓"}</div>)}
    </div>
    <div style={{ display: "Flex", flexDirection: "Column", flexGrow: 1 }}>
      <div style={{ fontSize: 12, color: TEXT }}>{props.label}</div>
      <div style={{ fontSize: 10, color: DIM, display: props.hint ? "Flex" : "None", marginTop: 2 }}>{props.hint || ""}</div>
    </div>
  </div>
)

const ParamCard = (props: {
  title: string
  unit?: string
  value: number
  onCommit: (v: number) => void
  onMinus: () => void
  onPlus: () => void
}) => {
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState("")

  const submit = () => {
    const parsed = Number(draft)
    if (Number.isNaN(parsed)) {
      setEditing(false)
      return
    }
    props.onCommit(Math.round(parsed))
    setEditing(false)
  }

  const startEdit = () => {
    setDraft(`${props.value}`)
    setEditing(true)
  }

  const chip = (t: string, on: () => void, bg = "rgba(125,211,252,0.15)", color = ACCENT) => (
    <div onPointerDown={on} style={{ fontSize: 12, color, backgroundColor: bg, borderRadius: 8, paddingLeft: 10, paddingRight: 10, paddingTop: 6, paddingBottom: 6 }}>{t}</div>
  )

  return (
    <div style={{ backgroundColor: PANEL, borderRadius: 10, padding: 8, width: 98 }}>
      <div style={{ fontSize: 10, color: DIM }}>{props.title}</div>
      {editing ? (
        <textfield
          value={draft}
          onValueChanged={(e: any) => setDraft(e?.newValue ?? e?.target?.value ?? draft)}
          onKeyDown={(e: any) => { if (e?.keyCode === 13) submit() }}
          style={{ fontSize: 18, color: TEXT, backgroundColor: BG, borderRadius: 6, paddingLeft: 6, paddingRight: 6, paddingTop: 2, paddingBottom: 2, marginBottom: 6 }}
        />
      ) : (
        <div style={{ fontSize: 18, color: TEXT, marginBottom: 6 }} onPointerDown={startEdit}>{`${props.value}${props.unit || ""}`}</div>
      )}

      <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" }}>
        {editing
          ? chip("OK", submit, "rgba(52,211,153,0.16)", OK)
          : (<div style={{ display: 'flex', flexDirection: 'row' }}>
            {chip("-", props.onMinus)}
            <div style={{ width: 8 }} />
            {chip("+", props.onPlus)}
          </div>)}
      </div>
    </div>
  )
}

const PomodoroPanel = () => {
  const [clock, setClock] = useState<any>(buildFallbackClock())
  const [pm, setPm] = useState<any>({ available: false, type: "Unknown", loopCurrent: 0, loopTotal: 0, workMinutes: 25, breakMinutes: 5, isRunning: false, isWorking: false, isResting: false })
  const [pg, setPg] = useState<any>({ available: false, level: 0, exp: 0, nextLevelExp: 1, totalWorkSeconds: 0 })
  const [remain, setRemain] = useState(0)
  const [total, setTotal] = useState(1)
  const [settings, setSettings] = useState<PomodoroSettings>(DEFAULT_SETTINGS)
  const [showSettingsPage, setShowSettingsPage] = useState(false)

  const commitWork = (v: number) => chill?.game?.setWorkMinutes?.(Math.max(1, v))
  const commitBreak = (v: number) => chill?.game?.setBreakMinutes?.(Math.max(1, v))
  const commitLoop = (v: number) => chill?.game?.setLoopCount?.(Math.max(1, v))

  const applyIdlePresetReset = () => {
    commitWork(45)
    commitBreak(15)
    commitLoop(3)
  }

  const syncPhaseFromState = (state: any, keepRemainWhenRunning = true) => {
    const defaults = phaseDefaultsFromState(state)
    setTotal(defaults.total)
    setRemain(prev => {
      if (keepRemainWhenRunning && state?.isRunning && prev > 0) return prev
      if (state?.isRunning && prev > defaults.total) return defaults.total
      return defaults.remain
    })
  }

  useEffect(() => {
    chill?.game?.ensureEventBridge?.()
    const loaded = loadPomodoroSettings()
    setSettings(loaded)
    syncDefaultPomodoroUI(loaded.hideDefaultPomodoroUI)

    const bootPm = json(chill?.game?.getPomodoroState?.(), pm)
    const bootPg = json(chill?.game?.getPlayerProgress?.(), pg)
    const bootClock = readClock(clock)
    setPm(bootPm)
    syncPhaseFromState(bootPm, false)
    setPg(bootPg)
    setClock(bootClock)

    const tk = chill?.game?.on?.("*", (evtJson: string) => {
      const evt = json(evtJson, null)
      if (!evt) return

      const payload = evt.payload || {}

      if (evt.name === "gameClockTick" || evt.name === "gameDateChanged") {
        setClock(payload)
        return
      }

      if (evt.name === "pomodoroProgress") {
        setRemain(Math.max(0, Math.round(Number(payload.remainingSeconds || 0))))
        setTotal(Math.max(1, Math.round(Number(payload.totalSeconds || 1))))
        if (hasPomodoroState(payload.state)) {
          setPm(payload.state)
        }
        return
      }

      if (hasPomodoroState(payload)) {
        setPm(payload)
        syncPhaseFromState(payload, false)
      } else if (hasPomodoroState(payload.state)) {
        setPm(payload.state)
        syncPhaseFromState(payload.state, false)
      }

      if (hasPlayerProgress(payload)) {
        setPg(payload)
      } else if (String(evt.name || "").indexOf("level") >= 0 || String(evt.name || "").indexOf("exp") >= 0 || String(evt.name || "").indexOf("workSeconds") >= 0) {
        setPg(json(chill?.game?.getPlayerProgress?.(), pg))
      }
    })
    return () => { if (tk) chill?.game?.off?.(tk) }
  }, [])

  const updateSettings = (patch: Partial<PomodoroSettings>) => {
    setSettings(prev => {
      const next = { ...prev, ...patch }
      savePomodoroSettings(next)
      syncDefaultPomodoroUI(next.hideDefaultPomodoroUI)
      return next
    })
  }

  const timerText = useMemo(() => {
    if (remain > 0) return `${hhmmss(remain)}`
    const defaults = phaseDefaultsFromState(pm)
    return `${hhmmss(defaults.remain)}`
  }, [pm, remain])

  const progress = useMemo(() => clamp(1 - remain / Math.max(1, total), 0, 1), [remain, total])
  const levelProg = useMemo(() => Math.min(1, Number(pg?.exp || 0) / Math.max(1, Number(pg?.nextLevelExp || 1))), [pg])
  const controlState = nextControlState(pm)
  const headerClockText = formatClockDisplay(clock, settings.showSeconds)

  const btn = (t: string, on: () => void, bg = "rgba(125,211,252,0.15)", color = ACCENT) => (<div onPointerDown={on} style={{ fontSize: 12, color, backgroundColor: bg, borderRadius: 8, paddingLeft: 10, paddingRight: 10, paddingTop: 6, paddingBottom: 6 }}>{t}</div>)

  return (
    <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", backgroundColor: BG, padding: 12 }}>
      <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 8 }}>
        <div style={{ fontSize: 12, color: ACCENT, unityFontStyleAndWeight: "Bold", letterSpacing: 1 }}>{`${clock?.date || ""}`}</div>
        <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
          <div style={{ fontSize: 12, color: TEXT }}>{`${headerClockText}`}</div>
          <div
            onPointerDown={() => setShowSettingsPage(v => !v)}
            style={{
              fontSize: 12,
              color: ACCENT,
              backgroundColor: "rgba(125,211,252,0.15)",
              borderRadius: 6,
              paddingLeft: 8,
              paddingRight: 8,
              paddingTop: 4,
              paddingBottom: 4,
              marginLeft: 6,
            }}
          >
            {showSettingsPage ? "←" : ""}
            <div style={{ width: 12 }}></div>
          </div>
        </div>
      </div>

      <div style={{ display: showSettingsPage ? "None" : "Flex", flexDirection: "Column", backgroundColor: CARD, borderRadius: 12, padding: 12, marginBottom: 10 }}>
        <div style={{ fontSize: 11, color: DIM, marginBottom: 4 }}>{`${pm?.isWorking ? "WORK" : (pm?.isResting ? "BREAK" : "IDLE")} · LOOP ${pm?.loopCurrent || 0}/${pm?.loopTotal || 0}`}</div>
        <div style={{ fontSize: 42, color: TEXT, unityFontStyleAndWeight: "Bold", unityTextAlign: "MiddleCenter", marginBottom: 8 }}>{timerText}</div>
        <div style={{ height: 3, borderRadius: 0, backgroundColor: "rgba(148,163,184,0.25)", overflow: "Hidden", marginBottom: 10 }}>
          <div style={{ width: `${Math.round(progress * 100)}%`, height: 3, backgroundColor: pm?.isWorking ? ACCENT : WARN }} />
        </div>
        <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" }}>
          {controlState === "idle" && btn("Reset", applyIdlePresetReset, "rgba(125,211,252,0.15)", ACCENT)}
          {controlState === "idle" && btn("Start", () => chill?.game?.startPomodoro?.(), "rgba(52,211,153,0.16)", OK)}
          {controlState === "running" && btn("Pause ", () => chill?.game?.togglePomodoroPause?.())}
          {controlState === "running" && btn(" Skip ", () => chill?.game?.skipPomodoroPhase?.())}
          {controlState === "running" && btn(" Stop ", () => chill?.game?.resetPomodoro?.(), "rgba(245,158,11,0.16)", WARN)}
          {controlState === "paused" && btn("Resume", () => chill?.game?.togglePomodoroPause?.(), "rgba(52,211,153,0.16)", OK)}
          {controlState === "paused" && btn(" Skip ", () => chill?.game?.skipPomodoroPhase?.())}
          {controlState === "paused" && btn(" Stop ", () => chill?.game?.resetPomodoro?.(), "rgba(245,158,11,0.16)", WARN)}
        </div>
      </div>

      <div style={{ display: showSettingsPage ? "None" : "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 10 }}>
        <ParamCard
          title="Work"
          unit="m"
          value={Number(pm?.workMinutes || 25)}
          onCommit={commitWork}
          onMinus={() => commitWork(Number(pm?.workMinutes || 25) - 1)}
          onPlus={() => commitWork(Number(pm?.workMinutes || 25) + 1)}
        />
        <ParamCard
          title="Break"
          unit="m"
          value={Number(pm?.breakMinutes || 5)}
          onCommit={commitBreak}
          onMinus={() => commitBreak(Number(pm?.breakMinutes || 5) - 1)}
          onPlus={() => commitBreak(Number(pm?.breakMinutes || 5) + 1)}
        />
        <ParamCard
          title="Loop"
          value={Number(pm?.loopTotal || 1)}
          onCommit={commitLoop}
          onMinus={() => commitLoop(Number(pm?.loopTotal || 1) - 1)}
          onPlus={() => commitLoop(Number(pm?.loopTotal || 1) + 1)}
        />
      </div>

      <div style={{ display: showSettingsPage ? "None" : "Flex", flexDirection: "Column", backgroundColor: CARD, borderRadius: 12, padding: 12 }}>
        <div style={{ fontSize: 11, color: DIM, marginBottom: 4 }}>Player Level</div>
        <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 6 }}>
          <div style={{ fontSize: 16, color: TEXT, unityFontStyleAndWeight: "Bold" }}>{`Lv.${pg?.level || 0}`}</div>
          <div style={{ fontSize: 12, color: DIM }}>{`${Math.round(Number(pg?.exp || 0))}/${Math.round(Number(pg?.nextLevelExp || 0))}`}</div>
        </div>
        <div style={{ height: 3, borderRadius: 0, backgroundColor: "rgba(148,163,184,0.25)", overflow: "Hidden", marginBottom: 6 }}><div style={{ width: `${Math.round(levelProg * 100)}%`, height: 3, backgroundColor: OK }} /></div>
        <div style={{ fontSize: 11, color: DIM }}>{`Total Focus: ${Math.round((Number(pg?.totalWorkSeconds || 0) / 3600) * 10) / 10}h`}</div>
      </div>

      <div style={{ display: showSettingsPage ? "Flex" : "None", flexDirection: "Column", flexGrow: 1 }}>
        <div style={{ backgroundColor: PANEL, borderRadius: 12, padding: 12, marginBottom: 8 }}>
          <div style={{ fontSize: 13, color: TEXT, unityFontStyleAndWeight: "Bold", marginBottom: 8 }}>设置</div>
          <SettingsCheckbox
            checked={settings.hideDefaultPomodoroUI}
            label="隐藏游戏默认番茄钟 UI"
            hint="启用后将按上方数组逐项隐藏，并应用指定偏移"
            onToggle={() => updateSettings({ hideDefaultPomodoroUI: !settings.hideDefaultPomodoroUI })}
          />
          <SettingsCheckbox
            checked={settings.showSeconds}
            label="时钟显示秒"
            hint="折叠与展开状态的右上角时钟统一跟随该选项"
            onToggle={() => updateSettings({ showSeconds: !settings.showSeconds })}
          />
        </div>
        <div style={{ fontSize: 10, color: DIM, paddingLeft: 2, paddingRight: 2 }}>
          {`配置保存在 ${SETTINGS_FILE}`}
        </div>
      </div>
    </div>
  )
}

const PomodoroCompact = () => {
  const [clock, setClock] = useState<any>(buildFallbackClock())
  const [state, setState] = useState<any>({ isRunning: false, isResting: false, workMinutes: 25, breakMinutes: 5 })
  const [remain, setRemain] = useState(0)
  const [total, setTotal] = useState(1)
  const [settings, setSettings] = useState<PomodoroSettings>(DEFAULT_SETTINGS)

  const commitWork = (v: number) => chill?.game?.setWorkMinutes?.(Math.max(1, v))
  const commitBreak = (v: number) => chill?.game?.setBreakMinutes?.(Math.max(1, v))
  const commitLoop = (v: number) => chill?.game?.setLoopCount?.(Math.max(1, v))
  const applyIdlePresetReset = () => {
    commitWork(45)
    commitBreak(15)
    commitLoop(3)
  }

  const syncCompactState = (next: any, keepRemain = true) => {
    setState(next)
    const defaults = phaseDefaultsFromState(next)
    setTotal(defaults.total)
    setRemain(prev => {
      if (keepRemain && next?.isRunning && prev > 0) return Math.min(prev, defaults.total)
      return defaults.remain
    })
  }

  useEffect(() => {
    chill?.game?.ensureEventBridge?.()
    const loaded = loadPomodoroSettings()
    setSettings(loaded)
    syncDefaultPomodoroUI(loaded.hideDefaultPomodoroUI)

    // 初始化数据
    const bootState = json(chill?.game?.getPomodoroState?.(), state)
    syncCompactState(bootState, false)

    const currentClock = json(chill?.game?.getGameClock?.(), clock)
    setClock(currentClock)

    const tk = chill?.game?.on?.("*", (e: string) => {
      const evt = json(e, null)
      if (!evt) return
      const payload = evt.payload || {}

      if (evt.name === "gameClockTick" || evt.name === "gameDateChanged") {
        setClock(payload)
        return
      }

      if (evt.name === "pomodoroProgress" && isProgressPayload(payload)) {
        setRemain(Math.max(0, Math.round(Number(payload.remainingSeconds || 0))))
        setTotal(Math.max(1, Math.round(Number(payload.totalSeconds || 1))))
        if (hasPomodoroState(payload.state)) {
          syncCompactState(payload.state)
        }
        return
      }

      if (hasPomodoroState(payload)) {
        syncCompactState(payload, false)
      } else if (hasPomodoroState(payload.state)) {
        syncCompactState(payload.state, false)
      }
    })
    return () => { if (tk) chill?.game?.off?.(tk) }
  }, [])

  const controlState = nextControlState(state)
  const isBreakPhase = state?.isResting || state?.type === "Break"
  const isWorkPhase = state?.isWorking || state?.type === "Work"
  const statusText = controlState === "idle"
    ? "待开始"
    : (isBreakPhase
      ? (controlState === "paused" ? "休息暂停" : "休息中")
      : (isWorkPhase
        ? (controlState === "paused" ? "专注暂停" : "专注中")
        : (controlState === "paused" ? "已暂停" : "专注中")))
  const statusColor = controlState === "idle"
    ? DIM
    : (isBreakPhase ? "#4CAF50" : ACCENT)
  const timerText = remain > 0 ? hhmmss(remain) : hhmmss(total)
  const compactClockText = formatClockDisplay(clock, settings.showSeconds)
  const cbtn = (t: string, on: () => void, bg = "rgba(125,211,252,0.15)", color = ACCENT) => (
    <div
      onPointerDown={on}
      style={{
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
        marginRight: 3,
      }}
    >
      {t}
    </div>
  )

  return (
    <div style={{
      flexGrow: 1,
      display: "Flex",
      flexDirection: "Column",
      backgroundColor: CARD,
      borderRadius: 8,
      paddingLeft: 10,
      paddingRight: 10,
      paddingTop: 8,
      paddingBottom: 8
    }}>
      <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginBottom: 4 }}>
        <div style={{ fontSize: 10, color: TEXT, opacity: 0.7 }}>{"Pomodoro"}</div>
        <div style={{ fontSize: 10, color: TEXT, opacity: 0.85 }}>{`${compactClockText}`}</div>
      </div>

      <div style={{ flexGrow: 1 }} />

      <div style={{ fontSize: 13, color: statusColor, unityFontStyleAndWeight: "Bold", unityTextAlign: "MiddleCenter", marginBottom: 2 }}>{`${statusText}`}</div>
      <div style={{ fontSize: 24, color: TEXT, unityFontStyleAndWeight: "Bold", unityTextAlign: "MiddleCenter", marginBottom: 8 }}>{`${timerText}`}</div>

      <div style={{ flexGrow: 1 }} />

      <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceEvenly" }}>
        {controlState === "idle" && cbtn("Reset", applyIdlePresetReset, "rgba(125,211,252,0.15)", ACCENT)}
        {controlState === "idle" && cbtn("Start", () => chill?.game?.startPomodoro?.(), "rgba(52,211,153,0.16)", OK)}
        {controlState === "running" && cbtn("Pause", () => chill?.game?.togglePomodoroPause?.())}
        {controlState === "running" && cbtn("Skip", () => chill?.game?.skipPomodoroPhase?.())}
        {controlState === "running" && cbtn("Stop", () => chill?.game?.resetPomodoro?.(), "rgba(245,158,11,0.16)", WARN)}
        {controlState === "paused" && cbtn("Resume", () => chill?.game?.togglePomodoroPause?.(), "rgba(52,211,153,0.16)", OK)}
        {controlState === "paused" && cbtn("Skip", () => chill?.game?.skipPomodoroPhase?.())}
        {controlState === "paused" && cbtn("Stop", () => chill?.game?.resetPomodoro?.(), "rgba(245,158,11,0.16)", WARN)}
      </div>
    </div>
  )
}

__registerPlugin({
  id: "pomodoro",
  title: "Pomodoro",
  width: 340,
  height: 520,
  initialX: 260,
  initialY: 120,
  resizable: true,
  launcher: { text: "󰄉", background: "#0ea5e9" },
  compact: { width: 180, height: 180, component: PomodoroCompact },
  component: PomodoroPanel,
})
