#pragma once

template<class T>
class CUnknownArray
{
public:
    CUnknownArray() :
        m_Length(0),
        m_Capacity(0),
        m_Buffer(nullptr)
    {
    }

    ~CUnknownArray()
    {
        if (m_Buffer)
        {
            for (ULONG i = 0; i < m_Length; i++)
                m_Buffer[i]->Release();

            free(m_Buffer);
        }
    }

    HRESULT Add(T* value)
    {
        HRESULT hr = S_OK;

        IfFailGo(EnsureCapacity());

        value->AddRef();
        m_Buffer[m_Length] = value;
        m_Length++;

    ErrExit:
        return hr;
    }

    T*& operator[](ULONG index)
    {
        return m_Buffer[index];
    }

    ULONG m_Length;
    
private:
    HRESULT EnsureCapacity()
    {
        if (m_Length == m_Capacity)
        {
            if (m_Capacity == 0)
                m_Capacity = 2;
            else
                m_Capacity = m_Capacity * 2;

            T** newBuffer = (T**)realloc(m_Buffer, m_Capacity * sizeof(T*));

            if (newBuffer == NULL)
                return E_OUTOFMEMORY;

            m_Buffer = newBuffer;
        }

        return S_OK;
    }

    ULONG m_Capacity;
    T** m_Buffer;
};