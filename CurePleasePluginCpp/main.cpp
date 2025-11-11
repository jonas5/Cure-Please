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
    {23, {"Dia", "Dia II", "Dia III"}},
    {71, {"Paralyze", "Paralyze II"}},
    {72, {"Slow", "Slow II"}},
    {73, {"Silence"}},
    {74, {"Blind", "Blind II"}},
    {75, {"Bind", "Bind II"}},
    {89, {"Gravity", "Gravity II"}},
    {134, {"Bio", "Bio II", "Bio III"}},
    {149, {"Burn"}},
    {151, {"Frost"}},
    {153, {"Shock"}},
    {154, {"Rasp"}},
    {155, {"Choke"}},
    {156, {"Drown"}}
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
    ILogManager* m_LogManager = nullptr;
    HANDLE m_hPipe = INVALID_HANDLE_VALUE;
    std::thread m_PipeThread;
    std::mutex m_PipeMutex;
    std::atomic<bool> m_Shutdown = false;
    std::string m_lastSpellName;
    std::string m_lastSpellTargetName;
    bool m_PipeConnected = false;
    bool m_isZoning = false;
    std::map<std::string, std::map<std::string, std::chrono::steady_clock::time_point>> m_spellTimers;

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
        char buffer[1024];
        DWORD bytesRead;

        while (!m_Shutdown) {
            m_hPipe = CreateNamedPipeW(PipeName.c_str(), PIPE_ACCESS_DUPLEX,
                PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
                1, 1024, 1024, 0, NULL);

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
                    if (ReadFile(m_hPipe, buffer, sizeof(buffer) - 1, &bytesRead, NULL)) {
                        buffer[bytesRead] = '\0';
                        std::string received(buffer);
                        // Simple parsing logic
                        if (received.rfind("SETTING|", 0) == 0) {
                            std::string setting = received.substr(8);
                            size_t del = setting.find("=");
                            if (del != std::string::npos) {
                                std::string key = setting.substr(0, del);
                                std::string value = setting.substr(del + 1);
                                int new_cooldown = std::stoi(value);

                                std::vector<std::string> spells_to_update;
                                if (key == "debuffDiaBioCooldown") {
                                    spells_to_update.push_back("Dia");
                                    spells_to_update.push_back("Bio");
                                }
                                else if (key == "debuffElementalCooldown") {
                                    spells_to_update.push_back("Burn");
                                    spells_to_update.push_back("Frost");
                                    spells_to_update.push_back("Shock");
                                    spells_to_update.push_back("Rasp");
                                    spells_to_update.push_back("Choke");
                                    spells_to_update.push_back("Drown");
                                }
                                else if (key == "debuffParalyzeCooldown") spells_to_update.push_back("Paralyze");
                                else if (key == "debuffSilenceCooldown") spells_to_update.push_back("Silence");
                                else if (key == "debuffBlindCooldown") spells_to_update.push_back("Blind");
                                else if (key == "debuffGravityCooldown") spells_to_update.push_back("Gravity");
                                else if (key == "debuffSlowCooldown") spells_to_update.push_back("Slow");
                                else if (key == "debuffBindCooldown") spells_to_update.push_back("Bind");

                                if (!spells_to_update.empty()) {
                                    for (const auto& spell_name : spells_to_update) {
                                        for (auto& pair : spells) {
                                            if (pair.second.name.rfind(spell_name, 0) == 0) {
                                                pair.second.cooldown = new_cooldown;
                                                std::stringstream log;
                                                log << "LOG|" << GetTimestamp() << " [SETTINGS] Updated cooldown for " << pair.second.name << " to " << new_cooldown << "s.\n";
                                                WriteToPipe(log.str());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else {
                        if (GetLastError() != ERROR_IO_PENDING) {
                            break; // Error or pipe closed
                        }
                    }
                    std::this_thread::sleep_for(std::chrono::milliseconds(100));
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
        m_LogManager = logger;
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
        std::string cmd(command);
        if (cmd.rfind("cdstatus", 0) == 0) {
            std::stringstream ss(cmd);
            std::string arg;
            std::vector<std::string> args;
            while (ss >> arg) {
                args.push_back(arg);
            }

            if (args.size() == 3) {
                std::string targetName = args[1];
                std::string spellName = args[2];
                auto target_it = m_spellTimers.find(targetName);
                if (target_it != m_spellTimers.end()) {
                    auto spell_it = target_it->second.find(spellName);
                    if (spell_it != target_it->second.end()) {
                        auto now = std::chrono::steady_clock::now();
                        auto elapsed = std::chrono::duration_cast<std::chrono::seconds>(now - spell_it->second).count();
                        int cooldown = 0;
                        for (const auto& pair : spells) {
                            if (pair.second.name == spellName) {
                                cooldown = pair.second.cooldown;
                                break;
                            }
                        }
                        long long remaining = cooldown - elapsed;
                        if (remaining > 0) {
                            m_AshitaCore->GetChatManager()->Writef(121, false, ("Cooldown for " + spellName + " on " + targetName + ": " + std::to_string(remaining) + "s").c_str());
                        }
                        else {
                            m_AshitaCore->GetChatManager()->Writef(121, false, ("Cooldown for " + spellName + " on " + targetName + " is up.").c_str());
                        }
                    }
                    else {
                        m_AshitaCore->GetChatManager()->Writef(121, false, ("No cooldown found for " + spellName + " on " + targetName).c_str());
                    }
                }
                else {
                    m_AshitaCore->GetChatManager()->Writef(121, false, ("No cooldowns found for " + targetName).c_str());
                }
            }
            return true;
        }
        return false;
    }

    bool HandleIncomingPacket(uint16_t id, uint32_t size, const uint8_t* data, uint8_t* modified, uint32_t sizeChunk, const uint8_t* dataChunk, bool injected, bool blocked) override
    {
        // Packet 0x00A: Zone Change
        if (id == 0x00A && size >= 4)
        {
            if (!m_isZoning) {
                m_isZoning = true;
                m_spellTimers.clear();
                m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", "[Miraculix] Zoning detected, clearing all spell timers.");
                WriteToPipe("LOG|Zoning detected, clearing all spell timers.\n");
            }
        }
        else if (id != 0x00A)
        {
            m_isZoning = false;
        }

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
                    m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", ("[Miraculix] DISPEL_REQUESTED: " + actorName + " used " + it_def->second).c_str());
                    WriteToPipe("DISPEL_REQUESTED|" + actorName + "|" + it_def->second + "\n");
                    return false;
                }

                // Check Magic Shield map
                auto it_mag = dispel_magic_map.find(abilityId);
                if (it_mag != dispel_magic_map.end())
                {
                    m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", ("[Miraculix] DISPEL_REQUESTED: " + actorName + " used " + it_mag->second).c_str());
                    WriteToPipe("DISPEL_REQUESTED|" + actorName + "|" + it_mag->second + "\n");
                    return false;
                }

                // Check Evasion Boost map
                auto it_eva = dispel_evasion_map.find(abilityId);
                if (it_eva != dispel_evasion_map.end())
                {
                    m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", ("[Miraculix] DISPEL_REQUESTED: " + actorName + " used " + it_eva->second).c_str());
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
            m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", ("[Miraculix] Spell action packet, category: " + std::to_string(category)).c_str());

            // Category 4: Magic Finish
            if (category == 4 || category == 256)
            {
                uint32_t actorId = *reinterpret_cast<const uint32_t*>(data + 4);
                uint16_t spellId = *reinterpret_cast<const uint16_t*>(data + 28) & 0x3FF;

                std::string actorName = GetEntityNameById(actorId);
                std::string spellName = ResolveSpellName(spellId);
                m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", ("[Miraculix] Magic finish: " + actorName + " cast " + spellName).c_str());

                auto it = spells.find(spellId);
                if (it != spells.end())
                {
                    const auto& spell = it->second;
                    uint8_t targetCount = 1;
                    if (spell.aoe > 0) {
                        targetCount = data[13];
                    }

                    for (uint8_t i = 0; i < targetCount; ++i)
                    {
                        uint32_t targetId = *reinterpret_cast<const uint32_t*>(data + 32 + i * 4);
                        std::string targetName = GetEntityNameById(targetId);

                        if (targetName == "Unknown" || targetName == "None") continue;

                        std::stringstream log;
                        log << "LOG|" << GetTimestamp() << " [MAGIC] " << spellName << " (" << spellId << ")"
                            << " - Actor: " << actorName << ", Target[" << (int)i << "]: " << targetName << "\n";
                        WriteToPipe(log.str());

                        if (spell.cooldown > 0)
                        {
                            m_spellTimers[targetName][spell.name] = std::chrono::steady_clock::now();
                            m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", ("[Miraculix] BUFF_APPLIED: " + spell.name + " on " + targetName).c_str());
                            WriteToPipe("BUFF_APPLIED|" + targetName + "|" + spell.name + "\n");
                        }
                    }
                }
                else
                {
                    // Original behavior for spells not in our map
                    uint32_t targetId = *reinterpret_cast<const uint32_t*>(data + 32);
                    std::string targetName = GetEntityNameById(targetId);
                    std::stringstream log;
                    log << "LOG|" << GetTimestamp() << " [MAGIC] " << spellName << " (" << spellId << ")"
                        << " - Actor: " << actorName << ", Target[0]: " << targetName << "\n";
                    WriteToPipe(log.str());
                }

                m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", "[Miraculix] CAST_FINISH");
                WriteToPipe("CAST_FINISH\n");
            }
            // Category 8: Action Message (includes resists, interruptions, etc.)
            else if (category == 8)
            {
                if (param == 24931) {
                    m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", "[Miraculix] CAST_BLOCKED");
                    WriteToPipe("CAST_BLOCKED\n");
                }
                else if (param == 28787) {
                    m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", "[Miraculix] CAST_INTERRUPT");
                    WriteToPipe("CAST_INTERRUPT\n");
                }
                else if (param == 258) // Magic is resisted.
                {
                    uint16_t spellId = *reinterpret_cast<const uint16_t*>(data + 20);
                    uint32_t targetId = *reinterpret_cast<const uint32_t*>(data + 24);
                    std::string spellName = ResolveSpellName(spellId);
                    std::string targetName = GetEntityNameById(targetId);
                    if (!spellName.empty() && !targetName.empty() && targetName != "None")
                    {
                        m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", ("[Miraculix] DEBUFF_RESISTED: " + spellName + " on " + targetName).c_str());
                        WriteToPipe("DEBUFF_RESISTED|" + targetName + "|" + spellName + "\n");
                        m_spellTimers[targetName].erase(spellName);
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
                m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", ("[Miraculix] DEBUFF_APPLIED: " + debuff_name + " on " + player_name).c_str());
                WriteToPipe("DEBUFF_APPLIED|" + player_name + "|" + debuff_name + "\n");
                return false;
            }

            size_t pos_wears_off1 = message.find(wears_off_str1);
            size_t pos_wears_off2 = message.find(wears_off_str2);
            if (pos_wears_off1 != std::string::npos && pos_wears_off2 != std::string::npos && pos_wears_off1 < pos_wears_off2)
            {
                std::string player_name = message.substr(0, pos_wears_off1);
                std::string spell_name = message.substr(pos_wears_off1 + wears_off_str1.length(), pos_wears_off2 - (pos_wears_off1 + wears_off_str1.length()));

                bool is_rdm_debuff = false;
                for (const auto& pair : rdm_debuff_map) {
                    for (const auto& name : pair.second) {
                        if (name == spell_name) {
                            is_rdm_debuff = true;
                            break;
                        }
                    }
                    if (is_rdm_debuff) break;
                }

                if (is_rdm_debuff) {
                    m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", ("[Miraculix] DEBUFF_FADED: " + spell_name + " from " + player_name).c_str());
                    WriteToPipe("DEBUFF_FADED|" + player_name + "|" + spell_name + "\n");
                    m_spellTimers[player_name].erase(spell_name);
                }
                else {
                    // Fallback to original logic for everything else.
                    bool is_buff = false;
                    for (const auto& pair : spells) {
                        if (pair.second.name == spell_name && pair.second.cooldown > 0) {
                            is_buff = true;
                            break;
                        }
                    }

                    if (is_buff) {
                        m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", ("[Miraculix] BUFF_FADED: " + spell_name + " from " + player_name).c_str());
                        WriteToPipe("BUFF_FADED|" + player_name + "|" + spell_name + "\n");
                        m_spellTimers[player_name].erase(spell_name);
                    }
                    else {
                        m_LogManager->Log(static_cast<uint32_t>(Ashita::LogLevel::Info), "Miraculix", ("[Miraculix] DEBUFF_FADED (non-RDM): " + spell_name + " from " + player_name).c_str());
                        WriteToPipe("DEBUFF_FADED|" + player_name + "|" + spell_name + "\n");
                    }
                }
                return false;
            }
        }
        return false;
    }

    bool HandleOutgoingPacket(uint16_t id, uint32_t size, const uint8_t* data, uint8_t* modified,
                              uint32_t sizeChunk, const uint8_t* dataChunk, bool injected, bool blocked) override {
        // Packet 0x15: Action Start
        if (id == 0x15 && size >= 8) {
            uint16_t category = *reinterpret_cast<const uint16_t*>(data);
            uint16_t spellId = *reinterpret_cast<const uint16_t*>(data + 4);

            // Category 8: Magic spell
            if (category == 8) {
                auto it = spells.find(spellId);
                if (it != spells.end()) {
                    const auto& spell_info = it->second;
                    if (spell_info.cooldown > 0) {
                        uint32_t targetId = *reinterpret_cast<const uint32_t*>(data + 8);
                        std::string targetName = GetEntityNameById(targetId);

                        if (!targetName.empty() && targetName != "Unknown" && targetName != "None") {
                            auto now = std::chrono::steady_clock::now();
                            if (m_spellTimers[targetName].count(spell_info.name)) {
                                auto last_cast = m_spellTimers[targetName][spell_info.name];
                                auto elapsed = std::chrono::duration_cast<std::chrono::seconds>(now - last_cast).count();
                                if (elapsed < spell_info.cooldown) {
                                    return true; // Block the cast
                                }
                            }
                            // If we are not blocking, we will let the cast go through and the incoming packet handler will set the timer.
                        }
                    }
                }
            }
        }
        return false;
    }
};

extern "C" __declspec(dllexport) IPlugin* __stdcall expCreatePlugin(const char* args) {
    return new CurePleasePlugin();
}

extern "C" __declspec(dllexport) double __stdcall expGetInterfaceVersion(void) {
    return ASHITA_INTERFACE_VERSION;
}
