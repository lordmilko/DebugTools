﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using ClrDebug;
using DebugTools.Profiler;

namespace DebugTools.PowerShell
{
    public class MethodFrameColorWriter : IFormattedMethodFrameWriter
    {
        private MethodFrameFormatter formatter;

        public ConcurrentDictionary<object, byte> HighlightValues { get; set; }

        public WildcardPattern[] HighlightMethodNames { get; set; }

        public List<IFrame> HighlightFrames { get; set; }

        public IOutputSource Output { get; }

        public MethodFrameColorWriter(MethodFrameFormatter formatter, IOutputSource output)
        {
            this.formatter = formatter;
            Output = output;
        }

        private StringBuilder nameBuilder = new StringBuilder();
        private bool buildingName = false;

        public IMethodFrameWriter Write(object value, IFrame frame, FrameTokenKind kind)
        {
            bool highlightFrame = HighlightFrames?.Contains(frame) == true;

            if (value is FormattedValue f)
            {
                if (HighlightValues?.ContainsKey(f.Original) == true)
                    HighlightValue(f, GetMDI(frame), GetSigType(value, frame, kind));
                else
                    WriteMaybeHighlight(f.Formatted, highlightFrame);
            }
            else
            {
                switch (kind)
                {
                    case FrameTokenKind.TypeName:
                        buildingName = true;
                        nameBuilder.Append(value);
                        break;

                    case FrameTokenKind.MethodName:
                        buildingName = false;
                        nameBuilder.Append(value);
                        var str = nameBuilder.ToString();
                        nameBuilder.Clear();

                        if (ShouldHighlightMethod(str))
                            Output.WriteColor(str, ConsoleColor.Green);
                        else
                            WriteMaybeHighlight(str, highlightFrame);

                        break;

                    case FrameTokenKind.Parameter:
                    case FrameTokenKind.ReturnValue:
                        if (HighlightValues?.ContainsKey(value) == true)
                            HighlightValue(value, GetMDI(frame), GetSigType(value, frame, kind));
                        else
                            WriteMaybeHighlight(value, highlightFrame);
                        break;

                    default:
                        if (buildingName)
                            nameBuilder.Append(value);
                        else
                            WriteMaybeHighlight(value, highlightFrame);
                        break;
                }
            }

            return this;
        }

        private SigType GetSigType(object value, IFrame frame, FrameTokenKind kind)
        {
            if (value is FormattedValue f)
                value = f.Original;

            if (frame is IMethodFrameDetailed d)
            {
                var parameters = d.GetEnterParameters();

                var info = (IMethodInfoDetailed) d.MethodInfo;
                var sigMethod = info.SigMethod;

                if (sigMethod == null)
                    return null;

                switch (kind)
                {
                    case FrameTokenKind.Parameter:
                        var index = parameters.IndexOf(value);
                        return sigMethod.Parameters[index].Type;

                    case FrameTokenKind.ReturnValue:
                        return sigMethod.RetType;

                    default:
                        throw new NotImplementedException($"Don't know how to handle {nameof(FrameTokenKind)} '{kind}'.");
                }
            }

            return null;
        }

        private MetaDataImport GetMDI(IFrame frame)
        {
            if (frame is IMethodFrameDetailed d)
                return ((IMethodInfoDetailed) d.MethodInfo).GetMDI();

            return null;
        }

        private void HighlightValue(object value, MetaDataImport import, SigType sigType)
        {
            string str;
            object obj;

            if (value is FormattedValue f)
            {
                str = f.Formatted;
                obj = f.Original;
            }
            else
            {
                str = value.ToString();
                obj = value;
            }

            var builder = new StringBuilder();
            builder.Append(str);

            BuildHighlightValue(obj, builder, import, sigType, true, false);

            Output.WriteColor(builder.ToString(), ConsoleColor.Yellow);
        }

        private void BuildHighlightValue(object obj, StringBuilder builder, MetaDataImport import, SigType sigType, bool root, bool inPointer)
        {
            if (obj is ComplexTypeValue c && (sigType is SigClassType || sigType is SigValueType))
            {
                mdFieldDef[] fields;

                var blobFieldCount = c.FieldValues?.Count ?? 0;

                if (sigType is SigClassType sigClass)
                {
                    fields = import.EnumFields((mdTypeDef)sigClass.Token).ToArray();

                    if (fields.Length != blobFieldCount)
                        throw new InvalidOperationException($"{sigClass} had {blobFieldCount} serialized fields however had {fields.Length} metadata fields.");
                }
                else
                {
                    var sigValueType = (SigValueType)sigType;

                    fields = import.EnumFields((mdTypeDef)sigValueType.Token).ToArray();

                    //This should ALWAYS be true. Even if a field doesn't have a value, we still scrape
                    //the default value
                    if (fields.Length != blobFieldCount)
                        throw new InvalidOperationException($"{sigValueType} had {blobFieldCount} serialized fields however had {fields.Length} metadata fields.");
                }

                for(var i = 0; i < blobFieldCount; i++)
                {
                    if (HighlightValues.ContainsKey(c.FieldValues[i]))
                    {
                        var props = import.GetFieldProps(fields[i]);

                        if (inPointer)
                            builder.Append("->");
                        else
                            builder.Append(".");

                        builder.Append(props.szField);

                        var reader = new SigReader(props.ppvSigBlob, props.pcbSigBlob, fields[i], import);

                        var fieldSigType = reader.ParseField();
                        BuildHighlightValue(c.FieldValues[i], builder, import, fieldSigType, false, false);
                        break;
                    }
                }
            }
            else if (obj is SZArrayValue sz && !root && sigType is SigSZArrayType szSig)
            {
                for (var i = 0; i < sz.Value.Length; i++)
                {
                    if (HighlightValues.ContainsKey(sz.Value[i]))
                    {
                        builder.Append("[").Append(i).Append("]");

                        BuildHighlightValue(sz.Value[i], builder, import, szSig.ElementType, false, false);

                        break;
                    }
                }
            }
            else if (obj is ArrayValue arr && !root && sigType is SigArrayType arrSig)
            {
                var dimensionSizes = new int[arr.Value.Rank];

                for (var i = 0; i < arr.Rank; i++)
                {
                    var dimensionLength = arr.Value.GetLength(i);
                    dimensionSizes[i] = dimensionLength;
                }

                var totalLength = arr.Value.Length;

                var indices = new int[arr.Rank];

                var currentDimension = arr.Value.Rank - 1;

                for (var i = 0; i < totalLength; i++)
                {
                    var current = arr.Value.GetValue(indices);

                    if (HighlightValues.ContainsKey(current))
                    {
                        builder.Append("[");

                        for(var j = 0; j < indices.Length; j++)
                        {
                            builder.Append(indices[j]);

                            if (j < indices.Length - 1)
                                builder.Append(",");
                        }

                        builder.Append("]");

                        BuildHighlightValue(current, builder, import, arrSig.ElementType, false, false);

                        break;
                    }

                    ArrayValue.UpdateArrayIndices(indices, ref currentDimension, dimensionSizes, arr.Value.Rank);

                    currentDimension = arr.Value.Rank - 1;
                }
            }
            else if (obj is PtrValue ptr && sigType is SigPtrType ptrSig)
            {
                //If there's multiple levels of indirection, unwrap them all down to the lowest
                //level pointer
                while (ptr.Value is PtrValue v)
                {
                    ptr = v;
                    ptrSig = (SigPtrType) ptrSig.PtrType;
                }

                if (ptr.Value is StructValue s)
                {
                    BuildHighlightValue(ptr.Value, builder, import, ptrSig.PtrType, false, true);
                }
                else
                {
                    if (!root)
                        BuildHighlightValue(ptr.Value, builder, import, ptrSig.PtrType, false, true);
                }
            }
            else
            {
                if (!root && HighlightValues.ContainsKey(obj))
                    builder.Append("=").Append(obj);
            }
        }

        private void WriteMaybeHighlight(object message, bool highlightFrame)
        {
            if (highlightFrame)
                Output.WriteColor(message, ConsoleColor.Green);
            else
                Output.Write(message);
        }

        public void Print(IFrame frame)
        {
            formatter.Format(frame, this);
        }

        private bool ShouldHighlightMethod(string str)
        {
            if (HighlightMethodNames == null)
                return false;

            return HighlightMethodNames.Any(h => h.IsMatch(str));
        }
    }
}
