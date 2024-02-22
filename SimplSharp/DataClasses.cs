using Crestron.SimplSharp;
using System;

namespace Cresmota
{
    internal static class SPlusBool
    {
        public const ushort FALSE = 0;
        public const ushort TRUE = 1;

        public static bool IsTrue(ushort value)
        {
            return (value != SPlusBool.FALSE);
        }

        public static bool IsFalse(ushort value)
        {
            return (value == SPlusBool.FALSE);
        }

        public static ushort Convert(ushort value)
        {
            return (value > SPlusBool.FALSE) ? SPlusBool.TRUE : SPlusBool.FALSE;
        }
    }
}