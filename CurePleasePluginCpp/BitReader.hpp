#pragma once
#include <vector>
#include <cstdint>

class BitReader {
public:
    BitReader(const uint8_t* data, size_t size);
    void setPosition(size_t byteOffset);
    uint32_t readBits(size_t bitCount);

private:
    std::vector<uint8_t> buffer;
    size_t bytePos;
    uint8_t bitPos;
};
