#include "../Ashita-v4beta/plugins/sdk/Ashita.h"
#include "CastInfo.h"
#include <windows.h>
#include <string>
#include <thread>
#include <mutex>
#include <chrono>
#include <iomanip>
#include <sstream>
#include <atomic>
#include <bitset>
#include "spells.h"
#include "BitReader.hpp"
#include <vector>
#include <unordered_map>
#include <cstdint>



// Struct definition matching Wings server-side packet 0x00E
#pragma pack(push, 1)
struct message_t {
    uint16_t size;
    uint16_t id;       // 0x0E
    uint32_t senderId;
    float    x, y, z;
    uint8_t  unknown;
    char     text[256]; // Shift-JIS string
};

#pragma pack(pop)


uint64_t getCurrentTimeMs() {
    using namespace std::chrono;
    return duration_cast<milliseconds>(
        steady_clock::now().time_since_epoch()
    ).count();
}


static std::unordered_map<uint16_t, std::string> actionMessages = {
    {2,    "%s takes %d damage."},
    {15,   "%s resists the spell."},
    {85,   "%s is interrupted."},
    {93,   "%s misses %s."},
    {362,  "%s casts %s on %s."},
    {3628, "%s's %s has no effect on %s."},
    {3630, "%s is %s."}
    // … thousands more from Windower’s list
};




std::unordered_map<uint32_t, CastInfo> castMemory; // keyed by actorId

struct Result {
    uint8_t miss;
    uint8_t kind;
    uint16_t subKind;
    uint8_t infoBits;
    uint8_t scale;
    uint32_t value;
    uint16_t messageId;
    uint32_t bitfield;
    bool hasProc;
    bool hasReact;
    std::string outcome;
};

// Plugin metadata
constexpr const char* g_PluginName        = "Miraculix";
constexpr const char* g_PluginAuthor      = "Jules";
constexpr const char* g_PluginDescription = "Packet listener for Miraculix.";
constexpr double      g_PluginVersion     = 1.0;

// ActionFlags bitmask values
enum ActionFlags : uint8_t {
    ACTIONFLAG_NONE        = 0x00,
    ACTIONFLAG_HIT         = 0x01,
    ACTIONFLAG_CRIT        = 0x02,
    ACTIONFLAG_RESIST      = 0x04,
    ACTIONFLAG_INTERRUPTED = 0x08,
    ACTIONFLAG_SHADOW      = 0x10,
    ACTIONFLAG_MISS        = 0x20,
    ACTIONFLAG_KNOCKBACK   = 0x40,
    ACTIONFLAG_UNKNOWN     = 0x80
};

std::string DecodeFlags(uint8_t flags)
{
    if (flags & ACTIONFLAG_RESIST)      return "RESISTED";
    if (flags & ACTIONFLAG_INTERRUPTED) return "INTERRUPTED";
    if (flags & ACTIONFLAG_SHADOW)      return "SHADOW_ABSORB";
    if (flags & ACTIONFLAG_MISS)        return "MISS";
    if (flags & ACTIONFLAG_CRIT)        return "CRIT";
    if (flags & ACTIONFLAG_HIT)         return "HIT";
    if (flags & ACTIONFLAG_KNOCKBACK)   return "KNOCKBACK";
    return "NONE";
}

std::wstring PipeName = L"\\\\.\\pipe\\MiraculixPipe";






std::string GetTimestamp()
{
    auto now       = std::chrono::system_clock::now();
    auto in_time_t = std::chrono::system_clock::to_time_t(now);
    auto ms        = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()) % 1000;

    std::tm buf;
    localtime_s(&buf, &in_time_t);

    std::stringstream ss;
    ss << std::put_time(&buf, "[%H:%M:%S") << '.'
       << std::setw(3) << std::setfill('0') << ms.count() << "]";
    return ss.str();
}


class CurePleasePlugin : public IPlugin
{
private:
    IAshitaCore* m_AshitaCore    = nullptr;
    ILogManager* m_LogManager    = nullptr;

    HANDLE       m_hPipe         = INVALID_HANDLE_VALUE;
    std::thread  m_PipeThread;
    std::mutex   m_PipeMutex;
    std::atomic<bool> m_Shutdown = false;
    bool         m_PipeConnected = false;

    uint32_t     myActorId       = 0;

    // Helpers
    void WriteToPipe(const std::string& message);
    void DebugDecipherPacket(uint16_t id, uint32_t size, const uint8_t* data);
    void PipeThread();

    void rememberCast(uint32_t actorId, uint32_t spellId,
                      uint32_t targetId, uint8_t category);
    void attachOutcome(uint32_t actorId, uint32_t targetId, const Result& result);

    // Parsing
    void ParseResultBlock(BitReader& reader,
                          uint32_t actorId,
                          uint32_t targetId,
                          uint32_t spellId,
                          uint8_t category,
                          const char* tag);

    void ParseChatPacket(uint16_t id, uint32_t size, const uint8_t* data);
    void ParseChatLogPacket(uint16_t id, uint32_t size, const uint8_t* data);
    void ParseActionPacket(uint16_t id, uint32_t size, const uint8_t* data, bool outgoing = false);
    void ParseOutcomePacket(uint16_t id, uint32_t size, const uint8_t* data);

    // Internal init
    void InitMyActorId()
    {
        auto entityMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
        for (int i = 0; i < entityMgr->GetCount(); ++i)
        {
            uint32_t id = entityMgr->GetServerId(i);
            const char* name = entityMgr->GetName(i);

            if (id != 0)
            {
                //myActorId = id;
                WriteToPipe("DEBUG|Entity[" + std::to_string(i) +
                            "] name=" + std::string(name) +
                            " id=" + std::to_string(id) + "\n");
            }
        }

    }


public:
    // Metadata
    const char* GetName() const override        { return g_PluginName; }
    const char* GetAuthor() const override      { return g_PluginAuthor; }
    const char* GetDescription() const override { return g_PluginDescription; }
    double GetVersion() const override          { return g_PluginVersion; }
    uint32_t GetFlags() const override          { return (uint32_t)Ashita::PluginFlags::UsePackets; }

    // Lifecycle (Ashita v4)
    bool Initialize(IAshitaCore* core, ILogManager* log, uint32_t id) override;
    void Release() override;

// in class definition (header or top of main.cpp)
bool HandleIncomingPacket(uint16_t id, uint32_t size, const uint8_t* data,
                          uint8_t* modified, uint32_t sizeChunk,
                          const uint8_t* dataChunk, bool injected, bool blocked) override;

bool HandleOutgoingPacket(uint16_t id, uint32_t size, const uint8_t* data,
                          uint8_t* modified, uint32_t sizeChunk,
                          const uint8_t* dataChunk, bool injected, bool blocked) override;


    // State
    std::unordered_map<uint32_t, CastInfo> castMemory;
};


// ----- Member function definitions -----

void CurePleasePlugin::DebugDecipherPacket(uint16_t id, uint32_t size, const uint8_t* data)
{
    // Adjust parsing depending on buffer layout
    uint16_t packetId   = data[0] | (data[1] << 8);
    uint16_t packetSize = data[2] | (data[3] << 8);

    // Dump first 32 bytes
    std::string dump;
    char buf[8];
    for (int i = 0; i < 32 && i < (int)size; ++i) {
        sprintf(buf, "%02X ", data[i]);
        dump += buf;
    }
    WriteToPipe("DEBUG|0x0E raw|" + dump + "\n");

    // Log header info
    WriteToPipe("DEBUG|header|id=" + std::to_string(packetId) +
                " declared=" + std::to_string(packetSize) +
                " received=" + std::to_string(size) + "\n");

    // Only decode text if we have a full message packet
    const int textOffset = 36;
    if (packetSize >= 264 && size >= 264) {
        const char* sjis = reinterpret_cast<const char*>(data + textOffset);

        int wideLen = MultiByteToWideChar(932, MB_ERR_INVALID_CHARS,
                                          sjis, -1, nullptr, 0);
        if (wideLen > 0) {
            std::wstring wide(wideLen, L'\0');
            MultiByteToWideChar(932, MB_ERR_INVALID_CHARS,
                                sjis, -1, &wide[0], wideLen);

            int utf8Len = WideCharToMultiByte(CP_UTF8, 0,
                                              wide.c_str(), -1,
                                              nullptr, 0, nullptr, nullptr);
            if (utf8Len > 0) {
                std::string message(utf8Len, '\0');
                WideCharToMultiByte(CP_UTF8, 0,
                                    wide.c_str(), -1,
                                    &message[0], utf8Len,
                                    nullptr, nullptr);

                WriteToPipe("DEBUG|0x0E text|" + message + "\n");
            }
        } else {
            WriteToPipe("DEBUG|0x0E text|(no valid SJIS)\n");
        }
    } else {
        WriteToPipe("DEBUG|0x0E text|(no text field in this packet)\n");
    }
}


void CurePleasePlugin::WriteToPipe(const std::string& message)
{
    std::lock_guard<std::mutex> lock(m_PipeMutex);
    if (!m_PipeConnected || m_hPipe == INVALID_HANDLE_VALUE) return;

    // Ensure each message ends with newline so the client can parse it
    std::string out = message;
    if (out.empty() || out.back() != '\n')
        out.push_back('\n');

    DWORD bytesWritten = 0;
    BOOL ok = WriteFile(m_hPipe,
                        out.c_str(),
                        static_cast<DWORD>(out.size()),
                        &bytesWritten,
                        NULL);

    if (!ok) {
        // optional: log GetLastError() for diagnostics
    }

    // Force flush so the client sees it immediately
    FlushFileBuffers(m_hPipe);
}


void CurePleasePlugin::PipeThread()
{
    while (!m_Shutdown) {
        m_hPipe = CreateNamedPipeW(PipeName.c_str(), PIPE_ACCESS_OUTBOUND,
            PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
            1, 4096, 4096, 0, NULL);

        if (m_hPipe == INVALID_HANDLE_VALUE) {
            Sleep(5000);
            continue;
        }

        BOOL connected = ConnectNamedPipe(m_hPipe, NULL) ? TRUE : (GetLastError() == ERROR_PIPE_CONNECTED);
        if (connected) {
            {
                std::lock_guard<std::mutex> lock(m_PipeMutex);
                m_PipeConnected = true;
            }

            InitMyActorId();    

            WriteToPipe("LOG|" + GetTimestamp() +
                    " Packet listener v" + std::to_string(g_PluginVersion) +
                     " connected." + std::to_string(myActorId) + "\n");


            while (!m_Shutdown) {
                if (!PeekNamedPipe(m_hPipe, NULL, 0, NULL, NULL, NULL)) {
                    DWORD err = GetLastError();
                    if (err == ERROR_BROKEN_PIPE || err == ERROR_PIPE_NOT_CONNECTED)
                        break;
                }
                Sleep(500);
            }
        }

        {
            std::lock_guard<std::mutex> lock(m_PipeMutex);
            m_PipeConnected = false;
        }

        if (m_hPipe != INVALID_HANDLE_VALUE) {
            DisconnectNamedPipe(m_hPipe);
            CloseHandle(m_hPipe);
            m_hPipe = INVALID_HANDLE_VALUE;
        }
    }
}


// --- rememberCast ---
void CurePleasePlugin::rememberCast(uint32_t actorId, uint32_t spellId,
                                    uint32_t targetId, uint8_t category) {
    castMemory[actorId] = CastInfo{ actorId, spellId, getCurrentTimeMs(),
                                    targetId, category, true };

    // Emit CASTING immediately
    WriteToPipe("ACTION|" + std::to_string(actorId) +
                "|spell=" + std::to_string(spellId) +
                "|target=" + std::to_string(targetId) +
                "|category=" + std::to_string(category) +
                "|outcome=CASTING\n");
}

// --- attachOutcome ---
void CurePleasePlugin::attachOutcome(uint32_t actorId, uint32_t targetId, const Result& result) {
    auto it = castMemory.find(actorId);
    if (it == castMemory.end()) {
        WriteToPipe("LOG|no-remembered-cast|actor=" + std::to_string(actorId) +
                    "|target=" + std::to_string(targetId) +
                    "|outcome=" + result.outcome + "\n");
        return;
    }

    const uint32_t finalTargetId = targetId ? targetId : it->second.lastTargetId;

    // Emit final outcome
    WriteToPipe("ACTION|" + std::to_string(actorId) +
                "|spell=" + std::to_string(it->second.spellId) +
                "|target=" + std::to_string(finalTargetId) +
                "|category=" + std::to_string(it->second.category) +
                "|outcome=" + result.outcome + "\n");

    // Clear memory after outcome
    castMemory.erase(it);
}

// --- ParseActionPacket (incoming summary/outcome) ---
void CurePleasePlugin::ParseActionPacket(uint16_t id, uint32_t size, const uint8_t* data, bool outgoing) {
    BitReader reader(data, size);
    reader.setPosition(5 * 8);

    uint32_t actorId     = reader.readBits(32);
    uint8_t  targetCount = reader.readBits(6);
    uint8_t  resultSum   = reader.readBits(4);
    uint8_t  category    = reader.readBits(4);
    uint32_t spellId     = reader.readBits(32);
    uint32_t info        = reader.readBits(32);

    WriteToPipe("LOG|header|actor=" + std::to_string(actorId) +
                "|targets=" + std::to_string(targetCount) +
                "|results=" + std::to_string(resultSum) +
                "|category=" + std::to_string(category) +
                "|spell=" + std::to_string(spellId) + "\n");

    if (targetCount == 0) {
        // Summary packet: mark as pending
        auto it = castMemory.find(actorId);
        if (it != castMemory.end()) {
            it->second.category = category;
            WriteToPipe("ACTION|" + std::to_string(actorId) +
                        "|spell=" + std::to_string(spellId) +
                        "|target=" + std::to_string(it->second.lastTargetId) +
                        "|category=" + std::to_string(category) +
                        "|res_sum=" + std::to_string(resultSum) +
                        "|outcome=PENDING\n");
        } else if (category == 8) {
            // fallback if no outgoing memory
            rememberCast(actorId, spellId, 0, category);
            WriteToPipe("ACTION|" + std::to_string(actorId) +
                        "|spell=" + std::to_string(spellId) +
                        "|category=" + std::to_string(category) +
                        "|res_sum=" + std::to_string(resultSum) +
                        "|outcome=PENDING\n");
        }
        return;
    }

    // Outcome packet: parse per-target blocks
    for (int t = 0; t < targetCount; ++t) {
        uint32_t targetId          = reader.readBits(32);
        uint8_t  targetResultCount = reader.readBits(4);

        for (int r = 0; r < targetResultCount; ++r) {
            ParseResultBlock(reader, actorId, targetId, spellId, category, "ACTION");
        }
    }
}

// --- ParseResultBlock ---
void CurePleasePlugin::ParseResultBlock(BitReader& reader,
                                        uint32_t actorId,
                                        uint32_t targetId,
                                        uint32_t spellId,
                                        uint8_t category,
                                        const char* /*tag*/) {
    Result res{};
    res.miss      = reader.readBits(3);
    res.kind      = reader.readBits(2);
    res.subKind   = reader.readBits(12);
    res.infoBits  = reader.readBits(5);
    res.scale     = reader.readBits(5);
    res.value     = reader.readBits(17);
    res.messageId = reader.readBits(10);
    res.bitfield  = reader.readBits(31);

    res.hasProc = reader.readBits(1);
    if (res.hasProc) { reader.readBits(6); reader.readBits(4); reader.readBits(17); reader.readBits(10); }
    res.hasReact = reader.readBits(1);
    if (res.hasReact) { reader.readBits(6); reader.readBits(4); reader.readBits(14); reader.readBits(10); }

    // Outcome inference
    if (category == 4) {
        if (res.miss == 1) res.outcome = "RESISTED";
        else if (res.messageId == 0x9A) res.outcome = "INTERRUPTED";
        else res.outcome = "LANDED";
    } else {
        res.outcome = (res.miss == 1) ? "MISS" : "LANDED";
    }

    attachOutcome(actorId, targetId, res);

    WriteToPipe("LOG|" + std::to_string(actorId) + "|" + std::to_string(targetId) + "|" + std::to_string(spellId) +
                "|MSG=" + std::to_string(res.messageId) +
                "|VAL=" + std::to_string(res.value) +
                "|MISS=" + std::to_string(res.miss) +
                "|KIND=" + std::to_string(res.kind) +
                "|OUTCOME=" + res.outcome + "\n");
}

void CurePleasePlugin::ParseOutcomePacket(uint16_t id, uint32_t size, const uint8_t* data) {
    BitReader reader(data, size);
    reader.setPosition(0);

    uint32_t actorId   = reader.readBits(32);
    uint32_t targetId  = reader.readBits(32);
    uint8_t  resultCnt = reader.readBits(4);

    // Look up spell/category from memory
    auto it = castMemory.find(actorId);
    uint32_t spellId  = (it != castMemory.end()) ? it->second.spellId : 0;
    uint8_t  category = (it != castMemory.end()) ? it->second.category : 0;

    for (int r = 0; r < resultCnt; ++r) {
        ParseResultBlock(reader, actorId, targetId, spellId, category, "ACTION");
    }
}


// --- HandleIncomingPacket ---
bool CurePleasePlugin::HandleIncomingPacket(uint16_t id, uint32_t size, const uint8_t* data,
                                            uint8_t* modified, uint32_t sizeChunk,
                                            const uint8_t* dataChunk, bool injected, bool blocked) {
    //WriteToPipe("DEBUG|Incoming id=" + std::to_string(id) +
    //            "|size=" + std::to_string(size) + "\n");
    //if (myActorId == 0)    
        //InitMyActorId();


   if (id == 0x28) {
        ParseActionPacket(id, size, data);
    } else if (id == 0x0E) {
        ParseChatLogPacket(id, size, data);
    }
    return false;
}

// --- HandleOutgoingPacket ---
bool CurePleasePlugin::HandleOutgoingPacket(uint16_t id, uint32_t size, const uint8_t* data,
                                            uint8_t* /*modified*/, uint32_t /*sizeChunk*/,
                                            const uint8_t* /*dataChunk*/, bool /*injected*/, bool /*blocked*/) {
    if (id == 0x15) { // outgoing cast
        BitReader reader(data, size);
        reader.setPosition(0);

        uint32_t actorId  = reader.readBits(32);
        uint32_t targetId = reader.readBits(32);
        uint8_t  category = reader.readBits(4);
        uint32_t spellId  = reader.readBits(32);
        if (category == 8) {
            rememberCast(actorId, spellId, targetId, category);
        }
    }
    return false;
}






void CurePleasePlugin::ParseChatLogPacket(uint16_t id, uint32_t size, const uint8_t* data) {
    BitReader reader(data, size);
    reader.setPosition(0);

    uint16_t messageId = reader.readBits(16);
    uint32_t actorId   = reader.readBits(32);
    uint8_t  paramCount = reader.readBits(8);

    std::vector<uint32_t> params;
    for (int i = 0; i < paramCount; ++i) {
        params.push_back(reader.readBits(32));
    }

    // Filter: only process if this is me
    if (actorId != myActorId) {
        return; // skip external messages
    }

    auto it = actionMessages.find(messageId);
    std::string msgText = (it != actionMessages.end()) ? it->second : "Unknown message";

    WriteToPipe("CHAT|msg=" + std::to_string(messageId) +
                "|actor=" + std::to_string(actorId) +
                "|params=" + std::to_string(paramCount) +
                "|text=" + msgText + "\n");
}



void CurePleasePlugin::ParseChatPacket(uint16_t id, uint32_t size, const uint8_t* data)
{
std::string message;
const char* sjis = reinterpret_cast<const char*>(data + 0x20);
int wideLen = MultiByteToWideChar(932, MB_ERR_INVALID_CHARS, sjis, -1, nullptr, 0);
if (wideLen > 0) {
    std::wstring wide(wideLen, L'\0');
    MultiByteToWideChar(932, MB_ERR_INVALID_CHARS, sjis, -1, &wide[0], wideLen);

    int utf8Len = WideCharToMultiByte(CP_UTF8, 0,
                                      wide.c_str(), -1,
                                      nullptr, 0, nullptr, nullptr);
    if (utf8Len > 0) {
        std::string message(utf8Len, '\0');
        WideCharToMultiByte(CP_UTF8, 0,
                            wide.c_str(), -1,
                            &message[0], utf8Len,
                            nullptr, nullptr);

        // Forward decoded chat line with packet ID
        WriteToPipe("CHAT|" + std::to_string(id) + "|" + message + "\n");
    }
}


// Forward decoded chat line with packet ID
//WriteToPipe("CHAT|" + std::to_string(id) + "|" + message + "\n");






    const std::string afflict_str    = " is afflicted by ";
    const std::string wears_off_str1 = "'s ";
    const std::string wears_off_str2 = " effect wears off.";
    const std::string resist_str     = " resists the effect of ";

    // Debuff applied
    size_t pos_afflict = message.find(afflict_str);
    if (pos_afflict != std::string::npos) {
        std::string target_name = message.substr(0, pos_afflict);
        std::string debuff_name = message.substr(pos_afflict + afflict_str.length());
        if (!debuff_name.empty() && debuff_name.back() == '.')
            debuff_name.pop_back();

        for (const auto& pair : rdm_debuff_map) {
            for (const auto& name : pair.second) {
                if (debuff_name == name) {
                    WriteToPipe("DEBUFF_APPLIED|" + target_name + "|" + debuff_name + "\n");
                    return;
                }
            }
        }
    }

    // Buff faded
    size_t pos_wears_off1 = message.find(wears_off_str1);
    size_t pos_wears_off2 = message.find(wears_off_str2);
    if (pos_wears_off1 != std::string::npos &&
        pos_wears_off2 != std::string::npos &&
        pos_wears_off1 < pos_wears_off2)
    {
        std::string target_name = message.substr(0, pos_wears_off1);
        std::string buff_name   = message.substr(pos_wears_off1 + wears_off_str1.length(),
                                                 pos_wears_off2 - (pos_wears_off1 + wears_off_str1.length()));

        // RDM debuffs
        for (const auto& pair : rdm_debuff_map) {
            for (const auto& name : pair.second) {
                if (buff_name == name) {
                    WriteToPipe("DEBUFF_FADED|" + target_name + "|" + buff_name + "\n");
                    return;
                }
            }
        }

        // Dispelable monster buffs
        auto entMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
        if (entMgr) {
            for (int i = 0; i < 2048; ++i) {
                const char* name = entMgr->GetName(i);
                if (!name || target_name != name) continue;
                uint32_t serverId = entMgr->GetServerId(i);

                for (const auto& pair : dispel_defense_map) {
                    if (buff_name == pair.second) {
                        WriteToPipe("MOB_BUFF_FADED|" + std::to_string(serverId) + "|" + buff_name + "\n");
                        return;
                    }
                }
                for (const auto& pair : dispel_magic_map) {
                    if (buff_name == pair.second) {
                        WriteToPipe("MOB_BUFF_FADED|" + std::to_string(serverId) + "|" + buff_name + "\n");
                        return;
                    }
                }
                for (const auto& pair : dispel_evasion_map) {
                    if (buff_name == pair.second) {
                        WriteToPipe("MOB_BUFF_FADED|" + std::to_string(serverId) + "|" + buff_name + "\n");
                        return;
                    }
                }
            }
        }
    }

    // Resist
    size_t pos_resist = message.find(resist_str);
    if (pos_resist != std::string::npos) {
        // Chat lines are usually "The <target> resists the effect of <spell>."
        std::string target_name = message.substr(4, pos_resist - 4); // skip "The "
        std::string spell_name  = message.substr(pos_resist + resist_str.length());
        if (!spell_name.empty() && spell_name.back() == '.')
            spell_name.pop_back();

        WriteToPipe("DEBUFF_RESISTED|" + target_name + "|" + spell_name + "\n");
        return;
    }
}

bool CurePleasePlugin::Initialize(IAshitaCore* core, ILogManager* log, uint32_t id)
{
    m_AshitaCore = core;
    m_LogManager = log;

    InitMyActorId();

    try {
        m_Shutdown = false;
        m_PipeThread = std::thread(&CurePleasePlugin::PipeThread, this);
    } catch (...) {
        return false;
    }
    return true;
}

void CurePleasePlugin::Release()
{
    m_Shutdown = true;

    // Touch the pipe so ConnectNamedPipe unblocks if waiting
    HANDLE hDummyPipe = CreateFileW(PipeName.c_str(), GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
    if (hDummyPipe != INVALID_HANDLE_VALUE)
        CloseHandle(hDummyPipe);

    if (m_PipeThread.joinable())
        m_PipeThread.join();

    if (m_hPipe != INVALID_HANDLE_VALUE)
    {
        DisconnectNamedPipe(m_hPipe);
        CloseHandle(m_hPipe);
        m_hPipe = INVALID_HANDLE_VALUE;
    }

    m_AshitaCore = nullptr;
    m_LogManager = nullptr;
}



// Plugin exports
extern "C" __declspec(dllexport) IPlugin* __stdcall expCreatePlugin(const char* args) {
    return new CurePleasePlugin();
}

extern "C" __declspec(dllexport) double __stdcall expGetInterfaceVersion(void) {
    return ASHITA_INTERFACE_VERSION;
}
