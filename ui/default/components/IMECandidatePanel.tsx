import { h } from "preact"
import { useState, useEffect, useRef } from "preact/hooks"
import { theme } from "./theme"
import { parse } from "./utils"

declare const chill: any

// --- IME 配置默认值 ---
const defaults = {
    blurEnabled: true,
    blurDownsample: 1,
    blurIterations: 4,
    blurInterval: 1,
    blurTint: "#ffffff1a",
    bgColor: "#1e1e2eF0",
    candidateCount: 5,
}

// 模块加载时注册配置（安全包裹，确保不阻断模块加载）
try {
    chill.config.appGetOrCreate("IME.BlurEnabled", defaults.blurEnabled, "是否启用候选词面板毛玻璃模糊效果")
    chill.config.appGetOrCreate("IME.BlurDownsample", defaults.blurDownsample, "模糊分辨率缩放 (1-8)")
    chill.config.appGetOrCreate("IME.BlurIterations", defaults.blurIterations, "模糊迭代次数 (1-8)")
    chill.config.appGetOrCreate("IME.BlurInterval", defaults.blurInterval, "模糊帧间隔 (每N帧更新)")
    chill.config.appGetOrCreate("IME.BlurTint", defaults.blurTint, "模糊叠加颜色 (hex)")
    chill.config.appGetOrCreate("IME.BackgroundColor", defaults.bgColor, "面板背景色 (hex)")
    chill.config.appGetOrCreate("IME.CandidateCount", defaults.candidateCount, "候选词显示数量")
} catch (e) {
    console.error("[IME] Config init error:", e)
}

export function getIMEConfig() {
    try {
        return {
            blurEnabled: chill.config.appGet("IME.BlurEnabled") ?? defaults.blurEnabled,
            blurDownsample: chill.config.appGet("IME.BlurDownsample") ?? defaults.blurDownsample,
            blurIterations: chill.config.appGet("IME.BlurIterations") ?? defaults.blurIterations,
            blurInterval: chill.config.appGet("IME.BlurInterval") ?? defaults.blurInterval,
            blurTint: chill.config.appGet("IME.BlurTint") ?? defaults.blurTint,
            bgColor: chill.config.appGet("IME.BackgroundColor") ?? defaults.bgColor,
            candidateCount: chill.config.appGet("IME.CandidateCount") ?? defaults.candidateCount,
        }
    } catch (e) {
        return { ...defaults }
    }
}

interface IMEContext {
    preedit: string
    cursorPos: number
    highlightedIndex: number
    candidates: Array<{ text: string; comment?: string }>
}

interface InputRect {
    x: number
    y: number
    width: number
    height: number
}

/**
 * IME 候选词浮窗组件
 * 在 TextField 获焦且有 Rime preedit 时自动显示
 * 显示编码串和候选词列表，支持点击选词
 */
export const IMECandidatePanel = () => {
    const [context, setContext] = useState<IMEContext | null>(null)
    const [inputRect, setInputRect] = useState<InputRect | null>(null)
    const [cfg, setCfg] = useState(getIMEConfig)

    useEffect(() => {
        // 获取初始状态
        try {
            const ctxJson = chill.ime.getContext() as string
            const ctx = parse<IMEContext>(ctxJson)
            setContext(ctx)
            if (ctx) {
                setInputRect(parse<InputRect>(chill.ime.getInputRect() as string))
            }
        } catch (e) {}

        // 订阅 IME Context 变化事件（替代 50ms 轮询）
        let unsub: (() => void) | undefined
        if (typeof chill !== 'undefined' && chill.events) {
            unsub = chill.events.on("imeContextChanged", (data: any) => {
                try {
                    const parsed = typeof data === 'string' ? JSON.parse(data) : data
                    const ctx = typeof parsed.context === 'string' ? parse<IMEContext>(parsed.context) : parsed.context
                    setContext(ctx)
                    if (ctx) {
                        const rect = typeof parsed.inputRect === 'string' ? parse<InputRect>(parsed.inputRect) : parsed.inputRect
                        setInputRect(rect)
                    }
                } catch (e) {}
            })
        }

        // Refresh config every 2s
        const cfgTimer = setInterval(() => {
            try { setCfg(getIMEConfig()) } catch (e) {}
        }, 2000)
        return () => { unsub?.(); clearInterval(cfgTimer) }
    }, [])

    // Don't render if no active IME context or no preedit
    if (!context || !context.preedit) {
        return null
    }

    const candidates = context.candidates || []
    const visibleCandidates = candidates.slice(0, cfg.candidateCount)

    // Position the panel below the input field
    const panelPosition: any = {
        position: "Absolute",
        minWidth: 200,
        maxWidth: 400,
    }

    if (inputRect) {
        panelPosition.left = inputRect.x
        panelPosition.top = inputRect.y + inputRect.height + 4
    } else {
        panelPosition.left = "30%"
        panelPosition.bottom = "10%"
    }

    const innerStyle: any = {
        flexDirection: "Column",
        display: "Flex",
        borderRadius: 6,
        borderWidth: 1,
        borderColor: "#3a3a5a",
        paddingTop: 6,
        paddingBottom: 4,
        paddingLeft: 8,
        paddingRight: 8,
        backgroundColor: cfg.bgColor,
        overflow: "hidden",
    }

    const panelContent = (
        <div style={{ flexDirection: "Column", display: "Flex" }}>
            {/* Preedit line with cursor */}
            <div style={{
                fontSize: 13,
                color: theme.accent,
                marginBottom: 4,
                paddingBottom: 4,
                borderBottomWidth: 1,
                borderBottomColor: "#2a2a4a",
                flexDirection: "Row",
                display: "Flex",
                alignItems: "Center",
            }}>
                {/* Text before cursor */}
                <div style={{ color: theme.accent }}>
                    {context.preedit.slice(0, context.cursorPos)}
                </div>
                {/* Cursor indicator */}
                <div style={{
                    width: 1.5,
                    height: 15,
                    backgroundColor: theme.textBright,
                    marginLeft: 0.5,
                    marginRight: 0.5,
                }} />
                {/* Text after cursor */}
                <div style={{ color: theme.textMuted }}>
                    {context.preedit.slice(context.cursorPos)}
                </div>
            </div>

            {/* Candidates */}
            <div style={{ flexDirection: "Column", display: "Flex" }}>
                {visibleCandidates.map((candidate, i) => {
                    const isHighlighted = i === context.highlightedIndex
                    return (
                        <div
                            key={i}
                            style={{
                                flexDirection: "Row",
                                display: "Flex",
                                alignItems: "Center",
                                paddingTop: 3,
                                paddingBottom: 3,
                                paddingLeft: 6,
                                paddingRight: 6,
                                borderRadius: 4,
                                backgroundColor: isHighlighted ? theme.accentDark : "transparent",
                            }}
                            onClick={() => {
                                try { chill.ime.selectCandidate(i) } catch (e) {}
                            }}
                        >
                            {/* Index number */}
                            <div style={{
                                fontSize: 11,
                                color: isHighlighted ? theme.textBright : theme.textMuted,
                                width: 18,
                                flexShrink: 0,
                            }}>
                                {`${i + 1}.`}
                            </div>

                            {/* Candidate text */}
                            <div style={{
                                fontSize: 14,
                                color: isHighlighted ? theme.textBright : theme.text,
                                marginRight: 6,
                            }}>
                                {candidate.text}
                            </div>

                            {/* Comment (if any) */}
                            {candidate.comment && (
                                <div style={{
                                    fontSize: 10,
                                    color: theme.textMuted,
                                }}>
                                    {candidate.comment}
                                </div>
                            )}
                        </div>
                    )
                })}
            </div>
        </div>
    )

    if (cfg.blurEnabled) {
        return (
            <div style={panelPosition}>
                <blur-panel
                    downsample={Number(cfg.blurDownsample)}
                    blur-iterations={Number(cfg.blurIterations)}
                    interval={Number(cfg.blurInterval)}
                    tint={cfg.blurTint}
                    style={innerStyle}
                >
                    {panelContent}
                </blur-panel>
            </div>
        )
    }

    return (
        <div style={{ ...panelPosition, ...innerStyle }}>
            {panelContent}
        </div>
    )
}
