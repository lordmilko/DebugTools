#include "pch.h"
#include "CUnknown.h"
#include <unordered_set>
#include <mutex>

long g_UnknownRefCount = 0;

//These have to be pointers because the first CUnknown that's created is CSigType::Sentinel which a global static, and runs before these globals have been initialized
std::unordered_set<CUnknown*>* g_UnknownMap;
std::mutex* g_UnknownMutex;

//#define DEBUG_UNKNOWN 1

CUnknown::CUnknown()
{
#if _DEBUG && DEBUG_UNKNOWN
    if (g_UnknownRefCount == 0)
    {
        g_UnknownMap = new std::unordered_set<CUnknown*>();
        g_UnknownMutex = new std::mutex();
    }

    auto pp = &g_UnknownMap;
    g_UnknownMutex->lock();
    (*g_UnknownMap).insert(this);
    g_UnknownMutex->unlock();
    InterlockedIncrement(&g_UnknownRefCount);
#endif
    m_RefCount = 1;
}

ULONG CUnknown::AddRef()
{
#if _DEBUG && DEBUG_UNKNOWN
    g_UnknownMutex->lock();
    (*g_UnknownMap).insert(this);
    g_UnknownMutex->unlock();
    InterlockedIncrement(&g_UnknownRefCount);
#endif

    return InterlockedIncrement(&m_RefCount);
}

ULONG CUnknown::Release()
{
#if _DEBUG && DEBUG_UNKNOWN
    g_UnknownMutex->lock();
    g_UnknownMap->erase(this);
    g_UnknownMutex->unlock();
    InterlockedDecrement(&g_UnknownRefCount);
#endif

    ULONG refCount = InterlockedDecrement(&m_RefCount);

    if (refCount == 0)
        delete this;

    return refCount;
}

HRESULT CUnknown::QueryInterface(REFIID riid, void** ppvObject)
{
    if (ppvObject == nullptr)
        return E_POINTER;

    if (riid == IID_IUnknown)
        *ppvObject = static_cast<IUnknown*>(this);
    else
    {
        *ppvObject = nullptr;
        return E_NOINTERFACE;
    }

    reinterpret_cast<IUnknown*>(*ppvObject)->AddRef();

    return S_OK;
}