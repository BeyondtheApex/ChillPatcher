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

  // plugins/weather/index.tsx
  var weatherLat = chill.config.appGetOrCreate("Weather.Latitude", 39.9, "\u5929\u6C14\u67E5\u8BE2\u7EAC\u5EA6 (\u4F8B: 39.9 = \u5317\u4EAC)");
  var weatherLon = chill.config.appGetOrCreate("Weather.Longitude", 116.4, "\u5929\u6C14\u67E5\u8BE2\u7ECF\u5EA6 (\u4F8B: 116.4 = \u5317\u4EAC)");
  var weatherLocation = chill.config.appGetOrCreate("Weather.LocationName", "\u5317\u4EAC", "\u663E\u793A\u7684\u5730\u70B9\u540D\u79F0");
  var WEATHER_API = `https://api.open-meteo.com/v1/forecast?latitude=${weatherLat}&longitude=${weatherLon}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto`;
  var getWeatherInfo = (code) => {
    switch (true) {
      case code === 0:
        return { text: "\u6674\u6717", icon: "\u{F0599}", bg: "#2563eb" };
      case code === 1:
        return { text: "\u6674\u95F4\u591A\u4E91", icon: "\u{F0595}", bg: "#3b82f6" };
      case code === 2:
        return { text: "\u591A\u4E91", icon: "\u{F0590}", bg: "#475569" };
      case code === 3:
        return { text: "\u9634\u5929", icon: "\u{F015F}", bg: "#334155" };
      case (code === 45 || code === 48):
        return { text: "\u96FE", icon: "\u{F0591}", bg: "#64748b" };
      case [51, 53, 55, 56, 57].includes(code):
        return { text: "\u6BDB\u6BDB\u96E8", icon: "\u{F0597}", bg: "#2c4a6b" };
      case (code === 61 || code === 63):
        return { text: "\u5C0F\u5230\u4E2D\u96E8", icon: "\u{F0597}", bg: "#1e3a5f" };
      case (code === 65 || code === 66 || code === 67):
        return { text: "\u5927\u96E8/\u66B4\u96E8", icon: "\u{F0596}", bg: "#152a45" };
      case [80, 81, 82].includes(code):
        return { text: "\u9635\u96E8", icon: "\u{F0597}", bg: "#224166" };
      case (code === 71 || code === 73):
        return { text: "\u5C0F\u5230\u4E2D\u96EA", icon: "\u{F0598}", bg: "#4a6078" };
      case (code === 75 || code === 77 || code === 85 || code === 86):
        return { text: "\u5927\u96EA/\u66B4\u96EA", icon: "\u{F0F36}", bg: "#3a4c61" };
      case code === 95:
        return { text: "\u96F7\u66B4", icon: "\u{F0593}", bg: "#1e293b" };
      case (code === 96 || code === 99):
        return { text: "\u96F7\u9635\u96E8/\u51B0\u96F9", icon: "\u{F0592}", bg: "#0f172a" };
      default:
        return { text: "\u672A\u77E5", icon: "\u{F0A39}", bg: "#6b7280" };
    }
  };
  var getDayName = (dateStr, index) => {
    if (index === 0)
      return "\u4ECA\u5929";
    const date = new Date(dateStr);
    const days = ["\u5468\u65E5", "\u5468\u4E00", "\u5468\u4E8C", "\u5468\u4E09", "\u5468\u56DB", "\u5468\u4E94", "\u5468\u516D"];
    return days[date.getDay()];
  };
  function useAnimationFrame(callback) {
    const phaseRef = useRef(0);
    useEffect(() => {
      let mounted = true;
      let frameId;
      const loop = () => {
        if (!mounted)
          return;
        phaseRef.current += 1;
        callback(phaseRef.current);
        frameId = requestAnimationFrame(loop);
      };
      frameId = requestAnimationFrame(loop);
      return () => {
        mounted = false;
        cancelAnimationFrame(frameId);
      };
    }, []);
  }
  var FloatingIcon = ({ icon, size = 52 }) => {
    const [offsetY, setOffsetY] = useState(0);
    useAnimationFrame((frame) => {
      setOffsetY(Math.sin(frame * 0.03) * 4);
    });
    return /* @__PURE__ */ h(
      "div",
      {
        style: {
          fontSize: size,
          color: "rgba(255,255,255,0.9)",
          translate: `0 ${Math.round(offsetY)}px`
        }
      },
      icon
    );
  };
  var LoadingView = () => {
    const [rotation, setRotation] = useState(0);
    const [pulse, setPulse] = useState(0.3);
    useAnimationFrame((frame) => {
      setRotation(frame * 5 % 360);
      setPulse(0.3 + Math.sin(frame * 0.04) * 0.35);
    });
    return /* @__PURE__ */ h(
      "div",
      {
        style: {
          flexGrow: 1,
          justifyContent: "Center",
          alignItems: "Center",
          display: "Flex",
          flexDirection: "Column",
          backgroundColor: "#1e293b"
        }
      },
      /* @__PURE__ */ h(
        "div",
        {
          style: {
            fontSize: 36,
            color: "#89b4fa",
            rotate: rotation,
            marginBottom: 16
          }
        },
        "\u{F0453}"
      ),
      /* @__PURE__ */ h("div", { style: { fontSize: 13, color: "#ffffff", opacity: pulse } }, "\u52A0\u8F7D\u4E2D...")
    );
  };
  var ErrorView = ({
    message,
    onRetry
  }) => /* @__PURE__ */ h(
    "div",
    {
      style: {
        flexGrow: 1,
        justifyContent: "Center",
        alignItems: "Center",
        display: "Flex",
        flexDirection: "Column",
        backgroundColor: "#1e293b",
        paddingLeft: 20,
        paddingRight: 20
      }
    },
    /* @__PURE__ */ h("div", { style: { fontSize: 14, color: "#f87171", marginBottom: 8 } }, "\u51FA\u9519\u4E86"),
    /* @__PURE__ */ h(
      "div",
      {
        style: {
          fontSize: 11,
          color: "rgba(255,255,255,0.5)",
          marginBottom: 16,
          unityTextAlign: "MiddleCenter"
        }
      },
      message
    ),
    /* @__PURE__ */ h(
      "div",
      {
        style: {
          fontSize: 12,
          color: "#89b4fa",
          paddingTop: 6,
          paddingBottom: 6,
          paddingLeft: 16,
          paddingRight: 16,
          borderRadius: 6,
          borderWidth: 1,
          borderColor: "#89b4fa"
        },
        onPointerDown: onRetry
      },
      "\u91CD\u8BD5"
    )
  );
  function fetchWeather(callback) {
    chill.net.get(WEATHER_API, (resultJson) => {
      try {
        const res = JSON.parse(resultJson);
        if (res.ok && res.body) {
          const api = JSON.parse(res.body);
          const dailyData = [];
          if (api.daily && api.daily.time) {
            for (let i = 0; i < 7 && i < api.daily.time.length; i++) {
              dailyData.push({
                date: api.daily.time[i],
                code: api.daily.weather_code[i],
                maxTemp: api.daily.temperature_2m_max[i],
                minTemp: api.daily.temperature_2m_min[i]
              });
            }
          }
          callback(
            {
              temperature: api.current.temperature_2m,
              humidity: api.current.relative_humidity_2m,
              windSpeed: api.current.wind_speed_10m,
              weatherCode: api.current.weather_code,
              location: weatherLocation,
              daily: dailyData
            },
            null
          );
        } else {
          callback(null, res.error || `HTTP ${res.status}`);
        }
      } catch (e) {
        callback(null, e.message || "\u89E3\u6790\u5931\u8D25");
      }
    });
  }
  var WeatherCompact = () => {
    const [loading, setLoading] = useState(true);
    const [weather, setWeather] = useState(null);
    const [error, setError] = useState(null);
    const doFetch = useCallback(() => {
      setLoading(true);
      setError(null);
      fetchWeather((data, err) => {
        setLoading(false);
        if (data)
          setWeather(data);
        else
          setError(err);
      });
    }, []);
    useEffect(() => {
      doFetch();
    }, [doFetch]);
    if (loading || error || !weather) {
      return /* @__PURE__ */ h(
        "div",
        {
          style: {
            flexGrow: 1,
            justifyContent: "Center",
            alignItems: "Center",
            display: "Flex",
            backgroundColor: "#1e293b"
          }
        },
        /* @__PURE__ */ h("div", { style: { fontSize: 14, color: "rgba(255,255,255,0.5)" } }, loading ? "\u83B7\u53D6\u5929\u6C14\u4E2D..." : "\u4FE1\u606F\u9519\u8BEF")
      );
    }
    const info = getWeatherInfo(weather.weatherCode);
    return /* @__PURE__ */ h(
      "div",
      {
        style: {
          flexGrow: 1,
          display: "Flex",
          flexDirection: "Row",
          alignItems: "Center",
          justifyContent: "SpaceBetween",
          backgroundColor: info.bg,
          paddingLeft: 20,
          paddingRight: 20
        }
      },
      /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 38, color: "rgba(255,255,255,0.9)", marginRight: 12 } }, info.icon), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Column", justifyContent: "Center" } }, /* @__PURE__ */ h("div", { style: { fontSize: 32, color: "#ffffff", unityFontStyleAndWeight: "Bold", marginBottom: -4 } }, `${Math.round(weather.temperature)}\xB0`), /* @__PURE__ */ h("div", { style: { fontSize: 13, color: "rgba(255,255,255,0.8)" } }, info.text))),
      /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Column", alignItems: "FlexEnd" } }, /* @__PURE__ */ h("div", { style: { fontSize: 16, color: "#ffffff", unityFontStyleAndWeight: "Bold", marginBottom: 6, letterSpacing: 1 } }, weather.location), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: "rgba(255,255,255,0.7)", marginBottom: 2 } }, `\u98CE\u901F ${weather.windSpeed} km/h`), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: "rgba(255,255,255,0.7)" } }, `\u6E7F\u5EA6 ${weather.humidity}%`))
    );
  };
  var WeatherContent = ({ data }) => {
    const info = getWeatherInfo(data.weatherCode);
    return /* @__PURE__ */ h(
      "div",
      {
        style: {
          flexGrow: 1,
          display: "Flex",
          flexDirection: "Column",
          backgroundColor: info.bg,
          paddingTop: 16,
          paddingBottom: 16,
          paddingLeft: 20,
          paddingRight: 20,
          transitionProperty: "background-color",
          transitionDuration: "0.8s",
          transitionTimingFunction: "ease-in-out"
        }
      },
      /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 16 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Column", flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 8 } }, /* @__PURE__ */ h("div", { style: { fontSize: 10, color: "rgba(255,255,255,0.6)", marginRight: 4 } }, "\u25C9"), /* @__PURE__ */ h("div", { style: { fontSize: 14, color: "rgba(255,255,255,0.85)", letterSpacing: 1 } }, data.location)), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "FlexEnd" } }, /* @__PURE__ */ h("div", { style: { fontSize: 42, color: "#ffffff", unityFontStyleAndWeight: "Bold", whiteSpace: "NoWrap" } }, `${Math.round(data.temperature)}\xB0`), /* @__PURE__ */ h("div", { style: { fontSize: 16, color: "rgba(255,255,255,0.9)", marginLeft: 8, marginBottom: 4, whiteSpace: "NoWrap" } }, info.text))), /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Column", alignItems: "Center", flexShrink: 0, width: 100 } }, /* @__PURE__ */ h(FloatingIcon, { icon: info.icon, size: 42 }), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: "rgba(255,255,255,0.7)", marginTop: 6, whiteSpace: "NoWrap" } }, `\u98CE\u901F ${data.windSpeed}km/h`), /* @__PURE__ */ h("div", { style: { fontSize: 11, color: "rgba(255,255,255,0.7)", marginTop: 2, whiteSpace: "NoWrap" } }, `\u6E7F\u5EA6 ${data.humidity}%`))),
      /* @__PURE__ */ h("div", { style: { height: 1, backgroundColor: "rgba(255,255,255,0.15)", marginBottom: 12 } }),
      /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Column", flexGrow: 1 } }, /* @__PURE__ */ h("div", { style: { fontSize: 12, color: "rgba(255,255,255,0.6)", marginBottom: 8, letterSpacing: 1 } }, "7 \u5929\u5929\u6C14\u9884\u62A5"), data.daily.map((day, index) => {
        const dayInfo = getWeatherInfo(day.code);
        return /* @__PURE__ */ h(
          "div",
          {
            key: index,
            style: {
              display: "Flex",
              flexDirection: "Row",
              justifyContent: "SpaceBetween",
              alignItems: "Center",
              paddingTop: 6,
              paddingBottom: 6,
              borderBottomWidth: index === 6 ? 0 : 1,
              borderBottomColor: "rgba(255,255,255,0.08)"
            }
          },
          /* @__PURE__ */ h("div", { style: { fontSize: 14, color: index === 0 ? "#ffffff" : "rgba(255,255,255,0.8)", width: 60, flexShrink: 0, whiteSpace: "NoWrap" } }, getDayName(day.date, index)),
          /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", alignItems: "Center", width: 80, flexShrink: 0 } }, /* @__PURE__ */ h("div", { style: { fontSize: 16, color: "rgba(255,255,255,0.9)", marginRight: 6 } }, dayInfo.icon), /* @__PURE__ */ h("div", { style: { fontSize: 12, color: "rgba(255,255,255,0.7)", whiteSpace: "NoWrap" } }, dayInfo.text)),
          /* @__PURE__ */ h("div", { style: { display: "Flex", flexDirection: "Row", justifyContent: "FlexEnd", alignItems: "Center", width: 90, flexShrink: 0 } }, /* @__PURE__ */ h("div", { style: { fontSize: 14, color: "#ffffff", whiteSpace: "NoWrap" } }, `${Math.round(day.minTemp)}\xB0 /`), /* @__PURE__ */ h("div", { style: { fontSize: 14, color: "rgba(255,255,255,0.6)", marginLeft: 4, whiteSpace: "NoWrap" } }, `${Math.round(day.maxTemp)}\xB0`))
        );
      }))
    );
  };
  var WeatherCard = () => {
    const [loading, setLoading] = useState(true);
    const [weather, setWeather] = useState(null);
    const [error, setError] = useState(null);
    const doFetch = useCallback(() => {
      setLoading(true);
      setError(null);
      fetchWeather((data, err) => {
        setLoading(false);
        if (data)
          setWeather(data);
        else
          setError(err);
      });
    }, []);
    useEffect(() => {
      doFetch();
    }, [doFetch]);
    if (loading)
      return /* @__PURE__ */ h(LoadingView, null);
    if (error)
      return /* @__PURE__ */ h(ErrorView, { message: error, onRetry: doFetch });
    if (weather)
      return /* @__PURE__ */ h(WeatherContent, { data: weather });
    return null;
  };
  __registerPlugin({
    id: "weather",
    title: "Weather",
    width: 300,
    height: 420,
    initialX: 200,
    initialY: 100,
    compact: {
      width: 280,
      height: 100,
      component: WeatherCompact
    },
    launcher: {
      text: "\u{F0898}",
      background: "#0066aa"
    },
    component: WeatherCard
  });
})();
//# sourceMappingURL=app.js.map
