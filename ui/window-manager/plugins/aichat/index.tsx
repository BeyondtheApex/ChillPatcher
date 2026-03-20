import { h, Fragment } from "preact"
import { useEffect, useMemo, useState } from "preact/hooks"

declare const __registerPlugin: any
declare const chill: any

type KV = Record<string, string>
const groups: Record<string, string[]> = {
  "1. LLM": ["Use_Ollama_API","ThinkMode","API_URL","API_Key","ModelName","LogApiRequestBody","FixApiPathForThinkMode"],
  "2. TTS": ["TTS_Service_URL","TTS_Service_Script_Path","LaunchTTSService","QuitTTSServiceOnQuit","Audio_File_Path","AudioPathCheck","Audio_File_Text","PromptLang","TargetLang","JapaneseCheck","VoiceVolume"],
  "3. UI": ["WindowWidth","WindowHeightBase","ReverseEnterBehavior","BackgroundOpacity","ShowWindowTitle"],
  "4. Persona": ["ExperimentalMemory","SystemPrompt"],
}
const multilineKeys = new Set(["Audio_File_Text", "SystemPrompt"])

const call = (name: string, ...args: any[]) => JSON.parse(chill.aichat[name](...args))
const loadCfg = (): KV => JSON.parse(chill.aichat.getAllConfig() || "{}")
const loadDefaults = (): KV => JSON.parse(chill.aichat.getAllConfigDefaults() || "{}")

const fieldEstimatedHeight = (k:string) => !multilineKeys.has(k) ? 52 : (k === "SystemPrompt" ? 320 : 210)

const paginateFields = (fields:string[], maxHeight:number) => {
  const pages:string[][] = []
  let current:string[] = []
  let used = 0
  const limit = Math.max(120, Math.floor(maxHeight))

  for (const k of fields) {
    const h = fieldEstimatedHeight(k)
    if (current.length > 0 && used + h > limit) {
      pages.push(current)
      current = []
      used = 0
    }
    current.push(k)
    used += h
  }

  if (current.length > 0) pages.push(current)
  return pages.length > 0 ? pages : [[]]
}

const SavePanel = ({draft,defaults,setDraft,onCancel,onOk}:{draft:KV;defaults:KV;setDraft:(v:KV)=>void;onCancel:()=>void;onOk:()=>void}) => {
  const pages = Object.keys(groups)
  const [page,setPage] = useState(0)
  const [contentPage,setContentPage] = useState(0)
  const [contentHeight,setContentHeight] = useState(220)
  const currentGroup = pages[page]
  const fields = groups[currentGroup] || []
  const fieldPages = paginateFields(fields, contentHeight - (currentGroup === "4. Persona" ? 40 : 24))
  const maxContentPage = Math.max(0, fieldPages.length - 1)
  const displayFields = fieldPages[Math.min(contentPage, maxContentPage)] || []

  useEffect(()=>{
    setContentPage(p=>Math.min(p, maxContentPage))
  },[maxContentPage, page])

  const row = (k:string)=>(
    multilineKeys.has(k)
      ? <div key={k} style={{display:"Flex",flexDirection:"Column",marginBottom:6,backgroundColor:"#111827",paddingTop:4,paddingBottom:4,paddingLeft:6,paddingRight:6,borderWidth:1,borderColor:"#1f2937",borderRadius:6,flexShrink:0}}>
          <div style={{display:"Flex",flexDirection:"Row",justifyContent:"SpaceBetween",alignItems:"Center",marginBottom:4}}>
            <div style={{fontSize:10,color:"#94a3b8"}}>{k}</div>
            <div onPointerDown={()=>setDraft({...draft,[k]:String(defaults[k] ?? "")})}
              style={{fontSize:9,color:"#93c5fd",paddingLeft:4,paddingRight:4,paddingTop:3,paddingBottom:3,borderWidth:1,borderColor:"#334155",borderRadius:4}}>恢复</div>
          </div>
          <div style={{height:34,marginBottom:4,backgroundColor:"#0b1220",borderWidth:1,borderColor:"#1f2937",paddingLeft:6,paddingRight:6,paddingTop:4,paddingBottom:4,flexShrink:0,overflow:"Hidden"}}>
            <div style={{fontSize:9,color:"#64748b",whiteSpace:"Normal"}}>默认: {String(defaults[k] ?? "")}</div>
          </div>
          <textfield value={String(draft[k] ?? "")} multiline={true} vertical-scroller-visibility={1} onValueChanged={(e:any)=>setDraft({...draft,[k]:e.newValue ?? ""})}
            style={{height:k==="SystemPrompt"?180:88,minHeight:k==="SystemPrompt"?180:88,maxHeight:k==="SystemPrompt"?180:88,width:"100%",fontSize:10,backgroundColor:"#1f2937",borderWidth:1,borderColor:"#334155",color:"#e2e8f0",paddingLeft:6,paddingRight:6,paddingTop:4,paddingBottom:4,flexShrink:0}} />
        </div>
      : <div key={k} style={{display:"Flex",flexDirection:"Row",alignItems:"Center",marginBottom:6,backgroundColor:"#111827",paddingTop:4,paddingBottom:4,paddingLeft:6,paddingRight:6,borderWidth:1,borderColor:"#1f2937",borderRadius:6,flexShrink:0}}>
          <div style={{width:126,fontSize:10,color:"#94a3b8",marginRight:6}}>{k}</div>
          <textfield value={String(draft[k] ?? "")} onValueChanged={(e:any)=>setDraft({...draft,[k]:e.newValue ?? ""})}
            style={{flexGrow:1,height:22,fontSize:10,backgroundColor:"#1f2937",borderWidth:1,borderColor:"#334155",color:"#e2e8f0",paddingLeft:6,paddingRight:6,marginRight:6}}/>
          <div style={{width:120,fontSize:9,color:"#64748b",overflow:"Hidden",marginRight:6}}>默认: {String(defaults[k] ?? "")}</div>
          <div onPointerDown={()=>setDraft({...draft,[k]:String(defaults[k] ?? "")})}
            style={{fontSize:9,color:"#93c5fd",paddingLeft:4,paddingRight:4,paddingTop:3,paddingBottom:3,borderWidth:1,borderColor:"#334155",borderRadius:4}}>恢复</div>
        </div>
  )

  return <div style={{flexGrow:1,height:"100%",display:"Flex",flexDirection:"Column",backgroundColor:"#0f172a",padding:10,overflow:"Hidden"}}>
    <div style={{fontSize:13,color:"#e2e8f0",marginBottom:8,flexShrink:0}}>AIChat 配置</div>

    <div style={{display:"Flex",flexDirection:"Row",marginBottom:8,flexShrink:0}}>
      {pages.map((g,idx)=><div key={g} onPointerDown={()=>{ setPage(idx); setContentPage(0) }} style={{fontSize:10,color:idx===page?"#0f172a":"#cbd5e1",backgroundColor:idx===page?"#93c5fd":"#1e293b",paddingLeft:8,paddingRight:8,paddingTop:5,paddingBottom:5,borderRadius:6,marginRight:6}}>{g.replace("1. ","").replace("2. ","").replace("3. ","").replace("4. ","")}</div>)}
    </div>

    <div style={{fontSize:11,color:"#93c5fd",marginBottom:6,flexShrink:0}}>{currentGroup}</div>
    <div style={{flexGrow:1,flexShrink:1,minHeight:120,backgroundColor:"#0b1220",borderWidth:1,borderColor:"#1f2937",borderRadius:6,padding:6,overflow:"Hidden"}}
      onGeometryChanged={(e:any)=>setContentHeight(Math.max(120, Math.floor(e?.newRect?.height ?? e?.target?.layout?.height ?? 220)))}>
      {displayFields.map(row)}
      {currentGroup === "4. Persona" && <div style={{fontSize:9,color:"#64748b",marginTop:4,flexShrink:0}}>SystemPrompt 建议手动粘贴完整内容后再点击确定保存。</div>}
    </div>

    <div style={{display:"Flex",flexDirection:"Row",justifyContent:"SpaceBetween",marginTop:8,flexShrink:0}}>
      <div onPointerDown={()=>setContentPage(Math.max(0,contentPage-1))} style={{fontSize:10,color:contentPage>0?"#cbd5e1":"#475569",padding:"5 8",borderWidth:1,borderColor:"#334155",borderRadius:6}}>上一页</div>
      <div style={{fontSize:10,color:"#64748b"}}>{`${Math.min(contentPage,maxContentPage)+1}/${fieldPages.length}`}</div>
      <div onPointerDown={()=>setContentPage(Math.min(maxContentPage,contentPage+1))} style={{fontSize:10,color:contentPage<maxContentPage?"#cbd5e1":"#475569",padding:"5 8",borderWidth:1,borderColor:"#334155",borderRadius:6}}>下一页</div>
    </div>

    <div style={{display:"Flex",flexDirection:"Row",justifyContent:"FlexEnd",marginTop:8,flexShrink:0}}>
      <div onPointerDown={onCancel} style={{fontSize:11,color:"#cbd5e1",padding:"6 12",marginRight:8,borderWidth:1,borderColor:"#475569",borderRadius:6}}>取消</div>
      <div onPointerDown={onOk} style={{fontSize:11,color:"#0f172a",backgroundColor:"#93c5fd",padding:"6 12",borderRadius:6}}>确定</div>
    </div>
  </div>
}

const ChatPanel = ({compact=false}:{compact?:boolean}) => {
  const [prompt,setPrompt] = useState("")
  const [rec,setRec] = useState(false)
  const [cfgMode,setCfgMode] = useState(false)
  const [draft,setDraft] = useState<KV>({})
  const [defaults,setDefaults] = useState<KV>({})
  const [status,setStatus] = useState<any>({available:false})
  const [last,setLast] = useState<any>(null)
  const [token,setToken] = useState("")
  const [vu,setVu] = useState(0)

  useEffect(()=>{
    if (!rec) { setVu(0); return }
    const id = setInterval(()=>{
      const t = Date.now() / 160
      const noise = Math.random() * 0.5
      const level = Math.max(0.08, Math.min(1, Math.abs(Math.sin(t)) * 0.7 + noise * 0.3))
      setVu(level)
    }, 80)
    return ()=>clearInterval(id as any)
  },[rec])

  const vuStyle = useMemo(()=>({
    width: compact ? 14 : 18,
    height: compact ? 14 : 18,
    borderRadius: 999,
    backgroundColor: rec ? "#f87171" : "#64748b",
    scale: rec ? (0.9 + vu * 0.9) : 1,
    opacity: rec ? (0.5 + vu * 0.5) : 0.35,
    marginRight: 6,
    borderWidth: 1,
    borderColor: rec ? "#fecaca" : "#94a3b8",
  }),[rec,vu,compact])

  useEffect(()=>{
    setStatus(JSON.parse(chill.aichat.getStatus()))
    setDraft(loadCfg())
    setDefaults(loadDefaults())
    const t = chill.aichat.onConversationCompleted((json:string)=>setLast(JSON.parse(json || "{}")))
    setToken(t)
    return ()=>{ if (t) chill.aichat.offConversationCompleted(t) }
  },[])

  const send = ()=>{ if (!prompt.trim()) return; const r = call("startTextConversation",prompt,"wm-plugin"); if (r.ok) setPrompt("") }
  const micDown = ()=>{ const r = call("startVoiceCapture"); if (r.ok) setRec(true) }
  const micUp = ()=>{ const r = call("stopVoiceCaptureAndSend","wm-plugin"); setRec(false); if (!r.ok) console.log(r.error) }
  const save = ()=>{
    let ok = true
    for (const k of Object.keys(draft)) { const r = call("setConfig",k,String(draft[k] ?? "")); if (!r.ok) ok = false }
    const rs = call("saveConfig"); setCfgMode(false); setStatus(JSON.parse(chill.aichat.getStatus())); if (!ok || !rs.ok) console.log("保存失败")
  }

  if (cfgMode && !compact) return <SavePanel draft={draft} defaults={defaults} setDraft={setDraft} onCancel={()=>setCfgMode(false)} onOk={save} />
  return <div style={{flexGrow:1,display:"Flex",flexDirection:"Column",backgroundColor:"#111827",padding:10}}>
    {!compact && <>
      <div style={{fontSize:11,color:status.available?"#86efac":"#fca5a5",marginBottom:4}}>{status.available?"AIChat 已连接":"AIChat 未安装"}</div>
      <div style={{fontSize:10,color:"#94a3b8",marginBottom:8}}>busy:{String(!!status.isBusy)} ready:{String(!!status.isReady)} ver:{status.apiVersion || "-"}</div>
    </>}
    <textfield text={prompt} onValueChanged={(e:any)=>setPrompt(e.newValue ?? "")}
      style={{height:compact?34:70,fontSize:12,backgroundColor:"#1f2937",borderWidth:1,borderColor:"#334155",color:"#e2e8f0",paddingLeft:8,paddingRight:8}} />
    <div style={{display:"Flex",flexDirection:"Row",marginTop:8}}>
      <div onPointerDown={send} style={{fontSize:11,color:"#111827",backgroundColor:"#93c5fd",padding:"6 10",borderRadius:6,marginRight:6}}>发送</div>
      <div onPointerDown={micDown} onPointerUp={micUp} onPointerLeave={micUp}
        style={{fontSize:11,color:rec?"#fff":"#e2e8f0",backgroundColor:rec?"#ef4444":"#334155",padding:"6 10",borderRadius:6,marginRight:6,display:"Flex",flexDirection:"Row",alignItems:"Center"}}><div style={vuStyle} />{rec?"松开发送":"按住说话"}</div>
      {!compact && <div onPointerDown={()=>setCfgMode(true)} style={{fontSize:11,color:"#e2e8f0",backgroundColor:"#334155",padding:"6 10",borderRadius:6}}>配置</div>}
    </div>
    {!compact && last && <div style={{marginTop:8,padding:6,borderWidth:1,borderColor:"#1e293b",borderRadius:6}}>
      <div style={{fontSize:10,color:"#93c5fd",marginBottom:2}}>{`[${last.EmotionTag || "Think"}]`}</div>
      <div style={{fontSize:11,color:"#e2e8f0",marginBottom:2}}>{last.VoiceText || ""}</div>
      <div style={{fontSize:11,color:"#cbd5e1"}}>{last.SubtitleText || ""}</div>
    </div>}
  </div>
}

__registerPlugin({
  id: "aichat",
  title: "AIChat",
  width: 520,
  height: 420,
  initialX: 120,
  initialY: 80,
  resizable: true,
  compact: { width: 420, height: 120, component: () => <ChatPanel compact={true} /> },
  launcher: {
    text: "",
    background: "#008055",
  },
  component: () => <ChatPanel />,
})
