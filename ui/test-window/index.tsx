import { h, render } from "preact"
import { useState, useRef, useEffect, useErrorBoundary } from "preact/hooks"

declare const chill: any
declare const CS: any

// ---- RAF polyfill (OneJS 可能未预加载 builtin.mjs) ----
if (typeof globalThis.requestAnimationFrame === "undefined") {
    ;(globalThis as any).requestAnimationFrame = (cb: (t: number) => void) =>
        setTimeout(() => cb(typeof CS !== "undefined"
            ? CS.UnityEngine.Time.realtimeSinceStartupAsDouble * 1000
            : Date.now()), 1)
    ;(globalThis as any).cancelAnimationFrame = (id: number) => clearTimeout(id)
}

// ---- Constants ----
const TITLE_HEIGHT = 30
const CARD_W = 300
const CARD_H = 420

const WEATHER_API =
    "https://api.open-meteo.com/v1/forecast?latitude=-90&longitude=0&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&timezone=auto"

interface WeatherData {
    temperature: number
    humidity: number
    windSpeed: number
    weatherCode: number
    location: string
}

const getWeatherInfo = (code: number) => {
    if (code === 0)
        return { text: "晴朗", icon: "󰖙", bg: "#2563eb" }
    if (code >= 1 && code <= 3)
        return { text: "多云", icon: "󰖐", bg: "#475569" }
    if (code >= 51 && code <= 67)
        return { text: "降雨", icon: "󰖖", bg: "#1e3a5f" }
    if (code >= 71 && code <= 86)
        return { text: "降雪", icon: "󰖘", bg: "#4a6078"}
    if (code >= 95)
        return { text: "雷暴", icon: "󰖓", bg: "#1e293b" }
    return { text: "未知", icon: "?", bg: "#6b7280" }
}

// ---- Safe RAF hook (guards against unmounted updates) ----
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

// ---- Error boundary wrapper ----
const ErrorBoundary = ({ children }: { children: any }) => {
    const [error, resetError] = useErrorBoundary((err) => {
        console.error("[WeatherCard] Error caught by boundary:", err)
    })

    if (error) {
        return (
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
                    渲染出错
                </div>
                <div
                    style={{
                        fontSize: 11,
                        color: "rgba(255,255,255,0.5)",
                        marginBottom: 16,
                        unityTextAlign: "MiddleCenter",
                    }}
                >
                    {String(error)}
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
                    onPointerDown={() => resetError()}
                >
                    重置
                </div>
            </div>
        )
    }

    return children
}

// ---- Loading spinner ----
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
            >
                ✦
            </div>
            <div style={{ fontSize: 13, color: "#ffffff", opacity: pulse }}>
                加载中...
            </div>
        </div>
    )
}

// ---- Error view ----
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

// ---- Floating icon ----
const FloatingIcon = ({ icon }: { icon: string }) => {
    const [offsetY, setOffsetY] = useState(0)

    useAnimationFrame((frame) => {
        setOffsetY(Math.sin(frame * 0.03) * 6)
    })

    return (
        <div
            style={{
                fontSize: 52,
                color: "rgba(255,255,255,0.25)",
                translate: `0 ${Math.round(offsetY)}px`,
                marginBottom: 12,
            }}
        >
            {icon}
        </div>
    )
}

// ---- Info item with hover transition ----
const InfoItem = ({ label, value }: { label: string; value: string }) => {
    const [hovered, setHovered] = useState(false)

    return (
        <div
            style={{
                display: "Flex",
                flexDirection: "Column",
                alignItems: "Center",
                flexGrow: 1,
                paddingTop: 8,
                paddingBottom: 8,
                borderRadius: 8,
                translate: hovered ? "0 -3px" : "0 0",
                transitionProperty: "translate",
                transitionDuration: "0.2s",
                transitionTimingFunction: "ease-out",
            }}
            onPointerEnter={() => setHovered(true)}
            onPointerLeave={() => setHovered(false)}
        >
            <div
                style={{
                    fontSize: 10,
                    color: "rgba(255,255,255,0.45)",
                    marginBottom: 4,
                    letterSpacing: 1.5,
                }}
            >
                {label}
            </div>
            <div style={{ fontSize: 14, color: "#ffffff" }}>{value}</div>
        </div>
    )
}

// ---- Weather content ----
const WeatherContent = ({ data }: { data: WeatherData }) => {
    const info = getWeatherInfo(data.weatherCode)

    return (
        <div
            style={{
                flexGrow: 1,
                display: "Flex",
                flexDirection: "Column",
                backgroundColor: info.bg,
                paddingTop: 20,
                paddingBottom: 20,
                paddingLeft: 24,
                paddingRight: 24,
                justifyContent: "SpaceBetween",
                transitionProperty: "background-color",
                transitionDuration: "0.8s",
                transitionTimingFunction: "ease-in-out",
            }}
        >
            {/* 位置 */}
            <div
                style={{
                    display: "Flex",
                    flexDirection: "Row",
                    alignItems: "Center",
                }}
            >
                <div
                    style={{
                        fontSize: 11,
                        color: "rgba(255,255,255,0.5)",
                        marginRight: 6,
                    }}
                >
                    ◉
                </div>
                <div
                    style={{
                        fontSize: 15,
                        color: "rgba(255,255,255,0.85)",
                        letterSpacing: 2,
                    }}
                >
                    {data.location}
                </div>
            </div>

            {/* 温度 & 图标 */}
            <div
                style={{
                    display: "Flex",
                    flexDirection: "Column",
                    alignItems: "Center",
                    justifyContent: "Center",
                    flexGrow: 1,
                }}
            >
                <FloatingIcon icon={info.icon} />
                <div
                    style={{
                        display: "Flex",
                        flexDirection: "Row",
                        alignItems: "Flex-Start",
                    }}
                >
                    <div
                        style={{
                            fontSize: 52,
                            color: "#ffffff",
                            unityFontStyleAndWeight: "Bold",
                        }}
                    >
                        {Math.round(data.temperature)}
                    </div>
                    <div
                        style={{
                            fontSize: 18,
                            color: "rgba(255,255,255,0.7)",
                            marginTop: 8,
                            marginLeft: 2,
                        }}
                    >
                        °C
                    </div>
                </div>
                <div
                    style={{
                        fontSize: 15,
                        color: "rgba(255,255,255,0.65)",
                        marginTop: 6,
                    }}
                >
                    {info.text}
                </div>
            </div>

            {/* 详情 */}
            <div
                style={{
                    display: "Flex",
                    flexDirection: "Row",
                    justifyContent: "SpaceBetween",
                    borderTopWidth: 1,
                    borderTopColor: "rgba(255,255,255,0.12)",
                    paddingTop: 14,
                }}
            >
                <InfoItem label="WIND" value={`${data.windSpeed} km/h`} />
                <InfoItem label="HUMIDITY" value={`${data.humidity}%`} />
            </div>
        </div>
    )
}

// ---- Fetch weather data via chill.net API ----
function fetchWeather(
    callback: (data: WeatherData | null, error: string | null) => void
) {
    chill.net.get(WEATHER_API, (resultJson: string) => {
        try {
            const res = JSON.parse(resultJson)
            if (res.ok && res.body) {
                const api = JSON.parse(res.body)
                const data: WeatherData = {
                    temperature: api.current.temperature_2m,
                    humidity: api.current.relative_humidity_2m,
                    windSpeed: api.current.wind_speed_10m,
                    weatherCode: api.current.weather_code,
                    location: "南极点",
                }
                callback(data, null)
            } else {
                callback(null, res.error || `HTTP ${res.status}`)
            }
        } catch (e: any) {
            callback(null, e.message || "解析失败")
        }
    })
}

// ---- 可拖拽天气卡片 ----
const DraggableWeatherCard = () => {
    const [pos, setPos] = useState({ x: 200, y: 100 })
    const drag = useRef({ active: false, ox: 0, oy: 0 })
    const [loading, setLoading] = useState(true)
    const [weather, setWeather] = useState<WeatherData | null>(null)
    const [error, setError] = useState<string | null>(null)
    const [hovered, setHovered] = useState(false)

    const doFetch = () => {
        setLoading(true)
        setError(null)
        fetchWeather((data, err) => {
            setLoading(false)
            if (data) setWeather(data)
            else setError(err)
        })
    }

    useEffect(() => {
        doFetch()
    }, [])

    return (
        <div
            style={{
                position: "Absolute",
                top: 0,
                left: 0,
                right: 0,
                bottom: 0,
            }}
            onPointerMove={(e: any) => {
                if (!drag.current.active) return
                setPos({
                    x: e.position.x - drag.current.ox,
                    y: e.position.y - drag.current.oy,
                })
            }}
            onPointerUp={() => {
                drag.current.active = false
            }}
        >
            {/* 卡片 */}
            <div
                style={{
                    position: "Absolute",
                    left: pos.x,
                    top: pos.y,
                    width: CARD_W,
                    height: CARD_H,
                    borderRadius: 20,
                    borderWidth: 1,
                    borderColor: "rgba(255,255,255,0.1)",
                    flexDirection: "Column",
                    display: "Flex",
                    overflow: "Hidden",
                    scale: hovered ? 1.03 : 1.0,
                    transitionProperty: "scale",
                    transitionDuration: "0.4s",
                    transitionTimingFunction: "ease-out",
                }}
                onPointerEnter={() => setHovered(true)}
                onPointerLeave={() => setHovered(false)}
            >
                {/* 标题栏 - 可拖动 */}
                <div
                    style={{
                        height: TITLE_HEIGHT,
                        backgroundColor: "#141422",
                        flexDirection: "Row",
                        display: "Flex",
                        alignItems: "Center",
                        justifyContent: "SpaceBetween",
                        paddingLeft: 14,
                        paddingRight: 14,
                    }}
                    onPointerDown={(e: any) => {
                        drag.current = {
                            active: true,
                            ox: e.position.x - pos.x,
                            oy: e.position.y - pos.y,
                        }
                    }}
                >
                    <div style={{ fontSize: 12, color: "#89b4fa" }}>
                        Weather
                    </div>
                    <div style={{ fontSize: 11, color: "#6c7086" }}>⠿</div>
                </div>

                {/* 内容 - 用错误边界包裹 */}
                <ErrorBoundary>
                    {loading ? (
                        <LoadingView />
                    ) : error ? (
                        <ErrorView message={error} onRetry={doFetch} />
                    ) : (
                        weather && <WeatherContent data={weather} />
                    )}
                </ErrorBoundary>
            </div>
        </div>
    )
}

render(<DraggableWeatherCard />, document.body)
