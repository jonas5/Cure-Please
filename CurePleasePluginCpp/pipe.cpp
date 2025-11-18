#include "Pipe.h"
#include <chrono>
#include <sstream>

static std::wstring PipeName = L"\\\\.\\pipe\\MiraculixPipe";

PipeManager::PipeManager() {}
PipeManager::~PipeManager() { Stop(); }

void PipeManager::Start()
{
    m_Shutdown = false;
    m_PipeThread = std::thread(&PipeManager::ThreadProc, this);
}

void PipeManager::Stop()
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
}

void PipeManager::Write(const std::string& message)
{
    std::lock_guard<std::mutex> lock(m_PipeMutex);
    if (!m_PipeConnected || m_hPipe == INVALID_HANDLE_VALUE) return;

    std::string out = message;
    if (out.empty() || out.back() != '\n')
        out.push_back('\n');

    DWORD bytesWritten = 0;
    WriteFile(m_hPipe, out.c_str(), static_cast<DWORD>(out.size()), &bytesWritten, NULL);
    FlushFileBuffers(m_hPipe);
}

void PipeManager::ThreadProc()
{
    while (!m_Shutdown)
    {
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

            Write("LOG|Pipe connected.\n");

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
