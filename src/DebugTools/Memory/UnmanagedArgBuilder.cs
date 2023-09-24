using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DebugTools.Memory
{
    class UnmanagedArgBuilder : IEnumerable<string>
    {
        private bool is32Bit;
        private List<string> values = new List<string>();

        public UnmanagedArgBuilder(bool is32Bit)
        {
            this.is32Bit = is32Bit;
        }

        public int Length
        {
            get
            {
                var ptrSize = is32Bit ? 4 : 8;

                int sum = 0;

                foreach (var value in values)
                {
                    //Include the length of the string, the null terminator, and the fact its Unicode
                    sum += (value.Length + 1) * 2;

                    sum += ptrSize;
                }

                return sum;
            }
        }

        public void Add(string value) => values.Add(value);

        public byte[] GetBytes(IntPtr memory)
        {
            var buffer = new byte[Length];

            var ptrSize = is32Bit ? 4 : 8;

            var startPos = values.Count * ptrSize;

            for (var i = 0; i < values.Count; i++)
            {
                var strBytes = Encoding.Unicode.GetBytes(values[i] + char.MinValue);

                var address = memory + startPos;

                if (ptrSize == 4)
                {
                    var addr = (uint) address;

                    var addrBytes = BitConverter.GetBytes(addr);

                    addrBytes.CopyTo(buffer, i * ptrSize);
                }
                else
                {
                    var addr = (ulong) address;

                    var addrBytes = BitConverter.GetBytes(addr);

                    addrBytes.CopyTo(buffer, i * ptrSize);
                }

                strBytes.CopyTo(buffer, startPos);

                startPos += strBytes.Length;
            }

            return buffer;
        }

        public IEnumerator<string> GetEnumerator() => values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
