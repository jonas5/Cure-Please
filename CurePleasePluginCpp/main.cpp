#include "../Ashita-v4beta/plugins/sdk/Ashita.h"
#include <windows.h>
#include <string>
#include <vector>
#include <thread>
#include <mutex>
#include <chrono>
#include <iomanip>
#include <sstream>

// Plugin Information
const char* g_PluginName = "CurePleasePluginCpp";
const char* g_PluginAuthor = "Jules";
const char* g_PluginDescription = "Packet listener for CurePlease.";
const double g_PluginVersion = 1.0;

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

public:
    CurePleasePlugin() : m_AshitaCore(nullptr), m_hPipe(INVALID_HANDLE_VALUE), m_PipeConnected(false) {}
    ~CurePleasePlugin()
    {
        if (m_PipeThread.joinable())
        {
            m_PipeThread.detach(); // Or a more graceful shutdown mechanism
        }
        if (m_hPipe != INVALID_HANDLE_VALUE)
        {
            DisconnectNamedPipe(m_hPipe);
            CloseHandle(m_hPipe);
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
        if (m_PipeThread.joinable())
        {
            m_PipeThread.detach();
        }
        if (m_hPipe != INVALID_HANDLE_VALUE)
        {
            DisconnectNamedPipe(m_hPipe);
            CloseHandle(m_hPipe);
        }
    }

    bool HandleCommand(int32_t mode, const char* command, bool injected) override { return false; }

    bool HandleIncomingPacket(uint16_t id, uint32_t size, const uint8_t* data, uint8_t* modified, bool injected, bool blocked) override
    {
        if (!m_PipeConnected) return false;
        if (!m_AshitaCore) return false;

        auto* party = m_AshitaCore->GetDataManager()->GetParty();
        if (!party) return false;

        if (id == 0x28) // Action Packet
        {
            uint32_t actor = *reinterpret_cast<const uint32_t*>(data + 4);
            if (actor == party->GetMember(0).ServerId)
            {
                int category = (data[10] >> 2) & 0x0F;
                if (category == 4)
                {
                    WriteToPipe("CAST_FINISH|0\n");
                    WriteToPipe("LOG|" + GetTimestamp() + " Spell cast finished.\n");
                }
                else if (category == 8)
                {
                    uint16_t param = *reinterpret_cast<const uint16_t*>(data + 8);
                    if (param == 28787)
                    {
                        WriteToPipe("CAST_INTERRUPT|0\n");
                        WriteToPipe("LOG|" + GetTimestamp() + " Spell cast interrupted.\n");
                    }
                    else if (param == 24931)
                    {
                        WriteToPipe("CAST_BLOCKED|0\n");
                        WriteToPipe("LOG|" + GetTimestamp() + " Spell cast blocked.\n");
                    }
                }
            }
        }
        else if (id == 0x076) // Buff packet
        {
            WriteToPipe("LOG|" + GetTimestamp() + " Processing status effect update (0x076).\n");
            auto* entity = m_AshitaCore->GetDataManager()->GetEntity();
            for (int k = 0; k < 5; k++)
            {
                uint16_t Uid = *reinterpret_cast<const uint16_t*>(data + 8 + (k * 0x30));
                if (Uid != 0)
                {
                    std::vector<int> buffs;
                    for (int i = 0; i < 32; i++)
                    {
                        int current_buff = data[k * 48 + 5 + 16 + i] + 256 * ((data[k * 48 + 5 + 8 + (i / 4)] >> ((i % 4) * 2)) & 3);
                        if (current_buff != 255 && current_buff != 0)
                        {
                            buffs.push_back(current_buff);
                        }
                    }

                    if (!buffs.empty())
                    {
                        const char* characterName = entity->GetName(Uid);
                        if (characterName)
                        {
                            std::string buff_str;
                            for (size_t i = 0; i < buffs.size(); ++i)
                            {
                                buff_str += std::to_string(buffs[i]);
                                if (i < buffs.size() - 1)
                                {
                                    buff_str += ",";
                                }
                            }
                            std::string message = "BUFF_UPDATE|" + std::string(characterName) + ":" + buff_str + "\n";
                            WriteToPipe(message);
                        }
                    }
                }
            }
        }
        return false;
    }

    bool HandleOutgoingPacket(uint16_t id, uint32_t size, const uint8_t* data, uint8_t* modified, bool injected, bool blocked) override { return false; }

private:
    void PipeThread()
    {
        while (true)
        {
            m_hPipe = CreateNamedPipe(
                L"\\\\.\\pipe\\CurePleasePipe",
                PIPE_ACCESS_OUTBOUND,
                PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
                1, 4096, 4096, 0, NULL);

            if (m_hPipe == INVALID_HANDLE_VALUE)
            {
                Sleep(5000);
                continue;
            }

            if (ConnectNamedPipe(m_hPipe, NULL) ? TRUE : (GetLastError() == ERROR_PIPE_CONNECTED))
            {
                {
                    std::lock_guard<std::mutex> lock(m_PipeMutex);
                    m_PipeConnected = true;
                }
                WriteToPipe("LOG|" + GetTimestamp() + " Packet listener connected.\n");

                while (true)
                {
                    DWORD dwError = 0;
                    DWORD dwBytesRead = 0, dwTotalBytesAvail = 0, dwBytesLeftThisMessage = 0;

                    if (!PeekNamedPipe(m_hPipe, NULL, 0, &dwBytesRead, &dwTotalBytesAvail, &dwBytesLeftThisMessage))
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
            DisconnectNamedPipe(m_hPipe);
            CloseHandle(m_hPipe);
            m_hPipe = INVALID_HANDLE_VALUE;
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

extern "C" __declspec(dllexport) uint32_t __stdcall expGetInterfaceVersion(void)
{
    return ASHITA_INTERFACE_VERSION;
}
