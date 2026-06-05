use std::{
    collections::VecDeque,
    ffi::{c_char, c_void, CStr, CString},
    os::raw::c_int,
    ptr,
    str::FromStr,
    sync::{
        atomic::{AtomicBool, AtomicU32, AtomicU64, AtomicUsize, Ordering},
        Arc, LazyLock, Mutex,
    },
    thread::{self, JoinHandle},
    time::Duration,
};

use cpal::{
    traits::{DeviceTrait, HostTrait, StreamTrait},
    Device, DeviceId, SampleFormat, Stream, StreamConfig,
};
use libloading::{Library, Symbol};
use rubato::{
    audioadapter_buffers::direct::InterleavedSlice, calculate_cutoff, Async, FixedAsync, Resampler,
    SincInterpolationParameters, SincInterpolationType, WindowFunction,
};
use serde::Serialize;

type PcmHandle = *mut c_void;

const OMNI_PCM_OK: i32 = 0;
const OMNI_PCM_FLAG_FORMAT_READY: u32 = 1 << 0;
const OMNI_PCM_FLAG_STREAM_ERROR: u32 = 1 << 2;
const OMNI_PCM_STATE_PAUSED: i32 = 3;
const OMNI_PCM_STATE_ERROR: i32 = 6;
const RESAMPLER_CHUNK_FRAMES: usize = 1024;
const LOCAL_BUFFER_TARGET_DIVISOR: usize = 10;
const LOCAL_BUFFER_MIN_CHUNKS: usize = 3;

#[repr(C)]
#[derive(Clone, Copy, Default)]
struct OmniPcmInfo {
    sample_rate: i32,
    channels: i32,
    bytes_per_frame: i32,
    buffer_frames: i32,
    total_frames_hint: i64,
    decoded_total_frames: i64,
    effective_total_frames: i64,
}

#[repr(C)]
#[derive(Clone, Copy)]
struct OmniPcmSnapshot {
    version: u32,
    sample_rate: i32,
    channels: i32,
    bytes_per_frame: i32,
    buffer_frames: i32,
    legacy_play_state: i32,
    flags: u32,
    write_cursor: i64,
    read_cursor: i64,
    stream_id: i64,
    state: i32,
    error_code: i32,
    total_frames_hint: i64,
    decoded_total_frames: i64,
    final_write_cursor: i64,
    audible_cursor: i64,
    seek_frame: i64,
    seek_generation: i64,
    last_update_tick: i64,
    format_generation: i32,
    current_uuid: [u8; 64],
}

impl Default for OmniPcmSnapshot {
    fn default() -> Self {
        Self {
            version: 0,
            sample_rate: 0,
            channels: 0,
            bytes_per_frame: 0,
            buffer_frames: 0,
            legacy_play_state: 0,
            flags: 0,
            write_cursor: 0,
            read_cursor: 0,
            stream_id: 0,
            state: 0,
            error_code: 0,
            total_frames_hint: 0,
            decoded_total_frames: 0,
            final_write_cursor: 0,
            audible_cursor: 0,
            seek_frame: 0,
            seek_generation: 0,
            last_update_tick: 0,
            format_generation: 0,
            current_uuid: [0; 64],
        }
    }
}

struct OmniApi {
    _lib: Library,
    open_utf8: unsafe extern "C" fn(*const c_char) -> PcmHandle,
    close: unsafe extern "C" fn(PcmHandle),
    get_info: unsafe extern "C" fn(PcmHandle, *mut OmniPcmInfo) -> i32,
    get_snapshot: unsafe extern "C" fn(PcmHandle, *mut OmniPcmSnapshot) -> i32,
    bind_current: unsafe extern "C" fn(PcmHandle) -> i32,
    read_frames: unsafe extern "C" fn(PcmHandle, *mut f32, i32) -> i64,
    set_audible: unsafe extern "C" fn(PcmHandle, i64, i32) -> i32,
}

impl OmniApi {
    fn load() -> Result<Arc<Self>, String> {
        unsafe {
            let lib = Library::new("OmniPcmShared.dll").map_err(|e| e.to_string())?;

            macro_rules! sym {
                ($name:literal, $ty:ty) => {{
                    let symbol: Symbol<$ty> = lib.get($name).map_err(|e| e.to_string())?;
                    *symbol
                }};
            }

            let open_utf8 = sym!(
                b"OmniPcm_OpenUtf8\0",
                unsafe extern "C" fn(*const c_char) -> PcmHandle
            );
            let close = sym!(b"OmniPcm_Close\0", unsafe extern "C" fn(PcmHandle));
            let get_info = sym!(
                b"OmniPcm_GetInfo\0",
                unsafe extern "C" fn(PcmHandle, *mut OmniPcmInfo) -> i32
            );
            let get_snapshot = sym!(
                b"OmniPcm_GetSnapshot\0",
                unsafe extern "C" fn(PcmHandle, *mut OmniPcmSnapshot) -> i32
            );
            let bind_current = sym!(
                b"OmniPcm_BindCurrentStream\0",
                unsafe extern "C" fn(PcmHandle) -> i32
            );
            let read_frames = sym!(
                b"OmniPcm_ReadFrames\0",
                unsafe extern "C" fn(PcmHandle, *mut f32, i32) -> i64
            );
            let set_audible = sym!(
                b"OmniPcm_SetAudibleCursor\0",
                unsafe extern "C" fn(PcmHandle, i64, i32) -> i32
            );

            Ok(Arc::new(Self {
                _lib: lib,
                open_utf8,
                close,
                get_info,
                get_snapshot,
                bind_current,
                read_frames,
                set_audible,
            }))
        }
    }
}

struct PcmClient {
    api: Arc<OmniApi>,
    handle: PcmHandle,
}

unsafe impl Send for PcmClient {}

impl PcmClient {
    fn open(api: Arc<OmniApi>, instance_id: &str) -> Result<Self, String> {
        let map_name = CString::new(format!("Global\\OmniMixPlayer_PCM_{instance_id}"))
            .map_err(|_| "invalid instance id".to_string())?;
        let handle = unsafe { (api.open_utf8)(map_name.as_ptr()) };
        if handle.is_null() {
            return Err("OmniPcm_OpenUtf8 returned null".to_string());
        }
        unsafe {
            (api.bind_current)(handle);
        }
        Ok(Self { api, handle })
    }
}

impl Drop for PcmClient {
    fn drop(&mut self) {
        if !self.handle.is_null() {
            unsafe {
                (self.api.close)(self.handle);
            }
        }
    }
}

#[derive(Clone)]
struct EngineStatus {
    instance_id: String,
    input_sample_rate: u32,
    input_channels: u32,
    output_sample_rate: u32,
    output_channels: u32,
    stream_id: i64,
    format_generation: i32,
    seek_generation: i64,
    last_error: String,
}

impl Default for EngineStatus {
    fn default() -> Self {
        Self {
            instance_id: String::new(),
            input_sample_rate: 0,
            input_channels: 0,
            output_sample_rate: 0,
            output_channels: 0,
            stream_id: 0,
            format_generation: 0,
            seek_generation: 0,
            last_error: String::new(),
        }
    }
}

struct Segment {
    output_end: u64,
    input_end: i64,
}

struct FeederState {
    pcm: PcmClient,
    status: Arc<Mutex<EngineStatus>>,
    output_sample_rate: u32,
    output_channels: usize,
    input_sample_rate: u32,
    input_channels: usize,
    stream_id: i64,
    format_generation: i32,
    current_uuid: [u8; 64],
    seek_generation: i64,
    pending: Vec<f32>,
    read_buf: Vec<f32>,
    last_read_cursor: i64,
    pending_start_cursor: i64,
    last_audible_cursor: i64,
    output_written_frames: u64,
    segments: VecDeque<Segment>,
    resampler: Option<Async<f32>>,
    resampler_input: Vec<f32>,
}

impl FeederState {
    fn new(
        pcm: PcmClient,
        status: Arc<Mutex<EngineStatus>>,
        output_sample_rate: u32,
        output_channels: usize,
    ) -> Self {
        Self {
            pcm,
            status,
            output_sample_rate,
            output_channels,
            input_sample_rate: 0,
            input_channels: 0,
            stream_id: 0,
            format_generation: 0,
            current_uuid: [0; 64],
            seek_generation: 0,
            pending: Vec::new(),
            read_buf: Vec::new(),
            last_read_cursor: 0,
            pending_start_cursor: 0,
            last_audible_cursor: 0,
            output_written_frames: 0,
            segments: VecDeque::new(),
            resampler: None,
            resampler_input: Vec::new(),
        }
    }

    fn reset_input(&mut self) {
        self.pending.clear();
        self.read_buf.clear();
        self.resampler_input.clear();
    }

    fn reset_input_at(&mut self, cursor: i64) {
        self.reset_input();
        self.last_read_cursor = cursor;
        self.pending_start_cursor = cursor;
        self.last_audible_cursor = cursor;
        self.segments.clear();
        if let Some(resampler) = self.create_resampler() {
            self.resampler = Some(resampler);
        }
    }

    fn pump(&mut self, ring: &AudioRingBuffer) -> usize {
        self.advance_audible(ring);
        if self.output_sample_rate == 0 || self.output_channels == 0 || ring.writable_frames() == 0
        {
            return 0;
        }
        let target_buffered_frames = (self.output_sample_rate as usize
            / LOCAL_BUFFER_TARGET_DIVISOR)
            .max(RESAMPLER_CHUNK_FRAMES * LOCAL_BUFFER_MIN_CHUNKS);
        if ring.readable_frames() >= target_buffered_frames {
            return 0;
        }

        let mut snapshot = OmniPcmSnapshot::default();
        unsafe {
            if (self.pcm.api.get_snapshot)(self.pcm.handle, &mut snapshot) != OMNI_PCM_OK {
                return 0;
            }
            if (snapshot.flags & OMNI_PCM_FLAG_STREAM_ERROR) != 0
                || snapshot.state == OMNI_PCM_STATE_ERROR
            {
                self.set_error("shared PCM stream reported an error");
                self.reset_input();
                ring.drain();
                return 0;
            }
            if snapshot.legacy_play_state == 2 || snapshot.state == OMNI_PCM_STATE_PAUSED {
                self.reset_input();
                ring.drain();
                return 0;
            }
            if snapshot.version >= 2 && (snapshot.flags & OMNI_PCM_FLAG_FORMAT_READY) == 0 {
                self.reset_input();
                ring.drain();
                return 0;
            }
        }

        let stream_changed = snapshot.stream_id != self.stream_id
            || snapshot.format_generation != self.format_generation
            || snapshot.current_uuid != self.current_uuid;
        let seek_detected = snapshot.seek_generation != self.seek_generation;
        if stream_changed || seek_detected {
            unsafe {
                if stream_changed {
                    (self.pcm.api.bind_current)(self.pcm.handle);
                }
            }
            if stream_changed {
                let mut info = OmniPcmInfo::default();
                unsafe {
                    if (self.pcm.api.get_info)(self.pcm.handle, &mut info) != OMNI_PCM_OK {
                        return 0;
                    }
                }
                self.input_sample_rate = info.sample_rate.max(1) as u32;
                self.input_channels = info.channels.clamp(1, 8) as usize;
                self.stream_id = snapshot.stream_id;
                self.format_generation = snapshot.format_generation;
                self.current_uuid = snapshot.current_uuid;
                self.resampler = self.create_resampler();
                self.update_status();
            }
            self.seek_generation = snapshot.seek_generation;
            self.reset_input_at(snapshot.read_cursor);
            ring.drain();
            self.output_written_frames = ring.consumed_frames();
            self.update_status();
            unsafe {
                let _ = (self.pcm.api.set_audible)(
                    self.pcm.handle,
                    self.pending_start_cursor,
                    if seek_detected { 1 } else { 0 },
                );
            }
        }

        if self.input_sample_rate == 0 || self.input_channels == 0 {
            return 0;
        }

        let needed_output_frames = self
            .resampler
            .as_ref()
            .map(|resampler| resampler.output_frames_next())
            .unwrap_or(RESAMPLER_CHUNK_FRAMES);
        if ring.writable_frames() < needed_output_frames {
            return 0;
        }

        let Some((output, input_frames)) = self.process_resampler_block() else {
            return 0;
        };
        if output.is_empty() {
            self.drop_pending_frames(input_frames);
            return 0;
        }

        let input_end = self.pending_start_cursor + input_frames as i64;
        let wrote_samples = ring.write_samples(&output);
        let wrote_frames = wrote_samples / self.output_channels;
        if wrote_frames > 0 {
            self.output_written_frames += wrote_frames as u64;
            self.segments.push_back(Segment {
                output_end: self.output_written_frames,
                input_end,
            });
        }
        self.drop_pending_frames(input_frames);
        wrote_frames
    }

    fn create_resampler(&self) -> Option<Async<f32>> {
        if self.input_sample_rate == 0 || self.output_sample_rate == 0 || self.output_channels == 0
        {
            return None;
        }
        if self.input_sample_rate == self.output_sample_rate {
            return None;
        }

        let window = WindowFunction::BlackmanHarris2;
        let sinc_len = 256;
        let params = SincInterpolationParameters {
            sinc_len,
            f_cutoff: calculate_cutoff(sinc_len, window),
            interpolation: SincInterpolationType::Cubic,
            oversampling_factor: 256,
            window,
        };
        Async::<f32>::new_sinc(
            self.output_sample_rate as f64 / self.input_sample_rate as f64,
            1.01,
            &params,
            RESAMPLER_CHUNK_FRAMES,
            self.output_channels,
            FixedAsync::Input,
        )
        .ok()
    }

    fn process_resampler_block(&mut self) -> Option<(Vec<f32>, usize)> {
        let input_frames = self
            .resampler
            .as_ref()
            .map(|resampler| resampler.input_frames_next())
            .unwrap_or(RESAMPLER_CHUNK_FRAMES);
        self.ensure_pending_frames(input_frames);
        if self.pending.len() / self.input_channels < input_frames {
            return None;
        }

        let samples = input_frames * self.output_channels;
        if self.resampler_input.len() < samples {
            self.resampler_input.resize(samples, 0.0);
        }
        mix_channels(
            &self.pending,
            self.input_channels,
            input_frames,
            &mut self.resampler_input[..samples],
            self.output_channels,
        );

        if let Some(resampler) = self.resampler.as_mut() {
            let input = InterleavedSlice::new(
                &self.resampler_input[..samples],
                self.output_channels,
                input_frames,
            )
            .ok()?;
            let output = resampler.process(&input, 0, None).ok()?.take_data();
            Some((output, input_frames))
        } else {
            Some((self.resampler_input[..samples].to_vec(), input_frames))
        }
    }

    fn ensure_pending_frames(&mut self, min_frames: usize) {
        while self.pending.len() / self.input_channels < min_frames {
            let pending_frames = self.pending.len() / self.input_channels;
            let want_frames = (min_frames - pending_frames).max(256).min(4096);
            let sample_count = want_frames * self.input_channels;
            if self.read_buf.len() < sample_count {
                self.read_buf.resize(sample_count, 0.0);
            }

            let got = unsafe {
                (self.pcm.api.read_frames)(
                    self.pcm.handle,
                    self.read_buf.as_mut_ptr(),
                    want_frames as i32,
                )
            };
            if got <= 0 {
                break;
            }

            let samples = got as usize * self.input_channels;
            self.pending.extend_from_slice(&self.read_buf[..samples]);
            self.last_read_cursor += got;
        }
    }

    fn drop_pending_frames(&mut self, drop_frames: usize) {
        let drop_samples = drop_frames * self.input_channels;
        if drop_samples >= self.pending.len() {
            self.pending.clear();
        } else {
            self.pending.drain(..drop_samples);
        }
        self.pending_start_cursor += drop_frames as i64;
    }

    fn advance_audible(&mut self, ring: &AudioRingBuffer) {
        let consumed = ring.consumed_frames();
        while let Some(segment) = self.segments.front() {
            if segment.output_end > consumed {
                break;
            }
            self.last_audible_cursor = self.last_audible_cursor.max(segment.input_end);
            self.segments.pop_front();
        }
        if self.last_audible_cursor > 0 {
            unsafe {
                let _ = (self.pcm.api.set_audible)(self.pcm.handle, self.last_audible_cursor, 0);
            }
        }
    }

    fn update_status(&self) {
        if let Ok(mut status) = self.status.lock() {
            status.input_sample_rate = self.input_sample_rate;
            status.input_channels = self.input_channels as u32;
            status.output_sample_rate = self.output_sample_rate;
            status.output_channels = self.output_channels as u32;
            status.stream_id = self.stream_id;
            status.format_generation = self.format_generation;
            status.seek_generation = self.seek_generation;
        }
    }

    fn set_error(&self, message: &str) {
        if let Ok(mut status) = self.status.lock() {
            status.last_error = message.to_string();
        }
    }
}

fn mix_channels(
    input: &[f32],
    input_channels: usize,
    frames: usize,
    output: &mut [f32],
    output_channels: usize,
) {
    for frame in 0..frames {
        let src = &input[frame * input_channels..(frame + 1) * input_channels];
        let dst = &mut output[frame * output_channels..(frame + 1) * output_channels];
        mix_frame(src, dst);
    }
}

fn mix_frame(input: &[f32], output: &mut [f32]) {
    if input.is_empty() || output.is_empty() {
        return;
    }

    if output.len() == 1 {
        output[0] = match input.len() {
            1 => input[0],
            2 => (input[0] + input[1]) * 0.5,
            _ => {
                let l = input[0];
                let r = input.get(1).copied().unwrap_or(l);
                let c = input.get(2).copied().unwrap_or(0.0);
                let ls = input.get(4).copied().unwrap_or(0.0);
                let rs = input.get(5).copied().unwrap_or(0.0);
                (l + r + 0.707 * c + 0.5 * (ls + rs)) * 0.293
            }
        };
        return;
    }

    let (left, right) = stereo_pair(input);
    output[0] = left;
    output[1] = right;

    if output.len() > 2 {
        let center = input
            .get(2)
            .copied()
            .unwrap_or_else(|| (left + right) * 0.5);
        output[2] = center;
    }
    if output.len() > 3 {
        output[3] = input.get(3).copied().unwrap_or(0.0);
    }
    if output.len() > 4 {
        output[4] = input.get(4).copied().unwrap_or(left);
    }
    if output.len() > 5 {
        output[5] = input.get(5).copied().unwrap_or(right);
    }
    if output.len() > 6 {
        output[6] = input.get(6).copied().unwrap_or(output[4]);
    }
    if output.len() > 7 {
        output[7] = input.get(7).copied().unwrap_or(output[5]);
    }
    for ch in 8..output.len() {
        output[ch] = 0.0;
    }
}

fn stereo_pair(input: &[f32]) -> (f32, f32) {
    match input.len() {
        0 => (0.0, 0.0),
        1 => (input[0], input[0]),
        2 => (input[0], input[1]),
        _ => {
            let l = input[0];
            let r = input.get(1).copied().unwrap_or(l);
            let c = input.get(2).copied().unwrap_or(0.0);
            let ls = input.get(4).copied().unwrap_or(0.0);
            let rs = input.get(5).copied().unwrap_or(0.0);
            let lb = input.get(6).copied().unwrap_or(0.0);
            let rb = input.get(7).copied().unwrap_or(0.0);
            (
                (l + 0.707 * c + 0.707 * ls + 0.5 * lb) * 0.5,
                (r + 0.707 * c + 0.707 * rs + 0.5 * rb) * 0.5,
            )
        }
    }
}

struct AudioRingBuffer {
    samples: Vec<AtomicU32>,
    capacity: usize,
    output_channels: usize,
    write_sample: AtomicUsize,
    read_sample: AtomicUsize,
    consumed_frames: AtomicU64,
}

impl AudioRingBuffer {
    fn new(output_sample_rate: u32, output_channels: usize) -> Self {
        let frames = output_sample_rate.max(1) as usize;
        let capacity = frames * output_channels.max(1);
        let samples = (0..capacity).map(|_| AtomicU32::new(0)).collect();
        Self {
            samples,
            capacity,
            output_channels: output_channels.max(1),
            write_sample: AtomicUsize::new(0),
            read_sample: AtomicUsize::new(0),
            consumed_frames: AtomicU64::new(0),
        }
    }

    fn writable_frames(&self) -> usize {
        self.writable_samples() / self.output_channels
    }

    fn readable_frames(&self) -> usize {
        let write = self.write_sample.load(Ordering::Acquire);
        let read = self.read_sample.load(Ordering::Acquire);
        write.saturating_sub(read) / self.output_channels
    }

    fn writable_samples(&self) -> usize {
        let write = self.write_sample.load(Ordering::Acquire);
        let read = self.read_sample.load(Ordering::Acquire);
        self.capacity.saturating_sub(write.saturating_sub(read))
    }

    fn consumed_frames(&self) -> u64 {
        self.consumed_frames.load(Ordering::Acquire)
    }

    fn drain(&self) {
        let write = self.write_sample.load(Ordering::Acquire);
        self.read_sample.store(write, Ordering::Release);
    }

    fn write_samples(&self, input: &[f32]) -> usize {
        let write = self.write_sample.load(Ordering::Relaxed);
        let read = self.read_sample.load(Ordering::Acquire);
        let writable = self.capacity.saturating_sub(write.saturating_sub(read));
        let count = input.len().min(writable);
        for (i, sample) in input.iter().take(count).enumerate() {
            self.samples[(write + i) % self.capacity].store(sample.to_bits(), Ordering::Relaxed);
        }
        self.write_sample.store(write + count, Ordering::Release);
        count
    }

    fn read_to(&self, output: &mut [f32]) {
        output.fill(0.0);
        let read = self.read_sample.load(Ordering::Relaxed);
        let write = self.write_sample.load(Ordering::Acquire);
        let readable = write.saturating_sub(read);
        let count = output.len().min(readable);
        for (i, dst) in output.iter_mut().take(count).enumerate() {
            *dst = f32::from_bits(self.samples[(read + i) % self.capacity].load(Ordering::Relaxed));
        }
        if count > 0 {
            self.read_sample.store(read + count, Ordering::Release);
            self.consumed_frames
                .fetch_add((count / self.output_channels) as u64, Ordering::Release);
        }
    }
}

struct AudioEngine {
    api: Option<Arc<OmniApi>>,
    stream: Option<Stream>,
    ring: Option<Arc<AudioRingBuffer>>,
    feeder_stop: Option<Arc<AtomicBool>>,
    feeder_thread: Option<JoinHandle<()>>,
    status: Arc<Mutex<EngineStatus>>,
    selected_device_id: Option<String>,
    running: bool,
    last_error: String,
}

impl Default for AudioEngine {
    fn default() -> Self {
        Self {
            api: None,
            stream: None,
            ring: None,
            feeder_stop: None,
            feeder_thread: None,
            status: Arc::new(Mutex::new(EngineStatus::default())),
            selected_device_id: None,
            running: false,
            last_error: String::new(),
        }
    }
}

impl AudioEngine {
    fn ensure_api(&mut self) -> Result<Arc<OmniApi>, String> {
        if let Some(api) = &self.api {
            return Ok(api.clone());
        }
        let api = OmniApi::load()?;
        self.api = Some(api.clone());
        Ok(api)
    }

    fn start(&mut self, instance_id: &str, device_id: Option<String>) -> Result<(), String> {
        self.stop();
        let api = self.ensure_api()?;
        let pcm = PcmClient::open(api, instance_id)?;
        self.selected_device_id = device_id;
        let stream = self.build_stream(pcm, instance_id)?;
        stream.play().map_err(|e| e.to_string())?;
        self.stream = Some(stream);
        self.running = true;
        Ok(())
    }

    fn spawn_feeder(
        &mut self,
        pcm: PcmClient,
        instance_id: &str,
        output_sample_rate: u32,
        output_channels: usize,
    ) -> Arc<AudioRingBuffer> {
        self.stop_feeder();
        let ring = Arc::new(AudioRingBuffer::new(output_sample_rate, output_channels));
        let stop = Arc::new(AtomicBool::new(false));
        if let Ok(mut status) = self.status.lock() {
            *status = EngineStatus {
                instance_id: instance_id.to_string(),
                output_sample_rate,
                output_channels: output_channels as u32,
                ..EngineStatus::default()
            };
        }

        let feeder_ring = ring.clone();
        let feeder_stop = stop.clone();
        let status = self.status.clone();
        let thread = thread::spawn(move || {
            let mut feeder = FeederState::new(pcm, status, output_sample_rate, output_channels);
            while !feeder_stop.load(Ordering::Acquire) {
                let wrote = feeder.pump(&feeder_ring);
                if wrote == 0 {
                    thread::sleep(Duration::from_millis(2));
                }
            }
        });

        self.ring = Some(ring.clone());
        self.feeder_stop = Some(stop);
        self.feeder_thread = Some(thread);
        ring
    }

    fn stop(&mut self) {
        self.stream = None;
        self.stop_feeder();
        self.running = false;
        self.ring = None;
        if let Ok(mut status) = self.status.lock() {
            *status = EngineStatus::default();
        }
    }

    fn stop_feeder(&mut self) {
        if let Some(stop) = self.feeder_stop.take() {
            stop.store(true, Ordering::Release);
        }
        if let Some(thread) = self.feeder_thread.take() {
            let _ = thread.join();
        }
    }

    fn set_device(&mut self, device_id: Option<String>) -> Result<(), String> {
        self.selected_device_id = device_id;
        if !self.running {
            return Ok(());
        }
        let instance_id = self
            .status
            .lock()
            .ok()
            .map(|status| status.instance_id.clone())
            .unwrap_or_default();
        if instance_id.is_empty() {
            return Err("audio engine has no active instance".to_string());
        }
        self.stream = None;
        self.stop_feeder();
        let api = self.ensure_api()?;
        let pcm = PcmClient::open(api, &instance_id)?;
        let stream = self.build_stream(pcm, &instance_id)?;
        stream.play().map_err(|e| e.to_string())?;
        self.stream = Some(stream);
        Ok(())
    }

    fn build_stream(&mut self, pcm: PcmClient, instance_id: &str) -> Result<Stream, String> {
        let host = cpal::default_host();
        let device = select_output_device(&host, self.selected_device_id.as_deref())?;
        let supported = device.default_output_config().map_err(|e| e.to_string())?;
        let sample_format = supported.sample_format();
        let config = supported.config();
        let output_sample_rate = config.sample_rate;
        let output_channels = config.channels.max(1) as usize;
        let ring = self.spawn_feeder(pcm, instance_id, output_sample_rate, output_channels);

        let err_status = self.status.clone();
        let err_fn = move |err: cpal::StreamError| {
            if let Ok(mut status) = err_status.lock() {
                status.last_error = err.to_string();
            }
        };

        match sample_format {
            SampleFormat::F32 => build_output_stream_f32(&device, &config, ring, err_fn),
            SampleFormat::I16 => build_output_stream_i16(&device, &config, ring, err_fn),
            SampleFormat::U16 => build_output_stream_u16(&device, &config, ring, err_fn),
            other => Err(format!("unsupported output sample format: {other:?}")),
        }
    }

    fn state_json(&self) -> String {
        let status = self.status.lock().ok().map(|status| status.clone());
        let state = EngineState {
            running: self.running,
            selected_device_id: self.selected_device_id.clone().unwrap_or_default(),
            instance_id: status
                .as_ref()
                .map(|s| s.instance_id.clone())
                .unwrap_or_default(),
            input_sample_rate: status.as_ref().map(|s| s.input_sample_rate).unwrap_or(0),
            input_channels: status.as_ref().map(|s| s.input_channels).unwrap_or(0),
            output_sample_rate: status.as_ref().map(|s| s.output_sample_rate).unwrap_or(0),
            output_channels: status.as_ref().map(|s| s.output_channels).unwrap_or(0),
            stream_id: status.as_ref().map(|s| s.stream_id).unwrap_or(0),
            format_generation: status.as_ref().map(|s| s.format_generation).unwrap_or(0),
            seek_generation: status.as_ref().map(|s| s.seek_generation).unwrap_or(0),
            last_error: if self.last_error.is_empty() {
                status
                    .as_ref()
                    .map(|s| s.last_error.clone())
                    .unwrap_or_default()
            } else {
                self.last_error.clone()
            },
        };
        serde_json::to_string(&state).unwrap_or_else(|_| "{}".to_string())
    }
}

fn build_output_stream_f32<F>(
    device: &Device,
    config: &StreamConfig,
    ring: Arc<AudioRingBuffer>,
    err_fn: F,
) -> Result<Stream, String>
where
    F: FnMut(cpal::StreamError) + Send + 'static,
{
    device
        .build_output_stream(
            config,
            move |data: &mut [f32], _| {
                ring.read_to(data);
            },
            err_fn,
            None,
        )
        .map_err(|e| e.to_string())
}

fn build_output_stream_i16<F>(
    device: &Device,
    config: &StreamConfig,
    ring: Arc<AudioRingBuffer>,
    err_fn: F,
) -> Result<Stream, String>
where
    F: FnMut(cpal::StreamError) + Send + 'static,
{
    device
        .build_output_stream(
            config,
            move |data: &mut [i16], _| {
                let mut temp = vec![0.0f32; data.len()];
                ring.read_to(&mut temp);
                for (dst, src) in data.iter_mut().zip(temp.iter()) {
                    *dst = (src.clamp(-1.0, 1.0) * i16::MAX as f32) as i16;
                }
            },
            err_fn,
            None,
        )
        .map_err(|e| e.to_string())
}

fn build_output_stream_u16<F>(
    device: &Device,
    config: &StreamConfig,
    ring: Arc<AudioRingBuffer>,
    err_fn: F,
) -> Result<Stream, String>
where
    F: FnMut(cpal::StreamError) + Send + 'static,
{
    device
        .build_output_stream(
            config,
            move |data: &mut [u16], _| {
                let mut temp = vec![0.0f32; data.len()];
                ring.read_to(&mut temp);
                for (dst, src) in data.iter_mut().zip(temp.iter()) {
                    *dst = ((src.clamp(-1.0, 1.0) * 0.5 + 0.5) * u16::MAX as f32) as u16;
                }
            },
            err_fn,
            None,
        )
        .map_err(|e| e.to_string())
}

fn select_output_device(host: &cpal::Host, name: Option<&str>) -> Result<Device, String> {
    if let Some(id) = name {
        if !id.is_empty() {
            let device_id = DeviceId::from_str(id).map_err(|e| e.to_string())?;
            if let Some(device) = host.device_by_id(&device_id) {
                return Ok(device);
            }
            return Err(format!("output device not found: {id}"));
        }
    }

    host.default_output_device()
        .ok_or_else(|| "no default output device".to_string())
}

#[derive(Serialize)]
struct DeviceInfo {
    id: String,
    name: String,
    is_default: bool,
}

#[derive(Serialize)]
struct EngineState {
    running: bool,
    selected_device_id: String,
    instance_id: String,
    input_sample_rate: u32,
    input_channels: u32,
    output_sample_rate: u32,
    output_channels: u32,
    stream_id: i64,
    format_generation: i32,
    seek_generation: i64,
    last_error: String,
}

static ENGINE: LazyLock<Mutex<AudioEngine>> = LazyLock::new(|| Mutex::new(AudioEngine::default()));

#[no_mangle]
pub extern "C" fn omnimix_audio_list_output_devices_json() -> *mut c_char {
    let result = list_devices_json().unwrap_or_else(|e| {
        serde_json::to_string(&serde_json::json!({ "error": e, "devices": [] }))
            .unwrap_or_else(|_| "{\"devices\":[]}".to_string())
    });
    string_to_ptr(result)
}

#[no_mangle]
pub extern "C" fn omnimix_audio_start(
    instance_id: *const c_char,
    device_id: *const c_char,
) -> c_int {
    let instance_id = match ptr_to_string(instance_id) {
        Some(v) if !v.is_empty() => v,
        _ => return -1,
    };
    let device_id = ptr_to_string(device_id).filter(|v| !v.is_empty());
    with_engine(|engine| engine.start(&instance_id, device_id))
}

#[no_mangle]
pub extern "C" fn omnimix_audio_stop() -> c_int {
    match ENGINE.lock() {
        Ok(mut engine) => {
            engine.stop();
            0
        }
        Err(_) => -1,
    }
}

#[no_mangle]
pub extern "C" fn omnimix_audio_set_device(device_id: *const c_char) -> c_int {
    let device_id = ptr_to_string(device_id).filter(|v| !v.is_empty());
    with_engine(|engine| engine.set_device(device_id))
}

#[no_mangle]
pub extern "C" fn omnimix_audio_get_state_json() -> *mut c_char {
    match ENGINE.lock() {
        Ok(engine) => string_to_ptr(engine.state_json()),
        Err(_) => string_to_ptr(
            "{\"running\":false,\"last_error\":\"audio engine lock poisoned\"}".to_string(),
        ),
    }
}

#[no_mangle]
pub extern "C" fn omnimix_audio_free_string(ptr: *mut c_char) {
    if !ptr.is_null() {
        unsafe {
            let _ = CString::from_raw(ptr);
        }
    }
}

fn with_engine<F>(f: F) -> c_int
where
    F: FnOnce(&mut AudioEngine) -> Result<(), String>,
{
    match ENGINE.lock() {
        Ok(mut engine) => match f(&mut engine) {
            Ok(()) => {
                engine.last_error.clear();
                0
            }
            Err(e) => {
                engine.last_error = e;
                -1
            }
        },
        Err(_) => -1,
    }
}

fn list_devices_json() -> Result<String, String> {
    let host = cpal::default_host();
    let default_id = host
        .default_output_device()
        .and_then(|d| d.id().ok())
        .map(|id| id.to_string());
    let mut devices = Vec::new();
    for device in host.output_devices().map_err(|e| e.to_string())? {
        let id = device
            .id()
            .map(|id| id.to_string())
            .unwrap_or_else(|_| String::new());
        let name = device
            .description()
            .map(|desc| desc.name().to_string())
            .unwrap_or_else(|_| "Unknown output device".to_string());
        devices.push(DeviceInfo {
            is_default: default_id.as_deref() == Some(id.as_str()),
            id,
            name,
        });
    }
    serde_json::to_string(&serde_json::json!({ "devices": devices })).map_err(|e| e.to_string())
}

fn ptr_to_string(ptr: *const c_char) -> Option<String> {
    if ptr.is_null() {
        return None;
    }
    unsafe { CStr::from_ptr(ptr).to_str().ok().map(|s| s.to_string()) }
}

fn string_to_ptr(value: String) -> *mut c_char {
    match CString::new(value) {
        Ok(s) => s.into_raw(),
        Err(_) => ptr::null_mut(),
    }
}
