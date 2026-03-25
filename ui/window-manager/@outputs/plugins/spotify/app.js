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

  // plugins/spotify/index.tsx
  var SPOTIFY_GREEN = "#1db954";
  var BG = "#0b1020";
  var CARD = "#111827";
  var TEXT = "#e5e7eb";
  var DIM = "#94a3b8";
  var BORDER = "rgba(255,255,255,0.08)";
  function getApi() {
    return chill.custom?.get("spotify") ?? null;
  }
  function deviceIcon(type) {
    switch ((type || "").toLowerCase()) {
      case "computer":
        return "\u{F0322}";
      case "smartphone":
        return "\u{F011C}";
      case "speaker":
        return "\u{F04C3}";
      case "tv":
        return "\u{F0502}";
      case "tablet":
        return "\u{F04F6}";
      default:
        return "\u{F075A}";
    }
  }
  var SectionTitle = ({ text }) => /* @__PURE__ */ h("div", { style: {
    fontSize: 11,
    color: DIM,
    marginBottom: 6,
    paddingLeft: 2,
    letterSpacing: 0.5
  } }, text.toUpperCase());
  var Separator = () => /* @__PURE__ */ h("div", { style: { height: 1, backgroundColor: BORDER, marginTop: 10, marginBottom: 10 } });
  var ActionButton = ({ text, onClick, primary = false, disabled = false }) => /* @__PURE__ */ h(
    "div",
    {
      onClick: disabled ? void 0 : onClick,
      style: {
        fontSize: 12,
        color: disabled ? "rgba(255,255,255,0.3)" : primary ? "#fff" : SPOTIFY_GREEN,
        backgroundColor: disabled ? "rgba(255,255,255,0.04)" : primary ? SPOTIFY_GREEN : "rgba(255,255,255,0.06)",
        paddingTop: 7,
        paddingBottom: 7,
        paddingLeft: 16,
        paddingRight: 16,
        borderRadius: 6,
        unityTextAlign: "MiddleCenter"
      }
    },
    text
  );
  var ConfigPanel = ({ onDone }) => {
    const [clientId, setClientId] = useState("");
    const api = getApi();
    const submit = () => {
      if (!clientId || clientId.length < 10)
        return;
      api?.submitClientId(clientId);
      onDone();
    };
    const cancel = () => {
      api?.cancelConfig();
      onDone();
    };
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 16 } }, /* @__PURE__ */ h("div", { style: { fontSize: 15, color: TEXT, marginBottom: 4, unityFontStyleAndWeight: "Bold" } }, "Spotify Configuration"), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: DIM, marginBottom: 12 } }, "Connect your Spotify Developer account"), /* @__PURE__ */ h(Separator, null), /* @__PURE__ */ h(SectionTitle, { text: "Client ID" }), /* @__PURE__ */ h(
      "textfield",
      {
        value: clientId,
        onValueChanged: (e) => setClientId(e.newValue ?? ""),
        style: {
          fontSize: 13,
          color: "#1a1a1a",
          backgroundColor: "rgba(255,255,255,0.06)",
          borderWidth: 0,
          borderRadius: 6,
          paddingTop: 8,
          paddingBottom: 8,
          paddingLeft: 10,
          paddingRight: 10,
          marginBottom: 10
        }
      }
    ), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM, marginBottom: 6 } }, "1. Go to developer.spotify.com/dashboard to create an App"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM, marginBottom: 6 } }, "2. Copy the Client ID and paste it above"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM, marginBottom: 12 } }, "3. Add Redirect URI: fullstop://callback"), /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }), /* @__PURE__ */ h(Separator, null), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" } }, /* @__PURE__ */ h(ActionButton, { text: "Cancel", onClick: cancel }), /* @__PURE__ */ h(
      ActionButton,
      {
        text: "Connect",
        onClick: submit,
        primary: true,
        disabled: !clientId || clientId.length < 10
      }
    )));
  };
  var DevicePanel = ({ onDone }) => {
    const api = getApi();
    const [devices, setDevices] = useState([]);
    const [loading, setLoading] = useState(true);
    const activeId = api?.activeDeviceId ?? "";
    useEffect(() => {
      if (!api)
        return;
      api.refreshDevices();
      const poll = setInterval(() => {
        try {
          const json = api.devicesJson;
          if (json)
            setDevices(JSON.parse(json));
          setLoading(api.isLoadingDevices);
        } catch {
        }
      }, 300);
      return () => clearInterval(poll);
    }, []);
    const selectDevice = (id) => {
      api?.selectDevice(id);
      onDone();
    };
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 16 } }, /* @__PURE__ */ h("div", { style: { fontSize: 15, color: TEXT, marginBottom: 4, unityFontStyleAndWeight: "Bold" } }, "Select Device"), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: DIM, marginBottom: 12 } }, "Choose a Spotify playback device"), /* @__PURE__ */ h(Separator, null), loading ? /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", justifyContent: "Center", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: DIM } }, "Loading devices...")) : devices.length === 0 ? /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", justifyContent: "Center", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 24, color: DIM, marginBottom: 8 } }, "\u{F075A}"), /* @__PURE__ */ h("div", { style: { fontSize: 12, color: DIM, unityTextAlign: "MiddleCenter" } }, "No devices found. Please open Spotify.")) : /* @__PURE__ */ h("div", { style: { flexGrow: 1, overflow: "Scroll" } }, devices.map((d) => {
      const isActive = d.id === activeId || d.isActive;
      return /* @__PURE__ */ h(
        "div",
        {
          key: d.id,
          onClick: () => selectDevice(d.id),
          style: {
            display: "Flex",
            flexDirection: "Row",
            alignItems: "Center",
            backgroundColor: isActive ? "rgba(29,185,84,0.12)" : "rgba(255,255,255,0.04)",
            borderRadius: 8,
            padding: 10,
            marginBottom: 4,
            borderLeftWidth: isActive ? 3 : 0,
            borderLeftColor: SPOTIFY_GREEN
          }
        },
        /* @__PURE__ */ h("div", { style: { fontSize: 20, color: isActive ? SPOTIFY_GREEN : DIM, marginRight: 10, width: 24 } }, deviceIcon(d.type)),
        /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: {
          fontSize: 13,
          color: isActive ? SPOTIFY_GREEN : TEXT,
          unityFontStyleAndWeight: isActive ? "Bold" : "Normal"
        } }, d.name || "Unknown"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM } }, (d.type || "Device") + (d.volume != null ? `  \xB7  Vol ${d.volume}%` : ""))),
        isActive && /* @__PURE__ */ h("div", { style: { fontSize: 10, color: SPOTIFY_GREEN } }, "Active")
      );
    })), /* @__PURE__ */ h(Separator, null), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" } }, /* @__PURE__ */ h(ActionButton, { text: "Refresh", onClick: () => api?.refreshDevices() }), /* @__PURE__ */ h(ActionButton, { text: "Close", onClick: () => {
      api?.cancelDeviceSelection();
      onDone();
    } })));
  };
  var SpotifyMain = () => {
    const [view, setView] = useState("main");
    const [status, setStatus] = useState("");
    const [loggedIn, setLoggedIn] = useState(false);
    const [user, setUser] = useState("");
    const [account, setAccount] = useState("");
    const [device, setDevice] = useState("");
    const [needsConfig, setNeedsConfig] = useState(false);
    const pollRef = useRef(null);
    useEffect(() => {
      const poll = () => {
        const api2 = getApi();
        if (!api2)
          return;
        setStatus(api2.loginStatus || "");
        setLoggedIn(api2.isLoggedIn);
        setUser(api2.userName || "");
        setAccount(api2.accountType || "");
        setDevice(api2.activeDeviceName || "");
        setNeedsConfig(api2.needsClientId);
        if (api2.showConfigPanel && view !== "config") {
          setView("config");
          api2.showConfigPanel = false;
        }
        if (api2.showDevicePanel && view !== "devices") {
          setView("devices");
          api2.showDevicePanel = false;
        }
      };
      pollRef.current = setInterval(poll, 500);
      poll();
      return () => clearInterval(pollRef.current);
    }, [view]);
    const api = getApi();
    const noApi = !api;
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", backgroundColor: BG, padding: 16 } }, view === "config" ? /* @__PURE__ */ h(ConfigPanel, { onDone: () => setView("main") }) : view === "devices" ? /* @__PURE__ */ h(DevicePanel, { onDone: () => setView("main") }) : /* @__PURE__ */ h("div", null, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 12 } }, /* @__PURE__ */ h("div", { style: { fontSize: 22, color: SPOTIFY_GREEN, marginRight: 8 } }, "\u{F04C7}"), /* @__PURE__ */ h("div", { style: { fontSize: 15, color: TEXT, unityFontStyleAndWeight: "Bold" } }, "Spotify")), /* @__PURE__ */ h(Separator, null), noApi ? /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", justifyContent: "Center", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: DIM } }, "Waiting for Spotify module...")) : loggedIn ? (
      // Logged In View
      /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }, /* @__PURE__ */ h(SectionTitle, { text: "Account" }), /* @__PURE__ */ h("div", { style: {
        backgroundColor: CARD,
        borderRadius: 8,
        padding: 12,
        marginBottom: 10
      } }, /* @__PURE__ */ h("div", { style: { fontSize: 13, color: TEXT, marginBottom: 2 } }, user || "Spotify User"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: account === "premium" ? SPOTIFY_GREEN : "#f59e0b" } }, account === "premium" ? "Premium" : "Free (Premium required for playback control)")), /* @__PURE__ */ h(SectionTitle, { text: "Playback Device" }), /* @__PURE__ */ h(
        "div",
        {
          onClick: () => setView("devices"),
          style: {
            backgroundColor: CARD,
            borderRadius: 8,
            padding: 12,
            display: "Flex",
            flexDirection: "Row",
            alignItems: "Center",
            marginBottom: 10
          }
        },
        /* @__PURE__ */ h("div", { style: { fontSize: 16, color: device ? SPOTIFY_GREEN : DIM, marginRight: 8 } }, device ? "\u{F04C3}" : "\u{F075A}"),
        /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: device ? TEXT : DIM } }, device || "No device selected")),
        /* @__PURE__ */ h("div", { style: { fontSize: 12, color: DIM } }, "\u203A")
      ), status ? /* @__PURE__ */ h("div", { style: { fontSize: 11, color: DIM, marginTop: 4 } }, status) : null, /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }), /* @__PURE__ */ h(Separator, null), /* @__PURE__ */ h(ActionButton, { text: "Logout", onClick: () => api?.requestLogout() }))
    ) : (
      // Not Logged In View
      /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column" } }, /* @__PURE__ */ h("div", { style: {
        flexGrow: 1,
        display: "Flex",
        flexDirection: "Column",
        justifyContent: "Center",
        alignItems: "Center"
      } }, /* @__PURE__ */ h("div", { style: { fontSize: 40, color: SPOTIFY_GREEN, marginBottom: 12 } }, "\u{F04C7}"), /* @__PURE__ */ h("div", { style: { fontSize: 13, color: TEXT, marginBottom: 6 } }, needsConfig ? "Client ID Required" : "Not Logged In"), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: DIM, unityTextAlign: "MiddleCenter", marginBottom: 16, paddingLeft: 12, paddingRight: 12 } }, status || (needsConfig ? "Click the button below to configure" : "Click login to connect Spotify"))), /* @__PURE__ */ h(Separator, null), needsConfig ? /* @__PURE__ */ h(ActionButton, { text: "Configure Spotify", onClick: () => {
        api.showConfigPanel = true;
        setView("config");
      }, primary: true }) : /* @__PURE__ */ h(ActionButton, { text: "Login to Spotify", onClick: () => api?.requestLogin(), primary: true }))
    )));
  };
  var SpotifyCompact = () => {
    const [loggedIn, setLoggedIn] = useState(false);
    const [user, setUser] = useState("");
    const [device, setDevice] = useState("");
    const [status, setStatus] = useState("");
    useEffect(() => {
      const poll = setInterval(() => {
        const api = getApi();
        if (!api)
          return;
        setLoggedIn(api.isLoggedIn);
        setUser(api.userName || "");
        setDevice(api.activeDeviceName || "");
        setStatus(api.loginStatus || "");
      }, 800);
      return () => clearInterval(poll);
    }, []);
    return /* @__PURE__ */ h("div", { style: {
      flexGrow: 1,
      display: "Flex",
      flexDirection: "Row",
      alignItems: "Center",
      backgroundColor: BG,
      padding: 12
    } }, /* @__PURE__ */ h("div", { style: { fontSize: 22, color: SPOTIFY_GREEN, marginRight: 10 } }, "\u{F04C7}"), /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: TEXT } }, loggedIn ? user || "Spotify" : "Spotify"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM } }, loggedIn ? device ? `${device}` : "No device selected" : status || "Not logged in")));
  };
  __registerPlugin({
    id: "spotify",
    title: "Spotify",
    width: 300,
    height: 400,
    initialX: 240,
    initialY: 120,
    compact: {
      width: 260,
      height: 60,
      component: SpotifyCompact
    },
    launcher: {
      text: "\uF1BC",
      background: "#1db954"
    },
    component: SpotifyMain
  });
})();
//# sourceMappingURL=app.js.map
