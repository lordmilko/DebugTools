#pragma once

#define WPARAM_LPARAM_POINTERS
#define WPARAM_POINTERS
#define LPARAM_POINTERS \
    X(WINDOWPOS*)

void Simple(UINT message, HWND hWnd, WPARAM wParam, LPARAM lParam);

#pragma region wParam + lParam

//Both types are pointers
template<typename TW, typename TL>
void WriteWLPointer(UINT message, HWND hWnd, TW wParam, TL lParam) = delete;

#define WRITE_WL_POINTER(_WPARAM, _LPARAM) template<> \
    void WriteWLPointer<_WPARAM, _LPARAM>(UINT message, HWND hWnd, _WPARAM wParam, _LPARAM lParam)  // NOLINT(bugprone-macro-parentheses)

#define X(l) \
    WRITE_WL_POINTER(WPARAM, l);
WPARAM_LPARAM_POINTERS
#undef X

#pragma endregion
#pragma region wParam

//wParam is a pointer
template<typename TW, typename TL>
void WriteWPointer(UINT message, HWND hWnd, TW wParam, TL lParam) = delete;

#define WRITE_W_POINTER(_WPARAM, _LPARAM) template<> \
    void WriteWPointer<_WPARAM, _LPARAM>(UINT message, HWND hWnd, _WPARAM wParam, _LPARAM lParam)  // NOLINT(bugprone-macro-parentheses)

#define X(l) \
    WRITE_W_POINTER(WPARAM, l);
WPARAM_POINTERS
#undef X

#pragma endregion
#pragma region lParam

//lParam is a pointer
template<typename TW, typename TL>
void WriteLPointer(UINT message, HWND hWnd, TW wParam, TL lParam) = delete;

#define WRITE_L_POINTER(_WPARAM, _LPARAM) template<> \
    void WriteLPointer<_WPARAM, _LPARAM>(UINT message, HWND hWnd, _WPARAM wParam, _LPARAM lParam)  // NOLINT(bugprone-macro-parentheses)

#define X(l) \
    WRITE_L_POINTER(WPARAM, l);
LPARAM_POINTERS
#undef X

WRITE_L_POINTER(WPARAM, char*);

#pragma endregion