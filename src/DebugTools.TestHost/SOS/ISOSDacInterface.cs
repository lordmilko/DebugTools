using System;
using System.Runtime.InteropServices;

namespace DebugTools.TestHost
{
    public delegate void MODULEMAPTRAVERSE(
        int index,
        ulong methodTable,
        IntPtr token);

    [Guid("436f00f2-b42a-4b9f-870c-e73db66ae930")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ISOSDacInterface
    {
        [PreserveSig]
        int GetThreadStoreData_();

        [PreserveSig]
        int GetAppDomainStoreData_();

        [PreserveSig]
        int GetAppDomainList(
            [In] int count,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ulong[] values,
            [Out] out int pNeeded);

        [PreserveSig]
        int GetAppDomainData_();

        [PreserveSig]
        int GetAppDomainName_();

        [PreserveSig]
        int GetDomainFromContext_();

        [PreserveSig]
        int GetAssemblyList(
            [In] ulong appDomain,
            [In] int count,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ulong[] values,
            [Out] out int pNeeded);

        [PreserveSig]
        int GetAssemblyData_();

        [PreserveSig]
        int GetAssemblyName_();

        [PreserveSig]
        int GetModule_();

        [PreserveSig]
        int GetModuleData_();

        [PreserveSig]
        int TraverseModuleMap(
            [In] ModuleMapType mmt,
            [In] ulong moduleAddr,
            [In, MarshalAs(UnmanagedType.FunctionPtr)] MODULEMAPTRAVERSE pCallback,
            [In] IntPtr token);

        [PreserveSig]
        int GetAssemblyModuleList(
            [In] ulong assembly,
            [In] int count,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ulong[] modules,
            [Out] out int pNeeded);

        [PreserveSig]
        int GetILForModule_();

        [PreserveSig]
        int GetThreadData_();

        [PreserveSig]
        int GetThreadFromThinlockID_();

        [PreserveSig]
        int GetStackLimits_();

        [PreserveSig]
        int GetMethodDescData_();

        [PreserveSig]
        int GetMethodDescPtrFromIP_();

        [PreserveSig]
        int GetMethodDescName_();

        [PreserveSig]
        int GetMethodDescPtrFromFrame_();

        [PreserveSig]
        int GetMethodDescFromToken_();

        [PreserveSig]
        int GetMethodDescTransparencyData_();

        [PreserveSig]
        int GetCodeHeaderData_();

        [PreserveSig]
        int GetJitManagerList_();

        [PreserveSig]
        int GetJitHelperFunctionName_();

        [PreserveSig]
        int GetJumpThunkTarget_();

        [PreserveSig]
        int GetThreadpoolData_();

        [PreserveSig]
        int GetWorkRequestData_();

        [PreserveSig]
        int GetHillClimbingLogEntry_();

        [PreserveSig]
        int GetObjectData_();

        [PreserveSig]
        int GetObjectStringData_();

        [PreserveSig]
        int GetObjectClassName_();

        [PreserveSig]
        int GetMethodTableName_();

        [PreserveSig]
        int GetMethodTableData_();

        [PreserveSig]
        int GetMethodTableSlot_();

        [PreserveSig]
        int GetMethodTableFieldData_();

        [PreserveSig]
        int GetMethodTableTransparencyData_();

        [PreserveSig]
        int GetMethodTableForEEClass_();

        [PreserveSig]
        int GetFieldDescData_();

        [PreserveSig]
        int GetFrameName_();

        [PreserveSig]
        int GetPEFileBase_();

        [PreserveSig]
        int GetPEFileName_();

        [PreserveSig]
        int GetGCHeapData_();

        [PreserveSig]
        int GetGCHeapList_();

        [PreserveSig]
        int GetGCHeapDetails_();

        [PreserveSig]
        int GetGCHeapStaticData_();

        [PreserveSig]
        int GetHeapSegmentData_();

        [PreserveSig]
        int GetOOMData_();

        [PreserveSig]
        int GetOOMStaticData_();

        [PreserveSig]
        int GetHeapAnalyzeData_();

        [PreserveSig]
        int GetHeapAnalyzeStaticData_();

        [PreserveSig]
        int GetDomainLocalModuleData_();

        [PreserveSig]
        int GetDomainLocalModuleDataFromAppDomain_();

        [PreserveSig]
        int GetDomainLocalModuleDataFromModule_();

        [PreserveSig]
        int GetThreadLocalModuleData_();

        [PreserveSig]
        int GetSyncBlockData_();

        [PreserveSig]
        int GetSyncBlockCleanupData_();

        [PreserveSig]
        int GetHandleEnum_();

        [PreserveSig]
        int GetHandleEnumForTypes_();

        [PreserveSig]
        int GetHandleEnumForGC_();

        [PreserveSig]
        int TraverseEHInfo_();

        [PreserveSig]
        int GetNestedExceptionData_();

        [PreserveSig]
        int GetStressLogAddress_();

        [PreserveSig]
        int TraverseLoaderHeap_();

        [PreserveSig]
        int GetCodeHeapList_();

        [PreserveSig]
        int TraverseVirtCallStubHeap_();

        [PreserveSig]
        int GetUsefulGlobals_();

        [PreserveSig]
        int GetClrWatsonBuckets_();

        [PreserveSig]
        int GetTLSIndex_();

        [PreserveSig]
        int GetDacModuleHandle_();

        [PreserveSig]
        int GetRCWData_();

        [PreserveSig]
        int GetRCWInterfaces_();

        [PreserveSig]
        int GetCCWData_();

        [PreserveSig]
        int GetCCWInterfaces_();

        [PreserveSig]
        int TraverseRCWCleanupList_();

        [PreserveSig]
        int GetStackReferences_();

        [PreserveSig]
        int GetRegisterName_();

        [PreserveSig]
        int GetThreadAllocData_();

        [PreserveSig]
        int GetHeapAllocData_();

        [PreserveSig]
        int GetFailedAssemblyList_();

        [PreserveSig]
        int GetPrivateBinPaths_();

        [PreserveSig]
        int GetAssemblyLocation_();

        [PreserveSig]
        int GetAppDomainConfigFile_();

        [PreserveSig]
        int GetApplicationBase_();

        [PreserveSig]
        int GetFailedAssemblyData_();

        [PreserveSig]
        int GetFailedAssemblyLocation_();

        [PreserveSig]
        int GetFailedAssemblyDisplayName_();
    }
}
