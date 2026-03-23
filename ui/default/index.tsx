import { h, render, Component } from "preact"
import { useState, useEffect } from "preact/hooks"
import { theme } from "./components/theme"
import { TabContainer } from "./components/TabContainer"
import { SettingsPanel } from "./components/SettingsPanel"
import { AboutPanel } from "./components/AboutPanel"
import { LicensesPanel } from "./components/LicensesPanel"
import { ModulesPanel } from "./components/ModulesPanel"
import { UIExplorerPanel } from "./components/UIExplorerPanel"
import { IMESettingsPanel } from "./components/IMESettingsPanel"
import { IMECandidatePanel } from "./components/IMECandidatePanel"

// Error boundary to catch render errors and display them visually
class ErrorBoundary extends Component<{ name: string }, { error: string | null }> {
    constructor(props: any) {
        super(props)
        this.state = { error: null }
    }
    componentDidCatch(error: any) {
        this.setState({ error: String(error) })
    }
    render() {
        if (this.state.error) {
            return (
                <div style={{ color: "#ff5555", fontSize: 11, padding: 8, backgroundColor: "#2a0000", borderRadius: 4, marginBottom: 4 }}>
                    <div style={{ color: "#ff8888", fontSize: 12, marginBottom: 4 }}>[{this.props.name}] Error:</div>
                    <div style={{ color: "#ffaaaa", fontSize: 10, whiteSpace: "Normal" }}>{this.state.error}</div>
                </div>
            )
        }
        return this.props.children
    }
}

const App = () => {
    const [visible, setVisible] = useState(false)
    const [isGameMode, setIsGameMode] = useState(true) // true = 游戏模式, false = 桌面模式

    // 从 API 获取初始状态并订阅变化事件
    useEffect(() => {
        // 获取初始状态
        if (typeof chill !== 'undefined' && chill.ime) {
            setIsGameMode(chill.ime.getInputMode())
        }

        // 订阅输入模式变化事件（替代轮询）
        if (typeof chill !== 'undefined' && chill.events) {
            const unsub = chill.events.on("inputModeChanged", (data: any) => {
                try {
                    const parsed = typeof data === 'string' ? JSON.parse(data) : data
                    setIsGameMode(parsed.isGameMode)
                } catch (e) {}
            })
            return unsub
        }
    }, [])

    // 切换输入模式
    const toggleInputMode = () => {
        if (typeof chill !== 'undefined' && chill.ime) {
            const newMode = !isGameMode
            chill.ime.setInputMode(newMode)
            setIsGameMode(newMode)
        }
    }

    if (!visible) {
        return (
            <div
                key="collapsed"
                style={{
                    position: "Absolute",
                    top: 0,
                    right: 0,
                    width: 80,
                    height: 52,
                    backgroundColor: theme.bg,
                    borderTopLeftRadius: 0,
                    borderTopRightRadius: 0,
                    borderBottomRightRadius: 0,
                    borderBottomLeftRadius: 52,
                    flexDirection: "Row",
                    justifyContent: "Center",
                    alignItems: "Center",
                    display: "Flex",
                    paddingLeft: 12,
                    paddingBottom: 8,
                }}
            >
                {/* 输入模式状态按钮 */}
                <div
                    style={{ fontSize: 18, color: theme.accent }}
                    onClick={(e) => {
                        e.StopPropagation()
                        toggleInputMode()
                    }}
                >
                    {isGameMode ? "󰮂  " : "  "}
                </div>
                {/* 设置图标 */}
                <div
                    style={{ fontSize: 18, color: theme.accent }}
                    onClick={() => setVisible(true)}
                >
                    
                </div>
            </div>
        )
    }

    return (
        <div key="panel" style={{
            position: "Absolute",
            top: "20%",
            bottom: "20%",
            left: "15%",
            right: "15%",
            backgroundColor: theme.bg,
            borderRadius: theme.radiusLg,
            flexDirection: "Column",
            display: "Flex",
            overflow: "Hidden",
        }}>
            {/* 标题栏 */}
            <div style={{
                flexDirection: "Row",
                display: "Flex",
                justifyContent: "SpaceBetween",
                alignItems: "Center",
                paddingTop: 12,
                paddingBottom: 8,
                paddingLeft: 20,
                paddingRight: 20,
                borderBottomWidth: 1,
                borderBottomColor: theme.border,
            }}>
                <div style={{ fontSize: 16, color: theme.accent }}>
                    ChillPatcher
                </div>
                <div style={{
                    flexDirection: "Row",
                    display: "Flex",
                    alignItems: "Center",
                }}>
                    {/* 输入模式切换按钮 */}
                    <div
                        style={{ fontSize: 18, color: theme.accent }}
                        onClick={() => {
                            toggleInputMode()
                        }}
                    >
                        {isGameMode ? "󰮂  " : "  "}
                    </div>
                    {/* 关闭按钮 */}
                    <div
                        style={{ fontSize: 18, color: theme.accent }}
                        onClick={() => setVisible(false)}
                    >
                        ✕
                    </div>
                </div>
            </div>

            {/* 主体 */}
            <scrollview style={{ flexGrow: 1 }}>
                <div style={{
                    flexDirection: "Column",
                    display: "Flex",
                    paddingTop: 8,
                    paddingBottom: 12,
                    paddingLeft: 16,
                    paddingRight: 16,
                }}>
                    <TabContainer
                        defaultTab="modules"
                        tabs={[
                            { id: "modules", label: "模块", content: () => <ErrorBoundary name="ModulesPanel"><ModulesPanel /></ErrorBoundary> },
                            { id: "explorer", label: "场景树", content: () => <ErrorBoundary name="UIExplorerPanel"><UIExplorerPanel /></ErrorBoundary> },
                            { id: "ime", label: "输入法", content: () => <ErrorBoundary name="IMESettingsPanel"><IMESettingsPanel /></ErrorBoundary> },
                            { id: "settings", label: "设置", content: () => <ErrorBoundary name="SettingsPanel"><SettingsPanel /></ErrorBoundary> },
                            { id: "licenses", label: "许可证", content: () => <ErrorBoundary name="LicensesPanel"><LicensesPanel /></ErrorBoundary> },
                            { id: "about", label: "关于", content: () => <ErrorBoundary name="AboutPanel"><AboutPanel /></ErrorBoundary> },
                        ]}
                    />
                </div>
            </scrollview>
        </div>
    )
}

render(
    <div style={{ position: "Absolute", top: 0, left: 0, right: 0, bottom: 0 }} picking-mode={1}>
        <App />
        <ErrorBoundary name="IMECandidatePanel">
            <IMECandidatePanel />
        </ErrorBoundary>
    </div>,
    document.body
)
