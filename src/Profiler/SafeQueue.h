#pragma once

#include <queue>
#include <mutex>
#include <condition_variable>

template<typename T>
class SafeQueue
{
public:
    SafeQueue() : m_Stop(FALSE)
    {
    }

    size_t Size() noexcept
    {
        std::lock_guard<std::mutex> lock(m_Mutex);

        return m_Queue.size();
    }

    void Push(T&& t)
    {
        std::lock_guard<std::mutex> lock(m_Mutex);

        m_Queue.push(t);

        m_Condition.notify_one();
    }

    BOOL Pop(T& result)
    {
        std::unique_lock<std::mutex> lock(m_Mutex);

        while (m_Queue.empty())
        {
            if (m_Stop)
                return FALSE;

            m_Condition.wait(lock);
        }

        if (m_Stop)
            return FALSE;

        result = m_Queue.front();
        m_Queue.pop();

        return TRUE;
    }

    T* Peek()
    {
        std::lock_guard<std::mutex> lock(m_Mutex);

        if (m_Queue.empty())
            return nullptr;

        T* front = &m_Queue.front();

        return front;
    }

    void Stop()
    {
        m_Stop = TRUE;
        m_Condition.notify_one();
    }

private:
    std::queue<T> queue;
    std::mutex mutex;
    std::condition_variable condition;
    bool stop;

    std::queue<T> m_Queue;
    std::mutex m_Mutex;
    std::condition_variable m_Condition;
    BOOL m_Stop;
};