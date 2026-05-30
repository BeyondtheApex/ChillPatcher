import { h } from "preact"
import { useEffect, useState, useCallback } from "preact/hooks"

declare const __registerPlugin: any
declare const chill: any

// ─── 颜色常量 (与其他插件保持一致) ───
const BG    = "#0b1020"
const CARD  = "#111827"
const PANEL = "#1e293b"
const TEXT  = "#e5e7eb"
const DIM   = "#94a3b8"
const ACCENT = "#7dd3fc"
const OK    = "#34d399"
const WARN  = "#f59e0b"
const ERR   = "#f87171"
const BORDER = "rgba(255,255,255,0.08)"

// ─── 分页常量 ───
const LOBBIES_PER_PAGE = 4
const MEMBERS_PER_PAGE = 4

// ─── 工具函数 ───
const json = (v: any, fb: any) => {
    try { return typeof v === "string" ? JSON.parse(v) : (v ?? fb) } catch { return fb }
}

function getSr(): any {
    return (chill as any)?.studyRoom ?? null
}

// ─── 类型定义 ───
interface LobbyEntry {
    lobbyId: string
    hostName: string
    memberCount: number
    maxMembers: number
    hasPassword: boolean
    pomodoroState?: string  // working / resting / idle
    mode?: string
}

interface Member {
    steamId: string
    personaName: string
    isHost: boolean
    syncState?: string
}

interface RoomInfo {
    lobbyId: string
    inviteCode: string
    isHost: boolean
    hostSteamId: string
}

// ─── 页面类型 ───
type View = "lobby" | "create" | "join" | "connecting" | "room"

// ─── 通用小组件 ───

const Separator = () => (
    <div style={{ height: 1, backgroundColor: BORDER, marginTop: 8, marginBottom: 8 }} />
)

const SectionTitle = ({ text }: { text: string }) => (
    <div style={{ fontSize: 10, color: DIM, letterSpacing: 0.5, marginBottom: 6, paddingLeft: 2 }}>
        {text.toUpperCase()}
    </div>
)

const Btn = ({ text, onClick, primary = false, danger = false, disabled = false }: {
    text: string; onClick: () => void; primary?: boolean; danger?: boolean; disabled?: boolean
}) => {
    const bg = disabled
        ? "rgba(255,255,255,0.04)"
        : danger
            ? "rgba(248,113,113,0.15)"
            : primary
                ? "rgba(52,211,153,0.18)"
                : "rgba(125,211,252,0.12)"
    const color = disabled
        ? "rgba(255,255,255,0.25)"
        : danger ? ERR : primary ? OK : ACCENT
    return (
        <div
            onPointerDown={disabled ? undefined : onClick}
            style={{
                fontSize: 12, color, backgroundColor: bg,
                borderRadius: 6, paddingTop: 7, paddingBottom: 7,
                paddingLeft: 14, paddingRight: 14,
                unityTextAlign: "MiddleCenter",
            }}
        >{text}</div>
    )
}

const SmallBtn = ({ text, onClick, color = ACCENT }: { text: string; onClick: () => void; color?: string }) => (
    <div
        onPointerDown={onClick}
        style={{
            fontSize: 11, color,
            backgroundColor: "rgba(255,255,255,0.06)",
            borderRadius: 5, paddingTop: 4, paddingBottom: 4,
            paddingLeft: 10, paddingRight: 10,
            unityTextAlign: "MiddleCenter",
        }}
    >{text}</div>
)

// ─── 翻页控制条 ───
const Pager = ({ page, total, onPrev, onNext }: {
    page: number; total: number; onPrev: () => void; onNext: () => void
}) => (
    <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", justifyContent: "SpaceBetween", marginTop: 6 }}>
        <SmallBtn text="‹" onClick={onPrev} color={page > 0 ? ACCENT : DIM} />
        <div style={{ fontSize: 10, color: DIM }}>{`${page + 1} / ${Math.max(1, total)}`}</div>
        <SmallBtn text="›" onClick={onNext} color={page < total - 1 ? ACCENT : DIM} />
    </div>
)

// ─── 大厅房间卡片 ───
const LobbyCard = ({ entry, onJoin }: { entry: LobbyEntry; onJoin: (e: LobbyEntry) => void }) => {
    const stateColor = entry.pomodoroState === "working"
        ? ACCENT : entry.pomodoroState === "resting" ? OK : DIM
    const stateText = entry.pomodoroState === "working"
        ? "专注中" : entry.pomodoroState === "resting" ? "休息中" : "空闲"
    return (
        <div style={{
            backgroundColor: PANEL, borderRadius: 8,
            paddingLeft: 12, paddingRight: 12, paddingTop: 10, paddingBottom: 10,
            marginBottom: 6, display: "Flex", flexDirection: "Row", alignItems: "Center",
        }}>
            <div style={{ flexGrow: 1 }}>
                <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 3 }}>
                    <div style={{ fontSize: 13, color: TEXT, unityFontStyleAndWeight: "Bold" }}>{entry.hostName}</div>
                    {entry.hasPassword && (
                        <div style={{ fontSize: 9, color: WARN, backgroundColor: "rgba(245,158,11,0.12)", borderRadius: 4, paddingLeft: 5, paddingRight: 5, paddingTop: 2, paddingBottom: 2, marginLeft: 6 }}>
                            密码
                        </div>
                    )}
                </div>
                <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                    <div style={{ fontSize: 10, color: DIM }}>{`${entry.memberCount}/${entry.maxMembers}人`}</div>
                    <div style={{ fontSize: 10, color: stateColor, marginLeft: 8 }}>{`● ${stateText}`}</div>
                </div>
            </div>
            <SmallBtn text="加入" onClick={() => onJoin(entry)} color={ACCENT} />
        </div>
    )
}

// ─── 成员行 ───
// ─── 大厅视图 ───
const LobbyView = ({ onSetView, onJoinEntry }: {
    onSetView: (v: View) => void
    onJoinEntry: (entry: LobbyEntry) => void
}) => {
    const [lobbies, setLobbies] = useState<LobbyEntry[]>([])
    const [loading, setLoading] = useState(false)
    const [inviteCode, setInviteCode] = useState("")
    const [page, setPage] = useState(0)

    const refresh = useCallback(() => {
        setLoading(true)
        getSr()?.refreshLobbyList?.()
    }, [])

    useEffect(() => {
        // 读取已缓存的列表并触发刷新
        const cached = json(getSr()?.getLobbyList?.(), [])
        setLobbies(cached)
        refresh()
    }, [])

    // 在父组件的事件里更新列表（lobbyListUpdated 在主面板处理，通过 prop 传下来）
    // 此处也支持每 3 秒轮询缓存
    useEffect(() => {
        const t = setInterval(() => {
            const cur = json(getSr()?.getLobbyList?.(), null)
            if (cur) {
                setLobbies(cur)
                setLoading(false)
            }
        }, 1000)
        return () => clearInterval(t)
    }, [])

    const totalPages = Math.ceil(lobbies.length / LOBBIES_PER_PAGE)
    const pageItems = lobbies.slice(page * LOBBIES_PER_PAGE, (page + 1) * LOBBIES_PER_PAGE)

    const handleJoinByCode = () => {
        const code = inviteCode.trim()
        if (!code) return
        const result = json(getSr()?.searchByInviteCode?.(code), null)
        if (result?.lobbyId) {
            onJoinEntry({ lobbyId: result.lobbyId, hostName: result.hostName || "", memberCount: 0, maxMembers: 8, hasPassword: !!result.hasPassword })
        }
    }

    return (
        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 14 }}>
            {/* 顶部标题行 */}
            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 10 }}>
                <div style={{ fontSize: 15, color: TEXT, unityFontStyleAndWeight: "Bold", flexGrow: 1 }}>自习室</div>
                <SmallBtn text="刷新" onClick={refresh} color={loading ? DIM : ACCENT} />
            </div>

            {/* 邀请码搜索 */}
            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 8 }}>
                <textfield
                    value={inviteCode}
                    onValueChanged={(e: any) => setInviteCode(e.newValue ?? "")}
                    style={{
                        flexGrow: 1, fontSize: 12, color: TEXT,
                        backgroundColor: PANEL, borderWidth: 0, borderRadius: 6,
                        paddingTop: 6, paddingBottom: 6, paddingLeft: 10, paddingRight: 10,
                        marginRight: 6,
                    }}
                />
                <SmallBtn text="搜索" onClick={handleJoinByCode} color={ACCENT} />
            </div>

            <Separator />

            {/* 房间列表（固定4行）*/}
            <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column" }}>
                {loading && lobbies.length === 0 ? (
                    <div style={{ flexGrow: 1, display: "Flex", alignItems: "Center", justifyContent: "Center" }}>
                        <div style={{ fontSize: 12, color: DIM }}>正在搜索...</div>
                    </div>
                ) : pageItems.length === 0 ? (
                    <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", alignItems: "Center", justifyContent: "Center" }}>
                        <div style={{ fontSize: 20, color: DIM, marginBottom: 6 }}>󰳠</div>
                        <div style={{ fontSize: 12, color: DIM }}>暂无自习室</div>
                    </div>
                ) : (
                    <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column" }}>
                        {pageItems.map(e => <LobbyCard key={e.lobbyId} entry={e} onJoin={onJoinEntry} />)}
                        {/* 补足空行，保证布局稳定 */}
                        {Array.from({ length: LOBBIES_PER_PAGE - pageItems.length }).map((_, i) => (
                            <div key={`pad-${i}`} style={{ height: 52, marginBottom: 6 }} />
                        ))}
                    </div>
                )}
            </div>

            {/* 翻页 */}
            <Pager
                page={page}
                total={totalPages}
                onPrev={() => setPage(p => Math.max(0, p - 1))}
                onNext={() => setPage(p => Math.min(totalPages - 1, p + 1))}
            />

            <Separator />

            <Btn text="创建自习室" onClick={() => onSetView("create")} primary />
        </div>
    )
}

// ─── 创建视图 ───
const CreateView = ({ onBack, onConnecting }: {
    onBack: () => void
    onConnecting: () => void
}) => {
    const [roomName, setRoomName] = useState("")
    const [password, setPassword] = useState("")
    const [maxMembers, setMaxMembers] = useState(4)
    const [inheritSave, setInheritSave] = useState(true)

    const canCreate = roomName.trim().length > 0

    const doCreate = () => {
        if (!canCreate) return
        getSr()?.createRoom?.(JSON.stringify({
            roomName: roomName.trim(),
            password: password,
            maxMembers,
            inheritSave,
        }))
        onConnecting()
    }

    const stepMembers = (d: number) => setMaxMembers(v => Math.max(2, Math.min(8, v + d)))

    return (
        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 14 }}>
            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 10 }}>
                <SmallBtn text="‹" onClick={onBack} />
                <div style={{ fontSize: 15, color: TEXT, unityFontStyleAndWeight: "Bold", marginLeft: 8 }}>创建自习室</div>
            </div>

            <Separator />

            <SectionTitle text="房间名称" />
            <textfield
                value={roomName}
                onValueChanged={(e: any) => setRoomName(e.newValue ?? "")}
                style={{
                    fontSize: 13, color: TEXT, backgroundColor: PANEL, borderWidth: 0,
                    borderRadius: 6, paddingTop: 8, paddingBottom: 8,
                    paddingLeft: 10, paddingRight: 10, marginBottom: 10,
                }}
            />

            <SectionTitle text="密码（可选）" />
            <textfield
                value={password}
                onValueChanged={(e: any) => setPassword(e.newValue ?? "")}
                style={{
                    fontSize: 13, color: TEXT, backgroundColor: PANEL, borderWidth: 0,
                    borderRadius: 6, paddingTop: 8, paddingBottom: 8,
                    paddingLeft: 10, paddingRight: 10, marginBottom: 10,
                }}
            />

            <SectionTitle text="人数上限" />
            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 12 }}>
                <SmallBtn text="-" onClick={() => stepMembers(-1)} />
                <div style={{ fontSize: 16, color: TEXT, marginLeft: 14, marginRight: 14, unityFontStyleAndWeight: "Bold" }}>{`${maxMembers}`}</div>
                <SmallBtn text="+" onClick={() => stepMembers(1)} />
                <div style={{ fontSize: 11, color: DIM, marginLeft: 10 }}>{`最多 8 人`}</div>
            </div>

            {/* 存档继承 */}
            <div
                onPointerDown={() => setInheritSave(v => !v)}
                style={{
                    display: "Flex", flexDirection: "Row", alignItems: "Center",
                    backgroundColor: PANEL, borderRadius: 8,
                    paddingLeft: 12, paddingRight: 12, paddingTop: 10, paddingBottom: 10,
                    marginBottom: 12,
                }}
            >
                <div style={{
                    width: 18, height: 18, borderWidth: 2,
                    borderColor: inheritSave ? ACCENT : DIM, borderRadius: 4,
                    backgroundColor: inheritSave ? "rgba(125,211,252,0.16)" : "transparent",
                    display: "Flex", alignItems: "Center", justifyContent: "Center",
                    marginRight: 10, flexShrink: 0,
                }}>
                    {inheritSave && <div style={{ fontSize: 11, color: TEXT }}>✓</div>}
                </div>
                <div style={{ flexGrow: 1 }}>
                    <div style={{ fontSize: 12, color: TEXT }}>继承当前存档</div>
                    <div style={{ fontSize: 10, color: DIM, marginTop: 2 }}>将待办/日历/备忘录等数据带入自习室</div>
                </div>
            </div>

            <div style={{ flexGrow: 1 }} />

            <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" }}>
                <Btn text="取消" onClick={onBack} />
                <Btn text="创建" onClick={doCreate} primary disabled={!canCreate} />
            </div>
        </div>
    )
}

// ─── 密码输入视图（加入有密码房间时） ───
const JoinView = ({ entry, onBack, onConnecting }: {
    entry: LobbyEntry
    onBack: () => void
    onConnecting: () => void
}) => {
    const [password, setPassword] = useState("")

    const doJoin = () => {
        getSr()?.joinRoom?.(entry.lobbyId, password)
        onConnecting()
    }

    return (
        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 14 }}>
            <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 10 }}>
                <SmallBtn text="‹" onClick={onBack} />
                <div style={{ fontSize: 15, color: TEXT, unityFontStyleAndWeight: "Bold", marginLeft: 8 }}>加入自习室</div>
            </div>

            <Separator />

            <div style={{ backgroundColor: PANEL, borderRadius: 8, padding: 12, marginBottom: 12 }}>
                <div style={{ fontSize: 13, color: TEXT, unityFontStyleAndWeight: "Bold", marginBottom: 2 }}>{entry.hostName}</div>
                <div style={{ fontSize: 11, color: DIM }}>{`${entry.memberCount}/${entry.maxMembers} 人`}</div>
            </div>

            {entry.hasPassword && (
                <div>
                    <SectionTitle text="房间密码" />
                    <textfield
                        value={password}
                        onValueChanged={(e: any) => setPassword(e.newValue ?? "")}
                        style={{
                            fontSize: 13, color: TEXT, backgroundColor: PANEL, borderWidth: 0,
                            borderRadius: 6, paddingTop: 8, paddingBottom: 8,
                            paddingLeft: 10, paddingRight: 10, marginBottom: 12,
                        }}
                    />
                </div>
            )}

            <div style={{ flexGrow: 1 }} />

            <div style={{ display: "Flex", flexDirection: "Row", justifyContent: "SpaceBetween" }}>
                <Btn text="取消" onClick={onBack} />
                <Btn text="加入" onClick={doJoin} primary />
            </div>
        </div>
    )
}

// ─── 连接中视图 ───
const ConnectingView = ({ msg }: { msg: string }) => (
    <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", alignItems: "Center", justifyContent: "Center", padding: 20 }}>
        <div style={{ fontSize: 16, color: DIM, marginBottom: 8 }}>⏳</div>
        <div style={{ fontSize: 13, color: TEXT, unityTextAlign: "MiddleCenter" }}>{msg}</div>
    </div>
)

// ─── 在房间内视图 ───
const RoomView = ({ onSetView }: { onSetView: (v: View) => void }) => {
    const sr = getSr()
    const [members, setMembers] = useState<Member[]>([])
    const [roomInfo, setRoomInfo] = useState<RoomInfo | null>(null)
    const [myInfo, setMyInfo] = useState<{ steamId: string; isHost: boolean } | null>(null)
    const [syncStates, setSyncStates] = useState<Record<string, any>>({})
    const [membersPage, setMembersPage] = useState(0)
    const [copied, setCopied] = useState(false)

    const refreshMembers = useCallback(() => {
        const m = json(sr?.getMembers?.(), [])
        setMembers(m)

        const states: Record<string, any> = {}
        for (const mb of m as Member[]) {
            if (!mb.isHost) {
                states[mb.steamId] = json(sr?.getMemberSyncState?.(mb.steamId), null)
            }
        }
        setSyncStates(states)
    }, [])

    useEffect(() => {
        const info = json(sr?.getRoomInfo?.(), null)
        const me = json(sr?.getMyInfo?.(), null)
        setRoomInfo(info)
        setMyInfo(me)
        refreshMembers()

        const t = setInterval(refreshMembers, 1500)
        return () => clearInterval(t)
    }, [])

    const copyInvite = () => {
        if (!roomInfo?.inviteCode) return
        try { (chill as any)?.io?.setClipboard?.(roomInfo.inviteCode) } catch { }
        setCopied(true)
        setTimeout(() => setCopied(false), 2000)
    }

    const doLeave = () => {
        if (myInfo?.isHost) {
            sr?.closeRoom?.()
        } else {
            sr?.leaveRoom?.()
        }
        onSetView("lobby")
    }

    const totalPages = Math.ceil(members.length / MEMBERS_PER_PAGE)
    const pageMembers = members.slice(membersPage * MEMBERS_PER_PAGE, (membersPage + 1) * MEMBERS_PER_PAGE)

    return (
        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", padding: 14 }}>
            {/* 顶部：房间信息 */}
            <div style={{ backgroundColor: PANEL, borderRadius: 8, paddingLeft: 12, paddingRight: 12, paddingTop: 10, paddingBottom: 10, marginBottom: 10 }}>
                <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center", marginBottom: 4 }}>
                    <div style={{ flexGrow: 1 }}>
                        <div style={{ fontSize: 14, color: TEXT, unityFontStyleAndWeight: "Bold" }}>
                            {myInfo?.isHost ? "我的自习室" : "自习室"}
                        </div>
                        <div style={{ fontSize: 10, color: DIM, marginTop: 2 }}>
                            {myInfo?.isHost ? `${members.length} 人在线 · 主机` : `${members.length} 人在线`}
                        </div>
                    </div>
                    {roomInfo?.inviteCode && (
                        <SmallBtn text={copied ? "已复制" : "复制邀请码"} onClick={copyInvite} color={copied ? OK : ACCENT} />
                    )}
                </div>
            </div>

            <SectionTitle text="成员" />

            {/* 成员列表（固定4行） */}
            <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column" }}>
                {pageMembers.map(m => (
                    <MemberRow
                        key={m.steamId}
                        member={m}
                        isMe={m.steamId === myInfo?.steamId}
                        syncState={syncStates[m.steamId] ?? null}
                    />
                ))}
                {Array.from({ length: MEMBERS_PER_PAGE - pageMembers.length }).map((_, i) => (
                    <div key={`pad-${i}`} style={{ height: 42, marginBottom: 5 }} />
                ))}
            </div>

            {/* 翻页 */}
            <Pager
                page={membersPage}
                total={totalPages}
                onPrev={() => setMembersPage(p => Math.max(0, p - 1))}
                onNext={() => setMembersPage(p => Math.min(totalPages - 1, p + 1))}
            />

            <Separator />

            <Btn
                text={myInfo?.isHost ? "关闭房间" : "离开自习室"}
                onClick={doLeave}
                danger
            />
        </div>
    )
}

// ─── 成员行 ───
const MemberRow = ({ member, isMe, syncState }: {
    member: Member; isMe: boolean
    syncState: { connected: boolean; latencyMs: number } | null
}) => {
    const connected: boolean = syncState?.connected ?? true
    const latency: number = syncState?.latencyMs ?? -1
    const latencyColor = latency < 0 ? DIM : latency < 80 ? OK : latency < 200 ? WARN : ERR
    const latencyText = latency >= 0 ? `${latency}ms` : ""

    return (
        <div style={{
            backgroundColor: PANEL, borderRadius: 8,
            paddingLeft: 12, paddingRight: 12, paddingTop: 8, paddingBottom: 8,
            marginBottom: 5, display: "Flex", flexDirection: "Row", alignItems: "Center",
        }}>
            <div style={{ flexGrow: 1 }}>
                <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                    <div style={{ fontSize: 13, color: TEXT }}>{member.personaName}</div>
                    {member.isHost && (
                        <div style={{ fontSize: 9, color: ACCENT, backgroundColor: "rgba(125,211,252,0.12)", borderRadius: 4, paddingLeft: 5, paddingRight: 5, paddingTop: 2, paddingBottom: 2, marginLeft: 6 }}>
                            主机
                        </div>
                    )}
                    {isMe && (
                        <div style={{ fontSize: 9, color: DIM, marginLeft: 4 }}>（你）</div>
                    )}
                </div>
            </div>
            {!member.isHost && (
                <div style={{ display: "Flex", flexDirection: "Row", alignItems: "Center" }}>
                    <div style={{ width: 7, height: 7, borderRadius: 4, backgroundColor: connected ? OK : ERR, marginRight: 5 }} />
                    {latencyText !== "" && <div style={{ fontSize: 10, color: latencyColor }}>{latencyText}</div>}
                </div>
            )}
        </div>
    )
}

// ─── 主面板 ───
const StudyRoomPanel = () => {
    const [view, setView] = useState<View>("lobby")
    const [connectingMsg, setConnectingMsg] = useState("连接中...")
    const [pendingEntry, setPendingEntry] = useState<LobbyEntry | null>(null)
    const [errMsg, setErrMsg] = useState("")

    const goConnecting = (msg: string) => {
        setConnectingMsg(msg)
        setErrMsg("")
        setView("connecting")
    }

    useEffect(() => {
        const sr = getSr()
        if (!sr) return

        // 场景重载后重新检查在房间状态
        const gameTk = chill?.game?.on?.("*", (e: string) => {
            try {
                const evt = JSON.parse(e)
                if (evt?.name === "sceneReloaded") {
                    if (sr.isInRoom?.()) setView("room")
                }
            } catch { }
        })

        const tk = sr.on?.("*", (jsonStr: string) => {
            try {
                const evt = JSON.parse(jsonStr)
                const name: string = evt?.name ?? ""
                switch (name) {
                    case "roomCreated":
                    case "syncReady":
                        setErrMsg("")
                        setView("room")
                        break
                    case "roomLeft":
                    case "roomClosed":
                        setView("lobby")
                        break
                    case "joinFailed":
                        setErrMsg(`加入失败: ${evt?.payload?.reason ?? "未知错误"}`)
                        setView("lobby")
                        break
                    case "kicked":
                        setErrMsg("你已被移出自习室")
                        setView("lobby")
                        break
                    case "connectionLost":
                        setConnectingMsg("连接中断，正在重连...")
                        setView("connecting")
                        break
                    case "reconnected":
                        setView("room")
                        break
                }
            } catch { }
        })

        // 启动时如果已在房间直接跳到 room 视图
        if (sr.isInRoom?.()) setView("room")

        return () => {
            if (tk) sr.off?.(tk)
            if (gameTk) chill?.game?.off?.(gameTk)
        }
    }, [])

    const handleJoinEntry = (entry: LobbyEntry) => {
        setPendingEntry(entry)
        if (!entry.hasPassword) {
            getSr()?.joinRoom?.(entry.lobbyId, "")
            goConnecting("正在加入...")
        } else {
            setView("join")
        }
    }

    return (
        <div style={{ flexGrow: 1, display: "Flex", flexDirection: "Column", backgroundColor: BG }}>
            {errMsg !== "" && (
                <div style={{
                    backgroundColor: "rgba(248,113,113,0.12)", borderRadius: 0,
                    paddingTop: 6, paddingBottom: 6, paddingLeft: 14, paddingRight: 14,
                }}>
                    <div style={{ fontSize: 11, color: ERR }}>{errMsg}</div>
                </div>
            )}

            {view === "lobby" && (
                <LobbyView onSetView={setView} onJoinEntry={handleJoinEntry} />
            )}
            {view === "create" && (
                <CreateView
                    onBack={() => setView("lobby")}
                    onConnecting={() => goConnecting("正在创建房间...")}
                />
            )}
            {view === "join" && pendingEntry && (
                <JoinView
                    entry={pendingEntry}
                    onBack={() => setView("lobby")}
                    onConnecting={() => goConnecting("正在加入...")}
                />
            )}
            {view === "connecting" && <ConnectingView msg={connectingMsg} />}
            {view === "room" && <RoomView onSetView={setView} />}
        </div>
    )
}

// ─── Compact 视图 ───
const StudyRoomCompact = () => {
    const [inRoom, setInRoom] = useState(false)
    const [memberCount, setMemberCount] = useState(0)
    const [isHost, setIsHost] = useState(false)

    useEffect(() => {
        const poll = setInterval(() => {
            const sr = getSr()
            if (!sr) return
            const active = !!sr.isInRoom?.()
            setInRoom(active)
            if (active) {
                const members = json(sr.getMembers?.(), []) as Member[]
                setMemberCount(members.length)
                setIsHost(!!sr.isHost?.())
            }
        }, 1500)
        return () => clearInterval(poll)
    }, [])

    return (
        <div style={{
            flexGrow: 1, display: "Flex", flexDirection: "Row",
            alignItems: "Center", backgroundColor: CARD,
            paddingLeft: 12, paddingRight: 12, paddingTop: 8, paddingBottom: 8,
            borderRadius: 8,
        }}>
            <div style={{ fontSize: 18, color: inRoom ? ACCENT : DIM, marginRight: 10 }}>󰡉</div>
            <div style={{ flexGrow: 1 }}>
                <div style={{ fontSize: 11, color: TEXT }}>{"Study Room"}</div>
                <div style={{ fontSize: 10, color: DIM }}>
                    {inRoom ? `${memberCount} 人在线${isHost ? " · 主机" : ""}` : "未加入"}
                </div>
            </div>
            <div style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: inRoom ? OK : "rgba(255,255,255,0.15)" }} />
        </div>
    )
}

__registerPlugin({
    id: "study-room",
    title: "Study Room",
    width: 320,
    height: 520,
    initialX: 300,
    initialY: 120,
    resizable: false,
    launcher: { text: "󰡉", background: "#6366f1" },
    compact: { width: 180, height: 70, component: StudyRoomCompact },
    component: StudyRoomPanel,
})
