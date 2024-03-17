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
        public static Cresmota.CresmotaDevice cresmota = new Cresmota.CresmotaDevice();

        static void Main(string[] args)
        {
            cresmota.PowerRequestDelegate += powerRequest;
            cresmota.LevelRequestDelegate += levelRequest;

            cresmota.StartDebugging();
            
            cresmota.ProgramSlot = 1;
            cresmota.ID = 2;
            cresmota.DeviceName = "DesktopTestApplication";
            cresmota.BrokerAddress = "mqttbroker";
            cresmota.Username = "mqttuser";
            cresmota.Password = "mqttpassword";
            cresmota.ReportAs = CresmotaDevice.RELAYS;

            for (int i = 0; i < 8; i++)
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

        public static void powerRequest(ushort channel, ushort state)
        {
            cresmota.SetPower(channel, state);            
        }

        public static void levelRequest(ushort channel, ushort level)
        {
            cresmota.SetLevel(channel, level);
        }

    }
}
