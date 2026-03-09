import { h } from "preact"
import { useState, useEffect } from "preact/hooks"
import { theme } from "./theme"
import { getIMEConfig } from "./IMECandidatePanel"

declare const chill: any

interface FieldDef {
    key: string
    label: string
    desc: string
    type: "bool" | "int" | "string"
}

const fields: FieldDef[] = [
    { key: "IME.BlurEnabled", label: "启用模糊", desc: "候选词面板毛玻璃模糊效果", type: "bool" },
    { key: "IME.BlurDownsample", label: "分辨率缩放", desc: "1=原图, 2=1/2, 4=1/4 (越大越省性能)", type: "int" },
    { key: "IME.BlurIterations", label: "模糊迭代次数", desc: "1-8, 越大越模糊 (建议 4-5)", type: "int" },
    { key: "IME.BlurInterval", label: "帧间隔", desc: "每N个游戏帧更新一次模糊 (1=每帧)", type: "int" },
    { key: "IME.BlurTint", label: "模糊叠加色", desc: "叠加在模糊效果上的颜色 (hex, 如 #ffffff1a)", type: "string" },
    { key: "IME.BackgroundColor", label: "面板背景色", desc: "候选词面板背景色 (hex, 如 #1e1e2eF0)", type: "string" },
    { key: "IME.CandidateCount", label: "候选词数量", desc: "显示的候选词个数", type: "int" },
]

export const IMESettingsPanel = () => {
    const [values, setValues] = useState<Record<string, any>>({})
    const [editingKey, setEditingKey] = useState<string | null>(null)
    const [draft, setDraft] = useState("")

    const refresh = () => {
        const cfg = getIMEConfig()
        const map: Record<string, any> = {
            "IME.BlurEnabled": cfg.blurEnabled,
            "IME.BlurDownsample": cfg.blurDownsample,
            "IME.BlurIterations": cfg.blurIterations,
            "IME.BlurInterval": cfg.blurInterval,
            "IME.BlurTint": cfg.blurTint,
            "IME.BackgroundColor": cfg.bgColor,
            "IME.CandidateCount": cfg.candidateCount,
        }
        setValues(map)
    }

    useEffect(() => {
        refresh()
        const timer = setInterval(refresh, 2000)
        return () => clearInterval(timer)
    }, [])

    const save = (key: string, value: any) => {
        try {
            chill.config.appSet(key, value)
            chill.config.save()
            refresh()
        } catch (e) {
            console.error("IMESettings save error:", e)
        }
    }

    return (
        <div style={{ flexDirection: "Column", display: "Flex", flexGrow: 1 }}>
            <div style={{ fontSize: 13, color: theme.textMuted, marginBottom: 10 }}>
                调整输入法候选词面板的外观和模糊效果，修改后实时生效。
            </div>

            {fields.map(f => {
                const val = values[f.key]
                const isEditing = editingKey === f.key

                return (
                    <div
                        key={f.key}
                        style={{
                            flexDirection: "Column",
                            display: "Flex",
                            backgroundColor: theme.bgCard,
                            borderRadius: theme.radius,
                            paddingTop: 10,
                            paddingBottom: 10,
                            paddingLeft: 14,
                            paddingRight: 14,
                            marginBottom: 6,
                        }}
                    >
                        <div style={{
                            flexDirection: "Row",
                            display: "Flex",
                            justifyContent: "SpaceBetween",
                            alignItems: "Center",
                        }}>
                            <div style={{ fontSize: 13, color: theme.text }}>{f.label}</div>

                            {f.type === "bool" ? (
                                <div
                                    style={{
                                        paddingTop: 3,
                                        paddingBottom: 3,
                                        paddingLeft: 10,
                                        paddingRight: 10,
                                        borderRadius: 4,
                                        fontSize: 12,
                                        backgroundColor: val ? theme.success : theme.danger,
                                        color: theme.textBright,
                                    }}
                                    onClick={() => save(f.key, !val)}
                                >
                                    {val ? "ON" : "OFF"}
                                </div>
                            ) : isEditing ? (
                                <div style={{ flexDirection: "Row", display: "Flex", alignItems: "Center" }}>
                                    <textfield
                                        value={draft}
                                        onValueChanged={(e: any) => setDraft(e.newValue ?? e.target?.value ?? draft)}
                                        onKeyDown={(e: any) => {
                                            if (e.keyCode === 13) {
                                                const v = f.type === "int" ? parseInt(draft, 10) : draft
                                                if (f.type === "int" && isNaN(v as number)) {
                                                    setEditingKey(null)
                                                    return
                                                }
                                                save(f.key, v)
                                                setEditingKey(null)
                                            }
                                        }}
                                        style={{
                                            fontSize: 12,
                                            color: theme.text,
                                            backgroundColor: theme.bg,
                                            borderRadius: 4,
                                            paddingLeft: 6,
                                            paddingRight: 6,
                                            paddingTop: 2,
                                            paddingBottom: 2,
                                            width: 100,
                                        }}
                                    />
                                    <div
                                        style={{
                                            marginLeft: 6,
                                            fontSize: 12,
                                            color: theme.success,
                                            paddingLeft: 6,
                                            paddingRight: 6,
                                        }}
                                        onClick={() => {
                                            const v = f.type === "int" ? parseInt(draft, 10) : draft
                                            if (f.type === "int" && isNaN(v as number)) {
                                                setEditingKey(null)
                                                return
                                            }
                                            save(f.key, v)
                                            setEditingKey(null)
                                        }}
                                    >
                                        ✓
                                    </div>
                                    <div
                                        style={{
                                            marginLeft: 4,
                                            fontSize: 12,
                                            color: theme.danger,
                                            paddingLeft: 6,
                                            paddingRight: 6,
                                        }}
                                        onClick={() => setEditingKey(null)}
                                    >
                                        ✕
                                    </div>
                                </div>
                            ) : (
                                <div
                                    style={{
                                        paddingTop: 3,
                                        paddingBottom: 3,
                                        paddingLeft: 10,
                                        paddingRight: 10,
                                        borderRadius: 4,
                                        fontSize: 12,
                                        backgroundColor: theme.bgPanel,
                                        color: theme.accent,
                                    }}
                                    onClick={() => {
                                        setEditingKey(f.key)
                                        setDraft(String(val ?? ""))
                                    }}
                                >
                                    {String(val ?? "")}
                                </div>
                            )}
                        </div>
                        <div style={{ fontSize: 11, color: theme.textMuted, marginTop: 4 }}>
                            {f.desc}
                        </div>
                    </div>
                )
            })}
        </div>
    )
}
