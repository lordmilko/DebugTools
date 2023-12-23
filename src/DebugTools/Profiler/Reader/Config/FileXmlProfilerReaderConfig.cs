using System;

namespace DebugTools.Profiler
{
    class FileXmlProfilerReaderConfig : IProfilerReaderConfig
    {
        public ProfilerSessionType SessionType => ProfilerSessionType.XmlFile;

        public ProfilerSetting[] Settings => Array.Empty<ProfilerSetting>();

        public string Path { get; }

        public FileXmlProfilerReaderConfig(string path)
        {
            Path = path;
        }
    }
}
