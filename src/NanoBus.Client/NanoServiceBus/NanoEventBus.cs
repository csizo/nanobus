using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NanoBus.Client.NanoServiceBus
{
    internal class NanoEventBus<TDocument> : INanoEventBus, INanoEventBus<TDocument> where TDocument : IDomainDocument
    {
        private static readonly TraceSource Trace = new TraceSource("Framework.Runtime.Azure.Messaging.NanoBus");

        private readonly NanoBusClient _nanoBusClient;
        private readonly Dictionary<string, Type> _messageTypeMap = new Dictionary<string, Type>();

        private readonly Dictionary<NanoSubsciptionKey, INanoSubscription> _subscriptions = new Dictionary<NanoSubsciptionKey, INanoSubscription>();

        public NanoEventBus(NanoBusClient nanoBusClient)
        {
            _nanoBusClient = nanoBusClient;
        }

        public NanoBusClient NanoBusClient
        {
            get { return _nanoBusClient; }
        }

        public Dictionary<NanoSubsciptionKey, INanoSubscription> Subscriptions
        {
            get { return _subscriptions; }
        }

        public void Dispose()
        {
            foreach (var value in _subscriptions.Values)
            {
                value.Dispose();
            }
        }

        public Task DistributeAsync(NanoPublishMessage publishMessage)
        {
            Type messageType;
            if (!_messageTypeMap.TryGetValue(publishMessage.MessageTypeName, out messageType))
                return Task.FromResult(0);

            var message = publishMessage.DeseralizePayload(messageType);

            INanoSubscription nanoSubscription;
            var tasks = new List<Task>(2);

            var key = new NanoSubsciptionKey(messageType, publishMessage.NanoDocumentId);
            if (_subscriptions.TryGetValue(key, out nanoSubscription))
            {
                var task = nanoSubscription.InvokeHandlers(message);
                tasks.Add(task);
            }

            key = new NanoSubsciptionKey(messageType, NanoDocumentId.Empty);
            if (_subscriptions.TryGetValue(key, out nanoSubscription))
            {
                var task = nanoSubscription.InvokeHandlers(message);
                tasks.Add(task);
            }

            return Task.WhenAll(tasks);
        }

        public async Task PublishAsync<TMessage>(TMessage message) where TMessage : IDomainMessage<TDocument>
        {
            var nanoMessage = NanoMessage.CreatePublishMessage<TDocument, TMessage>(NanoBusClient.ClientId, message, false);

            EnqueueMessage(nanoMessage);

            //distribute local callbacks without server roundtrip
            await Task.Run(() => NanoBusClient.DistributePublishMessageAsync(nanoMessage.PublishMessage).ConfigureAwait(false));

        }

        private void EnqueueMessage(NanoMessage nanoMessage)
        {
            _nanoBusClient.NanoConnections.Next().EnqueueMessage(nanoMessage);
        }

        public IDisposable Subscribe<TMessage>(Func<TMessage, Task> callback) where TMessage : IDomainMessage<TDocument>
        {
            var key = new NanoSubsciptionKey(typeof(TMessage), new NanoDocumentId(null));
            var subscription = (NanoSubscription<TDocument, TMessage>)_subscriptions.GetOrAdd(key, CreateSubscription<TMessage>);

            return subscription.AddHandler(callback);
        }

        private INanoSubscription CreateSubscription<TMessage>(NanoSubsciptionKey subscriptionKey) where TMessage : IDomainMessage<TDocument>
        {
            if (_messageTypeMap.TryAdd(typeof(TMessage).FullName, typeof(TMessage)))
            {
                var subscription = new NanoSubscription<TDocument, TMessage>();
                var nanoMessage = NanoMessage.CreateSubscribeMessage<TDocument, TMessage>(NanoBusClient.ClientId, subscriptionKey.NanoDocumentId);
                EnqueueMessage(nanoMessage);

                return subscription;
            };

            throw new InvalidOperationException();
        }

        public IDisposable Subscribe<TMessage>(Func<TMessage, Task> onMessage, Guid documentId)
            where TMessage : IDomainMessage<TDocument>
        {
            var key = new NanoSubsciptionKey(typeof(TMessage), new NanoDocumentId(documentId));
            var subscription = (NanoSubscription<TDocument, TMessage>)_subscriptions.GetOrAdd(key, CreateSubscription<TMessage>);

            return subscription.AddHandler(onMessage);
        }

    }
}