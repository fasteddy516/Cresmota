using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Cresmota
{
    public partial class CresmotaDevice : IDisposable
    {
        private Task ClientTask { get; set; }
        private bool stopRequested = false;


        public CresmotaDevice()
        {
            DebugPrint("+ CONSTRUCTOR started");
            DebugPrint("- CONSTRUCTOR complete");
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
                        Console.WriteLine($"Received application message {e.ApplicationMessage.Topic} = {e.ApplicationMessage.ConvertPayloadToString()}.");
                        return Task.CompletedTask;
                    };

                    managedMqttClient.ConnectedAsync += e =>
                    {
                        Console.WriteLine("The managed MQTT client is CONNECTED.");
                        return Task.CompletedTask;
                    };

                    managedMqttClient.DisconnectedAsync += e =>
                    {
                        Console.WriteLine("The managed MQTT client is DISCONNECTED.");
                        return Task.CompletedTask;
                    };

                    var options = new ManagedMqttClientOptionsBuilder()
                        .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                        .WithClientOptions(new MqttClientOptionsBuilder()
                            .WithClientId("Client1")
                            .WithTcpServer("127.0.0.1")
                            //.WithCredentials("username", "password")
                            .WithCleanSession()
                            .Build())
                        .Build();


                    await managedMqttClient.SubscribeAsync("TestSubscriptionTopic");
                    await managedMqttClient.StartAsync(options);

                    int testValue = 0;

                    while (!stopRequested)
                    {
                        if (managedMqttClient.IsConnected)
                        {
                            Console.WriteLine($"Published testValue={testValue}");
                            testValue++;
                            await managedMqttClient.EnqueueAsync("TestTopic", testValue.ToString());
                        }
                        else
                        {
                            Console.WriteLine("Waiting for connection...");
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
