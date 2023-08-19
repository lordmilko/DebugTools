using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using ClrDebug;
using DebugTools.Ui;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "UiMessage")]
    public class GetUiMessage : UiCmdlet
    {
        [Parameter(Mandatory = false)]
        public SwitchParameter Dbg { get; set; }

        [Parameter(Mandatory = false)]
        public WM[] Type { get; set; }

        [Parameter(Mandatory = false)]
        public WM[] ExcludeType { get; set; }

        private Dictionary<CORDB_ADDRESS, IUiElement> windowCache = new Dictionary<CORDB_ADDRESS, IUiElement>();

        private bool shouldRemoveTypeName;
        private string unifiedFormatName;

        protected override void BeginProcessing()
        {
            WindowMessageFormatProvider.Instance.RegisterFormats(this, generateFormat =>
            {
                /* We have a catch-22: we need to inspect the ExtraProperties map to know whether
                 * our values specified to -Type should be included, but we don't want to wait until
                 * the formats have actually been registered, since our whole goal here is to potentially
                 * add another format to the list! Thus we try and add this additional format via a callback
                 * that occurs after ExtraProperties is populated, but while we're still building up the list
                 * of formats to generate */

                shouldRemoveTypeName = GetShouldRemoveTypeName();

                if (!shouldRemoveTypeName)
                {
                    //Implicitly we have at least one -Type as GetShouldRemoveTypeName returned false.
                    //If any of values specified to -Type have more than one property, implicitly all of
                    //those values must have the same properties, hence we operate based on Type[0]
                    var properties = WindowMessageFormatProvider.Instance.ExtraProperties[Type[0]];

                    if (properties.Length > 0)
                    {
                        var unifiedFormatSuffix = $"WindowMessage_{string.Join("_", properties)}";

                        //All our requested types have custom properties, all of which are the same. Generate a custom
                        //format based on this
                        var format = generateFormat(Type[0], unifiedFormatSuffix);

                        unifiedFormatName = format.TypeName;

                        return format;
                    }
                }

                return null;
            });            
        }

        protected override void ProcessRecordEx()
        {
            var hostApp = Session.GetOrCreateHostApp(Dbg);

            while (!CancellationToken.IsCancellationRequested)
            {
                if (hostApp.TryReadMessage(Session.Process.Id, out var message))
                {
                    if (ShouldInclude(message))
                    {
                        if (!windowCache.TryGetValue(message.hWnd, out var window))
                        {
                            try
                            {
                                window = Session.FromHandle(message.hWnd);
                            }
                            catch
                            {
                                window = null;
                            }
                            
                            windowCache[message.hWnd] = window;
                        }

                        message.Window = window;

                        var pso = new PSObject(message);

                        /* When an object is emitted to the pipeline, PowerShell chooses a format to use based on its TypeName. When subsequent
                         * objects are emitted, if they do not match the TypeName we are using for formatting in this pipeline, they will be
                         * considered to be "out of band" and forcefully displayed as a ListViewItem. To avoid falling into this trap, we
                         * pre-emptively look at the types of messages we're planning to emit, and whether they'll all have the same properties
                         * or not. There's 3 scenarios we can fall into:
                         *     1. We're filtering for a specific window message. We can use the custom format we generated for it as is
                         *     
                         *     2. We're not filtering for any specific window message, or are filtering for two different messages with completely
                         *        different formats. We need to remove all of the custom formats on each window message type so that we automatically
                         *        fallback to the generic WindowMessage format we've defined in our ps1xml
                         *        
                         *     3. We're filtering for two window messages that actually have the same shape. We need to remove all normal formats
                         *        that could be applied, and instead synthesize a brand new format just for them to ensure they all get displayed
                         *        in a unified way
                         */

                        if (shouldRemoveTypeName)
                            pso.TypeNames.Remove(message.GetType().FullName);

                        if (unifiedFormatName != null)
                        {
                            pso.TypeNames.Remove(typeof(WindowMessage).FullName);

                            pso.TypeNames.Add(unifiedFormatName);
                        }

                        WriteObject(message);
                    }
                }
                else
                    Thread.Sleep(10);
            }
        }

        private bool GetShouldRemoveTypeName()
        {
            if (Type != null)
            {
                var extras = Type.Select(t =>
                {
                    if (WindowMessageFormatProvider.Instance.ExtraProperties.TryGetValue(t, out var values))
                        return values;

                    return new string[0];
                }).ToArray();

                var first = extras[0];
                var remaining = extras.Skip(1).ToArray();

                if (remaining.Any(r => r.Length != first.Length))
                    return true;

                for (var i = 0; i < remaining.Length; i++)
                {
                    for (var j = 0; j < first.Length; j++)
                    {
                        if (first[j] != remaining[i][j])
                            return true;
                    }
                }

                return false;
            }

            return true;
        }

        private bool ShouldInclude(WindowMessage message)
        {
            if (Type == null || Type.Length == 0)
            {
                if (ExcludeType == null || ExcludeType.Length == 0)
                    return true;

                return ExcludeType.All(t => message.Message != t);
            }

            if (Type.Any(t => t == message.Message))
            {
                if (ExcludeType != null && ExcludeType.Any(t => t == message.Message))
                    return false;

                return true;
            }

            return false;
        }
    }
}
