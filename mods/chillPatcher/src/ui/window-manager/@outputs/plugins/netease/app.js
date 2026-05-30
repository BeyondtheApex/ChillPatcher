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

  // plugins/netease/index.tsx
  var NETEASE_RED = "#e7515a";
  var BG = "#0b1020";
  var CARD = "#111827";
  var TEXT = "#e5e7eb";
  var DIM = "#94a3b8";
  var BORDER = "rgba(255,255,255,0.08)";
  function getApi() {
    return chill.custom?.get("netease_account") ?? null;
  }
  function vipLabel(vipType) {
    return vipType > 0 ? "VIP" : "\u514D\u8D39\u7528\u6237";
  }
  function vipColor(vipType) {
    return vipType > 0 ? NETEASE_RED : DIM;
  }
  function statusLabel(state) {
    switch (state) {
      case "logged_in":
        return "\u5DF2\u767B\u5F55";
      case "logged_out":
        return "\u672A\u767B\u5F55";
      case "expired":
        return "\u767B\u5F55\u5DF2\u8FC7\u671F";
      case "logging_in":
        return "\u626B\u7801\u4E2D...";
      default:
        return "\u672A\u77E5";
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
        color: disabled ? "rgba(255,255,255,0.3)" : primary ? "#fff" : NETEASE_RED,
        backgroundColor: disabled ? "rgba(255,255,255,0.04)" : primary ? NETEASE_RED : "rgba(255,255,255,0.06)",
        paddingTop: 7,
        paddingBottom: 7,
        paddingLeft: 16,
        paddingRight: 16,
        borderRadius: 6,
        flexGrow: 1,
        marginLeft: 4,
        marginRight: 4,
        unityTextAlign: "MiddleCenter"
      }
    },
    text
  );
  var AccountInfo = ({ state, nickname, avatar, vip }) => /* @__PURE__ */ h("div", { style: {
    backgroundColor: CARD,
    borderRadius: 8,
    padding: 12,
    marginBottom: 10,
    display: "Flex",
    flexDirection: "Column",
    alignItems: "Center"
  } }, state === "logged_in" && avatar ? /* @__PURE__ */ h(
    "img",
    {
      src: avatar,
      style: {
        width: 48,
        height: 48,
        borderRadius: 24,
        marginBottom: 8
      }
    }
  ) : null, /* @__PURE__ */ h("div", { style: { fontSize: 13, color: TEXT, marginBottom: 2, unityTextAlign: "MiddleCenter" } }, state === "logged_in" ? nickname || "\u7F51\u6613\u4E91\u7528\u6237" : statusLabel(state)), /* @__PURE__ */ h("div", { style: {
    fontSize: 10,
    color: state === "logged_in" ? vipColor(vip) : DIM,
    unityTextAlign: "MiddleCenter"
  } }, state === "logged_in" ? vipLabel(vip) : statusLabel(state)));
  var LoginGuide = ({ status, state }) => /* @__PURE__ */ h("div", { style: {
    display: "Flex",
    flexDirection: "Column",
    alignItems: "Center",
    marginTop: 6,
    marginBottom: 6
  } }, /* @__PURE__ */ h("div", { style: {
    backgroundColor: CARD,
    borderRadius: 8,
    padding: 16,
    marginBottom: 8
  } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: TEXT, unityTextAlign: "MiddleCenter", marginBottom: 6 } }, state === "expired" ? "\u8BF7\u91CD\u542F\u6E38\u620F\u4EE5\u91CD\u65B0\u767B\u5F55" : status === "\u7B49\u5F85\u626B\u7801" ? "\u8BF7\u7528\u7F51\u6613\u4E91 APP \u626B\u63CF\u5C01\u9762\u533A\u57DF\u7684\u4E8C\u7EF4\u7801" : "\u8BF7\u5728\u64AD\u653E\u5217\u8868\u4E2D\u70B9\u51FB\u300C\u7F51\u6613\u4E91\u626B\u7801\u767B\u5F55\u300D"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM, unityTextAlign: "MiddleCenter" } }, state === "expired" ? "\u91CD\u542F\u540E\u53EF\u5728\u64AD\u653E\u5217\u8868\u4E2D\u626B\u7801\u767B\u5F55" : status === "\u7B49\u5F85\u626B\u7801" ? "\u626B\u7801\u540E\u5728\u624B\u673A\u4E0A\u786E\u8BA4\u767B\u5F55" : "\u4E8C\u7EF4\u7801\u5C06\u663E\u793A\u5728\u5C01\u9762\u533A\u57DF")));
  var ActionButtons = ({ state }) => {
    const api = getApi();
    if (!api)
      return null;
    const isLoggingIn = state === "logging_in";
    switch (state) {
      case "logged_in":
        return /* @__PURE__ */ h("div", null, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" } }, /* @__PURE__ */ h(ActionButton, { text: "\u5237\u65B0\u767B\u5F55\u6001", onClick: () => api.refreshLogin() }), /* @__PURE__ */ h(ActionButton, { text: "\u767B\u51FA", onClick: () => api.logout() })));
      case "logged_out":
      case "expired":
      case "logging_in":
        return null;
      default:
        return null;
    }
  };
  var NeteaseMain = () => {
    const [state, setState] = useState("logged_out");
    const [nickname, setNickname] = useState("");
    const [avatar, setAvatar] = useState("");
    const [vip, setVip] = useState(0);
    const [status, setStatus] = useState("");
    useEffect(() => {
      const poll = () => {
        const api2 = getApi();
        if (!api2)
          return;
        setState(api2.sessionState || "logged_out");
        setNickname(api2.nickname || "");
        setAvatar(api2.avatarUrl || "");
        setVip(api2.vipType || 0);
        setStatus(api2.statusMessage || "");
      };
      const timer = setInterval(poll, 500);
      poll();
      return () => clearInterval(timer);
    }, []);
    const api = getApi();
    const noApi = !api;
    const showLogin = state === "logged_out" || state === "expired" || state === "logging_in";
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", backgroundColor: BG, padding: 16 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 12 } }, /* @__PURE__ */ h("div", { style: { fontSize: 22, color: NETEASE_RED, marginRight: 8 } }, "\u{F0386}"), /* @__PURE__ */ h("div", { style: { fontSize: 15, color: TEXT, unityFontStyleAndWeight: "Bold" } }, "\u7F51\u6613\u4E91\u97F3\u4E50")), /* @__PURE__ */ h(Separator, null), noApi ? /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", justifyContent: "Center", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: DIM } }, "\u7B49\u5F85\u7F51\u6613\u4E91\u6A21\u5757\u52A0\u8F7D...")) : /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }, /* @__PURE__ */ h(SectionTitle, { text: "\u8D26\u53F7" }), /* @__PURE__ */ h(AccountInfo, { state, nickname, avatar, vip }), showLogin && /* @__PURE__ */ h("div", null, /* @__PURE__ */ h(SectionTitle, { text: "\u767B\u5F55" }), /* @__PURE__ */ h(LoginGuide, { status, state })), state === "logged_in" && status ? /* @__PURE__ */ h("div", { style: { fontSize: 11, color: DIM, marginTop: 4 } }, status) : null, /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }), /* @__PURE__ */ h(Separator, null), /* @__PURE__ */ h(ActionButtons, { state })));
  };
  __registerPlugin({
    id: "netease",
    title: "\u7F51\u6613\u4E91\u97F3\u4E50",
    width: 280,
    height: 360,
    initialX: 260,
    initialY: 140,
    launcher: {
      text: "\u{F0386}",
      background: "#e7515a"
    },
    component: NeteaseMain
  });
})();
//# sourceMappingURL=app.js.map
