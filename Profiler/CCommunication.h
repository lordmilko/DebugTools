#pragma once

class CCommunication
{
public:
    CCommunication() :
        m_hPipe(nullptr),
        m_Stopping(FALSE),
        m_hPipeThread(nullptr)
    {
    }

    ~CCommunication();

    HRESULT Initialize();

    HANDLE m_hPipe;
    BOOL m_Stopping;

private:
    HANDLE m_hPipeThread;
};