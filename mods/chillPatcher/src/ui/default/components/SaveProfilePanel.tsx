import { h } from "preact"
import { useState, useCallback, useEffect } from "preact/hooks"
import { theme } from "./theme"
import { parse } from "./utils"

declare const chill: any

export const SaveProfilePanel = () => {
    const [profiles, setProfiles] = useState<string[]>([])
    const [active, setActive] = useState("")
    const [newName, setNewName] = useState("")
    const [inheritAll, setInheritAll] = useState(true)
    const [msg, setMsg] = useState<string | null>(null)
    const [loaded, setLoaded] = useState(false)

    const refresh = useCallback(() => {
        try {
            const sp = chill?.saveProfile
            if (!sp) return
            const rawList = sp.listProfiles()
            const rawActive = sp.getActiveProfile()
            const list = typeof rawList === "string" ? (parse<string[]>(rawList) || []) : (rawList || [])
            setProfiles(list)
            setActive(rawActive || "")
            setLoaded(true)
        } catch (e) {
            setMsg(`刷新失败: ${e}`)
        }
    }, [])

    if (!loaded) refresh()

    // 监听场景重载事件，自动刷新存档列表和当前存档
    useEffect(() => {
        chill?.game?.ensureEventBridge?.()
        const tk = chill?.game?.on?.("sceneReloaded", () => {
            setLoaded(false)
            refresh()
        })
        return () => { if (tk) chill?.game?.off?.(tk) }
    }, [])

    const showMsg = (text: string) => { setMsg(text); setTimeout(() => setMsg(null), 4000) }

    const onCreate = () => {
        const name = newName.trim()
        if (!name) { showMsg("请输入名称"); return }
        try {
            const ok = chill.saveProfile.createProfile(name, inheritAll ? ["*"] : null)
            if (ok) { showMsg(`已创建: ${name}`); setNewName(""); refresh() }
            else showMsg("创建失败 (可能已存在或被锁定)")
        } catch (e) { showMsg(`错误: ${e}`) }
    }

    const onDelete = (name: string) => {
        try {
            const ok = chill.saveProfile.deleteProfile(name)
            if (ok) { showMsg(`已删除: ${name}`); refresh() }
            else showMsg("删除失败 (可能是当前存档或被锁定)")
        } catch (e) { showMsg(`错误: ${e}`) }
    }

    const onSwitch = (name: string) => {
        try {
            showMsg(`正在切换到: ${name || "主存档"} ...`)
            chill.saveProfile.switchProfile(name)
        } catch (e) { showMsg(`切换失败: ${e}`) }
    }

    const locked = chill?.saveProfile?.locked ?? false

    return (
        <div style={{ flexDirection: "Column", display: "Flex", flexGrow: 1 }}>
            {/* 消息条 */}
            {msg && (
                <div style={{
                    backgroundColor: theme.bgCard, borderRadius: theme.radius,
                    paddingTop: 6, paddingBottom: 6, paddingLeft: 12, paddingRight: 12,
                    marginBottom: 8, fontSize: 11, color: theme.warning,
                }}>
                    {msg}
                </div>
            )}

            {/* 状态栏 */}
            <div style={{
                flexDirection: "Row", display: "Flex", alignItems: "Center",
                marginBottom: 8, fontSize: 12, color: theme.textMuted,
            }}>
                <div style={{ color: theme.text }}>
                    {`当前: ${active || "主存档"}`}
                </div>
                {locked && <div style={{ color: theme.danger, marginLeft: 12 }}>{"🔒 已锁定"}</div>}
                <div style={{ marginLeft: "auto", color: theme.accent, fontSize: 11 }}
                    onClick={refresh}>{"刷新"}</div>
            </div>

            {/* 创建区域 */}
            <div style={{
                flexDirection: "Row", display: "Flex", alignItems: "Center",
                backgroundColor: theme.bgCard, borderRadius: theme.radius,
                paddingTop: 8, paddingBottom: 8, paddingLeft: 12, paddingRight: 12,
                marginBottom: 8,
            }}>
                <textfield value={newName}
                    onValueChanged={(e: any) => setNewName(e.newValue ?? "")}
                    style={{
                        flexGrow: 1, fontSize: 12, color: theme.text,
                        backgroundColor: theme.bgPanel, borderRadius: 4,
                        paddingTop: 4, paddingBottom: 4, paddingLeft: 8, paddingRight: 8,
                        borderWidth: 1, borderColor: theme.border,
                    }} />
                <div style={{
                    marginLeft: 8, fontSize: 11,
                    color: inheritAll ? theme.success : theme.textMuted,
                }} onClick={() => setInheritAll(!inheritAll)}>
                    {inheritAll ? "继承全部" : "空白存档"}
                </div>
                <div style={{
                    marginLeft: 8, paddingTop: 4, paddingBottom: 4,
                    paddingLeft: 10, paddingRight: 10, borderRadius: 4,
                    fontSize: 11, backgroundColor: theme.accentDark, color: theme.textBright,
                }} onClick={onCreate}>
                    {"创建"}
                </div>
            </div>

            {/* 主存档条目 */}
            <ProfileRow name="" displayName="主存档 (默认)"
                isActive={!active} onSwitch={onSwitch} onDelete={null} />

            {/* 子存档列表 */}
            {profiles.map(name => (
                <ProfileRow key={name} name={name} displayName={name}
                    isActive={active === name}
                    onSwitch={onSwitch} onDelete={onDelete} />
            ))}

            {profiles.length === 0 && (
                <div style={{ fontSize: 12, color: theme.textMuted, padding: 12 }}>
                    {"暂无子存档"}
                </div>
            )}

        </div>
    )
}

const ProfileRow = ({ name, displayName, isActive, onSwitch, onDelete }: {
    name: string, displayName: string, isActive: boolean,
    onSwitch: (name: string) => void, onDelete: ((name: string) => void) | null,
}) => (
    <div style={{
        flexDirection: "Row", display: "Flex", alignItems: "Center",
        backgroundColor: isActive ? theme.bgHover : theme.bgCard,
        borderRadius: 4, paddingLeft: 12, paddingRight: 10,
        marginBottom: 3, height: 36,
    }}>
        <div style={{
            width: 8, height: 8, borderRadius: 4, marginRight: 8,
            backgroundColor: isActive ? theme.success : theme.textMuted,
        }} />
        <div style={{ flexGrow: 1, fontSize: 12, color: isActive ? theme.accent : theme.text }}>
            {displayName}
        </div>
        {!isActive && (
            <div style={{
                paddingTop: 2, paddingBottom: 2, paddingLeft: 8, paddingRight: 8,
                borderRadius: 4, fontSize: 11,
                backgroundColor: theme.accentDark, color: theme.textBright, marginRight: 4,
            }} onClick={() => onSwitch(name)}>
                {"切换"}
            </div>
        )}
        {isActive && (
            <div style={{ fontSize: 11, color: theme.success }}>{"● 当前"}</div>
        )}
        {onDelete && !isActive && (
            <div style={{
                paddingTop: 2, paddingBottom: 2, paddingLeft: 8, paddingRight: 8,
                borderRadius: 4, fontSize: 11,
                backgroundColor: theme.danger, color: theme.textBright,
            }} onClick={() => onDelete(name)}>
                {"删除"}
            </div>
        )}
    </div>
)
