using System;
using System.Threading.Tasks;

namespace NanoBus.Client.NanoServiceBus
{
    internal class NanoHandler<TDocument, TMessage> : IDisposable
        where TDocument : IDomainDocument
        where TMessage : IDomainMessage<TDocument>
    {
        private readonly Func<TMessage, Task> _callback;
        private readonly NanoSubscription<TDocument, TMessage> _nanoSubscription;
        private readonly Guid _handlerId = Guid.NewGuid();

        public NanoHandler(NanoSubscription<TDocument, TMessage> nanoSubscription, Func<TMessage, Task> callback)
        {
            _nanoSubscription = nanoSubscription;
            _callback = callback;
        }

        public Guid HandlerId
        {
            get { return _handlerId; }
        }

        public void Dispose()
        {
            NanoHandler<TDocument, TMessage> v;
            _nanoSubscription.Handlers.TryRemove(_handlerId, out v);
        }

        public Task HandleMessageAsync(TMessage message)
        {
            return _callback(message);
        }
    }
}