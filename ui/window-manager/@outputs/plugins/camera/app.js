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

  // plugins/camera/index.tsx
  var MOVE_SPEED = 2;
  var ROT_SPEED = 60;
  var FOV_SPEED = 20;
  var RES_SPEED = 0.3;
  var BTN_SIZE = 28;
  var BTN_R = 6;
  var BTN_COLOR = "rgba(137,180,250,0.15)";
  var BTN_TEXT = "#89b4fa";
  var LABEL_COLOR = "#6c7086";
  var CAMERAS_DIR = "cameras";
  var ITEMS_PER_PAGE = 8;
  function ensureDir() {
    if (!chill.io.exists(CAMERAS_DIR)) {
      chill.io.writeText(`${CAMERAS_DIR}/.keep`, "");
    }
  }
  function loadAllConfigs() {
    ensureDir();
    try {
      const raw = chill.io.listFiles(CAMERAS_DIR);
      const files = JSON.parse(raw);
      const configs = [];
      for (const f of files) {
        if (f.extension !== ".json")
          continue;
        try {
          const text = chill.io.readText(`${CAMERAS_DIR}/${f.name}`);
          if (text)
            configs.push(JSON.parse(text));
        } catch (_) {
        }
      }
      return configs;
    } catch (_) {
      return [];
    }
  }
  function saveConfig(cfg) {
    const safeName = cfg.name.replace(/[^a-zA-Z0-9_\-\u4e00-\u9fff]/g, "_");
    chill.io.writeText(`${CAMERAS_DIR}/${safeName}.json`, JSON.stringify(cfg, null, 2));
  }
  function deleteConfig(cfg) {
    const safeName = cfg.name.replace(/[^a-zA-Z0-9_\-\u4e00-\u9fff]/g, "_");
    chill.io.deleteFile(`${CAMERAS_DIR}/${safeName}.json`);
  }
  var dynamicWindowIds = /* @__PURE__ */ new Set();
  function registerCameraWindow(cfg) {
    const wid = `cam-${cfg.name}`;
    if (dynamicWindowIds.has(wid))
      return;
    const CamView = () => /* @__PURE__ */ h(
      "camera-view",
      {
        fov: cfg.fov,
        interval: 2,
        "resolution-scale": cfg.res,
        "pos-x": cfg.px,
        "pos-y": cfg.py,
        "pos-z": cfg.pz,
        "rot-x": cfg.rx,
        "rot-y": cfg.ry,
        "near-clip": 0.3,
        "far-clip": 1e3,
        "clear-color": "#000000",
        style: { flexGrow: 1, overflow: "Hidden" }
      }
    );
    __registerPlugin({
      id: wid,
      title: cfg.name,
      width: cfg.windowW || 300,
      height: cfg.windowH || 200,
      initialX: cfg.windowX || 100,
      initialY: cfg.windowY || 100,
      resizable: true,
      component: CamView,
      onGeometryChange: (x, y, w, h2) => {
        cfg.windowX = Math.round(x);
        cfg.windowY = Math.round(y);
        cfg.windowW = Math.round(w);
        cfg.windowH = Math.round(h2);
        saveConfig(cfg);
      },
      // 根据enabled状态设置窗口的visibility
      visible: cfg.enabled
    });
    dynamicWindowIds.add(wid);
  }
  function unregisterCameraWindow(name) {
    const wid = `cam-${name}`;
    if (!dynamicWindowIds.has(wid))
      return;
    __unregisterPlugin(wid);
    dynamicWindowIds.delete(wid);
  }
  var initialConfigs = loadAllConfigs();
  for (const cfg of initialConfigs) {
    if (cfg.enabled)
      registerCameraWindow(cfg);
  }
  var hold = (input, key, val) => ({
    onPointerDown: useCallback(() => {
      input.current[key] = val;
    }, [input, key, val]),
    onPointerUp: useCallback(() => {
      input.current[key] = 0;
    }, [input, key]),
    onPointerLeave: useCallback(() => {
      input.current[key] = 0;
    }, [input, key])
  });
  var Btn = ({ label, input, k, v }) => /* @__PURE__ */ h(
    "div",
    {
      style: {
        width: BTN_SIZE,
        height: BTN_SIZE,
        borderRadius: BTN_R,
        backgroundColor: BTN_COLOR,
        display: "Flex",
        justifyContent: "Center",
        alignItems: "Center",
        fontSize: 12,
        color: BTN_TEXT
      },
      ...hold(input, k, v)
    },
    label
  );
  var CameraEditor = () => {
    const camRef = useRef(null);
    const [configs, setConfigs] = useState(initialConfigs);
    const [page, setPage] = useState(0);
    const [newName, setNewName] = useState("");
    const [posX, setPosX] = useState(0);
    const [posY, setPosY] = useState(1);
    const [posZ, setPosZ] = useState(-10);
    const [rotX, setRotX] = useState(0);
    const [rotY, setRotY] = useState(0);
    const [fov, setFov] = useState(60);
    const [resScale, setResScale] = useState(0.5);
    const input = useRef({ mx: 0, my: 0, mz: 0, rx: 0, ry: 0, fov: 0, res: 0 });
    const state = useRef({ px: 0, py: 1, pz: -10, rx: 0, ry: 0, fov: 60, res: 0.5 });
    const lastTime = useRef(0);
    const mounted = useRef(true);
    useEffect(() => {
      mounted.current = true;
      const getTime = () => typeof CS !== "undefined" ? CS.UnityEngine.Time.realtimeSinceStartupAsDouble : Date.now() / 1e3;
      lastTime.current = getTime();
      const loop = () => {
        if (!mounted.current)
          return;
        const now = getTime();
        const dt = Math.min(now - lastTime.current, 0.1);
        lastTime.current = now;
        const inp = input.current;
        const s = state.current;
        if (inp.mx || inp.my || inp.mz || inp.rx || inp.ry || inp.fov || inp.res) {
          s.ry += inp.ry * ROT_SPEED * dt;
          s.rx = Math.max(-89, Math.min(89, s.rx + inp.rx * ROT_SPEED * dt));
          const rad = s.ry * Math.PI / 180;
          s.px += (Math.sin(rad) * inp.mz + Math.cos(rad) * inp.mx) * MOVE_SPEED * dt;
          s.pz += (Math.cos(rad) * inp.mz - Math.sin(rad) * inp.mx) * MOVE_SPEED * dt;
          s.py += inp.my * MOVE_SPEED * dt;
          s.fov = Math.max(5, Math.min(170, s.fov + inp.fov * FOV_SPEED * dt));
          s.res = Math.max(0.1, Math.min(2, s.res + inp.res * RES_SPEED * dt));
          const ve = camRef.current?.ve;
          if (ve) {
            ve.PosX = s.px;
            ve.PosY = s.py;
            ve.PosZ = s.pz;
            ve.RotX = s.rx;
            ve.RotY = s.ry;
            ve.Fov = s.fov;
            ve.ResolutionScale = s.res;
          }
          setPosX(Math.round(s.px * 10) / 10);
          setPosY(Math.round(s.py * 10) / 10);
          setPosZ(Math.round(s.pz * 10) / 10);
          setRotX(Math.round(s.rx));
          setRotY(Math.round(s.ry));
          setFov(Math.round(s.fov));
          setResScale(Math.round(s.res * 100) / 100);
        }
        requestAnimationFrame(loop);
      };
      requestAnimationFrame(loop);
      return () => {
        mounted.current = false;
      };
    }, []);
    const reloadConfigs = () => {
      setConfigs(loadAllConfigs());
    };
    const handleCreate = () => {
      const name = newName.trim();
      if (!name)
        return;
      if (configs.some((c) => c.name === name))
        return;
      const s = state.current;
      const cfg = {
        name,
        enabled: true,
        px: Math.round(s.px * 100) / 100,
        py: Math.round(s.py * 100) / 100,
        pz: Math.round(s.pz * 100) / 100,
        rx: Math.round(s.rx * 10) / 10,
        ry: Math.round(s.ry * 10) / 10,
        fov: Math.round(s.fov),
        res: Math.round(s.res * 100) / 100,
        windowX: 100 + configs.length * 30,
        windowY: 100 + configs.length * 30,
        windowW: 300,
        windowH: 200
      };
      saveConfig(cfg);
      registerCameraWindow(cfg);
      reloadConfigs();
      setNewName("");
    };
    const handleToggle = (cfg) => {
      cfg.enabled = !cfg.enabled;
      saveConfig(cfg);
      const wid = `cam-${cfg.name}`;
      __wmPluginControl?.togglePluginVisible?.(wid);
      reloadConfigs();
    };
    const handleDelete = (cfg) => {
      if (cfg.enabled) {
        unregisterCameraWindow(cfg.name);
      }
      deleteConfig(cfg);
      reloadConfigs();
    };
    const totalPages = Math.max(1, Math.ceil(configs.length / ITEMS_PER_PAGE));
    const pageItems = configs.slice(page * ITEMS_PER_PAGE, (page + 1) * ITEMS_PER_PAGE);
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Row", backgroundColor: "#1e1e2e" } }, /* @__PURE__ */ h("div", { style: {
      width: 130,
      display: "Flex",
      flexDirection: "Column",
      backgroundColor: "#1e1e2e",
      paddingTop: 6,
      paddingBottom: 6,
      paddingLeft: 6,
      paddingRight: 6
    } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: BTN_TEXT, marginBottom: 4 } }, "\u6444\u50CF\u673A\u5217\u8868"), pageItems.map((cfg) => /* @__PURE__ */ h("div", { key: cfg.name, style: {
      display: "Flex",
      flexDirection: "Row",
      alignItems: "Center",
      marginBottom: 2,
      paddingLeft: 4,
      paddingRight: 2,
      paddingTop: 3,
      paddingBottom: 3,
      backgroundColor: cfg.enabled ? "rgba(137,180,250,0.08)" : "transparent",
      borderRadius: 4
    } }, /* @__PURE__ */ h("div", { style: {
      flexGrow: 1,
      fontSize: 11,
      color: cfg.enabled ? "#141414" : "#ff4343",
      overflow: "Hidden"
    } }, cfg.name), /* @__PURE__ */ h("div", { style: {
      fontSize: 11,
      color: cfg.enabled ? "#a6e3a1" : "#585b70",
      paddingLeft: 4,
      paddingRight: 4
    }, onPointerDown: () => handleToggle(cfg) }, cfg.enabled ? "\u25CF" : "\u25CB"), /* @__PURE__ */ h("div", { style: {
      fontSize: 11,
      color: "#f38ba8",
      paddingLeft: 2,
      paddingRight: 2
    }, onPointerDown: () => handleDelete(cfg) }, "\u2715"))), /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }), totalPages > 1 && /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "Center", marginTop: 4 } }, /* @__PURE__ */ h(
      "div",
      {
        style: { fontSize: 11, color: page > 0 ? BTN_TEXT : LABEL_COLOR, paddingLeft: 6, paddingRight: 6 },
        onPointerDown: () => {
          if (page > 0)
            setPage(page - 1);
        }
      },
      "\u2039"
    ), /* @__PURE__ */ h("div", { style: { fontSize: 9, color: LABEL_COLOR } }, `${page + 1}/${totalPages}`), /* @__PURE__ */ h(
      "div",
      {
        style: { fontSize: 11, color: page < totalPages - 1 ? BTN_TEXT : LABEL_COLOR, paddingLeft: 6, paddingRight: 6 },
        onPointerDown: () => {
          if (page < totalPages - 1)
            setPage(page + 1);
        }
      },
      "\u203A"
    )), /* @__PURE__ */ h("div", { style: { marginTop: 6 } }, /* @__PURE__ */ h(
      "textfield",
      {
        style: {
          fontSize: 10,
          height: 22,
          backgroundColor: "#45475a",
          borderWidth: 1,
          borderColor: "#585b70",
          borderRadius: 4,
          color: "#cdd6f4",
          paddingLeft: 4,
          paddingRight: 4
        },
        value: newName,
        onValueChanged: (e) => setNewName(e.newValue ?? "")
      }
    ), /* @__PURE__ */ h("div", { style: {
      marginTop: 3,
      fontSize: 10,
      color: "#1e1e2e",
      backgroundColor: BTN_TEXT,
      borderRadius: 4,
      paddingTop: 4,
      paddingBottom: 4,
      unityTextAlign: "MiddleCenter"
    }, onPointerDown: handleCreate }, "\u521B\u5EFA"))), /* @__PURE__ */ h(
      "camera-view",
      {
        ref: camRef,
        fov: 60,
        interval: 2,
        "resolution-scale": 0.5,
        "pos-x": 0,
        "pos-y": 1,
        "pos-z": -10,
        "near-clip": 0.3,
        "far-clip": 1e3,
        "clear-color": "#000000",
        style: { flexGrow: 1, overflow: "Hidden" }
      }
    ), /* @__PURE__ */ h("div", { style: {
      display: "Flex",
      flexDirection: "Column",
      backgroundColor: "#1e1e2e",
      paddingTop: 8,
      paddingBottom: 8,
      paddingLeft: 6,
      paddingRight: 6,
      alignItems: "Center"
    } }, /* @__PURE__ */ h("div", { style: { fontSize: 9, color: LABEL_COLOR, marginBottom: 2 } }, "\u5E73\u79FB"), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row" } }, /* @__PURE__ */ h(Btn, { label: "\u25C4", input, k: "mx", v: -1 }), /* @__PURE__ */ h("div", { style: { width: 2 } }), /* @__PURE__ */ h(Btn, { label: "\u25B2", input, k: "mz", v: 1 }), /* @__PURE__ */ h("div", { style: { width: 2 } }), /* @__PURE__ */ h(Btn, { label: "\u25BC", input, k: "mz", v: -1 }), /* @__PURE__ */ h("div", { style: { width: 2 } }), /* @__PURE__ */ h(Btn, { label: "\u25BA", input, k: "mx", v: 1 })), /* @__PURE__ */ h("div", { style: { fontSize: 9, color: LABEL_COLOR, marginTop: 6, marginBottom: 2 } }, "\u9AD8\u5EA6"), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row" } }, /* @__PURE__ */ h(Btn, { label: "\u2193", input, k: "my", v: -1 }), /* @__PURE__ */ h("div", { style: { width: 2 } }), /* @__PURE__ */ h(Btn, { label: "\u2191", input, k: "my", v: 1 })), /* @__PURE__ */ h("div", { style: { fontSize: 9, color: LABEL_COLOR, marginTop: 6, marginBottom: 2 } }, "\u89C6\u89D2"), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row" } }, /* @__PURE__ */ h(Btn, { label: "\u25C4", input, k: "ry", v: -1 }), /* @__PURE__ */ h("div", { style: { width: 2 } }), /* @__PURE__ */ h(Btn, { label: "\u25B2", input, k: "rx", v: -1 }), /* @__PURE__ */ h("div", { style: { width: 2 } }), /* @__PURE__ */ h(Btn, { label: "\u25BC", input, k: "rx", v: 1 }), /* @__PURE__ */ h("div", { style: { width: 2 } }), /* @__PURE__ */ h(Btn, { label: "\u25BA", input, k: "ry", v: 1 })), /* @__PURE__ */ h("div", { style: { fontSize: 9, color: LABEL_COLOR, marginTop: 6, marginBottom: 2 } }, "FOV"), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h(Btn, { label: "\u2212", input, k: "fov", v: -1 }), /* @__PURE__ */ h("div", { style: { fontSize: 9, color: "#cdd6f4", marginLeft: 4, marginRight: 4, width: 28, unityTextAlign: "MiddleCenter" } }, `${fov}\xB0`), /* @__PURE__ */ h(Btn, { label: "+", input, k: "fov", v: 1 })), /* @__PURE__ */ h("div", { style: { fontSize: 9, color: LABEL_COLOR, marginTop: 6, marginBottom: 2 } }, "\u5206\u8FA8\u7387"), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h(Btn, { label: "\u2212", input, k: "res", v: -1 }), /* @__PURE__ */ h("div", { style: { fontSize: 9, color: "#cdd6f4", marginLeft: 4, marginRight: 4, width: 28, unityTextAlign: "MiddleCenter" } }, `${resScale}`), /* @__PURE__ */ h(Btn, { label: "+", input, k: "res", v: 1 })), /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }), /* @__PURE__ */ h("div", { style: { fontSize: 8, color: LABEL_COLOR, unityTextAlign: "MiddleCenter" } }, `\u4F4D\u7F6E: ${posX}, ${posY}, ${posZ}`), /* @__PURE__ */ h("div", { style: { fontSize: 8, color: LABEL_COLOR, marginTop: 2, unityTextAlign: "MiddleCenter" } }, `\u89D2\u5EA6: ${rotX}, ${rotY}`)));
  };
  __registerPlugin({
    id: "camera",
    title: "Camera Editor",
    width: 480,
    height: 340,
    initialX: 50,
    initialY: 50,
    resizable: true,
    launcher: {
      text: "\uF03D",
      background: "#c9860b"
    },
    component: CameraEditor
  });
})();
//# sourceMappingURL=app.js.map
