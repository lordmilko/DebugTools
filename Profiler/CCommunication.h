#pragma once

class CCommunication
{
public:
    CCommunication() : m_hPipe(nullptr), m_hPipeThread(nullptr)
    {
    }

    ~CCommunication();

    HRESULT Initialize();

    HANDLE m_hPipe;

private:
    HANDLE m_hPipeThread;
};