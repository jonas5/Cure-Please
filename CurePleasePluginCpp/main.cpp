#include "../Ashita-v4beta/plugins/sdk/Ashita.h"
#include <windows.h>
#include <string>
#include <iostream>
#include <fstream>
#include <vector>
#include <thread>
#include <mutex>
#include <chrono>
#include <iomanip>

class CurePleasePlugin : public IPlugin
{
private:
    IAshitaCore* m_AshitaCore;
    HANDLE m_Pipe;
    bool m_PipeConnected;
    std::ofstream m_LogFile;
    std::thread m_PipeThread;
    std::mutex m_PipeMutex;

public:
    CurePleasePlugin() : m_AshitaCore(nullptr), m_Pipe(INVALID_HANDLE_VALUE), m_PipeConnected(false)
    {
        m_LogFile.open("CurePleasePlugin.log", std::ios::out | std::ios::app);
    }
    ~CurePleasePlugin()
    {
        if (m_PipeThread.joinable())
        {
            m_PipeThread.join();
        }
        if (m_Pipe != INVALID_HANDLE_VALUE)
        {
            DisconnectNamedPipe(m_Pipe);
            CloseHandle(m_Pipe);
        }
    }

    const char* GetName() const override { return "CurePleasePlugin"; }
    const char* GetAuthor() const override { return "Jules"; }
    const char* GetDescription() const override { return "Packet listener for CurePlease."; }
    double GetVersion() const override { return 1.0; }

    bool Initialize(IAshitaCore* core) override
    {
        m_AshitaCore = core;
        m_PipeThread = std::thread(&CurePleasePlugin::PipeThread, this);
        return true;
    }

    void Release() override
    {
        if (m_PipeThread.joinable())
        {
            m_PipeThread.join();
        }
        if (m_Pipe != INVALID_HANDLE_VALUE)
        {
            DisconnectNamedPipe(m_Pipe);
            CloseHandle(m_Pipe);
        }
    }

    bool HandleCommand(const char* command, int type) override
    {
        return false;
    }

    bool HandleIncomingPacket(int id, int size, const unsigned char* data) override
    {
        std::lock_guard<std::mutex> lock(m_PipeMutex);
        if (m_LogFile.is_open())
        {
            m_LogFile << "Incoming Packet: ID=" << std::hex << id << " Size=" << size << std::endl;
        }

        if (!m_PipeConnected)
        {
            return false;
        }

        if (id == 0x28) // Action Packet
        {
            uint32_t actor = *(uint32_t*)(data + 4);
            if (actor == m_AshitaCore->GetDataManager()->GetParty()->GetMemberServerId(0))
            {
                int category = (data[10] >> 2) & 0x0F;
                if (category == 4)
                {
                    WriteToPipe("CAST_FINISH|0\n");
                    WriteToPipe("LOG|" + GetTimestamp() + " Spell cast finished.\n");
                }
                else if (category == 8)
                {
                    uint16_t param = *(uint16_t*)(data + 8);
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
            WriteToPipe("LOG|" + GetTimestamp() + " Registering buffs.\n");
            for (int k = 0; k < 5; k++)
            {
                uint16_t Uid = *(uint16_t*)(data + 8 + (k * 0x30));
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
                        const char* characterName = m_AshitaCore->GetDataManager()->GetEntity()->GetName(Uid);
                        if (characterName)
                        {
                            std::string buff_str;
                            for (size_t i = 0; i < buffs.size(); ++i) {
                                buff_str += std::to_string(buffs[i]);
                                if (i < buffs.size() - 1) {
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

    bool HandleOutgoingPacket(int id, int size, const unsigned char* data) override
    {
        return false;
    }

private:
    void PipeThread()
    {
        while (true)
        {
            {
                std::lock_guard<std::mutex> lock(m_PipeMutex);
                m_Pipe = CreateNamedPipe(
                    L"\\\\.\\pipe\\CurePleasePipe",
                    PIPE_ACCESS_OUTBOUND,
                    PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
                    1,
                    4096,
                    4096,
                    0,
                    NULL);
            }

            if (m_Pipe == INVALID_HANDLE_VALUE)
            {
                // Handle error
                return;
            }

            BOOL connected = ConnectNamedPipe(m_Pipe, NULL) ? TRUE : (GetLastError() == ERROR_PIPE_CONNECTED);
            if (connected)
            {
                std::lock_guard<std::mutex> lock(m_PipeMutex);
                m_PipeConnected = true;
                WriteToPipe("LOG|" + GetTimestamp() + " Packet listener started.\n");

                // Keep the pipe open until the client disconnects
                while (true)
                {
                    DWORD bytesRead = 0, bytesWritten = 0, bytesAvail = 0;
                    BOOL success = PeekNamedPipe(m_Pipe, NULL, 0, &bytesRead, &bytesAvail, NULL);
                    if (!success || bytesRead > 0)
                    {
                        break; // Pipe was closed or has data to read (which it shouldn't)
                    }
                    Sleep(100);
                }

                std::lock_guard<std::mutex> lock2(m_PipeMutex);
                m_PipeConnected = false;
                DisconnectNamedPipe(m_Pipe);
                CloseHandle(m_Pipe);
            }
            else
            {
                CloseHandle(m_Pipe);
            }
        }
    }

    void WriteToPipe(const std::string& message)
    {
        if (!m_PipeConnected) return;

        DWORD bytesWritten = 0;
        WriteFile(m_Pipe, message.c_str(), message.length(), &bytesWritten, NULL);
    }

    std::string GetTimestamp()
    {
        auto now = std::chrono::system_clock::now();
        auto in_time_t = std::chrono::system_clock::to_time_t(now);
        auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()) % 1000;

        std::stringstream ss;
        ss << std::put_time(std::localtime(&in_time_t), "[%H:%M:%S");
        ss << '.' << std::setfill('0') << std::setw(3) << ms.count() << "]";
        return ss.str();
    }
};

extern "C" __declspec(dllexport) IPlugin* __cdecl CreatePlugin()
{
    return new CurePleasePlugin();
}
