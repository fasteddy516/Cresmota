//using Crestron.SimplSharp;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom;
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

    public enum ANSIColor
    {
        None = 0,
        Black = 30,
        Red = 31,
        Green = 32,
        Yellow = 33,
        Blue = 34,
        Magenta = 35,
        Cyan = 36,
        White = 37,
        BrightBlack = 90,
        BrightRed = 91,
        BrightGreen = 92,
        BrightYellow = 93,
        BrightBlue = 94,
        BrightMagenta = 95,
        BrightCyan = 96,
        BrightWhite = 97
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


    public class Message
    {
        public string Topic = "";
        public string Payload = "";
        public bool Retained = false;
    }
    
    
    public class Channel
    {
        public ushort Power = 0;
        public ushort Level = 0;
        public RelayMode Mode = RelayMode.None;
    }

    
    public class TasmotaData
    {
        // The following properties represent Tasmota constructs that are used to generate JSON strings for MQTT responses.
        private TasmotaDiscoveryConfig discoveryConfig;
        private TasmotaDiscoverySensors discoverySensors;
        private TasmotaInfo1 info1;
        private TasmotaInfo2 info2;
        private TasmotaInfo3 info3;
        private TasmotaState state;
        private TasmotaStatus1 status1;
        private TasmotaStatus11 status11;

        // The following properties represent data shared between various Tasmota data constructs.
        // Cresmota operates on these properties directly, while the Tasmota constructs above use them to generate JSON strings.
        public string SoftwareVersion = CresmotaDevice.Version;
        public string Model = "Cresmota";
        public string DeviceName = "";
        public int IsTuya = 0;
        public int IsIfan = 0;
        public string[] FriendlyName = new string[CresmotaDevice.MaxChannels];
        public int[] Relay = new int[CresmotaDevice.MaxChannels];
        public int[] SwitchConfiguration = new int[28];
        public string[] SwitchName = new string[28];
        public int[] Button = new int[CresmotaDevice.MaxChannels];
        public Dictionary<string, int> SetOption;
        public int LightLink = 0;
        public int LightSubtype = 0;
        public List<int> ShutterOptions = new List<int>();
        public List<int> ShutterTilt = new List<int>();
        public Dictionary<string, string> Sensors;
        public string IPAddress = "";
        public string Hostname = "";
        public string MACAddress = "XXXXXXXXXXXX";
        public string OfflinePayload = "Offline";
        public string OnlinePayload = "Online";
        public string[] StatePayload = new string[4] { "OFF", "ON", "TOGGLE", "HOLD" };
        public string Topic = "";
        public string FullTopic = "%prefix%/%topic%/";
        public string[] TopicPrefix = new string[3] { "cmnd", "stat", "tele" };
        public string FallbackTopic => $"{TopicPrefix[(int)Prefix.Command]}/DVES_{MACAddress.Substring(6)}_fb/";
        public string GroupTopic => $"{TopicPrefix[(int)Prefix.Command]}/tasmotas/";
        public int MqttCount = 0;
        public DateTime StartTime { get; internal set; } = DateTime.Now;
        public string StartupUTC => StartTime.ToString(format: "yyyy-MM-ddTHH:mm:ss");
        public string BCResetTime => StartupUTC;
        public string Time => DateTime.Now.ToString(format: "yyyy-MM-ddTHH:mm:ss");
        public string Uptime => $"{(DateTime.Now - StartTime).Days}T{(DateTime.Now - StartTime).ToString(format: @"hh\:mm\:ss")}";
        public int UptimeSec => (int)(DateTime.Now - StartTime).TotalSeconds;
        public string State => state.ToString();

        // The following properties are not relevant to Cresmota but are included to match Tasmota data as closely as possible.
        // The values were selected based on Tasmota defaults where applicable, and from a Wemos Mini D1 running Tasmota 13.4.0 where required.
        public int IsOnBattery = 0;
        public int IsDeepSleepCapable = 0;
        public string WebServerMode = "Admin";
        public string RestartReason = "Software/System restart";
        public int BootCount = 1;
        public int Baudrate = 115200;
        public string SerialConfig = "8N1";
        public string OtaUrl = "http://ota.tasmota.com/tasmota/release/tasmota.bin";
        public int CfgHolder = 4617;
        public int SaveCount = 1;
        public string SaveAddress = "F8000";
        public int Heap = 26;
        public string SleepMode = "Dynamic";
        public int Sleep = 50;
        public int LoadAvg = 19;

        // This is used internally by Cresmota to track the state of each channel.
        public Channel[] Channels = new Channel[CresmotaDevice.MaxChannels];

        // Data constructs and certain default values are initialized in the constructor
        public TasmotaData()
        {
            this.discoveryConfig = new TasmotaDiscoveryConfig(this);
            this.discoverySensors = new TasmotaDiscoverySensors(this);
            this.info1 = new TasmotaInfo1(this);
            this.info2 = new TasmotaInfo2(this);
            this.info3 = new TasmotaInfo3(this);
            this.state = new TasmotaState(this);
            this.status1 = new TasmotaStatus1(this);
            this.status11 = new TasmotaStatus11(this);

            FriendlyName[0] = "Tasmota";

            for (int i = 0; i < SwitchConfiguration.Length; i++)
            {
                SwitchConfiguration[i] = -1;
            }

            for (int i = 0; i < CresmotaDevice.MaxChannels; i++)
            {
                Channels[i] = new Channel();
            }

            SetOption = new Dictionary<string, int>
            {
                { "4", 0 },
                { "11", 0 },
                { "13", 0 },
                { "17", 0 },
                { "20", 0 },
                { "30", 0 },
                { "68", 1 }, // enable multi-channel pwn instead of color pwm by default
                { "73", 0 },
                { "82", 0 },
                { "114", 0 },
                { "117", 0 }
            };

            Sensors = new Dictionary<string, string>
            {
                { "Time", Time }
            };
        }

        public Message OnlineMessage => new Message
        {
            Topic = $"{TopicPrefix[(int)Prefix.Telemetry]}/{Topic}/LWT",
            Payload = OnlinePayload,
            Retained = true
        };

        public Message OfflineMessage => new Message
        {
            Topic = $"{TopicPrefix[(int)Prefix.Telemetry]}/{Topic}/LWT",
            Payload = OfflinePayload,
            Retained = true
        };
        
        public Message DiscoveryConfigMessage => new Message
        {
            Topic = discoveryConfig.Topic,
            Payload = discoveryConfig.ToString(),
            Retained = true
        };

        public Message DiscoverySensorsMessage => new Message
        {
            Topic = discoverySensors.Topic,
            Payload = discoverySensors.ToString(),
            Retained = true
        };

        public Message Info1Message => new Message
        {
            Topic = info1.Topic,
            Payload = info1.ToString(),
            Retained = false
        };

        public Message Info2Message => new Message
        {
            Topic = info2.Topic,
            Payload = info2.ToString(),
            Retained = false
        };

        public Message Info3Message => new Message
        {
            Topic = info3.Topic,
            Payload = info3.ToString(),
            Retained = false
        };

        public Message Status1Message => new Message
        {
            Topic = status1.Topic,
            Payload = status1.ToString(),
            Retained = false
        };

        public Message Status11Message => new Message
        {
            Topic = status11.Topic,
            Payload = status11.ToString(),
            Retained = false
        };

        public Message StateMessage => new Message
        {
            Topic = $"{TopicPrefix[(int)Prefix.Telemetry]}/{Topic}/STATE",
            Payload = state.ToString(),
            Retained = false
        };
    }
    

    internal class TasmotaDataDerivative
    {
        protected TasmotaData data;

        public TasmotaDataDerivative(TasmotaData data)
        {
            this.data = data;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }


    internal class TasmotaDiscoveryConfig : TasmotaDataDerivative
    {
        [JsonProperty(Order = 1)]
        public string ip => data.IPAddress;

        [JsonProperty(Order = 2)]
        public string dn => data.DeviceName;
        
        [JsonProperty(Order = 3)]
        public string[] fn => data.FriendlyName;
                
        [JsonProperty(Order = 4)]
        public string hn => data.Hostname;

        [JsonProperty(Order = 5)]
        public string mac => data.MACAddress;

        [JsonProperty(Order = 6)]
        public string md => data.Model;

        [JsonProperty(Order = 7)]
        public int ty => data.IsTuya;

        [JsonProperty(Order = 8)]
        public int ifan => data.IsIfan;

        [JsonProperty(Order = 9)]
        public string ofln => data.OfflinePayload;

        [JsonProperty(Order = 10)]
        public string onln => data.OnlinePayload;

        [JsonProperty(Order = 11)]
        public string[] state => data.StatePayload;

        [JsonProperty(Order = 12)]
        public string sw => data.SoftwareVersion;

        [JsonProperty(Order = 13)]
        public string t => data.Topic;

        [JsonProperty(Order = 14)]
        public string ft => data.FullTopic;

        [JsonProperty(Order = 15)]
        public string[] tp => data.TopicPrefix;

        [JsonProperty(Order = 16)]
        public int[] rl => data.Relay;

        [JsonProperty(Order = 17)]
        public int[] swc => data.SwitchConfiguration;

        [JsonProperty(Order = 18)]
        public string[] swn => data.SwitchName;

        [JsonProperty(Order = 19)]
        public int[] btn => data.Button;

        [JsonProperty(Order = 20)]
        public Dictionary<string, int> so => data.SetOption;

        [JsonProperty(Order = 21)]
        public int lk => data.LightLink;

        [JsonProperty(Order = 22)]
        public int lt_st => data.LightSubtype;

        [JsonProperty(Order = 23)]
        public int bat => data.IsOnBattery;

        [JsonProperty(Order = 24)]
        public int dslp => data.IsDeepSleepCapable;

        [JsonProperty(Order = 25)]
        public List<int> sho => data.ShutterOptions;

        [JsonProperty(Order = 26)]
        public List<int> sht => data.ShutterTilt;

        [JsonProperty(Order = 27)]
        public const int ver = 1;

        [JsonIgnore]
        public string Topic => $"tasmota/discovery/{data.MACAddress}/config";

        public TasmotaDiscoveryConfig(TasmotaData data) : base(data)
        {
        }
    }


    internal class TasmotaDiscoverySensors : TasmotaDataDerivative
    {
        [JsonProperty(Order = 1)]
        public Dictionary<string, string> sn => data.Sensors;

        [JsonProperty(Order = 2)]
        public const int ver = 1;

        [JsonIgnore]
        public string Topic => $"tasmota/discovery/{data.MACAddress}/sensors";

        public TasmotaDiscoverySensors(TasmotaData data) : base(data)
        {
        }
    }


    internal class TasmotaInfo : TasmotaDataDerivative
    {
        protected int index;
        protected string prefix;

        [JsonIgnore]
        public string Topic => $"{data.TopicPrefix[(int)Prefix.Telemetry]}/{data.Topic}/INFO{index}";

        public TasmotaInfo(int index, string prefix, TasmotaData data) : base(data)
        {
            this.index = index;
            this.prefix = prefix;
        }

        public override string ToString()
        {
            return $"{{\"{prefix}\":{JsonConvert.SerializeObject(this)}}}";
        }
    }


    internal class TasmotaInfo1: TasmotaInfo
    {
        [JsonProperty(Order = 1)]
        public string Module => data.Model;

        [JsonProperty(Order = 2)]
        public string Version => $"{data.SoftwareVersion}(cresmota)";

        [JsonProperty(Order = 3)]
        public string FallbackTopic => data.FallbackTopic;

        [JsonProperty(Order = 4)]
        public string GroupTopic => data.GroupTopic;

        public TasmotaInfo1(TasmotaData data) : base(1, "Info1", data)
        {
        }
    }


    internal class TasmotaInfo2: TasmotaInfo
    {
        [JsonProperty(Order = 1)]
        public string WebServerMode => data.WebServerMode;

        [JsonProperty(Order = 2)]
        public string Hostname => data.Hostname;

        [JsonProperty(Order = 3)]
        public string IPAddress => data.IPAddress;

        public TasmotaInfo2(TasmotaData data) : base(2, "Info2", data)
        {
        }
    }


    internal class TasmotaInfo3: TasmotaInfo
    {
        [JsonProperty(Order = 1)]
        public string RestartReason => data.RestartReason;

        [JsonProperty(Order = 2)]
        public int BootCount => data.BootCount;

        public TasmotaInfo3(TasmotaData data) : base(3, "Info3", data) 
        {
        }
    }


    internal class TasmotaStatus : TasmotaInfo
    {
        [JsonIgnore]
        public new string Topic => $"{data.TopicPrefix[(int)Prefix.Status]}/{data.Topic}/STATUS{index}";

        public TasmotaStatus(int index, string prefix, TasmotaData data) : base(index, prefix, data)
        {
        }

        public override string ToString()
        {
            return $"{{\"{prefix}\":{JsonConvert.SerializeObject(this)}}}";
        }
    }


    internal class TasmotaStatus1 : TasmotaStatus
    {
        [JsonProperty(Order = 1)]
        public int Baudrate => data.Baudrate;

        [JsonProperty(Order = 2)]
        public string SerialConfig => data.SerialConfig;

        [JsonProperty(Order = 3)]
        public string GroupTopic => data.GroupTopic.Split('/')[1];

        [JsonProperty(Order = 4)]
        public string OtaUrl => data.OtaUrl;

        [JsonProperty(Order = 5)]
        public string RestartReason => data.RestartReason;

        [JsonProperty(Order = 6)]
        public string Uptime => data.Uptime;

        [JsonProperty(Order = 7)]
        public string StartupUTC => data.StartupUTC;

        [JsonProperty(Order = 8)]
        public int Sleep => data.Sleep;

        [JsonProperty(Order = 9)]
        public int CfgHolder => data.CfgHolder;

        [JsonProperty(Order = 10)]
        public int BootCount => data.BootCount;

        [JsonProperty(Order = 11)]
        public string BCResetTime => data.BCResetTime;

        [JsonProperty(Order = 12)]
        public int SaveCount => data.SaveCount;

        [JsonProperty(Order = 13)]
        public string SaveAddress => data.SaveAddress;

        public TasmotaStatus1(TasmotaData data) : base(1, "StatusPRM", data)
        {
        }
    }

    internal class TasmotaStatus11 : TasmotaStatus
    {
        public TasmotaStatus11(TasmotaData data) : base(11, "StatusSTS", data)
        {
        }

        public override string ToString()
        {
            return $"{{\"{prefix}\":{data.State}}}";
        }
    }


    internal class TasmotaState : TasmotaDataDerivative
    {
        [JsonProperty(Order = 1)]
        public string Time => data.Time;

        [JsonProperty(Order = 2)]
        public string Uptime => data.Uptime;

        [JsonProperty(Order = 3)]
        public int UptimeSec => data.UptimeSec;

        [JsonProperty(Order = 4)]
        public int Heap => data.Heap;

        [JsonProperty(Order = 5)]
        public string SleepMode => data.SleepMode;

        [JsonProperty(Order = 6)]
        public int Sleep => data.Sleep;

        [JsonProperty(Order = 7)]
        public int LoadAvg => data.LoadAvg;

        [JsonProperty(Order = 8)]
        public int MqttCount => data.MqttCount;

        public TasmotaState(TasmotaData data) : base(data)
        {
        }
        
        public override string ToString()
        {
            string state = JsonConvert.SerializeObject(this);

            if (data.Channels.Length == 0)
                return state;

            state = state.Substring(0, state.Length - 1);

            for (int i = 0; i < data.Channels.Length; i++)
            {
                if (data.Channels[i].Mode == RelayMode.None)
                    break;
                state += $",\"POWER{i + 1}\":";
                state += (data.Channels[i].Power == 0) ? "\"OFF\"" : "\"ON\"";
                if (data.Channels[i].Mode == RelayMode.Light)
                {
                    state += $",\"Channel{i + 1}\":";
                    state += $"{data.Channels[i].Level}";
                }
            }
            state += "}";

            return state;
        }
    }
}
