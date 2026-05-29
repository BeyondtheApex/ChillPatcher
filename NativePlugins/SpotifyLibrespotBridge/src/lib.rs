use std::{
    ffi::{CStr, c_char},
    ptr,
    slice,
    sync::{
        Arc, Mutex,
        atomic::{AtomicBool, AtomicU32, Ordering},
    },
    thread::{self, JoinHandle},
    time::Duration,
};

use crossbeam_channel::{Receiver, Sender, bounded};
use librespot_connect::{ConnectConfig, Spirc};
use librespot_core::{
    Session, SessionConfig,
    authentication::Credentials,
    cache::Cache,
};
use librespot_playback::{
    SAMPLE_RATE, NUM_CHANNELS,
    audio_backend::{Sink, SinkError, SinkResult},
    config::PlayerConfig,
    convert::Converter,
    decoder::AudioPacket,
    mixer::{MixerConfig, find as find_mixer},
    player::Player,
};

const QUEUE_CHUNKS: usize = 256;
const ERROR_OK: u32 = 0;
const ERROR_NULL: u32 = 1;
const ERROR_UTF8: u32 = 2;
const ERROR_START: u32 = 3;

struct PcmSink {
    tx: Sender<Vec<f32>>,
    closed: Arc<AtomicBool>,
}

impl Sink for PcmSink {
    fn write(&mut self, packet: AudioPacket, converter: &mut Converter) -> SinkResult<()> {
        if self.closed.load(Ordering::Acquire) {
            return Err(SinkError::OnWrite("bridge closed".to_string()));
        }

        let samples = match packet {
            AudioPacket::Samples(samples) => converter.f64_to_f32(&samples).to_vec(),
            AudioPacket::Raw(bytes) => {
                let byte_len = bytes.len() - (bytes.len() % std::mem::size_of::<f32>());
                let raw = &bytes[..byte_len];
                let mut out = Vec::with_capacity(byte_len / std::mem::size_of::<f32>());
                for chunk in raw.chunks_exact(std::mem::size_of::<f32>()) {
                    out.push(f32::from_ne_bytes([chunk[0], chunk[1], chunk[2], chunk[3]]));
                }
                out
            }
        };

        if samples.is_empty() {
            return Ok(());
        }

        self.tx
            .send(samples)
            .map_err(|_| SinkError::OnWrite("PCM queue closed".to_string()))
    }
}

pub struct Bridge {
    rx: Receiver<Vec<f32>>,
    pending: Mutex<Vec<f32>>,
    closed: Arc<AtomicBool>,
    eof: Arc<AtomicBool>,
    ready: Arc<AtomicBool>,
    last_error: Arc<AtomicU32>,
    spirc: Arc<Mutex<Option<Spirc>>>,
    thread: Mutex<Option<JoinHandle<()>>>,
}

#[unsafe(no_mangle)]
pub extern "C" fn omni_spotify_create(
    access_token: *const c_char,
    device_name: *const c_char,
    cache_dir: *const c_char,
) -> *mut Bridge {
    let access_token = match cstr(access_token) {
        Ok(value) => value,
        Err(_) => return ptr::null_mut(),
    };
    let device_name = match cstr(device_name) {
        Ok(value) => value,
        Err(_) => return ptr::null_mut(),
    };
    let cache_dir = match cstr(cache_dir) {
        Ok(value) => value,
        Err(_) => return ptr::null_mut(),
    };

    let (tx, rx) = bounded::<Vec<f32>>(QUEUE_CHUNKS);
    let bridge = Box::new(Bridge {
        rx,
        pending: Mutex::new(Vec::new()),
        closed: Arc::new(AtomicBool::new(false)),
        eof: Arc::new(AtomicBool::new(false)),
        ready: Arc::new(AtomicBool::new(false)),
        last_error: Arc::new(AtomicU32::new(ERROR_OK)),
        spirc: Arc::new(Mutex::new(None)),
        thread: Mutex::new(None),
    });

    let closed = bridge.closed.clone();
    let eof = bridge.eof.clone();
    let ready = bridge.ready.clone();
    let last_error = bridge.last_error.clone();
    let spirc_slot = bridge.spirc.clone();

    let join = thread::spawn(move || {
        if let Err(err) = run_connect_device(access_token, device_name, cache_dir, tx, closed, ready, eof.clone(), spirc_slot) {
            last_error.store(err, Ordering::Release);
            eof.store(true, Ordering::Release);
        }
    });

    if let Ok(mut thread) = bridge.thread.lock() {
        *thread = Some(join);
    }

    Box::into_raw(bridge)
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn omni_spotify_destroy(handle: *mut Bridge) {
    if handle.is_null() {
        return;
    }

    let bridge = unsafe { Box::from_raw(handle) };
    bridge.closed.store(true, Ordering::Release);

    if let Ok(mut spirc) = bridge.spirc.lock() {
        if let Some(spirc) = spirc.take() {
            let _ = spirc.shutdown();
        }
    }

    if let Ok(mut thread) = bridge.thread.lock() {
        let _ = thread.take();
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn omni_spotify_read(
    handle: *mut Bridge,
    out: *mut f32,
    frames_to_read: i32,
) -> i32 {
    if handle.is_null() || out.is_null() || frames_to_read <= 0 {
        return 0;
    }

    let bridge = unsafe { &*handle };
    let sample_count = frames_to_read as usize * NUM_CHANNELS as usize;
    let out = unsafe { slice::from_raw_parts_mut(out, sample_count) };
    let mut written = 0usize;

    if let Ok(mut pending) = bridge.pending.lock() {
        while written < sample_count {
            if pending.is_empty() {
                match bridge.rx.recv_timeout(Duration::from_millis(15)) {
                    Ok(chunk) => *pending = chunk,
                    Err(_) => break,
                }
            }

            let take = (sample_count - written).min(pending.len());
            out[written..written + take].copy_from_slice(&pending[..take]);
            pending.drain(..take);
            written += take;
        }
    }

    if written < sample_count {
        out[written..].fill(0.0);
    }

    (written / NUM_CHANNELS as usize) as i32
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn omni_spotify_is_ready(handle: *mut Bridge) -> i32 {
    if handle.is_null() {
        return 0;
    }
    let bridge = unsafe { &*handle };
    bridge.ready.load(Ordering::Acquire) as i32
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn omni_spotify_is_eof(handle: *mut Bridge) -> i32 {
    if handle.is_null() {
        return 1;
    }
    let bridge = unsafe { &*handle };
    bridge.eof.load(Ordering::Acquire) as i32
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn omni_spotify_last_error(handle: *mut Bridge) -> u32 {
    if handle.is_null() {
        return ERROR_NULL;
    }
    let bridge = unsafe { &*handle };
    bridge.last_error.load(Ordering::Acquire)
}

#[unsafe(no_mangle)]
pub extern "C" fn omni_spotify_sample_rate() -> i32 {
    SAMPLE_RATE as i32
}

#[unsafe(no_mangle)]
pub extern "C" fn omni_spotify_channels() -> i32 {
    NUM_CHANNELS as i32
}

fn run_connect_device(
    access_token: String,
    device_name: String,
    cache_dir: String,
    tx: Sender<Vec<f32>>,
    closed: Arc<AtomicBool>,
    ready: Arc<AtomicBool>,
    eof: Arc<AtomicBool>,
    spirc_slot: Arc<Mutex<Option<Spirc>>>,
) -> Result<(), u32> {
    let runtime = tokio::runtime::Builder::new_multi_thread()
        .enable_all()
        .thread_name("omni-spotify-librespot")
        .build()
        .map_err(|_| ERROR_START)?;

    runtime.block_on(async move {
        let cache = Cache::new(Some(cache_dir.as_str()), Some(cache_dir.as_str()), None, None)
            .map_err(|_| ERROR_START)?;
        let session = Session::new(SessionConfig::default(), Some(cache));
        let credentials = Credentials::with_access_token(access_token);

        let mixer_fn = find_mixer(None).ok_or(ERROR_START)?;
        let mixer = mixer_fn(MixerConfig::default()).map_err(|_| ERROR_START)?;
        let volume = mixer.get_soft_volume();

        let sink_closed = closed.clone();
        let player = Player::new(
            PlayerConfig {
                bitrate: librespot_playback::config::Bitrate::Bitrate320,
                ..PlayerConfig::default()
            },
            session.clone(),
            volume,
            move || Box::new(PcmSink { tx, closed: sink_closed }) as Box<dyn Sink>,
        );

        let connect_config = ConnectConfig {
            name: device_name,
            initial_volume: u16::MAX,
            ..ConnectConfig::default()
        };

        let (spirc, spirc_task) = Spirc::new(connect_config, session, credentials, player, mixer)
            .await
            .map_err(|_| ERROR_START)?;

        if let Ok(mut slot) = spirc_slot.lock() {
            *slot = Some(spirc);
        }

        ready.store(true, Ordering::Release);
        spirc_task.await;
        eof.store(true, Ordering::Release);
        Ok(())
    })
}

fn cstr(ptr: *const c_char) -> Result<String, u32> {
    if ptr.is_null() {
        return Err(ERROR_NULL);
    }

    unsafe { CStr::from_ptr(ptr) }
        .to_str()
        .map(|s| s.to_string())
        .map_err(|_| ERROR_UTF8)
}
