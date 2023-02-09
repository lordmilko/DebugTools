using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ClrDebug;

namespace DebugTools.SOS
{
    [Serializable]
    public abstract class SOSStackFrame
    {
        public abstract SOSStackFrameType Type { get; }

        public CLRDATA_ADDRESS IP { get; }

        public CLRDATA_ADDRESS SP { get; }

        public string MethodName { get; protected set; }

        public string Name { get; protected set; }

        public string FullName { get; protected set; }

        public SOSParameterInfo[] Parameters { get; protected set; }

        public CLRDATA_ADDRESS MethodDesc { get; protected set; }

        public CLRDATA_ADDRESS MethodTable { get; protected set; }

        protected SOSStackFrame(CLRDATA_ADDRESS ip, CLRDATA_ADDRESS sp)
        {
            IP = ip;
            SP = sp;
        }

        protected string GetShortName(string name)
        {
            var bracket = name.IndexOf('(');

            var nameMatch = Regex.Match(name, "(.+?)\\((.+)\\)");

            if (nameMatch.Success)
            {
                var paramStr = nameMatch.Groups[2].Value;

                var builder = new StringBuilder(ProcessCommaList(paramStr, name, nameMatch.Groups[2].Index));

                var methodName = nameMatch.Groups[1].Value;

                builder.Replace(methodName, RemoveNamespace(methodName, true), 0, methodName.Length);

                name = builder.ToString();
            }

            return name;
        }

        protected string GetMethodName(string name)
        {
            if (name == null)
                return null;

            var square = name.IndexOf('[');
            var paren = name.IndexOf('(');

            if (paren == -1)
            {
                if (square == -1)
                    return name;

                return name;
            }
            else
            {
                if (square == -1)
                    return name.Substring(0, paren);

                return name.Substring(0, Math.Min(square, paren));
            }
        }

        protected SOSParameterInfo[] GetParameters(XCLRDataStackWalk stackWalk)
        {
            if (stackWalk.TryGetFrame(out var frame) != HRESULT.S_OK)
                return new SOSParameterInfo[0];

            if (frame.TryGetNumArguments(out var numArgs) != HRESULT.S_OK)
                return new SOSParameterInfo[0];

            var parameters = new List<SOSParameterInfo>();

            for (var i = 0; i < numArgs; i++)
                parameters.Add(new SOSParameterInfo(this, frame, i));

            return parameters.ToArray();
        }

        private string ProcessCommaList(string commaStr, string fullName, int commaStrStartOffset)
        {
            var delims = Regex.Matches(commaStr, "[, ]+");

            var paramInfo = new List<(int paramStart, int paramEnd)>();

            if (delims.Count > 0)
            {
                for (var i = 0; i < delims.Count; i++)
                {
                    var current = delims[i];

                    var paramStart = current.Index + current.Length;
                    int paramEnd = 0;

                    if (i == 0)
                    {
                        paramInfo.Add((0, current.Index));
                    }

                    if (i == delims.Count - 1)
                    {
                        paramEnd = commaStr.Length;
                    }
                    else
                    {
                        paramEnd = delims[i + 1].Index;
                    }

                    paramInfo.Add((paramStart, paramEnd));
                }
            }
            else
            {
                if (commaStr.Length > 0)
                {
                    paramInfo.Add((0, commaStr.Length));
                }
            }

            paramInfo.Reverse();

            var builder = new StringBuilder(fullName);

            foreach (var item in paramInfo)
            {
                var length = item.paramEnd - item.paramStart;

                var value = commaStr.Substring(item.paramStart, length);

                var originalStart = item.paramStart + commaStrStartOffset;

                builder.Replace(value, RemoveNamespace(value, false), originalStart, length);
            }

            return builder.ToString();
        }

        private string RemoveNamespace(string name, bool isMethod)
        {
            var genericStart = name.IndexOf('<');

            var start = name.Length - 1;

            if (genericStart != -1)
            {
                var generics = name.Substring(genericStart);

                var match = Regex.Match(generics, "<(.+)>");

                if (match.Success)
                {
                    var genericsStr = match.Groups[1].Value;

                    var nameWithFixedGenerics = ProcessCommaList(genericsStr, name, match.Groups[1].Index + genericStart);

                    name = nameWithFixedGenerics;
                    start = genericStart;
                }
            }

            var square = name.IndexOf("[[", StringComparison.InvariantCulture);

            if (square != -1)
                start = square;

            var last = name.LastIndexOf('.', start);

            if (last == -1)
                return name;

            if (isMethod)
            {
                last = name.LastIndexOf('.', last - 1);
            }

            return name.Substring(last + 1);
        }

        public override string ToString()
        {
            return MethodName;
        }
    }
}
