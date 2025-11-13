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

const char* g_PluginName = "Miraculix";
const char* g_PluginAuthor = "Jules";
const char* g_PluginDescription = "Packet listener for Miraculix.";
const double g_PluginVersion = 1.0;

std::wstring PipeName = L"\\\\.\\pipe\\MiraculixPipe";

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

    bool HandleIncomingPacket(uint16_t id, uint32_t size, const uint8_t* data, uint8_t* modified,
                              uint32_t sizeChunk, const uint8_t* dataChunk, bool injected, bool blocked) override {
        if (id == 0x28 && size >= 32) {
            BitReader reader(data, size);
            reader.setPosition(5);

            uint32_t actorId     = reader.readBits(32);
            uint8_t  targetCount = reader.readBits(6);
            uint32_t resultCode = reader.readBits(4); // resultCount
            uint8_t category = reader.readBits(4);
            uint32_t spellId     = reader.readBits(16); // The spellId is here, even if it's a uint32, it's read as 16 bits in this context.
            reader.readBits(16); //
            reader.readBits(32); // info
            
            // This is the start of the targets block. No need to setPosition(32).
            if (category == 4 || category == 256) { // 4 for magic, 256 for some actions
                for (int i = 0; i < targetCount; ++i) {
                    uint32_t targetId = reader.readBits(32);
                    
                    // Each target has a block of animation data. We need to skip it.
                    // The animation block is 12 bytes (3 x 32 bits).
                    reader.readBits(32); // Animation data
                    reader.readBits(32); // Animation data
                    reader.readBits(32); // Animation data

                    std::string message;
                    if (resultCode == 1) { // Resisted
                        message = "DEBUFF_RESISTED|" + std::to_string(actorId) + "|" + std::to_string(targetId) + "|" + std::to_string(spellId) + "\n";
                    }
                    else if (resultCode == 2) { // Interrupted
                        message = "DEBUFF_INTERRUPTED|" + std::to_string(actorId) + "|" + std::to_string(targetId) + "|" + std::to_string(spellId) + "\n";
                    }
                    else {
                        message = "ACTION|" + std::to_string(actorId) + "|" + std::to_string(targetId) + "|" + std::to_string(spellId) + "\n";
                    }
                    WriteToPipe(message);
                }
            } else if (category == 8) { // Monster abilities
                std::string buff_name;
                auto it_def = dispel_defense_map.find(static_cast<uint16_t>(spellId));
                if (it_def != dispel_defense_map.end()) {
                    buff_name = it_def->second;
                }
                auto it_mag = dispel_magic_map.find(spellId);
                if (it_mag != dispel_magic_map.end()) {
                    buff_name = it_mag->second;
                }
                auto it_eva = dispel_evasion_map.find(spellId);
                if (it_eva != dispel_evasion_map.end()) {
                    buff_name = it_eva->second;
                }

                if (!buff_name.empty()) {
                    WriteToPipe("MOB_BUFF_APPLIED|" + std::to_string(actorId) + "|" + buff_name + "\n");
                }
            }
        }
        // Handle incoming chat log messages (Packet ID: 0x00E)
        else if (id == 0x00E && size > 4) {
            std::string message(reinterpret_cast<const char*>(data) + 4);
            std::string afflict_str = " is afflicted by ";
            std::string wears_off_str1 = "'s ";
            std::string wears_off_str2 = " effect wears off.";

            size_t pos_afflict = message.find(afflict_str);
            if (pos_afflict != std::string::npos) {
                std::string target_name = message.substr(0, pos_afflict);
                std::string debuff_name = message.substr(pos_afflict + afflict_str.length());
                if (!debuff_name.empty() && debuff_name.back() == '.') {
                    debuff_name.pop_back();
                }

                for (const auto& pair : rdm_debuff_map) {
                    for (const auto& name : pair.second) {
                        if (debuff_name == name) {
                            WriteToPipe("DEBUFF_APPLIED|" + target_name + "|" + debuff_name + "\n");
                            return false;
                        }
                    }
                }
            }

            size_t pos_wears_off1 = message.find(wears_off_str1);
            size_t pos_wears_off2 = message.find(wears_off_str2);
            if (pos_wears_off1 != std::string::npos && pos_wears_off2 != std::string::npos && pos_wears_off1 < pos_wears_off2) {
                std::string target_name = message.substr(0, pos_wears_off1);
                std::string buff_name = message.substr(pos_wears_off1 + wears_off_str1.length(), pos_wears_off2 - (pos_wears_off1 + wears_off_str1.length()));

                for (const auto& pair : rdm_debuff_map) {
                    for (const auto& name : pair.second) {
                        if (buff_name == name) {
                            auto entMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
                            if (entMgr) {
                                for (int i = 0; i < 2048; ++i) {
                                    const char* entName = entMgr->GetName(i);
                                    if (entName && target_name == entName) {
                                        uint32_t targetId = entMgr->GetServerId(i);
                                        uint16_t spellId = pair.first;
                                        WriteToPipe("DEBUFF_FADED|" + std::to_string(targetId) + "|" + std::to_string(spellId) + "\n");
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }

                // Check if the faded buff is a dispelable monster buff
                for (const auto& pair : dispel_defense_map) {
                    if (buff_name == pair.second) {
                        auto entMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
                        if (entMgr) {
                            for (int i = 0; i < 2048; ++i) {
                                const char* name = entMgr->GetName(i);
                                if (name && target_name == name) {
                                    WriteToPipe("MOB_BUFF_FADED|" + std::to_string(entMgr->GetServerId(i)) + "|" + buff_name + "\n");
                                    return false;
                                }
                            }
                        }
                    }
                }
                for (const auto& pair : dispel_magic_map) {
                    if (buff_name == pair.second) {
                         auto entMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
                        if (entMgr) {
                            for (int i = 0; i < 2048; ++i) {
                                const char* name = entMgr->GetName(i);
                                if (name && target_name == name) {
                                    WriteToPipe("MOB_BUFF_FADED|" + std::to_string(entMgr->GetServerId(i)) + "|" + buff_name + "\n");
                                    return false;
                                }
                            }
                        }
                    }
                }
                for (const auto& pair : dispel_evasion_map) {
                    if (buff_name == pair.second) {
                         auto entMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
                        if (entMgr) {
                            for (int i = 0; i < 2048; ++i) {
                                const char* name = entMgr->GetName(i);
                                if (name && target_name == name) {
                                    WriteToPipe("MOB_BUFF_FADED|" + std::to_string(entMgr->GetServerId(i)) + "|" + buff_name + "\n");
                                    return false;
                                }
                            }
                        }
                    }
                }
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
