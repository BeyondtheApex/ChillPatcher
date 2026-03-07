import { h } from "preact"
import { theme } from "./theme"

export const Pagination = ({ page, totalPages, onPageChange }: {
    page: number
    totalPages: number
    onPageChange: (p: number) => void
}) => {
    if (totalPages <= 1) return null

    return (
        <div style={{
            flexDirection: "Row",
            display: "Flex",
            justifyContent: "Center",
            alignItems: "Center",
            marginTop: 8,
        }}>
            <div
                style={{
                    fontSize: 13,
                    color: page > 0 ? theme.accent : theme.textMuted,
                    paddingLeft: 12,
                    paddingRight: 12,
                    paddingTop: 6,
                    paddingBottom: 6,
                }}
                onClick={() => { if (page > 0) onPageChange(page - 1) }}
            >
                {`‹ 上一页`}
            </div>
            <div style={{ fontSize: 12, color: theme.textMuted, paddingLeft: 8, paddingRight: 8 }}>
                {`${page + 1} / ${totalPages}`}
            </div>
            <div
                style={{
                    fontSize: 13,
                    color: page < totalPages - 1 ? theme.accent : theme.textMuted,
                    paddingLeft: 12,
                    paddingRight: 12,
                    paddingTop: 6,
                    paddingBottom: 6,
                }}
                onClick={() => { if (page < totalPages - 1) onPageChange(page + 1) }}
            >
                {`下一页 ›`}
            </div>
        </div>
    )
}
