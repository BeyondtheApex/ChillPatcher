using System;

namespace ChillPatcher.ModuleSystem.Services.Streaming
{
    /// <summary>
    /// 线程安全的环形缓冲区, 用于生产者-消费者 PCM 数据传递
    /// </summary>
    public class RingBuffer
    {
        private readonly float[] _buffer;
        private readonly int _capacity;
        private int _writePos;
        private int _readPos;
        private int _count;
        private readonly object _lock = new object();

        public RingBuffer(int capacity)
        {
            _capacity = capacity;
            _buffer = new float[capacity];
        }

        /// <summary>当前可读的 sample 数量</summary>
        public int Count { get { lock (_lock) return _count; } }

        /// <summary>缓冲区总容量 (samples)</summary>
        public int Capacity => _capacity;

        /// <summary>当前可写的空闲 sample 数量</summary>
        public int FreeSpace { get { lock (_lock) return _capacity - _count; } }

        /// <summary>
        /// 写入 samples 到缓冲区
        /// </summary>
        /// <returns>实际写入的 sample 数</returns>
        public int Write(float[] data, int offset, int length)
        {
            lock (_lock)
            {
                int writable = _capacity - _count;
                if (writable <= 0) return 0;
                int toWrite = Math.Min(writable, length);

                int firstChunk = Math.Min(toWrite, _capacity - _writePos);
                Array.Copy(data, offset, _buffer, _writePos, firstChunk);

                int secondChunk = toWrite - firstChunk;
                if (secondChunk > 0)
                    Array.Copy(data, offset + firstChunk, _buffer, 0, secondChunk);

                _writePos = (_writePos + toWrite) % _capacity;
                _count += toWrite;
                return toWrite;
            }
        }

        /// <summary>
        /// 从缓冲区读取 samples
        /// </summary>
        /// <returns>实际读取的 sample 数</returns>
        public int Read(float[] output, int offset, int count)
        {
            lock (_lock)
            {
                if (_count <= 0) return 0;
                int toRead = Math.Min(_count, count);

                int firstChunk = Math.Min(toRead, _capacity - _readPos);
                Array.Copy(_buffer, _readPos, output, offset, firstChunk);

                int secondChunk = toRead - firstChunk;
                if (secondChunk > 0)
                    Array.Copy(_buffer, 0, output, offset + firstChunk, secondChunk);

                _readPos = (_readPos + toRead) % _capacity;
                _count -= toRead;
                return toRead;
            }
        }

        /// <summary>清空缓冲区</summary>
        public void Clear()
        {
            lock (_lock)
            {
                _writePos = 0;
                _readPos = 0;
                _count = 0;
            }
        }
    }
}
