using System.IO;

namespace DebugTools.Profiler
{
    public class ClassValue : ComplexTypeValue
    {
        public ClassValue(BinaryReader reader, ValueSerializer serializer) : base(reader, serializer)
        {
        }        
    }
}
