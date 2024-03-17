using MQTTnet;
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
using System.Collections.Concurrent;

namespace Cresmota
{
    public partial class CresmotaDevice : IDisposable
    {
        public const string Version = "0.1.0";
        
        public const ushort MaxProgramSlots = 10;
        public const ushort MaxChannels = 128;
        public const ushort RELAYS = 0;
        public const ushort LIGHTS = 1;

        public delegate void PowerRequestDelegateHandler(ushort channel, ushort state);
        public PowerRequestDelegateHandler PowerRequestDelegate { get; set; }

        public delegate void LevelRequestDelegateHandler(ushort channel, ushort level);
        public LevelRequestDelegateHandler LevelRequestDelegate { get; set; }

        public TasmotaData Data = new TasmotaData();
        
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
            get { return new SimplSharpString(Data.Topic); }
            set { Data.Topic = value.ToString(); }
        }
        public string DeviceName
        {
            get
            {
                return Data.DeviceName;
            }
            set
            {
                Data.DeviceName = value;
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
                    Data.SetOption["30"] = 1;
                    DebugPrint("@ Reporting as LIGHTS");
                }
                else
                {
                    _reportAsLights = false;
                    Data.SetOption["30"] = 0;
                    DebugPrint("@ Reporting as RELAYS");
                }
            }
        }

        private Task ClientTask { get; set; }
        private bool stopRequested = false;

        private BlockingCollection<Message> OutgoingMessages = new BlockingCollection<Message>();


        public CresmotaDevice()
        {
            DebugPrint("+ CONSTRUCTOR started");
            DebugPrint("- CONSTRUCTOR complete");
        }
        
        private bool _checkConfigValue<T>(string name, T value)
        {
            // Check if the value is null, the default for its type or an empty string
            if (EqualityComparer<T>.Default.Equals(value, default) || (value is string str && str == ""))
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
            validConfig &= _checkConfigValue(nameof(Data.DeviceName), Data.DeviceName);
            validConfig &= _checkConfigValue(nameof(BrokerAddress), BrokerAddress.ToString());

            if (!validConfig)
            {
                DebugPrint("X Cannot START with invalid configuration");
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
            Data.MACAddress = macAddress;
            Data.IPAddress = ipAddress;
            DebugPrint($"@ MAC = {Data.MACAddress}");

            ClientID = new SimplSharpString($"Cresmota-{ProgramSlot:D2}-{ID:D2}");
            DebugPrint($"@ MQTT Client ID = {ClientID}");

            if (string.IsNullOrEmpty(Data.Topic))
            {
                Data.Topic = ClientID.ToString();
            }
            DebugPrint($"@ MQTT Topic = {Data.Topic}");
            DebugPrint($"@ MQTT Broker = {BrokerAddress}:{BrokerPort}");

            if (string.IsNullOrEmpty(Data.FriendlyName[0]))
            {
                Data.FriendlyName[0] = Data.DeviceName;
            }

            if (string.IsNullOrEmpty(Data.Hostname))
            {
                Data.Hostname = ClientID.ToString();
            }

            Data.StartTime = DateTime.Now;

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

            // Dispose the outgoing message queue
            OutgoingMessages?.Dispose();
            OutgoingMessages = null;

            // Dispose the state update timer
            stateUpdateTimer?.Dispose();
            stateUpdateTimer = null;

            DebugStatusDelegate = null;
            PowerRequestDelegate = null;
            LevelRequestDelegate = null;
             
            DebugPrint("- DISPOSE complete");
        }

        public void Add(SimplSharpString name, RelayMode mode=RelayMode.Basic)
        {
            if (ChannelCount >= MaxChannels)
            {
                DebugPrint($"! Cannot add {name} - ChannelCount is at maximum ({MaxChannels})");
                return;
            }
            Data.FriendlyName[ChannelCount] = name.ToString();
            Data.Relay[ChannelCount] = (int)mode;
            Data.Channels[ChannelCount].Mode = mode;
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
        
        public void SetPower(ushort channel, ushort state)
        {
            if (channel < 1 || channel > ChannelCount)
            {
                DebugPrint($"! Invalid channel [{channel}] specified in SetPower request");
                return;
            }
            Data.Channels[channel - 1].Power = state;
            _publishPower(channel, state);
        }

        public void SetLevel(ushort channel, ushort level)
        {
            if (channel < 1 || channel > ChannelCount)
            {
                DebugPrint($"! Invalid channel [{channel}] specified in SetLevel request");
                return;
            }
            if (Data.Channels[channel - 1].Mode != RelayMode.Light)
            {
                DebugPrint($"! Channel {channel} is not a light, cannot set level");
                return;
            }
            Data.Channels[channel - 1].Power = SPlusBool.TRUE;
            Data.Channels[channel - 1].Level = level;
            _publishLevel(channel, level);
        }

        private void _publishPower(ushort channel, ushort state)
        {
            string endpoint = $"POWER{channel}";
            string payload = (Data.Channels[channel - 1].Power == 0) ? "OFF" : "ON";

            DebugPrint($"< [TX] Publish Power state for channel {channel} = {payload}");

            OutgoingMessages.Add(new Message { Topic = $"{Data.TopicPrefix[(int)Prefix.Status]}/{Data.Topic}/RESULT", Payload = $"{{\"{endpoint}\":\"{payload}\"}}", Retained = false });
            OutgoingMessages.Add(new Message { Topic = $"{Data.TopicPrefix[(int)Prefix.Status]}/{Data.Topic}/{endpoint}", Payload = payload, Retained = false });
        }
        
        private void _publishLevel(ushort channel, ushort level, bool levelOnly=false)
        {
            string powerPayload = (levelOnly) ? "" : $"\"POWER{channel}\":\"ON\",";
            string payload = $"{{{powerPayload}\"Channel{channel}\":{level}}}";

            DebugPrint($"< [TX] Publish Level for channel {channel} = {level}");

            OutgoingMessages.Add(new Message { Topic = $"{Data.TopicPrefix[(int)Prefix.Status]}/{Data.Topic}/RESULT", Payload = payload, Retained = false });
        }

        private void _publishState(bool telemetry=false, bool result=true)
        {
            string state = Data.State;

            if (result)
            {
                DebugPrint("< [TX] Publish STATE to RESULT");
                OutgoingMessages.Add(new Message { Topic = $"{Data.TopicPrefix[(int)Prefix.Status]}/{Data.Topic}/RESULT", Payload = state, Retained = false });
            }
            if (telemetry)
            {
                DebugPrint("< [TX] Publish STATE to TELEMETRY");
                OutgoingMessages.Add(new Message { Topic = $"{Data.TopicPrefix[(int)Prefix.Telemetry]}/{Data.Topic}/STATE", Payload = state, Retained = false });
            }
        }
        
        private void _processCommand(string endpoint, string payload="")
        {
            string directive = endpoint;
            ushort channel = 1;

            Match endpointData = Regex.Match(endpoint, @"^([a-zA-Z]+).*?(\d+)$");
            if (endpointData.Success)
            {
                directive = endpointData.Groups[1].Value;
                ushort.TryParse(endpointData.Groups[2].Value, out channel);
            }

            switch (directive)
            {
                case "Power":
                    if (string.IsNullOrEmpty(payload))
                    {
                        DebugPrint($"> [RX] Power state request for channel {channel}");
                        _publishPower(channel, Data.Channels[channel - 1].Power);
                    }
                    else if (payload == "ON")
                    {
                        DebugPrint($"> [RX] Power ON channel {channel}");
                        PowerRequestDelegate?.Invoke(channel, SPlusBool.TRUE);
                    }
                    else if (payload == "OFF")
                    {
                        DebugPrint($"> [RX] Power OFF channel {channel}");
                        PowerRequestDelegate?.Invoke(channel, SPlusBool.FALSE);
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
                        _publishLevel(channel, Data.Channels[channel - 1].Level, levelOnly: true);
                    }
                    else
                    {
                        ushort level = 0;
                        if (ushort.TryParse(payload, out level))
                        {
                            DebugPrint($"> [RX] Set Channel {channel} to {level}");
                            LevelRequestDelegate?.Invoke(channel, level);
                        }
                        else
                        {
                            DebugPrint($"! [RX] Invalid payload [{payload}] for Channel command");
                        }
                    }
                    break;
                
                case "NoDelay":
                    break;

                case "STATUS":
                    DebugPrint($"> [RX] STATUS [{payload}] request");
                    switch (payload)
                    {
                        case "1":
                            DebugPrint("< [TX] Publish STATUS1");
                            OutgoingMessages.Add(Data.Status1Message);
                            break;
                        
                        case "11":
                            DebugPrint("< [TX] Publish STATUS11");
                            OutgoingMessages.Add(Data.Status11Message);
                            break;
                    }
                    break;

                case "STATE":
                    DebugPrint($"> [RX] STATE request");
                    _publishState();
                    break;

                default:
                    DebugPrint($"> [RX] ? Unknown Directive [{directive}]");
                    break;
            }
        }

        private Timer stateUpdateTimer;
        
        private async Task Client()
        {
            DebugPrint("+ CLIENT task started");

            try
            {
                var mqttFactory = new MqttFactory();

                using (var managedMqttClient = mqttFactory.CreateManagedMqttClient())
                {
                    async Task publishMessage(Message msg) => await managedMqttClient.EnqueueAsync(msg.Topic, msg.Payload, retain: msg.Retained);

                    managedMqttClient.ApplicationMessageReceivedAsync += e =>
                    {
                        string payload = e.ApplicationMessage.ConvertPayloadToString();
                        string[] topic = e.ApplicationMessage.Topic.Split(new char[] { '/' });
                        string endpoint = topic[topic.Length - 1];

                        if (topic[0] == Data.TopicPrefix[(int)Prefix.Command])
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
                        Data.MqttCount++;
                        DebugPrint("< [TX] Publish ONLINE state");
                        await publishMessage(Data.OnlineMessage);

                        Message configMessage = Data.DiscoveryConfigMessage;
                        Message sensorsMessage = Data.DiscoverySensorsMessage;
                        
                        if (_autoDiscovery)
                        {

                            DebugPrint("+ Autodiscovery ENABLED");
                            DebugPrint($"< [TX] Publish to tasmota/discovery/{Data.MACAddress}");
                            await publishMessage(configMessage);
                            await publishMessage(sensorsMessage);
                        }
                        else
                        {
                            DebugPrint("- Autodiscovery DISABLED");
                            DebugPrint($"* Clearing any retained topics at tasmota/discovery/{Data.MACAddress}");
                            await publishMessage(new Message { Topic = configMessage.Topic, Payload = "", Retained = true });
                            await publishMessage(new Message { Topic = configMessage.Topic, Payload = "", Retained = false });
                            await publishMessage(new Message { Topic = sensorsMessage.Topic, Payload = "", Retained = true });
                            await publishMessage(new Message { Topic = sensorsMessage.Topic, Payload = "", Retained = false });
                        }
                        DebugPrint("< [TX] Publish STATE to TELEMETRY");
                        await publishMessage(Data.StateMessage);

                        //start timer to publish 'state' unsolicited to telemetry every 300 seconds                    
                        if (stateUpdateTimer != null)
                        {
                            stateUpdateTimer.Dispose();
                            stateUpdateTimer = null;
                        }
                        stateUpdateTimer = new Timer((state) => _publishState(result: false, telemetry: true), null, 300000, 300000);

                        DebugPrint("< [TX] Publish INFO1, INFO2, INFO3");
                        await publishMessage(Data.Info1Message);
                        await publishMessage(Data.Info2Message);
                        await publishMessage(Data.Info3Message);
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
                            .WithWillTopic($"tele/{Data.Topic}/LWT")
                            .WithWillPayload(Data.OfflinePayload)
                            .WithWillRetain(true)
                            .Build())
                        .Build();

                    DebugPrint($"* Subscribing to default topic [{Data.TopicPrefix[(int)Prefix.Command]}/{Data.Topic}/#]");
                    await managedMqttClient.SubscribeAsync($"{Data.TopicPrefix[(int)Prefix.Command]}/{Data.Topic}/#");

                    DebugPrint($"* Subscribing to fallback topic [{Data.FallbackTopic}#]");
                    await managedMqttClient.SubscribeAsync($"{Data.FallbackTopic}#");

                    DebugPrint($"* Subscribing to group topic [{Data.GroupTopic}#]");
                    await managedMqttClient.SubscribeAsync($"{Data.GroupTopic}#");

                    await managedMqttClient.StartAsync(options);

                    while (!stopRequested)
                    {
                        if (managedMqttClient.IsConnected)
                        {
                            if (OutgoingMessages.TryTake(out Message msg, 250))
                            {
                                await publishMessage(msg);
                            }
                        }
                        else
                        {
                            DebugPrint("~ Waiting for connection...");
                            Thread.Sleep(5000);
                        }
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
