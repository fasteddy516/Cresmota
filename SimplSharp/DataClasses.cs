//using Crestron.SimplSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;

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

    public enum Prefix
    {
        Command = 0,
        Status = 1,
        Telemetry = 2,
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

    public class TasmotaInfo1
    {
        //{"Info1":{"Module":"Wemos Mini D1","Version":"13.4.0(tasmota)","FallbackTopic":"cmnd/DVES_7C040B_fb/","GroupTopic":"cmnd/tasmotas/"}}
        [JsonProperty(Order = 1)]
        public string Module = "";

        [JsonProperty(Order = 2)]
        public string Version = "";

        [JsonProperty(Order = 3)]
        public string FallbackTopic = "";

        [JsonProperty(Order = 4)]
        public string GroupTopic = "";

        public override string ToString()
        {
            return $"{{\"Info1\":{JsonConvert.SerializeObject(this)}}}";
        }
    }

    public class TasmotaInfo2
    {
        [JsonProperty(Order = 1)]
        public string WebServerMode = "Admin";

        [JsonProperty(Order = 2)]
        public string Hostname = "";

        [JsonProperty(Order = 3)]
        public string IPAddress = "";

        public override string ToString()
        {
            return $"{{\"Info2\":{JsonConvert.SerializeObject(this)}}}";
        }
    }

    public class TasmotaInfo3
    {
        [JsonProperty(Order = 1)]
        public string RestartReason = "Software/System restart";

        [JsonProperty(Order = 2)]
        public int BootCount = 1;

        public override string ToString()
        {
            return $"{{\"Info3\":{JsonConvert.SerializeObject(this)}}}";
        }
    }

    public class TasmotaState
    {
        [JsonIgnore]
        public DateTime StartTime { get; internal set; } = DateTime.Now;
        
        [JsonProperty(Order = 1)]
        public string Time
        {
            get { return DateTime.Now.ToString(format: "yyyy-MM-ddTHH:mm:ss"); }
        }

        [JsonProperty(Order = 2)]
        public string Uptime
        {
            get { return $"{(DateTime.Now - StartTime).Days}T{(DateTime.Now - StartTime).ToString(format: @"hh\:mm\:ss")}"; }
        }

        [JsonProperty(Order = 3)]
        public int UptimeSec
        {
            get { return (int)(DateTime.Now - StartTime).TotalSeconds; }
        }

        [JsonProperty(Order = 4)]
        public int Heap = 26;

        [JsonProperty(Order = 5)]
        public string SleepMode = "Dynamic";

        [JsonProperty(Order = 6)]
        public int Sleep = 50;

        [JsonProperty(Order = 7)]
        public int LoadAvg = 19;

        [JsonProperty(Order = 8)]
        public int MqttCount = 0;

        [JsonIgnore]
        public Channel[] Channels = new Channel[CresmotaDevice.MaxChannels];
        
        public TasmotaState()
        {
            for (int i = 0; i < CresmotaDevice.MaxChannels; i++)
            {
                Channels[i] = new Channel();
            }
        }
        
        public override string ToString()
        {
            string state = JsonConvert.SerializeObject(this);

            if (Channels.Length == 0)
                return state;

            state = state.Substring(0, state.Length - 1);

            for (int i = 0; i < Channels.Length; i++)
            {
                if (Channels[i].Mode == RelayMode.None)
                    break;
                state += $",\"POWER{i + 1}\":";
                state += (Channels[i].Power == 0) ? "\"OFF\"" : "\"ON\"";
                if (Channels[i].Mode == RelayMode.Light)
                {
                    state += $",\"Channel{i + 1}\":";
                    state += $"{Channels[i].Level}";
                }
            }
            state += "}";

            return state;
        }
    }

    public class Channel
    {
        public ushort Power = 0;
        public ushort Level = 0;
        public RelayMode Mode = RelayMode.None;
    }
}
