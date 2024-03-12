﻿using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Threading;
using System.Threading.Tasks;
using System;
using Crestron.SimplSharp;
using System.Diagnostics.Eventing.Reader;
using System.Collections.Generic;

namespace Cresmota
{
    public partial class CresmotaDevice : IDisposable
    {
        public const ushort MaxProgramSlots = 10;
        public const ushort MaxChannels = 8;

        public TasmotaConfig Config = new TasmotaConfig();
        public TasmotaSensors Sensors { get; private set; } = new TasmotaSensors();

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
                DebugPrint($"* ProgramSlot set to {_programSlot}");
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
                DebugPrint($"* ID set to {_id}");
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
                DebugPrint($"@ Set DeviceName to {value}");
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

        public CresmotaDevice()
        {
            DebugPrint("+ CONSTRUCTOR started");
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
                DebugPrint($"* LAN = {ipAddress} ({macAddress})");
            }
            macAddress = $"02{ProgramSlot:X2}{ID:X2}" + macAddress.Replace(":", "").ToUpper().Substring(6);
            Config.MACAddress = macAddress;
            Config.IPAddress = ipAddress;
            DebugPrint($"* Cresmota MAC = {Config.MACAddress}");

            ClientID = new SimplSharpString($"Cresmota-{ProgramSlot:D2}-{ID:D2}");
            DebugPrint($"* Cresmota MQTT Client ID = {ClientID}");

            if (string.IsNullOrEmpty(Config.Topic))
            {
                Config.Topic = ClientID.ToString();
            }
            DebugPrint($"* Cresmota MQTT Topic = {Config.Topic}");
            DebugPrint($"* Cresmota MQTT Broker = {BrokerAddress}:{BrokerPort}");

            if (string.IsNullOrEmpty(Config.FriendlyName[0]))
            {
                Config.FriendlyName[0] = Config.DeviceName;
            }
            
            if (string.IsNullOrEmpty(Config.HostName))
            {
                Config.HostName = ClientID.ToString();
            }

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

        public void Add(SimplSharpString name)
        {
            if (ChannelCount >= MaxChannels)
            {
                DebugPrint($"! Cannot add {name} - ChannelCount is at maximum ({MaxChannels})");
                return;
            }
            Config.FriendlyName[ChannelCount] = name.ToString();
            Config.Relay[ChannelCount] = 1;
            ChannelCount++;
            DebugPrint($"> Channel [{ChannelCount:D2}] = {name}");
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
                        DebugPrint($"Received application message {e.ApplicationMessage.Topic} = {e.ApplicationMessage.ConvertPayloadToString()}.");
                        return Task.CompletedTask;
                    };

                    managedMqttClient.ConnectedAsync += async e =>
                    {
                        DebugPrint($"+ Connected to broker @ {BrokerAddress}:{BrokerPort}");
                        if (_autoDiscovery)
                        {

                            DebugPrint("+ Autodiscovery ENABLED");
                            DebugPrint($"  > Publishing to tasmota/discovery/{Config.MACAddress}");
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.MACAddress}/config", Config.ToString(), retain: true);
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.MACAddress}/sensors", Sensors.ToString(), retain: true);
                        }
                        else
                        {
                            DebugPrint("- Autodiscovery DISABLED");
                            DebugPrint($"  > Clearing any retained topics at tasmota/discovery/{Config.MACAddress}");
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.Topic}/config", "", retain: true);
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.Topic}/config", "", retain: false);
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.Topic}/sensors", "", retain: true);
                            await managedMqttClient.EnqueueAsync($"tasmota/discovery/{Config.Topic}/sensors", "", retain: false);
                        }
                        DebugPrint("* Publishing ONLINE state");
                        await managedMqttClient.EnqueueAsync($"tele/{Config.Topic}/LWT", Config.OnlinePayload, retain: true);
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


                    await managedMqttClient.SubscribeAsync("TestSubscriptionTopic");
                    await managedMqttClient.StartAsync(options);

                    int testValue = 0;

                    while (!stopRequested)
                    {
                        if (managedMqttClient.IsConnected)
                        {
                            DebugPrint($"Published testValue={testValue}");
                            testValue++;
                            await managedMqttClient.EnqueueAsync("TestTopic", testValue.ToString());
                        }
                        else
                        {
                            DebugPrint("Waiting for connection...");
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
