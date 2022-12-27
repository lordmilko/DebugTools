#pragma once

class CUnknown : public IUnknown
{
public:
    CUnknown();
    virtual ~CUnknown() = default;

    // IUnknown
    STDMETHODIMP_(ULONG) AddRef() override;
    STDMETHODIMP QueryInterface(REFIID riid, void** ppvObject) override;
    STDMETHODIMP_(ULONG) Release() override;

private:
    long m_RefCount;
};