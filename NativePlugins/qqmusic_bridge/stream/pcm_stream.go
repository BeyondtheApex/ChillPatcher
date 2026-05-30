package stream

import (
	"encoding/binary"
	"fmt"
	"io"
	"os"
	"os/exec"
	"path/filepath"
	"qqmusic_bridge/models"
	"sync"
	"sync/atomic"
	"syscall"
	"time"

	"github.com/hajimehoshi/go-mp3"
	"github.com/mewkiz/flac"
	"github.com/youpy/go-wav"
)

// pcmDebugLog writes debug message (uses shared debug function from cache.go)

// StreamManager manages PCM streams
type StreamManager struct {
	streams    map[int64]*PcmStream
	nextID     int64
	mu         sync.RWMutex
	cacheManager *CacheManager
}

// PcmStream represents a PCM audio stream
type PcmStream struct {
	id           int64
	songMid      string
	quality      string
	format       string
	info         models.PcmStreamInfo
	downloadTask *downloadTask

	// Decoder state
	decoder      interface{} // mp3.Decoder or flac.Stream
	file         *os.File

	// Stream state
	currentFrame uint64
	totalFrames  uint64
	sampleRate   int
	channels     int

	// Buffer for interleaved samples
	sampleBuffer []float32
	bufferPos    int

	// Status
	ready        int32 // atomic
	closed       int32 // atomic
	err          error
	mu           sync.Mutex

	// For seeking
	canSeek       bool
	pendingSeek   int64 // -1 if no pending seek
	wavDataOffset int64 // Offset to WAV data section (after header)
}

// NewStreamManager creates a new stream manager
func NewStreamManager(cacheManager *CacheManager) *StreamManager {
	return &StreamManager{
		streams:      make(map[int64]*PcmStream),
		cacheManager: cacheManager,
	}
}

// CreateStream creates a new PCM stream for a song
func (sm *StreamManager) CreateStream(songMid, quality, url, format string, duration float64) (int64, error) {
	debugLog("[CreateStream] songMid=%s, quality=%s, format=%s, duration=%.2f", songMid, quality, format, duration)
	debugLog("[CreateStream] URL: %s", url)

	sm.mu.Lock()
	sm.nextID++
	streamID := sm.nextID
	sm.mu.Unlock()

	stream := &PcmStream{
		id:          streamID,
		songMid:     songMid,
		quality:     quality,
		format:      format,
		pendingSeek: -1,
		sampleRate:  44100, // Default, will be updated by decoder
		channels:    2,     // Default stereo
	}

	// Estimate total frames from duration
	if duration > 0 {
		stream.totalFrames = uint64(duration * float64(stream.sampleRate))
	}

	// Start download or get cached file
	cachedPath := sm.cacheManager.GetCachedFile(songMid, quality)
	if cachedPath != "" {
		debugLog("[CreateStream] Using cached file: %s", cachedPath)
		// File is cached, open directly
		if err := stream.openCachedFile(cachedPath); err != nil {
			debugLog("[CreateStream] Failed to open cached file: %v", err)
			return 0, err
		}
	} else {
		debugLog("[CreateStream] Starting download...")
		// Start download
		task, err := sm.cacheManager.StartDownload(songMid, quality, url, format)
		if err != nil {
			debugLog("[CreateStream] StartDownload failed: %v", err)
			return 0, err
		}
		stream.downloadTask = task

		// Wait for minimum buffer in background
		go stream.waitAndInitialize()
	}

	sm.mu.Lock()
	sm.streams[streamID] = stream
	sm.mu.Unlock()

	debugLog("[CreateStream] Created stream ID=%d", streamID)
	return streamID, nil
}

// GetStream returns a stream by ID
func (sm *StreamManager) GetStream(streamID int64) *PcmStream {
	sm.mu.RLock()
	defer sm.mu.RUnlock()
	return sm.streams[streamID]
}

// CloseStream closes and removes a stream
func (sm *StreamManager) CloseStream(streamID int64) {
	sm.mu.Lock()
	stream, ok := sm.streams[streamID]
	if ok {
		delete(sm.streams, streamID)
	}
	sm.mu.Unlock()

	if ok && stream != nil {
		stream.Close()
	}
}

// PcmStream methods

func (s *PcmStream) openCachedFile(path string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	file, err := os.Open(path)
	if err != nil {
		s.err = err
		return err
	}
	s.file = file
	s.canSeek = true

	// Detect actual format from file extension (may differ from requested quality)
	ext := filepath.Ext(path)
	// Handle .tmp extension
	if ext == ".tmp" {
		// Get the real extension before .tmp
		basePath := path[:len(path)-4] // remove .tmp
		ext = filepath.Ext(basePath)
	}
	switch ext {
	case ".mp3":
		s.format = "mp3"
	case ".flac":
		s.format = "flac"
	case ".m4a":
		s.format = "m4a"
	case ".wav":
		s.format = "wav"
	}
	debugLog("[openCachedFile] Detected format from path: %s -> %s", path, s.format)

	if err := s.initDecoder(); err != nil {
		file.Close()
		s.err = err
		return err
	}

	atomic.StoreInt32(&s.ready, 1)
	return nil
}

func (s *PcmStream) waitAndInitialize() {
	debugLog("[waitAndInitialize] Starting for songMid=%s", s.songMid)

	if s.downloadTask == nil {
		debugLog("[waitAndInitialize] No download task")
		return
	}

	// For M4A/AAC formats, we need to wait for complete download before ffmpeg conversion
	if s.format == "m4a" || s.format == "aac" {
		debugLog("[waitAndInitialize] M4A/AAC format detected, waiting for complete download...")
		// Wait for download to complete (timeout 5 minutes for large files)
		timeout := 5 * time.Minute
		startTime := time.Now()
		for !s.downloadTask.IsComplete() {
			if time.Since(startTime) > timeout {
				debugLog("[waitAndInitialize] Timeout waiting for download to complete! Downloaded=%d, Error=%v",
					s.downloadTask.GetDownloaded(), s.downloadTask.GetError())
				s.mu.Lock()
				s.err = fmt.Errorf("timeout waiting for download to complete")
				s.mu.Unlock()
				return
			}
			if s.downloadTask.GetError() != nil {
				debugLog("[waitAndInitialize] Download error: %v", s.downloadTask.GetError())
				s.mu.Lock()
				s.err = s.downloadTask.GetError()
				s.mu.Unlock()
				return
			}
			time.Sleep(100 * time.Millisecond)
		}
		debugLog("[waitAndInitialize] Download complete, total=%d bytes", s.downloadTask.GetDownloaded())
	} else {
		// For MP3/FLAC, wait for minimum buffer
		debugLog("[waitAndInitialize] Waiting for minimum buffer (30s timeout)...")
		if !s.downloadTask.WaitForMinBuffer(30 * time.Second) {
			debugLog("[waitAndInitialize] Timeout waiting for buffer! Downloaded=%d, Error=%v",
				s.downloadTask.GetDownloaded(), s.downloadTask.GetError())
			s.mu.Lock()
			s.err = fmt.Errorf("timeout waiting for buffer")
			s.mu.Unlock()
			return
		}
		debugLog("[waitAndInitialize] Buffer ready, downloaded=%d bytes", s.downloadTask.GetDownloaded())
	}

	s.mu.Lock()
	defer s.mu.Unlock()

	// Open file for reading
	debugLog("[waitAndInitialize] Opening file for reading...")
	file, err := s.downloadTask.OpenForReading()
	if err != nil {
		debugLog("[waitAndInitialize] Failed to open file: %v", err)
		s.err = err
		return
	}
	s.file = file
	s.canSeek = s.downloadTask.IsComplete()

	debugLog("[waitAndInitialize] Initializing decoder for format=%s", s.format)
	if err := s.initDecoder(); err != nil {
		debugLog("[waitAndInitialize] Failed to init decoder: %v", err)
		file.Close()
		s.err = err
		return
	}

	atomic.StoreInt32(&s.ready, 1)
	debugLog("[waitAndInitialize] Stream is now ready")
}

func (s *PcmStream) initDecoder() error {
	switch s.format {
	case "mp3":
		return s.initMP3Decoder()
	case "flac":
		return s.initFLACDecoder()
	case "m4a", "aac":
		return s.initAACDecoder()
	case "wav":
		return s.initWAVDecoder()
	default:
		return fmt.Errorf("unsupported format: %s", s.format)
	}
}

func (s *PcmStream) initWAVDecoder() error {
	debugLog("[initWAVDecoder] Opening WAV file")

	wavReader := wav.NewReader(s.file)
	format, err := wavReader.Format()
	if err != nil {
		return fmt.Errorf("failed to read WAV format: %w", err)
	}

	s.decoder = wavReader
	s.sampleRate = int(format.SampleRate)
	s.channels = int(format.NumChannels)

	// Get current file position (this is the start of WAV data after header)
	dataOffset, _ := s.file.Seek(0, io.SeekCurrent)
	s.wavDataOffset = dataOffset

	// Calculate total frames
	duration, _ := wavReader.Duration()
	s.totalFrames = uint64(duration.Seconds() * float64(s.sampleRate))

	debugLog("[initWAVDecoder] WAV SampleRate=%d, Channels=%d, TotalFrames=%d, DataOffset=%d", s.sampleRate, s.channels, s.totalFrames, s.wavDataOffset)

	s.info = models.PcmStreamInfo{
		SampleRate:  s.sampleRate,
		Channels:    s.channels,
		TotalFrames: s.totalFrames,
		Format:      "wav",
		CanSeek:     true,
	}

	return nil
}

func (s *PcmStream) initMP3Decoder() error {
	decoder, err := mp3.NewDecoder(s.file)
	if err != nil {
		return fmt.Errorf("failed to create MP3 decoder: %w", err)
	}

	s.decoder = decoder
	s.sampleRate = decoder.SampleRate()
	s.channels = 2 // go-mp3 always outputs stereo

	// Calculate total frames
	length := decoder.Length()
	if length > 0 {
		// Length is in bytes, 4 bytes per frame (2 channels * 2 bytes per sample)
		s.totalFrames = uint64(length / 4)
	}

	s.info = models.PcmStreamInfo{
		SampleRate:  s.sampleRate,
		Channels:    s.channels,
		TotalFrames: s.totalFrames,
		Format:      "mp3",
		CanSeek:     s.canSeek,
	}

	return nil
}

func (s *PcmStream) initFLACDecoder() error {
	stream, err := flac.New(s.file)
	if err != nil {
		return fmt.Errorf("failed to create FLAC decoder: %w", err)
	}

	s.decoder = stream
	s.sampleRate = int(stream.Info.SampleRate)
	s.channels = int(stream.Info.NChannels)
	s.totalFrames = stream.Info.NSamples

	s.info = models.PcmStreamInfo{
		SampleRate:  s.sampleRate,
		Channels:    s.channels,
		TotalFrames: s.totalFrames,
		Format:      "flac",
		CanSeek:     s.canSeek,
	}

	return nil
}

func (s *PcmStream) initAACDecoder() error {
	debugLog("[initAACDecoder] Creating AAC decoder for format=%s", s.format)

	// Close the current file first
	filePath := s.file.Name()
	s.file.Close()

	// Convert M4A to WAV using ffmpeg
	wavPath := filePath + ".wav"
	debugLog("[initAACDecoder] Converting %s to WAV: %s", filePath, wavPath)

	// Try to find ffmpeg in multiple locations
	cacheDir := filepath.Dir(filePath)
	ffmpegPaths := []string{
		"ffmpeg",     // system PATH
		"ffmpeg.exe", // system PATH (Windows)
		filepath.Join(cacheDir, "ffmpeg.exe"),                                                  // audio_cache dir
		filepath.Join(cacheDir, "..", "..", "..", "..", "..", "..", "ffmpeg.exe"),                // game root (from audio_cache)
		filepath.Join(cacheDir, "..", "..", "..", "..", "..", "..", "..", "ffmpeg.exe"),           // game root (alternative depth)
	}

	// Also try to find via executable path
	if exePath, err := os.Executable(); err == nil {
		exeDir := filepath.Dir(exePath)
		ffmpegPaths = append(ffmpegPaths,
			filepath.Join(exeDir, "ffmpeg.exe"),          // same dir as exe
			filepath.Join(exeDir, "..", "ffmpeg.exe"),     // parent of exe dir
			filepath.Join(exeDir, "..", "..", "ffmpeg.exe"), // grandparent of exe dir
		)
	}

	var ffmpegPath string
	for _, p := range ffmpegPaths {
		// For absolute paths, check if file exists directly
		if filepath.IsAbs(p) {
			if _, err := os.Stat(p); err == nil {
				ffmpegPath = p
				debugLog("[initAACDecoder] Found ffmpeg at: %s", p)
				break
			}
		} else {
			// For relative paths, use LookPath (searches PATH)
			if found, err := exec.LookPath(p); err == nil {
				ffmpegPath = found
				debugLog("[initAACDecoder] Found ffmpeg via PATH: %s", found)
				break
			}
		}
	}

	if ffmpegPath == "" {
		debugLog("[initAACDecoder] ffmpeg not found in any location, tried: %v", ffmpegPaths)
		return fmt.Errorf("ffmpeg not found. Please install ffmpeg and add it to PATH, or place ffmpeg.exe in the game root directory")
	}

	// Run ffmpeg to convert M4A to WAV (hide console window on Windows)
	cmd := exec.Command(ffmpegPath, "-i", filePath, "-f", "wav", "-acodec", "pcm_s16le", "-y", wavPath)
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	output, err := cmd.CombinedOutput()
	if err != nil {
		debugLog("[initAACDecoder] ffmpeg error: %v, output: %s", err, string(output))
		return fmt.Errorf("failed to convert M4A to WAV: %w (output: %s)", err, string(output))
	}

	debugLog("[initAACDecoder] ffmpeg conversion successful")

	// Open WAV file
	wavFile, err := os.Open(wavPath)
	if err != nil {
		return fmt.Errorf("failed to open WAV file: %w", err)
	}
	s.file = wavFile

	// Parse WAV header
	wavReader := wav.NewReader(wavFile)
	format, err := wavReader.Format()
	if err != nil {
		wavFile.Close()
		return fmt.Errorf("failed to read WAV format: %w", err)
	}

	s.decoder = wavReader
	s.sampleRate = int(format.SampleRate)
	s.channels = int(format.NumChannels)

	// Get current file position (this is the start of WAV data after header)
	dataOffset, _ := wavFile.Seek(0, io.SeekCurrent)
	s.wavDataOffset = dataOffset

	// Calculate total frames
	duration, _ := wavReader.Duration()
	s.totalFrames = uint64(duration.Seconds() * float64(s.sampleRate))

	debugLog("[initAACDecoder] WAV SampleRate=%d, Channels=%d, TotalFrames=%d, DataOffset=%d", s.sampleRate, s.channels, s.totalFrames, s.wavDataOffset)

	// Update format to wav for decodeChunk routing
	s.format = "wav"
	// WAV file is fully converted, so seeking is now available
	s.canSeek = true

	s.info = models.PcmStreamInfo{
		SampleRate:  s.sampleRate,
		Channels:    s.channels,
		TotalFrames: s.totalFrames,
		Format:      "wav", // Converted to WAV
		CanSeek:     true,
	}

	return nil
}

// IsReady returns whether the stream is ready to read
func (s *PcmStream) IsReady() bool {
	return atomic.LoadInt32(&s.ready) == 1
}

// IsClosed returns whether the stream is closed
func (s *PcmStream) IsClosed() bool {
	return atomic.LoadInt32(&s.closed) == 1
}

// GetInfo returns stream information
func (s *PcmStream) GetInfo() models.PcmStreamInfo {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.info
}

// GetCurrentFrame returns the current frame position
func (s *PcmStream) GetCurrentFrame() uint64 {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.currentFrame
}

// GetCacheProgress returns the download progress (0.0 - 1.0), -1 if cached
func (s *PcmStream) GetCacheProgress() float64 {
	if s.downloadTask == nil {
		return 1.0 // Fully cached
	}
	if s.downloadTask.IsComplete() {
		return 1.0
	}
	return s.downloadTask.GetProgress()
}

// IsCacheComplete returns whether the cache download is complete
func (s *PcmStream) IsCacheComplete() bool {
	if s.downloadTask == nil {
		return true
	}
	return s.downloadTask.IsComplete()
}

// ReadFrames reads PCM frames into the buffer
// Returns number of frames read, or negative on error
func (s *PcmStream) ReadFrames(buffer []float32, framesToRead int) int {
	if !s.IsReady() {
		return -1
	}

	s.mu.Lock()
	defer s.mu.Unlock()

	if s.err != nil {
		return -2
	}

	samplesNeeded := framesToRead * s.channels
	samplesRead := 0

	for samplesRead < samplesNeeded {
		// Use buffered samples first
		if s.bufferPos < len(s.sampleBuffer) {
			toCopy := len(s.sampleBuffer) - s.bufferPos
			if toCopy > samplesNeeded-samplesRead {
				toCopy = samplesNeeded - samplesRead
			}
			copy(buffer[samplesRead:], s.sampleBuffer[s.bufferPos:s.bufferPos+toCopy])
			s.bufferPos += toCopy
			samplesRead += toCopy
			continue
		}

		// Decode more samples
		newSamples, eof := s.decodeChunk()
		if len(newSamples) == 0 {
			if eof {
				break // End of stream
			}
			// May need to wait for more data
			if s.downloadTask != nil && !s.downloadTask.IsComplete() {
				// Wait a bit for more data
				s.mu.Unlock()
				time.Sleep(10 * time.Millisecond)
				s.mu.Lock()
				continue
			}
			break
		}

		s.sampleBuffer = newSamples
		s.bufferPos = 0
	}

	framesRead := samplesRead / s.channels
	s.currentFrame += uint64(framesRead)
	return framesRead
}

func (s *PcmStream) decodeChunk() ([]float32, bool) {
	switch s.format {
	case "mp3":
		return s.decodeMP3Chunk()
	case "flac":
		return s.decodeFLACChunk()
	case "m4a", "aac", "wav":
		return s.decodeWAVChunk()
	default:
		return nil, true
	}
}

func (s *PcmStream) decodeMP3Chunk() ([]float32, bool) {
	decoder, ok := s.decoder.(*mp3.Decoder)
	if !ok {
		return nil, true
	}

	// Read 4096 bytes (1024 stereo frames)
	buf := make([]byte, 4096)
	n, err := decoder.Read(buf)
	if err != nil && err != io.EOF {
		s.err = err
		return nil, true
	}

	if n == 0 {
		return nil, err == io.EOF
	}

	// Convert int16 samples to float32
	samples := make([]float32, n/2)
	for i := 0; i < n/2; i++ {
		sample := int16(binary.LittleEndian.Uint16(buf[i*2 : i*2+2]))
		samples[i] = float32(sample) / 32768.0
	}

	return samples, err == io.EOF
}

func (s *PcmStream) decodeFLACChunk() ([]float32, bool) {
	stream, ok := s.decoder.(*flac.Stream)
	if !ok {
		return nil, true
	}

	frame, err := stream.ParseNext()
	if err != nil {
		if err == io.EOF {
			return nil, true
		}
		s.err = err
		return nil, true
	}

	// Convert subframes to interleaved float32
	nSamples := len(frame.Subframes[0].Samples)
	nChannels := len(frame.Subframes)
	samples := make([]float32, nSamples*nChannels)

	bps := int(frame.BitsPerSample)
	maxVal := float32(int(1) << (bps - 1))

	for i := 0; i < nSamples; i++ {
		for ch := 0; ch < nChannels; ch++ {
			sample := frame.Subframes[ch].Samples[i]
			samples[i*nChannels+ch] = float32(sample) / maxVal
		}
	}

	return samples, false
}

func (s *PcmStream) decodeWAVChunk() ([]float32, bool) {
	// Check if we're in direct file reading mode (after seek)
	if s.decoder == nil {
		return s.decodeWAVChunkDirect()
	}

	reader, ok := s.decoder.(*wav.Reader)
	if !ok {
		return nil, true
	}

	// Read 1024 samples
	samples, err := reader.ReadSamples(1024)
	if err != nil && err != io.EOF {
		s.err = err
		return nil, true
	}

	if len(samples) == 0 {
		return nil, err == io.EOF
	}

	// Convert to float32
	result := make([]float32, len(samples)*s.channels)
	for i, sample := range samples {
		for ch := 0; ch < s.channels; ch++ {
			result[i*s.channels+ch] = float32(reader.IntValue(sample, uint(ch))) / 32768.0
		}
	}

	return result, err == io.EOF
}

func (s *PcmStream) decodeWAVChunkDirect() ([]float32, bool) {
	if s.file == nil {
		return nil, true
	}

	// Read 1024 frames of raw PCM data (16-bit)
	framesToRead := 1024
	bytesPerFrame := s.channels * 2
	buf := make([]byte, framesToRead*bytesPerFrame)

	n, err := s.file.Read(buf)
	if err != nil && err != io.EOF {
		s.err = err
		return nil, true
	}

	if n == 0 {
		return nil, err == io.EOF
	}

	// Convert int16 samples to float32
	samplesRead := n / 2
	result := make([]float32, samplesRead)
	for i := 0; i < samplesRead; i++ {
		sample := int16(binary.LittleEndian.Uint16(buf[i*2 : i*2+2]))
		result[i] = float32(sample) / 32768.0
	}

	return result, err == io.EOF
}

// Seek seeks to a specific frame position
func (s *PcmStream) Seek(frameIndex uint64) bool {
	s.mu.Lock()
	defer s.mu.Unlock()

	if !s.canSeek {
		// Set pending seek
		s.pendingSeek = int64(frameIndex)
		return false
	}

	return s.doSeek(frameIndex)
}

func (s *PcmStream) doSeek(frameIndex uint64) bool {
	// Clear buffer
	s.sampleBuffer = nil
	s.bufferPos = 0

	switch s.format {
	case "mp3":
		return s.seekMP3(frameIndex)
	case "flac":
		return s.seekFLAC(frameIndex)
	case "wav", "m4a":
		return s.seekWAV(frameIndex)
	default:
		return false
	}
}

func (s *PcmStream) seekMP3(frameIndex uint64) bool {
	decoder, ok := s.decoder.(*mp3.Decoder)
	if !ok {
		return false
	}

	// go-mp3 decoder implements io.Seeker
	// Position is in bytes: frame * channels * 2 bytes per sample
	bytePos := int64(frameIndex) * int64(s.channels) * 2

	if seeker, ok := interface{}(decoder).(io.Seeker); ok {
		_, err := seeker.Seek(bytePos, io.SeekStart)
		if err != nil {
			return false
		}
		s.currentFrame = frameIndex
		return true
	}

	return false
}

func (s *PcmStream) seekFLAC(frameIndex uint64) bool {
	stream, ok := s.decoder.(*flac.Stream)
	if !ok {
		return false
	}

	// flac.Stream supports seeking
	_, err := stream.Seek(frameIndex)
	if err != nil {
		return false
	}
	s.currentFrame = frameIndex
	return true
}

func (s *PcmStream) seekWAV(frameIndex uint64) bool {
	if s.file == nil {
		return false
	}

	// Each frame = channels * 2 bytes (16-bit PCM)
	bytesPerFrame := int64(s.channels) * 2
	bytePos := s.wavDataOffset + int64(frameIndex)*bytesPerFrame

	_, err := s.file.Seek(bytePos, io.SeekStart)
	if err != nil {
		return false
	}

	// Set decoder to nil to indicate direct file reading mode
	s.decoder = nil
	s.currentFrame = frameIndex
	return true
}

// HasPendingSeek returns whether there's a pending seek
func (s *PcmStream) HasPendingSeek() bool {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.pendingSeek >= 0
}

// GetPendingSeek returns the pending seek frame, or -1 if none
func (s *PcmStream) GetPendingSeek() int64 {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.pendingSeek
}

// CancelPendingSeek cancels a pending seek
func (s *PcmStream) CancelPendingSeek() {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.pendingSeek = -1
}

// TryCompletePendingSeek attempts to complete a pending seek if possible
func (s *PcmStream) TryCompletePendingSeek() bool {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.pendingSeek < 0 {
		return false
	}

	// Check if seek is now possible
	if s.downloadTask != nil && s.downloadTask.IsComplete() {
		s.canSeek = true
	}

	if !s.canSeek {
		return false
	}

	frameIndex := uint64(s.pendingSeek)
	s.pendingSeek = -1
	return s.doSeek(frameIndex)
}

// GetError returns any error
func (s *PcmStream) GetError() error {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.err
}

// Close closes the stream
func (s *PcmStream) Close() {
	if !atomic.CompareAndSwapInt32(&s.closed, 0, 1) {
		return
	}

	s.mu.Lock()
	defer s.mu.Unlock()

	// Close FLAC stream if applicable
	if stream, ok := s.decoder.(*flac.Stream); ok {
		stream.Close()
	}

	if s.file != nil {
		s.file.Close()
		s.file = nil
	}
}
