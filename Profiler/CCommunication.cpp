#include "pch.h"
#include "CCommunication.h"
#include <stdio.h>

#define MESSAGE_DATA_SIZE 1000

//Keep in sync with MessageType.cs
enum class MessageType
{
};

typedef struct _Message {
    MessageType Type;
    BYTE Data[MESSAGE_DATA_SIZE];
} Message;

DWORD WINAPI PipeThreadProc(LPVOID lpParameter)
{
    CCommunication* communication = static_cast<CCommunication*>(lpParameter);

    DWORD numberOfBytesRead;

    BYTE buffer[MESSAGE_DATA_SIZE + sizeof(MessageType)];

    //Wait for the client to connect
    ConnectNamedPipe(communication->m_hPipe, NULL);

    while (true)
    {
        BOOL result = ReadFile(
            communication->m_hPipe,
            buffer,
            sizeof(Message),
            &numberOfBytesRead,
            nullptr
        );

        if (result)
        {
            Message* message = (Message*)buffer;

            /*switch (message->Type)
            {
            default:
                dprintf(L"Don't know how to handle MessageType %d\n", message->Type);
                break;
            }*/
        }
    }

    return 0;
}

CCommunication::~CCommunication()
{
    if (m_hPipeThread != nullptr)
    {
        CancelSynchronousIo(m_hPipeThread);
        CloseHandle(m_hPipeThread);
        m_hPipeThread = nullptr;
    }

    if (m_hPipe != nullptr && m_hPipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hPipe);
        m_hPipe = nullptr;
    }
}

HRESULT CCommunication::Initialize()
{
    HRESULT hr = S_OK;

    DWORD pid = GetProcessId(GetCurrentProcess());

    WCHAR buffer[1000];
    swprintf_s(buffer, L"\\\\.\\pipe\\DebugToolsProfilerPipe_%d", pid);

    //Setup pipe

    m_hPipe = CreateNamedPipe(
        buffer,
        PIPE_ACCESS_INBOUND,
        PIPE_TYPE_MESSAGE,
        1,
        0,
        0,
        0,
        nullptr
    );

    if (m_hPipe == INVALID_HANDLE_VALUE)
        return HRESULT_FROM_WIN32(GetLastError());

    //Setup thread

    m_hPipeThread = CreateThread(
        nullptr,
        0,
        PipeThreadProc,
        this,
        0,
        nullptr
    );

    //If this fails, hPipe will be closed in the destructor
    if (m_hPipeThread == nullptr)
        return HRESULT_FROM_WIN32(GetLastError());

    return S_OK;
}