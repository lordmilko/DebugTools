namespace DebugTools.Profiler
{
    public enum FrameTokenKind
    {
        ThreadId,
        ThreadName,

        Space,
        Dot,
        Comma,
        OpenParen,
        CloseParen,

        ReturnValue,
        ReturnValueUnknown,

        TypeName,
        MethodName,

        Parameter,
        ParametersUnknown,

        FunctionId,
        FrameKind,
    }
}
