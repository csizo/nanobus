using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NanoBus.Client.NanoServiceBus
{
    internal class NanoSubscription<TDocument, TMessage> : INanoSubscription
        where TDocument : IDomainDocument
        where TMessage : IDomainMessage<TDocument>
    {
        private static readonly TraceSource Trace = new TraceSource("Framework.Runtime.Azure.Messaging.NanoBus");

        private readonly ConcurrentDictionary<Guid, NanoHandler<TDocument, TMessage>> _handlers = new ConcurrentDictionary<Guid, NanoHandler<TDocument, TMessage>>();

        public ConcurrentDictionary<Guid, NanoHandler<TDocument, TMessage>> Handlers
        {
            get { return _handlers; }
        }

        public void Dispose()
        {
            foreach (var handler in _handlers.Values)
            {
                handler.Dispose();
            }
        }

        public Task InvokeHandlers(object message)
        {
            return InvokeHandlers((TMessage)message);
        }

        public Task InvokeHandlers(TMessage message)
        {
            var tasks = new List<Task>(_handlers.Count);


            Trace.TraceEvent(TraceEventType.Verbose, 1000, () => "Invoking {0} callbacks (client side)", _handlers.Values.Count.ToString());

            tasks.AddRange(_handlers.Values.Select(handler => handler.HandleMessageAsync(message)));

            if (tasks.Count == 0)
                throw new InvalidOperationException();

            return Task.WhenAll(tasks);
        }

        public IDisposable AddHandler(Func<TMessage, Task> callback)
        {
            var handler = new NanoHandler<TDocument, TMessage>(this, callback);
            _handlers.TryAdd(handler.HandlerId, handler);
            return handler;
        }


    }
}