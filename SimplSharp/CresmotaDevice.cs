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

        public TasmotaData Data { get; set; } = new TasmotaData();
        
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
                    DebugPrint($"! ProgramSlot already set to {_programSlot}, cannot change to {value}", DebugColor.Error);
                    return;
                }
                else if (value == 0 || value > MaxProgramSlots)
                {
                    DebugPrint($"! Requested ProgramSlot ({value}) is out of range (1-{MaxProgramSlots})", DebugColor.Error);
                    return;
                }
                _programSlot = value;
                DebugPrint($"@ ProgramSlot set to {_programSlot}", DebugColor.Info);
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
                    DebugPrint($"! ID already set to {_id}, cannot change to {value}", DebugColor.Error);
                    return;
                }
                else if (value == 0 || value > MaxDevices)
                {
                    DebugPrint($"! Requested ID ({value}) is out of range (1-{MaxDevices})", DebugColor.Error);
                    return;
                }
                else if (_devices.Contains(value))
                {
                    DebugPrint($"! Requested ID ({value}) is already is use.", DebugColor.Error);
                    return;
                }
                _id = value;
                _devices.Add(_id);
                DebugPrint($"@ ID set to {_id}", DebugColor.Info);
            }
        }

        public SimplSharpString ClientID { get; private set; } = "";
        public SimplSharpString BrokerAddress { get; set; } = "";
        public ushort BrokerPort { get; set; } = 1883;
        public SimplSharpString Username { get; set; } = "";
        public SimplSharpString Password { get; set; } = "";
        public SimplSharpString GroupTopic { get; set; } = "";
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
                DebugPrint($"@ DeviceName set to {value}", DebugColor.Info);
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
                    DebugPrint("@ AutoDiscovery is ENABLED", DebugColor.Start);
                }
                else
                {
                    _autoDiscovery = false;
                    DebugPrint("@ AutoDiscovery is DISABLED", DebugColor.Stop);
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
                    DebugPrint("@ Reporting as LIGHTS", DebugColor.Info);
                }
                else
                {
                    _reportAsLights = false;
                    Data.SetOption["30"] = 0;
                    DebugPrint("@ Reporting as RELAYS", DebugColor.Info);
                }
            }
        }

        private Task _clientTask { get; set; }
        private bool _stopRequested = false;

        private BlockingCollection<Message> _outgoingMessages = new BlockingCollection<Message>();


        public CresmotaDevice()
        {
            DebugPrint("+ CONSTRUCTOR started", DebugColor.Start);
            DebugPrint("- CONSTRUCTOR complete", DebugColor.Stop);
        }
        
        private bool _CheckConfigValue<T>(string name, T value)
        {
            // Check if the value is null, the default for its type or an empty string
            if (EqualityComparer<T>.Default.Equals(value, default) || (value is string str && str == ""))
            {
                DebugPrint($"! {name} is not set", DebugColor.Error);
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Start()
        {
            DebugPrint("+ START requested", DebugColor.Start);

            if (_clientTask != null)
            {
                DebugPrint("! START operation already completed", DebugColor.Error);
                return;
            }
            else if (_stopRequested == true)
            {
                DebugPrint("! Cannot START until STOP operation is complete", DebugColor.Error);
                return;
            }

            bool validConfig = true;
            validConfig &= _CheckConfigValue(nameof(ProgramSlot), ProgramSlot);
            validConfig &= _CheckConfigValue(nameof(ID), ID);
            validConfig &= _CheckConfigValue(nameof(Data.DeviceName), Data.DeviceName);
            validConfig &= _CheckConfigValue(nameof(BrokerAddress), BrokerAddress.ToString());

            if (!validConfig)
            {
                DebugPrint("X Cannot START with invalid configuration", DebugColor.Error);
                return;
            }

            string macAddress = "02XXXXFA57ED";
            string ipAddress = "x.x.x.x";
            
            if (RunningOnCrestron)
            {
                short lanAdapter = CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType.EthernetLANAdapter);
                macAddress = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_MAC_ADDRESS, lanAdapter);
                ipAddress = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, lanAdapter);
                DebugPrint($"@ LAN = {ipAddress} ({macAddress})", DebugColor.Info);
            }
            macAddress = $"02{ProgramSlot:X2}{ID:X2}" + macAddress.Replace(":", "").ToUpper().Substring(6);
            Data.MACAddress = macAddress;
            Data.IPAddress = ipAddress;
            DebugPrint($"@ MAC = {Data.MACAddress}", DebugColor.Info);

            ClientID = new SimplSharpString($"Cresmota-{ProgramSlot:D2}-{ID:D2}");
            DebugPrint($"@ MQTT Client ID = {ClientID}", DebugColor.Info);

            if (string.IsNullOrEmpty(Data.Topic))
            {
                Data.Topic = ClientID.ToString();
            }
            DebugPrint($"@ MQTT Topic = {Data.Topic}", DebugColor.Info);
            DebugPrint($"@ MQTT Broker = {BrokerAddress}:{BrokerPort}", DebugColor.Info);

            if (string.IsNullOrEmpty(Data.FriendlyName[0]))
            {
                Data.FriendlyName[0] = Data.DeviceName;
            }

            if (string.IsNullOrEmpty(Data.Hostname))
            {
                Data.Hostname = ClientID.ToString();
            }

            Data.StartTime = DateTime.Now;

            _clientTask = Task.Run(_Client);

            DebugPrint("- START complete", DebugColor.Stop);
        }

        public void Stop()
        {
            DebugPrint("+ STOP requested", DebugColor.Start);

            _stopRequested = true;

            if (_clientTask != null)
            {
                _clientTask.Wait();
                _clientTask.Dispose();
                _clientTask = null;
            }

            _stopRequested = false;

            DebugPrint("- STOP complete", DebugColor.Stop);
        }

        public void Dispose()
        {
            DebugPrint("+ DISPOSE started", DebugColor.Start);

            Stop();

            // Dispose the outgoing message queue
            _outgoingMessages?.Dispose();
            _outgoingMessages = null;

            // Dispose the state update timer
            _stateUpdateTimer?.Dispose();
            _stateUpdateTimer = null;

            DebugStatusDelegate = null;
            PowerRequestDelegate = null;
            LevelRequestDelegate = null;
             
            DebugPrint("- DISPOSE complete", DebugColor.Stop);
        }

        public void Add(SimplSharpString name, RelayMode mode=RelayMode.Basic)
        {
            if (ChannelCount >= MaxChannels)
            {
                DebugPrint($"! Cannot add {name} - ChannelCount is at maximum ({MaxChannels})", DebugColor.Error);
                return;
            }
            Data.FriendlyName[ChannelCount] = name.ToString();
            Data.Relay[ChannelCount] = (int)mode;
            Data.Channels[ChannelCount].Mode = mode;
            ChannelCount++;
            DebugPrint($"~ Channel [{ChannelCount:D3}]:[{mode}] = {name}", DebugColor.Info);
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
                DebugPrint($"! Invalid channel [{channel}] specified in SetPower request", DebugColor.Error);
                return;
            }
            Data.Channels[channel - 1].Power = state;
            _PublishPower(channel, state);
        }

        public void SetLevel(ushort channel, ushort level)
        {
            if (channel < 1 || channel > ChannelCount)
            {
                DebugPrint($"! Invalid channel [{channel}] specified in SetLevel request", DebugColor.Error);
                return;
            }
            if (Data.Channels[channel - 1].Mode != RelayMode.Light)
            {
                DebugPrint($"! Channel {channel} is not a light, cannot set level", DebugColor.Error);
                return;
            }
            Data.Channels[channel - 1].Power = SPlusBool.TRUE;
            Data.Channels[channel - 1].Level = level;
            _PublishLevel(channel, level);
        }

        private void _PublishPower(ushort channel, ushort state)
        {
            string endpoint = $"POWER{channel}";
            string payload = (Data.Channels[channel - 1].Power == 0) ? "OFF" : "ON";

            DebugPrint($"< [TX] Publish Power state for channel {channel} = {payload}", DebugColor.TX);

            _outgoingMessages.Add(new Message { Topic = $"{Data.TopicPrefix[(int)Prefix.Status]}/{Data.Topic}/RESULT", Payload = $"{{\"{endpoint}\":\"{payload}\"}}", Retained = false });
            _outgoingMessages.Add(new Message { Topic = $"{Data.TopicPrefix[(int)Prefix.Status]}/{Data.Topic}/{endpoint}", Payload = payload, Retained = false });
        }
        
        private void _PublishLevel(ushort channel, ushort level, bool levelOnly=false)
        {
            string powerPayload = (levelOnly) ? "" : $"\"POWER{channel}\":\"ON\",";
            string payload = $"{{{powerPayload}\"Channel{channel}\":{level}}}";

            DebugPrint($"< [TX] Publish Level for channel {channel} = {level}", DebugColor.TX);

            _outgoingMessages.Add(new Message { Topic = $"{Data.TopicPrefix[(int)Prefix.Status]}/{Data.Topic}/RESULT", Payload = payload, Retained = false });
        }

        private void _PublishState(bool telemetry=false, bool result=true)
        {
            string state = Data.State;

            if (result)
            {
                DebugPrint("< [TX] Publish STATE to RESULT", DebugColor.TX);
                _outgoingMessages.Add(new Message { Topic = $"{Data.TopicPrefix[(int)Prefix.Status]}/{Data.Topic}/RESULT", Payload = state, Retained = false });
            }
            if (telemetry)
            {
                DebugPrint("< [TX] Publish STATE to TELEMETRY", DebugColor.TX);
                _outgoingMessages.Add(new Message { Topic = $"{Data.TopicPrefix[(int)Prefix.Telemetry]}/{Data.Topic}/STATE", Payload = state, Retained = false });
            }
        }
        
        private void _ProcessCommand(string endpoint, string payload="")
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
                        DebugPrint($"> [RX] Power state request for channel {channel}", DebugColor.RX);
                        _PublishPower(channel, Data.Channels[channel - 1].Power);
                    }
                    else if (payload == "ON")
                    {
                        DebugPrint($"> [RX] Power ON channel {channel}", DebugColor.RX);
                        PowerRequestDelegate?.Invoke(channel, SPlusBool.TRUE);
                    }
                    else if (payload == "OFF")
                    {
                        DebugPrint($"> [RX] Power OFF channel {channel}", DebugColor.RX);
                        PowerRequestDelegate?.Invoke(channel, SPlusBool.FALSE);
                    }
                    else
                    {
                        DebugPrint($"! [RX] Invalid payload [{payload}] for Power command", DebugColor.Error);
                    }
                    break;

                case "Channel":
                    if (string.IsNullOrEmpty(payload))
                    {
                        DebugPrint($"> [RX] Channel state request for channel {channel}", DebugColor.RX);
                        _PublishLevel(channel, Data.Channels[channel - 1].Level, levelOnly: true);
                    }
                    else
                    {
                        ushort level = 0;
                        if (ushort.TryParse(payload, out level))
                        {
                            DebugPrint($"> [RX] Set Channel {channel} to {level}", DebugColor.RX);
                            LevelRequestDelegate?.Invoke(channel, level);
                        }
                        else
                        {
                            DebugPrint($"! [RX] Invalid payload [{payload}] for Channel command", DebugColor.Error);
                        }
                    }
                    break;
                
                case "NoDelay":
                    break;

                case "STATUS":
                    DebugPrint($"> [RX] STATUS [{payload}] request", DebugColor.RX);
                    switch (payload)
                    {
                        case "1":
                            DebugPrint("< [TX] Publish STATUS1", DebugColor.TX);
                            _outgoingMessages.Add(Data.Status1Message);
                            break;
                        
                        case "11":
                            DebugPrint("< [TX] Publish STATUS11", DebugColor.TX);
                            _outgoingMessages.Add(Data.Status11Message);
                            break;
                    }
                    break;

                case "STATE":
                    DebugPrint($"> [RX] STATE request", DebugColor.RX);
                    _PublishState();
                    break;

                default:
                    DebugPrint($"> [RX] ? Unknown Directive [{directive}]", DebugColor.Error);
                    break;
            }
        }

        private Timer _stateUpdateTimer;
        
        private async Task _Client()
        {
            DebugPrint("+ CLIENT task started", DebugColor.Start);

            try
            {
                var mqttFactory = new MqttFactory();

                using (var managedMqttClient = mqttFactory.CreateManagedMqttClient())
                {
                    async Task __PublishMessage(Message msg) => await managedMqttClient.EnqueueAsync(msg.Topic, msg.Payload, retain: msg.Retained);

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
                                        _ProcessCommand(commandData[0], commandData[1]);
                                    }
                                    else
                                    {
                                        _ProcessCommand(command);
                                    }
                                }
                            }
                            else
                            {
                                _ProcessCommand(endpoint, payload);
                            }
                        }

                        else
                        { 
                            DebugPrint($"RX: ! Unhandled message [topic: {e.ApplicationMessage.Topic}] [payload: {payload}]", DebugColor.Error);
                        }

                        return Task.CompletedTask;
                    };

                    managedMqttClient.ConnectedAsync += async e =>
                    {
                        DebugPrint($"+ Connected to broker @ {BrokerAddress}:{BrokerPort}", DebugColor.Start);
                        Data.MqttCount++;
                        DebugPrint("< [TX] Publish ONLINE state", DebugColor.TX);
                        await __PublishMessage(Data.OnlineMessage);

                        Message configMessage = Data.DiscoveryConfigMessage;
                        Message sensorsMessage = Data.DiscoverySensorsMessage;
                        
                        if (_autoDiscovery)
                        {

                            DebugPrint("+ Autodiscovery ENABLED", DebugColor.Info);
                            DebugPrint($"< [TX] Publish to tasmota/discovery/{Data.MACAddress}", DebugColor.TX);
                            await __PublishMessage(configMessage);
                            await __PublishMessage(sensorsMessage);
                        }
                        else
                        {
                            DebugPrint("- Autodiscovery DISABLED", DebugColor.Info);
                            DebugPrint($"* Clearing any retained topics at tasmota/discovery/{Data.MACAddress}", DebugColor.TX);
                            await __PublishMessage(new Message { Topic = configMessage.Topic, Payload = "", Retained = true });
                            await __PublishMessage(new Message { Topic = configMessage.Topic, Payload = "", Retained = false });
                            await __PublishMessage(new Message { Topic = sensorsMessage.Topic, Payload = "", Retained = true });
                            await __PublishMessage(new Message { Topic = sensorsMessage.Topic, Payload = "", Retained = false });
                        }
                        DebugPrint("< [TX] Publish STATE to TELEMETRY", DebugColor.TX);
                        await __PublishMessage(Data.StateMessage);

                        //start timer to publish 'state' unsolicited to telemetry every 300 seconds                    
                        if (_stateUpdateTimer != null)
                        {
                            _stateUpdateTimer.Dispose();
                            _stateUpdateTimer = null;
                        }
                        _stateUpdateTimer = new Timer((state) => _PublishState(result: false, telemetry: true), null, 300000, 300000);

                        DebugPrint("< [TX] Publish INFO1, INFO2, INFO3", DebugColor.TX);
                        await __PublishMessage(Data.Info1Message);
                        await __PublishMessage(Data.Info2Message);
                        await __PublishMessage(Data.Info3Message);
                    };

                    managedMqttClient.DisconnectedAsync += e =>
                    {
                        DebugPrint("! The managed MQTT client is DISCONNECTED.", DebugColor.Error);
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

                    DebugPrint($"* Subscribing to default topic [{Data.TopicPrefix[(int)Prefix.Command]}/{Data.Topic}/#]", DebugColor.TX);
                    await managedMqttClient.SubscribeAsync($"{Data.TopicPrefix[(int)Prefix.Command]}/{Data.Topic}/#");

                    DebugPrint($"* Subscribing to fallback topic [{Data.FallbackTopic}#]", DebugColor.TX);
                    await managedMqttClient.SubscribeAsync($"{Data.FallbackTopic}#");

                    DebugPrint($"* Subscribing to group topic [{Data.GroupTopic}#]", DebugColor.TX);
                    await managedMqttClient.SubscribeAsync($"{Data.GroupTopic}#");

                    await managedMqttClient.StartAsync(options);

                    while (!_stopRequested)
                    {
                        if (managedMqttClient.IsConnected)
                        {
                            if (_outgoingMessages.TryTake(out Message msg, 250))
                            {
                                await __PublishMessage(msg);
                            }
                        }
                        else
                        {
                            DebugPrint("~ Waiting for connection...", ANSIColor.Yellow);
                            Thread.Sleep(5000);
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                DebugPrint("! CLIENT task aborted", DebugColor.Error);
            }
            catch (TaskCanceledException)
            {
                DebugPrint("! CLIENT task cancelled", DebugColor.Error);
            }
            DebugPrint("- CLIENT task exited", DebugColor.Stop);
        }
    }
}
