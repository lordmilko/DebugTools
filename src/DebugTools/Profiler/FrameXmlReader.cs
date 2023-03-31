using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using ClrDebug;

namespace DebugTools.Profiler
{
    class FrameXmlReader : FrameXmlBase
    {
        internal Dictionary<FunctionID, IMethodInfo> Methods { get; } = new Dictionary<FunctionID, IMethodInfo>();

        public IFrame[] Read(Stream stream)
        {
            IFrame[] results;

            using (var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                IgnoreWhitespace = true
            }))
            {
                reader.ReadStartElement(RootTag);
                ReadMethods(reader);
                results = ReadFrames(reader);
                reader.ReadEndElement();
            }

            return results;
        }

        private void ReadMethods(XmlReader reader)
        {
            reader.ReadStartElement(MethodsTag);

            while (reader.Name != MethodsTag)
            {
                switch (reader.Name)
                {
                    case MethodInfoTag:
                        ReadMethod(reader, false);
                        break;

                    case MethodInfoDetailedTag:
                        ReadMethod(reader, true);
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle method tag '{reader.Name}'.");
                }

                reader.Read();
            }

            reader.ReadEndElement();
        }

        private void ReadMethod(XmlReader reader, bool isDetailed)
        {
            var functionId = reader.GetAttribute(FunctionIDAttrib);
            var methodName = reader.GetAttribute(MethodNameAttrib);
            var typeName = reader.GetAttribute(TypeNameAttrib);
            var modulePath = reader.GetAttribute(ModulePathAttrib);

            FunctionID id = ulong.Parse(functionId, NumberStyles.HexNumber);
            IMethodInfo method;

            if (isDetailed)
            {
                var token = reader.GetAttribute(TokenAttrib);

                method = new MethodInfoDetailed(
                    id,
                    modulePath,
                    typeName,
                    methodName,
                    uint.Parse(token, NumberStyles.HexNumber)
                );
            }
            else
            {
                method = new MethodInfo(
                    id,
                    modulePath,
                    typeName,
                    methodName
                );
            }

            Methods[id] = method;
        }

        private IFrame[] ReadFrames(XmlReader reader)
        {
            var frames = new List<IFrame>();

            reader.ReadStartElement(FramesTag);

            while (reader.Name != FramesTag)
            {
                var frame = ReadFrame(reader);

                frames.Add(frame);
            }

            reader.ReadEndElement();

            return frames.ToArray();
        }

        private IFrame ReadFrame(XmlReader reader)
        {
            IFrame item;

            bool isEmpty = reader.IsEmptyElement;

            switch (reader.Name)
            {
                case RootFrameTag:
                    item = ReadRootFrame(reader);
                    break;

                case MethodFrameTag:
                    item = ReadMethodFrame(reader, (m, s) => new MethodFrame(m, s));
                    break;

                case MethodFrameDetailedTag:
                    item = ReadMethodFrame(reader, (m, s) =>
                    {
                        var enter = reader.GetAttribute(EnterAttrib);
                        var leave = reader.GetAttribute(LeaveAttrib);

                        var enterBytes = enter == null ? null : Convert.FromBase64String(enter);
                        var leaveBytes = leave == null ? null : Convert.FromBase64String(leave);

                        return new MethodFrameDetailed(m, s, enterBytes, leaveBytes);
                    });
                    break;

                case UnmanagedTransitionFrameTag:
                    item = ReadMethodFrame(reader, (m, s) =>
                    {
                        var kind = reader.GetAttribute(KindAttrib);

                        return new UnmanagedTransitionFrame(m, s, (FrameKind) Enum.Parse(typeof(FrameKind), kind));
                    });
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle frame tag '{reader.Name}'.");
            }

            while (reader.NodeType != XmlNodeType.EndElement && !isEmpty)
            {
                var child = ReadFrame(reader);
                child.Parent = item;

                item.Children.Add((IMethodFrame) child);
            }

            if (!isEmpty)
                reader.ReadEndElement();

            return item;
        }

        private IRootFrame ReadRootFrame(XmlReader reader)
        {
            var threadId = reader.GetAttribute(ThreadIdAttrib);
            var threadName = reader.GetAttribute(ThreadNameAttrib);

            if (threadId == null)
            {
                var lineInfo = (IXmlLineInfo)reader;

                throw new XmlException($"{reader.Name} was missing a {ThreadNameAttrib} attribute.", null, lineInfo.LineNumber, lineInfo.LinePosition);
            }

            var result = new RootFrame
            {
                ThreadId = Convert.ToInt32(threadId),
                ThreadName = string.IsNullOrWhiteSpace(threadName) ? null : threadName
            };

            reader.Read();

            return result;
        }

        private IMethodFrame ReadMethodFrame(
            XmlReader reader,
            Func<IMethodInfo, long, IMethodFrame> makeFrame)
        {
            var functionId = reader.GetAttribute(FunctionIDAttrib);
            FunctionID id = ulong.Parse(functionId, NumberStyles.HexNumber);

            var sequence = reader.GetAttribute(SequenceAttrib);

            if (!Methods.TryGetValue(id, out var method))
                throw new InvalidOperationException($"Could not find a method with FunctionID '{id}'.");

            var result = makeFrame(method, Convert.ToInt64(sequence));

            reader.Read();

            return result;
        }
    }
}
