using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Cresmota;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DesktopTestApplication
{
    internal class DesktopTestApplication
    {
        static void Main(string[] args)
        {
            //Cresmota.ManagedClientTest.Connect_Client().RunSynchronously();
            Cresmota.CresmotaDevice cresmota = new Cresmota.CresmotaDevice();

            cresmota.StartDebugging();
            
            cresmota.ProgramSlot = 1;
            cresmota.ID = 1;
            cresmota.DeviceName = "TestDevice";
            cresmota.BrokerAddress = "mqttbroker";
            cresmota.Username = "mqttuser";
            cresmota.Password = "mqttpassword";

            for (int i = 0; i < CresmotaDevice.MaxChannels; i++)
            {
                cresmota.AddRelay($"Channel {i + 1:D2}");
            }

            //cresmota.Config.SetOption["30"] = 1;
            //cresmota.Config.SetOption["68"] = 1;
            //cresmota.Config.LightSubtype = 1;

            cresmota.Start();
            Thread.Sleep(30000);
            cresmota.Stop();    
            Console.WriteLine("Press any key to exit...");
            _ = Console.Read();
        }
    }
}
