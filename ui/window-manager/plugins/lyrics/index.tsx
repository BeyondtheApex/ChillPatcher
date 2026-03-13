import { h } from "preact"
import { useState, useRef, useEffect } from "preact/hooks"

declare const chill: any
declare const __registerPlugin: any

// ---- Types ----
interface LyricLine {
    time: number
    text: string
}

// ---- LRC Parser ----
function parseLRC(lrcText: string): LyricLine[] {
    const lines: LyricLine[] = []
    const regex = /\[(\d{1,2}):(\d{2})(?:[.:](\d{1,3}))?\](.*)/

    for (const raw of lrcText.split("\n")) {
        const match = raw.match(regex)
        if (!match) continue
        const min = parseInt(match[1], 10)
        const sec = parseInt(match[2], 10)
        let ms = 0
        if (match[3]) {
            ms = match[3].length === 1
                ? parseInt(match[3], 10) * 100
                : match[3].length === 2
                    ? parseInt(match[3], 10) * 10
                    : parseInt(match[3], 10)
        }
        const text = match[4].trim()
        if (!text) continue
        lines.push({ time: min * 60 + sec + ms / 1000, text })
    }

    lines.sort((a, b) => a.time - b.time)
    return lines
}

function base64Decode(base64: string): string {
    try {
        const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/="
        let output = ""
        let i = 0
        const str = base64.replace(/[^A-Za-z0-9+/=]/g, "")
        while (i < str.length) {
            const a = chars.indexOf(str.charAt(i++))
            const b = chars.indexOf(str.charAt(i++))
            const c = chars.indexOf(str.charAt(i++))
            const d = chars.indexOf(str.charAt(i++))
            const n = (a << 18) | (b << 12) | (c << 6) | d
            output += String.fromCharCode((n >> 16) & 0xff)
            if (c !== 64) output += String.fromCharCode((n >> 8) & 0xff)
            if (d !== 64) output += String.fromCharCode(n & 0xff)
        }
        let result = ""
        let j = 0
        while (j < output.length) {
            const cc = output.charCodeAt(j)
            if (cc < 128) {
                result += String.fromCharCode(cc)
                j++
            } else if (cc < 224) {
                const c2 = output.charCodeAt(j + 1)
                result += String.fromCharCode(((cc & 31) << 6) | (c2 & 63))
                j += 2
            } else if (cc < 240) {
                const c2 = output.charCodeAt(j + 1)
                const c3 = output.charCodeAt(j + 2)
                result += String.fromCharCode(((cc & 15) << 12) | ((c2 & 63) << 6) | (c3 & 63))
                j += 3
            } else {
                const c2 = output.charCodeAt(j + 1)
                const c3 = output.charCodeAt(j + 2)
                const c4 = output.charCodeAt(j + 3)
                const cp = ((cc & 7) << 18) | ((c2 & 63) << 12) | ((c3 & 63) << 6) | (c4 & 63)
                result += String.fromCodePoint(cp)
                j += 4
            }
        }
        return result
    } catch (e: any) {
        log("base64Decode error: " + (e?.message || e))
        return ""
    }
}

function getCurrentLineIndex(lyrics: LyricLine[], currentTime: number): number {
    if (lyrics.length === 0) return -1
    for (let i = lyrics.length - 1; i >= 0; i--) {
        if (currentTime >= lyrics[i].time) return i
    }
    return -1
}

function extractSongMid(uuid: string): string | null {
    if (!uuid) return null
    const match = uuid.match(/^qqmusic_(?:pl\d+_)?(.+)$/)
    return match ? match[1] : null
}

function log(msg: string) {
    try { console.log("[Lyrics] " + msg) } catch {}
}

// ---- Colors (Catppuccin Mocha) ----
const BG = "#1e1e2e"
const TEXT_DIM = "rgba(205, 214, 244, 0.3)"
const TEXT_SUB = "rgba(205, 214, 244, 0.6)"
const TEXT_NEARBY = "rgba(205, 214, 244, 0.45)"
const ACCENT = "#89b4fa"

// ---- Lyrics cache (shared across instances) ----
const lyricsCache: Record<string, LyricLine[]> = {}

// ---- Shared lyrics poller ----
const useLyricsPoller = () => {
    const [currentIdx, setCurrentIdx] = useState(-1)
    const [lyrics, setLyrics] = useState<LyricLine[]>([])
    const [statusText, setStatusText] = useState("等待播放...")
    const [title, setTitle] = useState("")
    const [artist, setArtist] = useState("")
    const lyricsRef = useRef<LyricLine[]>([])
    const currentSongRef = useRef("")
    const lastIdxRef = useRef(-1)
    const loadingRef = useRef(false)

    useEffect(() => {
        const poll = () => {
            try {
                if (loadingRef.current) return

                const songJson = chill.audio.getCurrentSong()
                if (!songJson || songJson === "null") return

                const song = JSON.parse(songJson)
                const mid = extractSongMid(song.uuid)
                if (!mid) return

                // Song changed? Load new lyrics
                if (mid !== currentSongRef.current) {
                    currentSongRef.current = mid
                    if (song.title) setTitle(song.title)
                    if (song.artist) setArtist(song.artist)
                    lastIdxRef.current = -1
                    setCurrentIdx(-1)

                    // Check cache first
                    if (lyricsCache[mid]) {
                        log("cache hit: " + mid + " (" + lyricsCache[mid].length + " lines)")
                        lyricsRef.current = lyricsCache[mid]
                        setLyrics(lyricsCache[mid])
                        setStatusText(lyricsCache[mid].length > 0 ? "♪" : "暂无歌词")
                        return
                    }

                    lyricsRef.current = []
                    setLyrics([])
                    setStatusText("加载歌词中...")
                    log("new song: " + song.title + " mid=" + mid)

                    const lyricApi = chill.custom.get("lyric")
                    if (!lyricApi) {
                        currentSongRef.current = ""
                        return
                    }

                    loadingRef.current = true
                    const b64 = lyricApi.getSongLyric(mid)
                    loadingRef.current = false

                    if (!b64) {
                        lyricsCache[mid] = []
                        setStatusText("暂无歌词")
                        return
                    }

                    const parsed = parseLRC(base64Decode(b64))
                    log("parsed lines: " + parsed.length)
                    lyricsCache[mid] = parsed
                    lyricsRef.current = parsed
                    setLyrics(parsed)
                    if (parsed.length === 0) {
                        setStatusText("暂无歌词")
                    }
                    return
                }

                // Update current lyric line based on playback state
                const cur = lyricsRef.current
                if (cur.length === 0) return
                const stateJson = chill.audio.getPlaybackState()
                if (!stateJson || stateJson === "null") return
                const state = JSON.parse(stateJson)
                const currentTime = state.currentTime || 0

                const idx = getCurrentLineIndex(cur, currentTime)
                if (idx !== lastIdxRef.current && idx >= 0) {
                    lastIdxRef.current = idx
                    setCurrentIdx(idx)
                    setStatusText(cur[idx].text)
                }
            } catch (e: any) {
                loadingRef.current = false
                log("poll error: " + (e?.message || e))
            }
        }

        const timer = setInterval(poll, 200)
        return () => clearInterval(timer)
    }, [])

    return { currentIdx, lyrics, statusText, title, artist }
}

// ---- Compact View ----
const LyricsCompact = () => {
    const { statusText, title } = useLyricsPoller()

    return (
        <div
            style={{
                flexGrow: 1,
                display: "Flex",
                flexDirection: "Column",
                justifyContent: "Center",
                backgroundColor: BG,
                paddingLeft: 16,
                paddingRight: 16,
                overflow: "Hidden",
            }}
        >
            {title && (
                <div style={{ fontSize: 11, color: TEXT_SUB, marginBottom: 4, overflow: "Hidden", whiteSpace: "NoWrap", unityFontStyleAndWeight: "Bold" }}>
                    {title}
                </div>
            )}
            <div style={{ fontSize: 15, color: ACCENT, overflow: "Hidden", whiteSpace: "NoWrap", unityFontStyleAndWeight: "Bold" }}>
                {statusText}
            </div>
        </div>
    )
}

// ---- Full View (3 lines: prev / current / next) ----
const LyricsCard = () => {
    const { currentIdx, lyrics, statusText, title, artist } = useLyricsPoller()

    const currText = currentIdx >= 0 ? lyrics[currentIdx].text : statusText
    const hasLyrics = lyrics.length > 0 && currentIdx >= 0

    const getLine = (offset: number) => {
        const i = currentIdx + offset
        return hasLyrics && i >= 0 && i < lyrics.length ? lyrics[i].text : ""
    }

    return (
        <div
            style={{
                flexGrow: 1,
                display: "Flex",
                flexDirection: "Column",
                backgroundColor: BG,
                paddingLeft: 24,
                paddingRight: 24,
                paddingTop: 36,
                paddingBottom: 12,
            }}
        >
            {/* Title */}
            <div style={{ fontSize: 15, color: TEXT_SUB, unityTextAlign: "MiddleCenter", whiteSpace: "NoWrap", unityFontStyleAndWeight: "Bold", marginBottom: 2 }}>
                {title || ""}
            </div>
            {/* Artist */}
            <div style={{ fontSize: 12, color: TEXT_DIM, unityTextAlign: "MiddleCenter", whiteSpace: "NoWrap", marginBottom: 4 }}>
                {artist || ""}
            </div>

            {/* Flexible spacer */}
            <div style={{ flexGrow: 1 }} />

            {/* -2 line */}
            <div style={{ fontSize: 13, color: TEXT_DIM, unityTextAlign: "MiddleCenter", whiteSpace: "NoWrap", marginBottom: 4 }}>
                {getLine(-2)}
            </div>

            {/* -1 line */}
            <div style={{ fontSize: 14, color: TEXT_NEARBY, unityTextAlign: "MiddleCenter", whiteSpace: "NoWrap", marginBottom: 4 }}>
                {getLine(-1)}
            </div>

            {/* Current line */}
            <div style={{ fontSize: 20, color: ACCENT, unityFontStyleAndWeight: "Bold", unityTextAlign: "MiddleCenter", whiteSpace: "NoWrap", marginBottom: 4 }}>
                {currText}
            </div>

            {/* +1 line */}
            <div style={{ fontSize: 14, color: TEXT_NEARBY, unityTextAlign: "MiddleCenter", whiteSpace: "NoWrap", marginBottom: 4 }}>
                {getLine(1)}
            </div>

            {/* +2 line */}
            <div style={{ fontSize: 13, color: TEXT_DIM, unityTextAlign: "MiddleCenter", whiteSpace: "NoWrap" }}>
                {getLine(2)}
            </div>

            {/* Flexible spacer */}
            <div style={{ flexGrow: 1 }} />
        </div>
    )
}

// ---- Register ----
__registerPlugin({
    id: "lyrics",
    title: "Lyrics",
    width: 380,
    height: 260,
    initialX: 150,
    initialY: 80,
    resizable: true,
    component: LyricsCard,
    compact: {
        width: 480,
        height: 60,
        component: LyricsCompact,
    },
})
