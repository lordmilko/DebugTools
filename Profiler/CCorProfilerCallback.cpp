#include "pch.h"
#include "CCorProfilerCallback.h"

#pragma region IUnknown

/// <summary>
/// Increments the reference count of this object.
/// </summary>
/// <returns>The incremented reference count.</returns>
ULONG CCorProfilerCallback::AddRef()
{
	return InterlockedIncrement(&m_RefCount);
}

/// <summary>
/// Decrements the reference count of this object. When the reference count reaches 0 this object will be deleted.
/// </summary>
/// <returns>The new reference count of this object.</returns>
ULONG CCorProfilerCallback::Release()
{
	ULONG refCount = InterlockedDecrement(&m_RefCount);

	if (refCount == 0)
		delete this;

	return refCount;
}

/// <summary>
/// Queries the class factory for a pointer to a specific interface it may implement.
/// </summary>
/// <param name="riid">The identifier of the interface that is being queried for.</param>
/// <param name="ppvObject">A pointer to store a pointer to the interface to, if it is supported.</param>
/// <returns>E_POINTER if ppvObject was null, S_OK if the interface is supported or E_NOINTERFACE if the interface is not supported.</returns>
HRESULT CCorProfilerCallback::QueryInterface(REFIID riid, void** ppvObject)
{
	if (ppvObject == nullptr)
		return E_POINTER;

	if (riid == IID_IUnknown)
		*ppvObject = static_cast<IUnknown*>(this);
	else if (riid == __uuidof(ICorProfilerCallback))
		*ppvObject = static_cast<ICorProfilerCallback*>(this);
	else if (riid == __uuidof(ICorProfilerCallback2))
		*ppvObject = static_cast<ICorProfilerCallback2*>(this);
	else if (riid == __uuidof(ICorProfilerCallback3))
		*ppvObject = static_cast<ICorProfilerCallback3*>(this);
	else
	{
		*ppvObject = nullptr;
		return E_NOINTERFACE;
	}

	reinterpret_cast<IUnknown*>(*ppvObject)->AddRef();

	return S_OK;
}

#pragma endregion
#pragma region ICorProfilerCallback

HRESULT CCorProfilerCallback::Initialize(IUnknown* pICorProfilerInfoUnk)
{
	return E_NOTIMPL;
}

HRESULT CCorProfilerCallback::Shutdown()
{
	return E_NOTIMPL;
}

#pragma endregion