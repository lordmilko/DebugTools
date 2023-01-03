namespace DebugTools.Profiler
{
    public class VoidValue
    {
        public static VoidValue Instance = new VoidValue();

        public override string ToString()
        {
            return "void";
        }
    }
}
