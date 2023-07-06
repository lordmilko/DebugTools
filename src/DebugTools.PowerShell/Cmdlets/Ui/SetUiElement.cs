using System;
using System.Management.Automation;
using DebugTools.Ui;
using FlaUI.Core.AutomationElements;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "UiElement")]
    public class SetUiElement : UiCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public IUiElement Element { get; set; }

        [Parameter(Mandatory = false, Position = 0)]
        public object Value { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        protected override void BeginProcessing()
        {
            if (!MyInvocation.BoundParameters.ContainsKey(nameof(Value)))
                throw new ParameterBindingException($"Value parameter is mandatory, however a value was not specified. If Value should be empty, specify $null.");
        }

        protected override void ProcessRecordEx()
        {
            if (Element is TextBox t)
            {
                if (t.IsReadOnly && !Force)
                    throw new InvalidOperationException($"Cannot modify {nameof(TextBox)} {Element}: element is readonly. To override, specify -Force");

                t.Text = GetValue<string>();
            }
            if (Element is CheckBox c)
            {
                c.IsChecked = GetValue<bool>();
            }
            else
                throw new InvalidOperationException($"Don't know how to set value on element of type {Element.ControlType}");
        }

        private T GetValue<T>()
        {
            if (typeof(T) == typeof(string))
                return (T) (object) Value?.ToString();

            if (Value == null)
                throw new InvalidOperationException($"Expected a value of type {typeof(T).Name}. Actual: null");

            if (Value.GetType() == typeof(T))
                return (T) Value;

            throw new InvalidOperationException($"Expected a value of type {typeof(T).Name}. Actual: {Value.GetType().Name}");
        }
    }
}
