using System;
using FlaUI.Core.Definitions;

namespace DebugTools.Ui
{
    public interface IUiElement
    {
        string Name { get; }

        ControlType ControlType { get; }

        IUiElement Parent { get; }

        IUiElement[] Children { get; }

        IntPtr Handle { get; }
    }
}
