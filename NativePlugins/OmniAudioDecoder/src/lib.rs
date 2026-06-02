//! OmniAudioDecoder — Unified Rust audio decoder (Symphonia 0.6).
//! Replaces ChillAudioDecoder.dll + ChillFlacDecoder.dll.
//! API-compatible: exports ALL 17 C functions with identical signatures.

use std::ffi::CStr;
use std::fs::File;
use std::io::{Cursor, Read, Seek, SeekFrom};
use std::os::raw::c_char;
use std::os::windows::ffi::OsStringExt;
use std::path::PathBuf;
use std::sync::Mutex;

use symphonia::core::codecs::audio::{AudioDecoder, AudioDecoderOptions};
use symphonia::core::codecs::CodecParameters;
use symphonia::core::errors::Result as SymphResult;
use symphonia::core::formats::{FormatOptions, FormatReader, SeekMode, SeekTo, TrackType};
use symphonia::core::formats::probe::Hint;
use symphonia::core::io::{MediaSource, MediaSourceStream, MediaSourceStreamOptions};
use symphonia::core::meta::MetadataOptions;
use symphonia::core::units::Time;
use symphonia::default::{get_probe, get_codecs};

thread_local! { static LAST_ERROR: Mutex<Option<String>> = Mutex::new(None); }

fn set_error(msg: impl Into<String>) { LAST_ERROR.with(|e| *e.lock().unwrap() = Some(msg.into())); }

fn error_to_c(s: Option<String>) -> *const c_char {
    thread_local! { static BUF: std::cell::RefCell<Option<std::ffi::CString>> = std::cell::RefCell::new(None); }
    match s { Some(msg) => { let cs = std::ffi::CString::new(msg).unwrap_or_default(); let ptr = cs.as_ptr(); BUF.with(|b| *b.borrow_mut() = Some(cs)); ptr } None => std::ptr::null() }
}

fn wstr_to_path(raw: *const u16) -> Option<PathBuf> {
    if raw.is_null() { return None; }
    let len = unsafe { (0..).take_while(|&i| *raw.add(i) != 0).count() };
    Some(PathBuf::from(std::ffi::OsString::from_wide(unsafe { std::slice::from_raw_parts(raw, len) })))
}

fn frame_to_time(frame: u64, sample_rate: u32) -> Time {
    let total_ns = (frame as u128) * 1_000_000_000u128 / (sample_rate as u128).max(1);
    Time::try_from_nanos_u128(total_ns).unwrap_or(Time::ZERO)
}

// ── GrowingFile ──

struct GrowingFile { inner: File, last_len: u64 }
impl GrowingFile {
    fn new(file: File) -> Self { let len = file.metadata().map(|m| m.len()).unwrap_or(0); Self { inner: file, last_len: len } }
}
impl Read for GrowingFile {
    fn read(&mut self, buf: &mut [u8]) -> std::io::Result<usize> {
        let mut retries = 0;
        loop {
            match self.inner.read(buf) {
                Ok(0) => {
                    let cl = self.inner.metadata().map(|m| m.len()).unwrap_or(0);
                    if cl > self.last_len { 
                        self.last_len = cl; 
                        continue; 
                    } else { 
                        // Wait up to ~3 seconds for new data to arrive
                        if retries < 60 {
                            std::thread::sleep(std::time::Duration::from_millis(50));
                            retries += 1;
                            continue;
                        } else {
                            return Ok(0); // Actually return EOF instead of UnexpectedEof
                        }
                    }
                }
                Ok(n) => {
                    if let Ok(m) = self.inner.metadata() { self.last_len = m.len(); }
                    return Ok(n);
                }
                Err(e) => return Err(e),
            }
        }
    }
}
impl Seek for GrowingFile { fn seek(&mut self, pos: SeekFrom) -> std::io::Result<u64> { self.inner.seek(pos) } }
impl MediaSource for GrowingFile { fn is_seekable(&self) -> bool { true } fn byte_len(&self) -> Option<u64> { None } }

// ── FileDec ──

struct FileDec {
    _file: File, format: Box<dyn FormatReader>, decoder: Box<dyn AudioDecoder>,
    track_id: u32, sample_rate: u32, channels: u16, total_frames: u64,
    pcm: Vec<f32>, pcm_pos: usize, format_name: String,
}

fn make_decoder(params: &CodecParameters) -> SymphResult<Box<dyn AudioDecoder>> {
    let CodecParameters::Audio(ap) = params else {
        return Err(symphonia::core::errors::Error::DecodeError("not an audio track"));
    };
    let reg = get_codecs().get_audio_decoder(ap.codec)
        .ok_or_else(|| symphonia::core::errors::Error::DecodeError("codec not found"))?;
    (reg.factory)(ap, &AudioDecoderOptions::default())
}

fn extract_info(cp_opt: &Option<CodecParameters>) -> (u32, u16, u64, String) {
    match cp_opt {
        Some(CodecParameters::Audio(ap)) => (
            ap.sample_rate.unwrap_or(44100),
            ap.channels.as_ref().map(|c| c.count() as u16).unwrap_or(2),
            0,
            format!("{:?}", ap.codec).to_lowercase(),
        ),
        _ => (44100, 2, 0, "unknown".into()),
    }
}

impl FileDec {
    fn open(path: &std::path::Path, is_growing: bool) -> SymphResult<Self> {
        let file = File::open(path)?;

        let source: Box<dyn MediaSource> = if is_growing {
            Box::new(GrowingFile::new(file.try_clone()?))
        } else {
            Box::new(file.try_clone()?)
        };

        let mss = MediaSourceStream::new(source, MediaSourceStreamOptions::default());
        let mut hint = Hint::new();
        if let Some(ext) = path.extension().and_then(|e| e.to_str()) { hint.with_extension(ext); }
        let format = get_probe().probe(&hint, mss, FormatOptions::default(), MetadataOptions::default())?;
        let cp = format.default_track(TrackType::Audio)
            .and_then(|t| t.codec_params.clone());
        let track_id = format.default_track(TrackType::Audio).map(|t| t.id).unwrap_or(0);
        let decoder = match &cp {
            Some(p) => make_decoder(p)?,
            None => return Err(symphonia::core::errors::Error::DecodeError("no codec params")),
        };
        let (sr, ch, tf, fn_) = extract_info(&cp);
        Ok(Self { _file: file, format, decoder, track_id, sample_rate: sr, channels: ch, total_frames: tf, pcm: Vec::new(), pcm_pos: 0, format_name: fn_ })
    }

    fn decode_next(&mut self) -> SymphResult<bool> {
        let packet = match self.format.next_packet() {
            Ok(Some(p)) => p,
            Ok(None) => return Ok(false), // EOF
            Err(symphonia::core::errors::Error::IoError(ref e)) if e.kind() == std::io::ErrorKind::UnexpectedEof => return Ok(false),
            Err(symphonia::core::errors::Error::IoError(_)) => return Ok(false),
            Err(e) => return Err(e),
        };
        let decoded = self.decoder.decode(&packet)?;
        let frames = decoded.frames(); let ch = decoded.spec().channels().count();
        let mut il = vec![0.0f32; frames * ch];
        decoded.copy_to_slice_interleaved(&mut il);
        self.pcm.extend_from_slice(&il);
        Ok(true)
    }

    fn read_frames(&mut self, buf: &mut [f32], n: usize) -> i64 {
        let ch = self.channels.max(1) as usize; let need = n * ch; let mut pos = 0usize;
        loop {
            let avail = self.pcm.len().saturating_sub(self.pcm_pos);
            if avail > 0 { let k = avail.min(need - pos); buf[pos..pos+k].copy_from_slice(&self.pcm[self.pcm_pos..self.pcm_pos+k]); self.pcm_pos += k; pos += k; if pos >= need { break; } }
            
            if self.pcm_pos >= self.pcm.len() {
                self.pcm.clear();
                self.pcm_pos = 0;
            } else if self.pcm_pos > 4096 * ch {
                self.pcm.drain(..self.pcm_pos);
                self.pcm_pos = 0;
            }

            match self.decode_next() { Ok(true) => { continue; } _ => break, }
        }
        if need > pos { buf[pos..need].fill(0.0); }
        (pos / ch) as i64
    }

    fn seek(&mut self, frame: u64) -> SymphResult<()> {
        self.pcm.clear(); self.pcm_pos = 0;
        self.format.seek(SeekMode::Accurate, SeekTo::Time { time: frame_to_time(frame, self.sample_rate), track_id: Some(self.track_id) })?;
        self.decoder.reset();
        Ok(())
    }
}

// ── StreamDec ──

struct StreamDec {
    buffer: Vec<u8>, feed_complete: bool,
    format: Option<Box<dyn FormatReader>>, decoder: Option<Box<dyn AudioDecoder>>,
    track_id: u32, sample_rate: u32, channels: u16, total_frames: u64, ready: bool,
    pcm: Vec<f32>, pcm_pos: usize, format_hint: String,
}
impl StreamDec {
    fn new(hint: &str) -> Self { Self { buffer: Vec::new(), feed_complete: false, format: None, decoder: None, track_id: 0, sample_rate: 0, channels: 2, total_frames: 0, ready: false, pcm: Vec::new(), pcm_pos: 0, format_hint: hint.to_lowercase() } }
    fn feed(&mut self, data: &[u8]) { self.buffer.extend_from_slice(data); if self.format.is_none() { self.try_init(); } }
    fn feed_complete(&mut self) { self.feed_complete = true; if self.format.is_none() { self.try_init(); } }
    fn try_init(&mut self) {
        if self.format.is_some() { return; }
        let buf = std::mem::take(&mut self.buffer);
        let mss = MediaSourceStream::new(Box::new(Cursor::new(buf.clone())), MediaSourceStreamOptions::default());
        let mut hint = Hint::new();
        match self.format_hint.as_str() { "aac"|"m4a" => { hint.with_extension("m4a"); } o => { hint.with_extension(o); } }
        match get_probe().probe(&hint, mss, FormatOptions::default(), MetadataOptions::default()) {
            Ok(fmt) => {
                let cp_clone = fmt.default_track(TrackType::Audio)
                    .and_then(|t| t.codec_params.clone());
                if let Some(ref cp) = cp_clone {
                    if let Ok(d) = make_decoder(cp) {
                        let (sr, ch, tf, _) = extract_info(&cp_clone);
                        self.sample_rate = sr; self.channels = ch; self.total_frames = tf;
                        if let Some(t) = fmt.default_track(TrackType::Audio) { self.track_id = t.id; }
                        self.format = Some(fmt); self.decoder = Some(d); self.ready = true;
                        self.buffer = buf; return;
                    }
                }
                self.buffer = buf;
            }
            Err(_) => { self.buffer = buf; }
        }
    }
    fn read_frames(&mut self, buf: &mut [f32], n: usize) -> i64 {
        if self.format.is_none() { self.try_init(); } if self.format.is_none() { return 0; }
        let ch = self.channels.max(1) as usize; let need = n * ch; let mut pos = 0usize;
        loop {
            let avail = self.pcm.len().saturating_sub(self.pcm_pos);
            if avail > 0 { let k = avail.min(need - pos); buf[pos..pos+k].copy_from_slice(&self.pcm[self.pcm_pos..self.pcm_pos+k]); self.pcm_pos += k; pos += k; if pos >= need { break; } }
            
            if self.pcm_pos >= self.pcm.len() {
                self.pcm.clear();
                self.pcm_pos = 0;
            } else if self.pcm_pos > 4096 * ch {
                self.pcm.drain(..self.pcm_pos);
                self.pcm_pos = 0;
            }

            // Decode using a free function to avoid borrow conflicts
            let decoded = decode_one(&mut self.format, &mut self.decoder);
            match decoded {
                Some(il) => { self.pcm.extend_from_slice(&il); continue; }
                None => {
                    if self.feed_complete { break; }
                    // Check if it's a "no data yet" vs "real EOF"
                    return (pos / ch) as i64;
                }
            }
        }
        if need > pos { buf[pos..need].fill(0.0); } (pos / ch) as i64
    }
}

fn decode_one(
    fmt: &mut Option<Box<dyn FormatReader>>,
    dec: &mut Option<Box<dyn AudioDecoder>>,
) -> Option<Vec<f32>> {
    let fmt = fmt.as_mut()?;
    let dec = dec.as_mut()?;
    let pkt = match fmt.next_packet() {
        Ok(Some(p)) => p,
        _ => return None,
    };
    let d = dec.decode(&pkt).ok()?;
    let frames = d.frames(); let ch = d.spec().channels().count();
    let mut il = vec![0.0f32; frames * ch];
    d.copy_to_slice_interleaved(&mut il);
    Some(il)
}

// ── C exports ──

type Handle = *mut std::ffi::c_void;

#[no_mangle] pub extern "C" fn AudioDecoder_OpenFile(fp: *const u16, sr: *mut i32, ch: *mut i32, tf: *mut u64, fmt: *mut u8, is_growing: bool) -> Handle {
    let path = match wstr_to_path(fp) { Some(p) => p, None => { set_error("null path"); return std::ptr::null_mut(); }};
    match FileDec::open(&path, is_growing) {
        Ok(d) => { unsafe {
            if !sr.is_null() { *sr = d.sample_rate as i32; } if !ch.is_null() { *ch = d.channels as i32; } if !tf.is_null() { *tf = d.total_frames; }
            if !fmt.is_null() { let b = d.format_name.as_bytes(); let l = b.len().min(15); std::ptr::copy_nonoverlapping(b.as_ptr(), fmt, l); *fmt.add(l) = 0; }
        } Box::into_raw(Box::new(d)) as Handle }
        Err(e) => { set_error(format!("{}", e)); std::ptr::null_mut() }
    }
}

#[no_mangle] pub extern "C" fn AudioDecoder_ReadFrames(h: Handle, buf: *mut f32, n: i32) -> i64 {
    if h.is_null() || buf.is_null() || n <= 0 { return -1; }
    let d = unsafe { &mut *(h as *mut FileDec) };
    d.read_frames(unsafe { std::slice::from_raw_parts_mut(buf, n as usize * d.channels.max(1) as usize) }, n as usize)
}

#[no_mangle] pub extern "C" fn AudioDecoder_Seek(h: Handle, f: u64) -> i32 {
    if h.is_null() { return -1; } match unsafe { &mut *(h as *mut FileDec) }.seek(f) { Ok(()) => 0, Err(e) => { set_error(format!("{}",e)); -1 } }
}

#[no_mangle] pub extern "C" fn AudioDecoder_Close(h: Handle) { if !h.is_null() { unsafe { drop(Box::from_raw(h as *mut FileDec)); } } }

#[no_mangle] pub extern "C" fn AudioDecoder_GetLastError() -> *const c_char { error_to_c(LAST_ERROR.with(|e| e.lock().unwrap().take())) }

#[no_mangle] pub extern "C" fn AudioDecoder_CreateStreaming(fmt: *const c_char) -> Handle {
    let f = if fmt.is_null() { "mp3" } else { unsafe { CStr::from_ptr(fmt) }.to_str().unwrap_or("mp3") };
    Box::into_raw(Box::new(StreamDec::new(f))) as Handle
}

#[no_mangle] pub extern "C" fn AudioDecoder_FeedData(h: Handle, d: *const std::ffi::c_void, s: i32) -> i32 {
    if h.is_null() || d.is_null() || s <= 0 { return -1; }
    unsafe { &mut *(h as *mut StreamDec) }.feed(unsafe { std::slice::from_raw_parts(d as *const u8, s as usize) }); 0
}

#[no_mangle] pub extern "C" fn AudioDecoder_FeedComplete(h: Handle) { if !h.is_null() { unsafe { (h as *mut StreamDec).as_mut() }.map(|d| d.feed_complete()); } }

#[no_mangle] pub extern "C" fn AudioDecoder_StreamingRead(h: Handle, buf: *mut f32, n: i32) -> i64 {
    if h.is_null() || buf.is_null() || n <= 0 { return -1; }
    let d = unsafe { &mut *(h as *mut StreamDec) };
    let r = d.read_frames(unsafe { std::slice::from_raw_parts_mut(buf, n as usize * d.channels.max(1) as usize) }, n as usize);
    if r == 0 && d.feed_complete && d.pcm.is_empty() { -2 } else { r }
}

#[no_mangle] pub extern "C" fn AudioDecoder_StreamingIsReady(h: Handle) -> i32 { if h.is_null() { 0 } else { unsafe { &*(h as *const StreamDec) }.ready as i32 } }

#[no_mangle] pub extern "C" fn AudioDecoder_StreamingGetInfo(h: Handle, sr: *mut i32, ch: *mut i32, tf: *mut u64) -> i32 {
    if h.is_null() { return -1; } let d = unsafe { &*(h as *const StreamDec) }; if !d.ready { return -1; }
    unsafe { if !sr.is_null() { *sr = d.sample_rate as i32; } if !ch.is_null() { *ch = d.channels as i32; } if !tf.is_null() { *tf = d.total_frames; } } 0
}

#[no_mangle] pub extern "C" fn AudioDecoder_CloseStreaming(h: Handle) { if !h.is_null() { unsafe { drop(Box::from_raw(h as *mut StreamDec)); } } }

// FLAC wrappers
#[no_mangle] pub extern "C" fn OpenFlacStream(fp: *const u16, sr: *mut i32, ch: *mut i32, tf: *mut u64) -> Handle { let mut fb=[0u8;16]; AudioDecoder_OpenFile(fp,sr,ch,tf,fb.as_mut_ptr(),false) }
#[no_mangle] pub extern "C" fn ReadFlacFrames(h: Handle, b: *mut f32, n: u64) -> i64 { AudioDecoder_ReadFrames(h,b,n as i32) }
#[no_mangle] pub extern "C" fn SeekFlacStream(h: Handle, f: u64) -> i32 { AudioDecoder_Seek(h,f) }
#[no_mangle] pub extern "C" fn CloseFlacStream(h: Handle) { AudioDecoder_Close(h); }
#[no_mangle] pub extern "C" fn FlacGetLastError() -> *const c_char { AudioDecoder_GetLastError() }

#[repr(C)] pub struct FlacAudioInfo { sample_rate: i32, channels: i32, total_pcm_frame_count: u64, pcm_data: *mut f32, pcm_data_size: usize }

#[no_mangle] pub extern "C" fn DecodeFlacFile(fp: *const u16, info: *mut FlacAudioInfo) -> i32 {
    if fp.is_null() || info.is_null() { return -1; }
    let path = match wstr_to_path(fp) { Some(p) => p, None => { set_error("null"); return -1; }};
    let mut d = match FileDec::open(&path, false) { Ok(d) => d, Err(e) => { set_error(format!("{}",e)); return -1; }};
    let ch = d.channels.max(1) as usize; let mut all=Vec::new(); let mut tmp=vec![0f32;4096*ch];
    loop { let r = d.read_frames(&mut tmp, 4096); if r <= 0 { break; } all.extend_from_slice(&tmp[..(r as usize)*ch]); }
    let sz = all.len()*std::mem::size_of::<f32>();
    let ptr = unsafe { let layout=std::alloc::Layout::from_size_align(sz,4).unwrap(); let p=std::alloc::alloc(layout) as *mut f32; std::ptr::copy_nonoverlapping(all.as_ptr(),p,all.len()); p };
    unsafe { (*info).sample_rate=d.sample_rate as i32; (*info).channels=d.channels as i32; (*info).total_pcm_frame_count=d.total_frames.max(1); (*info).pcm_data=ptr; (*info).pcm_data_size=sz; }
    std::mem::forget(all); 0
}

#[no_mangle] pub extern "C" fn FreeFlacData(info: *mut FlacAudioInfo) {
    if info.is_null() { return; }
    unsafe { if !(*info).pcm_data.is_null() { let sz=(*info).pcm_data_size; let layout=std::alloc::Layout::from_size_align(sz,4).unwrap(); std::alloc::dealloc((*info).pcm_data as *mut u8, layout); (*info).pcm_data=std::ptr::null_mut(); (*info).pcm_data_size=0; } }
}
