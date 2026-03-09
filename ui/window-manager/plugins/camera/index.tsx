import { h } from "preact"
import { useState, useRef, useEffect } from "preact/hooks"

declare const __registerPlugin: any
declare const __unregisterPlugin: any
declare const __refreshPlugins: any
declare const chill: any
declare const CS: any

// ---- Constants ----
const MOVE_SPEED = 2.0
const ROT_SPEED = 60.0
const FOV_SPEED = 20.0
const RES_SPEED = 0.3
const BTN_SIZE = 28
const BTN_R = 6
const BTN_COLOR = "rgba(137,180,250,0.15)"
const BTN_TEXT = "#89b4fa"
const LABEL_COLOR = "#6c7086"
const CAMERAS_DIR = "cameras"
const ITEMS_PER_PAGE = 8

// ---- Types ----
interface CamConfig {
    name: string
    enabled: boolean
    px: number; py: number; pz: number
    rx: number; ry: number
    fov: number; res: number
    windowX: number; windowY: number
    windowW: number; windowH: number
}

// ---- IO Helpers ----
function ensureDir() {
    // writeText auto-creates dirs, but check existence for listing
    if (!chill.io.exists(CAMERAS_DIR)) {
        chill.io.writeText(`${CAMERAS_DIR}/.keep`, "")
    }
}

function loadAllConfigs(): CamConfig[] {
    ensureDir()
    try {
        const raw = chill.io.listFiles(CAMERAS_DIR)
        const files: { name: string; nameWithoutExt: string; extension: string }[] = JSON.parse(raw)
        const configs: CamConfig[] = []
        for (const f of files) {
            if (f.extension !== ".json") continue
            try {
                const text = chill.io.readText(`${CAMERAS_DIR}/${f.name}`)
                if (text) configs.push(JSON.parse(text))
            } catch (_) {}
        }
        return configs
    } catch (_) { return [] }
}

function saveConfig(cfg: CamConfig) {
    const safeName = cfg.name.replace(/[^a-zA-Z0-9_\-\u4e00-\u9fff]/g, "_")
    chill.io.writeText(`${CAMERAS_DIR}/${safeName}.json`, JSON.stringify(cfg, null, 2))
}

function deleteConfig(cfg: CamConfig) {
    const safeName = cfg.name.replace(/[^a-zA-Z0-9_\-\u4e00-\u9fff]/g, "_")
    chill.io.deleteFile(`${CAMERAS_DIR}/${safeName}.json`)
}

// ---- Track dynamic camera windows ----
const dynamicWindowIds = new Set<string>()

function registerCameraWindow(cfg: CamConfig) {
    const wid = `cam-${cfg.name}`
    if (dynamicWindowIds.has(wid)) return
    const CamView = () => (
        <camera-view
            fov={cfg.fov} interval={2} resolution-scale={cfg.res}
            pos-x={cfg.px} pos-y={cfg.py} pos-z={cfg.pz}
            rot-x={cfg.rx} rot-y={cfg.ry}
            near-clip={0.3} far-clip={1000} clear-color="#000000"
            style={{ flexGrow: 1, overflow: "Hidden" }}
        />
    )
    __registerPlugin({
        id: wid, title: cfg.name,
        width: cfg.windowW || 300, height: cfg.windowH || 200,
        initialX: cfg.windowX || 100, initialY: cfg.windowY || 100,
        resizable: true, component: CamView,
        onGeometryChange: (x: number, y: number, w: number, h: number) => {
            cfg.windowX = Math.round(x)
            cfg.windowY = Math.round(y)
            cfg.windowW = Math.round(w)
            cfg.windowH = Math.round(h)
            saveConfig(cfg)
        },
    })
    dynamicWindowIds.add(wid)
}

function unregisterCameraWindow(name: string) {
    const wid = `cam-${name}`
    if (!dynamicWindowIds.has(wid)) return
    __unregisterPlugin(wid)
    dynamicWindowIds.delete(wid)
}

// ---- Startup: load and register enabled configs ----
const initialConfigs = loadAllConfigs()
for (const cfg of initialConfigs) {
    if (cfg.enabled) registerCameraWindow(cfg)
}

// ---- Btn component ----
const hold = (input: any, key: string, val: number) => ({
    onPointerDown: () => { input.current[key] = val },
    onPointerUp: () => { input.current[key] = 0 },
    onPointerLeave: () => { input.current[key] = 0 },
})

const Btn = ({ label, input, k, v }: { label: string; input: any; k: string; v: number }) => (
    <div
        style={{
            width: BTN_SIZE, height: BTN_SIZE, borderRadius: BTN_R,
            backgroundColor: BTN_COLOR,
            display: "Flex", justifyContent: "Center", alignItems: "Center",
            fontSize: 12, color: BTN_TEXT,
        }}
        {...hold(input, k, v)}
    >
        {label}
    </div>
)

// ---- CameraEditor: main component ----
const CameraEditor = () => {
    const camRef = useRef<any>(null)
    const [configs, setConfigs] = useState<CamConfig[]>(initialConfigs)
    const [page, setPage] = useState(0)
    const [newName, setNewName] = useState("")

    // Camera state
    const [posX, setPosX] = useState(0)
    const [posY, setPosY] = useState(1)
    const [posZ, setPosZ] = useState(-10)
    const [rotX, setRotX] = useState(0)
    const [rotY, setRotY] = useState(0)
    const [fov, setFov] = useState(60)
    const [resScale, setResScale] = useState(0.5)

    const input = useRef({ mx: 0, my: 0, mz: 0, rx: 0, ry: 0, fov: 0, res: 0 })
    const state = useRef({ px: 0, py: 1, pz: -10, rx: 0, ry: 0, fov: 60, res: 0.5 })
    const lastTime = useRef(0)
    const mounted = useRef(true)

    // RAF loop
    useEffect(() => {
        mounted.current = true
        const getTime = () =>
            typeof CS !== "undefined"
                ? CS.UnityEngine.Time.realtimeSinceStartupAsDouble
                : Date.now() / 1000
        lastTime.current = getTime()

        const loop = () => {
            if (!mounted.current) return
            const now = getTime()
            const dt = Math.min(now - lastTime.current, 0.1)
            lastTime.current = now
            const inp = input.current
            const s = state.current
            if (inp.mx || inp.my || inp.mz || inp.rx || inp.ry || inp.fov || inp.res) {
                s.ry += inp.ry * ROT_SPEED * dt
                s.rx = Math.max(-89, Math.min(89, s.rx + inp.rx * ROT_SPEED * dt))
                const rad = (s.ry * Math.PI) / 180
                s.px += (Math.sin(rad) * inp.mz + Math.cos(rad) * inp.mx) * MOVE_SPEED * dt
                s.pz += (Math.cos(rad) * inp.mz - Math.sin(rad) * inp.mx) * MOVE_SPEED * dt
                s.py += inp.my * MOVE_SPEED * dt
                s.fov = Math.max(5, Math.min(170, s.fov + inp.fov * FOV_SPEED * dt))
                s.res = Math.max(0.1, Math.min(2.0, s.res + inp.res * RES_SPEED * dt))
                const ve = camRef.current?.ve
                if (ve) {
                    ve.PosX = s.px; ve.PosY = s.py; ve.PosZ = s.pz
                    ve.RotX = s.rx; ve.RotY = s.ry; ve.Fov = s.fov; ve.ResolutionScale = s.res
                }
                setPosX(Math.round(s.px * 10) / 10)
                setPosY(Math.round(s.py * 10) / 10)
                setPosZ(Math.round(s.pz * 10) / 10)
                setRotX(Math.round(s.rx)); setRotY(Math.round(s.ry))
                setFov(Math.round(s.fov)); setResScale(Math.round(s.res * 100) / 100)
            }
            requestAnimationFrame(loop)
        }
        requestAnimationFrame(loop)
        return () => { mounted.current = false }
    }, [])

    const reloadConfigs = () => { setConfigs(loadAllConfigs()) }

    const handleCreate = () => {
        const name = newName.trim()
        if (!name) return
        if (configs.some(c => c.name === name)) return
        const s = state.current
        const cfg: CamConfig = {
            name, enabled: true,
            px: Math.round(s.px * 100) / 100, py: Math.round(s.py * 100) / 100,
            pz: Math.round(s.pz * 100) / 100,
            rx: Math.round(s.rx * 10) / 10, ry: Math.round(s.ry * 10) / 10,
            fov: Math.round(s.fov), res: Math.round(s.res * 100) / 100,
            windowX: 100 + configs.length * 30, windowY: 100 + configs.length * 30,
            windowW: 300, windowH: 200,
        }
        saveConfig(cfg)
        registerCameraWindow(cfg)
        __refreshPlugins()
        reloadConfigs()
        setNewName("")
    }

    const handleToggle = (cfg: CamConfig) => {
        cfg.enabled = !cfg.enabled
        saveConfig(cfg)
        if (cfg.enabled) { registerCameraWindow(cfg); } else { unregisterCameraWindow(cfg.name); }
        __refreshPlugins()
        reloadConfigs()
    }

    const handleDelete = (cfg: CamConfig) => {
        if (cfg.enabled) { unregisterCameraWindow(cfg.name) }
        deleteConfig(cfg)
        __refreshPlugins()
        reloadConfigs()
    }

    const totalPages = Math.max(1, Math.ceil(configs.length / ITEMS_PER_PAGE))
    const pageItems = configs.slice(page * ITEMS_PER_PAGE, (page + 1) * ITEMS_PER_PAGE)

    return (
        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Row", backgroundColor: "#1e1e2e" }}>
            {/* Left: config list */}
            <div style={{
                width: 130, display: "Flex", flexDirection: "Column",
                backgroundColor: "#1e1e2e",
                paddingTop: 6, paddingBottom: 6, paddingLeft: 6, paddingRight: 6,
            }}>
                <div style={{ fontSize: 10, color: BTN_TEXT, marginBottom: 4 }}>摄像机列表</div>
                {pageItems.map(cfg => (
                    <div key={cfg.name} style={{
                        display: "Flex", flexDirection: "Row", alignItems: "Center",
                        marginBottom: 2, paddingLeft: 4, paddingRight: 2,
                        paddingTop: 3, paddingBottom: 3,
                        backgroundColor: cfg.enabled ? "rgba(137,180,250,0.08)" : "transparent",
                        borderRadius: 4,
                    }}>
                        <div style={{
                            flexGrow: 1, fontSize: 11,
                            color: cfg.enabled ? "#141414" : "#ff4343",
                            overflow: "Hidden",
                        }}>{cfg.name}</div>
                        <div style={{
                            fontSize: 11, color: cfg.enabled ? "#a6e3a1" : "#585b70",
                            paddingLeft: 4, paddingRight: 4,
                        }} onPointerDown={() => handleToggle(cfg)}>
                            {cfg.enabled ? "●" : "○"}
                        </div>
                        <div style={{
                            fontSize: 11, color: "#f38ba8",
                            paddingLeft: 2, paddingRight: 2,
                        }} onPointerDown={() => handleDelete(cfg)}>✕</div>
                    </div>
                ))}
                <div style={{ flexGrow: 1 }} />
                {/* Pagination */}
                {totalPages > 1 && (
                    <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "Center", marginTop: 4 }}>
                        <div style={{ fontSize: 11, color: page > 0 ? BTN_TEXT : LABEL_COLOR, paddingLeft: 6, paddingRight: 6 }}
                            onPointerDown={() => { if (page > 0) setPage(page - 1) }}>{"‹"}</div>
                        <div style={{ fontSize: 9, color: LABEL_COLOR }}>{`${page + 1}/${totalPages}`}</div>
                        <div style={{ fontSize: 11, color: page < totalPages - 1 ? BTN_TEXT : LABEL_COLOR, paddingLeft: 6, paddingRight: 6 }}
                            onPointerDown={() => { if (page < totalPages - 1) setPage(page + 1) }}>{"›"}</div>
                    </div>
                )}
                {/* Create */}
                <div style={{ marginTop: 6 }}>
                    <textfield style={{
                        fontSize: 10, height: 22, backgroundColor: "#45475a",
                        borderWidth: 1, borderColor: "#585b70", borderRadius: 4,
                        color: "#cdd6f4", paddingLeft: 4, paddingRight: 4,
                    }} value={newName} onValueChanged={(e: any) => setNewName(e.newValue ?? "")}
                    />
                    <div style={{
                        marginTop: 3, fontSize: 10, color: "#1e1e2e",
                        backgroundColor: BTN_TEXT, borderRadius: 4,
                        paddingTop: 4, paddingBottom: 4,
                        unityTextAlign: "MiddleCenter",
                    }} onPointerDown={handleCreate}>创建</div>
                </div>
            </div>

            {/* Center: camera preview */}
            <camera-view
                ref={camRef} fov={60} interval={2} resolution-scale={0.5}
                pos-x={0} pos-y={1} pos-z={-10}
                near-clip={0.3} far-clip={1000} clear-color="#000000"
                style={{ flexGrow: 1, overflow: "Hidden" }}
            />

            {/* Right: controls */}
            <div style={{
                display: "Flex", flexDirection: "Column",
                backgroundColor: "#1e1e2e",
                paddingTop: 8, paddingBottom: 8, paddingLeft: 6, paddingRight: 6,
                alignItems: "Center",
            }}>
                <div style={{ fontSize: 9, color: LABEL_COLOR, marginBottom: 2 }}>平移</div>
                <div style={{ display: "Flex", flexDirection: "Row" }}>
                    <Btn label="◄" input={input} k="mx" v={-1} />
                    <div style={{ width: 2 }} />
                    <Btn label="▲" input={input} k="mz" v={1} />
                    <div style={{ width: 2 }} />
                    <Btn label="▼" input={input} k="mz" v={-1} />
                    <div style={{ width: 2 }} />
                    <Btn label="►" input={input} k="mx" v={1} />
                </div>
                <div style={{ fontSize: 9, color: LABEL_COLOR, marginTop: 6, marginBottom: 2 }}>高度</div>
                <div style={{ display: "Flex", flexDirection: "Row" }}>
                    <Btn label="↓" input={input} k="my" v={-1} />
                    <div style={{ width: 2 }} />
                    <Btn label="↑" input={input} k="my" v={1} />
                </div>
                <div style={{ fontSize: 9, color: LABEL_COLOR, marginTop: 6, marginBottom: 2 }}>视角</div>
                <div style={{ display: "Flex", flexDirection: "Row" }}>
                    <Btn label="◄" input={input} k="ry" v={-1} />
                    <div style={{ width: 2 }} />
                    <Btn label="▲" input={input} k="rx" v={-1} />
                    <div style={{ width: 2 }} />
                    <Btn label="▼" input={input} k="rx" v={1} />
                    <div style={{ width: 2 }} />
                    <Btn label="►" input={input} k="ry" v={1} />
                </div>
                <div style={{ fontSize: 9, color: LABEL_COLOR, marginTop: 6, marginBottom: 2 }}>FOV</div>
                <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                    <Btn label="−" input={input} k="fov" v={-1} />
                    <div style={{ fontSize: 9, color: "#cdd6f4", marginLeft: 4, marginRight: 4, width: 28, unityTextAlign: "MiddleCenter" }}>{`${fov}°`}</div>
                    <Btn label="+" input={input} k="fov" v={1} />
                </div>
                <div style={{ fontSize: 9, color: LABEL_COLOR, marginTop: 6, marginBottom: 2 }}>分辨率</div>
                <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                    <Btn label="−" input={input} k="res" v={-1} />
                    <div style={{ fontSize: 9, color: "#cdd6f4", marginLeft: 4, marginRight: 4, width: 28, unityTextAlign: "MiddleCenter" }}>{`${resScale}`}</div>
                    <Btn label="+" input={input} k="res" v={1} />
                </div>
                <div style={{ flexGrow: 1 }} />
                <div style={{ fontSize: 8, color: LABEL_COLOR, unityTextAlign: "MiddleCenter" }}>{`位置: ${posX}, ${posY}, ${posZ}`}</div>
                <div style={{ fontSize: 8, color: LABEL_COLOR, marginTop: 2, unityTextAlign: "MiddleCenter" }}>{`角度: ${rotX}, ${rotY}`}</div>
            </div>
        </div>
    )
}

__registerPlugin({
    id: "camera",
    title: "Camera Editor",
    width: 480,
    height: 340,
    initialX: 50,
    initialY: 50,
    resizable: true,
    component: CameraEditor,
})

