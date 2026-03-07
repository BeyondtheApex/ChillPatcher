import { h } from "preact"
import { useState } from "preact/hooks"
import { theme } from "./theme"

interface Tab {
    id: string
    label: string
    content: () => h.JSX.Element
}

interface TabContainerProps {
    tabs: Tab[]
    defaultTab?: string
}

export const TabContainer = ({ tabs, defaultTab }: TabContainerProps) => {
    const [activeTab, setActiveTab] = useState(defaultTab || tabs[0]?.id)
    const current = tabs.find(t => t.id === activeTab)

    return (
        <div style={{ flexGrow: 1, flexDirection: "Column", display: "Flex" }}>
            {/* Tab 栏 */}
            <div style={{
                flexDirection: "Row",
                display: "Flex",
                borderBottomWidth: 1,
                borderBottomColor: theme.border,
                marginBottom: 12,
                paddingLeft: 4,
                paddingRight: 4,
            }}>
                {tabs.map(tab => (
                    <div
                        key={tab.id}
                        style={{
                            paddingTop: 8,
                            paddingBottom: 8,
                            paddingLeft: 16,
                            paddingRight: 16,
                            marginRight: 4,
                            fontSize: 14,
                            color: activeTab === tab.id ? theme.accent : theme.textMuted,
                            borderBottomWidth: activeTab === tab.id ? 2 : 0,
                            borderBottomColor: theme.accent,
                        }}
                        onClick={() => setActiveTab(tab.id)}
                    >
                        {tab.label}
                    </div>
                ))}
            </div>

            {/* Tab 内容 */}
            <div style={{
                flexGrow: 1,
                paddingLeft: 4,
                paddingRight: 4,
            }}>
                {current?.content()}
            </div>
        </div>
    )
}
