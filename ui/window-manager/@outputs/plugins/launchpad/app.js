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

  // plugins/launchpad/index.tsx
  var BG = "#0b1020";
  var CARD = "#111827";
  var TEXT = "#e5e7eb";
  var DIM = "#94a3b8";
  var ACCENT = "#7dd3fc";
  var defaultCfg = { blur: true, iconScale: 1, compactScale: 1, maxItems: 12 };
  function normalizeCfg(raw) {
    return {
      blur: raw.blur !== false,
      iconScale: Math.max(0.7, Math.min(1.8, Number(raw.iconScale) || defaultCfg.iconScale)),
      compactScale: Math.max(0.6, Math.min(1.8, Number(raw.compactScale) || defaultCfg.compactScale)),
      maxItems: Math.max(4, Math.min(48, Number(raw.maxItems) || defaultCfg.maxItems))
    };
  }
  function getRootRel() {
    const base = String(chill.io.basePath).replace(/\\/g, "/").replace(/\/$/, "");
    const wd = String(chill.workingDir).replace(/\\/g, "/").replace(/\/$/, "");
    return wd.startsWith(base + "/") ? wd.substring(base.length + 1) : "ui/window-manager";
  }
  var cfgPath = `${getRootRel()}/state/launchpad-config.json`;
  var globalState = {
    items: [],
    cfg: loadCfg()
  };
  try {
    const all = __wmPluginControl?.listPlugins?.() || [];
    globalState.items = all.filter((p) => p.id !== "launchpad").sort((a, b) => a.title.localeCompare(b.title));
  } catch {
  }
  function calcCompactSize(items, cfg) {
    const scale = cfg.compactScale;
    const edgePadding = 10;
    const topPadding = Math.round(edgePadding * 3.5);
    const shown = items.slice(0, cfg.maxItems);
    const innerWidth = shown.length * (42 * scale) + Math.max(0, shown.length - 1) * 8;
    return {
      w: Math.max(120, Math.min(900, 20 + innerWidth)),
      h: Math.round(topPadding + 42 * scale + edgePadding)
    };
  }
  function loadCfg() {
    try {
      if (!chill.io.exists(cfgPath))
        return defaultCfg;
      const raw = JSON.parse(chill.io.readText(cfgPath) || "{}");
      return normalizeCfg(raw || {});
    } catch {
      return defaultCfg;
    }
  }
  function saveCfg(cfg) {
    chill.io.writeText(cfgPath, JSON.stringify(cfg, null, 2));
  }
  var usePluginItems = () => {
    const [items, setItems] = useState([]);
    const refresh = useCallback(() => {
      const all = __wmPluginControl?.listPlugins?.() || [];
      const filtered = all.filter((p) => p.id !== "launchpad").sort((a, b) => a.title.localeCompare(b.title));
      setItems(filtered);
      globalState.items = filtered;
      try {
        if (typeof __refreshPlugins === "function") {
          __refreshPlugins();
        }
      } catch {
      }
    }, []);
    useEffect(() => {
      refresh();
      const off = __wmPluginControl?.subscribe?.(refresh);
      return () => {
        if (typeof off === "function")
          off();
      };
    }, [refresh]);
    const toggle = useCallback((id) => {
      __wmPluginControl?.togglePluginVisible?.(id);
    }, []);
    return { items, toggle };
  };
  var LaunchpadCompact = () => {
    const { items, toggle } = usePluginItems();
    const [cfg] = useState(loadCfg);
    const compactSizeRef = useRef({ w: 420, h: 87 });
    const scale = cfg.compactScale;
    const edgePadding = 10;
    const topPadding = Math.round(edgePadding * 3.5);
    useEffect(() => {
      globalState.items = items;
      globalState.cfg = cfg;
    }, [items, cfg]);
    const compactSize = useMemo(() => {
      const shown2 = items.slice(0, cfg.maxItems);
      const innerWidth = shown2.length * (42 * scale) + Math.max(0, shown2.length - 1) * 8;
      return {
        w: Math.max(120, Math.min(900, 20 + innerWidth)),
        h: Math.round(topPadding + 42 * scale + edgePadding)
      };
    }, [items.length, scale, cfg.maxItems, topPadding, edgePadding]);
    useEffect(() => {
      compactSizeRef.current = compactSize;
      try {
        const win = document.body?.firstChild;
        if (win?.style) {
          win.style.width = compactSize.w;
          win.style.height = compactSize.h;
        }
      } catch {
      }
    }, [compactSize]);
    const shown = useMemo(() => items.slice(0, cfg.maxItems), [items, cfg.maxItems]);
    const handleItemClick = useCallback((id) => {
      toggle(id);
    }, [toggle]);
    const content = useMemo(() => /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Row", alignItems: "Center", paddingLeft: edgePadding, paddingRight: edgePadding, paddingTop: topPadding, paddingBottom: edgePadding, overflow: "Hidden" } }, shown.map((item, index) => /* @__PURE__ */ h(
      "div",
      {
        key: item.id,
        onPointerDown: () => handleItemClick(item.id),
        style: {
          width: 42 * scale,
          height: 42 * scale,
          borderRadius: 10 * scale,
          backgroundColor: item.launcher.background,
          marginRight: index === shown.length - 1 ? 0 : 8,
          display: "Flex",
          justifyContent: "Center",
          alignItems: "Center",
          fontSize: 16 * scale,
          color: "#fff",
          opacity: item.enabled ? 1 : 0.35
        }
      },
      item.launcher.text
    ))), [shown, scale, edgePadding, topPadding, handleItemClick]);
    return cfg.blur ? /* @__PURE__ */ h("blur-panel", { downsample: 1, "blur-iterations": 4, interval: 1, tint: "#ffffff1a", style: { flexGrow: 1, display: "Flex", backgroundColor: CARD } }, content) : /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", backgroundColor: CARD } }, content);
  };
  var LaunchpadPanel = () => {
    const { items, toggle } = usePluginItems();
    const [cfg, setCfg] = useState(loadCfg);
    const [showCfg, setShowCfg] = useState(false);
    const patchCfg = useCallback((partial) => {
      const next = normalizeCfg({ ...cfg, ...partial });
      setCfg(next);
      saveCfg(next);
    }, [cfg]);
    const toggleShowCfg = useCallback(() => setShowCfg((prev) => !prev), []);
    const shown = useMemo(() => items.slice(0, cfg.maxItems), [items, cfg.maxItems]);
    const stepBtnStyle = { fontSize: 11, color: "#cbd5e1", marginLeft: 6, marginRight: 6 };
    const iconSize = 56 * cfg.iconScale;
    const marginSize = 10 * cfg.iconScale;
    const handleItemClick = useCallback((id) => {
      toggle(id);
    }, [toggle]);
    const content = useMemo(() => showCfg ? /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", color: TEXT, fontSize: 11, padding: 10 } }, /* @__PURE__ */ h("div", { style: { marginBottom: 8 } }, "Launchpad \u8BBE\u7F6E"), /* @__PURE__ */ h("div", { onPointerDown: () => patchCfg({ blur: !cfg.blur }), style: { marginBottom: 6, color: cfg.blur ? "#86efac" : DIM } }, "\u6BDB\u73BB\u7483: ", cfg.blur ? "\u5F00" : "\u5173"), /* @__PURE__ */ h("div", { style: { marginBottom: 6, display: "Flex", flexDirection: "Row", alignItems: "Center" } }, "\u56FE\u6807\u7F29\u653E ", /* @__PURE__ */ h("div", { onPointerDown: () => patchCfg({ iconScale: Math.round((cfg.iconScale - 0.1) * 10) / 10 }), style: stepBtnStyle }, "-"), " ", cfg.iconScale.toFixed(1), " ", /* @__PURE__ */ h("div", { onPointerDown: () => patchCfg({ iconScale: Math.round((cfg.iconScale + 0.1) * 10) / 10 }), style: stepBtnStyle }, "+")), /* @__PURE__ */ h("div", { style: { marginBottom: 6, display: "Flex", flexDirection: "Row", alignItems: "Center" } }, "\u6298\u53E0\u7F29\u653E ", /* @__PURE__ */ h("div", { onPointerDown: () => patchCfg({ compactScale: Math.round((cfg.compactScale - 0.1) * 10) / 10 }), style: stepBtnStyle }, "-"), " ", cfg.compactScale.toFixed(1), " ", /* @__PURE__ */ h("div", { onPointerDown: () => patchCfg({ compactScale: Math.round((cfg.compactScale + 0.1) * 10) / 10 }), style: stepBtnStyle }, "+")), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, "\u6700\u591A\u5C55\u793A ", /* @__PURE__ */ h("div", { onPointerDown: () => patchCfg({ maxItems: cfg.maxItems - 1 }), style: stepBtnStyle }, "-"), " ", cfg.maxItems, " ", /* @__PURE__ */ h("div", { onPointerDown: () => patchCfg({ maxItems: cfg.maxItems + 1 }), style: stepBtnStyle }, "+"))) : /* @__PURE__ */ h("div", { style: {
      width: "100%",
      height: "100%",
      display: "Flex",
      flexDirection: "Row",
      flexWrap: "Wrap",
      justifyContent: "FlexStart",
      // 修改点：从左侧开始对齐
      alignContent: "Center",
      // 垂直方向依然保持整体居中
      overflow: "Hidden"
    } }, shown.map((item) => /* @__PURE__ */ h(
      "div",
      {
        key: item.id,
        onPointerDown: () => handleItemClick(item.id),
        style: {
          width: iconSize,
          height: iconSize,
          margin: marginSize,
          borderRadius: 14 * cfg.iconScale,
          backgroundColor: item.launcher.background,
          display: "Flex",
          justifyContent: "Center",
          alignItems: "Center",
          opacity: item.enabled ? 1 : 0.35
        }
      },
      /* @__PURE__ */ h("div", { style: {
        color: "#fff",
        fontSize: 28 * cfg.iconScale,
        display: "Flex",
        justifyContent: "Center",
        alignItems: "Center"
      } }, item.launcher.text)
    ))), [showCfg, cfg, shown, iconSize, marginSize, patchCfg, handleItemClick]);
    return /* @__PURE__ */ h(
      "div",
      {
        style: {
          flexGrow: 1,
          display: "Flex",
          flexDirection: "Column",
          backgroundColor: BG,
          paddingLeft: 12,
          paddingRight: 12,
          paddingTop: 10,
          paddingBottom: 10
        }
      },
      /* @__PURE__ */ h("div", { style: { fontSize: 12, color: ACCENT, marginBottom: 8, unityFontStyleAndWeight: "Bold", display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" } }, /* @__PURE__ */ h("div", null, "Launchpad"), /* @__PURE__ */ h("div", { onPointerDown: toggleShowCfg, style: { color: "#cbd5e1" } }, showCfg ? "\u5B8C\u6210" : "\u8BBE\u7F6E")),
      content
    );
  };
  __registerPlugin({
    id: "launchpad",
    title: "Launchpad",
    width: 560,
    height: 220,
    initialX: 80,
    initialY: 120,
    resizable: true,
    canClose: false,
    launcher: {
      text: "\uEB44",
      background: "#0ea5e9"
    },
    compact: {
      get width() {
        try {
          if (globalState.items.length === 0) {
            try {
              const all = __wmPluginControl?.listPlugins?.() || [];
              globalState.items = all.filter((p) => p.id !== "launchpad").sort((a, b) => a.title.localeCompare(b.title));
            } catch {
            }
          }
          const size = calcCompactSize(globalState.items, globalState.cfg);
          return size.w;
        } catch {
          return 420;
        }
      },
      get height() {
        try {
          if (globalState.items.length === 0) {
            try {
              const all = __wmPluginControl?.listPlugins?.() || [];
              globalState.items = all.filter((p) => p.id !== "launchpad").sort((a, b) => a.title.localeCompare(b.title));
            } catch {
            }
          }
          const size = calcCompactSize(globalState.items, globalState.cfg);
          return size.h;
        } catch {
          return 87;
        }
      },
      component: LaunchpadCompact
    },
    component: () => /* @__PURE__ */ h(LaunchpadPanel, null)
  });
})();
//# sourceMappingURL=app.js.map
