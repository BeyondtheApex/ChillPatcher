import { h } from "preact"
import { useState, useEffect } from "preact/hooks"
import { theme } from "./theme"
import { parse } from "./utils"
import { Pagination } from "./Pagination"

declare const chill: any

interface LicenseFile {
    name: string
    extension: string
    nameWithoutExt: string
    size: number
}

interface ListItem {
    type: "dir" | "file"
    name: string
    displayName: string
    size?: number
}

const ITEMS_PER_PAGE = 8

export const LicensesPanel = () => {
    const [currentPath, setCurrentPath] = useState("licenses")
    const [items, setItems] = useState<ListItem[]>([])
    const [page, setPage] = useState(0)
    const [selected, setSelected] = useState<string | null>(null)
    const [content, setContent] = useState("")

    useEffect(() => {
        loadDirectory(currentPath)
        setPage(0)
    }, [currentPath])

    const loadDirectory = (dirPath: string) => {
        try {
            const dirs = parse<string[]>(chill.io.listDirs(dirPath)) || []
            const files = parse<LicenseFile[]>(chill.io.listFiles(dirPath)) || []
            const combined: ListItem[] = [
                ...dirs.map(d => ({ type: "dir" as const, name: d, displayName: d })),
                ...files.map(f => ({ type: "file" as const, name: f.name, displayName: f.nameWithoutExt, size: f.size })),
            ]
            setItems(combined)
        } catch (e) {
            console.error("LicensesPanel load error:", e)
            setItems([])
        }
    }

    const enterDir = (dirName: string) => {
        setCurrentPath(`${currentPath}/${dirName}`)
    }

    const goUp = () => {
        const idx = currentPath.lastIndexOf("/")
        if (idx > 0) setCurrentPath(currentPath.substring(0, idx))
    }

    const selectFile = (fileName: string) => {
        setSelected(fileName)
        try {
            const text = chill.io.readText(`${currentPath}/${fileName}`)
            setContent(text || "无法读取文件内容")
        } catch (e) {
            console.error("LicensesPanel read error:", e)
            setContent("读取失败")
        }
    }

    if (selected) {
        return (
            <div style={{ flexDirection: "Column", display: "Flex", flexGrow: 1 }}>
                <LicenseDetail
                    fileName={selected}
                    content={content}
                    onBack={() => setSelected(null)}
                />
            </div>
        )
    }

    const totalPages = Math.max(1, Math.ceil(items.length / ITEMS_PER_PAGE))
    const pageItems = items.slice(page * ITEMS_PER_PAGE, (page + 1) * ITEMS_PER_PAGE)
    const isSubDir = currentPath !== "licenses"

    return (
        <div style={{ flexDirection: "Column", display: "Flex", flexGrow: 1 }}>
            {isSubDir && (
                <div
                    style={{ fontSize: 13, color: theme.accent, marginBottom: 6 }}
                    onClick={goUp}
                >
                    {`← ${currentPath.substring(currentPath.lastIndexOf("/") + 1)}`}
                </div>
            )}
            <div style={{ flexDirection: "Column", display: "Flex", flexGrow: 1 }}>
                {pageItems.length === 0 ? (
                    <div style={{ fontSize: 13, color: theme.textMuted, paddingTop: 20 }}>
                        {`该目录下未找到许可证文件`}
                    </div>
                ) : (
                    pageItems.map(item => (
                        <div
                            key={`${item.type}-${item.name}`}
                            style={{
                                flexDirection: "Row",
                                display: "Flex",
                                justifyContent: "SpaceBetween",
                                alignItems: "Center",
                                backgroundColor: theme.bgCard,
                                borderRadius: theme.radius,
                                paddingTop: 10,
                                paddingBottom: 10,
                                paddingLeft: 14,
                                paddingRight: 14,
                                marginBottom: 4,
                            }}
                            onClick={() => item.type === "dir" ? enterDir(item.name) : selectFile(item.name)}
                        >
                            <div style={{ fontSize: 13, color: item.type === "dir" ? theme.accent : theme.text }}>
                                {item.type === "dir" ? ` ${item.displayName}` : item.displayName}
                            </div>
                            <div style={{ fontSize: 11, color: theme.textMuted }}>
                                {item.type === "dir" ? "" : formatSize(item.size!)}
                            </div>
                        </div>
                    ))
                )}
            </div>
            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </div>
    )
}

const LINES_PER_PAGE = 20

const LicenseDetail = ({ fileName, content, onBack }: {
    fileName: string
    content: string
    onBack: () => void
}) => {
    const [page, setPage] = useState(0)
    const lines = content.split("\n")
    const totalPages = Math.max(1, Math.ceil(lines.length / LINES_PER_PAGE))
    const pageLines = lines.slice(page * LINES_PER_PAGE, (page + 1) * LINES_PER_PAGE)

    return (
        <div style={{ flexDirection: "Column", display: "Flex", flexGrow: 1 }}>
            <div style={{
                flexDirection: "Row",
                display: "Flex",
                justifyContent: "SpaceBetween",
                alignItems: "Center",
                marginBottom: 8,
            }}>
                <div
                    style={{ fontSize: 13, color: theme.accent }}
                    onClick={onBack}
                >
                    {`← 返回列表`}
                </div>
                <div style={{ fontSize: 12, color: theme.textMuted }}>
                    {fileName}
                </div>
            </div>
            <div style={{
                flexGrow: 1,
                backgroundColor: theme.bgCard,
                borderRadius: theme.radius,
                paddingTop: 12,
                paddingBottom: 12,
                paddingLeft: 14,
                paddingRight: 14,
                flexDirection: "Column",
                display: "Flex",
            }}>
                {pageLines.map((line, i) => (
                    <div key={i} style={{ fontSize: 11, color: theme.textMuted, minHeight: 14 }}>
                        {line || " "}
                    </div>
                ))}
            </div>
            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
        </div>
    )
}

function formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
