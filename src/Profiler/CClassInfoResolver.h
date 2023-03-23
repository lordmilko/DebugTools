#pragma once

class CClassInfo;
class IClassInfo;
class CSigMethodDef;
class CValueTracer;

class CClassInfoResolver
{
public:
    CClassInfoResolver(IClassInfo* info)
    {
        m_pClassInfo = info;

        m_FunctionId.functionID = 0;
        m_pMethod = nullptr;
        m_FrameInfo = 0;
        m_pTracer = nullptr;
    }

    CClassInfoResolver(FunctionIDOrClientID functionId, CSigMethodDef* pMethod, COR_PRF_FRAME_INFO frameInfo, CValueTracer* pTracer)
    {
        m_pClassInfo = nullptr;

        m_FunctionId = functionId;
        m_pMethod = pMethod;
        m_FrameInfo = frameInfo;
        m_pTracer = pTracer;
    }

    HRESULT Resolve(
        _Out_ CClassInfo** ppClassInfo);

private:
    HRESULT GetMethodTypeArgsAndContainingClass(
        _Out_ IClassInfo** ppMethodClassInfo);

    HRESULT GetMethodCanonicalType(
        _In_ ModuleID moduleId,
        _In_ mdToken token,
        _Out_ IClassInfo** ppMethodClassInfo);

    IClassInfo* m_pClassInfo;

    FunctionIDOrClientID m_FunctionId;
    CSigMethodDef* m_pMethod;
    COR_PRF_FRAME_INFO m_FrameInfo;
    CValueTracer* m_pTracer;
};