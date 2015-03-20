using System;
using System.Threading.Tasks;

namespace NanoBus.Client.NanoServiceBus
{
    internal interface INanoSubscription : IDisposable
    {
        Task InvokeHandlers(object message);
    }
}