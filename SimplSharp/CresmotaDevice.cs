﻿using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System;
using Crestron.SimplSharp;
using System.Diagnostics.Eventing.Reader;
using System.Collections.Generic;
using MQTTnet.Diagnostics;

namespace Cresmota
{
    public partial class CresmotaDevice : IDisposable
    {
        public const string Version = "0.1.0";
        
        public const ushort MaxProgramSlots = 10;
        public const ushort MaxChannels = 128;
        public const ushort RELAYS = 0;
        public const ushort LIGHTS = 1;

        public TasmotaConfig Config = new TasmotaConfig();
        public TasmotaSensors Sensors { get; private set; } = new TasmotaSensors();

        public TasmotaInfo1 Info1 = new TasmotaInfo1();
        public TasmotaInfo2 Info2 = new TasmotaInfo2();
        public TasmotaInfo3 Info3 = new TasmotaInfo3();

        private ushort _programSlot = 0;
        public ushort ProgramSlot
        {
            get
            {
                return _programSlot;
            }
            set
            {
                if (_programSlot > 0)
                {
                    DebugPrint($"! ProgramSlot already set to {_programSlot}, cannot change to {value}");
                    return;
                }
                else if (value == 0 || value > MaxProgramSlots)
                {
                    DebugPrint($"! Requested ProgramSlot ({value}) is out of range (1-{MaxProgramSlots})");
                    return;
                }
                _programSlot = value;
                DebugPrint($"@ ProgramSlot set to {_programSlot}");
            }
        }

        public const ushort MaxDevices = 100;
        private static List<ushort> _devices = new List<ushort>();

        private ushort _id = 0;
        public ushort ID
        {
            get
            {
                return _id;
            }
            set
            {
                if (_id > 0)
                {
                    DebugPrint($"! ID already set to {_id}, cannot change to {value}");
                    return;
                }
                else if (value == 0 || value > MaxDevices)
                {
                    DebugPrint($"! Requested ID ({value}) is out of range (1-{MaxDevices})");
                    return;
                }
                else if (_devices.Contains(value))
                {
                    DebugPrint($"! Requested ID ({value}) is already is use.");
                    return;
                }
                _id = value;
                _devices.Add(_id);
                DebugPrint($"@ ID set to {_id}");
            }
        }

        public SimplSharpString ClientID { get; private set; } = "";
        public SimplSharpString BrokerAddress = "";
        public ushort BrokerPort = 1883;
        public SimplSharpString Username = "";
        public SimplSharpString Password = "";
        public SimplSharpString GroupTopic = "";
        public SimplSharpString Topic
        {
            get { return new SimplSharpString(Config.Topic); }
            set { Config.Topic = value.ToString(); }
        }
        public string DeviceName
        {
            get
            {
                return Config.DeviceName;
            }
            set
            {
                Config.DeviceName = value;
                DebugPrint($"@ DeviceName set to {value}");
            }
        }

        public ushort ChannelCount { get; private set; } = 0;

        private bool _autoDiscovery = true;
        public ushort AutoDiscovery
        {
            get
            {
                return SPlusBool.FromBool(_autoDiscovery);
            }
            set
            {
                if (SPlusBool.IsTrue(value))
                {
                    _autoDiscovery = true;
                    DebugPrint("@ AutoDiscovery is ENABLED");
                }
                else
                {
                    _autoDiscovery = false;
                    DebugPrint("@ AutoDiscovery is DISABLED");
                }
            }
        }

        private bool _reportAsLights = false;
        public ushort ReportAs
        {
            get
            {
                return SPlusBool.FromBool(_reportAsLights);
            }
            set
            {
                if (SPlusBool.IsTrue(value))
                {
                    _reportAsLights = true;
                    Config.SetOption["30"] = 1;
                    DebugPrint("@ Reporting as LIGHTS");
                }
                else
                {
                    _reportAsLights = false;
                    Config.SetOption["30"] = 0;
                    DebugPrint("@ Reporting as RELAYS");
                }
            }
        }

        private Task ClientTask { get; set; }
        private bool stopRequested = false;

        public Channel[] Channels = new Channel[MaxChannels];


        public CresmotaDevice()
        {
            DebugPrint("+ CONSTRUCTOR started");
            
            for (int i = 0; i < Channels.Length; i++)
            {
                Channels[i] = new Channel();
            }
            
            DebugPrint("- CONSTRUCTOR complete");
        }
        
        private bool _checkConfigValue(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                DebugPrint($"! {name} is not set");
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool _checkConfigValue(string name, ushort value)
        {
            if (value == 0)
            {
                DebugPrint($"! {name} is not set");
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Start()
        {
            DebugPrint("+ START requested");

            if (ClientTask != null)
            {
                DebugPrint("! START operation already completed");
                return;
            }
            else if (stopRequested == true)
            {
                DebugPrint("! Cannot START until STOP operation is complete");
                return;
            }

            bool validConfig = true;
            validConfig &= _checkConfigValue(nameof(ProgramSlot), ProgramSlot);
            validConfig &= _checkConfigValue(nameof(ID), ID);
            validConfig &= _checkConfigValue(nameof(Config.DeviceName), Config.DeviceName);
            validConfig &= _checkConfigValue(nameof(BrokerAddress), BrokerAddress.ToString());

            if (!validConfig)
            {
                DebugPrint("! Cannot START with invalid configuration");
                return;
            }

            string macAddress = "02XXXXFA57ED";
            string ipAddress = "x.x.x.x";
            
            if (RunningOnCrestron)
            {
                short lanAdapter = CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType.EthernetLANAdapter);
                macAddress = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_MAC_ADDRESS, lanAdapter);
                ipAddress = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, lanAdapter);
                DebugPrint($"@ LAN = {ipAddress} ({macAddress})");
            }
            macAddress = $"02{ProgramSlot:X2}{ID:X2}" + macAddress.Replace(":", "").ToUpper().Substring(6);
            Config.MACAddress = macAddress;
            Config.IPAddress = ipAddress;
            DebugPrint($"@ MAC = {Config.MACAddress}");

            ClientID = new SimplSharpString($"Cresmota-{ProgramSlot:D2}-{ID:D2}");
            DebugPrint($"@ MQTT Client ID = {ClientID}");

            if (string.IsNullOrEmpty(Config.Topic))
            {
                Config.Topic = ClientID.ToString();
            }
            DebugPrint($"@ MQTT Topic = {Config.Topic}");
            DebugPrint($"@ MQTT Broker = {BrokerAddress}:{BrokerPort}");

            if (string.IsNullOrEmpty(Config.FriendlyName[0]))
            {
                Config.FriendlyName[0] = Config.DeviceName;
            }
            
            if (string.IsNullOrEmpty(Config.HostName))
            {
                Config.HostName = ClientID.ToString();
            }
            
            Info1.Module = Config.Model;
            Info1.Version = $"{Version}(cresmota)";
            Info1.FallbackTopic = $"{Config.TopicPrefix[(int)Prefix.Command]}/DVES_{Config.MACAddress.Substring(6)}_fb/";
            Info1.GroupTopic = $"{Config.TopicPrefix[(int)Prefix.Command]}/tasmotas/";
            Info2.IPAddress = Config.IPAddress;
            Info2.Hostname = Config.HostName;

            ClientTask = Task.Run(Client);

            DebugPrint("- START complete");
        }

        public void Stop()
        {
            DebugPrint("+ STOP requested");

            stopRequested = true;

            if (ClientTask != null)
            {
                ClientTask.Wait();
                ClientTask.Dispose();
                ClientTask = null;
            }

            stopRequested = false;

            DebugPrint("- STOP complete");
        }

        public void Dispose()
        {
            DebugPrint("+ DISPOSE started");

            Stop();
            DebugStatusDelegate = null;
             
            DebugPrint("- DISPOSE complete");
        }

        public void Add(SimplSharpString name, RelayMode mode=RelayMode.Basic)
        {
            if (ChannelCount >= MaxChannels)
            {
                DebugPrint($"! Cannot add {name} - ChannelCount is at maximum ({MaxChannels})");
                return;
            }
            Config.FriendlyName[ChannelCount] = name.ToString();
            Config.Relay[ChannelCount] = (int)mode;
            Channels[ChannelCount].Mode = mode;
            ChannelCount++;
            DebugPrint($"~ Channel [{ChannelCount:D3}]:[{mode}] = {name} ");
        }

        public void AddBasic(SimplSharpString name)
        {
            Add(name, RelayMode.Basic);
        }

        public void AddLight(SimplSharpString name)
        {
            Add(name, RelayMode.Light);
        }

        public void AddShutter(SimplSharpString name)
        {
            throw new NotImplementedException("Shutters are not supported in Cresmota at this time.");
        }

        public void AddChannel(SimplSharpString channel)
        {
            string[] channelData = channel.ToString().Split(new char[] { ';' }, 2);
            string name;
            string mode;

            if (channelData.Length == 2)
            {
                mode = channelData[0].ToLower();
                name = channelData[1];
            }
            else
            {
                mode = "b";
                name = channelData[0];
            }

            switch (mode)
            {
                case "b":
                    AddBasic(name);
                    break;
                case "l":
                    AddLight(name);
                    break;
                case "s":
                    AddShutter(name);
                    break;
                default:
                    throw new ArgumentException($"Invalid mode [{mode}] specified in channel name string [{channelData}]");
            }   
        }
        
        private void _processCommand(string endpoint, string payload="")
        {
            string directive = endpoint;
            int channel = 1;

            Match endpointData = Regex.Match(endpoint, @"^([a-zA-Z]+).*?(\d+)$");
            if (endpointData.Success)
            {
                directive = endpointData.Groups[1].Value;
                int.TryParse(endpointData.Groups[2].Value, out channel);
            }

            switch (directive)
            {
                case "Power":
                    if (string.IsNullOrEmpty(payload))
                    {
                        DebugPrint($"> [RX] Power state request for channel {channel}");
                    }
                    else if (payload == "ON")
                    {
                        DebugPrint($"> [RX] Power ON channel {channel}");
                    }
                    else if (payload == "OFF")
                    {
                        DebugPrint($"> [RX] Power OFF channel {channel}");
                    }
                    else
                    {
                        DebugPrint($"! [RX] Invalid payload [{payload}] for Power command");
                    }
                    break;

                case "Channel":
                    if (string.IsNullOrEmpty(payload))
                    {
                        DebugPrint($"> [RX] Channel state request for channel {channel}");
                    }
                    else
                    {
                        DebugPrint($"> [RX] Set Channel {channel} to {payload}");
                    }
                    break;
                
                case "NoDelay":
                    break;

                case "STATUS":
                    DebugPrint($"> [RX] STATUS [{endpoint}][{payload}] request");
                    break;

                case "STATE":
                    DebugPrint($"> [RX] STATE [{endpoint}][{payload}] request");
                    break;

                default:
                    DebugPrint($"> [RX] ? Unknown Directive [{directive}]");
                    break;
            }
        }

        private async Task Client()
        {
            DebugPrint("+ CLIENT task started");

            try
            {
                var mqttFactory = new MqttFactory();

                using (var managedMqttClient = mqttFactory.CreateManagedMqttClient())
                {

                    managedMqttClient.ApplicationMessageReceivedAsync += e =>
                    {
                        string payload = e.ApplicationMessage.ConvertPayloadToString();
                        string[] topic = e.ApplicationMessage.Topic.Split(new char[] { '/' });
                        string endpoint = topic[topic.Length - 1];

                        if (topic[0] == Config.TopicPrefix[(int)Prefix.Command])
                        {
                            if (endpoint == "Backlog")
                            {
                                string[] commands = payload.Split(new char[] { ';' });
                                foreach (string command in commands)
                                {
                                    string[] commandData = command.Split(new char[] { ' ' }, 2);
                                    if (commandData.Length == 2)
                                    {
                                        _processCommand(commandData[0], commandData[1]);
                                    }
                                    else
                                    {
                                        _processCommand(command);
                                    }
                                }
                            }
                            else
                            {
                                _processCommand(endpoint, payload);
                            }
                        }

                        else
                        { 
                            DebugPrint($"RX: ! Unhandled message [topic: {e.ApplicationMessage.Topic}] [payload: {payload}]");
                        }

                        return Task.CompletedTask;
                    };

                    managedMqttClient.ConnectedAsync += async e =>
                    {
                        DebugPrint($"+ Connected to broker @ {BrokerAddress}:{BrokerPort}");
                        DebugPrint("< [TX] Publish ONLINE state");
                        await managedMqttClient.EnqueueAsync($"{Config.TopicPrefix[(int)Prefix.Telemetry]}/{Config.Topic}/LWT", Config.OnlinePayload, retain: true);
                        if (_autoDiscovery)
                        {

                            DebugPrint("+ Autodiscovery ENABLED");
                            DebugPrint($"< [TX] Publish to tasmota/discovery/{Config.MACAddress}");
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.MACAddress}/config", Config.ToString(), retain: true);
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.MACAddress}/sensors", Sensors.ToString(), retain: true);
                        }
                        else
                        {
                            DebugPrint("- Autodiscovery DISABLED");
                            DebugPrint($"* Clearing any retained topics at tasmota/discovery/{Config.MACAddress}");
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.Topic}/config", "", retain: true);
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.Topic}/config", "", retain: false);
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.Topic}/sensors", "", retain: true);
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.Topic}/sensors", "", retain: false);
                        }
                        DebugPrint("< [TX] Publish INFO1, INFO2, INFO3");
                        await managedMqttClient.EnqueueAsync($"{Config.TopicPrefix[(int)Prefix.Telemetry]}/{Config.Topic}/INFO1", Info1.ToString(), retain: false);
                        await managedMqttClient.EnqueueAsync($"{Config.TopicPrefix[(int)Prefix.Telemetry]}/{Config.Topic}/INFO2", Info2.ToString(), retain: false);
                        await managedMqttClient.EnqueueAsync($"{Config.TopicPrefix[(int)Prefix.Telemetry]}/{Config.Topic}/INFO3", Info3.ToString(), retain: false);

                        //publish power/state for all channels
                        
                        //publish 'state' thingy to telemetry topic only

                        //start timer to publish 'state' unsolicited to telemetry every 300 seconds                    
                    };

                    managedMqttClient.DisconnectedAsync += e =>
                    {
                        DebugPrint("The managed MQTT client is DISCONNECTED.");
                        return Task.CompletedTask;
                    };

                    var options = new ManagedMqttClientOptionsBuilder()
                        .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                        .WithClientOptions(new MqttClientOptionsBuilder()
                            .WithClientId(ClientID.ToString())
                            .WithTcpServer(BrokerAddress.ToString(), BrokerPort)
                            .WithCredentials(Username.ToString(), Password.ToString())
                            .WithCleanSession()
                            .WithWillTopic($"tele/{Config.Topic}/LWT")
                            .WithWillPayload(Config.OfflinePayload)
                            .WithWillRetain(true)
                            .Build())
                        .Build();

                    DebugPrint($"* Subscribing to default topic [{Config.TopicPrefix[(int)Prefix.Command]}/{Config.Topic}/#]");
                    await managedMqttClient.SubscribeAsync($"{Config.TopicPrefix[(int)Prefix.Command]}/{Config.Topic}/#");

                    DebugPrint($"* Subscribing to fallback topic [{Info1.FallbackTopic}#]");
                    await managedMqttClient.SubscribeAsync($"{Info1.FallbackTopic}#");

                    DebugPrint($"* Subscribing to group topic [{Info1.GroupTopic}#]");
                    await managedMqttClient.SubscribeAsync($"{Info1.GroupTopic}#");

                    await managedMqttClient.StartAsync(options);

                    int testValue = 0;

                    while (!stopRequested)
                    {
                        if (managedMqttClient.IsConnected)
                        {
                            //DebugPrint($"Published testValue={testValue}");
                            testValue++;
                            await managedMqttClient.EnqueueAsync($"[{ProgramSlot}][{ID}] testValue", testValue.ToString());
                        }
                        else
                        {
                            DebugPrint("> Waiting for connection...");
                        }
                        Thread.Sleep(2500);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                DebugPrint("! CLIENT task aborted");
            }
            catch (TaskCanceledException)
            {
                DebugPrint("! CLIENT task cancelled");
            }
            DebugPrint("- CLIENT task exited");
        }
    }
}
