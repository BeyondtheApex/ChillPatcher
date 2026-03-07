import { h } from "preact"
import { theme } from "./theme"

declare const chill: any

export const AboutPanel = () => {
    let version = "unknown"
    let pluginPathVal = "N/A"
    try {
        version = String(chill.version || "unknown").trim()
        pluginPathVal = String(chill.pluginPath || "N/A").trim()
    } catch (e) {
        console.error("AboutPanel init error:", e)
    }

    return (
        <div style={{ flexDirection: "Column", display: "Flex", alignItems: "Center", paddingTop: 40 }}>
            <div style={{
                fontSize: 28,
                color: theme.accent,
                marginBottom: 8,
            }}>
                ChillPatcher
            </div>

            <div style={{ fontSize: 14, color: theme.textMuted, marginBottom: 24 }}>
                {`v${version}`}
            </div>

            <div style={{
                flexDirection: "Column",
                display: "Flex",
                backgroundColor: theme.bgCard,
                borderRadius: theme.radiusLg,
                paddingTop: 20,
                paddingBottom: 20,
                paddingLeft: 24,
                paddingRight: 24,
                maxWidth: 400,
                width: "100%",
            }}>
                <InfoRow label="Plugin GUID" value="com.chillpatcher.core" />
                <InfoRow label="Runtime" value=".NET Framework 4.7.2" />
                <InfoRow label="UI Engine" value="OneJS + Preact" />
                <InfoRow label="Framework" value="BepInEx 5" />
                <InfoRow label="Plugin Path" value={pluginPathVal} />
            </div>

            <div style={{
                fontSize: 12,
                color: theme.textMuted,
                marginTop: 24,
            }}>
                A modding framework for "Chill With You"
            </div>
        </div>
    )
}

const InfoRow = ({ label, value }: { label: string; value: string }) => (
    <div style={{
        flexDirection: "Row",
        display: "Flex",
        justifyContent: "SpaceBetween",
        paddingTop: 6,
        paddingBottom: 6,
        borderBottomWidth: 1,
        borderBottomColor: theme.border,
    }}>
        <div style={{ fontSize: 13, color: theme.textMuted }}>{label}</div>
        <div style={{ fontSize: 13, color: theme.text, maxWidth: 240 }}>{value}</div>
    </div>
)
