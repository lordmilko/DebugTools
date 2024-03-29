#include "pch.h"
#include "CCommunication.h"
#include "CStaticTracer.h"
#include "CCorProfilerCallback.h"

#define MESSAGE_DATA_SIZE 1000

bool g_TracingEnabled = FALSE;

//Keep in sync with MessageType.cs
enum class MessageType
{
    EnableTracing,
    GetStaticField
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

    //Unfortunately, this is not enough to prevent GetClassFromObject() returning
    //CORPROF_E_NOT_MANAGED_THREAD. In order for AllowObjectInspection() to succeed,
    //gCurrentThreadInfo.m_pThread must have a value. All InitializeCurrentThread() does
    //however is set other properties on gCurrentThreadInfo.
    g_pProfiler->m_pInfo->InitializeCurrentThread();

    while (true)
    {
        if (communication->m_Stopping)
            break;

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

            switch (message->Type)
            {
            case MessageType::EnableTracing:
                g_TracingEnabled = *(bool*)message->Data;
                break;

            case MessageType::GetStaticField:
                CStaticTracer::Trace((LPWSTR)message->Data);
                break;

            default:
                dprintf(L"Don't know how to handle MessageType %d\n", message->Type);
                break;
            }
        }
    }

    return 0;
}

CCommunication::~CCommunication()
{
    m_Stopping = TRUE;

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