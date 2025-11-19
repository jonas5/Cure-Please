// main.cpp - implementation for Miraculix plugin

#define NOMINMAX

#include "../Ashita-v4beta/plugins/sdk/Ashita.h"
#include <algorithm>
#include <set>
#include <sstream>
#include <iomanip>

#include "main.h"
#include "spells.h"
#include "EffectNames.h"
#include "SpellNames.h"
#include "debuffhandler.h"
#include "PendingCast.h"

#include <string>
#include <unordered_map>
#include <set>   // add this at the top of main.cpp

#include <cfloat>
#include <cmath>

bool CurePleasePlugin::debugEnabled = false;

// --- Define status message sets (from HXUI debuffhandler.lua) ---
const std::set<uint16_t> statusOnMes = {
    160, 164, 166, 186, 194, 203, 205, 230, 236,
    266, 267, 268, 269, 237, 271, 272, 277, 278,
    279, 280, 319, 320, 375, 412, 754, 755, 804
    // 645 removed
};

const std::set<uint16_t> statusOffMes = {
    206, 64, 159, 168, 204, 206, 321, 322, 341, 342,
    343, 344, 350, 378, 531, 645, 647, 805, 806
    // 645 added here
};

const std::set<uint16_t> deathMes = {
    6, 20, 97, 113, 406, 605, 646
};

const std::set<uint16_t> spellDamageMes = {
    2, 252, 264, 265
};


// Helper: read a specific number of bits from a byte array (big-endian)
static uint32_t readBitsBig(const uint8_t* data, size_t bitOffset, size_t bitLength, size_t dataSizeBytes)
{
    if (bitLength > 32) return 0;

    uint32_t value = 0;
    for (size_t i = 0; i < bitLength; ++i) {
        size_t currentBit = bitOffset + i;
        size_t byteIndex = currentBit / 8;
        size_t bitInByte = 7 - (currentBit % 8); // MSB first

        if (byteIndex >= dataSizeBytes) {
            return 0; // Out of bounds
        }

        uint8_t bit = (data[byteIndex] >> bitInByte) & 1;
        value = (value << 1) | bit;
    }
    return value;
}

// Helper: read a specific number of bits from a byte array (little-endian)
static uint32_t readBitsLittle(const uint8_t* data, size_t bitOffset, size_t bitLength, size_t dataSizeBytes)
{
    size_t byteOffset = bitOffset / 8;
    size_t bitInByte  = bitOffset % 8;
    size_t byteCount  = (bitLength + 7) / 8;

    if (byteOffset + byteCount > dataSizeBytes) return 0;

    uint32_t val = 0;
    for (size_t i = 0; i < byteCount; ++i) {
        val |= static_cast<uint32_t>(data[byteOffset + i]) << (8 * i);
    }
    return (val >> bitInByte) & ((1u << bitLength) - 1);
}




using namespace Ashita;

// Plugin metadata constants
constexpr const char* g_PluginName        = "Miraculix";
constexpr const char* g_PluginAuthor      = "Jules";
constexpr const char* g_PluginDescription = "Minimal parser for 0x0E chat packets.";
constexpr double      g_PluginVersion     = 1.0;

// ---------------------------------------------------------------------------
// Metadata implementations
// ---------------------------------------------------------------------------
const char* CurePleasePlugin::GetName() const        { return g_PluginName; }
const char* CurePleasePlugin::GetAuthor() const      { return g_PluginAuthor; }
const char* CurePleasePlugin::GetDescription() const { return g_PluginDescription; }
double      CurePleasePlugin::GetVersion() const     { return g_PluginVersion; }
uint32_t    CurePleasePlugin::GetFlags() const       { return (uint32_t)PluginFlags::UsePackets; }

// ---------------------------------------------------------------------------
// Lifecycle
// ---------------------------------------------------------------------------
bool CurePleasePlugin::Initialize(IAshitaCore* core, ILogManager* log, uint32_t id)
{
    m_AshitaCore = core;
    m_LogManager = log;
    m_Pipe.Start();
    TryToGetPlayerInfo();
    return true;
}

void CurePleasePlugin::Release()
{
    m_Pipe.Stop();
}

bool CurePleasePlugin::OnTick()
{
    TryToGetPlayerInfo();

    uint64_t now = getCurrentTimeMs();
    m_PendingCasts.erase(std::remove_if(m_PendingCasts.begin(), m_PendingCasts.end(),
        [&](const PendingCast& cast) {
            return (now - cast.timestamp0x1A) > 5000; // 5 second timeout
        }), m_PendingCasts.end());

    return false;
}
std::pair<uint32_t, std::string> CurePleasePlugin::ResolveTargetIndex(uint16_t targetIndex, IAshitaCore* core, uint32_t actorId, bool debugEnabled)
{
    uint32_t targetId = 0;
    std::string targetName = "Unknown";

    if (!core || targetIndex >= 2048)
    {
        if (debugEnabled)
            WriteToPipe("LOG|RESOLVE|Invalid core or targetIndex=" + std::to_string(targetIndex));
        return { targetId, targetName };
    }

    auto mmgr = core->GetMemoryManager();
    if (!mmgr)
    {
        if (debugEnabled)
            WriteToPipe("LOG|RESOLVE|MemoryManager unavailable");
        return { targetId, targetName };
    }


    // Try EntityManager first, but safely.
    auto entMgr = mmgr->GetEntity();
    if (entMgr)
    {
        try {
            // First, check if the server ID is valid. An ID of 0 means no entity.
            uint32_t sid = entMgr->GetServerId(targetIndex);
            if (sid != 0)
            {
                // If the ID is valid, it's safer to get the name.
                const char* nm = entMgr->GetName(targetIndex);

                targetId = sid;
                if (nm && nm[0] != '\0')
                {
                    targetName.assign(nm);
                }

                if (debugEnabled)
                {
                    uintptr_t rawPtr = reinterpret_cast<uintptr_t>(nm);
                    WriteToPipe("LOG|RESOLVE|EntityManager|idx=" + std::to_string(targetIndex) +
                                "|sid=" + std::to_string(sid) +
                                "|name=" + (nm ? std::string(nm) : "null") +
                                "|ptr=" + std::to_string(rawPtr));
                }
            }
            else
            {
                if (debugEnabled)
                    WriteToPipe("LOG|RESOLVE|EntityManager|idx=" + std::to_string(targetIndex) + "|sid=0, entity is invalid.");
            }
        }
        catch (...)
        {
            if (debugEnabled)
                WriteToPipe("LOG|ERROR|EntityManager threw during resolution of idx=" + std::to_string(targetIndex));
        }
    }

    // Fallback: scan PartyManager if EntityManager failed
    if (targetId == 0)
    {
        auto partyMgr = mmgr->GetParty();
        if (partyMgr)
        {
            for (int i = 0; i < 18; ++i)
            {
                try {
                    uint32_t sid = partyMgr->GetMemberServerId(i);
                    const char* nm = partyMgr->GetMemberName(i);

                    uintptr_t rawPtr = reinterpret_cast<uintptr_t>(nm);
                    if (debugEnabled)
                        WriteToPipe("LOG|TRACE|PartyManager|slot=" + std::to_string(i) +
                                    "|sid=" + std::to_string(sid) +
                                    "|ptr=" + std::to_string(rawPtr));

                    if (sid != 0 && nm && nm[0] != '\0')
                    {
                        targetId = sid;
                        targetName.assign(nm);

                        if (debugEnabled)
                            WriteToPipe("LOG|RESOLVE|PartyManager|slot=" + std::to_string(i) +
                                        "|sid=" + std::to_string(sid) +
                                        "|name=" + std::string(nm));
                        break;
                    }
                }
                catch (...)
                {
                    if (debugEnabled)
                        WriteToPipe("LOG|ERROR|PartyManager threw at slot=" + std::to_string(i));
                }
            }
        }
    }

    // Optional: fallback to self if resolution failed
    if (targetId == 0 && actorId != 0)
    {
        targetId = actorId;
        targetName = "Self";

        if (debugEnabled)
            WriteToPipe("LOG|RESOLVE|Fallback to actorId=" + std::to_string(actorId));
    }

    return { targetId, targetName };
}


// ---------------------------------------------------------------------------
// Packet handlers
// ---------------------------------------------------------------------------
bool CurePleasePlugin::HandleIncomingPacket(uint16_t id, uint32_t size, const uint8_t* data,
                                            uint8_t* /*modified*/, uint32_t /*sizeChunk*/,
                                            const uint8_t* /*dataChunk*/, bool /*injected*/, bool /*blocked*/)
{
    if (!ready_ || !data || size == 0)
        return false;

    if (id == 0x0E) {
        ParseChatLogPacket(id, size, data);
    } else if (id == 0x28) {
        Handle0x28(data, size);
    } else if (id == 0xCA || id == 0x076) {
        HandleBuffPacket(data, size);
    }

    // message
    //local basic = {
    //    sender     = struct.unpack('i4', e, 0x04 + 1),
    //    target     = struct.unpack('i4', e, 0x08 + 1),
    //    param      = struct.unpack('i4', e, 0x0C + 1),
    //    value      = struct.unpack('i4', e, 0x10 + 1),
    //    sender_tgt = struct.unpack('i2', e, 0x14 + 1),
    //    target_tgt = struct.unpack('i2', e, 0x16 + 1),
    //    message    = struct.unpack('i2', e, 0x18 + 1),
    //}
    
    return false;
}

bool CurePleasePlugin::HandleOutgoingPacket(uint16_t id, uint32_t size, const uint8_t* data,
                                            uint8_t*, uint32_t, const uint8_t*, bool, bool)
{
    TryToGetPlayerInfo();

    if (id == 0x1A && size >= 14)
    {
        //if (debugEnabled)
        //    Discovery(data, size);

        uint32_t targetId = 0;
        std::memcpy(&targetId, data + 4, sizeof(targetId));

        uint16_t spellId = 0;
        std::memcpy(&spellId, data + 12, sizeof(spellId));

        uint32_t casterId = m_PlayerActorId;

        if (debugEnabled)
        {
            std::ostringstream dbg;
            dbg << "LOG|DEBUG|0x1A|casterId=" << casterId;
            WriteToPipe(dbg.str());

            // Optional hex dump for verification
            //std::ostringstream hex;
            //hex << "LOG|HEX|0x1A|";
            //for (size_t i = 0; i < size; ++i)
            //{
            //    hex << std::hex << std::setw(2) << std::setfill('0')
            //        << static_cast<int>(data[i]);
            //}
            //WriteToPipe(hex.str());
        }

        std::string targetName = GetEntityNameById(targetId);

        // Compute cast duration
        uint32_t castDurationMs = 1000; // default
        auto* resourceManager = m_AshitaCore->GetResourceManager();
        if (resourceManager)
        {
            auto* spell = resourceManager->GetSpellById(spellId);
            if (spell && spell->CastTime > 0)
                castDurationMs = (spell->CastTime * 250) + 500;
        }

        // Assumption: a player can only start one action at a time.
        // Clear any previous pending casts to prevent state corruption.
        m_PendingCasts.clear();

        // Track pending cast
        uint32_t seq = ++m_sequenceIdCounter;
        m_PendingCasts.push_back({
            seq,
            spellId,
            casterId,
            targetId,
            getCurrentTimeMs(),
            0,
            CastingStatus::STARTED,
            castDurationMs
            });

        // Debug logging
        if (debugEnabled)
        {
            WriteToPipe("CAST_START");
            WriteToPipe("LOG|DEBUG|0x1A|casterId=" + std::to_string(casterId) +
                "|targetId=" + std::to_string(targetId) +
                "|spellId=" + std::to_string(spellId) +
                "|targetName=" + targetName);

            //std::ostringstream log;
            //log << "LOG|PACKET_OUT|0x1A|spellId=" << spellId << "|";
            //for (size_t i = 0; i < size; ++i)
            //    log << std::hex << std::setw(2) << std::setfill('0')
            //    << static_cast<int>(data[i]);
            //log << "\n";
            //WriteToPipe(log.str());
        }
    }

    else if (id == 0x15) {
        if (debugEnabled) {
            std::ostringstream log;
            log << "LOG|PACKET_OUT|0015|";
            for (size_t i = 0; i < size; ++i) {
                log << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(data[i]);
            }
            log << "\n";
            WriteToPipe(log.str());
        }
    }
    return false;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

// A helper function to get an entity's array index from their server ID.
int CurePleasePlugin::GetIndexFromId(uint32_t id) {
    auto* entMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
    if (!entMgr) return 0;

    // Fast path for monsters/static NPCs
    if ((id & 0x1000000) != 0) {
        int index = id & 0xFFF;
        if (index >= 0x900) {
            index -= 0x100;
        }
        if (index < 0x900 && entMgr->GetServerId(index) == id) {
            return index;
        }
    }

    // Slow path for players/other entities
    for (int i = 1; i < 0x900; ++i) {
        if (entMgr->GetServerId(i) == id) {
            return i;
        }
    }
    return 0;
}

void CurePleasePlugin::HandleStatusMessage(uint16_t messageId,
                                           uint32_t actorId,
                                           uint32_t targetId,
                                           uint32_t spellId,
                                           const std::vector<uint32_t>& params)
{
    uint64_t now = getCurrentTimeMs();
    auto it = std::find_if(m_PendingCasts.rbegin(), m_PendingCasts.rend(),
        [&](const PendingCast& cast) {
            bool targetMatch = cast.targetId == targetId;
            bool timeMatch = (now - cast.timestamp0x1A) < cast.castDurationMs;
            return targetMatch && timeMatch;
    });

    if (it != m_PendingCasts.rend()) {
        LogSuccessfulCast(it->actorId, it->targetId, it->spellId);
        m_PendingCasts.erase(std::next(it).base());
    }
}


void CurePleasePlugin::ParseChatLogPacket(uint16_t id, uint32_t size, const uint8_t* data)
{
    return;
    // if (e.id == 0x00E) then
	//	local mobPacket = T{};
	//	mobPacket.monsterId = struct.unpack('L', e.data, 0x04 + 1);
	//	mobPacket.monsterIndex = struct.unpack('H', e.data, 0x08 + 1);
	//	mobPacket.updateFlags = struct.unpack('B', e.data, 0x0A + 1);
	//	if (bit.band(mobPacket.updateFlags, 0x02) == 0x02) then
	//		mobPacket.newClaimId = struct.unpack('L', e.data, 0x2C + 1);
	//	end
	//	return mobPacket;
	//end

    // Special case: mob packet (id == 0x00E)
    if (id == 0x0E && size >= 0x30) 
    {
        struct MobPacket {
            uint32_t monsterId;
            uint16_t monsterIndex;
            uint8_t  updateFlags;
            uint32_t newClaimId;
        } mobPacket{};

        mobPacket.monsterId    = *reinterpret_cast<const uint32_t*>(data + 0x04);
        mobPacket.monsterIndex = *reinterpret_cast<const uint16_t*>(data + 0x08);
        mobPacket.updateFlags  = *(data + 0x0A);

        if ((mobPacket.updateFlags & 0x02) == 0x02) {
            mobPacket.newClaimId = *reinterpret_cast<const uint32_t*>(data + 0x2C);
        }

        if (debugEnabled == false) {
            std::ostringstream dbg;
            dbg << "LOG|DEBUG|0x0e|MobPacket"
                << "|monsterId="    << mobPacket.monsterId
                << "|monsterIndex=" << mobPacket.monsterIndex
                << "|updateFlags=0x" << std::hex << static_cast<int>(mobPacket.updateFlags)
                << "|newClaimId="   << mobPacket.newClaimId;
            WriteToPipe(dbg.str() + "\n");
        }

        //HandleMobPacket(mobPacket);
        //return;
    }

    // --- Status packet parsing ---
    std::vector<uint32_t> params;

    // Direct param extraction: assume params start at fixed offset (no discovery loop)
    const size_t bitsTotal = static_cast<size_t>(size) * 8;
    const size_t PARAMS_OFFSET = 24; // after msgId (16) + paramCount (8)
    const size_t bitsRemaining = bitsTotal - PARAMS_OFFSET;
    const size_t maxParams = bitsRemaining / 32u;

    // For simplicity, read up to 4 params (adjust as needed)
    size_t toRead = std::min<size_t>(4, maxParams);
    for (size_t i = 0; i < toRead; ++i) {
        params.push_back(readBitsLittle(data, PARAMS_OFFSET + i * 32, 32, size));
    }

    // Crucial fields
    uint32_t targetId       = (params.size() > 0 ? params[0] : 0);
    uint32_t spellOrEffectId= (params.size() > 1 ? params[1] : 0); // CRUCIAL
    uint32_t actorId        = 0; // not present in these packets

    if (debugEnabled == false) {
        std::ostringstream dbg;
        dbg << "LOG|DEBUG|0x00E|StatusPacket"
            << "|actorId=" << actorId
            << "|targetId=" << targetId
            << "|spellOrEffectId=" << spellOrEffectId
            << "|Params=[";
        for (size_t i = 0; i < params.size(); ++i) {
            dbg << params[i];
            if (i + 1 < params.size()) dbg << ",";
        }
        dbg << "]";
        WriteToPipe(dbg.str() + "\n");
    }

    //HandleStatusMessage(id, actorId, targetId, spellOrEffectId, params);
}



void CurePleasePlugin::TryToGetPlayerInfo()
{
    if (m_PlayerActorId != 0) return;

    auto* entMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
    auto* party  = m_AshitaCore->GetMemoryManager()->GetParty();
    if (!entMgr || !party) return;

    int playerIndex = party->GetMemberTargetIndex(0);
    if (playerIndex < 0) return;

    m_PlayerActorId = entMgr->GetServerId(playerIndex);
    m_PlayerName    = std::string(entMgr->GetName(playerIndex));

    if (m_PlayerActorId != 0)
    {
        ready_ = true;
        WriteToPipe("LOG|INFO|actorId=" + std::to_string(m_PlayerActorId) +
                    "|name=" + m_PlayerName + "\n");
    }
}

std::string CurePleasePlugin::CastingStatusToString(CastingStatus status) {
    switch (status) {
        case CastingStatus::STARTED:     return "STARTED";
        case CastingStatus::FINISHED:    return "FINISHED";
        case CastingStatus::INTERRUPTED: return "INTERRUPTED";
        case CastingStatus::BLOCKED:     return "BLOCKED";
        case CastingStatus::UNKNOWN:     return "UNKNOWN";
        case CastingStatus::RESISTED:    return "RESISTED";
        default:                         return "INVALID_STATUS";
    }
}

void CurePleasePlugin::LogSuccessfulCast(uint32_t actorId, uint32_t targetId, uint16_t spellId)
{
    std::string castMessage = "ACTION|" + std::to_string(actorId) + "|" +
                              std::to_string(targetId) + "|" +
                              std::to_string(spellId) +
                              "|MSG=0|PARAM=0|EFFECT=0|FLAGS=0\n";
    WriteToPipe(castMessage);
}

// ---------------------------------------------------------------------------
// Plugin exports (Ashita v4)
// ---------------------------------------------------------------------------
extern "C" __declspec(dllexport) IPlugin* __stdcall expCreatePlugin(const char* args)
{
    return new CurePleasePlugin();
}

extern "C" __declspec(dllexport) double __stdcall expGetInterfaceVersion(void)
{
    return ASHITA_INTERFACE_VERSION;
}

void CurePleasePlugin::Discovery(const uint8_t* data, size_t size)
{
    if (size < 16) return;

    std::ostringstream log;
    log << "LOG|DISCOVER|DWORDS|size=" << size << "\n";

    // Slide a 4-byte window from offset 1 to 32 (or up to size-4)
    for (size_t offset = 1; offset <= 32 && offset + 3 < size; ++offset)
    {
        uint32_t val = 0;
        std::memcpy(&val, data + offset, sizeof(val));
        log << "@"
            << std::setw(2) << offset
            << "=" << std::dec << val << " ";
    }

    WriteToPipe(log.str());
}


void CurePleasePlugin::Handle0x28(const uint8_t* data, size_t size)
{
    if (size < 16) return; // Basic sanity check

    if (debugEnabled)
        Discovery(data, size);

    uint32_t casterId = 0;
    std::memcpy(&casterId, data + 5, sizeof(casterId));

    uint16_t spellId = 0;
    std::memcpy(&spellId, data + 29, sizeof(spellId));

    uint32_t category = readBitsBig(data, 82, 4, size);


    const uint32_t msgBits      = readBitsBE(data, 230, 10, size);  // Lua: msg at 230 bits


    if (debugEnabled)
    {
        std::ostringstream dbg;
        dbg << "LOG|DEBUG|0x28|casterId=" << casterId
            << "|spellId=" << spellId
            << "|category=" << category;
        WriteToPipe(dbg.str());

        //std::ostringstream hex;
        //hex << "LOG|HEX|0x28|";
        //for (size_t i = 0; i < size; ++i)
        //{
        //    hex << std::hex << std::setw(2) << std::setfill('0')
        //        << static_cast<int>(data[i]);
        //}
        //WriteToPipe(hex.str());
    }

    uint64_t now = getCurrentTimeMs();

    if (category == 8) // Interrupted / Blocked / Resisted / Started
    {
        uint16_t subval = static_cast<uint16_t>(readBitsBig(data, 86, 16, size));
        // subval is the same 16-bit field as Lua 'param' at 86 bits

        auto it = std::find_if(m_PendingCasts.rbegin(), m_PendingCasts.rend(),
            [&](const PendingCast& cast) {
                return cast.actorId == casterId && (now - cast.timestamp0x1A) < cast.castDurationMs;
            });

        if (it != m_PendingCasts.rend())
        {
            bool shouldErase = false;

            if (subval == 28787) // Interrupted
            {
                it->status = CastingStatus::INTERRUPTED;
                std::ostringstream msg;
                msg << "CAST_INTERRUPT|" << casterId
                    << "|" << it->targetId
                    << "|" << it->spellId
                    << "|status=" << static_cast<int>(it->status);
                WriteToPipe(msg.str());
                shouldErase = true;
            }
            else if (subval == 24931) // Blocked
            {
                it->status = CastingStatus::BLOCKED;
                std::ostringstream msg;
                msg << "CAST_BLOCKED|" << casterId
                    << "|" << it->targetId
                    << "|" << it->spellId
                    << "|status=" << static_cast<int>(it->status);
                WriteToPipe(msg.str());
                shouldErase = true;
            }
            else if (msgBits == 85 || msgBits == 284 || subval == 258) // Resisted (partial/full)
            {
                it->status = CastingStatus::RESISTED;

                std::string trigger;
                if (msgBits == 85)       trigger = "msgBits=85";
                else if (msgBits == 284) trigger = "msgBits=284";
                else if (subval == 258) trigger = "subval=258";
                else trigger = "unknown";

                std::ostringstream msg;
                msg << "LOG|CAST_RESISTED|" << casterId
                    << "|" << it->targetId
                    << "|" << it->spellId
                    << "|status=" << static_cast<int>(it->status)
                    << "|trigger=" << trigger;
                WriteToPipe(msg.str());

                shouldErase = true;
            }
            else
            {
                it->status = CastingStatus::STARTED;
            }

            it->timestamp0x28 = now;

            if (debugEnabled)
            {
                std::ostringstream dbg;
                dbg << "LOG|DEBUG|0x28|STATUS_UPDATE"
                    << "|casterId=" << casterId
                    << "|reliableTargetId=" << it->targetId
                    << "|reliableSpellId=" << it->spellId
                    << "|status=" << CastingStatusToString(it->status);
                WriteToPipe(dbg.str());
            }

            if (shouldErase)
                m_PendingCasts.erase(std::next(it).base());
        }
    }


 
    else if (category == 4) // Finished
    {
        auto it = std::find_if(m_PendingCasts.rbegin(), m_PendingCasts.rend(),
            [&](const PendingCast& cast) {
                // The spellId in a FINISHED packet is not reliable.
                // Instead, we find the latest spell from this caster that is in a 'STARTED' state.
                return cast.actorId == casterId && cast.status == CastingStatus::STARTED && (now - cast.timestamp0x1A) < cast.castDurationMs;
            });

        if (it != m_PendingCasts.rend())
        {
            it->status = CastingStatus::FINISHED;
            it->timestamp0x28 = now;

            WriteToPipe("CAST_FINISH");

            if (debugEnabled)
            {
                std::ostringstream dbg;
                dbg << "LOG|DEBUG|0x28|CORRELATED|FINAL_BUILD_MARKER"
                    << "|casterId=" << casterId
                    << "|reliableTargetId=" << it->targetId
                    << "|reliableSpellId=" << it->spellId
                    << "|status=" << CastingStatusToString(it->status);
                WriteToPipe(dbg.str());
            }

            std::ostringstream msg;
            msg << "ACTION|" << casterId
                << "|" << it->targetId
                << "|" << it->spellId
                << "|EFFECT=0"
                << "|status=" << static_cast<int>(it->status);
            WriteToPipe(msg.str());

            // ✅ Always erase after FINISHED
            m_PendingCasts.erase(std::next(it).base());
        }
        else if (debugEnabled)
        {
            WriteToPipe("LOG|DEBUG|0x28|No matching 'STARTED' cast found for casterId=" + std::to_string(casterId));
        }
    }

}

void CurePleasePlugin::HandleBuffPacket(const uint8_t* data, size_t size)
{
    auto* entMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
    if (!entMgr) return;

    for (int k = 0; k < 5; ++k) {
        uint16_t userIndex;
        std::memcpy(&userIndex, data + 8 + (k * 0x30), sizeof(userIndex));

        if (userIndex == 0) continue;

        // Safety Check: Validate the userIndex before using it.
        uint32_t serverId = entMgr->GetServerId(userIndex);
        if (serverId == 0) continue;

        const char* characterNameC = entMgr->GetName(userIndex);
        if (!characterNameC) continue;
        std::string characterName(characterNameC);

        std::vector<uint16_t> buffs;
        for (int i = 1; i <= 32; ++i) {
            uint8_t byte1 = data[k * 48 + 5 + 16 + i - 1];
            uint8_t byte2 = data[k * 48 + 5 + 8 + (i - 1) / 4];
            uint16_t current_buff = byte1 + 256 * ((byte2 >> (2 * ((i - 1) % 4))) & 3);

            if (current_buff != 255 && current_buff != 0) {
                buffs.push_back(current_buff);
            }
        }

        if (!buffs.empty()) {
            std::string formattedString = "CUREPLEASE_buffs_" + characterName + "_";
            for (size_t i = 0; i < buffs.size(); ++i) {
                formattedString += std::to_string(buffs[i]);
                if (i < buffs.size() - 1) {
                    formattedString += ",";
                }
            }
            WriteToPipe(formattedString + "\n");
        }
    }
}
