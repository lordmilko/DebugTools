using System;
using System.Management.Automation.Host;

namespace Profiler.Tests
{
    public class FakePSHostRawUserInterface : PSHostRawUserInterface
    {
        public override ConsoleColor BackgroundColor { get; set; }

        public override Size BufferSize
        {
            get => new Size(120, 200);
            set => throw new NotSupportedException();
        }

        public override Coordinates CursorPosition { get; set; }

        public override int CursorSize { get; set; }

        public override ConsoleColor ForegroundColor { get; set; }

        public override bool KeyAvailable => true;

        public override Size MaxPhysicalWindowSize => new Size(100, 100);

        public override Size MaxWindowSize => new Size(100, 100);

        public override Coordinates WindowPosition { get; set; }

        public override Size WindowSize { get; set; }

        public override string WindowTitle { get; set; }

        public override void FlushInputBuffer()
        {
        }

        public override BufferCell[,] GetBufferContents(Rectangle rectangle) =>
            throw new NotImplementedException();

        public override KeyInfo ReadKey(ReadKeyOptions options) =>
            throw new NotImplementedException();

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
        }

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents) =>
            throw new NotImplementedException();

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill) =>
            throw new NotImplementedException();
    }
}
