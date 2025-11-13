// CastInfo.h
#pragma once
#include <cstdint>

struct CastInfo {
    uint32_t actorId = 0;
    uint32_t spellId = 0;
    uint64_t timestamp = 0;
    uint32_t lastTargetId = 0;
    uint8_t  category = 0;
    bool     pending = false;
};
