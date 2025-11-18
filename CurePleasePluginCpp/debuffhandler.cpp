#include "debuffhandler.h"
#include "main.h"
#include <chrono>

// Helper to get current time in seconds
static uint64_t getCurrentTimeSec() {
    return std::chrono::duration_cast<std::chrono::seconds>(
        std::chrono::system_clock::now().time_since_epoch()).count();
}

// A simplified debuff application logic based on spell IDs.
// This is where you would expand the logic for different spells.
void DebuffHandler::HandleActionPacket(const ActionPacket& packet) {
    uint64_t now = getCurrentTimeSec();
    for (const auto& target : packet.Targets) {
        for (const auto& action : target.Actions) {
            // Placeholder: Assume action.Param is a buffId for now.
            // A more robust solution would map spellId to buffId.
            uint16_t buffId = static_cast<uint16_t>(action.Param);

            // Example: Apply a 60-second timer for a specific spell.
            // You would add more cases here for different debuffs.
            if (packet.Param == 23) { // Dia
                 m_enemies[target.Id][buffId] = now + 60;
            }
        }
    }
}

const DebuffMap& DebuffHandler::GetActiveDebuffs() const {
    // In a real application, you would also filter out expired debuffs here.
    return m_enemies;
}
