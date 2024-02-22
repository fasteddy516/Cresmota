using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Cresmota;

namespace DesktopTestApplication
{
    internal class DesktopTestApplication
    {
        static void Main(string[] args)
        {
            //Cresmota.ManagedClientTest.Connect_Client().RunSynchronously();
            Cresmota.CresmotaDevice cresmota = new Cresmota.CresmotaDevice();
            cresmota.StartDebugging();
            cresmota.Start();
            Thread.Sleep(30000);
            cresmota.Stop();    
            Console.WriteLine("Press any key to exit...");
            _ = Console.Read();
        }
    }
}
