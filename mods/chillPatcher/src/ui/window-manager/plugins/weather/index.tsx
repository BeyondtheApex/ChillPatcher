import { h } from "preact"
import { useState, useRef, useEffect, useCallback } from "preact/hooks"

declare const chill: any
declare const __registerPlugin: any

// ---- Config ----
const weatherLat = chill.config.appGetOrCreate("Weather.Latitude", 39.9, "天气查询纬度 (例: 39.9 = 北京)")
const weatherLon = chill.config.appGetOrCreate("Weather.Longitude", 116.4, "天气查询经度 (例: 116.4 = 北京)")
const weatherLocation = chill.config.appGetOrCreate("Weather.LocationName", "北京", "显示的地点名称")

// ---- Constants ----
const WEATHER_API =
    `https://api.open-meteo.com/v1/forecast?latitude=${weatherLat}&longitude=${weatherLon}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto`

interface DailyWeather {
    date: string
    code: number
    maxTemp: number
    minTemp: number
}

interface WeatherData {
    temperature: number
    humidity: number
    windSpeed: number
    weatherCode: number
    location: string
    daily: DailyWeather[]
}

const getWeatherInfo = (code: number) => {
    switch (true) {
        // 晴天
        case code === 0:
            return { text: "晴朗", icon: "󰖙", bg: "#2563eb" }; // nf-mdi-weather_sunny

        // 多云 / 阴天
        case code === 1:
            return { text: "晴间多云", icon: "󰖕", bg: "#3b82f6" }; // nf-mdi-weather_partly_cloudy
        case code === 2:
            return { text: "多云", icon: "󰖐", bg: "#475569" }; // nf-mdi-weather_cloudy
        case code === 3:
            return { text: "阴天", icon: "󰅟", bg: "#334155" };

        // 雾 / 霾
        case code === 45 || code === 48:
            return { text: "雾", icon: "󰖑", bg: "#64748b" }; // nf-mdi-weather_fog

        // 毛毛雨 / 局部小雨
        case [51, 53, 55, 56, 57].includes(code):
            return { text: "毛毛雨", icon: "󰖗", bg: "#2c4a6b" }; // nf-mdi-weather_rainy

        // 降雨
        case code === 61 || code === 63:
            return { text: "小到中雨", icon: "󰖗", bg: "#1e3a5f" };
        case code === 65 || code === 66 || code === 67:
            return { text: "大雨/暴雨", icon: "󰖖", bg: "#152a45" }; // nf-mdi-weather_pouring

        // 阵雨
        case [80, 81, 82].includes(code):
            return { text: "阵雨", icon: "󰖗", bg: "#224166" };

        // 降雪
        case code === 71 || code === 73:
            return { text: "小到中雪", icon: "󰖘", bg: "#4a6078" }; // nf-mdi-weather_snowy
        case code === 75 || code === 77 || code === 85 || code === 86:
            return { text: "大雪/暴雪", icon: "󰼶", bg: "#3a4c61" }; // nf-mdi-weather_snowy_heavy

        // 雷暴
        case code === 95:
            return { text: "雷暴", icon: "󰖓", bg: "#1e293b" }; // nf-mdi-weather_lightning
        case code === 96 || code === 99:
            return { text: "雷阵雨/冰雹", icon: "󰖒", bg: "#0f172a" }; // nf-mdi-weather_lightning_rainy

        // 默认兜底
        default:
            return { text: "未知", icon: "󰨹", bg: "#6b7280" }; // fallback to cloudy
    }
}

const getDayName = (dateStr: string, index: number) => {
    if (index === 0) return "今天"
    const date = new Date(dateStr)
    const days = ["周日", "周一", "周二", "周三", "周四", "周五", "周六"]
    return days[date.getDay()]
}

// ---- Hooks ----
function useAnimationFrame(callback: (phase: number) => void) {
    const phaseRef = useRef(0)
    useEffect(() => {
        let mounted = true
        let frameId: number
        const loop = () => {
            if (!mounted) return
            phaseRef.current += 1
            callback(phaseRef.current)
            frameId = requestAnimationFrame(loop)
        }
        frameId = requestAnimationFrame(loop)
        return () => {
            mounted = false
            cancelAnimationFrame(frameId)
        }
    }, [])
}

// ---- Sub-components ----
const FloatingIcon = ({ icon, size = 52 }: { icon: string; size?: number }) => {
    const [offsetY, setOffsetY] = useState(0)
    useAnimationFrame((frame) => {
        setOffsetY(Math.sin(frame * 0.03) * 4)
    })
    return (
        <div
            style={{
                fontSize: size,
                color: "rgba(255,255,255,0.9)",
                translate: `0 ${Math.round(offsetY)}px`,
            }}
        >
            {icon}
        </div>
    )
}

const LoadingView = () => {
    const [rotation, setRotation] = useState(0)
    const [pulse, setPulse] = useState(0.3)
    useAnimationFrame((frame) => {
        setRotation((frame * 5) % 360)
        setPulse(0.3 + Math.sin(frame * 0.04) * 0.35)
    })
    return (
        <div
            style={{
                flexGrow: 1,
                justifyContent: "Center",
                alignItems: "Center",
                display: "Flex",
                flexDirection: "Column",
                backgroundColor: "#1e293b",
            }}
        >
            <div
                style={{
                    fontSize: 36,
                    color: "#89b4fa",
                    rotate: rotation,
                    marginBottom: 16,
                }}
            >󰑓</div>
            <div style={{ fontSize: 13, color: "#ffffff", opacity: pulse }}>
                加载中...
            </div>
        </div>
    )
}

const ErrorView = ({
    message,
    onRetry,
}: {
    message: string
    onRetry: () => void
}) => (
    <div
        style={{
            flexGrow: 1,
            justifyContent: "Center",
            alignItems: "Center",
            display: "Flex",
            flexDirection: "Column",
            backgroundColor: "#1e293b",
            paddingLeft: 20,
            paddingRight: 20,
        }}
    >
        <div style={{ fontSize: 14, color: "#f87171", marginBottom: 8 }}>
            出错了
        </div>
        <div
            style={{
                fontSize: 11,
                color: "rgba(255,255,255,0.5)",
                marginBottom: 16,
                unityTextAlign: "MiddleCenter",
            }}
        >
            {message}
        </div>
        <div
            style={{
                fontSize: 12,
                color: "#89b4fa",
                paddingTop: 6,
                paddingBottom: 6,
                paddingLeft: 16,
                paddingRight: 16,
                borderRadius: 6,
                borderWidth: 1,
                borderColor: "#89b4fa",
            }}
            onPointerDown={onRetry}
        >
            重试
        </div>
    </div>
)

// ---- Fetch ----
function fetchWeather(
    callback: (data: WeatherData | null, error: string | null) => void
) {
    chill.net.get(WEATHER_API, (resultJson: string) => {
        try {
            const res = JSON.parse(resultJson)
            if (res.ok && res.body) {
                const api = JSON.parse(res.body)

                // 解析 7 天预报数据
                const dailyData: DailyWeather[] = []
                if (api.daily && api.daily.time) {
                    for (let i = 0; i < 7 && i < api.daily.time.length; i++) {
                        dailyData.push({
                            date: api.daily.time[i],
                            code: api.daily.weather_code[i],
                            maxTemp: api.daily.temperature_2m_max[i],
                            minTemp: api.daily.temperature_2m_min[i],
                        })
                    }
                }

                callback(
                    {
                        temperature: api.current.temperature_2m,
                        humidity: api.current.relative_humidity_2m,
                        windSpeed: api.current.wind_speed_10m,
                        weatherCode: api.current.weather_code,
                        location: weatherLocation,
                        daily: dailyData,
                    },
                    null
                )
            } else {
                callback(null, res.error || `HTTP ${res.status}`)
            }
        } catch (e: any) {
            callback(null, e.message || "解析失败")
        }
    })
}

// ---- Compact weather view ----
const WeatherCompact = () => {
    const [loading, setLoading] = useState(true)
    const [weather, setWeather] = useState<WeatherData | null>(null)
    const [error, setError] = useState<string | null>(null)

    const doFetch = useCallback(() => {
        setLoading(true)
        setError(null)
        fetchWeather((data, err) => {
            setLoading(false)
            if (data) setWeather(data)
            else setError(err)
        })
    }, [])

    useEffect(() => {
        doFetch()
    }, [doFetch])

    if (loading || error || !weather) {
        return (
            <div
                style={{
                    flexGrow: 1,
                    justifyContent: "Center",
                    alignItems: "Center",
                    display: "Flex",
                    backgroundColor: "#1e293b",
                }}
            >
                <div style={{ fontSize: 14, color: "rgba(255,255,255,0.5)" }}>
                    {loading ? "获取天气中..." : "信息错误"}
                </div>
            </div>
        )
    }

    const info = getWeatherInfo(weather.weatherCode)
    return (
        <div
            style={{
                flexGrow: 1,
                display: "Flex",
                flexDirection: "Row",
                alignItems: "Center",
                justifyContent: "SpaceBetween",
                backgroundColor: info.bg,
                paddingLeft: 20,
                paddingRight: 20,
            }}
        >
            {/* 左侧：图标与温度 */}
            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                <div style={{ fontSize: 38, color: "rgba(255,255,255,0.9)", marginRight: 12 }}>
                    {info.icon}
                </div>
                <div style={{ display: "Flex", flexDirection: "Column", justifyContent: "Center" }}>
                    <div style={{ fontSize: 32, color: "#ffffff", unityFontStyleAndWeight: "Bold", marginBottom: -4 }}>
                        {`${Math.round(weather.temperature)}°`}
                    </div>
                    <div style={{ fontSize: 13, color: "rgba(255,255,255,0.8)" }}>
                        {info.text}
                    </div>
                </div>
            </div>
            {/* 右侧：地点与详情 */}
            <div style={{ display: "Flex", flexDirection: "Column", alignItems: "FlexEnd" }}>
                <div style={{ fontSize: 16, color: "#ffffff", unityFontStyleAndWeight: "Bold", marginBottom: 6, letterSpacing: 1 }}>
                    {weather.location}
                </div>
                <div style={{ fontSize: 11, color: "rgba(255,255,255,0.7)", marginBottom: 2 }}>
                    {`风速 ${weather.windSpeed} km/h`}
                </div>
                <div style={{ fontSize: 11, color: "rgba(255,255,255,0.7)" }}>
                    {`湿度 ${weather.humidity}%`}
                </div>
            </div>
        </div>
    )
}

// ---- Weather content (紧凑头部 + 七天预报) ----
const WeatherContent = ({ data }: { data: WeatherData }) => {
    const info = getWeatherInfo(data.weatherCode)
    return (
        <div
            style={{
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
                transitionTimingFunction: "ease-in-out",
            }}
        >
            {/* 头部：当前天气主信息 */}
            <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween", alignItems: "Center", marginBottom: 16 }}>
                <div style={{ display: "Flex", flexDirection: "Column", flexGrow: 1 }}>
                    <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 8 }}>
                        <div style={{ fontSize: 10, color: "rgba(255,255,255,0.6)", marginRight: 4 }}>◉</div>
                        <div style={{ fontSize: 14, color: "rgba(255,255,255,0.85)", letterSpacing: 1 }}>{data.location}</div>
                    </div>
                    <div style={{ display: "Flex", flexDirection: "Row", alignItems: "FlexEnd" }}>
                        <div style={{ fontSize: 42, color: "#ffffff", unityFontStyleAndWeight: "Bold", whiteSpace: "NoWrap" }}>
                            {`${Math.round(data.temperature)}°`}
                        </div>
                        <div style={{ fontSize: 16, color: "rgba(255,255,255,0.9)", marginLeft: 8, marginBottom: 4, whiteSpace: "NoWrap" }}>
                            {info.text}
                        </div>
                    </div>
                </div>

                <div style={{ display: "Flex", flexDirection: "Column", alignItems: "Center", flexShrink: 0, width: 100 }}>
                    <FloatingIcon icon={info.icon} size={42} />
                    <div style={{ fontSize: 11, color: "rgba(255,255,255,0.7)", marginTop: 6, whiteSpace: "NoWrap" }}>
                        {`风速 ${data.windSpeed}km/h`}
                    </div>
                    <div style={{ fontSize: 11, color: "rgba(255,255,255,0.7)", marginTop: 2, whiteSpace: "NoWrap" }}>
                        {`湿度 ${data.humidity}%`}
                    </div>
                </div>
            </div>

            {/* 分割线 */}
            <div style={{ height: 1, backgroundColor: "rgba(255,255,255,0.15)", marginBottom: 12 }} />

            {/* 7 天天气列表 */}
            <div style={{ display: "Flex", flexDirection: "Column", flexGrow: 1 }}>
                <div style={{ fontSize: 12, color: "rgba(255,255,255,0.6)", marginBottom: 8, letterSpacing: 1 }}>
                    7 天天气预报
                </div>
                {data.daily.map((day: DailyWeather, index: number) => {
                    const dayInfo = getWeatherInfo(day.code)
                    return (
                        <div
                            key={index}
                            style={{
                                display: "Flex",
                                flexDirection: "Row",
                                justifyContent: "SpaceBetween",
                                alignItems: "Center",
                                paddingTop: 6,
                                paddingBottom: 6,
                                borderBottomWidth: index === 6 ? 0 : 1,
                                borderBottomColor: "rgba(255,255,255,0.08)",
                            }}
                        >
                            <div style={{ fontSize: 14, color: index === 0 ? "#ffffff" : "rgba(255,255,255,0.8)", width: 60, flexShrink: 0, whiteSpace: "NoWrap" }}>
                                {getDayName(day.date, index)}
                            </div>
                            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", width: 80, flexShrink: 0 }}>
                                <div style={{ fontSize: 16, color: "rgba(255,255,255,0.9)", marginRight: 6 }}>
                                    {dayInfo.icon}
                                </div>
                                <div style={{ fontSize: 12, color: "rgba(255,255,255,0.7)", whiteSpace: "NoWrap" }}>
                                    {dayInfo.text}
                                </div>
                            </div>
                            {/* 温度范围 */}
                            <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "FlexEnd", alignItems: "Center", width: 90, flexShrink: 0 }}>
                                <div style={{ fontSize: 14, color: "#ffffff", whiteSpace: "NoWrap" }}>
                                    {`${Math.round(day.minTemp)}° /`}
                                </div>
                                <div style={{ fontSize: 14, color: "rgba(255,255,255,0.6)", marginLeft: 4, whiteSpace: "NoWrap" }}>
                                    {`${Math.round(day.maxTemp)}°`}
                                </div>
                            </div>
                        </div>
                    )
                })}
            </div>
        </div>
    )
}

// ---- Main component ----
const WeatherCard = () => {
    const [loading, setLoading] = useState(true)
    const [weather, setWeather] = useState<WeatherData | null>(null)
    const [error, setError] = useState<string | null>(null)

    const doFetch = useCallback(() => {
        setLoading(true)
        setError(null)
        fetchWeather((data, err) => {
            setLoading(false)
            if (data) setWeather(data)
            else setError(err)
        })
    }, [])

    useEffect(() => {
        doFetch()
    }, [doFetch])

    if (loading) return <LoadingView />
    if (error) return <ErrorView message={error} onRetry={doFetch} />
    if (weather) return <WeatherContent data={weather} />
    return null
}

// ---- Register ----
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
        component: WeatherCompact,
    },
    launcher: {
        text: "󰢘",
        background: "#0066aa",
    },
    component: WeatherCard,
})
