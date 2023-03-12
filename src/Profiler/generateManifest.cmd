@echo off

REM This script must be executed under a Visual Studio Developer Command Prompt
REM Manage the event manifest using ECManGen which can be found in older Windows SDK versions
mc.exe -um -b DebugToolsProfiler.man

REM This manifest is not registered globally, so we don't care about all the resources that would get embedded in the DLL
del DebugToolsProfiler_MSG00001.bin
del DebugToolsProfilerTEMP.BIN
del DebugToolsProfiler.rc