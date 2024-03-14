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
            Cresmota.CresmotaDevice cresmota = new Cresmota.CresmotaDevice();

            cresmota.StartDebugging();
            
            cresmota.ProgramSlot = 1;
            cresmota.ID = 2;
            cresmota.DeviceName = "DesktopTestApplication";
            cresmota.BrokerAddress = "mqttbroker";
            cresmota.Username = "mqttuser";
            cresmota.Password = "mqttpassword";
            cresmota.ReportAs = CresmotaDevice.RELAYS;

            for (int i = 0; i < CresmotaDevice.MaxChannels; i++)
            {
                if (i % 2 == 0)
                    cresmota.AddBasic($"Channel {i + 1:D3}");
                else
                    cresmota.AddLight($"Channel {i + 1:D3}");
            }

            cresmota.Start();
            _ = Console.Read();
            cresmota.Stop();    
        }
    }
}
