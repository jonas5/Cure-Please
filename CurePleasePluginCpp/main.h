// main.h - header for Miraculix plugin

#pragma once

#include "../Ashita-v4beta/plugins/sdk/Ashita.h"
#include "Pipe.h"
#include "debuffhandler.h"
#include "PendingCast.h"
#include "spells.h"

#include <string>
#include <vector>
#include <cstdint>
#include <chrono>
#include <optional>
#include <deque>
#include <unordered_set>

// Helper to get current time in milliseconds
inline uint64_t getCurrentTimeMs() {
    using namespace std::chrono;
    return duration_cast<milliseconds>(steady_clock::now().time_since_epoch()).count();
}

// Data structures to hold the parsed action packet data, based on HXUI's format.

struct AdditionalEffect {
    uint32_t Damage;
    uint32_t Param;
    uint32_t Message;
};

struct SpikesEffect {
    uint32_t Damage;
    uint32_t Param;
    uint32_t Message;
};

struct Action {
    uint32_t Reaction;
    uint32_t Animation;
    uint32_t SpecialEffect;
    uint32_t Knockback;
    uint32_t Param;
    uint32_t Message;
    uint32_t Flags;
    std::optional<AdditionalEffect> additionalEffect;
    std::optional<SpikesEffect> spikesEffect;
};

struct Target {
    uint32_t Id;
    std::vector<Action> Actions;
};

struct ActionPacket {
    uint32_t UserId;
    uint32_t UserIndex;
    uint32_t Type;
    uint32_t Param;
    uint32_t Recast;
    std::vector<Target> Targets;
};


// ---------------------------------------------------------------------------
// CurePleasePlugin class skeleton
// ---------------------------------------------------------------------------
class CurePleasePlugin : public IPlugin
{
private:
    // Core Ashita interfaces
    IAshitaCore* m_AshitaCore = nullptr;
    ILogManager* m_LogManager = nullptr;
    static bool debugEnabled;
    uint32_t m_PlayerActorId = 0;
    std::string m_PlayerName;



    std::string GetEntityNameById(uint32_t id)
    {
        auto entMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
        if (!entMgr || id == 0) return "None";
        for (int i = 0; i < 2048; ++i) {
            if (entMgr->GetServerId(i) == id) {
                const char* name = entMgr->GetName(i);
                return name ? name : "Unknown";
            }
        }
        return "Unknown";
    }

    std::string ResolveSpellName(uint32_t spellId)
    {
        auto it = spells.find(static_cast<uint16_t>(spellId));
        return (it != spells.end()) ? it->second.name : "Unknown Spell (" + std::to_string(spellId) + ")";
    }

    static inline uint32_t readBitsBE(const uint8_t* buf, size_t bitOffset, size_t bitLen, size_t bufSizeBytes)
    {
        uint32_t out = 0;
        for (size_t i = 0; i < bitLen; ++i)
        {
            const size_t bitPos    = bitOffset + i;
            const size_t byteIndex = bitPos / 8;
            if (byteIndex >= bufSizeBytes) break;
            const size_t bitInByte = bitPos % 8;
            const uint8_t byte     = buf[byteIndex];
            const uint8_t bit      = (byte >> (7 - bitInByte)) & 0x01;
            out = (out << 1) | bit;
        }
        return out;
    }

    static inline uint64_t getCurrentTimeMs()
    {
        using namespace std::chrono;
        return duration_cast<milliseconds>(steady_clock::now().time_since_epoch()).count();
    }




    // Track grammatical number for entities
    std::unordered_set<uint32_t> commonNouns;
    std::unordered_set<uint32_t> pluralEntities;

    // Pipe manager for external logging
    PipeManager m_Pipe;

    // Debuff handler
    DebuffHandler m_debuffHandler;

    // Pending casts queue
    std::deque<PendingCast> m_PendingCasts;

    // Internal state
    bool ready_ = false;
    uint32_t m_sequenceIdCounter = 0;

    // Helpers
    void TryToGetPlayerInfo();
    static std::string CastingStatusToString(CastingStatus status);
    void ParseChatLogPacket(uint16_t id, uint32_t size, const uint8_t* data);
    void HandleStatusMessage(uint16_t messageId, uint32_t actorId, uint32_t targetId, uint32_t spellId, const std::vector<uint32_t>& params);
    void Handle0x28(const uint8_t* data, size_t size);
    std::pair<uint32_t, std::string> ResolveTargetIndex(uint16_t targetIndex, IAshitaCore* core, uint32_t actorId, bool debugEnabled = false);
    void Discovery(const uint8_t* data, size_t size);
    void HandleBuffPacket(const uint8_t* data, size_t size);
    int GetIndexFromId(uint32_t id);

    // Wrapper to keep WriteToPipe syntax
    void WriteToPipe(const std::string& message) { m_Pipe.Write(message); }
    void LogSuccessfulCast(uint32_t actorId, uint32_t targetId, uint16_t spellId);

public:
    // Metadata
    const char* GetName() const override;
    const char* GetAuthor() const override;
    const char* GetDescription() const override;
    double GetVersion() const override;
    uint32_t GetFlags() const override;

    // Lifecycle
    bool Initialize(IAshitaCore* core, ILogManager* log, uint32_t id) override;
    void Release() override;
    bool OnTick();

    // Packet handlers (must return bool per SDK)
    bool HandleIncomingPacket(uint16_t id, uint32_t size, const uint8_t* data,
                              uint8_t* modified, uint32_t sizeChunk,
                              const uint8_t* dataChunk, bool injected, bool blocked) override;

    bool HandleOutgoingPacket(uint16_t id, uint32_t size, const uint8_t* data,
                              uint8_t* modified, uint32_t sizeChunk,
                              const uint8_t* dataChunk, bool injected, bool blocked) override;

};
