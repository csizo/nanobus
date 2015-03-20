using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NanoBus.Service;

namespace NanoBus.Server
{
    class Program
    {
        static void Main(string[] args)
        {

            var tokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) => tokenSource.Cancel();
            
            Console.WriteLine("Running nano bus service...");
            
            RunNanoBusService(tokenSource.Token).Wait();
        }

        private static async Task RunNanoBusService(CancellationToken cancellationToken)
        {
            var o = new NanoBusService();
            await o.RunAsync("http://localhost:8091/NanoBus/", cancellationToken);
        }
    }
}
