using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Threading;
using DebugTools.Dynamic;

namespace DebugTools.Ui
{
    public class WndProcMonitor : IDisposable
    {
        private static IReadOnlyDictionary<WM, (Type type, ConstructorInfo ctor, WindowMessageStructReader structReader)> messageTypeCache;

        static WndProcMonitor()
        {
            var builder = new DynamicWindowMessageBuilder();
            messageTypeCache = builder.Build();
        }

        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor mma;
        private EventWaitHandle hasDataEvent;
        private EventWaitHandle wasProcessedEvent;

        private CancellationTokenSource cts = new CancellationTokenSource();

        private Thread thread;
        private ConcurrentQueue<WindowMessage> queue = new ConcurrentQueue<WindowMessage>();

        public WndProcMonitor(Process process)
        {
            mmf = MemoryMappedFile.CreateNew($"DebugToolsMemoryMappedFile_WndProc_{process.Id}", 1000000000);
            mma = mmf.CreateViewAccessor();

            hasDataEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"DebugToolsHasDataEvent_WndProc_{process.Id}");
            wasProcessedEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $"DebugToolsWasProcessedEvent_WndProc_{process.Id}");

            thread = new Thread(ThreadProc);
        }

        public void Start() => thread.Start();

        public void Stop() => cts.Cancel();

        internal bool TryReadMessage(out WindowMessage message) => queue.TryDequeue(out message);

        private void ThreadProc()
        {
            while (!cts.IsCancellationRequested)
            {
                WaitHandle.WaitAny(new[] { cts.Token.WaitHandle, hasDataEvent });

                if (cts.IsCancellationRequested)
                    break;

                var reader = new MMAReader(mma);

                var numEntries = reader.ReadUInt32();

                for (var i = 0; i < numEntries; i++)
                {
                    WindowMessage message;

                    var size = reader.ReadUInt32();
                    var msg = (WM) reader.ReadUInt32();
                    var hwnd = reader.ReadPointer();
                    var wParam = reader.ReadPointer();
                    var lParam = reader.ReadPointer();

                    if (messageTypeCache.TryGetValue(msg, out var typeInfo))
                    {
                        var args = new List<object> { hwnd, msg, wParam, lParam };

                        if (typeInfo.structReader != null)
                            args.AddRange(typeInfo.structReader.ReadStructs(reader));

                        message = (WindowMessage) typeInfo.ctor.Invoke(args.ToArray());
                    }
                    else
                        message = new WindowMessage(hwnd, msg, wParam, lParam);

                    queue.Enqueue(message);
                }

                wasProcessedEvent.Set();
            }
        }

        public void Dispose()
        {
            mma?.Dispose();
            mmf?.Dispose();
            hasDataEvent?.Dispose();
            wasProcessedEvent?.Dispose();
        }
    }
}
