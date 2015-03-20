using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NanoBus.Services.Testing.NanoServiceBus;

namespace NanoBus.Services.Testing.ProfilerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += ((sender, eventArgs) => Console.WriteLine("Error: {0}", eventArgs.ExceptionObject));
            try
            {
                new NanoServiceBusTests().NanoServiceBus_AddHandler_Then_HandleMessage().Wait();
            }
            catch (Exception) { }
            try
            {
                new NanoServiceBusTests().NanoServiceBus_AddHandlers_Then_HandleMessage().Wait();
            }
            catch (Exception) { }

            if (Environment.UserInteractive)
            {
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }
        }
    }
}
