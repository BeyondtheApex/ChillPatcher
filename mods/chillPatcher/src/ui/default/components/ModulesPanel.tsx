import { h } from "preact"
import { useState, useEffect, useRef } from "preact/hooks"
import { theme } from "./theme"
import { parse } from "./utils"
import { Pagination } from "./Pagination"

declare const chill: any

interface ModuleInfo {
    moduleId: string
    displayName: string
    version: string
    priority: number
    directory: string
    loadedAt: string
    enabled: boolean
    capabilities: {
        canDelete: boolean
        canFavorite: boolean
        canExclude: boolean
        supportsLiveUpdate: boolean
        providesCover: boolean
        providesAlbum: boolean
    } | null
}

const MODULES_PER_PAGE = 4
const POLL_INTERVAL = 3000

export const ModulesPanel = () => {
    const [modules, setModules] = useState<ModuleInfo[]>([])
    const [page, setPage] = useState(0)
    const lastJson = useRef("")

    const refresh = () => {
        try {
            const json = chill.modules.getAll() as string
            if (json === lastJson.current) return
            lastJson.current = json
            setModules(parse<ModuleInfo[]>(json) || [])
        } catch (e) {
            console.error("ModulesPanel refresh error:", e)
        }
    }

    useEffect(() => {
        refresh()
        const timer = setInterval(refresh, POLL_INTERVAL)
        return () => clearInterval(timer)
    }, [])

    const toggle = (mod: ModuleInfo) => {
        try {
            if (mod.enabled) {
                chill.modules.disable(mod.moduleId)
            } else {
                chill.modules.enable(mod.moduleId)
            }
            refresh()
        } catch (e) {
            console.error("ModulesPanel toggle error:", e)
        }
    }

    const totalPages = Math.max(1, Math.ceil(modules.length / MODULES_PER_PAGE))
    const pageModules = modules.slice(page * MODULES_PER_PAGE, (page + 1) * MODULES_PER_PAGE)

    return (
        <div style={{ flexDirection: "Column", display: "Flex", flexGrow: 1 }}>
            <div style={{ fontSize: 12, color: theme.textMuted, marginBottom: 8 }}>
                {`已注册 ${modules.length} 个模块（禁用/启用后需重启游戏生效）`}
            </div>

            {pageModules.map(mod => (
                <ModuleCard key={mod.moduleId} module={mod} onToggle={() => toggle(mod)} />
            ))}

            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </div>
    )
}

const ModuleCard = ({ module, onToggle }: { module: ModuleInfo; onToggle: () => void }) => (
    <div style={{
        flexDirection: "Column",
        display: "Flex",
        backgroundColor: theme.bgCard,
        borderRadius: theme.radius,
        paddingTop: 12,
        paddingBottom: 12,
        paddingLeft: 14,
        paddingRight: 14,
        marginBottom: 6,
    }}>
        {/* 头部：名称 + 开关 */}
        <div style={{
            flexDirection: "Row",
            display: "Flex",
            justifyContent: "SpaceBetween",
            alignItems: "Center",
            marginBottom: 6,
        }}>
            <div style={{ flexDirection: "Row", display: "Flex", alignItems: "Center" }}>
                <div style={{ fontSize: 14, color: theme.text, marginRight: 8 }}>
                    {module.displayName}
                </div>
                <div style={{ fontSize: 11, color: theme.textMuted }}>
                    {`v${module.version}`}
                </div>
            </div>
            <div
                style={{
                    paddingTop: 3,
                    paddingBottom: 3,
                    paddingLeft: 10,
                    paddingRight: 10,
                    borderRadius: 4,
                    fontSize: 11,
                    backgroundColor: module.enabled ? theme.success : theme.danger,
                    color: theme.textBright,
                }}
                onClick={onToggle}
            >
                {module.enabled ? "启用" : "禁用"}
            </div>
        </div>

        {/* 详情 */}
        <div style={{ fontSize: 11, color: theme.textMuted, marginBottom: 4 }}>
            {module.moduleId}
        </div>
        <div style={{ fontSize: 11, color: theme.textMuted }}>
            {`优先级: ${module.priority} · 加载于 ${module.loadedAt}`}
        </div>

        {/* 能力标签 */}
        {module.capabilities ? (
            <div style={{
                flexDirection: "Row",
                display: "Flex",
                flexWrap: "Wrap",
                marginTop: 6,
            }}>
                {module.capabilities.canDelete && <CapBadge label="删除" />}
                {module.capabilities.canFavorite && <CapBadge label="收藏" />}
                {module.capabilities.canExclude && <CapBadge label="排除" />}
                {module.capabilities.supportsLiveUpdate && <CapBadge label="热更新" />}
                {module.capabilities.providesCover && <CapBadge label="封面" />}
                {module.capabilities.providesAlbum && <CapBadge label="专辑" />}
            </div>
        ) : null}
    </div>
)

const CapBadge = ({ label }: { label: string }) => (
    <div style={{
        fontSize: 10,
        color: theme.accent,
        backgroundColor: theme.bg,
        borderRadius: 3,
        paddingTop: 2,
        paddingBottom: 2,
        paddingLeft: 6,
        paddingRight: 6,
        marginRight: 4,
        marginBottom: 2,
    }}>
        {label}
    </div>
)
