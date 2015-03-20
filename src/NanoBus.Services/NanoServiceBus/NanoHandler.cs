using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NanoBus.Service.NanoServiceBus
{
    internal class NanoHandler : IDisposable
    {
        private static readonly TraceSource Trace = new TraceSource("NanoBus.Services");

        private readonly NanoMessageBus _nanoMessageBus;
        private readonly Func<NanoMessage, Task> _onMessage;

        public NanoHandler(NanoMessageBus nanoMessageBus, Guid clientId, NanoDocumentId nanoDocumentId, Func<NanoMessage, Task> onMessage)
        {
            _nanoMessageBus = nanoMessageBus;
            _onMessage = onMessage;
            ClientId = clientId;
            NanoDocumentId = nanoDocumentId;
            HandlerId = Guid.NewGuid();
        }

        public Guid HandlerId { get; private set; }
        public Guid ClientId { get; private set; }
        public NanoDocumentId NanoDocumentId { get; private set; }

        public void Dispose()
        {
            NanoHandler nanoHandler;
            _nanoMessageBus.Handlers.TryRemove(HandlerId, out nanoHandler);
            List<NanoHandler> list;
            if (_nanoMessageBus.HandlersByDocumentId.TryGetValue(NanoDocumentId, out list))
            {
                lock (list)
                {
                    list.RemoveAll(a => a == this);
                }
            }
        }

        public Task HandleMessageAsync(NanoMessage nanoMessage)
        {
            return _onMessage(nanoMessage);
        }
    }
}