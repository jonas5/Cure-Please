#pragma once

#include <cstdint>

enum class CastingStatus {
    STARTED,
    FINISHED,
    INTERRUPTED,
    BLOCKED,
    UNKNOWN,   // add this
    RESISTED   // add this
};

// Represents a spell cast that has been initiated (0x1A) but not yet completed (0x0E).
struct PendingCast {
    uint32_t sequenceId;
    uint16_t spellId;
    uint32_t actorId;
    uint32_t targetId;
    uint64_t timestamp0x1A;
    uint64_t timestamp0x28;
    CastingStatus status;
    uint32_t castDurationMs;
};
