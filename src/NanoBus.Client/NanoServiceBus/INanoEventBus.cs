using System;
using System.Threading.Tasks;

namespace NanoBus.Client.NanoServiceBus
{
    internal interface INanoEventBus : IDisposable
    {
        Task DistributeAsync(NanoPublishMessage publishMessage);
    }
}