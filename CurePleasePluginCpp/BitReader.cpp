#include "BitReader.hpp"

BitReader::BitReader(const uint8_t* data, size_t size)
    : buffer(data, data + size), bytePos(0), bitPos(0) {}

void BitReader::setPosition(size_t byteOffset) {
    bytePos = byteOffset;
    bitPos = 0;
}

uint32_t BitReader::readBits(size_t bitCount) {
    uint32_t result = 0;
    for (size_t i = 0; i < bitCount; ++i) {
        if (bytePos >= buffer.size()) break;
        uint8_t bit = (buffer[bytePos] >> bitPos) & 1;
        result |= (bit << i);

        ++bitPos;
        if (bitPos == 8) {
            bitPos = 0;
            ++bytePos;
        }
    }
    return result;
}
