using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace DebugTools.Profiler
{
    unsafe class FrameXmlWriter : FrameXmlBase
    {
        private IFrame[] frames;

        public FrameXmlWriter(params IFrame[] frames)
        {
            this.frames = frames;
        }

        public void Write(Stream stream)
        {
            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Indent = true
            }))
            {
                writer.WriteStartDocument();

                writer.WriteStartElement(RootTag);
                WriteMethods(writer);
                WriteFrames(writer);
                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }

        private void WriteMethods(XmlWriter writer)
        {
            writer.WriteStartElement(MethodsTag);

            var methods = GetUniqueMethods();

            foreach (var method in methods)
            {
                writer.WriteStartElement(method is IMethodInfoDetailed ? MethodInfoDetailedTag : MethodInfoTag);

                writer.WriteAttributeString(FunctionIDAttrib, ((ulong)(void*)method.FunctionID.Value).ToString("X"));
                writer.WriteAttributeString(MethodNameAttrib, method.MethodName);
                writer.WriteAttributeString(TypeNameAttrib, method.TypeName);
                writer.WriteAttributeString(ModulePathAttrib, method.ModulePath);

                if (method is IMethodInfoDetailed d)
                {
                    writer.WriteAttributeString(TokenAttrib, d.Token.Value.ToString("X"));
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private HashSet<IMethodInfo> GetUniqueMethods()
        {
            var methods = new HashSet<IMethodInfo>(MethodInfoComparer.Instance);

            foreach (var frame in frames)
                GetUniqueMethodsInternal(frame, methods);

            return methods;
        }

        private void GetUniqueMethodsInternal(IFrame frame, HashSet<IMethodInfo> methods)
        {
            if (frame is IMethodFrame m)
                methods.Add(m.MethodInfo);

            foreach (var child in frame.Children)
                GetUniqueMethodsInternal(child, methods);
        }

        private void WriteFrames(XmlWriter writer)
        {
            writer.WriteStartElement(FramesTag);

            foreach (var frame in frames)
            {
                WriteFrame(frame, writer);
            }

            writer.WriteEndElement();
        }

        private void WriteFrame(IFrame frame, XmlWriter writer)
        {
            if (frame is IRootFrame r)
            {
                writer.WriteStartElement(RootFrameTag);

                writer.WriteAttributeString(ThreadIdAttrib, r.ThreadId.ToString());

                if (r.ThreadName != null)
                    writer.WriteAttributeString(ThreadNameAttrib, r.ThreadName);
            }
            else
            {
                if (frame is IMethodFrameDetailedInternal d)
                {
                    WriteMethodFrameCommon(d, MethodFrameDetailedTag, writer);

                    if (d.EnterValue != null)
                    {
                        var enterStr = Convert.ToBase64String(d.EnterValue);
                        writer.WriteAttributeString(EnterAttrib, enterStr);
                    }

                    if (d.ExitValue != null)
                    {
                        var leaveStr = Convert.ToBase64String(d.ExitValue);
                        writer.WriteAttributeString(LeaveAttrib, leaveStr);
                    }
                }
                else if (frame is IUnmanagedTransitionFrame u)
                {
                    WriteMethodFrameCommon(u, UnmanagedTransitionFrameTag, writer);
                    writer.WriteAttributeString(KindAttrib, u.Kind.ToString());
                }
                else
                {
                    var methodFrame = (IMethodFrame)frame;

                    WriteMethodFrameCommon(methodFrame, MethodFrameTag, writer);
                }
            }

            foreach (var child in frame.Children)
                WriteFrame(child, writer);

            writer.WriteEndElement();
        }

        private void WriteMethodFrameCommon(IMethodFrame frame, string elementName, XmlWriter writer)
        {
            writer.WriteStartElement(elementName);
            writer.WriteAttributeString(FunctionIDAttrib, ((ulong) (void*) frame.MethodInfo.FunctionID.Value).ToString("X"));
            writer.WriteAttributeString(SequenceAttrib, frame.Sequence.ToString());
        }
    }
}
