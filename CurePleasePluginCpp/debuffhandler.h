#pragma once

#include <unordered_map>
#include <cstdint>

// Forward-declare the ActionPacket struct to avoid circular dependency.
struct ActionPacket;

// A map to store active debuffs on enemies: enemy_id -> {buff_id -> expiry_timestamp}
using DebuffMap = std::unordered_map<uint32_t, std::unordered_map<uint16_t, uint64_t>>;

class DebuffHandler {
public:
    void HandleActionPacket(const ActionPacket& packet);
    const DebuffMap& GetActiveDebuffs() const;

private:
    DebuffMap m_enemies;
};
