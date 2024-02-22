using Crestron.SimplSharp;
using System;


namespace Cresmota
{
    public partial class CresmotaDevice
    {
        public delegate void DebugStatusDelegateHandler(ushort value);
        public DebugStatusDelegateHandler DebugStatusDelegate { get; set; }

        public static bool RunningOnCrestron { get; private set; } = false;

        private static Action<string> DebugOutput = null;

        private static bool _debug = false;

        public static bool Debug
        {
            get
            {
                return _debug;
            }
            set
            {
                if (value)
                {
                    string platform;
                    GetPlatform();
                    if (RunningOnCrestron)
                    {
                        DebugOutput = CrestronConsole.PrintLine;
                        platform = "CRESTRON";
                    }
                    else
                    {
                        DebugOutput = Console.WriteLine;
                        platform = "OTHER";
                    }
                    _debug = true;
                    DebugPrint($"+ Debugging ENABLED, platform = {platform}");
                }
                else
                {
                    if (_debug && DebugOutput != null)
                        DebugPrint("- Debugging DISABLED");
                    _debug = false;
                    DebugOutput = null;
                }
            }
        }

        private static void DebugPrint(string msg)
        {
            if (Debug)
                DebugOutput($"(CRESMOTA-S#) {msg}");
        }

        public void StartDebugging()
        {
            Debug = true;
            DebugStatusDelegate?.Invoke(SPlusBool.TRUE);
        }

        public void StopDebugging()
        {
            Debug = false;
            DebugStatusDelegate?.Invoke(SPlusBool.FALSE);
        }

        private static string GetPlatform()
        {
            try
            {
                eDevicePlatform platform = CrestronEnvironment.DevicePlatform;
                if (platform == eDevicePlatform.Appliance)
                {
                    RunningOnCrestron = true;
                    return "Crestron";
                }
                else
                    throw new NotSupportedException();
            }
            catch
            {
                RunningOnCrestron = false;
                return "Other";
            }
        }
    }
}