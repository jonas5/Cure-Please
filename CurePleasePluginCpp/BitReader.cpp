#include "BitReader.hpp"

BitReader::BitReader(const uint8_t* data, size_t size)
    : m_Data(data), m_Size(size), m_BitPos(0) {}

void BitReader::setPosition(size_t bitPos) {
    m_BitPos = bitPos;
}

size_t BitReader::getPosition() const {
    return m_BitPos;
}

uint32_t BitReader::readBits(size_t count) {
    uint32_t value = 0;
    for (size_t i = 0; i < count; ++i) {
        size_t byteIndex = m_BitPos >> 3;       // divide by 8
        size_t bitIndex  = 7 - (m_BitPos & 7);  // MSB-first: 7 down to 0
        if (byteIndex >= m_Size) break;
        uint8_t bit = (m_Data[byteIndex] >> bitIndex) & 1;
        value = (value << 1) | bit;
        ++m_BitPos;
    }
    return value;
}
