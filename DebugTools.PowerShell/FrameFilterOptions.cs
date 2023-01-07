namespace DebugTools.PowerShell
{
    class FrameFilterOptions
    {
        public bool Unique { get; set; }

        public string[] Include { get; set; }

        public string[] Exclude { get; set; }

        public string[] StringValue { get; set; }

        public string[] TypeName { get; set; }

        public bool HasFilterValue { get; set; }
    }
}
