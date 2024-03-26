using Crestron.SimplSharp;
using System;
using System.Reflection;


namespace Cresmota
{
    public partial class CresmotaDevice
    {
        public delegate void DebugStatusDelegateHandler(ushort value);
        public DebugStatusDelegateHandler DebugStatusDelegate { get; set; }

        public static bool RunningOnCrestron { get; private set; } = false;

        private Action<string> DebugOutput = null;

        private bool _debug = false;

        public bool Debug
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
                    DebugPrint($"+ Debugging ENABLED, platform = {platform}", DebugColor.Start);
                }
                else
                {
                    if (_debug && DebugOutput != null)
                        DebugPrint("- Debugging DISABLED", DebugColor.Stop);
                    _debug = false;
                    DebugOutput = null;
                }
            }
        }

        public bool DebugShowColor { get; set; } = false;
        public bool DebugShowTimestamp { get; set; } = true;
        public bool DebugShowIDStamp { get; set; } = true;

        public static class DebugColor
        {
            public const ANSIColor TX = ANSIColor.BrightYellow;
            public const ANSIColor RX = ANSIColor.BrightCyan;
            public const ANSIColor Error = ANSIColor.Red;
            public const ANSIColor Start = ANSIColor.Green;
            public const ANSIColor Stop = ANSIColor.Magenta;
            public const ANSIColor Info = ANSIColor.BrightWhite;
        }

        private void DebugPrint(string msg, ANSIColor color=ANSIColor.None)
        {
            if (Debug)
            {
                const string reset = "\u001b[0m";

                string device = (DebugShowIDStamp) ? $"(CRESMOTA-{ProgramSlot:D2}:{ID:D2}-S#) " : "";
                string timestamp = (DebugShowTimestamp) ? $"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")} " : "";

                if (DebugShowColor)
                {
                    string msgcolor = (color == ANSIColor.None) ? $"\u001b[97m" : $"\u001b[{(int)color}m";
                    DebugOutput($"\u001b[90m{timestamp}{device}{msgcolor}{msg}{reset}");
                }
                else
                {
                    DebugOutput($"{timestamp}{device}{msg}");
                }
            }
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