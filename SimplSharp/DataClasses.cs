//using Crestron.SimplSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

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

        public static ushort FromBool(bool value)
        {
            return (value) ? SPlusBool.TRUE : SPlusBool.FALSE;
        }
        
        public static bool ToBool(ushort value)
        {
            return (value > SPlusBool.FALSE) ? true : false;
        }    
    }

    public enum RelayMode
    {
        None = 0,
        Basic = 1,
        Light = 2,
        Shutter = 3,
    }

    public class TasmotaConfig
    {
        [JsonProperty("ip", Order = 1)]
        public string IPAddress = "";

        [JsonProperty("dn", Order = 2)]
        public string DeviceName = "";
        
        [JsonProperty("fn", Order = 3)]
        public string[] FriendlyName = new string[CresmotaDevice.MaxChannels];

        [JsonProperty("hn", Order = 4)]
        public string HostName = "";

        [JsonProperty("mac", Order = 5)]
        public string MACAddress = "";

        [JsonProperty("md", Order = 6)]
        public string Model = "Cresmota";

        [JsonProperty("ty", Order = 7)]
        public int IsTuya = 0;

        [JsonProperty("if", Order = 8)]
        public int IsIfan = 0;

        [JsonProperty("ofln", Order = 9)]
        public string OfflinePayload = "Offline";

        [JsonProperty("onln", Order = 10)]
        public string OnlinePayload = "Online";

        [JsonProperty("state", Order = 11)]
        public string[] StatePayload = new string[4] { "OFF", "ON", "TOGGLE", "HOLD" };

        [JsonProperty("sw", Order = 12)]
        public string SoftwareVersion = CresmotaDevice.Version;

        [JsonProperty("t", Order = 13)]
        public string Topic;

        [JsonProperty("ft", Order = 14)]
        public string FullTopic = "%prefix%/%topic%/";

        [JsonProperty("tp", Order = 15)]
        public string[] TopicPrefix = new string[3] { "cmnd", "stat", "tele" };

        [JsonProperty("rl", Order = 16)]
        public int[] Relay = new int[CresmotaDevice.MaxChannels];

        [JsonProperty("swc", Order = 17)]
        public int[] SwitchConfiguration = new int[28];

        [JsonProperty("swn", Order = 18)]
        public string[] SwitchName = new string[28];

        [JsonProperty("btn", Order = 19)]
        public int[] Button = new int[CresmotaDevice.MaxChannels];

        [JsonProperty("so", Order = 20)]
        public Dictionary<string, int> SetOption = new Dictionary<string, int>
            {
            { "4", 0 },
            { "11", 0 },
            { "13", 0 },
            { "17", 0 },
            { "20", 0 },
            { "30", 0 },
            { "68", 0 },
            { "73", 0 },
            { "82", 0 },
            { "114", 0 },
            { "117", 0 }
        };

        [JsonProperty("lk", Order = 21)]
        public int LightLink = 0;

        [JsonProperty("lt_st", Order = 22)]
        public int LightSubtype = 0;

        [JsonProperty("bat", Order = 23)]
        public int IsOnBattery = 0;

        [JsonProperty("dslp", Order = 24)]
        public int IsDeepSleepCapable = 0;

        [JsonProperty("sho", Order = 25)]
        public List<int> ShutterOptions = new List<int>();

        [JsonProperty("sht", Order = 26)]
        public List<int> ShutterTilt = new List<int>();

        [JsonProperty("ver", Order = 27)]
        public const int Version = 1;

        public TasmotaConfig()
        {
            FriendlyName[0] = "Tasmota";
            for (int i = 0; i < SwitchConfiguration.Length; i++)
            {
                SwitchConfiguration[i] = -1;
            }
            SetOption["68"] = 1; // enable mulit-channel pwn instead of color pwm by default
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class TasmotaSensors
    {
        [JsonProperty("sn", Order = 1)]
        public Dictionary<string, string> Sensors = new Dictionary<string, string>
        {
            { "Time", DateTime.Now.ToString(format: "yyyy-MM-ddTHH:mm:ss") }
        };

        [JsonProperty("ver", Order = 2)]
        public const int Version = 1;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
