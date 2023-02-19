EXTERN EnterStub:PROC
EXTERN LeaveStub:PROC
EXTERN TailcallStub:PROC

; Constants which are used in the following assembly code.
SIZEOF_OUTGOING_ARGUMENT_HOMES          equ 8h*4h
NUMBER_XMM_SAVES                        equ 6h
SIZEOF_STACK_ALLOC                      equ 10h*NUMBER_XMM_SAVES + SIZEOF_OUTGOING_ARGUMENT_HOMES
OFFSETOF_XMM_SAVE                       equ SIZEOF_OUTGOING_ARGUMENT_HOMES

_text SEGMENT PARA 'CODE'

ALIGN 16
PUBLIC EnterNaked

EnterNaked PROC FRAME

    ; save integer return register
    push    rax
    .allocstack 8

SaveVolIntRegs_Enter:
    push    rcx
    .allocstack 8

    push    rdx
    .allocstack 8

    push    r8
    .allocstack 8

    push    r9
    .allocstack 8

    push    r10
    .allocstack 8

    push    r11
    .allocstack 8

DoneSaveVolIntRegs_Enter:
    ; reserve space for floating point registers to be saved
    sub     rsp, SIZEOF_STACK_ALLOC
    .allocstack SIZEOF_STACK_ALLOC

    ; save floating point return register
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 0h], xmm0
    .savexmm128 xmm0, OFFSETOF_XMM_SAVE + 0h
    
SaveVolFPRegs_Enter:
    ; save volatile floating point registers
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 10h], xmm1
    .savexmm128 xmm1, OFFSETOF_XMM_SAVE + 10h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 20h], xmm2
    .savexmm128 xmm2, OFFSETOF_XMM_SAVE + 20h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 30h], xmm3
    .savexmm128 xmm3, OFFSETOF_XMM_SAVE + 30h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 40h], xmm4
    .savexmm128 xmm4, OFFSETOF_XMM_SAVE + 40h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 50h], xmm5
    .savexmm128 xmm5, OFFSETOF_XMM_SAVE + 50h

DoneSaveVolFPRegs_Enter:
    .endprolog

    ; call C++ helper
    call                    EnterStub

    ; restore floating-point return register
    movdqa                  xmm0, [rsp + OFFSETOF_XMM_SAVE + 0h]

RestoreVolFPRegs_Enter:
    ; restore volatile floating point registers
    movdqa                  xmm1, [rsp + OFFSETOF_XMM_SAVE + 10h]
    movdqa                  xmm2, [rsp + OFFSETOF_XMM_SAVE + 20h]
    movdqa                  xmm3, [rsp + OFFSETOF_XMM_SAVE + 30h]
    movdqa                  xmm4, [rsp + OFFSETOF_XMM_SAVE + 40h]
    movdqa                  xmm5, [rsp + OFFSETOF_XMM_SAVE + 50h]

DoneRestoreVolFPRegs_Enter:
    ; restore the stack pointer 
    add                     rsp, SIZEOF_STACK_ALLOC

RestoreVolIntRegs_Enter:
    ; restore volatile integer registers
    pop                     r11
    pop                     r10
    pop                     r9
    pop                     r8
    pop                     rdx
    pop                     rcx

DoneRestoreVolIntRegs_Enter:
    ; restore integer return register
    pop                     rax

    ; return
    ret

EnterNaked ENDP

ALIGN 16
PUBLIC LeaveNaked

LeaveNaked PROC FRAME

    ; save integer return register
    push    rax
    .allocstack 8

SaveVolIntRegs_Leave:
    push    rcx
    .allocstack 8

    push    rdx
    .allocstack 8

    push    r8
    .allocstack 8

    push    r9
    .allocstack 8

    push    r10
    .allocstack 8

    push    r11
    .allocstack 8

DoneSaveVolIntRegs_Leave:
    ; reserve space for floating point registers to be saved
    sub     rsp, SIZEOF_STACK_ALLOC
    .allocstack SIZEOF_STACK_ALLOC

    ; save floating point return register
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 0h], xmm0
    .savexmm128 xmm0, OFFSETOF_XMM_SAVE + 0h
    
SaveVolFPRegs_Leave:
    ; save volatile floating point registers
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 10h], xmm1
    .savexmm128 xmm1, OFFSETOF_XMM_SAVE + 10h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 20h], xmm2
    .savexmm128 xmm2, OFFSETOF_XMM_SAVE + 20h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 30h], xmm3
    .savexmm128 xmm3, OFFSETOF_XMM_SAVE + 30h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 40h], xmm4
    .savexmm128 xmm4, OFFSETOF_XMM_SAVE + 40h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 50h], xmm5
    .savexmm128 xmm5, OFFSETOF_XMM_SAVE + 50h

DoneSaveVolFPRegs_Leave:
    .endprolog

    ; call C++ helper
    call                    LeaveStub

    ; restore floating-point return register
    movdqa                  xmm0, [rsp + OFFSETOF_XMM_SAVE + 0h]

RestoreVolFPRegs_Leave:
    ; restore volatile floating point registers
    movdqa                  xmm1, [rsp + OFFSETOF_XMM_SAVE + 10h]
    movdqa                  xmm2, [rsp + OFFSETOF_XMM_SAVE + 20h]
    movdqa                  xmm3, [rsp + OFFSETOF_XMM_SAVE + 30h]
    movdqa                  xmm4, [rsp + OFFSETOF_XMM_SAVE + 40h]
    movdqa                  xmm5, [rsp + OFFSETOF_XMM_SAVE + 50h]

DoneRestoreVolFPRegs_Leave:
    ; restore the stack pointer 
    add                     rsp, SIZEOF_STACK_ALLOC

RestoreVolIntRegs_Leave:
    ; restore volatile integer registers
    pop                     r11
    pop                     r10
    pop                     r9
    pop                     r8
    pop                     rdx
    pop                     rcx

DoneRestoreVolIntRegs_Leave:
    ; restore integer return register
    pop                     rax

    ; return
    ret

LeaveNaked ENDP

ALIGN 16
PUBLIC TailcallNaked

TailcallNaked PROC FRAME

    ; save integer return register
    push    rax
    .allocstack 8

SaveVolIntRegs_Tailcall:
    push    rcx
    .allocstack 8

    push    rdx
    .allocstack 8

    push    r8
    .allocstack 8

    push    r9
    .allocstack 8

    push    r10
    .allocstack 8

    push    r11
    .allocstack 8

DoneSaveVolIntRegs_Tailcall:
    ; reserve space for floating point registers to be saved
    sub     rsp, SIZEOF_STACK_ALLOC
    .allocstack SIZEOF_STACK_ALLOC

    ; save floating point return register
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 0h], xmm0
    .savexmm128 xmm0, OFFSETOF_XMM_SAVE + 0h
    
SaveVolFPRegs_Tailcall:
    ; save volatile floating point registers
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 10h], xmm1
    .savexmm128 xmm1, OFFSETOF_XMM_SAVE + 10h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 20h], xmm2
    .savexmm128 xmm2, OFFSETOF_XMM_SAVE + 20h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 30h], xmm3
    .savexmm128 xmm3, OFFSETOF_XMM_SAVE + 30h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 40h], xmm4
    .savexmm128 xmm4, OFFSETOF_XMM_SAVE + 40h
    
    movdqa  [rsp + OFFSETOF_XMM_SAVE + 50h], xmm5
    .savexmm128 xmm5, OFFSETOF_XMM_SAVE + 50h

DoneSaveVolFPRegs_Tailcall:
    .endprolog

    ; call C++ helper
    call                    TailcallStub

    ; restore floating-point return register
    movdqa                  xmm0, [rsp + OFFSETOF_XMM_SAVE + 0h]

RestoreVolFPRegs_Tailcall:
    ; restore volatile floating point registers
    movdqa                  xmm1, [rsp + OFFSETOF_XMM_SAVE + 10h]
    movdqa                  xmm2, [rsp + OFFSETOF_XMM_SAVE + 20h]
    movdqa                  xmm3, [rsp + OFFSETOF_XMM_SAVE + 30h]
    movdqa                  xmm4, [rsp + OFFSETOF_XMM_SAVE + 40h]
    movdqa                  xmm5, [rsp + OFFSETOF_XMM_SAVE + 50h]

DoneRestoreVolFPRegs_Tailcall:
    ; restore the stack pointer 
    add                     rsp, SIZEOF_STACK_ALLOC

RestoreVolIntRegs_Tailcall:
    ; restore volatile integer registers
    pop                     r11
    pop                     r10
    pop                     r9
    pop                     r8
    pop                     rdx
    pop                     rcx

DoneRestoreVolIntRegs_Tailcall:
    ; restore integer return register
    pop                     rax

    ; return
    ret

TailcallNaked ENDP

_text ENDS

END