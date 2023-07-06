using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace DebugTools.Ui
{
    public static class UiElement
    {
        private static ConditionalWeakTable<AutomationElement, IUiElement> cache = new ConditionalWeakTable<AutomationElement, IUiElement>();

        public static IUiElement New(AutomationElement element)
        {
            if (element == null)
                return null;

            if (cache.TryGetValue(element, out var existing))
                return existing;

            existing = NewInternal(element);

            cache.Add(element, existing);

            return existing;
        }

        private static IUiElement NewInternal(AutomationElement element)
        {
            var raw = element.FrameworkAutomationElement;

            switch (element.Properties.ControlType.ValueOrDefault)
            {
                case ControlType.Unknown:
                    return new UnknownUiElement(raw);

                case ControlType.AppBar:
                    return new AppBarUiElement(raw);

                case ControlType.Button:
                    return new ButtonUiElement(raw);

                case ControlType.Calendar:
                    return new CalendarUiElement(raw);

                case ControlType.CheckBox:
                    return new CheckBoxUiElement(raw);

                case ControlType.ComboBox:
                    return new ComboBoxUiElement(raw);

                case ControlType.Custom:
                    return new CustomUiElement(raw);

                case ControlType.DataGrid:
                    return new DataGridUiElement(raw);

                case ControlType.DataItem:
                    return new DataItemUiElement(raw);

                case ControlType.Document:
                    return new DocumentUiElement(raw);

                case ControlType.Edit:
                    return new EditUiElement(raw);

                case ControlType.Group:
                    return new GroupUiElement(raw);

                case ControlType.Header:
                    return new HeaderUiElement(raw);

                case ControlType.HeaderItem:
                    return new HeaderItemUiElement(raw);

                case ControlType.Hyperlink:
                    return new HyperlinkUiElement(raw);

                case ControlType.Image:
                    return new ImageUiElement(raw);

                case ControlType.List:
                    return new ListUiElement(raw);

                case ControlType.ListItem:
                    return new ListItemUiElement(raw);

                case ControlType.MenuBar:
                    return new MenuBarUiElement(raw);

                case ControlType.Menu:
                    return new MenuUiElement(raw);

                case ControlType.MenuItem:
                    return new MenuItemUiElement(raw);

                case ControlType.Pane:
                    return new PaneUiElement(raw);

                case ControlType.ProgressBar:
                    return new ProgressBarUiElement(raw);

                case ControlType.RadioButton:
                    return new RadioButtonUiElement(raw);

                case ControlType.ScrollBar:
                    return new ScrollBarUiElement(raw);

                case ControlType.SemanticZoom:
                    return new SemanticZoomUiElement(raw);

                case ControlType.Separator:
                    return new SeparatorUiElement(raw);

                case ControlType.Slider:
                    return new SliderUiElement(raw);

                case ControlType.Spinner:
                    return new SpinnerUiElement(raw);

                case ControlType.SplitButton:
                    return new SplitButtonUiElement(raw);

                case ControlType.StatusBar:
                    return new StatusBarUiElement(raw);

                case ControlType.Tab:
                    return new TabUiElement(raw);

                case ControlType.TabItem:
                    return new TabItemUiElement(raw);

                case ControlType.Table:
                    return new TableUiElement(raw);

                case ControlType.Text:
                    return new TextUiElement(raw);

                case ControlType.Thumb:
                    return new ThumbUiElement(raw);

                case ControlType.TitleBar:
                    return new TitleBarUiElement(raw);

                case ControlType.ToolBar:
                    return new ToolBarUiElement(raw);

                case ControlType.ToolTip:
                    return new ToolTipUiElement(raw);

                case ControlType.Tree:
                    return new TreeUiElement(raw);

                case ControlType.TreeItem:
                    return new TreeItemUiElement(raw);

                case ControlType.Window:
                    return new WindowUiElement(raw);

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(AutomationElement)} of type {element.ControlType}");
            }
        }

        internal static T GetProperty<T>(AutomationProperty<T> property) => property.ValueOrDefault;
    }
}
