using System.Collections.Generic;
using FlaUI.Core.AutomationElements;

namespace DebugTools.Ui
{
    static class UiElementExtensions
    {
        public static IEnumerable<IUiElement> DescendantNodes(this IUiElement elm)
        {
            var raw = (AutomationElement) elm;

            foreach (var item in raw.FindAllDescendants())
                yield return UiElement.New(item);
        }

        public static IEnumerable<IUiElement> DescendantNodesAndSelf(this IUiElement elm)
        {
            yield return elm;

            foreach (var item in DescendantNodes(elm))
                yield return item;
        }
    }
}