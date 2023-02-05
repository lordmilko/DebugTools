#pragma once

#pragma warning(push)
#pragma warning(disable: 4102) //unreferenced label

// Can't be declared inline because x64 assembly won't work
// ReSharper disable once CppNonInlineFunctionDefinitionInHeaderFile
extern "C" void STDMETHODCALLTYPE LeaveStub(FunctionIDOrClientID functionId)
{
    HRESULT hr = S_OK;

    LEAVE_FUNCTION(functionId.functionID);
    LogCall(L"Leave", functionId);

    CExceptionManager::ClearStaleExceptions();

ErrExit:
    if (!g_TracingEnabled)
        return;

    ValidateETW(EventWriteCallLeaveEvent(functionId.functionID, g_Sequence, hr));
}

#ifdef _X86_
void __declspec(naked) LeaveNaked(FunctionIDOrClientID functionIDOrClientID)
{
    __asm
    {
        //The method prologue will push the function ID onto the stack; reset ESP so it's correctly visible within this function.
        //This is not optional, in Debug or Release
        push ebp
        mov ebp, esp

        // Make space for locals.
        sub esp, __LOCAL_SIZE

        // Save all registers.
        //
        // Technically you need only save what you use, but if you're working in C
        // rather than assembly that's hard to predict. Saving them all is safer.
        // If you really need to save the minimal set, code the whole darn thing in 
        // assembly so you can control the register usage.
        //
        // Pushing ESP again is redundant (and in DEBUG pushing EBP is too), but using
        // pushad is cleaner and saves some clock time over pushing individual registers.
        // You can push the individual registers if you want; doing so will
        // save a couple of DWORDs of stack space, perhaps at the expense of
        // a clock cycle.
        pushad

        // Check if there's anything on the FP stack.
        //
        // Again technically you need only save what you use. You might think that
        // FP regs are not commonly used in the kind of code you'd write in these,
        // but there are common cases that might interfere. For example, in the 8.0 MS CRT, 
        // memcpy clears the FP stack.
        fstsw   ax
        and ax, 3800h                // Check the top-of-fp-stack bits
        cmp     ax, 0                // If non-zero, we have something to save
        jnz     SaveFPReg
        push    0                    // otherwise, mark that there is no float value
        jmp     NoSaveFPReg

SaveFPReg:
        sub     esp, 8               // Make room for the FP value
        fstp    qword ptr[esp]       // Copy the FP value to the buffer as a double
        push    1                    // mark that a float value is present

NoSaveFPReg:
    }

    LeaveStub(functionIDOrClientID);

    __asm
    {
        // Now see if we have to restore any floating point registers
        cmp     [esp], 0             // Check the flag
        jz      NoRestoreFPRegs      // If zero, no FP regs

RestoreFPRegs:
        fld     qword ptr [esp + 4]  // Restore FP regs
        add    esp, 12               // Move ESP past the storage space
        jmp   RestoreFPRegsDone

NoRestoreFPRegs:
        add     esp, 4               // Move ESP past the flag

RestoreFPRegsDone:
        // Restore other registers
        popad

        // Pop off locals
        add esp, __LOCAL_SIZE

        // Restore EBP
        pop ebp

        // stdcall: Callee cleans up args from stack on return
        ret SIZE functionIDOrClientID
    }
}
#elif defined(_AMD64_)
EXTERN_C void LeaveNaked(FunctionIDOrClientID functionIDOrClientID);
#endif

#pragma warning(pop)