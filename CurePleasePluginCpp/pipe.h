#pragma once
#include <string>
#include <thread>
#include <mutex>
#include <atomic>
#include <windows.h>

// Simple pipe manager class
class PipeManager
{
public:
    PipeManager();
    ~PipeManager();

    void Start();
    void Stop();

    void Write(const std::string& message);

private:
    void ThreadProc();

    HANDLE       m_hPipe         = INVALID_HANDLE_VALUE;
    std::thread  m_PipeThread;
    std::mutex   m_PipeMutex;
    std::atomic<bool> m_Shutdown = false;
    bool         m_PipeConnected = false;
};
