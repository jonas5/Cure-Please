#include "../Ashita-v4beta/plugins/sdk/Ashita.h"
#include <windows.h>
#include <string>
#include <thread>
#include <mutex>
#include <chrono>
#include <iomanip>
#include <sstream>
#include <atomic>
#include "spells.h"
#include "BitReader.hpp"
#include <vector>
#include <map>


const char* g_PluginName = "Miraculix";
const char* g_PluginAuthor = "Jules";
const char* g_PluginDescription = "Packet listener for Miraculix.";
const double g_PluginVersion = 1.0;

std::wstring PipeName = L"\\\\.\\pipe\\MiraculixPipe";


// Define the maps for debuffs and dispelable buffs
static const std::map<int, std::vector<std::string>> rdm_debuff_map = {
    {134, {"Dia", "Dia II", "Dia III"}},
    {135, {"Bio", "Bio II", "Bio III"}},
    {4, {"Paralyze", "Paralyze II"}},
    {13, {"Slow", "Slow II"}},
    {5, {"Blind", "Blind II"}},
    {12, {"Gravity", "Gravity II"}},
    {6, {"Silence"}},
    {11, {"Bind"}},
    {130, {"Choke"}},
    {128, {"Burn"}},
    {132, {"Shock"}},
    {131, {"Rasp"}},
    {129, {"Frost"}},
    {133, {"Drown"}}
};




// Category: Defense Boost
inline std::unordered_map<uint16_t, std::string> dispel_defense_map = {
    {872, "Harden Shell"}, {873, "Sand Shield"}, {874, "Scutum"}, {547, "Cocoon"},
    {875, "Scissor Guard"}, {876, "Promyvion Barrier"}, {877, "Barrier Tusk"},
    {878, "Arm Block"}, {879, "Shell Guard"}, {880, "Particle Shield"},
    {881, "Amber Scutum"}, {882, "Aura of Persistence"}, {883, "Hexagon Belt"},
    {884, "Parry"}, {885, "Shiko no Mitate"}, {886, "Molluscous Mutation"},
    {887, "Reactor Cool"}
};

// Category: Magic Shield
inline std::unordered_map<uint16_t, std::string> dispel_magic_map = {
    {888, "Magic Barrier"}, {889, "Perfect Defense"}, {890, "Polar Bulwark"},
    {891, "Spectral Barrier"}, {892, "Mind Wall"}, {893, "Discharger"},
    {894, "Bastion of Twilight"}, {895, "Mana Screen"}, {896, "Hydro Blast"},
    {897, "Shadow Lord (Magic Stance)"}, {898, "Immortal Shield"}
};

// Category: Evasion Boost
inline std::unordered_map<uint16_t, std::string> dispel_evasion_map = {
    {899, "Sand Veil"}, {900, "Rhino Guard"}, {901, "Rabid Dance"},
    {902, "Material Fend"}, {903, "Secretion"}, {904, "Warm-Up"},
    {905, "Water Shield"}, {906, "Feather Barrier"}, {907, "Evasion"},
    {908, "Hard Membrane"}, {909, "Sigh"}, {910, "Mirage"},
    {911, "Wind Wall"}
};



std::string GetTimestamp()
{
    auto now = std::chrono::system_clock::now();
    auto in_time_t = std::chrono::system_clock::to_time_t(now);
    auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()) % 1000;
    std::tm buf;
    localtime_s(&buf, &in_time_t);
    std::stringstream ss;
    ss << std::put_time(&buf, "[%H:%M:%S") << '.' << std::setw(3) << std::setfill('0') << ms.count() << "]";
    return ss.str();
}

class CurePleasePlugin : public IPlugin {
private:
    IAshitaCore* m_AshitaCore = nullptr;
    HANDLE m_hPipe = INVALID_HANDLE_VALUE;
    std::thread m_PipeThread;
    std::mutex m_PipeMutex;
    std::atomic<bool> m_Shutdown = false;
    std::string m_lastSpellName;
    std::string m_lastSpellTargetName;
    bool m_PipeConnected = false;
    bool m_isZoning = false;

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

    void WriteToPipe(const std::string& message)
    {
        std::lock_guard<std::mutex> lock(m_PipeMutex);
        if (!m_PipeConnected || m_hPipe == INVALID_HANDLE_VALUE) return;
        DWORD bytesWritten = 0;
        WriteFile(m_hPipe, message.c_str(), message.length(), &bytesWritten, NULL);
    }

    void PipeThread()
    {
        while (!m_Shutdown) {
            m_hPipe = CreateNamedPipeW(PipeName.c_str(), PIPE_ACCESS_OUTBOUND,
                PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT, 1, 4096, 4096, 0, NULL);

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
                WriteToPipe("LOG|" + GetTimestamp() + " Packet listener v" + std::to_string(g_PluginVersion) + " connected.\n");

                while (!m_Shutdown) {
                    if (!PeekNamedPipe(m_hPipe, NULL, 0, NULL, NULL, NULL)) {
                        DWORD err = GetLastError();
                        if (err == ERROR_BROKEN_PIPE || err == ERROR_PIPE_NOT_CONNECTED) break;
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

public:
    CurePleasePlugin() = default;
    ~CurePleasePlugin() {
        m_Shutdown = true;
        if (m_PipeThread.joinable()) m_PipeThread.join();
    }

    const char* GetName() const override { return g_PluginName; }
    const char* GetAuthor() const override { return g_PluginAuthor; }
    const char* GetDescription() const override { return g_PluginDescription; }
    double GetVersion() const override { return g_PluginVersion; }
    uint32_t GetFlags() const override { return (uint32_t)Ashita::PluginFlags::UsePackets; }

    bool Initialize(IAshitaCore* core, ILogManager* logger, uint32_t id) override {
        m_AshitaCore = core;
        m_PipeThread = std::thread(&CurePleasePlugin::PipeThread, this);
        return true;
    }

    void Release() override {
        m_Shutdown = true;
        HANDLE hDummyPipe = CreateFileW(PipeName.c_str(), GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
        if (hDummyPipe != INVALID_HANDLE_VALUE) CloseHandle(hDummyPipe);
        if (m_PipeThread.joinable()) m_PipeThread.join();
    }

    bool HandleCommand(int32_t mode, const char* command, bool injected) override {
        return false;
    }

    bool HandleIncomingPacket(uint16_t id, uint32_t size, const uint8_t* data, uint8_t* modified, uint32_t sizeChunk, const uint8_t* dataChunk, bool injected, bool blocked) override
    {
        // Packet 0x29: Action (Ability)
        if (id == 0x29 && size >= 16)
        {
            uint32_t actorId = *reinterpret_cast<const uint32_t*>(data + 4);
            uint16_t abilityId = *reinterpret_cast<const uint16_t*>(data + 12);
            std::string actorName = GetEntityNameById(actorId);

            if (!actorName.empty() && actorName != "Unknown" && actorName != "None")
            {
                // Check Defense Boost map
                auto it_def = dispel_defense_map.find(abilityId);
                if (it_def != dispel_defense_map.end())
                {
                    WriteToPipe("DISPEL_REQUESTED|" + actorName + "|" + it_def->second + "\n");
                    return false;
                }

                // Check Magic Shield map
                auto it_mag = dispel_magic_map.find(abilityId);
                if (it_mag != dispel_magic_map.end())
                {
                    WriteToPipe("DISPEL_REQUESTED|" + actorName + "|" + it_mag->second + "\n");
                    return false;
                }

                // Check Evasion Boost map
                auto it_eva = dispel_evasion_map.find(abilityId);
                if (it_eva != dispel_evasion_map.end())
                {
                    WriteToPipe("DISPEL_REQUESTED|" + actorName + "|" + it_eva->second + "\n");
                    return false;
                }
            }
        }

        // Packet 0x28: Spell Action
        if (id == 0x28 && size >= 32)
        {
            uint16_t category = *reinterpret_cast<const uint16_t*>(data + 8);
            uint32_t param = *reinterpret_cast<const uint32_t*>(data + 12);

            // Category 4: Magic Finish
            if (category == 4)
            {
                uint32_t actorId = *reinterpret_cast<const uint32_t*>(data + 4);
                uint16_t spellId = *reinterpret_cast<const uint16_t*>(data + 28) & 0x3FF;
                uint32_t targetId = *reinterpret_cast<const uint32_t*>(data + 32);

                std::string actorName = GetEntityNameById(actorId);
                std::string targetName = GetEntityNameById(targetId);
                std::string spellName = ResolveSpellName(spellId);

                std::stringstream log;
                log << "LOG|" << GetTimestamp() << " [MAGIC] " << spellName << " (" << spellId << ")"
                    << " - Actor: " << actorName << ", Target[0]: " << targetName << "\n";
                WriteToPipe(log.str());

                WriteToPipe("CAST_FINISH\n");
            }
            // Category 8: Action Message (includes resists, interruptions, etc.)
            else if (category == 8)
            {
                if (param == 24931) WriteToPipe("CAST_BLOCKED\n");
                else if (param == 28787) WriteToPipe("CAST_INTERRUPT\n");
                else if (param == 258) // Magic is resisted.
                {
                    uint16_t spellId = *reinterpret_cast<const uint16_t*>(data + 20);
                    uint32_t targetId = *reinterpret_cast<const uint32_t*>(data + 24);
                    std::string spellName = ResolveSpellName(spellId);
                    std::string targetName = GetEntityNameById(targetId);
                    if (!spellName.empty() && !targetName.empty() && targetName != "None")
                    {
                        WriteToPipe("DEBUFF_RESISTED|" + targetName + "|" + spellName + "\n");
                    }
                }
            }
        }
        // Packet 0x00E: Chat Log
        else if (id == 0x00E && size > 4)
        {
            std::string message(reinterpret_cast<const char*>(data) + 4);

            std::string afflict_str = " is afflicted with the effect of ";
            std::string wears_off_str1 = "'s ";
            std::string wears_off_str2 = " effect wears off.";

            size_t pos_afflict = message.find(afflict_str);
            if (pos_afflict != std::string::npos)
            {
                size_t end_of_player_name = message.rfind(" ", pos_afflict - 1);
                if (end_of_player_name == std::string::npos) end_of_player_name = 0; else end_of_player_name++;
                std::string player_name = message.substr(end_of_player_name, pos_afflict - end_of_player_name);
                std::string debuff_name = message.substr(pos_afflict + afflict_str.length());
                if (!debuff_name.empty() && debuff_name.back() == '.')
                {
                    debuff_name.pop_back();
                }
                WriteToPipe("DEBUFF_APPLIED|" + player_name + "|" + debuff_name + "\n");
                return false;
            }

            size_t pos_wears_off1 = message.find(wears_off_str1);
            size_t pos_wears_off2 = message.find(wears_off_str2);
            if (pos_wears_off1 != std::string::npos && pos_wears_off2 != std::string::npos && pos_wears_off1 < pos_wears_off2)
            {
                std::string player_name = message.substr(0, pos_wears_off1);
                std::string debuff_name = message.substr(pos_wears_off1 + wears_off_str1.length(), pos_wears_off2 - (pos_wears_off1 + wears_off_str1.length()));
                WriteToPipe("DEBUFF_FADED|" + player_name + "|" + debuff_name + "\n");
                return false;
            }
        }
        return false;
    }

    bool HandleOutgoingPacket(uint16_t id, uint32_t size, const uint8_t* data, uint8_t* modified,
                              uint32_t sizeChunk, const uint8_t* dataChunk, bool injected, bool blocked) override {
        return false;
    }
};

extern "C" __declspec(dllexport) IPlugin* __stdcall expCreatePlugin(const char* args) {
    return new CurePleasePlugin();
}

extern "C" __declspec(dllexport) double __stdcall expGetInterfaceVersion(void) {
    return ASHITA_INTERFACE_VERSION;
}