using System;
using System.Management.Automation;
using System.Runtime.InteropServices;
using DebugTools.Ui;

namespace DebugTools.PowerShell.Cmdlets
{
    [Cmdlet(VerbsCommunications.Send, "UiMessage")]
    internal class SendUiMessage : DebugToolsCmdlet
    {
        private const string FullFull = "FullFullSet";
        private const string FullPart = "FullPartSet";
        private const string PartFull = "PartFullSet";
        private const string PartPart = "PartPartSet";

        [Parameter(Mandatory = false, Position = 0)]
        public IntPtr hWnd { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public WM Message { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = FullFull)]
        [Parameter(Mandatory = false, ParameterSetName = FullPart)]
        public object wParam { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = FullFull)]
        [Parameter(Mandatory = false, ParameterSetName = PartFull)]
        public object lParam { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = FullPart)]
        [Parameter(Mandatory = false, ParameterSetName = PartFull)]
        [Parameter(Mandatory = false, ParameterSetName = PartPart)]
        public object wParamHIWORD { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = FullPart)]
        [Parameter(Mandatory = false, ParameterSetName = PartFull)]
        [Parameter(Mandatory = false, ParameterSetName = PartPart)]
        public object wParamLOWORD { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = FullPart)]
        [Parameter(Mandatory = false, ParameterSetName = PartFull)]
        [Parameter(Mandatory = false, ParameterSetName = PartPart)]
        public object lParamHIWORD { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = FullPart)]
        [Parameter(Mandatory = false, ParameterSetName = PartFull)]
        [Parameter(Mandatory = false, ParameterSetName = PartPart)]
        public object lParamLOWORD { get; set; }

        protected override void BeginProcessing()
        {
            if (MyInvocation.BoundParameters.ContainsKey(nameof(hWnd)))
            {
                var session = DebugToolsSessionState.Services.GetImplicitSubSession<LocalUiSession>(false);

                if (session == null)
                    throw new ParameterBindingException($"-{nameof(hWnd)} must be specified when there is no active UI Session");

                hWnd = session.Root.Handle;
            }
        }

        protected override void ProcessRecord()
        {
            IntPtr wParamNative = IntPtr.Zero;
            IntPtr lParamNative = IntPtr.Zero;

            try
            {
                switch (ParameterSetName)
                {
                    case FullFull:
                        wParamNative = GetNative(wParam);
                        lParamNative = GetNative(lParam);
                        break;

                    case FullPart:
                        wParamNative = GetNative(wParam);
                        lParamNative = GetNative(lParamHIWORD, lParamLOWORD);
                        break;

                    case PartFull:
                        wParamNative = GetNative(wParamHIWORD, wParamLOWORD);
                        lParamNative = GetNative(lParam);
                        break;

                    case PartPart:
                        wParamNative = GetNative(wParamHIWORD, wParamLOWORD);
                        lParamNative = GetNative(lParamHIWORD, lParamLOWORD);
                        break;

                    default:
                        throw new UnknownParameterSetException(ParameterSetName);
                }

                var result = User32.SendMessageW(
                    hWnd,
                    Message,
                    wParamNative,
                    lParamNative
                );

                WriteObject(result);
            }
            finally
            {
                FreeNative(wParamNative);
                FreeNative(lParamNative);
            }
        }

        private IntPtr GetNative(object managed)
        {
            if (managed == null)
                return IntPtr.Zero;

            throw new NotImplementedException($"Marshalling values of type {managed.GetType().Name} to native is not yet implemented");
        }

        private IntPtr GetNative(object hiword, object loword)
        {
            //todo:
            //-verify the object types arent structs
            //-verify this works for x64 and x32

            //var nativeHIWORD = GetNative(hiword);
            //var nativeLOWORD = GetNative(loword);

            throw new NotImplementedException("Marshalling hiwords and lowords is not yet implemented");
        }

        private void FreeNative(IntPtr value)
        {
            if (value != IntPtr.Zero)
                Marshal.FreeHGlobal(value);
        }
    }
}
