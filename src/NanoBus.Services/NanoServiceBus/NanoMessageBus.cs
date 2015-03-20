using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NanoBus.Service.NanoServiceBus
{
    internal class NanoMessageBus : IDisposable
    {
        private readonly string _eventTypeName;
        private readonly NanoEventBus _eventBus;

        private readonly ConcurrentDictionary<Guid, NanoHandler> _handlers = new ConcurrentDictionary<Guid, NanoHandler>();

        private readonly ConcurrentDictionary<NanoDocumentId, List<NanoHandler>> _handlersByDocumentId =
            new ConcurrentDictionary<NanoDocumentId, List<NanoHandler>>();

        //ConcurrentDictionary<Guid, HandlerContainer> _sessionHandlers = new ConcurrentDictionary<Guid, HandlerContainer>();

        //internal class HandlerContainer
        //{
        //    public Guid SessionId { get; private set; }

        //    ConcurrentDictionary<Guid, NanoHandler> _allHandlers = new ConcurrentDictionary<Guid, NanoHandler>();

        //    ConcurrentDictionary<NanoDocumentId, List<NanoHandler>> _subscriptionHandlers = new ConcurrentDictionary<NanoDocumentId, List<NanoHandler>>();


        //}
        public NanoMessageBus(string eventTypeName, NanoEventBus eventBus)
        {
            _eventTypeName = eventTypeName;
            _eventBus = eventBus;
        }

        public NanoEventBus EventBus
        {
            get { return _eventBus; }
        }

        public string EventTypeName
        {
            get { return _eventTypeName; }
        }

        public ConcurrentDictionary<Guid, NanoHandler> Handlers
        {
            get { return _handlers; }
        }

        public ConcurrentDictionary<NanoDocumentId, List<NanoHandler>> HandlersByDocumentId
        {
            get { return _handlersByDocumentId; }
        }

        public void Dispose()
        {
            foreach (var handler in _handlers.Values)
            {
                handler.Dispose();
            }
            NanoMessageBus nanoMessageBus;
            _eventBus.NanoMessageBusses.TryRemove(_eventTypeName, out nanoMessageBus);
        }

        public async Task HandleMessage(Guid clientId, NanoPublishMessage nanoPublishMessage)
        {
            var tasks = new List<Task>();
            NanoMessage publishMessage = null;

            List<NanoHandler> nanoSubscribers;


            if (nanoPublishMessage.NanoDocumentId != NanoDocumentId.Empty)
                if (_handlersByDocumentId.TryGetValue(nanoPublishMessage.NanoDocumentId, out nanoSubscribers))
                {
                    publishMessage = nanoPublishMessage.ToNewPublishMessage();
                    //TODO: find a solution elliminating where condition...
                    tasks.AddRange(nanoSubscribers.AsSnapshot().Where(a => a.ClientId != clientId).Select(subscriber => subscriber.HandleMessageAsync(publishMessage)));
                }
            if (_handlersByDocumentId.TryGetValue(NanoDocumentId.Empty, out nanoSubscribers))
            {
                publishMessage = publishMessage ?? nanoPublishMessage.ToNewPublishMessage();
                //TODO: find a solution elliminating where condition...
                tasks.AddRange(nanoSubscribers.AsSnapshot().Where(a => a.ClientId != clientId).Select(subscriber => subscriber.HandleMessageAsync(publishMessage)));
            }

            await TaskHelpers.Iterate(tasks);

        }

        public NanoHandler AddHandler(Guid clientId, NanoDocumentId nanoDocumentId, Func<NanoMessage, Task> onMessage)
        {
            var handler = new NanoHandler(this, clientId, nanoDocumentId, onMessage);

            _handlers.TryAdd(handler.HandlerId, handler);

            var nanoHandlers = _handlersByDocumentId.GetOrAdd(nanoDocumentId, new List<NanoHandler>());
            lock (nanoHandlers)
            {
                nanoHandlers.Add(handler);
            }

            return handler;
        }


    }
}