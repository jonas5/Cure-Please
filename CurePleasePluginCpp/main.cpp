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

const char* g_PluginName = "CurePleasePluginCpp";
const char* g_PluginAuthor = "Jules";
const char* g_PluginDescription = "Packet listener for CurePlease.";
const double g_PluginVersion = 2.3;

std::wstring PipeName = L"\\\\.\\pipe\\CurePleasePipe";

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
            reader.readBits(4); // resultCount
            reader.readBits(4); // category
            uint32_t spellId     = reader.readBits(32);
            reader.readBits(32); // info

            std::string spellName = ResolveSpellName(spellId);
            std::string actorName = GetEntityNameById(actorId);

            for (uint8_t i = 0; i < targetCount; ++i) {
                uint32_t targetId = reader.readBits(32);
                std::string targetName = GetEntityNameById(targetId);

                std::stringstream log;
                log << "LOG|" << GetTimestamp() << " [MAGIC] " << spellName << " (" << spellId << ")"
                    << " - Actor: " << actorName << ", Target[" << static_cast<int>(i) << "]: " << targetName << "\n";

                WriteToPipe(log.str());

                uint8_t resultCount = reader.readBits(4);
                for (uint8_t r = 0; r < resultCount; ++r) {
                    reader.readBits(3 + 2 + 12 + 5 + 5 + 17 + 10 + 31); // result block
                    if (reader.readBits(1)) reader.readBits(6 + 4 + 17 + 10); // proc
                    if (reader.readBits(1)) reader.readBits(6 + 4 + 14 + 10); // react
                }
            }

            if (spellName.find("Unknown") != std::string::npos) {
                WriteToPipe("DEBUG|Unmapped Spell ID: " + std::to_string(spellId) + "\n");
            }
            if (actorName == "Unknown") {
                WriteToPipe("DEBUG|Missing actor resolution - actorId: " + std::to_string(actorId) + "\n");
            }
        }
        // Handle incoming chat log messages (Packet ID: 0x00E)
        else if (id == 0x00E && size > 4) {
            // The chat message starts at offset 4. We can cast the data to a C-style string.
            // Note: This string may contain non-printable control codes for color, etc.
            // We rely on the fact that the player/debuff names and key phrases are standard ASCII.
            std::string message(reinterpret_cast<const char*>(data) + 4);

            // Define the phrases we are looking for.
            std::string afflict_str = " is afflicted by ";
            std::string wears_off_str1 = "'s ";
            std::string wears_off_str2 = " effect wears off.";

            // Check for the "afflicted" message pattern
            size_t pos_afflict = message.find(afflict_str);
            if (pos_afflict != std::string::npos) {
                std::string player_name = message.substr(0, pos_afflict);
                std::string debuff_name = message.substr(pos_afflict + afflict_str.length());

                // Remove the trailing period if it exists.
                if (!debuff_name.empty() && debuff_name.back() == '.') {
                    debuff_name.pop_back();
                }

                // We only care about specific debuffs for this feature.
                if (debuff_name == "Paralysis" || debuff_name == "Poison" || debuff_name == "Silence" || debuff_name == "Blindness") {
                    WriteToPipe("DEBUFF_APPLIED|" + player_name + "|" + debuff_name + "\n");
                }
                return false; // Packet handled
            }

            // Check for the "wears off" message pattern
            size_t pos_wears_off1 = message.find(wears_off_str1);
            size_t pos_wears_off2 = message.find(wears_off_str2);
            if (pos_wears_off1 != std::string::npos && pos_wears_off2 != std::string::npos && pos_wears_off1 < pos_wears_off2) {
                std::string player_name = message.substr(0, pos_wears_off1);
                std::string debuff_name = message.substr(pos_wears_off1 + wears_off_str1.length(), pos_wears_off2 - (pos_wears_off1 + wears_off_str1.length()));

                if (debuff_name == "Paralysis" || debuff_name == "Poison" || debuff_name == "Silence" || debuff_name == "Blindness") {
                     WriteToPipe("DEBUFF_FADED|" + player_name + "|" + debuff_name + "\n");
                }
                return false; // Packet handled
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
