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

  // plugins/study-room/index.tsx
  var BG = "#0b1020";
  var CARD = "#111827";
  var PANEL = "#1e293b";
  var TEXT = "#e5e7eb";
  var DIM = "#94a3b8";
  var ACCENT = "#7dd3fc";
  var OK = "#34d399";
  var WARN = "#f59e0b";
  var ERR = "#f87171";
  var BORDER = "rgba(255,255,255,0.08)";
  var LOBBIES_PER_PAGE = 4;
  var MEMBERS_PER_PAGE = 4;
  var json = (v, fb) => {
    try {
      return typeof v === "string" ? JSON.parse(v) : v ?? fb;
    } catch {
      return fb;
    }
  };
  function getSr() {
    return chill?.studyRoom ?? null;
  }
  var Separator = () => /* @__PURE__ */ h("div", { style: { height: 1, backgroundColor: BORDER, marginTop: 8, marginBottom: 8 } });
  var SectionTitle = ({ text }) => /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM, letterSpacing: 0.5, marginBottom: 6, paddingLeft: 2 } }, text.toUpperCase());
  var Btn = ({ text, onClick, primary = false, danger = false, disabled = false }) => {
    const bg = disabled ? "rgba(255,255,255,0.04)" : danger ? "rgba(248,113,113,0.15)" : primary ? "rgba(52,211,153,0.18)" : "rgba(125,211,252,0.12)";
    const color = disabled ? "rgba(255,255,255,0.25)" : danger ? ERR : primary ? OK : ACCENT;
    return /* @__PURE__ */ h(
      "div",
      {
        onPointerDown: disabled ? void 0 : onClick,
        style: {
          fontSize: 12,
          color,
          backgroundColor: bg,
          borderRadius: 6,
          paddingTop: 7,
          paddingBottom: 7,
          paddingLeft: 14,
          paddingRight: 14,
          unityTextAlign: "MiddleCenter"
        }
      },
      text
    );
  };
  var SmallBtn = ({ text, onClick, color = ACCENT }) => /* @__PURE__ */ h(
    "div",
    {
      onPointerDown: onClick,
      style: {
        fontSize: 11,
        color,
        backgroundColor: "rgba(255,255,255,0.06)",
        borderRadius: 5,
        paddingTop: 4,
        paddingBottom: 4,
        paddingLeft: 10,
        paddingRight: 10,
        unityTextAlign: "MiddleCenter"
      }
    },
    text
  );
  var Pager = ({ page, total, onPrev, onNext }) => /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "SpaceBetween", marginTop: 6 } }, /* @__PURE__ */ h(SmallBtn, { text: "\u2039", onClick: onPrev, color: page > 0 ? ACCENT : DIM }), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM } }, `${page + 1} / ${Math.max(1, total)}`), /* @__PURE__ */ h(SmallBtn, { text: "\u203A", onClick: onNext, color: page < total - 1 ? ACCENT : DIM }));
  var LobbyCard = ({ entry, onJoin }) => {
    const stateColor = entry.pomodoroState === "working" ? ACCENT : entry.pomodoroState === "resting" ? OK : DIM;
    const stateText = entry.pomodoroState === "working" ? "\u4E13\u6CE8\u4E2D" : entry.pomodoroState === "resting" ? "\u4F11\u606F\u4E2D" : "\u7A7A\u95F2";
    return /* @__PURE__ */ h("div", { style: {
      backgroundColor: PANEL,
      borderRadius: 8,
      paddingLeft: 12,
      paddingRight: 12,
      paddingTop: 10,
      paddingBottom: 10,
      marginBottom: 6,
      display: "Flex",
      flexDirection: "Row",
      alignItems: "Center"
    } }, /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 3 } }, /* @__PURE__ */ h("div", { style: { fontSize: 13, color: TEXT, unityFontStyleAndWeight: "Bold" } }, entry.hostName), entry.hasPassword && /* @__PURE__ */ h("div", { style: { fontSize: 9, color: WARN, backgroundColor: "rgba(245,158,11,0.12)", borderRadius: 4, paddingLeft: 5, paddingRight: 5, paddingTop: 2, paddingBottom: 2, marginLeft: 6 } }, "\u5BC6\u7801")), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM } }, `${entry.memberCount}/${entry.maxMembers}\u4EBA`), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: stateColor, marginLeft: 8 } }, `\u25CF ${stateText}`))), /* @__PURE__ */ h(SmallBtn, { text: "\u52A0\u5165", onClick: () => onJoin(entry), color: ACCENT }));
  };
  var LobbyView = ({ onSetView, onJoinEntry }) => {
    const [lobbies, setLobbies] = useState([]);
    const [loading, setLoading] = useState(false);
    const [inviteCode, setInviteCode] = useState("");
    const [page, setPage] = useState(0);
    const refresh = useCallback(() => {
      setLoading(true);
      getSr()?.refreshLobbyList?.();
    }, []);
    useEffect(() => {
      const cached = json(getSr()?.getLobbyList?.(), []);
      setLobbies(cached);
      refresh();
    }, []);
    useEffect(() => {
      const t = setInterval(() => {
        const cur = json(getSr()?.getLobbyList?.(), null);
        if (cur) {
          setLobbies(cur);
          setLoading(false);
        }
      }, 1e3);
      return () => clearInterval(t);
    }, []);
    const totalPages = Math.ceil(lobbies.length / LOBBIES_PER_PAGE);
    const pageItems = lobbies.slice(page * LOBBIES_PER_PAGE, (page + 1) * LOBBIES_PER_PAGE);
    const handleJoinByCode = () => {
      const code = inviteCode.trim();
      if (!code)
        return;
      const result = json(getSr()?.searchByInviteCode?.(code), null);
      if (result?.lobbyId) {
        onJoinEntry({ lobbyId: result.lobbyId, hostName: result.hostName || "", memberCount: 0, maxMembers: 8, hasPassword: !!result.hasPassword });
      }
    };
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 14 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 10 } }, /* @__PURE__ */ h("div", { style: { fontSize: 15, color: TEXT, unityFontStyleAndWeight: "Bold", flexGrow: 1 } }, "\u81EA\u4E60\u5BA4"), /* @__PURE__ */ h(SmallBtn, { text: "\u5237\u65B0", onClick: refresh, color: loading ? DIM : ACCENT })), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 8 } }, /* @__PURE__ */ h(
      "textfield",
      {
        value: inviteCode,
        onValueChanged: (e) => setInviteCode(e.newValue ?? ""),
        style: {
          flexGrow: 1,
          fontSize: 12,
          color: TEXT,
          backgroundColor: PANEL,
          borderWidth: 0,
          borderRadius: 6,
          paddingTop: 6,
          paddingBottom: 6,
          paddingLeft: 10,
          paddingRight: 10,
          marginRight: 6
        }
      }
    ), /* @__PURE__ */ h(SmallBtn, { text: "\u641C\u7D22", onClick: handleJoinByCode, color: ACCENT })), /* @__PURE__ */ h(Separator, null), /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column" } }, loading && lobbies.length === 0 ? /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", alignItems: "Center", justifyContent: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: DIM } }, "\u6B63\u5728\u641C\u7D22...")) : pageItems.length === 0 ? /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", alignItems: "Center", justifyContent: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 20, color: DIM, marginBottom: 6 } }, "\u{F0CE0}"), /* @__PURE__ */ h("div", { style: { fontSize: 12, color: DIM } }, "\u6682\u65E0\u81EA\u4E60\u5BA4")) : /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column" } }, pageItems.map((e) => /* @__PURE__ */ h(LobbyCard, { key: e.lobbyId, entry: e, onJoin: onJoinEntry })), Array.from({ length: LOBBIES_PER_PAGE - pageItems.length }).map((_, i) => /* @__PURE__ */ h("div", { key: `pad-${i}`, style: { height: 52, marginBottom: 6 } })))), /* @__PURE__ */ h(
      Pager,
      {
        page,
        total: totalPages,
        onPrev: () => setPage((p) => Math.max(0, p - 1)),
        onNext: () => setPage((p) => Math.min(totalPages - 1, p + 1))
      }
    ), /* @__PURE__ */ h(Separator, null), /* @__PURE__ */ h(Btn, { text: "\u521B\u5EFA\u81EA\u4E60\u5BA4", onClick: () => onSetView("create"), primary: true }));
  };
  var CreateView = ({ onBack, onConnecting }) => {
    const [roomName, setRoomName] = useState("");
    const [password, setPassword] = useState("");
    const [maxMembers, setMaxMembers] = useState(4);
    const [inheritSave, setInheritSave] = useState(true);
    const canCreate = roomName.trim().length > 0;
    const doCreate = () => {
      if (!canCreate)
        return;
      getSr()?.createRoom?.(JSON.stringify({
        roomName: roomName.trim(),
        password,
        maxMembers,
        inheritSave
      }));
      onConnecting();
    };
    const stepMembers = (d) => setMaxMembers((v) => Math.max(2, Math.min(8, v + d)));
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 14 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 10 } }, /* @__PURE__ */ h(SmallBtn, { text: "\u2039", onClick: onBack }), /* @__PURE__ */ h("div", { style: { fontSize: 15, color: TEXT, unityFontStyleAndWeight: "Bold", marginLeft: 8 } }, "\u521B\u5EFA\u81EA\u4E60\u5BA4")), /* @__PURE__ */ h(Separator, null), /* @__PURE__ */ h(SectionTitle, { text: "\u623F\u95F4\u540D\u79F0" }), /* @__PURE__ */ h(
      "textfield",
      {
        value: roomName,
        onValueChanged: (e) => setRoomName(e.newValue ?? ""),
        style: {
          fontSize: 13,
          color: TEXT,
          backgroundColor: PANEL,
          borderWidth: 0,
          borderRadius: 6,
          paddingTop: 8,
          paddingBottom: 8,
          paddingLeft: 10,
          paddingRight: 10,
          marginBottom: 10
        }
      }
    ), /* @__PURE__ */ h(SectionTitle, { text: "\u5BC6\u7801\uFF08\u53EF\u9009\uFF09" }), /* @__PURE__ */ h(
      "textfield",
      {
        value: password,
        onValueChanged: (e) => setPassword(e.newValue ?? ""),
        style: {
          fontSize: 13,
          color: TEXT,
          backgroundColor: PANEL,
          borderWidth: 0,
          borderRadius: 6,
          paddingTop: 8,
          paddingBottom: 8,
          paddingLeft: 10,
          paddingRight: 10,
          marginBottom: 10
        }
      }
    ), /* @__PURE__ */ h(SectionTitle, { text: "\u4EBA\u6570\u4E0A\u9650" }), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 12 } }, /* @__PURE__ */ h(SmallBtn, { text: "-", onClick: () => stepMembers(-1) }), /* @__PURE__ */ h("div", { style: { fontSize: 16, color: TEXT, marginLeft: 14, marginRight: 14, unityFontStyleAndWeight: "Bold" } }, `${maxMembers}`), /* @__PURE__ */ h(SmallBtn, { text: "+", onClick: () => stepMembers(1) }), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: DIM, marginLeft: 10 } }, `\u6700\u591A 8 \u4EBA`)), /* @__PURE__ */ h(
      "div",
      {
        onPointerDown: () => setInheritSave((v) => !v),
        style: {
          display: "Flex",
          flexDirection: "Row",
          alignItems: "Center",
          backgroundColor: PANEL,
          borderRadius: 8,
          paddingLeft: 12,
          paddingRight: 12,
          paddingTop: 10,
          paddingBottom: 10,
          marginBottom: 12
        }
      },
      /* @__PURE__ */ h("div", { style: {
        width: 18,
        height: 18,
        borderWidth: 2,
        borderColor: inheritSave ? ACCENT : DIM,
        borderRadius: 4,
        backgroundColor: inheritSave ? "rgba(125,211,252,0.16)" : "transparent",
        display: "Flex",
        alignItems: "Center",
        justifyContent: "Center",
        marginRight: 10,
        flexShrink: 0
      } }, inheritSave && /* @__PURE__ */ h("div", { style: { fontSize: 11, color: TEXT } }, "\u2713")),
      /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: TEXT } }, "\u7EE7\u627F\u5F53\u524D\u5B58\u6863"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM, marginTop: 2 } }, "\u5C06\u5F85\u529E/\u65E5\u5386/\u5907\u5FD8\u5F55\u7B49\u6570\u636E\u5E26\u5165\u81EA\u4E60\u5BA4"))
    ), /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" } }, /* @__PURE__ */ h(Btn, { text: "\u53D6\u6D88", onClick: onBack }), /* @__PURE__ */ h(Btn, { text: "\u521B\u5EFA", onClick: doCreate, primary: true, disabled: !canCreate })));
  };
  var JoinView = ({ entry, onBack, onConnecting }) => {
    const [password, setPassword] = useState("");
    const doJoin = () => {
      getSr()?.joinRoom?.(entry.lobbyId, password);
      onConnecting();
    };
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 14 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 10 } }, /* @__PURE__ */ h(SmallBtn, { text: "\u2039", onClick: onBack }), /* @__PURE__ */ h("div", { style: { fontSize: 15, color: TEXT, unityFontStyleAndWeight: "Bold", marginLeft: 8 } }, "\u52A0\u5165\u81EA\u4E60\u5BA4")), /* @__PURE__ */ h(Separator, null), /* @__PURE__ */ h("div", { style: { backgroundColor: PANEL, borderRadius: 8, padding: 12, marginBottom: 12 } }, /* @__PURE__ */ h("div", { style: { fontSize: 13, color: TEXT, unityFontStyleAndWeight: "Bold", marginBottom: 2 } }, entry.hostName), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: DIM } }, `${entry.memberCount}/${entry.maxMembers} \u4EBA`)), entry.hasPassword && /* @__PURE__ */ h("div", null, /* @__PURE__ */ h(SectionTitle, { text: "\u623F\u95F4\u5BC6\u7801" }), /* @__PURE__ */ h(
      "textfield",
      {
        value: password,
        onValueChanged: (e) => setPassword(e.newValue ?? ""),
        style: {
          fontSize: 13,
          color: TEXT,
          backgroundColor: PANEL,
          borderWidth: 0,
          borderRadius: 6,
          paddingTop: 8,
          paddingBottom: 8,
          paddingLeft: 10,
          paddingRight: 10,
          marginBottom: 12
        }
      }
    )), /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" } }, /* @__PURE__ */ h(Btn, { text: "\u53D6\u6D88", onClick: onBack }), /* @__PURE__ */ h(Btn, { text: "\u52A0\u5165", onClick: doJoin, primary: true })));
  };
  var ConnectingView = ({ msg }) => /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", alignItems: "Center", justifyContent: "Center", padding: 20 } }, /* @__PURE__ */ h("div", { style: { fontSize: 16, color: DIM, marginBottom: 8 } }, "\u23F3"), /* @__PURE__ */ h("div", { style: { fontSize: 13, color: TEXT, unityTextAlign: "MiddleCenter" } }, msg));
  var RoomView = ({ onSetView }) => {
    const sr = getSr();
    const [members, setMembers] = useState([]);
    const [roomInfo, setRoomInfo] = useState(null);
    const [myInfo, setMyInfo] = useState(null);
    const [syncStates, setSyncStates] = useState({});
    const [membersPage, setMembersPage] = useState(0);
    const [copied, setCopied] = useState(false);
    const refreshMembers = useCallback(() => {
      const m = json(sr?.getMembers?.(), []);
      setMembers(m);
      const states = {};
      for (const mb of m) {
        if (!mb.isHost) {
          states[mb.steamId] = json(sr?.getMemberSyncState?.(mb.steamId), null);
        }
      }
      setSyncStates(states);
    }, []);
    useEffect(() => {
      const info = json(sr?.getRoomInfo?.(), null);
      const me = json(sr?.getMyInfo?.(), null);
      setRoomInfo(info);
      setMyInfo(me);
      refreshMembers();
      const t = setInterval(refreshMembers, 1500);
      return () => clearInterval(t);
    }, []);
    const copyInvite = () => {
      if (!roomInfo?.inviteCode)
        return;
      try {
        chill?.io?.setClipboard?.(roomInfo.inviteCode);
      } catch {
      }
      setCopied(true);
      setTimeout(() => setCopied(false), 2e3);
    };
    const doLeave = () => {
      if (myInfo?.isHost) {
        sr?.closeRoom?.();
      } else {
        sr?.leaveRoom?.();
      }
      onSetView("lobby");
    };
    const totalPages = Math.ceil(members.length / MEMBERS_PER_PAGE);
    const pageMembers = members.slice(membersPage * MEMBERS_PER_PAGE, (membersPage + 1) * MEMBERS_PER_PAGE);
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 14 } }, /* @__PURE__ */ h("div", { style: { backgroundColor: PANEL, borderRadius: 8, paddingLeft: 12, paddingRight: 12, paddingTop: 10, paddingBottom: 10, marginBottom: 10 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 4 } }, /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: { fontSize: 14, color: TEXT, unityFontStyleAndWeight: "Bold" } }, myInfo?.isHost ? "\u6211\u7684\u81EA\u4E60\u5BA4" : "\u81EA\u4E60\u5BA4"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM, marginTop: 2 } }, myInfo?.isHost ? `${members.length} \u4EBA\u5728\u7EBF \xB7 \u4E3B\u673A` : `${members.length} \u4EBA\u5728\u7EBF`)), roomInfo?.inviteCode && /* @__PURE__ */ h(SmallBtn, { text: copied ? "\u5DF2\u590D\u5236" : "\u590D\u5236\u9080\u8BF7\u7801", onClick: copyInvite, color: copied ? OK : ACCENT }))), /* @__PURE__ */ h(SectionTitle, { text: "\u6210\u5458" }), /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column" } }, pageMembers.map((m) => /* @__PURE__ */ h(
      MemberRow,
      {
        key: m.steamId,
        member: m,
        isMe: m.steamId === myInfo?.steamId,
        syncState: syncStates[m.steamId] ?? null
      }
    )), Array.from({ length: MEMBERS_PER_PAGE - pageMembers.length }).map((_, i) => /* @__PURE__ */ h("div", { key: `pad-${i}`, style: { height: 42, marginBottom: 5 } }))), /* @__PURE__ */ h(
      Pager,
      {
        page: membersPage,
        total: totalPages,
        onPrev: () => setMembersPage((p) => Math.max(0, p - 1)),
        onNext: () => setMembersPage((p) => Math.min(totalPages - 1, p + 1))
      }
    ), /* @__PURE__ */ h(Separator, null), /* @__PURE__ */ h(
      Btn,
      {
        text: myInfo?.isHost ? "\u5173\u95ED\u623F\u95F4" : "\u79BB\u5F00\u81EA\u4E60\u5BA4",
        onClick: doLeave,
        danger: true
      }
    ));
  };
  var MemberRow = ({ member, isMe, syncState }) => {
    const connected = syncState?.connected ?? true;
    const latency = syncState?.latencyMs ?? -1;
    const latencyColor = latency < 0 ? DIM : latency < 80 ? OK : latency < 200 ? WARN : ERR;
    const latencyText = latency >= 0 ? `${latency}ms` : "";
    return /* @__PURE__ */ h("div", { style: {
      backgroundColor: PANEL,
      borderRadius: 8,
      paddingLeft: 12,
      paddingRight: 12,
      paddingTop: 8,
      paddingBottom: 8,
      marginBottom: 5,
      display: "Flex",
      flexDirection: "Row",
      alignItems: "Center"
    } }, /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 13, color: TEXT } }, member.personaName), member.isHost && /* @__PURE__ */ h("div", { style: { fontSize: 9, color: ACCENT, backgroundColor: "rgba(125,211,252,0.12)", borderRadius: 4, paddingLeft: 5, paddingRight: 5, paddingTop: 2, paddingBottom: 2, marginLeft: 6 } }, "\u4E3B\u673A"), isMe && /* @__PURE__ */ h("div", { style: { fontSize: 9, color: DIM, marginLeft: 4 } }, "\uFF08\u4F60\uFF09"))), !member.isHost && /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { width: 7, height: 7, borderRadius: 4, backgroundColor: connected ? OK : ERR, marginRight: 5 } }), latencyText !== "" && /* @__PURE__ */ h("div", { style: { fontSize: 10, color: latencyColor } }, latencyText)));
  };
  var StudyRoomPanel = () => {
    const [view, setView] = useState("lobby");
    const [connectingMsg, setConnectingMsg] = useState("\u8FDE\u63A5\u4E2D...");
    const [pendingEntry, setPendingEntry] = useState(null);
    const [errMsg, setErrMsg] = useState("");
    const goConnecting = (msg) => {
      setConnectingMsg(msg);
      setErrMsg("");
      setView("connecting");
    };
    useEffect(() => {
      const sr = getSr();
      if (!sr)
        return;
      const gameTk = chill?.game?.on?.("*", (e) => {
        try {
          const evt = JSON.parse(e);
          if (evt?.name === "sceneReloaded") {
            if (sr.isInRoom?.())
              setView("room");
          }
        } catch {
        }
      });
      const tk = sr.on?.("*", (jsonStr) => {
        try {
          const evt = JSON.parse(jsonStr);
          const name = evt?.name ?? "";
          switch (name) {
            case "roomCreated":
            case "syncReady":
              setErrMsg("");
              setView("room");
              break;
            case "roomLeft":
            case "roomClosed":
              setView("lobby");
              break;
            case "joinFailed":
              setErrMsg(`\u52A0\u5165\u5931\u8D25: ${evt?.payload?.reason ?? "\u672A\u77E5\u9519\u8BEF"}`);
              setView("lobby");
              break;
            case "kicked":
              setErrMsg("\u4F60\u5DF2\u88AB\u79FB\u51FA\u81EA\u4E60\u5BA4");
              setView("lobby");
              break;
            case "connectionLost":
              setConnectingMsg("\u8FDE\u63A5\u4E2D\u65AD\uFF0C\u6B63\u5728\u91CD\u8FDE...");
              setView("connecting");
              break;
            case "reconnected":
              setView("room");
              break;
          }
        } catch {
        }
      });
      if (sr.isInRoom?.())
        setView("room");
      return () => {
        if (tk)
          sr.off?.(tk);
        if (gameTk)
          chill?.game?.off?.(gameTk);
      };
    }, []);
    const handleJoinEntry = (entry) => {
      setPendingEntry(entry);
      if (!entry.hasPassword) {
        getSr()?.joinRoom?.(entry.lobbyId, "");
        goConnecting("\u6B63\u5728\u52A0\u5165...");
      } else {
        setView("join");
      }
    };
    return /* @__PURE__ */ h("div", { style: { flexGrow: 1, display: "Flex", flexDirection: "Column", backgroundColor: BG } }, errMsg !== "" && /* @__PURE__ */ h("div", { style: {
      backgroundColor: "rgba(248,113,113,0.12)",
      borderRadius: 0,
      paddingTop: 6,
      paddingBottom: 6,
      paddingLeft: 14,
      paddingRight: 14
    } }, /* @__PURE__ */ h("div", { style: { fontSize: 11, color: ERR } }, errMsg)), view === "lobby" && /* @__PURE__ */ h(LobbyView, { onSetView: setView, onJoinEntry: handleJoinEntry }), view === "create" && /* @__PURE__ */ h(
      CreateView,
      {
        onBack: () => setView("lobby"),
        onConnecting: () => goConnecting("\u6B63\u5728\u521B\u5EFA\u623F\u95F4...")
      }
    ), view === "join" && pendingEntry && /* @__PURE__ */ h(
      JoinView,
      {
        entry: pendingEntry,
        onBack: () => setView("lobby"),
        onConnecting: () => goConnecting("\u6B63\u5728\u52A0\u5165...")
      }
    ), view === "connecting" && /* @__PURE__ */ h(ConnectingView, { msg: connectingMsg }), view === "room" && /* @__PURE__ */ h(RoomView, { onSetView: setView }));
  };
  var StudyRoomCompact = () => {
    const [inRoom, setInRoom] = useState(false);
    const [memberCount, setMemberCount] = useState(0);
    const [isHost, setIsHost] = useState(false);
    useEffect(() => {
      const poll = setInterval(() => {
        const sr = getSr();
        if (!sr)
          return;
        const active = !!sr.isInRoom?.();
        setInRoom(active);
        if (active) {
          const members = json(sr.getMembers?.(), []);
          setMemberCount(members.length);
          setIsHost(!!sr.isHost?.());
        }
      }, 1500);
      return () => clearInterval(poll);
    }, []);
    return /* @__PURE__ */ h("div", { style: {
      flexGrow: 1,
      display: "Flex",
      flexDirection: "Row",
      alignItems: "Center",
      backgroundColor: CARD,
      paddingLeft: 12,
      paddingRight: 12,
      paddingTop: 8,
      paddingBottom: 8,
      borderRadius: 8
    } }, /* @__PURE__ */ h("div", { style: { fontSize: 18, color: inRoom ? ACCENT : DIM, marginRight: 10 } }, "\u{F0849}"), /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: { fontSize: 11, color: TEXT } }, "Study Room"), /* @__PURE__ */ h("div", { style: { fontSize: 10, color: DIM } }, inRoom ? `${memberCount} \u4EBA\u5728\u7EBF${isHost ? " \xB7 \u4E3B\u673A" : ""}` : "\u672A\u52A0\u5165")), /* @__PURE__ */ h("div", { style: { width: 8, height: 8, borderRadius: 4, backgroundColor: inRoom ? OK : "rgba(255,255,255,0.15)" } }));
  };
  __registerPlugin({
    id: "study-room",
    title: "Study Room",
    width: 320,
    height: 520,
    initialX: 300,
    initialY: 120,
    resizable: false,
    launcher: { text: "\u{F0849}", background: "#6366f1" },
    compact: { width: 180, height: 70, component: StudyRoomCompact },
    component: StudyRoomPanel
  });
})();
//# sourceMappingURL=app.js.map
