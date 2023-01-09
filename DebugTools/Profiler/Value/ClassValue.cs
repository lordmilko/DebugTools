using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DebugTools.Profiler
{
    public class ClassValue : ComplexTypeValue
    {
        public ClassValue(BinaryReader reader, ValueSerializer serializer) : base(reader, serializer)
        {
        }        
    }
}
