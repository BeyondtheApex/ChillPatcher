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

  // plugins/aichat/index.tsx
  var groups = {
    "1. LLM": ["Use_Ollama_API", "ThinkMode", "API_URL", "API_Key", "ModelName", "LogApiRequestBody", "FixApiPathForThinkMode"],
    "2. TTS": ["TTS_Service_URL", "TTS_Service_Script_Path", "LaunchTTSService", "QuitTTSServiceOnQuit", "Audio_File_Path", "AudioPathCheck", "Audio_File_Text", "PromptLang", "TargetLang", "JapaneseCheck", "VoiceVolume"],
    "3. UI": ["WindowWidth", "WindowHeightBase", "ReverseEnterBehavior", "BackgroundOpacity", "ShowWindowTitle"],
    "4. Persona": ["ExperimentalMemory", "SystemPrompt"]
  };
  var multilineKeys = /* @__PURE__ */ new Set(["Audio_File_Text", "SystemPrompt"]);
  var call = (name, ...args) => JSON.parse(chill.aichat[name](...args));
  var loadCfg = () => JSON.parse(chill.aichat.getAllConfig() || "{}");
  var loadDefaults = () => JSON.parse(chill.aichat.getAllConfigDefaults() || "{}");
  var fieldEstimatedHeight = (k) => !multilineKeys.has(k) ? 52 : k === "SystemPrompt" ? 320 : 210;
  var paginateFields = (fields, maxHeight) => {
    const pages = [];
    let current = [];
    let used = 0;
    const limit = Math.max(120, Math.floor(maxHeight));
    for (const k of fields) {
      const h2 = fieldEstimatedHeight(k);
      if (current.length > 0 && used + h2 > limit) {
        pages.push(current);
        current = [];
        used = 0;
      }
      current.push(k);
      used += h2;
    }
    if (current.length > 0)
      pages.push(current);
    return pages.length > 0 ? pages : [[]];
  };
  var SavePanel = ({ draft, defaults, setDraft, onCancel, onOk }) => {
    const pages = Object.keys(groups);
    const [page, setPage] = useState(0);
    const [contentPage, setContentPage] = useState(0);
    const [contentHeight, setContentHeight] = useState(220);
    const currentGroup = pages[page];
    const fields = groups[currentGroup] || [];
    const fieldPages = paginateFields(fields, contentHeight - (currentGroup === "4. Persona" ? 40 : 24));
    const maxContentPage = Math.max(0, fieldPages.length - 1);
    const displayFields = fieldPages[Math.min(contentPage, maxContentPage)] || [];
    useEffect(() => {
      setContentPage((p) => Math.min(p, maxContentPage));
    }, [maxContentPage, page]);
    const row = (k) => multilineKeys.has(k) ? /* @__PURE__ */ h("div", { key: k, style: { display: "Flex", flexDirection: "Column", marginBottom: 6, backgroundColor: "#111827", paddingTop: 4, paddingBottom: 4, paddingLeft: 6, paddingRight: 6, borderWidth: 1, borderColor: "#1f2937", borderRadius: 6, flexShrink: 0 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 4 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: "#94a3b8" } }, k), /* @__PURE__ */ h(
      "div",
      {
        onPointerDown: () => setDraft({ ...draft, [k]: String(defaults[k] ?? "") }),
        style: { fontSize: 9, color: "#93c5fd", paddingLeft: 4, paddingRight: 4, paddingTop: 3, paddingBottom: 3, borderWidth: 1, borderColor: "#334155", borderRadius: 4 }
      },
      "\u6062\u590D"
    )), /* @__PURE__ */ h("div", { style: { height: 34, marginBottom: 4, backgroundColor: "#0b1220", borderWidth: 1, borderColor: "#1f2937", paddingLeft: 6, paddingRight: 6, paddingTop: 4, paddingBottom: 4, flexShrink: 0, overflow: "Hidden" } }, /* @__PURE__ */ h("div", { style: { fontSize: 9, color: "#64748b", whiteSpace: "Normal" } }, "\u9ED8\u8BA4: ", String(defaults[k] ?? ""))), /* @__PURE__ */ h(
      "textfield",
      {
        value: String(draft[k] ?? ""),
        multiline: true,
        "vertical-scroller-visibility": 1,
        onValueChanged: (e) => setDraft({ ...draft, [k]: e.newValue ?? "" }),
        style: { height: k === "SystemPrompt" ? 180 : 88, minHeight: k === "SystemPrompt" ? 180 : 88, maxHeight: k === "SystemPrompt" ? 180 : 88, width: "100%", fontSize: 10, backgroundColor: "#1f2937", borderWidth: 1, borderColor: "#334155", color: "#e2e8f0", paddingLeft: 6, paddingRight: 6, paddingTop: 4, paddingBottom: 4, flexShrink: 0 }
      }
    )) : /* @__PURE__ */ h("div", { key: k, style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 6, backgroundColor: "#111827", paddingTop: 4, paddingBottom: 4, paddingLeft: 6, paddingRight: 6, borderWidth: 1, borderColor: "#1f2937", borderRadius: 6, flexShrink: 0 } }, /* @__PURE__ */ h("div", { style: { width: 126, fontSize: 10, color: "#94a3b8", marginRight: 6 } }, k), /* @__PURE__ */ h(
      "textfield",
      {
        value: String(draft[k] ?? ""),
        onValueChanged: (e) => setDraft({ ...draft, [k]: e.newValue ?? "" }),
        style: { flexGrow: 1, height: 22, fontSize: 10, backgroundColor: "#1f2937", borderWidth: 1, borderColor: "#334155", color: "#e2e8f0", paddingLeft: 6, paddingRight: 6, marginRight: 6 }
      }
    ), /* @__PURE__ */ h("div", { style: { width: 120, fontSize: 9, color: "#64748b", overflow: "Hidden", marginRight: 6 } }, "\u9ED8\u8BA4: ", String(defaults[k] ?? "")), /* @__PURE__ */ h(
      "div",
      {
        onPointerDown: () => setDraft({ ...draft, [k]: String(defaults[k] ?? "") }),
        style: { fontSize: 9, color: "#93c5fd", paddingLeft: 4, paddingRight: 4, paddingTop: 3, paddingBottom: 3, borderWidth: 1, borderColor: "#334155", borderRadius: 4 }
      },
      "\u6062\u590D"
    ));
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, height: "100%", display: "Flex", flexDirection: "Column", backgroundColor: "#0f172a", padding: 10, overflow: "Hidden" } }, /* @__PURE__ */ h("div", { style: { fontSize: 13, color: "#e2e8f0", marginBottom: 8, flexShrink: 0 } }, "AIChat \u914D\u7F6E"), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", marginBottom: 8, flexShrink: 0 } }, pages.map((g, idx) => /* @__PURE__ */ h("div", { key: g, onPointerDown: () => {
      setPage(idx);
      setContentPage(0);
    }, style: { fontSize: 10, color: idx === page ? "#0f172a" : "#cbd5e1", backgroundColor: idx === page ? "#93c5fd" : "#1e293b", paddingLeft: 8, paddingRight: 8, paddingTop: 5, paddingBottom: 5, borderRadius: 6, marginRight: 6 } }, g.replace("1. ", "").replace("2. ", "").replace("3. ", "").replace("4. ", "")))), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: "#93c5fd", marginBottom: 6, flexShrink: 0 } }, currentGroup), /* @__PURE__ */ h(
      "div",
      {
        style: { flexGrow: 1, flexShrink: 1, minHeight: 120, backgroundColor: "#0b1220", borderWidth: 1, borderColor: "#1f2937", borderRadius: 6, padding: 6, overflow: "Hidden" },
        onGeometryChanged: (e) => setContentHeight(Math.max(120, Math.floor(e?.newRect?.height ?? e?.target?.layout?.height ?? 220)))
      },
      displayFields.map(row),
      currentGroup === "4. Persona" && /* @__PURE__ */ h("div", { style: { fontSize: 9, color: "#64748b", marginTop: 4, flexShrink: 0 } }, "SystemPrompt \u5EFA\u8BAE\u624B\u52A8\u7C98\u8D34\u5B8C\u6574\u5185\u5BB9\u540E\u518D\u70B9\u51FB\u786E\u5B9A\u4FDD\u5B58\u3002")
    ), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", marginTop: 8, flexShrink: 0 } }, /* @__PURE__ */ h("div", { onPointerDown: () => setContentPage(Math.max(0, contentPage - 1)), style: { fontSize: 10, color: contentPage > 0 ? "#cbd5e1" : "#475569", padding: "5 8", borderWidth: 1, borderColor: "#334155", borderRadius: 6 } }, "\u4E0A\u4E00\u9875"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: "#64748b" } }, `${Math.min(contentPage, maxContentPage) + 1}/${fieldPages.length}`), /* @__PURE__ */ h("div", { onPointerDown: () => setContentPage(Math.min(maxContentPage, contentPage + 1)), style: { fontSize: 10, color: contentPage < maxContentPage ? "#cbd5e1" : "#475569", padding: "5 8", borderWidth: 1, borderColor: "#334155", borderRadius: 6 } }, "\u4E0B\u4E00\u9875")), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "FlexEnd", marginTop: 8, flexShrink: 0 } }, /* @__PURE__ */ h("div", { onPointerDown: onCancel, style: { fontSize: 11, color: "#cbd5e1", padding: "6 12", marginRight: 8, borderWidth: 1, borderColor: "#475569", borderRadius: 6 } }, "\u53D6\u6D88"), /* @__PURE__ */ h("div", { onPointerDown: onOk, style: { fontSize: 11, color: "#0f172a", backgroundColor: "#93c5fd", padding: "6 12", borderRadius: 6 } }, "\u786E\u5B9A")));
  };
  var ChatPanel = ({ compact = false }) => {
    const [prompt, setPrompt] = useState("");
    const [rec, setRec] = useState(false);
    const [cfgMode, setCfgMode] = useState(false);
    const [draft, setDraft] = useState({});
    const [defaults, setDefaults] = useState({});
    const [status, setStatus] = useState({ available: false });
    const [last, setLast] = useState(null);
    const [token, setToken] = useState("");
    const [vu, setVu] = useState(0);
    useEffect(() => {
      if (!rec) {
        setVu(0);
        return;
      }
      const id = setInterval(() => {
        const t = Date.now() / 160;
        const noise = Math.random() * 0.5;
        const level = Math.max(0.08, Math.min(1, Math.abs(Math.sin(t)) * 0.7 + noise * 0.3));
        setVu(level);
      }, 80);
      return () => clearInterval(id);
    }, [rec]);
    const vuStyle = useMemo(() => ({
      width: compact ? 14 : 18,
      height: compact ? 14 : 18,
      borderRadius: 999,
      backgroundColor: rec ? "#f87171" : "#64748b",
      scale: rec ? 0.9 + vu * 0.9 : 1,
      opacity: rec ? 0.5 + vu * 0.5 : 0.35,
      marginRight: 6,
      borderWidth: 1,
      borderColor: rec ? "#fecaca" : "#94a3b8"
    }), [rec, vu, compact]);
    useEffect(() => {
      setStatus(JSON.parse(chill.aichat.getStatus()));
      setDraft(loadCfg());
      setDefaults(loadDefaults());
      const t = chill.aichat.onConversationCompleted((json) => setLast(JSON.parse(json || "{}")));
      setToken(t);
      return () => {
        if (t)
          chill.aichat.offConversationCompleted(t);
      };
    }, []);
    const send = () => {
      if (!prompt.trim())
        return;
      const r = call("startTextConversation", prompt, "wm-plugin");
      if (r.ok)
        setPrompt("");
    };
    const micDown = () => {
      const r = call("startVoiceCapture");
      if (r.ok)
        setRec(true);
    };
    const micUp = () => {
      const r = call("stopVoiceCaptureAndSend", "wm-plugin");
      setRec(false);
      if (!r.ok)
        console.log(r.error);
    };
    const save = () => {
      let ok = true;
      for (const k of Object.keys(draft)) {
        const r = call("setConfig", k, String(draft[k] ?? ""));
        if (!r.ok)
          ok = false;
      }
      const rs = call("saveConfig");
      setCfgMode(false);
      setStatus(JSON.parse(chill.aichat.getStatus()));
      if (!ok || !rs.ok)
        console.log("\u4FDD\u5B58\u5931\u8D25");
    };
    if (cfgMode && !compact)
      return /* @__PURE__ */ h(SavePanel, { draft, defaults, setDraft, onCancel: () => setCfgMode(false), onOk: save });
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", backgroundColor: "#111827", padding: 10 } }, !compact && /* @__PURE__ */ h(Fragment, null, /* @__PURE__ */ h("div", { style: { fontSize: 11, color: status.available ? "#86efac" : "#fca5a5", marginBottom: 4 } }, status.available ? "AIChat \u5DF2\u8FDE\u63A5" : "AIChat \u672A\u5B89\u88C5"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: "#94a3b8", marginBottom: 8 } }, "busy:", String(!!status.isBusy), " ready:", String(!!status.isReady), " ver:", status.apiVersion || "-")), /* @__PURE__ */ h(
      "textfield",
      {
        text: prompt,
        onValueChanged: (e) => setPrompt(e.newValue ?? ""),
        style: { height: compact ? 34 : 70, fontSize: 12, backgroundColor: "#1f2937", borderWidth: 1, borderColor: "#334155", color: "#e2e8f0", paddingLeft: 8, paddingRight: 8 }
      }
    ), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", marginTop: 8 } }, /* @__PURE__ */ h("div", { onPointerDown: send, style: { fontSize: 11, color: "#111827", backgroundColor: "#93c5fd", padding: "6 10", borderRadius: 6, marginRight: 6 } }, "\u53D1\u9001"), /* @__PURE__ */ h(
      "div",
      {
        onPointerDown: micDown,
        onPointerUp: micUp,
        onPointerLeave: micUp,
        style: { fontSize: 11, color: rec ? "#fff" : "#e2e8f0", backgroundColor: rec ? "#ef4444" : "#334155", padding: "6 10", borderRadius: 6, marginRight: 6, display: "Flex", flexDirection: "Row", alignItems: "Center" }
      },
      /* @__PURE__ */ h("div", { style: vuStyle }),
      rec ? "\u677E\u5F00\u53D1\u9001" : "\u6309\u4F4F\u8BF4\u8BDD"
    ), !compact && /* @__PURE__ */ h("div", { onPointerDown: () => setCfgMode(true), style: { fontSize: 11, color: "#e2e8f0", backgroundColor: "#334155", padding: "6 10", borderRadius: 6 } }, "\u914D\u7F6E")), !compact && last && /* @__PURE__ */ h("div", { style: { marginTop: 8, padding: 6, borderWidth: 1, borderColor: "#1e293b", borderRadius: 6 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: "#93c5fd", marginBottom: 2 } }, `[${last.EmotionTag || "Think"}]`), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: "#e2e8f0", marginBottom: 2 } }, last.VoiceText || ""), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: "#cbd5e1" } }, last.SubtitleText || "")));
  };
  __registerPlugin({
    id: "aichat",
    title: "AIChat",
    width: 520,
    height: 420,
    initialX: 120,
    initialY: 80,
    resizable: true,
    compact: { width: 420, height: 120, component: () => /* @__PURE__ */ h(ChatPanel, { compact: true }) },
    launcher: {
      text: "\uF27A",
      background: "#008055"
    },
    component: () => /* @__PURE__ */ h(ChatPanel, null)
  });
})();
//# sourceMappingURL=app.js.map
