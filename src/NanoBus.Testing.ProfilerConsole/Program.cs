using System;
using System.Threading;

namespace NanoBus.Services.Testing.ProfilerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += ((sender, eventArgs) => Console.WriteLine("Error: {0}", eventArgs.ExceptionObject));
            try
            {
                new NanoBusTests().NanoBus_SingleClient_Test().Wait();
            }
            catch (Exception) { }
            try
            {
                new NanoBusTests().NanoBus_SingleClient_PerformanceTest().Wait();
            }
            catch (Exception) { }
            try
            {
                new NanoBusTests().NanoBus_PooledClient_PerformanceTest().Wait();
            }
            catch (Exception) { }
            try
            {
                new NanoBusTests().NanoBus_SeveralClient_PerformanceTest().Wait();
            }
            catch (Exception) { }
            try
            {
                new NanoBusTests().NanoBus_SeveralClient_SomeSubscriptions_PerformanceTest().Wait();
            }
            catch (Exception) { }
            try
            {
                new NanoBusTests().NanoBus_SeveralClient_MediumSubscriptions_PerformanceTest().Wait();
            }
            catch (Exception) { }
            try
            {
                new NanoBusTests().NanoBus_SomeClient_DozensOfSubscriptions_PerformanceTest().Wait();
            }
            catch (Exception) { }
            try
            {
                new NanoBusTests().NanoBus_SeveralClient_DozensOfSubscriptions_PerformanceTest().Wait();
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
