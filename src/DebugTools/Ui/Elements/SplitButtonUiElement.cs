﻿using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace DebugTools.Ui
{
    class SplitButtonUiElement : AutomationElement, IUiElement
    {
        public new IUiElement Parent => UiElement.New(base.Parent);

        public IUiElement[] Children => FindAllChildren().Select(UiElement.New).ToArray();

        public SplitButtonUiElement(FrameworkAutomationElementBase frameworkAutomationElement) : base(frameworkAutomationElement)
        {
        }
    }
}