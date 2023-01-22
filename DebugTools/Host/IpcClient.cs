using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;

namespace DebugTools.Host
{
    internal class IpcClient : IDisposable
    {
        private static int connectionIndex;

        private IpcClientChannel channel;

        public void Connect(Process process)
        {
            var channelName = GetIpcClientChannelName(process);

            channel = new IpcClientChannel(channelName, null);
            ChannelServices.RegisterChannel(channel, true);
        }

        private static string GetIpcClientChannelName(Process process)
        {
            var index = Interlocked.Increment(ref connectionIndex) - 1;

            if (index == 0)
                return $"DebugTools IPC channel client for {process.Id}";
            else
                return $"DebugTools IPC channel client for {process.Id} ({index})";
        }

        public void Close()
        {
            if (channel != null && ChannelServices.RegisteredChannels.Contains(channel))
            {
                ChannelServices.UnregisterChannel(channel);
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}