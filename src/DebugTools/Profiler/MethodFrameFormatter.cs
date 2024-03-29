﻿using System;
using System.Collections.Generic;

namespace DebugTools.Profiler
{
    public class MethodFrameFormatter
    {
        public static readonly MethodFrameFormatter Default = new MethodFrameFormatter(false);
        public static readonly MethodFrameFormatter WithoutNamespace = new MethodFrameFormatter(true);

        private bool excludeNamespace;

        private Action<IMethodFrame, IMethodFrameWriter>[] extras;

        public MethodFrameFormatter(bool excludeNamespace, bool includeSequence = false, bool includeModule = false)
        {
            this.excludeNamespace = excludeNamespace;

            List<Action<IMethodFrame, IMethodFrameWriter>> items = new List<Action<IMethodFrame, IMethodFrameWriter>>();

            if (includeSequence)
                items.Add((f, w) => w.Write(f.Sequence, f, FrameTokenKind.Sequence));

            if (includeModule)
                items.Add((f, w) => w.Write(f.MethodInfo.ModuleName, f, FrameTokenKind.Sequence));

            extras = items.ToArray();
        }

        public void Format(IFrame frame, IMethodFrameWriter writer)
        {
            if (frame is IRootFrame r)
            {
                if (r.ThreadName == null)
                    writer.Write(r.ThreadId, frame, FrameTokenKind.ThreadId);
                else
                {
                    writer
                        .Write(r.ThreadName, frame, FrameTokenKind.ThreadName)
                        .Write(" ", frame, FrameTokenKind.Space)
                        .Write(r.ThreadId, frame, FrameTokenKind.ThreadId);
                }
            }
            else if (frame is IMethodFrameDetailed d)
            {
                var exitResult = d.GetExitResult();

                if (exitResult == null)
                    writer.Write("<Error>", frame, FrameTokenKind.ReturnValueUnknown);
                else
                    writer.Write(StringifyValue(exitResult), frame, FrameTokenKind.ReturnValue);

                writer.Write(" ", frame, FrameTokenKind.Space);

                var info = d.MethodInfo;

                writer
                    .Write(GetTypeName(info.TypeName), frame, FrameTokenKind.TypeName)
                    .Write(".", frame, FrameTokenKind.Dot)
                    .Write(GetMethodName(info.MethodName), frame, FrameTokenKind.MethodName);

                writer.Write("(", frame, FrameTokenKind.OpenParen);

                var parameters = d.GetEnterParameters();

                if (parameters == null)
                    writer.Write("<Error>", frame, FrameTokenKind.ParametersUnknown); //If there's no parameters we get a list of count 0. The blob explicitly says "0 parameters"
                else
                {
                    for (var i = 0; i < parameters.Count; i++)
                    {
                        writer.Write(StringifyValue(parameters[i]), frame, FrameTokenKind.Parameter);

                        if (i < parameters.Count - 1)
                            writer.Write(",", frame, FrameTokenKind.Comma).Write(" ", frame, FrameTokenKind.Space);
                    }
                }

                writer.Write(")", frame, FrameTokenKind.CloseParen);

                WriteExtra(d, writer);
            }
            else if (frame is IMethodFrame m)
            {
                var info = m.MethodInfo;

                if (frame is IUnmanagedTransitionFrame f)
                {
                    writer
                        .Write(f.Kind, frame, FrameTokenKind.FrameKind)
                        .Write(" ", frame, FrameTokenKind.Space);
                }

                if (info is UnknownMethodInfo)
                {
                    writer.Write("0x" + info.FunctionID, frame, FrameTokenKind.FunctionId);
                }
                else
                {
                    writer
                        .Write(GetTypeName(info.TypeName), frame, FrameTokenKind.TypeName)
                        .Write(".", frame, FrameTokenKind.Dot)
                        .Write(GetMethodName(info.MethodName), frame, FrameTokenKind.MethodName);
                }

                WriteExtra(m, writer);
            }
            else
                throw new NotImplementedException($"Don't know how to handle frame of type '{frame}'.");
        }

        private void WriteExtra(IMethodFrame frame, IMethodFrameWriter writer)
        {
            if (extras.Length == 0)
                return;

            writer
                .Write(" ", frame, FrameTokenKind.Space)
                .Write("(", frame, FrameTokenKind.OpenParen);

            for (var i = 0; i < extras.Length; i++)
            {
                extras[i](frame, writer);

                if (i < extras.Length - 1)
                {
                    writer
                        .Write(",", frame, FrameTokenKind.Comma)
                        .Write(" ", frame, FrameTokenKind.Space);
                }
            }

            writer.Write(")", frame, FrameTokenKind.CloseParen);
        }

        private string GetTypeName(string name)
        {
            if (name == null)
                return "null";

            if (excludeNamespace)
            {
                var dot = name.LastIndexOf('.');

                if (dot != -1)
                    return name.Substring(dot + 1);
            }

            return name;
        }

        private string GetMethodName(string name)
        {
            //Interface names may be fully qualified

            if (excludeNamespace && name != null)
            {
                var lastDot = name.LastIndexOf('.');

                if (lastDot != -1 && lastDot > 0)
                {
                    var secondLastDot = name.LastIndexOf('.', lastDot - 1);

                    name = name.Substring(secondLastDot + 1);
                }    
            }

            return name;
        }

        private object StringifyValue(object value)
        {
            if (value is ComplexTypeValue c)
                return new FormattedValue(value, GetTypeName(c.Name));

            return value;
        }
    }
}
