namespace DebugTools.Profiler
{
    class FrameXmlBase
    {
        public const string RootTag = "Root";
        public const string MethodsTag = "Methods";
        public const string MethodInfoTag = "MethodInfo";
        public const string MethodInfoDetailedTag = "MethodInfoDetailed";
        public const string RootFrameTag = "RootFrame";
        public const string MethodFrameTag = "MethodFrame";
        public const string MethodFrameDetailedTag = "MethodFrameDetailed";
        public const string UnmanagedTransitionFrameTag = "UnmanagedTransitionFrame";

        public const string FramesTag = "Frames";
        public const string FunctionIDAttrib = "FunctionID";
        public const string ModulePathAttrib = "ModulePath";
        public const string TypeNameAttrib = "TypeName";
        public const string MethodNameAttrib = "MethodName";
        public const string TokenAttrib = "Token";
        public const string KindAttrib = "Kind";

        public const string ThreadIdAttrib = "ThreadId";
        public const string ThreadNameAttrib = "ThreadName";
        public const string SequenceAttrib = "Sequence";
        public const string EnterAttrib = "Enter";
        public const string LeaveAttrib = "Leave";
    }
}
