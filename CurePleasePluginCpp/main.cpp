#include "../Ashita-v4beta/plugins/sdk/Ashita.h"
#include <windows.h>
#include <string>
#include <vector>
#include <thread>
#include <mutex>
#include <chrono>
#include <iomanip>
#include <sstream>
#include <atomic>

// Plugin Information
const char* g_PluginName = "CurePleasePluginCpp";
const char* g_PluginAuthor = "Jules";
const char* g_PluginDescription = "Packet listener for CurePlease.";
const double g_PluginVersion = 2.6;

// Forward Declarations
std::string GetTimestamp();

class CurePleasePlugin : public IPlugin
{
private:
    IAshitaCore* m_AshitaCore;
    HANDLE m_hPipe;
    std::thread m_PipeThread;
    std::mutex m_PipeMutex;
    bool m_PipeConnected;
    std::atomic<bool> m_Shutdown;
    bool m_isZoning;

    uint16_t GetIndexFromServerId(uint32_t serverId)
    {
        if (!m_AshitaCore) return 0;
        auto entMgr = m_AshitaCore->GetMemoryManager()->GetEntity();
        if (!entMgr) return 0;

        for (int i = 0; i < 2048; i++) // Iterate through entity list
        {
            if (entMgr->GetServerId(i) == serverId)
                return i;
        }
        return 0;
    }

public:
    CurePleasePlugin() : m_AshitaCore(nullptr), m_hPipe(INVALID_HANDLE_VALUE), m_PipeConnected(false), m_Shutdown(false), m_isZoning(false) {}
    ~CurePleasePlugin()
    {
        m_Shutdown = true;
        if (m_PipeThread.joinable())
        {
            m_PipeThread.join();
        }
    }

    const char* GetName() const override { return g_PluginName; }
    const char* GetAuthor() const override { return g_PluginAuthor; }
    const char* GetDescription() const override { return g_PluginDescription; }
    double GetVersion() const override { return g_PluginVersion; }
    uint32_t GetFlags() const override { return (uint32_t)Ashita::PluginFlags::UsePackets; }

    bool Initialize(IAshitaCore* core, ILogManager* logger, uint32_t id) override
    {
        m_AshitaCore = core;
        m_PipeThread = std::thread(&CurePleasePlugin::PipeThread, this);
        return true;
    }

    void Release() override
    {
        m_Shutdown = true;
        HANDLE hDummyPipe = CreateFile(
            L"\\\\.\\pipe\\CurePleasePipe", GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
        if (hDummyPipe != INVALID_HANDLE_VALUE)
        {
            CloseHandle(hDummyPipe);
        }
        if (m_PipeThread.joinable())
        {
            m_PipeThread.join();
        }
    }

    bool HandleCommand(int32_t mode, const char* command, bool injected) override
    {
        return false;
    }

    bool HandleIncomingPacket(uint16_t id, uint32_t size, const uint8_t* data, uint8_t* modified, uint32_t sizeChunk, const uint8_t* dataChunk, bool injected, bool blocked) override
    {
        if (id == 0x0B) {
            m_isZoning = true;
            WriteToPipe("LOG|" + GetTimestamp() + " Zoning started. Pausing packet processing.\n");
            return false;
        }

        if (id == 0x0A) {
            m_isZoning = false;
            WriteToPipe("LOG|" + GetTimestamp() + " Zoning finished. Resuming packet processing.\n");
            return false;
        }

        if (m_isZoning) {
            return false;
        }

        if (!m_PipeConnected || !m_AshitaCore) return false;

        std::stringstream ss;
        ss << "LOG|" << GetTimestamp() << " Incoming Packet ID: 0x" << std::hex << std::setw(4) << std::setfill('0') << id << "\n";
        WriteToPipe(ss.str());

        auto* party = m_AshitaCore->GetMemoryManager()->GetParty();
        if (!party) return false;

        uint32_t myServerId = party->GetMemberServerId(0);
        if (myServerId == 0) return false;

        if (id == 0x28) // Action packet
        {
            uint32_t actorId = *reinterpret_cast<const uint32_t*>(data + 4);

            uint8_t category = (uint8_t)(Ashita::BinaryData::UnpackBitsLE(const_cast<uint8_t*>(data), 82, 4));
            uint16_t param = *reinterpret_cast<const uint16_t*>(data + 14);

            if (param == 28787) {
                WriteToPipe("CAST_INTERRUPT|Magic interrupted.\n");
            } else if (param == 24931) {
                WriteToPipe("CAST_BLOCKED|Magic blocked.\n");
            } else if (category == 3) {
                WriteToPipe("CAST_START|Magic cast started.\n");
            } else if (category == 4 && sizeChunk > 0) { // Magic Finish
                uint32_t finishActorId = *reinterpret_cast<const uint32_t*>(dataChunk + 8);
                uint16_t actionId = (uint16_t)Ashita::BinaryData::UnpackBitsLE(const_cast<uint8_t*>(dataChunk), 96, 10);
                uint32_t targetId = *reinterpret_cast<const uint32_t*>(dataChunk + 16);
                if (targetId == 0) targetId = finishActorId;

                std::stringstream ss;
                ss << "ACTION|" << finishActorId << "|" << targetId << "|" << actionId << "\n";
                WriteToPipe(ss.str());

                WriteToPipe("CAST_FINISH|Magic cast finished.\n");
            }
        }
        else if (id == 0x29) // Ability/WS packet
        {
            uint32_t actorId = *reinterpret_cast<const uint32_t*>(data + 4);
            uint32_t targetId = *reinterpret_cast<const uint32_t*>(data + 8);
            uint16_t abilityId = *reinterpret_cast<const uint16_t*>(data + 12);
            if (targetId == 0) targetId = actorId;

            std::stringstream ss;
            ss << "ABILITY|" << actorId << "|" << targetId << "|" << abilityId << "\n";
            WriteToPipe(ss.str());
        }
        else if (id == 0x00E) // Chat Message
        {
            if (size <= 4) return false;

            // The message starts at offset 4.
            const char* start = reinterpret_cast<const char*>(data + 4);
            const char* end = (const char*)memchr(start, '\0', size - 4);
            size_t len = (end == nullptr) ? (size - 4) : (end - start);
            std::string message(start, len);

            // Clean the FFXI message string
            std::string cleanedMessage;
            for (size_t i = 0; i < message.length(); ++i) {
                if (static_cast<unsigned char>(message[i]) == 0xEF || static_cast<unsigned char>(message[i]) == 0x1E) {
                    i += 2; // Skip multi-byte control codes
                } else if (static_cast<unsigned char>(message[i]) >= 32) {
                    cleanedMessage += message[i];
                }
            }

            // Check for buff fade messages
            const std::string wearOffStr = "The effect of ";
            size_t pos = cleanedMessage.find(wearOffStr);
            if (pos != std::string::npos)
            {
                size_t buffStart = pos + wearOffStr.length();
                const std::string wearOffMidStr = " wears off ";
                size_t buffEnd = cleanedMessage.find(wearOffMidStr);

                if (buffEnd != std::string::npos)
                {
                    std::string buffName = cleanedMessage.substr(buffStart, buffEnd - buffStart);

                    size_t playerStart = buffEnd + wearOffMidStr.length();
                    std::string playerName = cleanedMessage.substr(playerStart);
                    if (!playerName.empty() && playerName.back() == '.')
                    {
                        playerName.pop_back();
                    }

                    const std::vector<std::string> targetBuffs = {"Regen", "Haste", "Protect", "Shell", "Phalanx"};
                    bool isTargetBuff = false;
                    for(const auto& target : targetBuffs) {
                        if (buffName.find(target) != std::string::npos) {
                            isTargetBuff = true;
                            break;
                        }
                    }

                    if (isTargetBuff) {
                        std::string pipeMsg = "BUFF_FADE|" + playerName + ":" + buffName + "\n";
                        WriteToPipe(pipeMsg);

                        std::string logMsg = "LOG|" + GetTimestamp() + " [Buff Fade] Player: " + playerName + ", Buff: " + buffName + "\n";
                        WriteToPipe(logMsg);
                    }
                }
            }
        }
        return false;
    }

    bool HandleOutgoingPacket(uint16_t id, uint32_t size, const uint8_t* data, uint8_t* modified, uint32_t sizeChunk, const uint8_t* dataChunk, bool injected, bool blocked) override
    {
        return false;
    }

private:
    void PipeThread()
    {
        while (!m_Shutdown)
        {
            m_hPipe = CreateNamedPipe(
                L"\\\\.\\pipe\\CurePleasePipe", PIPE_ACCESS_OUTBOUND, PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
                1, 4096, 4096, 0, NULL);

            if (m_hPipe == INVALID_HANDLE_VALUE)
            {
                if (m_Shutdown) break;
                Sleep(5000);
                continue;
            }

            BOOL fConnected = ConnectNamedPipe(m_hPipe, NULL) ? TRUE : (GetLastError() == ERROR_PIPE_CONNECTED);

            if (m_Shutdown)
            {
                if (fConnected) DisconnectNamedPipe(m_hPipe);
                CloseHandle(m_hPipe);
                m_hPipe = INVALID_HANDLE_VALUE;
                break;
            }

            if (fConnected)
            {
                {
                    std::lock_guard<std::mutex> lock(m_PipeMutex);
                    m_PipeConnected = true;
                }
                std::stringstream ss;
                ss << "LOG|" << GetTimestamp() << " Packet listener v" << std::fixed << std::setprecision(1) << g_PluginVersion << " connected.\n";
                WriteToPipe(ss.str());

                while (!m_Shutdown)
                {
                    DWORD dwError = 0;
                    if (!PeekNamedPipe(m_hPipe, NULL, 0, NULL, NULL, NULL))
                    {
                        dwError = GetLastError();
                        if (dwError == ERROR_BROKEN_PIPE || dwError == ERROR_PIPE_NOT_CONNECTED)
                        {
                            break;
                        }
                    }
                    Sleep(500);
                }
            }

            {
                std::lock_guard<std::mutex> lock(m_PipeMutex);
                m_PipeConnected = false;
            }

            if(m_hPipe != INVALID_HANDLE_VALUE) {
               if (fConnected) DisconnectNamedPipe(m_hPipe);
               CloseHandle(m_hPipe);
               m_hPipe = INVALID_HANDLE_VALUE;
            }
        }
    }

    void WriteToPipe(const std::string& message)
    {
        std::lock_guard<std::mutex> lock(m_PipeMutex);
        if (!m_PipeConnected || m_hPipe == INVALID_HANDLE_VALUE) return;
        DWORD bytesWritten = 0;
        WriteFile(m_hPipe, message.c_str(), message.length(), &bytesWritten, NULL);
    }
};

std::string GetTimestamp()
{
    auto now = std::chrono::system_clock::now();
    auto in_time_t = std::chrono::system_clock::to_time_t(now);
    auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()) % 1000;
    std::tm buf;
    localtime_s(&buf, &in_time_t);
    std::stringstream ss;
    ss << std::put_time(&buf, "[%H:%M:%S");
    ss << '.' << std::setfill('0') << std::setw(3) << ms.count() << "]";
    return ss.str();
}

extern "C" __declspec(dllexport) IPlugin* __stdcall expCreatePlugin(const char* args)
{
    return new CurePleasePlugin();
}

extern "C" __declspec(dllexport) double __stdcall expGetInterfaceVersion(void)
{
    return ASHITA_INTERFACE_VERSION;
}
