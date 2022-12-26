#pragma once

//Based on https://web.archive.org/web/20090322014322/http://blogs.msdn.com/jkeljo/archive/2005/08/11/450506.aspx
//All hooks are the same (info hooks factor in SIZE eltInfo before returning). All hooks can be defined the same in v3 since all 3 take the same number of parameters
//(unlike v2 where Tailcall takes 3 parameters instead of 4)

#include "DebugToolsProfiler.h"
#include "CValueTracer.h"

#include "EnterHook.h"
#include "LeaveHook.h"
#include "TailcallHook.h"

#include "EnterHookWithInfo.h"
#include "LeaveHookWithInfo.h"
#include "TailcallHookWithInfo.h"