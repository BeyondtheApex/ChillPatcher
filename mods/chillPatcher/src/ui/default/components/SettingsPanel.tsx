import { h } from "preact"
import { useState, useEffect, useRef } from "preact/hooks"
import { theme } from "./theme"
import { parse } from "./utils"
import { Pagination } from "./Pagination"

declare const chill: any

interface ConfigEntry {
    section: string
    key: string
    value: any
    defaultValue: any
    type: string
    description: string
    acceptableValues?: { type: string; description: string }
}

const SETTINGS_PER_PAGE = 6
const POLL_INTERVAL = 3000

export const SettingsPanel = () => {
    const [sections, setSections] = useState<string[]>([])
    const [activeSection, setActiveSection] = useState("")
    const [entries, setEntries] = useState<ConfigEntry[]>([])
    const [page, setPage] = useState(0)
    const lastJson = useRef("")

    useEffect(() => {
        try {
            const secs = parse<string[]>(chill.config.getSections()) || []
            setSections(secs)
            if (secs.length > 0) {
                setActiveSection(secs[0])
            }
        } catch (e) {
            console.error("SettingsPanel init error:", e)
        }
    }, [])

    const refreshEntries = (section: string) => {
        if (!section) return
        try {
            const json = chill.config.getAll(section) as string
            if (json === lastJson.current) return
            lastJson.current = json
            setEntries(parse<ConfigEntry[]>(json) || [])
        } catch (e) {
            console.error("SettingsPanel entries error:", e)
        }
    }

    useEffect(() => {
        if (!activeSection) return
        lastJson.current = ""
        refreshEntries(activeSection)
        setPage(0)
        const timer = setInterval(() => refreshEntries(activeSection), POLL_INTERVAL)
        return () => clearInterval(timer)
    }, [activeSection])

    const handleChange = (entry: ConfigEntry, newValue: any) => {
        try {
            chill.config.set(entry.section, entry.key, newValue)
            chill.config.save()
            lastJson.current = ""
            refreshEntries(activeSection)
        } catch (e) {
            console.error("SettingsPanel change error:", e)
        }
    }

    const totalPages = Math.max(1, Math.ceil(entries.length / SETTINGS_PER_PAGE))
    const pageEntries = entries.slice(page * SETTINGS_PER_PAGE, (page + 1) * SETTINGS_PER_PAGE)

    return (
        <div style={{ flexDirection: "Column", display: "Flex", flexGrow: 1 }}>
            {/* Section 选择 */}
            <div style={{
                flexDirection: "Row",
                display: "Flex",
                flexWrap: "Wrap",
                marginBottom: 12,
            }}>
                {sections.map(sec => (
                    <div
                        key={sec}
                        style={{
                            paddingTop: 4,
                            paddingBottom: 4,
                            paddingLeft: 12,
                            paddingRight: 12,
                            marginRight: 6,
                            marginBottom: 4,
                            fontSize: 12,
                            borderRadius: theme.radius,
                            backgroundColor: activeSection === sec ? theme.accent : theme.bgCard,
                            color: activeSection === sec ? theme.textBright : theme.textMuted,
                        }}
                        onClick={() => setActiveSection(sec)}
                    >
                        {sec}
                    </div>
                ))}
            </div>

            {/* 配置项列表 */}
            <div style={{ flexDirection: "Column", display: "Flex", flexGrow: 1 }}>
                {pageEntries.map(entry => (
                    <ConfigItem
                        key={`${entry.section}::${entry.key}`}
                        entry={entry}
                        onChange={handleChange}
                    />
                ))}
            </div>

            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </div>
    )
}

const ConfigItem = ({ entry, onChange }: { entry: ConfigEntry; onChange: (e: ConfigEntry, v: any) => void }) => {
    return (
        <div style={{
            flexDirection: "Column",
            display: "Flex",
            backgroundColor: theme.bgCard,
            borderRadius: theme.radius,
            paddingTop: 10,
            paddingBottom: 10,
            paddingLeft: 14,
            paddingRight: 14,
            marginBottom: 6,
        }}>
            <div style={{ flexDirection: "Row", display: "Flex", justifyContent: "SpaceBetween", alignItems: "Center" }}>
                <div style={{ fontSize: 13, color: theme.text }}>{entry.key}</div>
                <ConfigValueEditor entry={entry} onChange={onChange} />
            </div>
            {entry.description ? (
                <div style={{ fontSize: 11, color: theme.textMuted, marginTop: 4 }}>
                    {entry.description.split("\n")[0]}
                </div>
            ) : null}
        </div>
    )
}

const ConfigValueEditor = ({ entry, onChange }: { entry: ConfigEntry; onChange: (e: ConfigEntry, v: any) => void }) => {
    const [editing, setEditing] = useState(false)
    const [draft, setDraft] = useState("")

    if (entry.type === "bool") {
        return (
            <div
                style={{
                    paddingTop: 3,
                    paddingBottom: 3,
                    paddingLeft: 10,
                    paddingRight: 10,
                    borderRadius: 4,
                    fontSize: 12,
                    backgroundColor: entry.value ? theme.success : theme.danger,
                    color: theme.textBright,
                }}
                onClick={() => onChange(entry, !entry.value)}
            >
                {entry.value ? "ON" : "OFF"}
            </div>
        )
    }

    if (editing) {
        const confirm = () => {
            const val = entry.type === "int" || entry.type === "float" || entry.type === "double"
                ? Number(draft) : draft
            if (entry.type === "int" || entry.type === "float" || entry.type === "double") {
                if (isNaN(val as number)) {
                    setEditing(false)
                    return
                }
            }
            onChange(entry, val)
            setEditing(false)
        }

        return (
            <div style={{ flexDirection: "Row", display: "Flex", alignItems: "Center" }}>
                <textfield
                    value={draft}
                    onValueChanged={(e: any) => setDraft(e.newValue ?? e.target?.value ?? draft)}
                    onKeyDown={(e: any) => { if (e.keyCode === 13) confirm() }}
                    style={{
                        fontSize: 12,
                        color: theme.text,
                        backgroundColor: theme.bg,
                        borderRadius: 4,
                        paddingLeft: 6,
                        paddingRight: 6,
                        paddingTop: 2,
                        paddingBottom: 2,
                        minWidth: 80,
                    }}
                />
                <div
                    style={{
                        fontSize: 11,
                        color: theme.success,
                        paddingLeft: 6,
                    }}
                    onClick={confirm}
                >
                    {`✓`}
                </div>
                <div
                    style={{
                        fontSize: 11,
                        color: theme.danger,
                        paddingLeft: 4,
                    }}
                    onClick={() => setEditing(false)}
                >
                    {`✕`}
                </div>
            </div>
        )
    }

    return (
        <div
            style={{
                fontSize: 12,
                color: theme.accent,
                paddingLeft: 8,
            }}
            onClick={() => { setDraft(String(entry.value)); setEditing(true) }}
        >
            {String(entry.value)}
        </div>
    )
}
