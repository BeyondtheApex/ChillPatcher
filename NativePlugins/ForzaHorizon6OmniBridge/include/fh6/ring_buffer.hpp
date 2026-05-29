#pragma once

#include <atomic>
#include <cstddef>
#include <cstdint>
#include <memory>

namespace fh6 {

class RingBuffer {
public:
    explicit RingBuffer(std::size_t capacity_bytes);

    std::size_t capacity() const noexcept { return capacity_; }
    std::size_t readable() const noexcept;
    std::size_t writable() const noexcept;
    std::size_t read_position() const noexcept {
        return read_pos_.load(std::memory_order_acquire);
    }
    std::size_t write_position() const noexcept {
        return write_pos_.load(std::memory_order_acquire);
    }

    std::size_t write(const void* src, std::size_t n) noexcept;
    std::size_t read(void* dst, std::size_t n) noexcept;
    void drain() noexcept;

    void set_hold(bool on) noexcept { hold_.store(on, std::memory_order_release); }
    bool held() const noexcept { return hold_.load(std::memory_order_acquire); }

private:
    static std::size_t round_pow2(std::size_t n) noexcept;

    std::unique_ptr<std::byte[]> data_;
    std::size_t capacity_;
    std::size_t mask_;
    alignas(64) std::atomic<std::size_t> write_pos_{0};
    alignas(64) std::atomic<std::size_t> read_pos_{0};
    alignas(64) std::atomic<bool> hold_{false};
};

} // namespace fh6

