using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using DebugTools.Dynamic;
using DebugTools.Ui;

namespace DebugTools.PowerShell
{
    class WindowMessageFormatProvider
    {
        private static ConstructorInfo psSnapInTypeAndFormatErrorsTypeCtor_ETS;
        private static ConstructorInfo psSnapInTypeAndFormatErrorsTypeCtor_FileName;
        private static PropertyInfo psSnapInTypeAndFormatErrorsTypeErrors;
        private static Type psPSSnapInTypeAndFormatErrorsCollectionType;

        private bool initialized;

        public static readonly WindowMessageFormatProvider Instance = new WindowMessageFormatProvider();

        public Dictionary<WM, string[]> ExtraProperties { get; private set; } = new Dictionary<WM, string[]>();

        static WindowMessageFormatProvider()
        {
            var psSnapInTypeAndFormatErrorsType = typeof(PSCmdlet).Assembly.GetType("System.Management.Automation.Runspaces.PSSnapInTypeAndFormatErrors");
            psSnapInTypeAndFormatErrorsTypeCtor_ETS = psSnapInTypeAndFormatErrorsType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(ExtendedTypeDefinition) }, null);
            psSnapInTypeAndFormatErrorsTypeCtor_FileName = psSnapInTypeAndFormatErrorsType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(string) }, null);
            psSnapInTypeAndFormatErrorsTypeErrors = psSnapInTypeAndFormatErrorsType.GetInternalPropertyInfo("Errors");
            psPSSnapInTypeAndFormatErrorsCollectionType = typeof(Collection<>).MakeGenericType(psSnapInTypeAndFormatErrorsType);
        }

        internal void Reset() => initialized = false;

        private WindowMessageFormatProvider()
        {
        }

        public void RegisterFormats(PSCmdlet cmdlet, Func<Func<WM, string, ExtendedTypeDefinition>, ExtendedTypeDefinition> getUnifiedRequestFormat)
        {
            if (initialized)
                return;

            var context = cmdlet.GetInternalProperty("Context");
            var formatDBManager = context.GetInternalProperty("FormatDBManager");
            var authorizationManager = context.GetInternalProperty("AuthorizationManager");
            var engineHostInterface = context.GetInternalProperty("EngineHostInterface");
            var initialSessionState = (InitialSessionState) context.GetInternalProperty("InitialSessionState");
            var updateDatabase = formatDBManager.GetInternalMethod("UpdateDataBase");

            var newETSDefinitions = GetFormats(getUnifiedRequestFormat);

            var originalFormats = initialSessionState.Formats.ToArray();

            var snapinFormats = (IList)Activator.CreateInstance(psPSSnapInTypeAndFormatErrorsCollectionType);

            try
            {
                //UpdateDataBase will clear all formats it knows about, so we need to re-specify them

                initialSessionState.Formats.Clear();

                foreach (var sessionStateFormat in originalFormats)
                {
                    initialSessionState.Formats.Add(sessionStateFormat);
                    snapinFormats.Add(GetPSSnapInTypeForExistingFormat(sessionStateFormat));
                }

                foreach (var etsDefinition in newETSDefinitions)
                {
                    initialSessionState.Formats.Add(new SessionStateFormatEntry(etsDefinition));
                    snapinFormats.Add(psSnapInTypeAndFormatErrorsTypeCtor_ETS.Invoke(new object[] {null, etsDefinition}));
                }

                updateDatabase.Invoke(formatDBManager, new[] { snapinFormats, authorizationManager, engineHostInterface, false });
            }
            catch
            {
                initialSessionState.Formats.Clear();
                initialSessionState.Formats.Add(originalFormats);
                throw;
            }

            Validate(snapinFormats);

            initialized = true;
        }

        private object GetPSSnapInTypeForExistingFormat(SessionStateFormatEntry format)
        {
            var name = format.FileName;
            PSSnapInInfo snapin = format.PSSnapIn;

            if (snapin != null && !string.IsNullOrEmpty(snapin.Name))
                name = snapin.Name;

            if (format.FormatData != null)
                return psSnapInTypeAndFormatErrorsTypeCtor_ETS.Invoke(new object[]{name, format.FormatData});

            return psSnapInTypeAndFormatErrorsTypeCtor_FileName.Invoke(new object[]{ name, format.FileName });
        }

        private ExtendedTypeDefinition[] GetFormats(Func<Func<WM, string, ExtendedTypeDefinition>, ExtendedTypeDefinition> getUnifiedRequestFormat)
        {
            var values = Enum.GetValues(typeof(WM)).Cast<WM>().ToArray();

            var formats = values.Select(v => GenerateFormat(v)).Where(v => v != null).ToList();

            var unified = getUnifiedRequestFormat(GenerateFormat);

            if (unified != null)
                formats.Add(unified);

            return formats.ToArray();
        }

        private ExtendedTypeDefinition GenerateFormat(WM message, string name = null)
        {
            var tableBuilder = TableControl.Create();

            var properties = GetProperties(message);

            if (properties == null)
                return null;

            tableBuilder.AddHeader(width: 30, label: "Window");

            for (var i = 0; i < properties.Length; i++)
                tableBuilder.AddHeader();

            var rowBuilder = tableBuilder.StartRowDefinition();

            rowBuilder.AddScriptBlockColumn("\"[$($_.Window.ControlType)] $($_.Window.Name)\"");

            for (var i = 0; i < properties.Length; i++)
                rowBuilder.AddPropertyColumn(properties[i]);

            rowBuilder.EndRowDefinition();

            var table = tableBuilder.EndTable();

            return new ExtendedTypeDefinition(
                $"{DynamicAssembly.Instance.Name}.{name ?? message.ToString()}",
                new[] {new FormatViewDefinition("Default", table)}
            );
        }

        private string[] GetProperties(WM message)
        {
            var list = new List<string>
            {
                nameof(WindowMessage.Message)
            };

            var fieldInfo = typeof(WM).GetMember(message.ToString())[0];

            bool attribRequiresColumn(WMParamAttribute a)
            {
                if (a.Type.Assembly != typeof(WindowMessageFormatProvider).Assembly && a.Type.Assembly != typeof(IUiElement).Assembly)
                    return true;

                if (a.Type.IsEnum)
                    return true;

                return !a.Type.IsValueType;
            }

            var wParamAttribs = fieldInfo.GetCustomAttributes<WPARAMAttribute>().Cast<WMParamAttribute>().Where(attribRequiresColumn).ToArray();
            var lParamAttribs = fieldInfo.GetCustomAttributes<LPARAMAttribute>().Cast<WMParamAttribute>().Where(attribRequiresColumn).ToArray();

            if (wParamAttribs.Length == 0 && lParamAttribs.Length == 0)
                return null;

            if (wParamAttribs.Any())
                list.AddRange(wParamAttribs.Select(a => a.Name));
            else
                list.Add(nameof(WindowMessage.wParam));

            if (lParamAttribs.Any())
                list.AddRange(lParamAttribs.Select(a => a.Name));
            else
                list.Add(nameof(WindowMessage.lParam));

            ExtraProperties[message] = wParamAttribs.Concat(lParamAttribs).Select(v => v.Name).ToArray();

            return list.ToArray();
        }

        private void Validate(IList entries)
        {
            foreach (var item in entries)
            {
                var errors = (ConcurrentBag<string>) psSnapInTypeAndFormatErrorsTypeErrors.GetValue(item);

                if (errors.Count > 0)
                    throw new InvalidOperationException(string.Join(", ", errors).TrimStart(' ', ','));
            }
        }
    }
}
