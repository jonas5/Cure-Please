#pragma once
#include <cstddef>
#include <cstdint>

class BitReader {
private:
    const uint8_t* m_Data;
    size_t m_Size;     // size of buffer in bytes
    size_t m_BitPos;   // current bit position

public:
    BitReader(const uint8_t* data, size_t size);

    void setPosition(size_t bitPos);
    size_t getPosition() const;

    // New helper
    size_t remainingBits() const;

    uint32_t readBits(size_t count);
};
