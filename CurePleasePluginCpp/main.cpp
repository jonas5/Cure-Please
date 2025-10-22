#include "../Ashita-v4beta/plugins/sdk/Ashita.h"
#include <windows.h>
#include <string>
#include <thread>
#include <mutex>
#include <chrono>
#include <iomanip>
#include <sstream>
#include <atomic>
#include <vector>
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
    std::thread m_PlayerScanThread;
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

    void PlayerScanThread()
    {
        while (!m_Shutdown)
        {
            std::this_thread::sleep_for(std::chrono::seconds(5)); // Scan every 5 seconds

            if (!m_PipeConnected || m_AshitaCore == nullptr)
                continue;

            auto entMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
            auto partyMgr = m_AshitaCore->GetMemoryManager()->GetParty();
            if (!entMgr || !partyMgr)
                continue;

            std::string nearbyPlayers = "NEARBY_PLAYERS|";
            int playerCount = 0;

            for (int i = 0; i < 2048; ++i)
            {
                if (!entMgr->GetIsEntityValid(i)) continue;

                const char* name = entMgr->GetName(i);
                if (!name) continue;

                uint16_t type = entMgr->GetType(i);
                float distance = entMgr->GetDistance(i);
                uint8_t hpPercent = entMgr->GetHPPercent(i);

                std::stringstream log;
                log << "LOG|" << GetTimestamp() << " [Scan] Checking '" << name << "' (Type: " << type << ", Dist: " << std::fixed << std::setprecision(1) << distance << ", HP: " << (int)hpPercent << "%).";

                bool isPlayerType = ((type & 1) != 0 || type == 0);
                if (!isPlayerType) {
                    log << " -> Reject: Not player type.\n";
                    WriteToPipe(log.str());
                    continue;
                }

                if (distance > 20.0f) {
                    log << " -> Reject: Too far.\n";
                    WriteToPipe(log.str());
                    continue;
                }

                if (hpPercent == 0) {
                    log << " -> Reject: HP is 0.\n";
                    WriteToPipe(log.str());
                    continue;
                }

                bool inParty = false;
                for (int j = 0; j < 18; ++j)
                {
                    if (partyMgr->GetMemberName(j) && strcmp(partyMgr->GetMemberName(j), name) == 0)
                    {
                        inParty = true;
                        break;
                    }
                }

                if (inParty) {
                    log << " -> Reject: In party.\n";
                    WriteToPipe(log.str());
                    continue;
                }

                log << " -> Accept.\n";
                WriteToPipe(log.str());

                if (playerCount > 0)
                {
                    nearbyPlayers += ",";
                }
                nearbyPlayers += name;
                playerCount++;
            }
            nearbyPlayers += "\n";
            WriteToPipe(nearbyPlayers);
        }
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
        m_PlayerScanThread = std::thread(&CurePleasePlugin::PlayerScanThread, this);
        return true;
    }

    void Release() override {
        m_Shutdown = true;
        HANDLE hDummyPipe = CreateFileW(PipeName.c_str(), GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
        if (hDummyPipe != INVALID_HANDLE_VALUE) CloseHandle(hDummyPipe);
        if (m_PipeThread.joinable()) m_PipeThread.join();
        if (m_PlayerScanThread.joinable()) m_PlayerScanThread.join();
    }

    bool HandleCommand(int32_t mode, const char* command, bool injected) override {
        std::vector<std::string> args;
        std::string arg;
        std::istringstream iss(command);
        while (iss >> arg) {
            args.push_back(arg);
        }

        if (!args.empty() && (args[0] == "cpaddon" || args[0] == "/cpaddon")) {
            if (args.size() > 1 && args[1] == "verify") {
                WriteToPipe("LOG|" + GetTimestamp() + " Player scanning is active.\n");
                return true;
            }
        }
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
