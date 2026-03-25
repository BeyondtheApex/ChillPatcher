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

  // plugins/lyrics/index.tsx
  function parseLRC(lrcText) {
    const lines = [];
    const regex = /\[(\d{1,2}):(\d{2})(?:[.:](\d{1,3}))?\](.*)/;
    for (const raw of lrcText.split("\n")) {
      const match = raw.match(regex);
      if (!match)
        continue;
      const min = parseInt(match[1], 10);
      const sec = parseInt(match[2], 10);
      let ms = 0;
      if (match[3]) {
        ms = match[3].length === 1 ? parseInt(match[3], 10) * 100 : match[3].length === 2 ? parseInt(match[3], 10) * 10 : parseInt(match[3], 10);
      }
      const text = match[4].trim();
      if (!text)
        continue;
      lines.push({ time: min * 60 + sec + ms / 1e3, text });
    }
    lines.sort((a, b) => a.time - b.time);
    return lines;
  }
  function base64Decode(base64) {
    try {
      const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
      let output = "";
      let i = 0;
      const str = base64.replace(/[^A-Za-z0-9+/=]/g, "");
      while (i < str.length) {
        const a = chars.indexOf(str.charAt(i++));
        const b = chars.indexOf(str.charAt(i++));
        const c = chars.indexOf(str.charAt(i++));
        const d = chars.indexOf(str.charAt(i++));
        const n = a << 18 | b << 12 | c << 6 | d;
        output += String.fromCharCode(n >> 16 & 255);
        if (c !== 64)
          output += String.fromCharCode(n >> 8 & 255);
        if (d !== 64)
          output += String.fromCharCode(n & 255);
      }
      let result = "";
      let j = 0;
      while (j < output.length) {
        const cc = output.charCodeAt(j);
        if (cc < 128) {
          result += String.fromCharCode(cc);
          j++;
        } else if (cc < 224) {
          const c2 = output.charCodeAt(j + 1);
          result += String.fromCharCode((cc & 31) << 6 | c2 & 63);
          j += 2;
        } else if (cc < 240) {
          const c2 = output.charCodeAt(j + 1);
          const c3 = output.charCodeAt(j + 2);
          result += String.fromCharCode((cc & 15) << 12 | (c2 & 63) << 6 | c3 & 63);
          j += 3;
        } else {
          const c2 = output.charCodeAt(j + 1);
          const c3 = output.charCodeAt(j + 2);
          const c4 = output.charCodeAt(j + 3);
          const cp = (cc & 7) << 18 | (c2 & 63) << 12 | (c3 & 63) << 6 | c4 & 63;
          result += String.fromCodePoint(cp);
          j += 4;
        }
      }
      return result;
    } catch (e) {
      log("base64Decode error: " + (e?.message || e));
      return "";
    }
  }
  function getCurrentLineIndex(lyrics, currentTime) {
    if (lyrics.length === 0)
      return -1;
    for (let i = lyrics.length - 1; i >= 0; i--) {
      if (currentTime >= lyrics[i].time)
        return i;
    }
    return -1;
  }
  function extractSongInfo(song) {
    if (!song?.uuid)
      return null;
    const qqMatch = song.uuid.match(/^qqmusic_(?:pl\d+_)?(.+)$/);
    if (qqMatch)
      return { source: "qq", id: qqMatch[1], cacheKey: "qq:" + qqMatch[1] };
    if (song.moduleId && song.moduleId.indexOf("netease") >= 0) {
      return { source: "netease", id: song.uuid, cacheKey: "ne:" + song.uuid };
    }
    return null;
  }
  function log(msg) {
    try {
      console.log("[Lyrics] " + msg);
    } catch {
    }
  }
  var BG = "#1e1e2e";
  var TEXT_DIM = "rgba(205, 214, 244, 0.3)";
  var TEXT_SUB = "rgba(205, 214, 244, 0.6)";
  var COMPACT_SIZES = { S: 240, M: 360, L: 480 };
  var _currentCompactWidth = 480;
  var TEXT_NEARBY = "rgba(205, 214, 244, 0.45)";
  var ACCENT = "#89b4fa";
  var lyricsCache = {};
  var useLyricsPoller = () => {
    const [currentIdx, setCurrentIdx] = useState(-1);
    const [lyrics, setLyrics] = useState([]);
    const [statusText, setStatusText] = useState("\u7B49\u5F85\u64AD\u653E...");
    const [title, setTitle] = useState("");
    const [artist, setArtist] = useState("");
    const lyricsRef = useRef([]);
    const currentSongRef = useRef("");
    const lastIdxRef = useRef(-1);
    const loadingRef = useRef(false);
    useEffect(() => {
      const poll = () => {
        try {
          if (loadingRef.current)
            return;
          const songJson = chill.audio.getCurrentSong();
          if (!songJson || songJson === "null")
            return;
          const song = JSON.parse(songJson);
          const info = extractSongInfo(song);
          if (!info)
            return;
          if (info.cacheKey !== currentSongRef.current) {
            currentSongRef.current = info.cacheKey;
            if (song.title)
              setTitle(song.title);
            if (song.artist)
              setArtist(song.artist);
            lastIdxRef.current = -1;
            setCurrentIdx(-1);
            if (lyricsCache[info.cacheKey]) {
              log("cache hit: " + info.cacheKey + " (" + lyricsCache[info.cacheKey].length + " lines)");
              lyricsRef.current = lyricsCache[info.cacheKey];
              setLyrics(lyricsCache[info.cacheKey]);
              setStatusText(lyricsCache[info.cacheKey].length > 0 ? "\u266A" : "\u6682\u65E0\u6B4C\u8BCD");
              return;
            }
            lyricsRef.current = [];
            setLyrics([]);
            setStatusText("\u52A0\u8F7D\u6B4C\u8BCD\u4E2D...");
            log("new song: " + song.title + " source=" + info.source + " id=" + info.id);
            let lrcText = null;
            if (info.source === "qq") {
              const lyricApi = chill.custom.get("lyric");
              if (!lyricApi) {
                currentSongRef.current = "";
                return;
              }
              loadingRef.current = true;
              const b64 = lyricApi.getSongLyric(info.id);
              loadingRef.current = false;
              if (b64)
                lrcText = base64Decode(b64);
            } else if (info.source === "netease") {
              const neteaseApi = chill.custom.get("lyric_netease");
              if (!neteaseApi) {
                log("lyric_netease API not available");
                currentSongRef.current = "";
                setStatusText("\u7B49\u5F85\u6B4C\u8BCD\u63A5\u53E3...");
                return;
              }
              loadingRef.current = true;
              lrcText = neteaseApi.getSongLyric(info.id);
              loadingRef.current = false;
            }
            if (!lrcText) {
              lyricsCache[info.cacheKey] = [];
              setStatusText("\u6682\u65E0\u6B4C\u8BCD");
              return;
            }
            const parsed = parseLRC(lrcText);
            log("parsed lines: " + parsed.length);
            lyricsCache[info.cacheKey] = parsed;
            lyricsRef.current = parsed;
            setLyrics(parsed);
            if (parsed.length === 0) {
              setStatusText("\u6682\u65E0\u6B4C\u8BCD");
            }
            return;
          }
          const cur = lyricsRef.current;
          if (cur.length === 0)
            return;
          const stateJson = chill.audio.getPlaybackState();
          if (!stateJson || stateJson === "null")
            return;
          const state = JSON.parse(stateJson);
          const currentTime = state.currentTime || 0;
          const idx = getCurrentLineIndex(cur, currentTime);
          if (idx !== lastIdxRef.current && idx >= 0) {
            lastIdxRef.current = idx;
            setCurrentIdx(idx);
            setStatusText(cur[idx].text);
          }
        } catch (e) {
          loadingRef.current = false;
          log("poll error: " + (e?.message || e));
        }
      };
      const timer = setInterval(poll, 200);
      return () => clearInterval(timer);
    }, []);
    return { currentIdx, lyrics, statusText, title, artist };
  };
  var LyricsCompact = () => {
    const { statusText, title } = useLyricsPoller();
    const [sizeMode, setSizeMode] = useState("L");
    const rootRef = useRef(null);
    const resizeWindow = (w) => {
      try {
        const el = rootRef.current;
        if (!el)
          return;
        const contentWrapper = el.parentNode;
        const windowContainer = contentWrapper?.parentNode;
        if (!windowContainer)
          return;
        windowContainer.style.width = w;
        const dragClip = windowContainer.childNodes?.[1];
        const pill = dragClip?.childNodes?.[0];
        if (pill?.style) {
          pill.style.left = (w - 40) / 2;
        }
      } catch (e) {
        log("resize error: " + (e?.message || e));
      }
    };
    const handleSize = useCallback((mode) => {
      _currentCompactWidth = COMPACT_SIZES[mode];
      setSizeMode(mode);
      resizeWindow(COMPACT_SIZES[mode]);
    }, []);
    const SizeBtn = useMemo(() => ({ mode }) => /* @__PURE__ */ h(
      "div",
      {
        style: {
          fontSize: 9,
          color: sizeMode === mode ? ACCENT : TEXT_DIM,
          backgroundColor: sizeMode === mode ? "rgba(137,180,250,0.15)" : "rgba(205,214,244,0.08)",
          paddingLeft: 5,
          paddingRight: 5,
          paddingTop: 2,
          paddingBottom: 2,
          borderRadius: 3,
          marginLeft: 3,
          unityFontStyleAndWeight: "Bold"
        },
        onClick: () => handleSize(mode)
      },
      mode
    ), [sizeMode, handleSize]);
    return /* @__PURE__ */ h(
      "div",
      {
        ref: rootRef,
        style: {
          flexGrow: 1,
          display: "Flex",
          flexDirection: "Column",
          justifyContent: "Center",
          backgroundColor: BG,
          paddingLeft: 12,
          paddingRight: 12,
          overflow: "Hidden"
        }
      },
      /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 4 } }, /* @__PURE__ */ h("div", { style: { fontSize: 11, color: TEXT_SUB, overflow: "Hidden", whiteSpace: "NoWrap", unityFontStyleAndWeight: "Bold", flexGrow: 1, flexShrink: 1 } }, title || ""), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", flexShrink: 0 } }, /* @__PURE__ */ h(SizeBtn, { mode: "S" }), /* @__PURE__ */ h(SizeBtn, { mode: "M" }), /* @__PURE__ */ h(SizeBtn, { mode: "L" }))),
      /* @__PURE__ */ h("div", { style: { fontSize: 15, color: ACCENT, overflow: "Hidden", whiteSpace: "NoWrap", unityFontStyleAndWeight: "Bold" } }, statusText)
    );
  };
  var CURRENT_LINE_H = 26;
  var OTHER_LINE_H = 20;
  var HEADER_H = 90;
  function getLineStyle(distance) {
    if (distance === 0)
      return { fontSize: 20, color: ACCENT, unityFontStyleAndWeight: "Bold" };
    const abs = Math.abs(distance);
    if (abs === 1)
      return { fontSize: 14, color: TEXT_NEARBY };
    return { fontSize: 13, color: TEXT_DIM };
  }
  var LyricsCard = () => {
    const { currentIdx, lyrics, statusText, title, artist } = useLyricsPoller();
    const cardRef = useRef(null);
    const [visibleLines, setVisibleLines] = useState(5);
    const currText = currentIdx >= 0 ? lyrics[currentIdx].text : statusText;
    const hasLyrics = lyrics.length > 0 && currentIdx >= 0;
    const getLine = (offset) => {
      const i = currentIdx + offset;
      return hasLyrics && i >= 0 && i < lyrics.length ? lyrics[i].text : "";
    };
    useEffect(() => {
      const check = () => {
        try {
          const h2 = cardRef.current?.ve?.resolvedStyle?.height;
          if (h2 && h2 > 0) {
            const available = h2 - HEADER_H;
            const sideLines = Math.max(0, Math.floor((available - CURRENT_LINE_H) / (2 * OTHER_LINE_H)));
            const total = 1 + sideLines * 2;
            setVisibleLines(Math.max(1, total));
          }
        } catch {
        }
      };
      const timer = setInterval(check, 500);
      check();
      return () => clearInterval(timer);
    }, []);
    const sideCount = Math.floor((visibleLines - 1) / 2);
    const lineOffsets = [];
    for (let i = -sideCount; i <= sideCount; i++) {
      lineOffsets.push(i);
    }
    return /* @__PURE__ */ h(
      "div",
      {
        ref: cardRef,
        style: {
          flexGrow: 1,
          display: "Flex",
          flexDirection: "Column",
          backgroundColor: BG,
          paddingLeft: 24,
          paddingRight: 24,
          paddingTop: 36,
          paddingBottom: 12
        }
      },
      /* @__PURE__ */ h("div", { style: { fontSize: 15, color: TEXT_SUB, unityTextAlign: "MiddleCenter", whiteSpace: "NoWrap", unityFontStyleAndWeight: "Bold", marginBottom: 2 } }, title || ""),
      /* @__PURE__ */ h("div", { style: { fontSize: 12, color: TEXT_DIM, unityTextAlign: "MiddleCenter", whiteSpace: "NoWrap", marginBottom: 6 } }, artist || ""),
      /* @__PURE__ */ h("div", { style: { height: 1, backgroundColor: "rgba(205, 214, 244, 0.15)", marginBottom: 8 } }),
      /* @__PURE__ */ h("div", { style: { flexGrow: 1 } }),
      lineOffsets.map((offset) => {
        const style = getLineStyle(offset);
        return /* @__PURE__ */ h(
          "div",
          {
            key: offset,
            style: {
              ...style,
              unityTextAlign: "MiddleCenter",
              whiteSpace: "NoWrap",
              marginBottom: offset < sideCount ? 4 : 0
            }
          },
          offset === 0 ? currText : getLine(offset)
        );
      }),
      /* @__PURE__ */ h("div", { style: { flexGrow: 1 } })
    );
  };
  __registerPlugin({
    id: "lyrics",
    title: "Lyrics",
    width: 380,
    height: 260,
    initialX: 150,
    initialY: 80,
    resizable: true,
    launcher: {
      text: "\u{F0CB8}",
      background: "#6d28d9"
    },
    component: LyricsCard,
    compact: {
      get width() {
        return _currentCompactWidth;
      },
      height: 60,
      component: LyricsCompact
    }
  });
})();
//# sourceMappingURL=app.js.map
