package main

import (
	"io"
	"os"
	"sync"
	"time"

	"github.com/mewkiz/flac"
)

// FlacStreamingDecoder FLAC 流式解码器（边下边播）
// 与 MP3 StreamingDecoder 不同，FLAC 需要先读取头部元数据才能开始解码
// 因此我们采用"先缓存再解码"的策略
type FlacStreamingDecoder struct {
	cachePath       string
	stream          *flac.Stream
	mutex           sync.Mutex
	buffer          []float32 // 已解码的 PCM 数据
	sampleRate      int
	channels        int
	totalFrames     uint64
	isReady         bool
	isEOF           bool
	lastError       string
	currentPos      uint64 // 当前样本位置
	prefillStarted  bool   // 是否已启动预填充
	isClosed        bool   // 是否已关闭
	stopChan        chan struct{} // 停止信号
	bitsPerSample   int    // 位深度
	isCacheComplete func() bool // 检查缓存是否下载完成的回调
}

// NewFlacStreamingDecoder 创建 FLAC 流式解码器
// cachePath 是本地缓存文件路径
// isCacheComplete 是检查缓存是否下载完成的回调函数
func NewFlacStreamingDecoder(cachePath string, isCacheComplete func() bool) *FlacStreamingDecoder {
	return &FlacStreamingDecoder{
		cachePath:       cachePath,
		stopChan:        make(chan struct{}),
		isCacheComplete: isCacheComplete,
	}
}

// TryOpen 尝试打开 FLAC 文件
// 返回 true 如果成功打开，false 如果文件还不完整
func (d *FlacStreamingDecoder) TryOpen() bool {
	d.mutex.Lock()
	defer d.mutex.Unlock()

	if d.stream != nil {
		return true // 已经打开
	}

	// 检查文件是否存在且有足够大小
	info, err := os.Stat(d.cachePath)
	if err != nil || info.Size() < 1024 {
		return false // 文件太小，等待更多数据
	}

	// 尝试打开 FLAC 文件
	stream, err := flac.Open(d.cachePath)
	if err != nil {
		// FLAC 头部可能还没下载完
		return false
	}

	d.stream = stream
	d.sampleRate = int(stream.Info.SampleRate)
	d.channels = int(stream.Info.NChannels)
	d.totalFrames = stream.Info.NSamples
	d.bitsPerSample = int(stream.Info.BitsPerSample)
	
	// 启动后台预填充协程
	if !d.prefillStarted {
		d.prefillStarted = true
		go d.prefillBuffer()
	}

	return true
}

// prefillBuffer 后台预填充缓冲区，达到一定数据量后设置 isReady
func (d *FlacStreamingDecoder) prefillBuffer() {
	scale := 1.0 / float64(int(1)<<(d.bitsPerSample-1))
	
	// 目标：预填充约 0.5 秒的数据（sampleRate * channels * 0.5）
	targetSamples := d.sampleRate * d.channels / 2
	if targetSamples < 44100 {
		targetSamples = 44100 // 至少 44100 样本
	}
	
	for {
		// 检查是否已关闭
		select {
		case <-d.stopChan:
			return
		default:
		}
		
		d.mutex.Lock()
		if d.isClosed {
			d.mutex.Unlock()
			return
		}
		
		bufLen := len(d.buffer)
		isEOF := d.isEOF
		
		// 如果已经够了或者 EOF 了，设置 ready 并退出
		if bufLen >= targetSamples || isEOF {
			if !d.isReady {
				d.isReady = true
			}
			d.mutex.Unlock()
			return
		}
		
		// 如果 stream 为空，尝试重新打开
		if d.stream == nil {
			d.mutex.Unlock()
			time.Sleep(100 * time.Millisecond)
			d.tryReopen()
			continue
		}
		
		// 解码一帧
		frame, err := d.stream.ParseNext()
		if err != nil {
			// 检查缓存是否已下载完成
			cacheComplete := d.isCacheComplete != nil && d.isCacheComplete()
			
			if cacheComplete {
				// 文件已下载完，这是真正的 EOF
				if err == io.EOF {
					d.isEOF = true
				} else {
					d.lastError = err.Error()
					d.isEOF = true
				}
				if !d.isReady {
					d.isReady = true
				}
				d.mutex.Unlock()
				return
			}
			
			// 文件还在下载，等待更多数据
			d.stream.Close()
			d.stream = nil
			d.mutex.Unlock()
			time.Sleep(100 * time.Millisecond)
			continue
		}
		
		// 成功解码，将样本转换为 float32 并交错存入缓冲区
		nSamples := len(frame.Subframes[0].Samples)
		for i := 0; i < nSamples; i++ {
			for ch := 0; ch < d.channels; ch++ {
				if ch < len(frame.Subframes) {
					sample := float32(float64(frame.Subframes[ch].Samples[i]) * scale)
					d.buffer = append(d.buffer, sample)
				}
			}
		}
		d.currentPos += uint64(nSamples)
		d.mutex.Unlock()
		
		// 给其他协程一些时间
		time.Sleep(1 * time.Millisecond)
	}
}

// tryReopen 尝试重新打开 FLAC 文件
func (d *FlacStreamingDecoder) tryReopen() {
	d.mutex.Lock()
	defer d.mutex.Unlock()
	
	if d.isClosed || d.stream != nil {
		return
	}
	
	stream, err := flac.Open(d.cachePath)
	if err != nil {
		return // 文件还没准备好，下次再试
	}
	
	d.stream = stream
	
	// 跳过已解码的帧
	skippedFrames := uint64(0)
	for skippedFrames < d.currentPos {
		frame, err := d.stream.ParseNext()
		if err != nil {
			// 跳过过程中遇到错误，关闭等下次重试
			d.stream.Close()
			d.stream = nil
			return
		}
		skippedFrames += uint64(len(frame.Subframes[0].Samples))
	}
}

// GetInfo 获取音频信息
func (d *FlacStreamingDecoder) GetInfo() (sampleRate, channels int, isReady bool, errStr string) {
	d.mutex.Lock()
	defer d.mutex.Unlock()

	return d.sampleRate, d.channels, d.isReady, d.lastError
}

// Read 读取 PCM 数据
// 返回: 读取的帧数，0=暂无数据，-1=错误，-2=EOF
func (d *FlacStreamingDecoder) Read(buffer []float32, framesToRead int) int {
	d.mutex.Lock()
	defer d.mutex.Unlock()

	if d.isClosed {
		return -2 // 已关闭
	}
	
	// 如果已经 EOF 且缓冲区为空，返回 EOF
	if d.isEOF && len(d.buffer) == 0 {
		return -2 // EOF
	}

	totalSamples := framesToRead * d.channels
	samplesRead := 0
	scale := 1.0 / float64(int(1)<<(d.bitsPerSample-1))

	for samplesRead < totalSamples {
		// 首先从缓冲区读取
		if len(d.buffer) > 0 {
			toCopy := len(d.buffer)
			if toCopy > totalSamples-samplesRead {
				toCopy = totalSamples - samplesRead
			}
			copy(buffer[samplesRead:], d.buffer[:toCopy])
			d.buffer = d.buffer[toCopy:]
			samplesRead += toCopy
			continue
		}
		
		// 缓冲区已空
		// 如果不是 ready 状态或 stream 为空，等待 prefillBuffer
		if !d.isReady || d.stream == nil {
			break
		}

		// 解码下一帧
		frame, err := d.stream.ParseNext()
		if err != nil {
			// 检查缓存是否已下载完成
			cacheComplete := d.isCacheComplete != nil && d.isCacheComplete()
			
			if cacheComplete {
				// 文件已下载完，这是真正的 EOF
				if err == io.EOF {
					d.isEOF = true
				} else {
					d.lastError = err.Error()
					d.isEOF = true
				}
			} else {
				// 文件还在下载，关闭 stream 等待 prefillBuffer 重新打开
				d.stream.Close()
				d.stream = nil
			}
			break
		}

		// 将样本转换为 float32 并交错
		nSamples := len(frame.Subframes[0].Samples)
		for i := 0; i < nSamples; i++ {
			for ch := 0; ch < d.channels; ch++ {
				if ch < len(frame.Subframes) {
					sample := float32(float64(frame.Subframes[ch].Samples[i]) * scale)
					if samplesRead < totalSamples {
						buffer[samplesRead] = sample
						samplesRead++
					} else {
						d.buffer = append(d.buffer, sample)
					}
				}
			}
		}
		d.currentPos += uint64(nSamples)
	}

	framesRead := samplesRead / d.channels
	
	// 第一次成功读取到数据时设置 isReady
	if framesRead > 0 && !d.isReady {
		d.isReady = true
	}
	
	// 如果没有读取到任何数据且已经 EOF，返回 EOF
	if framesRead == 0 && d.isEOF {
		if !d.isReady {
			d.isReady = true
		}
		return -2 // EOF
	}
	
	return framesRead
}

// IsEOF 是否结束
func (d *FlacStreamingDecoder) IsEOF() bool {
	d.mutex.Lock()
	defer d.mutex.Unlock()
	return d.isEOF
}

// Close 关闭解码器
func (d *FlacStreamingDecoder) Close() {
	// 先设置关闭标志（在锁外，因为 stopChan 可能会阻塞等待锁）
	select {
	case <-d.stopChan:
		// 已经关闭
	default:
		close(d.stopChan)
	}
	
	d.mutex.Lock()
	defer d.mutex.Unlock()

	d.isClosed = true
	
	if d.stream != nil {
		d.stream.Close()
		d.stream = nil
	}
}

// FlacSeekableDecoder 可 Seek 的 FLAC 解码器
type FlacSeekableDecoder struct {
	file        *os.File
	stream      *flac.Stream
	mutex       sync.Mutex
	sampleRate  int
	channels    int
	totalFrames uint64
	currentPos  uint64
	isReady     bool
	isEOF       bool   // 流是否已结束
	lastError   string
	buffer      []float32 // 解码缓冲区
	bufferStart uint64    // 缓冲区起始位置（样本）
}

// NewFlacSeekableDecoder 从缓存文件创建可 Seek 的 FLAC 解码器
func NewFlacSeekableDecoder(cachePath string) (*FlacSeekableDecoder, error) {
	file, err := os.Open(cachePath)
	if err != nil {
		return nil, err
	}

	// 使用 NewSeek 创建支持 Seek 的 Stream
	stream, err := flac.NewSeek(file)
	if err != nil {
		file.Close()
		return nil, err
	}

	d := &FlacSeekableDecoder{
		file:        file,
		stream:      stream,
		sampleRate:  int(stream.Info.SampleRate),
		channels:    int(stream.Info.NChannels),
		totalFrames: stream.Info.NSamples,
		isReady:     true,
	}

	return d, nil
}

// GetInfo 获取音频信息
func (d *FlacSeekableDecoder) GetInfo() (sampleRate, channels int, totalFrames uint64) {
	return d.sampleRate, d.channels, d.totalFrames
}

// IsReady 是否准备好
func (d *FlacSeekableDecoder) IsReady() bool {
	return d.isReady
}

// Seek 定位到指定样本
func (d *FlacSeekableDecoder) Seek(sampleIndex uint64) error {
	d.mutex.Lock()
	defer d.mutex.Unlock()

	actualPos, err := d.stream.Seek(sampleIndex)
	if err != nil {
		d.lastError = err.Error()
		return err
	}

	d.currentPos = actualPos
	d.buffer = nil // 清空缓冲区
	d.bufferStart = actualPos
	d.isEOF = false // 重置 EOF 标志

	return nil
}

// ReadFrames 读取 PCM 帧
// 返回: 读取的帧数，0=暂无数据，-1=错误，-2=EOF
func (d *FlacSeekableDecoder) ReadFrames(buffer []float32, framesToRead int) int {
	d.mutex.Lock()
	defer d.mutex.Unlock()

	if !d.isReady {
		return 0
	}
	
	// 如果已经 EOF 且缓冲区为空，返回 EOF
	if d.isEOF && len(d.buffer) == 0 {
		return -2
	}

	totalSamples := framesToRead * d.channels
	samplesRead := 0
	bitsPerSample := int(d.stream.Info.BitsPerSample)
	scale := 1.0 / float64(int(1)<<(bitsPerSample-1))

	for samplesRead < totalSamples {
		// 首先从缓冲区读取
		if len(d.buffer) > 0 {
			toCopy := len(d.buffer)
			if toCopy > totalSamples-samplesRead {
				toCopy = totalSamples - samplesRead
			}
			copy(buffer[samplesRead:], d.buffer[:toCopy])
			d.buffer = d.buffer[toCopy:]
			samplesRead += toCopy
			continue
		}
		
		// 如果已经 EOF，不再尝试解码
		if d.isEOF {
			break
		}

		// 解码下一帧
		frame, err := d.stream.ParseNext()
		if err != nil {
			if err == io.EOF {
				d.isEOF = true
				break // 继续处理已读取的数据
			}
			d.lastError = err.Error()
			d.isEOF = true // 错误也设置 EOF，确保 C# 能检测到流结束
			return -1 // Error
		}

		// 将样本转换为 float32 并交错
		nSamples := len(frame.Subframes[0].Samples)
		for i := 0; i < nSamples; i++ {
			for ch := 0; ch < d.channels; ch++ {
				if ch < len(frame.Subframes) {
					sample := float32(float64(frame.Subframes[ch].Samples[i]) * scale)
					if samplesRead < totalSamples {
						buffer[samplesRead] = sample
						samplesRead++
					} else {
						d.buffer = append(d.buffer, sample)
					}
				}
			}
		}
		d.currentPos += uint64(nSamples)
	}

	framesRead := samplesRead / d.channels
	
	// 如果没有读取到数据且已经 EOF，返回 EOF
	if framesRead == 0 && d.isEOF {
		return -2
	}
	
	return framesRead
}

// IsEOF 是否结束
func (d *FlacSeekableDecoder) IsEOF() bool {
	d.mutex.Lock()
	defer d.mutex.Unlock()
	return d.isEOF
}

// Close 关闭解码器
func (d *FlacSeekableDecoder) Close() {
	d.mutex.Lock()
	defer d.mutex.Unlock()

	if d.stream != nil {
		d.stream.Close()
		d.stream = nil
	}
	if d.file != nil {
		d.file.Close()
		d.file = nil
	}
}
