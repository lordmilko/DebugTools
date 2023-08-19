#include "pch.h"
#include "MMF.h"
#include "Serialization.h"
#include "WindowMessages.h"

extern "C" LRESULT CALLBACK WndProcHook(int code, WPARAM wParam, LPARAM lParam)
{
    MMFInitialize();

    if (code == HC_ACTION)
    {
        BOOL sentByCurrentThread = wParam != 0;
        CWPSTRUCT* info = (CWPSTRUCT*)lParam;

        switch (info->message)
        {
#define X(msg, func, wp, lp) case msg: func(info->message, info->hwnd, (wp) info->wParam, (lp) info->lParam); break;
            WINDOW_MESSAGES
#undef X
        }
    }

    return CallNextHookEx(NULL, code, wParam, lParam);
}

extern "C" LRESULT CALLBACK WndProcRetHook(int code, WPARAM wParam, LPARAM lParam)
{
    MMFInitialize();

    if (code == HC_ACTION)
    {
        BOOL sentByCurrentThread = wParam != 0;
        CWPRETSTRUCT* info = (CWPRETSTRUCT*)lParam;

        //Not yet implemented
    }

    return CallNextHookEx(NULL, code, wParam, lParam);
}