import { h } from "preact"
import { useState, useEffect, useCallback, useRef } from "preact/hooks"

declare const chill: any
declare const __registerPlugin: any

// ---- Constants ----
const SPOTIFY_GREEN = "#1db954"
const BG = "#0b1020"
const CARD = "#111827"
const TEXT = "#e5e7eb"
const DIM = "#94a3b8"
const BORDER = "rgba(255,255,255,0.08)"

// ---- Types ----
interface SpotifyDevice {
    id: string
    name: string
    type: string
    isActive: boolean
    volume: number | null
}

// ---- Helper: get Spotify JSApi ----
function getApi(): any {
    return chill.custom?.get("spotify") ?? null
}

// ---- Device type icon ----
function deviceIcon(type: string): string {
    switch ((type || "").toLowerCase()) {
        case "computer": return "󰌢"
        case "smartphone": return "󰄜"
        case "speaker": return "󰓃"
        case "tv": return "󰔂"
        case "tablet": return "󰓶"
        default: return "󰝚"
    }
}

// ---- Sub-components ----

const SectionTitle = ({ text }: { text: string }) => (
    <div style={{
        fontSize: 11, color: DIM, marginBottom: 6,
        paddingLeft: 2, letterSpacing: 0.5,
    }}>
        {text.toUpperCase()}
    </div>
)

const Separator = () => (
    <div style={{ height: 1, backgroundColor: BORDER, marginTop: 10, marginBottom: 10 }} />
)

const ActionButton = ({ text, onClick, primary = false, disabled = false }: {
    text: string; onClick: () => void; primary?: boolean; disabled?: boolean
}) => (
    <div
        onClick={disabled ? undefined : onClick}
        style={{
            fontSize: 12,
            color: disabled ? "rgba(255,255,255,0.3)" : (primary ? "#fff" : SPOTIFY_GREEN),
            backgroundColor: disabled ? "rgba(255,255,255,0.04)" : (primary ? SPOTIFY_GREEN : "rgba(255,255,255,0.06)"),
            paddingTop: 7, paddingBottom: 7,
            paddingLeft: 16, paddingRight: 16,
            borderRadius: 6,
            unityTextAlign: "MiddleCenter",

        }}
    >
        {text}
    </div>
)

// ---- Config Panel (Client ID input) ----
const ConfigPanel = ({ onDone }: { onDone: () => void }) => {
    const [clientId, setClientId] = useState("")
    const api = getApi()

    const submit = () => {
        if (!clientId || clientId.length < 10) return
        api?.submitClientId(clientId)
        onDone()
    }

    const cancel = () => {
        api?.cancelConfig()
        onDone()
    }

    return (
        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 16 }}>
            <div style={{ fontSize: 15, color: TEXT, marginBottom: 4, unityFontStyleAndWeight: "Bold" }}>
                Spotify Configuration
            </div>
            <div style={{ fontSize: 11, color: DIM, marginBottom: 12 }}>
                Connect your Spotify Developer account
            </div>

            <Separator />

            <SectionTitle text="Client ID" />
            <textfield
                value={clientId}
                onValueChanged={(e: any) => setClientId(e.newValue ?? "")}
                style={{
                    fontSize: 13, color: "#1a1a1a",
                    backgroundColor: "rgba(255,255,255,0.06)",
                    borderWidth: 0, borderRadius: 6,
                    paddingTop: 8, paddingBottom: 8,
                    paddingLeft: 10, paddingRight: 10,
                    marginBottom: 10,
                }}
            />

            <div style={{ fontSize: 10, color: DIM, marginBottom: 6 }}>
                1. Go to developer.spotify.com/dashboard to create an App
            </div>
            <div style={{ fontSize: 10, color: DIM, marginBottom: 6 }}>
                2. Copy the Client ID and paste it above
            </div>
            <div style={{ fontSize: 10, color: DIM, marginBottom: 12 }}>
                3. Add Redirect URI: fullstop://callback
            </div>

            <div style={{ flexGrow: 1 }} />

            <Separator />

            <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" }}>
                <ActionButton text="Cancel" onClick={cancel} />
                <ActionButton
                    text="Connect"
                    onClick={submit}
                    primary
                    disabled={!clientId || clientId.length < 10}
                />
            </div>
        </div>
    )
}

// ---- Device Panel ----
const DevicePanel = ({ onDone }: { onDone: () => void }) => {
    const api = getApi()
    const [devices, setDevices] = useState<SpotifyDevice[]>([])
    const [loading, setLoading] = useState(true)
    const activeId = api?.activeDeviceId ?? ""

    useEffect(() => {
        if (!api) return
        api.refreshDevices()
        const poll = setInterval(() => {
            try {
                const json = api.devicesJson
                if (json) setDevices(JSON.parse(json))
                setLoading(api.isLoadingDevices)
            } catch { }
        }, 300)
        return () => clearInterval(poll)
    }, [])

    const selectDevice = (id: string) => {
        api?.selectDevice(id)
        onDone()
    }

    return (
        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 16 }}>
            <div style={{ fontSize: 15, color: TEXT, marginBottom: 4, unityFontStyleAndWeight: "Bold" }}>
                Select Device
            </div>
            <div style={{ fontSize: 11, color: DIM, marginBottom: 12 }}>
                Choose a Spotify playback device
            </div>

            <Separator />

            {loading ? (
                <div style={{ flexGrow: 1, display: "Flex", justifyContent: "Center", alignItems: "Center" }}>
                    <div style={{ fontSize: 12, color: DIM }}>Loading devices...</div>
                </div>
            ) : devices.length === 0 ? (
                <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", justifyContent: "Center", alignItems: "Center" }}>
                    <div style={{ fontSize: 24, color: DIM, marginBottom: 8 }}>󰝚</div>
                    <div style={{ fontSize: 12, color: DIM, unityTextAlign: "MiddleCenter" }}>
                        No devices found. Please open Spotify.
                    </div>
                </div>
            ) : (
                <div style={{ flexGrow: 1, overflow: "Scroll" }}>
                    {devices.map((d) => {
                        const isActive = d.id === activeId || d.isActive
                        return (
                            <div
                                key={d.id}
                                onClick={() => selectDevice(d.id)}
                                style={{
                                    display: "Flex", flexDirection: "Row", alignItems: "Center",
                                    backgroundColor: isActive ? "rgba(29,185,84,0.12)" : "rgba(255,255,255,0.04)",
                                    borderRadius: 8, padding: 10, marginBottom: 4,
                                    borderLeftWidth: isActive ? 3 : 0,
                                    borderLeftColor: SPOTIFY_GREEN,

                                }}
                            >
                                <div style={{ fontSize: 20, color: isActive ? SPOTIFY_GREEN : DIM, marginRight: 10, width: 24 }}>
                                    {deviceIcon(d.type)}
                                </div>
                                <div style={{ flexGrow: 1 }}>
                                    <div style={{
                                        fontSize: 13,
                                        color: isActive ? SPOTIFY_GREEN : TEXT,
                                        unityFontStyleAndWeight: isActive ? "Bold" : "Normal",
                                    }}>
                                        {d.name || "Unknown"}
                                    </div>
                                    <div style={{ fontSize: 10, color: DIM }}>
                                        {(d.type || "Device") + (d.volume != null ? `  ·  Vol ${d.volume}%` : "")}
                                    </div>
                                </div>
                                {isActive && (
                                    <div style={{ fontSize: 10, color: SPOTIFY_GREEN }}>Active</div>
                                )}
                            </div>
                        )
                    })}
                </div>
            )}

            <Separator />

            <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" }}>
                <ActionButton text="Refresh" onClick={() => api?.refreshDevices()} />
                <ActionButton text="Close" onClick={() => { api?.cancelDeviceSelection(); onDone() }} />
            </div>
        </div>
    )
}

// ---- Main Component ----
const SpotifyMain = () => {
    const [view, setView] = useState<"main" | "config" | "devices">("main")
    const [status, setStatus] = useState("")
    const [loggedIn, setLoggedIn] = useState(false)
    const [user, setUser] = useState("")
    const [account, setAccount] = useState("")
    const [device, setDevice] = useState("")
    const [needsConfig, setNeedsConfig] = useState(false)
    const pollRef = useRef<any>(null)

    // Poll JSApi status
    useEffect(() => {
        const poll = () => {
            const api = getApi()
            if (!api) return
            setStatus(api.loginStatus || "")
            setLoggedIn(api.isLoggedIn)
            setUser(api.userName || "")
            setAccount(api.accountType || "")
            setDevice(api.activeDeviceName || "")
            setNeedsConfig(api.needsClientId)

            // Respond to C# requests to open panels
            if (api.showConfigPanel && view !== "config") {
                setView("config")
                api.showConfigPanel = false
            }
            if (api.showDevicePanel && view !== "devices") {
                setView("devices")
                api.showDevicePanel = false
            }
        }
        pollRef.current = setInterval(poll, 500)
        poll()
        return () => clearInterval(pollRef.current)
    }, [view])

    const api = getApi()
    const noApi = !api

    return (
        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", backgroundColor: BG, padding: 16 }}>
            {view === "config" ? (
                <ConfigPanel onDone={() => setView("main")} />
            ) : view === "devices" ? (
                <DevicePanel onDone={() => setView("main")} />
            ) : (
                <div>
                    {/* Header */}
                    <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 12 }}>
                        <div style={{ fontSize: 22, color: SPOTIFY_GREEN, marginRight: 8 }}>󰓇</div>
                        <div style={{ fontSize: 15, color: TEXT, unityFontStyleAndWeight: "Bold" }}>Spotify</div>
                    </div>

                    <Separator />

                    {noApi ? (
                        <div style={{ flexGrow: 1, display: "Flex", justifyContent: "Center", alignItems: "Center" }}>
                            <div style={{ fontSize: 12, color: DIM }}>Waiting for Spotify module...</div>
                        </div>
                    ) : loggedIn ? (
                        // Logged In View
                        <div style={{ flexGrow: 1 }}>
                            <SectionTitle text="Account" />
                            <div style={{
                                backgroundColor: CARD, borderRadius: 8, padding: 12, marginBottom: 10,
                            }}>
                                <div style={{ fontSize: 13, color: TEXT, marginBottom: 2 }}>{user || "Spotify User"}</div>
                                <div style={{ fontSize: 10, color: account === "premium" ? SPOTIFY_GREEN : "#f59e0b" }}>
                                    {account === "premium" ? "Premium" : "Free (Premium required for playback control)"}
                                </div>
                            </div>

                            <SectionTitle text="Playback Device" />
                            <div
                                onClick={() => setView("devices")}
                                style={{
                                    backgroundColor: CARD, borderRadius: 8, padding: 12,
                                    display: "Flex", flexDirection: "Row", alignItems: "Center",
                                    marginBottom: 10,
                                }}
                            >
                                <div style={{ fontSize: 16, color: device ? SPOTIFY_GREEN : DIM, marginRight: 8 }}>
                                    {device ? "󰓃" : "󰝚"}
                                </div>
                                <div style={{ flexGrow: 1 }}>
                                    <div style={{ fontSize: 12, color: device ? TEXT : DIM }}>
                                        {device || "No device selected"}
                                    </div>
                                </div>
                                <div style={{ fontSize: 12, color: DIM }}>›</div>
                            </div>

                            {status ? (
                                <div style={{ fontSize: 11, color: DIM, marginTop: 4 }}>{status}</div>
                            ) : null}

                            <div style={{ flexGrow: 1 }} />
                            <Separator />
                            <ActionButton text="Logout" onClick={() => api?.requestLogout()} />
                        </div>
                    ) : (
                        // Not Logged In View
                        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column" }}>
                            <div style={{
                                flexGrow: 1, display: "Flex", flexDirection: "Column",
                                justifyContent: "Center", alignItems: "Center",
                            }}>
                                <div style={{ fontSize: 40, color: SPOTIFY_GREEN, marginBottom: 12 }}>󰓇</div>
                                <div style={{ fontSize: 13, color: TEXT, marginBottom: 6 }}>
                                    {needsConfig ? "Client ID Required" : "Not Logged In"}
                                </div>
                                <div style={{ fontSize: 11, color: DIM, unityTextAlign: "MiddleCenter", marginBottom: 16, paddingLeft: 12, paddingRight: 12 }}>
                                    {status || (needsConfig ? "Click the button below to configure" : "Click login to connect Spotify")}
                                </div>
                            </div>

                            <Separator />

                            {needsConfig ? (
                                <ActionButton text="Configure Spotify" onClick={() => { api.showConfigPanel = true; setView("config") }} primary />
                            ) : (
                                <ActionButton text="Login to Spotify" onClick={() => api?.requestLogin()} primary />
                            )}
                        </div>
                    )}
                </div>
            )}
        </div>
    )
}

// ---- Compact Component ----
const SpotifyCompact = () => {
    const [loggedIn, setLoggedIn] = useState(false)
    const [user, setUser] = useState("")
    const [device, setDevice] = useState("")
    const [status, setStatus] = useState("")

    useEffect(() => {
        const poll = setInterval(() => {
            const api = getApi()
            if (!api) return
            setLoggedIn(api.isLoggedIn)
            setUser(api.userName || "")
            setDevice(api.activeDeviceName || "")
            setStatus(api.loginStatus || "")
        }, 800)
        return () => clearInterval(poll)
    }, [])

    return (
        <div style={{
            flexGrow: 1, display: "Flex", flexDirection: "Row",
            alignItems: "Center", backgroundColor: BG, padding: 12,
        }}>
            <div style={{ fontSize: 22, color: SPOTIFY_GREEN, marginRight: 10 }}>󰓇</div>
            <div style={{ flexGrow: 1 }}>
                <div style={{ fontSize: 12, color: TEXT }}>
                    {loggedIn ? (user || "Spotify") : "Spotify"}
                </div>
                <div style={{ fontSize: 10, color: DIM }}>
                    {loggedIn
                        ? (device ? `${device}` : "No device selected")
                        : (status || "Not logged in")}
                </div>
            </div>
        </div>
    )
}

// ---- Register ----
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
        component: SpotifyCompact,
    },
    launcher: {
        text: "",
        background: "#1db954",
    },
    component: SpotifyMain,
})
